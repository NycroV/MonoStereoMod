﻿using MonoStereo;
using MonoStereo.Sources;
using MonoStereo.Sources.Songs;
using MonoStereoMod.Audio;
using NAudio.Wave;
using ReLogic.Content.Sources;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;

namespace MonoStereoMod.Detours
{
    internal static partial class Detours
    {
        // Updates MonoStereo mixer volumes whenever vanilla update behavior occurs.
        public static void On_LegacyAudioSystem_Update(On_LegacyAudioSystem.orig_Update orig, LegacyAudioSystem self)
        {
            MonoStereoModAPI.SongMixer.Volume = Main.musicVolume.GetRealVolume();
            MonoStereoModAPI.SoundEffectMixer.Volume = Main.soundVolume;

            orig(self);
        }

        // Exactly the same as vanilla, but we replace their
        // IAudioTrack implementations with our own MonoStereo sources.
        public static IAudioTrack On_LegacyAudioSystem_FindReplacementTrack(On_LegacyAudioSystem.orig_FindReplacementTrack orig, LegacyAudioSystem self, List<IContentSource> sources, string assetPath)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                IContentSource contentSource = sources[i];
                if (!contentSource.HasAsset(assetPath))
                    continue;

#if !SERVER
                string extension = contentSource.GetExtension(assetPath);
                string assetPathWithExtension = assetPath + extension;

                try
                {
                    Stream contentStream = contentSource.OpenStream(assetPathWithExtension);

                    WaveStream waveStream;
                    Dictionary<string, string> comments;

                    if (extension == ".xwb")
                    {
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

                    var source = new UniversalAudioSource(waveStream, assetPathWithExtension, comments)
                    {
                        IsLooped = true
                    };

                    ISongSource reader = MonoStereoModAPI.Config.ForceHighPerformance ? new HighPerformanceSongSource(source) : BufferedSongReader.Create(source);

                    reader.IsLooped = true;
                    return new MonoStereoAudioTrack(reader);
                }
                catch
                {
                    string textToShow = "A resource pack failed to load " + assetPath + "!";
                    Main.IssueReporter.AddReport(textToShow);
                    Main.IssueReporterIndicator.AttemptLettingPlayerKnow();
                }
#endif
            }

            return null;
        }

        // Pause the mixer in addition to each sound (performance benefit).
        public static void On_LegacyAudioSystem_PauseAll(On_LegacyAudioSystem.orig_PauseAll orig, LegacyAudioSystem self)
        {
            MonoStereoEngine.MasterMixer.Pause();
            orig(self);
        }

        // Resume the mixer in addition to each sound.
        public static void On_LegacyAudioSystem_ResumeAll(On_LegacyAudioSystem.orig_ResumeAll orig, LegacyAudioSystem self)
        {
            MonoStereoEngine.MasterMixer.Resume();
            orig(self);
        }
    }
}
