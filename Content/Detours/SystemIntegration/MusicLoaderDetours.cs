using MonoMod.RuntimeDetour;
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
            bool highPerformance = MonoStereoModAPI.Config.ForceHighPerformance;
            if (extension.StartsWith(MonoStereoModAPI.HighPerformanceExtensionPrefix))
            {
                highPerformance = true;
                extension = extension.Replace(MonoStereoModAPI.HighPerformanceExtensionPrefix, ".");
            }

            path = $"{path}{extension}";
            Stream contentStream = ModContent.OpenRead($"tmod:{path}", true);

            WaveStream waveStream;
            Dictionary<string, string> comments;

            if (extension == ".xwb")
            {
                // We use a custom content source that will return CueReader's as the content stream.
                CueReader cueStream = contentStream as CueReader;
                comments = [];

                if (cueStream.Cue.LoopStart > 0)
                    comments.Add("LOOPSTART", cueStream.Cue.LoopStart.ToString());

                if (cueStream.Cue.LoopEnd > 0)
                    comments.Add("LOOPEND", cueStream.Cue.LoopEnd.ToString());

                waveStream = cueStream;
            }

            else
            {
                waveStream = UniversalAudioSource.GetWaveStream(contentStream, extension, false, out comments);
            }

            var source = new UniversalAudioSource(waveStream, path, comments)
            {
                IsLooped = true
            };

            ISongSource reader = MonoStereoModAPI.Config.ForceHighPerformance ? new HighPerformanceSongSource(source) : BufferedSongReader.Create(source);

            reader.IsLooped = true;
            return new MonoStereoAudioTrack(reader);
        }
    }
}
