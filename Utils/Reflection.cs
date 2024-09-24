using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using System;
using System.Reflection;
using Terraria.ModLoader;

namespace MonoStereoMod.Utils
{
    internal static partial class MonoStereoUtils
    {
        private static readonly MethodInfo getPath = typeof(Terraria.ModLoader.Engine.DistributionPlatform).Assembly
            .GetType("Terraria.ModLoader.Engine.TMLContentManager", true)
            .GetMethod("GetPath", BindingFlags.Instance | BindingFlags.Public, [typeof(string)]);

        public static string GetPath(this ContentManager instance, string path) => (string)getPath.Invoke(instance, [path]);

        private static readonly MethodInfo resizeArrays = typeof(ILoader).GetMethod("ResizeArrays", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void ResizeArrays(this ILoader loader) => resizeArrays.Invoke(loader, null);

        private static readonly FieldInfo internal_looped = typeof(SoundEffectInstance).GetField("INTERNAL_looped", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void set_IsLooped(this SoundEffectInstance sound, bool value) => internal_looped.SetValue(sound, value);

        private static readonly FieldInfo internal_pan = typeof(SoundEffectInstance).GetField("INTERNAL_pan", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void set_Pan(this SoundEffectInstance sound, float value) => internal_pan.SetValue(sound, value);

        private static readonly FieldInfo internal_pitch = typeof(SoundEffectInstance).GetField("INTERNAL_pitch", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void set_Pitch(this SoundEffectInstance sound, float value) => internal_pitch.SetValue(sound, value);

        private static readonly FieldInfo internal_volume = typeof(SoundEffectInstance).GetField("INTERNAL_volume", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void set_Volume(this SoundEffectInstance sound, float value) => internal_volume.SetValue(sound, value);

        private static readonly FieldInfo handle = typeof(SoundEffect).GetField("handle", BindingFlags.Instance | BindingFlags.NonPublic);

        public static FAudio.FAudioBuffer GetHandle(this SoundEffect sound) => (FAudio.FAudioBuffer)handle.GetValue(sound);

        private static readonly FieldInfo formatPtr = typeof(SoundEffect).GetField("formatPtr", BindingFlags.Instance | BindingFlags.NonPublic);

        public static IntPtr GetFormatPtr(this SoundEffect sound) => (IntPtr)formatPtr.GetValue(sound);

        private static readonly FieldInfo loopStart = typeof(SoundEffect).GetField("loopStart", BindingFlags.Instance | BindingFlags.NonPublic);

        public static uint GetLoopStart(this SoundEffect sound) => (uint)loopStart.GetValue(sound);

        private static readonly FieldInfo loopLength = typeof(SoundEffect).GetField("loopLength", BindingFlags.Instance | BindingFlags.NonPublic);

        public static uint GetLoopLength(this SoundEffect sound) => (uint)loopLength.GetValue(sound);
    }
}
