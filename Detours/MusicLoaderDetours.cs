using MonoMod.RuntimeDetour;
using MonoStereo.AudioSources;
using MonoStereo.AudioSources.Songs;
using MonoStereoMod.Audio.Reading;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria.Audio;
using Terraria.ModLoader;

namespace MonoStereoMod.Detours
{
    internal static partial class Detours
    {
        public static IAudioTrack On_MusicLoader_LoadMusic(On_MusicLoader.orig_LoadMusic orig, string path, string extension)
        {
            path = $"tmod:{path}{extension}";
            string fileName = path + extension;

            Stream stream = ModContent.OpenRead(path, true);

            ISongSource source = extension switch
            {
                ".wav" => new WavSongSource(stream, fileName),
                ".mp3" => new Mp3SongSource(stream, fileName),
                ".ogg" => new OggSongSource(stream, fileName),
                _ => throw new FileLoadException($"Unknown music extension {extension}"),
            };

            return new MonoStereoAudioTrack(new BufferedSongReader(source, 2f));
        }
    }

    public static class On_MusicLoader
    {
        private static readonly MethodInfo loadMusic = typeof(MusicLoader).GetMethod("LoadMusic", BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly List<KeyValuePair<hook_LoadMusic, Hook>> hooks = [];

        public delegate IAudioTrack orig_LoadMusic(string path, string extension);

        public delegate IAudioTrack hook_LoadMusic(orig_LoadMusic orig, string path, string extension);

        public static event hook_LoadMusic LoadMusic
        {
            add
            {
                Hook hook = new(loadMusic, value);
                hooks.Add(new(value, hook));
                hook.Apply();
            }

            remove
            {
                var hookInfo = hooks.FirstOrDefault(h => h.Key == value, new(null, null));
                if (hookInfo.Value != null)
                {
                    hookInfo.Value.Undo();
                    hookInfo.Value.Dispose();
                    hooks.Remove(hookInfo);
                }
            }
        }
    }
}
