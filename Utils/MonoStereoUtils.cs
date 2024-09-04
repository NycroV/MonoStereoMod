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
using NAudio.Wave.SampleProviders;
using NAudio.Wave;

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

        public static ISampleProvider ConvertWaveProviderIntoSampleProvider(this IWaveProvider waveProvider)
        {
            if (waveProvider.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
            {
                if (waveProvider.WaveFormat.BitsPerSample == 8)
                {
                    return new Pcm8BitToSampleProvider(waveProvider);
                }

                if (waveProvider.WaveFormat.BitsPerSample == 16)
                {
                    return new Pcm16BitToSampleProvider(waveProvider);
                }

                if (waveProvider.WaveFormat.BitsPerSample == 24)
                {
                    return new Pcm24BitToSampleProvider(waveProvider);
                }

                if (waveProvider.WaveFormat.BitsPerSample == 32)
                {
                    return new Pcm32BitToSampleProvider(waveProvider);
                }

                throw new InvalidOperationException("Unsupported bit depth");
            }

            if (waveProvider.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                if (waveProvider.WaveFormat.BitsPerSample == 64)
                {
                    return new WaveToSampleProvider64(waveProvider);
                }

                return new WaveToSampleProvider(waveProvider);
            }

            throw new ArgumentException("Unsupported source encoding");
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
            var sources = contentSources.ToList();
            int index = sources.FindIndex(0, c => c is XnaDirectContentSource);
            sources.Insert(index + 1, MonoStereoMod.Instance.RootlessSource);
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
    }
}
