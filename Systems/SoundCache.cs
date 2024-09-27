using MonoStereo;
using MonoStereoMod.Systems.VanillaReaders;
using System.Collections.Generic;
using System.Linq;
using Xna = Microsoft.Xna.Framework.Audio;

namespace MonoStereoMod
{
    internal static class SoundCache
    {
        // Provides a way to access a MonoStereo CachedSoundEffect that contains the same
        // data as an FNA SoundEffect, but is usable by MonoStereo.
        public static readonly Dictionary<Xna.SoundEffect, CachedSoundEffect> Cache = [];

        // Allows us to forward calls for certain SoundEffectInstance properties/methods to
        // a MonoStereoSoundEffect that takes its place.
        public static Dictionary<Xna.SoundEffectInstance, MonoStereoSoundEffect> SoundTracker { get; } = [];

        // This allows for the accessing of the FNA sounds from MonoStereo instances, in addition to the other way around.
        public static Dictionary<MonoStereoSoundEffect, Xna.SoundEffectInstance> SoundLookup => SoundTracker.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

        // Searches for a FNA SoundEffect => MonoStereo CachedSoundEffect mapping,
        // creates one if it does not already exist, and returns that mapping.
        public static CachedSoundEffect GetCachedSound(Xna.SoundEffect sound)
        {
            if (Cache.TryGetValue(sound, out var cachedSound))
                return cachedSound;

            var monoStereoEffect = sound.GetMonoStereoEffect();
            Cache.Add(sound, monoStereoEffect);
            return monoStereoEffect;
        }

        // Attempts to access the MonoStereo sound effect that calls for an FNA
        // sound effect should be forwarded to, if it exists.
        public static bool TryGetMonoStereo(Xna.SoundEffectInstance sound, out MonoStereoSoundEffect instance)
        {
            if (SoundTracker.TryGetValue(sound, out instance))
                return true;

            return false;
        }

        // Attempts to access the FNA sound effect that should forward
        // its calls to the given MonoStereo sound effect, if it exists.
        public static bool TryGetFNA(MonoStereoSoundEffect instance, out Xna.SoundEffectInstance sound)
        {
            if (SoundLookup.TryGetValue(instance, out sound))
                return true;

            return false;
        }

        // Creates a mapping entry for an FNA => MonoStereo sound effect instance, which
        // allows us to forward certain property/method calls to the MonoStereo engine instead of FAudio.
        public static void Map(Xna.SoundEffectInstance sound, MonoStereoSoundEffect instance) => SoundTracker.TryAdd(sound, instance);

        // Collects garbage for sound effects that have finished playing.
        // Uses the vanilla sound garbage collector as a base, which disposes
        // sound effect instances once they stop playing. This disposes the
        // MonoStereo instances for any vanilla sound that has been disposed.
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

        // Clears all mappings, for both cached sound data and active instances.
        public static void Unload()
        {
            foreach (var sound in Cache.Values)
                sound.Dispose();

            foreach (var sound in SoundTracker.Values)
                sound.Dispose();

            Cache.Clear();
            SoundTracker.Clear();
        }
    }
}
