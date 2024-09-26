﻿using MonoMod.RuntimeDetour;
using MonoStereo.AudioSources;
using MonoStereo.AudioSources.Songs;
using MonoStereoMod.Audio.Reading;
using System.IO;
using System.Reflection;
using Terraria.Audio;
using Terraria.ModLoader;

namespace MonoStereoMod.Detours
{
    internal static partial class Detours
    {
        public static Hook MusicLoader_LoadMusic_Hook;

        public static MethodInfo MusicLoader_LoadMusic_Method = typeof(MusicLoader).GetMethod("LoadMusic", BindingFlags.NonPublic | BindingFlags.Static);

        public delegate IAudioTrack MusicLoader_LoadMusic_OrigDelegate(string path, string extension);

        public static IAudioTrack On_MusicLoader_LoadMusic(MusicLoader_LoadMusic_OrigDelegate orig, string path, string extension)
        {
            string fileName = path + extension;
            path = $"tmod:{path}{extension}";

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
}