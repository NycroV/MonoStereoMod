using MonoMod.RuntimeDetour;
using MonoStereo.Decoding;
using MonoStereo.Sources;
using MonoStereo.Sources.Songs;
using MonoStereoMod.Audio;
using MonoStereoMod.Systems;
using NAudio.Wave;
using System.Collections.Generic;
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

            path = $"tmod:{path}{extension}";
            Stream contentStream = ModContent.OpenRead(path, true);

            WaveStream waveStream;
            Dictionary<string, string> comments;

            switch (extension)
            {
                case ".ogg":
                    waveStream = new OggReader(contentStream);
                    comments = (waveStream as OggReader).Comments.ComposeComments();
                    break;

                case ".wav":
                    comments = contentStream.ReadComments();
                    waveStream = new WaveFileReader(contentStream);
                    break;

                case ".mp3":
                    comments = contentStream.ReadComments();
                    waveStream = new Mp3Reader(contentStream);
                    break;

                default:
                    throw new FileLoadException($"Unknown music extension {extension}");
            }

            StreamFormattingSongSource source = new(waveStream, comments);
            ISongSource reader = highPerformance ? new HighPerformanceSongSource(source) : BufferedSongReader.Create(source, 2f);

            reader.IsLooped = true;
            return new MonoStereoAudioTrack(reader);
        }
    }
}
