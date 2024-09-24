using MonoStereo;
using System.Collections.Generic;
using Xna = Microsoft.Xna.Framework.Audio;
using MonoStereoMod.Systems.VanillaReaders;
using System.Linq;

namespace MonoStereoMod
{
    internal static class SoundCache
    {
        public static readonly Dictionary<Xna.SoundEffect, CachedSoundEffect> Cache = [];

        public static Dictionary<Xna.SoundEffectInstance, MonoStereoSoundEffect> SoundTracker { get; } = [];

        // This allows for the accessing of the FNA sounds from MonoStereo instances, in addition to the other way around.
        public static Dictionary<MonoStereoSoundEffect, Xna.SoundEffectInstance> SoundLookup => SoundTracker.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

        public static CachedSoundEffect GetCachedSound(Xna.SoundEffect sound)
        {
            if (Cache.TryGetValue(sound, out var cachedSound))
                return cachedSound;

            var monoStereoEffect = sound.GetMonoStereoEffect();
            Cache.Add(sound, monoStereoEffect);
            return monoStereoEffect;
        }

        public static bool TryGetMonoStereo(Xna.SoundEffectInstance sound, out MonoStereoSoundEffect instance)
        {
            if (SoundTracker.TryGetValue(sound, out instance))
                return true;

            return false;
        }

        public static bool TryGetFNA(MonoStereoSoundEffect instance, out Xna.SoundEffectInstance sound)
        {
            if (SoundLookup.TryGetValue(instance, out sound))
                return true;

            return false;
        }

        public static void Map(Xna.SoundEffectInstance sound, MonoStereoSoundEffect instance) => SoundTracker.TryAdd(sound, instance);

        public static void Unmap(Xna.SoundEffectInstance sound)
        {
            if (!SoundTracker.TryGetValue(sound, out var instance))
                return;

            instance.Dispose();
        }

        public static void CollectGarbage()
        {
            for (int i = 0; i < SoundTracker.Count; i++)
            {
                var kvp = SoundTracker.ElementAt(i);
                var sound = kvp.Key;

                if (sound is null || sound.IsDisposed)
                {
                    var instance = kvp.Value;
                    instance.Dispose();

                    SoundTracker.Remove(sound);
                    i--;
                }
            }
        }

        public static void Unload()
        {
            foreach (var sound in Cache.Values)
                sound.Dispose();

            Cache.Clear();
            SoundTracker.Clear();
        }
    }
}
