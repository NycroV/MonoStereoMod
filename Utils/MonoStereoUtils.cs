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
using Terraria.Audio;
using Terraria.Localization;
using Terraria.ModLoader.Exceptions;

namespace MonoStereoMod.Utils
{
    internal static partial class MonoStereoUtils
    {
        #region Replacement Vanilla Utilities

        public static void RunOnMainThreadAndWait(Action action) => Main.RunOnMainThread(action).GetAwaiter().GetResult();

        public static CachedSoundEffect GetRandomSoundEffect(this SoundStyle style)
        {
            var variants = style.Variants();

            if (variants == null || variants.Length == 0)
            {
                return SoundCache.Cache(style.SoundPath);
            }
            else
            {
                int variantIndex = style.GetRandomVariantIndex();
                int variant = variants[variantIndex];

                return SoundCache.Cache(style.SoundPath + variant);
            }
        }

        public static int GetRandomVariantIndex(this SoundStyle style)
        {
            var variants = style.Variants();
            var variantsWeights = style.VariantsWeights();

            if (variantsWeights == null)
            {
                // Simple random.
                return SoundStyleRandom.Next(variants!.Length);
            }

            // Weighted random.
            var totalVariantWeight = style.TotalVariantWeight();
            if (totalVariantWeight is null)
            {
                totalVariantWeight = variantsWeights.Sum();
                style.SetTotalVariantWeight(totalVariantWeight.Value);
            }

            float random = (float)SoundStyleRandom.NextDouble() * totalVariantWeight.Value;
            float accumulatedWeight = 0f;

            for (int i = 0; i < variantsWeights.Length; i++)
            {
                accumulatedWeight += variantsWeights[i];

                if (random < accumulatedWeight)
                {
                    return i;
                }
            }

            return 0; // Unreachable.
        }

        private static readonly char[] nameSplitters = new char[] { '/', ' ', ':' };
        public static void SplitName(string name, out string domain, out string subName)
        {
            int slash = name.IndexOfAny(nameSplitters); // slash is the canonical splitter, but we'll accept space and colon for backwards compatibility, just in case
            if (slash < 0)
                throw new MissingResourceException(Language.GetTextValue("tModLoader.LoadErrorMissingModQualifier", name));

            domain = name.Substring(0, slash);
            subName = name.Substring(slash + 1);
        }

        #endregion

        #region MonoStereo Metadata Utilities

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

        private static readonly string[] supportedExtensions = [".ogg", ".wav", ".mp3"];
        public static bool IsSupported(this string extension) => supportedExtensions.Contains(extension);

        public static ISampleProvider Reformat(this ISampleProvider provider, ref long loopStart, ref long loopEnd)
        {
            if (provider.WaveFormat.SampleRate != AudioStandards.SampleRate)
            {
                provider = new WdlResamplingSampleProvider(provider, AudioStandards.SampleRate);
                float scalar = AudioStandards.SampleRate / (float)provider.WaveFormat.SampleRate;

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

        #region MonoStereo Mod Utilities

        public static List<IContentSource> InsertMonoStereoSource(this List<IContentSource> contentSources)
        {
            var source = //MonoStereoMod.Instance.GetReplacementSource();
                         new CueReadingContentSource();

            var sources = contentSources.ToList();
            int index = sources.FindIndex(0, c => c is XnaDirectContentSource);
            sources.Insert(index + 1, source);

            return sources;
        }

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
                if (samplesToCopy % sampleProvider.WaveFormat.Channels != 0)
                    samplesToCopy--;

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

        #endregion

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
