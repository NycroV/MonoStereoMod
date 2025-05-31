using MonoStereo;
using MonoStereoMod.Audio;
using System.Collections.Generic;
using System.Linq;
using Xna = Microsoft.Xna.Framework.Audio;

namespace MonoStereoMod.Systems
{
    internal static class SoundCache
    {
        // Provides a way to access a MonoStereo CachedSoundEffect that contains the same
        // data as an FNA SoundEffect, but is usable by MonoStereo.
        public static readonly Dictionary<Xna.SoundEffect, CachedSoundEffect> Cache = [];

        // This allows users to register custom music implementations for MonoStereo use
        public static readonly Dictionary<string, MonoStereoAudioTrack> CustomMusicSlots = [];

        // Allows us to forward calls for certain SoundEffectInstance properties/methods to
        // a MonoStereoSoundEffect that takes its place.
        public static Dictionary<Xna.SoundEffectInstance, MonoStereoSoundEffect> SoundTracker { get; } = [];

        // This allows for the accessing of the FNA sounds from MonoStereo instances, in addition to the other way around.
        public static Dictionary<MonoStereoSoundEffect, Xna.SoundEffectInstance> SoundLookup => SoundTracker.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

        // Gets the MonoStereo version of an FNA sound effect.
        //
        // This allows the offloading of data copying from FNA => MS to a separate thread to improve performance,
        // if the desired sound effect has not been copied over yet.
        //
        // Attempts to access data before the load is complete will wait for the load to complete before executing.
        public static TerrariaCachedSoundEffectReader GetCachedSoundReader(Xna.SoundEffect sound)
        {
            if (Cache.TryGetValue(sound, out var cachedSound))
                return new(cachedSound);

            return new(() => LoadCachedSound(sound));
        }

        // Copies the data from an FNA sound into a MS sound, and caches
        // that MS sound for later re-use.
        public static CachedSoundEffect LoadCachedSound(Xna.SoundEffect sound)
        {
            var monoStereoEffect = sound.GetMonoStereoEffect();

            lock (Cache)
            {
                Cache[sound] = monoStereoEffect;
            }

            return monoStereoEffect;
        }

        // Retrieves a custom music implementation, and marks it as loaded by TML
        // so that we don't accidentally call dispose twice.
        public static MonoStereoAudioTrack GetCustomMusic(string musicName)
        {
            var music = CustomMusicSlots[musicName];
            CustomMusicSlots.Remove(musicName);

            return music;
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

        // Clears all mappings, for cached sound data, active instances, and custom music
        public static void Unload()
        {
            foreach (var sound in Cache.Values)
                sound.Dispose();

            foreach (var sound in SoundTracker.Values)
                sound.Dispose();

            foreach (var song in CustomMusicSlots.Values)
                song.Dispose();

            Cache.Clear();
            SoundTracker.Clear();
            CustomMusicSlots.Clear();
        }
    }
}
