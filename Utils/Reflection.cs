﻿using Microsoft.Xna.Framework.Content;
using ReLogic.Content;
using ReLogic.Content.Sources;
using ReLogic.Utilities;
using System;
using System.IO;
using System.Reflection;
using Terraria.Audio;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Terraria.Utilities;

namespace MonoStereoMod.Utils
{
    internal static partial class MonoStereoUtils
    {
        private static readonly FieldInfo soundStyleRandom = typeof(SoundStyle).GetField("Random", BindingFlags.Static | BindingFlags.NonPublic);

        public static readonly UnifiedRandom SoundStyleRandom = (UnifiedRandom)soundStyleRandom.GetValue(null);

        private static readonly MethodInfo getPath = typeof(Terraria.ModLoader.Engine.DistributionPlatform).Assembly
            .GetType("TMLContentManager")
            .GetMethod("GetPath", BindingFlags.Instance | BindingFlags.NonPublic, [typeof(string)]);

        public static string GetPath(this ContentManager instance, string path) => (string)getPath.Invoke(instance, [path]);

        private static readonly FieldInfo variants = typeof(SoundStyle).GetField("variants", BindingFlags.Instance | BindingFlags.NonPublic);

        public static int[] Variants(this SoundStyle style) => (int[])variants.GetValue(style);

        private static readonly FieldInfo variantsWeights = typeof(SoundStyle).GetField("variantsWeights", BindingFlags.Instance | BindingFlags.NonPublic);

        public static float[] VariantsWeights(this SoundStyle style) => (float[])variantsWeights.GetValue(style);

        private static readonly FieldInfo totalVariantWeight = typeof(SoundStyle).GetField("totalVariantWeight", BindingFlags.Instance | BindingFlags.NonPublic);

        public static float? TotalVariantWeight(this SoundStyle style) => (float?)totalVariantWeight.GetValue(style);

        public static void SetTotalVariantWeight(this SoundStyle style, float weight) => totalVariantWeight.SetValue(style, weight);

        private static readonly PropertyInfo usesMusicPitch = typeof(SoundStyle).GetProperty("UsesMusicPitch", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool UsesMusicPitch(this SoundStyle style) => (bool)usesMusicPitch.GetValue(style);

        private static readonly FieldInfo trackedSounds = typeof(SoundPlayer).GetField("_trackedSounds", BindingFlags.Instance | BindingFlags.NonPublic);

        public static SlotVector<ActiveSound> TrackedSounds(this SoundPlayer soundPlayer) => (SlotVector<ActiveSound>)trackedSounds.GetValue(soundPlayer);

        private static readonly PropertyInfo tmodFile = typeof(Mod).GetProperty("File", BindingFlags.Instance | BindingFlags.NonPublic);

        public static TmodFile File(this Mod mod) => (TmodFile)tmodFile.GetValue(mod);

        private static readonly PropertyInfo sources = typeof(AssetRepository).GetProperty("_sources", BindingFlags.Instance | BindingFlags.NonPublic);

        public static IContentSource[] Sources(this AssetRepository repository) => (IContentSource[])sources.GetValue(repository);

        private static readonly Type lzxDecoderType = typeof(ContentReader).Assembly.GetType("LzxDecoder");

        public static object LzxDecoder(int window) => Activator.CreateInstance(lzxDecoderType, [window]);

        private static readonly MethodInfo decompress = lzxDecoderType.GetMethod("Decompress", BindingFlags.Instance, [typeof(Stream), typeof(int), typeof(Stream), typeof(int)]);

        public static int LzxDecoderDecompress(this object lzxDecoder, Stream inData, int inLen, Stream outData, int outLen) => (int)decompress.Invoke(lzxDecoder, [inData, inLen, outData, outLen]);

        private static readonly ConstructorInfo contentReader = typeof(ContentReader).GetConstructor([typeof(ContentManager), typeof(Stream), typeof(string), typeof(int), typeof(char), typeof(Action<IDisposable>)]);

        public static ContentReader ContentReaderCtor(ContentManager manager, Stream stream, string assetName, int version, char platform, Action<IDisposable> recordDisposableObject) => (ContentReader)contentReader.Invoke([manager, stream, assetName, version, platform, recordDisposableObject]);
    }
}