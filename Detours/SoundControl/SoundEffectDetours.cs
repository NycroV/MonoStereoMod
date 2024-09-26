﻿using Microsoft.Xna.Framework.Audio;
using MonoMod.RuntimeDetour;
using MonoStereo.AudioSources.Sounds;
using System.Reflection;

namespace MonoStereoMod.Detours
{
    internal static partial class Detours
    {
        public static Hook SoundEffect_CreateInstance_Hook;

        public static Hook SoundEffect_Play_Hook;

        public static MethodInfo SoundEffect_CreateInstance_Method = typeof(SoundEffect).GetMethod("CreateInstance", BindingFlags.Public | BindingFlags.Instance);

        public static MethodInfo SoundEffect_Play_Method = typeof(SoundEffect).GetMethod("Play", BindingFlags.Instance | BindingFlags.Public, [typeof(float), typeof(float), typeof(float)]);

        public delegate SoundEffectInstance SoundEffect_CreateInstance_OrigDelegate(SoundEffect self);

        public delegate bool SoundEffect_Play_OrigDelegate(SoundEffect self, float volume, float pitch, float pan);

        public static SoundEffectInstance On_SoundEffect_CreateInstance(SoundEffect_CreateInstance_OrigDelegate orig, SoundEffect self)
        {
            var xnaInstance = orig(self);
            var msInstance = new MonoStereoSoundEffect(new CachedSoundEffectReader(SoundCache.GetCachedSound(self)));

            SoundCache.Map(xnaInstance, msInstance);
            return xnaInstance;
        }

        public static bool On_SoundEffect_Play(SoundEffect_Play_OrigDelegate orig, SoundEffect self, float volume, float pitch, float pan)
        {
            var instance = self.CreateInstance();
            instance.Volume = volume;
            instance.Pitch = pitch;
            instance.Pan = pan;
            instance.Play();
            if (instance.State != SoundState.Playing)
            {
                // Ran out of AL sources, probably.
                instance.Dispose();
                return false;
            }
            return true;
        }
    }
}