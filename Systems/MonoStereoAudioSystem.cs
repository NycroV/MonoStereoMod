using Microsoft.Xna.Framework.Audio;
using MonoStereo;
using MonoStereo.AudioSources;
using MonoStereo.AudioSources.Songs;
using MonoStereoMod.Audio.Reading;
using ReLogic.Content.Sources;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;

namespace MonoStereoMod
{
    internal class MonoStereoAudioSystem(LegacyAudioSystem oldSystem) : IAudioSystem
    {
        private static IAudioSystem oldSystem = null;

        private static MonoStereoAudioSystem instance = null;

        public static void Initialize()
        {
            if (Main.audioSystem is not LegacyAudioSystem)
                return;

            // Initialize the MonoStereo engine and create a new audio system to wrap it
            AudioManager.Initialize(() => !MonoStereoMod.ModRunning || Main.instance is null, 100);
            var newSystem = new MonoStereoAudioSystem((LegacyAudioSystem)Main.audioSystem);

            // Re-assign the audio system to use the new engine
            oldSystem = Main.audioSystem;
            instance = newSystem;

            Main.audioSystem = newSystem;
            Main.audioSystem.LoadFromSources();
        }

        public static void Shutdown()
        {
            Main.audioSystem = oldSystem;

            foreach (MonoStereoAudioTrack track in instance.AudioTracks.Cast<MonoStereoAudioTrack>())
                track.Dispose();

            oldSystem = null;
            instance = null;
        }

        // Duplicating the reference-type lists means that old track data is not lost on unload
        public IAudioTrack[] AudioTracks = oldSystem.AudioTracks.ToArray();
        public int MusicReplayDelay = oldSystem.MusicReplayDelay;
        public Dictionary<int, string> TrackNamesByIndex = oldSystem.TrackNamesByIndex.ToDictionary();
        public Dictionary<int, IAudioTrack> DefaultTrackByIndex = oldSystem.DefaultTrackByIndex.ToDictionary();

        // Insert the MonoStereo content with a priority that's just higher than the default source.
        // This will allow us to actually read the song files as .ogg's (or other formats) for MonoStereo's engine to use.
        public List<IContentSource> FileSources = oldSystem.FileSources.InsertMonoStereoSource();

        public void LoadFromSources()
        {
            List<IContentSource> fileSources = FileSources;
            for (int i = 0; i < AudioTracks.Length; i++)
            {
                if (TrackNamesByIndex.TryGetValue(i, out var value))
                {
                    string assetPath = "Music" + Path.DirectorySeparatorChar + value;
                    IAudioTrack defaultTrack = DefaultTrackByIndex[i];
                    IAudioTrack trackToUse = defaultTrack;
                    IAudioTrack replacementTrack = FindReplacementTrack(fileSources, assetPath);

                    if (replacementTrack != null)
                        trackToUse = replacementTrack;

                    if (AudioTracks[i] != trackToUse)
                        AudioTracks[i].Stop(AudioStopOptions.Immediate);

                    if (AudioTracks[i] != defaultTrack)
                        AudioTracks[i].Dispose();

                    AudioTracks[i] = trackToUse;
                }
            }
        }

        public void UseSources(List<IContentSource> sourcesFromLowestToHighest)
        {
            FileSources = sourcesFromLowestToHighest;
            LoadFromSources();
        }

        public void Update()
        {
            for (int i = 0; i < AudioTracks.Length; i++)
            {
                if (AudioTracks[i] != null)
                    AudioTracks[i].Update();
            }
        }

        private static MonoStereoAudioTrack FindReplacementTrack(List<IContentSource> sources, string assetPath)
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
                    ISongSource source = extension switch
                    {
                        ".ogg" => new OggSongSource(contentSource.OpenStream(assetPathWithExtension), assetPathWithExtension),
                        ".wav" => new WavSongSource(contentSource.OpenStream(assetPathWithExtension), assetPathWithExtension),
                        ".mp3" => new Mp3SongSource(contentSource.OpenStream(assetPathWithExtension), assetPathWithExtension),
                        _ => null
                    };

                    if (source != null)
                        return new MonoStereoAudioTrack(new BufferedSongReader(source, 2f));
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

        public IEnumerator PrepareWaveBank()
        {
            yield break;
        }

        public void LoadCue(int cueIndex, string cueName) { }

        public void UpdateMisc()
        {
            if (Main.curMusic != Main.newMusic)
                MusicReplayDelay = 0;

            if (MusicReplayDelay > 0)
                MusicReplayDelay--;
        }

        public void PauseAll()
        {
            float[] musicFade = Main.musicFade;
            for (int i = 0; i < AudioTracks.Length; i++)
            {
                if (AudioTracks[i] != null && !AudioTracks[i].IsPaused && AudioTracks[i].IsPlaying && musicFade[i] > 0f)
                {
                    try
                    {
                        AudioTracks[i].Pause();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        public void ResumeAll()
        {
            float[] musicFade = Main.musicFade;
            for (int i = 0; i < AudioTracks.Length; i++)
            {
                if (AudioTracks[i] != null && AudioTracks[i].IsPaused && musicFade[i] > 0f)
                {
                    try
                    {
                        AudioTracks[i].Resume();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        public void UpdateAmbientCueState(int i, bool gameIsActive, ref float trackVolume, float systemVolume)
        {
            if (systemVolume == 0f)
            {
                if (AudioTracks[i].IsPlaying)
                    AudioTracks[i].Stop(AudioStopOptions.Immediate);

                return;
            }

            if (!AudioTracks[i].IsPlaying)
            {
                AudioTracks[i].Reuse();
                AudioTracks[i].Play();
                AudioTracks[i].SetVariable("Volume", trackVolume * systemVolume);
                return;
            }

            if (AudioTracks[i].IsPaused && gameIsActive)
            {
                AudioTracks[i].Resume();
                return;
            }

            trackVolume += 0.005f;
            if (trackVolume > 1f)
                trackVolume = 1f;

            AudioTracks[i].SetVariable("Volume", trackVolume * systemVolume);
        }

        public void UpdateAmbientCueTowardStopping(int i, float stoppingSpeed, ref float trackVolume, float systemVolume)
        {
            if (!AudioTracks[i].IsPlaying)
            {
                trackVolume = 0f;
                return;
            }

            if (trackVolume > 0f)
            {
                trackVolume -= stoppingSpeed;
                if (trackVolume < 0f)
                    trackVolume = 0f;
            }

            if (trackVolume <= 0f)
                AudioTracks[i].Stop(AudioStopOptions.Immediate);
            else
                AudioTracks[i].SetVariable("Volume", trackVolume * systemVolume);
        }

        public bool IsTrackPlaying(int trackIndex) => AudioTracks[trackIndex].IsPlaying;

        public void UpdateCommonTrack(bool active, int i, float totalVolume, ref float tempFade)
        {
            tempFade += 0.005f;
            if (tempFade > 1f)
                tempFade = 1f;

            if (!AudioTracks[i].IsPlaying && active)
            {
                if (MusicReplayDelay == 0)
                {
                    if (Main.SettingMusicReplayDelayEnabled)
                        MusicReplayDelay = Main.rand.Next(14400, 21601);

                    AudioTracks[i].Reuse();
                    AudioTracks[i].SetVariable("Volume", totalVolume);
                    AudioTracks[i].Play();
                }
            }
            else
            {
                AudioTracks[i].SetVariable("Volume", totalVolume);
            }
        }

        public void UpdateCommonTrackTowardStopping(int i, float totalVolume, ref float tempFade, bool isMainTrackAudible)
        {
            // Track should never really be null, save for some in-between loading if music is forced to play before mod loading is complete
            if (AudioTracks[i] is null)
                return;

            if (AudioTracks[i].IsPlaying || !AudioTracks[i].IsStopped)
            {
                if (isMainTrackAudible)
                    tempFade -= 0.005f;
                else if (Main.curMusic == 0)
                    tempFade = 0f;

                if (tempFade <= 0f)
                {
                    tempFade = 0f;
                    AudioTracks[i].SetVariable("Volume", 0f);
                    AudioTracks[i].Stop(AudioStopOptions.Immediate);
                }
                else
                {
                    AudioTracks[i].SetVariable("Volume", totalVolume);
                }
            }
            else
            {
                tempFade = 0f;
            }
        }

        public void UpdateAudioEngine()
        {
        }

        public void Dispose()
        {
        }
    }
}
