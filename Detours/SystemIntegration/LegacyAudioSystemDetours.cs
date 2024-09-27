using MonoStereo;
using MonoStereo.AudioSources;
using MonoStereo.AudioSources.Songs;
using MonoStereoMod.Audio;
using MonoStereoMod.Audio.Reading;
using ReLogic.Content.Sources;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;

namespace MonoStereoMod.Detours
{
    internal static partial class Detours
    {
        // Updates MonoStereo mixer volumes whenever vanilla update behavior occurs.
        public static void On_LegacyAudioSystem_Update(On_LegacyAudioSystem.orig_Update orig, LegacyAudioSystem self)
        {
            AudioManager.MusicVolume = Main.musicVolume.GetRealVolume();
            AudioManager.SoundEffectVolume = Main.soundVolume;

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
                    ISongSource source = extension switch
                    {
                        ".ogg" => new OggSongSource(contentSource.OpenStream(assetPathWithExtension), assetPathWithExtension),
                        ".wav" => new WavSongSource(contentSource.OpenStream(assetPathWithExtension), assetPathWithExtension),
                        ".mp3" => new Mp3SongSource(contentSource.OpenStream(assetPathWithExtension), assetPathWithExtension),
                        ".xwb" => new CueSongSource(contentSource.OpenStream(assetPath) as CueReader),
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

        // Pause the mixer in addition to each sound (performance benefit).
        public static void On_LegacyAudioSystem_PauseAll(On_LegacyAudioSystem.orig_PauseAll orig, LegacyAudioSystem self)
        {
            AudioManager.MasterMixer.Pause();
            orig(self);
        }

        // Resume the mixer in addition to each sound.
        public static void On_LegacyAudioSystem_ResumeAll(On_LegacyAudioSystem.orig_ResumeAll orig, LegacyAudioSystem self)
        {
            AudioManager.MasterMixer.Resume();
            orig(self);
        }
    }
}
