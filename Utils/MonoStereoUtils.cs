using ATL;
using MonoStereo;
using MonoStereo.SampleProviders;
using MonoStereoMod.Systems;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ReLogic.Content.Sources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Exceptions;

namespace MonoStereoMod.Utils
{
    internal static partial class MonoStereoUtils
    {
        // These utilities are all available in vanilla source, but are
        // either internal or private. We can just re-implement them here.
        #region Replacement Vanilla Utilities

        public static void RunOnMainThreadAndWait(Action action) => Main.RunOnMainThread(action).GetAwaiter().GetResult();

        private static readonly char[] nameSplitters = ['/', ' ', ':'];
        public static void SplitName(string name, out string domain, out string subName)
        {
            int slash = name.IndexOfAny(nameSplitters); // slash is the canonical splitter, but we'll accept space and colon for backwards compatibility, just in case
            if (slash < 0)
                throw new MissingResourceException(Language.GetTextValue("tModLoader.LoadErrorMissingModQualifier", name));

            domain = name.Substring(0, slash);
            subName = name.Substring(slash + 1);
        }

        #endregion

        // Provides standardized MonoStereo metadata management utilities.
        #region MonoStereo Metadata Utilities

        // Reads comments of a track that is NOT a .ogg file.
        // We use ATL for this.
        public static Dictionary<string, string> ReadComments(this Stream stream)
        {
            long position = stream.Position;
            Dictionary<string, string> comments = [];

            void AddComment(string name, string value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    comments.Add(name, value);
            }

            Track track = new(stream);

            AddComment("Artist", track.Artist);
            AddComment("Title", track.Title);
            AddComment("Album", track.Album);
            AddComment("Comments", track.Comment);

            foreach (var keyValuePair in track.AdditionalFields)
                AddComment(keyValuePair.Key, keyValuePair.Value);

            if (stream.CanSeek)
                stream.Position = position;

            return comments;
        }

        // Converts an ISampleProvider of any formatting (sample rate or channels) to
        // a standardized, 44.1kHz 2 channel stream. Also modifies loopStart and loopEnd
        // tags to account for sample size adjustment.
        public static ISampleProvider Reformat(this ISampleProvider provider, ref long loopStart, ref long loopEnd)
        {
            if (provider.WaveFormat.SampleRate != AudioStandards.SampleRate)
            {
                float scalar = AudioStandards.SampleRate / (float)provider.WaveFormat.SampleRate;
                provider = new WdlResamplingSampleProvider(provider, AudioStandards.SampleRate);

                if (loopStart > 0)
                {
                    loopStart = (long)(loopStart * scalar);
                    loopStart -= loopStart % provider.WaveFormat.Channels;
                }

                if (loopEnd > 0)
                {
                    loopEnd = (long)(loopEnd * scalar);
                    loopEnd -= loopEnd % provider.WaveFormat.Channels;
                }
            }

            if (provider.WaveFormat.Channels != AudioStandards.ChannelCount)
            {
                if (provider.WaveFormat.Channels == 1)
                    provider = new MonoToStereoSampleProvider(provider);

                else
                    throw new ArgumentException("Song file must be in either mono or stereo!", nameof(provider));
            }

            return provider;
        }
        #endregion

        // Utilities for integrating MonoStereo into tMod.
        #region MonoStereo Mod Utilities

        // Adds the MonoStereo content source to a list of sources.
        // This source acts as a psuedo-texture pack, so that we can directly override
        // vanilla's WaveBank readers, without needing ModScenes for every single track.
        public static List<IContentSource> InsertMonoStereoSource(this List<IContentSource> contentSources)
        {
            var source = new CueReadingContentSource();
            int index = contentSources.FindIndex(0, c => c is XnaDirectContentSource);
            contentSources.Insert(index + 1, source);
            return contentSources;
        }

        // Removes the custom MonoStereo source from a list of sources.
        public static List<IContentSource> RemoveMonoStereoSource(this List<IContentSource> contentSources)
        {
            int index = contentSources.FindIndex(c => c is CueReadingContentSource);
            if (index >= 0)
                contentSources.RemoveAt(index);
            return contentSources;
        }

        // A utility for doing a looped read when looping tags are present.
        // sampleProvider and seekSource are two different parameters, as
        // there is usually an underlying sampleProvider, but an encapsulating
        // class for containing the ability to seek.
        public static int LoopedRead(this ISampleProvider sampleProvider, float[] buffer, int offset, int count, ISeekableSampleProvider seekSource, bool isLooped, long length, long loopStart, long loopEnd)
        {
            int samplesCopied = 0;

            do
            {
                long endIndex = length;

                if (isLooped && loopEnd != -1)
                    endIndex = loopEnd;

                long samplesAvailable = endIndex - seekSource.Position;
                long samplesRemaining = count - samplesCopied;

                int samplesToCopy = (int)Math.Min(samplesAvailable, samplesRemaining);

                if (samplesToCopy > 0)
                    samplesCopied += sampleProvider.Read(buffer, offset + samplesCopied, samplesToCopy);

                if (isLooped && seekSource.Position == endIndex)
                {
                    long startIndex = Math.Max(0, loopStart);
                    seekSource.Position = startIndex;
                }
            }
            while (isLooped && samplesCopied < count);

            return samplesCopied;
        }

        // Applies vanilla's volume curve, but slightly
        // modified to allow tracks to smoothly slope to 0.
        public static float GetRealVolume(this float value)
        {
            float exponent = (value * 31f - 36.94f) * 0.05f;
            float volume = MathF.Pow(10f, exponent);
            float naturalEndFadeout = Terraria.Utils.GetLerpValue(0f, 0.074f, value, true);
            return volume * naturalEndFadeout;
        }

        // Registers custom music
        public static void RegisterCustomMusic(string musicPath, string extension)
        {
            // Reserve a music slot, and attach the mod name to the path.
            int id = ReserveMusicLoaderID();

            // Sets MusicLoader.musicByPath (for use by MusicLoader.GetMusic()) as well as
            // MusicLoader.musicExtensions (for use to determine loading from our own cache)
            MusicLoaderMusicByPath()[musicPath] = id;
            MusicLoaderMusicExtensions()[musicPath] = extension;
        }

        #endregion

        // We do not have a way to read ADPCM samples in real time, as the encoding is very complex
        // and retrieving a specific number of samples is not easy. This serves as a way to
        // convert ADPCM encoded data into readable, PCM samples. Credit to MonoGame for this logic.
        //
        // In the future it may be worth finding a way to convert data in real-time. This would have a slightly
        // negative impact on performance (and mental sanity), but would have a positive impace on memory profile.
        // Could save maybe 20mb of RAM on average, with some instances being higher and some lower. Depends
        // on the length of a track that is currently playing, as well as how many are currently playing. PCM data
        // for ADPCM tracks is only cached when that track is actively playing so as to reduce memory overhead
        // as much as possible.
        #region ADPCM Conversion

        private static readonly int[] adaptationTable =
        [
            230,
            230,
            230,
            230,
            307,
            409,
            512,
            614,
            768,
            614,
            512,
            409,
            307,
            230,
            230,
            230
        ];

        private static readonly int[] adaptationCoeff1 =
        [
            256,
            512,
            0,
            192,
            240,
            460,
            392
        ];

        private static readonly int[] adaptationCoeff2 =
        [
            0,
            -256,
            0,
            64,
            0,
            -208,
            -232
        ];

        private struct MsAdpcmState
        {
            public int delta;
            public int sample1;
            public int sample2;
            public int coeff1;
            public int coeff2;
        }

        private static int AdpcmMsExpandNibble(ref MsAdpcmState channel, int nibble)
        {
            int nibbleSign = nibble - (((nibble & 0x08) != 0) ? 0x10 : 0);
            int predictor = ((channel.sample1 * channel.coeff1) + (channel.sample2 * channel.coeff2)) / 256 + (nibbleSign * channel.delta);

            if (predictor < -32768)
                predictor = -32768;
            else if (predictor > 32767)
                predictor = 32767;

            channel.sample2 = channel.sample1;
            channel.sample1 = predictor;

            channel.delta = (adaptationTable[nibble] * channel.delta) / 256;
            if (channel.delta < 16)
                channel.delta = 16;

            return predictor;
        }

        // I heart MonoGame <3333
        /// <summary>
        /// Converts a buffer of Adpcm data to 16-bit pcm
        /// </summary>
        internal static byte[] ConvertMsAdpcmToPcm(byte[] buffer, int offset, int count, int channels, int blockAlignment)
        {
            MsAdpcmState channel0 = new();
            MsAdpcmState channel1 = new();
            int blockPredictor;

            int sampleCountFullBlock = ((blockAlignment / channels) - 7) * 2 + 2;
            int sampleCountLastBlock = 0;
            if ((count % blockAlignment) > 0)
                sampleCountLastBlock = (((count % blockAlignment) / channels) - 7) * 2 + 2;
            int sampleCount = ((count / blockAlignment) * sampleCountFullBlock) + sampleCountLastBlock;
            var samples = new byte[sampleCount * sizeof(short) * channels];
            int sampleOffset = 0;

            bool stereo = channels == 2;

            while (count > 0)
            {
                int blockSize = blockAlignment;
                if (count < blockSize)
                    blockSize = count;
                count -= blockAlignment;

                int totalSamples = ((blockSize / channels) - 7) * 2 + 2;
                if (totalSamples < 2)
                    break;

                int offsetStart = offset;
                blockPredictor = buffer[offset];
                ++offset;
                if (blockPredictor > 6)
                    blockPredictor = 6;
                channel0.coeff1 = adaptationCoeff1[blockPredictor];
                channel0.coeff2 = adaptationCoeff2[blockPredictor];
                if (stereo)
                {
                    blockPredictor = buffer[offset];
                    ++offset;
                    if (blockPredictor > 6)
                        blockPredictor = 6;
                    channel1.coeff1 = adaptationCoeff1[blockPredictor];
                    channel1.coeff2 = adaptationCoeff2[blockPredictor];
                }

                channel0.delta = buffer[offset];
                channel0.delta |= buffer[offset + 1] << 8;
                if ((channel0.delta & 0x8000) != 0)
                    channel0.delta -= 0x10000;
                offset += 2;
                if (stereo)
                {
                    channel1.delta = buffer[offset];
                    channel1.delta |= buffer[offset + 1] << 8;
                    if ((channel1.delta & 0x8000) != 0)
                        channel1.delta -= 0x10000;
                    offset += 2;
                }

                channel0.sample1 = buffer[offset];
                channel0.sample1 |= buffer[offset + 1] << 8;
                if ((channel0.sample1 & 0x8000) != 0)
                    channel0.sample1 -= 0x10000;
                offset += 2;
                if (stereo)
                {
                    channel1.sample1 = buffer[offset];
                    channel1.sample1 |= buffer[offset + 1] << 8;
                    if ((channel1.sample1 & 0x8000) != 0)
                        channel1.sample1 -= 0x10000;
                    offset += 2;
                }

                channel0.sample2 = buffer[offset];
                channel0.sample2 |= buffer[offset + 1] << 8;
                if ((channel0.sample2 & 0x8000) != 0)
                    channel0.sample2 -= 0x10000;
                offset += 2;
                if (stereo)
                {
                    channel1.sample2 = buffer[offset];
                    channel1.sample2 |= buffer[offset + 1] << 8;
                    if ((channel1.sample2 & 0x8000) != 0)
                        channel1.sample2 -= 0x10000;
                    offset += 2;
                }

                if (stereo)
                {
                    samples[sampleOffset] = (byte)channel0.sample2;
                    samples[sampleOffset + 1] = (byte)(channel0.sample2 >> 8);
                    samples[sampleOffset + 2] = (byte)channel1.sample2;
                    samples[sampleOffset + 3] = (byte)(channel1.sample2 >> 8);
                    samples[sampleOffset + 4] = (byte)channel0.sample1;
                    samples[sampleOffset + 5] = (byte)(channel0.sample1 >> 8);
                    samples[sampleOffset + 6] = (byte)channel1.sample1;
                    samples[sampleOffset + 7] = (byte)(channel1.sample1 >> 8);
                    sampleOffset += 8;
                }
                else
                {
                    samples[sampleOffset] = (byte)channel0.sample2;
                    samples[sampleOffset + 1] = (byte)(channel0.sample2 >> 8);
                    samples[sampleOffset + 2] = (byte)channel0.sample1;
                    samples[sampleOffset + 3] = (byte)(channel0.sample1 >> 8);
                    sampleOffset += 4;
                }

                blockSize -= (offset - offsetStart);
                if (stereo)
                {
                    for (int i = 0; i < blockSize; ++i)
                    {
                        int nibbles = buffer[offset];

                        int sample = AdpcmMsExpandNibble(ref channel0, nibbles >> 4);
                        samples[sampleOffset] = (byte)sample;
                        samples[sampleOffset + 1] = (byte)(sample >> 8);

                        sample = AdpcmMsExpandNibble(ref channel1, nibbles & 0x0f);
                        samples[sampleOffset + 2] = (byte)sample;
                        samples[sampleOffset + 3] = (byte)(sample >> 8);

                        sampleOffset += 4;
                        ++offset;
                    }
                }
                else
                {
                    for (int i = 0; i < blockSize; ++i)
                    {
                        int nibbles = buffer[offset];

                        int sample = AdpcmMsExpandNibble(ref channel0, nibbles >> 4);
                        samples[sampleOffset] = (byte)sample;
                        samples[sampleOffset + 1] = (byte)(sample >> 8);

                        sample = AdpcmMsExpandNibble(ref channel0, nibbles & 0x0f);
                        samples[sampleOffset + 2] = (byte)sample;
                        samples[sampleOffset + 3] = (byte)(sample >> 8);

                        sampleOffset += 4;
                        ++offset;
                    }
                }
            }

            return samples;
        }

        #endregion
    }
}
