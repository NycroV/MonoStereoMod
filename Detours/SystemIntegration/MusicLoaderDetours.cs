using MonoMod.RuntimeDetour;
using MonoStereo.AudioSources;
using MonoStereo.AudioSources.Songs;
using MonoStereoMod.Audio;
using MonoStereoMod.Systems;
using System.IO;
using System.Reflection;
using Terraria.Audio;
using Terraria.ModLoader;

namespace MonoStereoMod.Detours
{
    internal static partial class Detours
    {
        #region Hooks, Delegates, and Reflection, Oh My!

        public static Hook MusicLoader_LoadMusic_Hook;

        public static MethodInfo MusicLoader_LoadMusic_Method = typeof(MusicLoader).GetMethod("LoadMusic", BindingFlags.NonPublic | BindingFlags.Static);

        public delegate IAudioTrack MusicLoader_LoadMusic_OrigDelegate(string path, string extension);

        #endregion

        // Exactly the same as vanilla, but we replace their
        // IAudioTrack implementations with our own MonoStereo sources.
        public static IAudioTrack On_MusicLoader_LoadMusic(MusicLoader_LoadMusic_OrigDelegate orig, string path, string extension)
        {
            // This means this track is a custom implementation registered by the user.
            if (extension == ".monostereo")
                return SoundCache.GetCustomMusic(path);

            // This means the track has been marked to be "high performance" by the user.
            bool highPerformance = MonoStereoMod.Config.ForceHighPerformance;
            if (extension.StartsWith(MonoStereoMod.HighPerformanceExtensionPrefix))
            {
                highPerformance = true;
                extension = extension.Replace(MonoStereoMod.HighPerformanceExtensionPrefix, ".");
            }

            string fileName = path + extension;
            path = $"tmod:{path}{extension}";

            Stream stream = ModContent.OpenRead(path, true);

            ITerrariaSongSource source = extension switch
            {
                ".wav" => new WavSongSource(stream, fileName),
                ".mp3" => new Mp3SongSource(stream, fileName),
                ".ogg" => new OggSongSource(stream, fileName),
                _ => throw new FileLoadException($"Unknown music extension {extension}"),
            };

            ISongSource reader = highPerformance ? new HighPerformanceSongSource(source) : BufferedSongReader.Create(source, 2f);
            return new MonoStereoAudioTrack(reader);
        }
    }
}
