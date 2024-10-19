using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.ModLoader;

namespace MonoStereoMod.Utils
{
    internal static partial class MonoStereoUtils
    {
        // Used to access the full path to "Wave Bank.xwb"
        private static readonly MethodInfo getPath = typeof(Terraria.ModLoader.Engine.DistributionPlatform).Assembly
            .GetType("Terraria.ModLoader.Engine.TMLContentManager", true)
            .GetMethod("GetPath", BindingFlags.Instance | BindingFlags.Public, [typeof(string)]);
        public static string GetPath(this ContentManager instance, string path) => (string)getPath.Invoke(instance, [path]);

        // Used to force a reload of audio tracks, which allows us to inject our own cue readers (which utilizie MonoStereo) in place of the vanilla readers.
        private static readonly MethodInfo resizeArrays = typeof(ILoader).GetMethod("ResizeArrays", BindingFlags.NonPublic | BindingFlags.Instance);
        public static void ResizeArrays(this ILoader loader) => resizeArrays.Invoke(loader, null);

        // All of these "set_XXXXX" methods set the underlying field for the SoundEffectInstance's corresponding property.
        // We don't override the property's getter, so we need to make sure this value is set. This allows us to change the
        // set behavior of the properties, and forward calls to underlying MonoStereo instances (when applicable).

        private static readonly FieldInfo internal_looped = typeof(SoundEffectInstance).GetField("INTERNAL_looped", BindingFlags.Instance | BindingFlags.NonPublic);
        public static void set_IsLooped(this SoundEffectInstance sound, bool value) => internal_looped.SetValue(sound, value);

        private static readonly FieldInfo internal_pan = typeof(SoundEffectInstance).GetField("INTERNAL_pan", BindingFlags.Instance | BindingFlags.NonPublic);
        public static void set_Pan(this SoundEffectInstance sound, float value) => internal_pan.SetValue(sound, value);

        private static readonly FieldInfo internal_pitch = typeof(SoundEffectInstance).GetField("INTERNAL_pitch", BindingFlags.Instance | BindingFlags.NonPublic);
        public static void set_Pitch(this SoundEffectInstance sound, float value) => internal_pitch.SetValue(sound, value);

        private static readonly FieldInfo internal_volume = typeof(SoundEffectInstance).GetField("INTERNAL_volume", BindingFlags.Instance | BindingFlags.NonPublic);
        public static void set_Volume(this SoundEffectInstance sound, float value) => internal_volume.SetValue(sound, value);

        // Gets the audio buffer handle for the FNA sound effect. This is used to copy
        // cached sound data into our own MonoStereo CachedSoundEffects.
        private static readonly FieldInfo handle = typeof(SoundEffect).GetField("handle", BindingFlags.Instance | BindingFlags.NonPublic);
        public static FAudio.FAudioBuffer GetHandle(this SoundEffect sound) => (FAudio.FAudioBuffer)handle.GetValue(sound);

        // This value is a pointer to the FAudio wFormatTag for a given SoundEffect. It contains
        // data like the sound's encoding, sample rate, channels, etc etc. This info is necessary
        // when we conver the data to a MonoStereo CachedSoundEffect, as we need to convert a buffer
        // that could be one of many types into a standardized IEEE float array.
        private static readonly FieldInfo formatPtr = typeof(SoundEffect).GetField("formatPtr", BindingFlags.Instance | BindingFlags.NonPublic);
        public static IntPtr GetFormatPtr(this SoundEffect sound) => (IntPtr)formatPtr.GetValue(sound);

        // These values are not public for some reason.
        // We need to copy them over to our MonoStereo CachedSoundEffectInstances.

        private static readonly FieldInfo loopStart = typeof(SoundEffect).GetField("loopStart", BindingFlags.Instance | BindingFlags.NonPublic);
        public static uint GetLoopStart(this SoundEffect sound) => (uint)loopStart.GetValue(sound);

        private static readonly FieldInfo loopLength = typeof(SoundEffect).GetField("loopLength", BindingFlags.Instance | BindingFlags.NonPublic);
        public static uint GetLoopLength(this SoundEffect sound) => (uint)loopLength.GetValue(sound);

        // Accesses the internal `loading` field of a mod. This should only be true when a mod is loading.
        // We use this for the MonoStereoMod API when adding custom music implementations.
        private static readonly FieldInfo modLoading = typeof(Mod).GetField("loading", BindingFlags.Instance | BindingFlags.NonPublic);
        public static bool IsLoading(this Mod mod) => (bool)modLoading.GetValue(mod);

        // Used to reserve our own music slot ID for the MonoStereoMod API when adding custom music implementations.
        private static readonly MethodInfo reserveMusicId = typeof(MusicLoader).GetMethod("ReserveMusicID", BindingFlags.Static | BindingFlags.NonPublic);
        public static int ReserveMusicLoaderID() => (int)reserveMusicId.Invoke(null, null);

        // Used to retrieve the internal TML dictionary for music paths to slots.
        private static readonly FieldInfo musicByPath = typeof(MusicLoader).GetField("musicByPath", BindingFlags.Static | BindingFlags.NonPublic);
        public static Dictionary<string, int> MusicLoaderMusicByPath() => (Dictionary<string, int>)musicByPath.GetValue(null);

        // Used to retrieve the internal TML dictionary for music paths to extensions.
        private static readonly FieldInfo musicExtensions = typeof(MusicLoader).GetField("musicExtensions", BindingFlags.Static | BindingFlags.NonPublic);
        public static Dictionary<string, string> MusicLoaderMusicExtensions() => (Dictionary<string, string>)musicExtensions.GetValue(null);

        // Contains tML supported music extensions
        private static readonly FieldInfo supportedExtensions = typeof(MusicLoader).GetField("supportedExtensions", BindingFlags.Static | BindingFlags.NonPublic);
        public static string[] MusicLoaderSupportedExtensions() => (string[])supportedExtensions.GetValue(null);
    }
}
