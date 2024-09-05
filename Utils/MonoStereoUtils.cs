using MonoStereo;
using System;
using Terraria;
using Terraria.Audio;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using ATL;
using Terraria.Localization;
using Terraria.ModLoader.Exceptions;
using ReLogic.Content.Sources;
using MonoStereoMod.Systems;
using NAudio.Wave;
using Microsoft.CodeAnalysis.Operations;
using Mono.Cecil;
using NAudio.Wave.SampleProviders;
using MonoStereo.SampleProviders;

namespace MonoStereoMod.Utils
{
    internal static partial class MonoStereoUtils
    {
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

        public static List<IContentSource> InsertMonoStereoSource(this List<IContentSource> contentSources)
        {
            var source = MonoStereoMod.Instance.ReplacementSource(); //CueReadingContentSource source = new();
            var sources = contentSources.ToList();

            int index = sources.FindIndex(0, c => c is XnaDirectContentSource);
            sources.Insert(index + 1, source);

            return sources;
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
    }
}
