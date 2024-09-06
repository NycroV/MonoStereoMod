using MonoMod.RuntimeDetour;
using System.IO;
using System.Reflection;
using Terraria.Audio;
using Terraria.ModLoader;

namespace MonoStereoMod.Detours
{
    internal class On_MusicLoader
    {
        private static readonly MethodInfo loadMusic = typeof(MusicLoader).GetMethod("LoadMusic", BindingFlags.NonPublic | BindingFlags.Static);

        internal delegate IAudioTrack orig_LoadMusic(string path, string extension);

        public delegate IAudioTrack hook_LoadMusic(orig_LoadMusic orig, string path, string extension);

        internal static event hook_LoadMusic LoadMusic
        {
            add
            {
                Hook hook = new(loadMusic, value);
                hook.Apply();
            }

            remove { }
        }
    }

    internal class MusicLoaderDetours
    {
        public static IAudioTrack On_MusicLoader_LoadMusic(On_MusicLoader.orig_LoadMusic orig, string path, string extension)
        {
            path = $"tmod:{path}{extension}";

            Stream stream = ModContent.OpenRead(path, true);

            return extension switch
            {
                ".wav" => new WAVAudioTrack(stream),
                ".mp3" => new MP3AudioTrack(stream),
                ".ogg" => new OGGAudioTrack(stream),
                _ => throw new FileLoadException($"Unknown music extension {extension}"),
            };
        }
    }
}
