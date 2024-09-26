global using static MonoStereoMod.Utils.MonoStereoUtils;
using Microsoft.Xna.Framework;
using MonoStereo;
using MonoStereo.Outputs;
using ReLogic.Utilities;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using static MonoStereoMod.Detours.Detours;

namespace MonoStereoMod
{
    public class MonoStereoMod : Mod
    {
        public static class Config
        {
            // Configs are accessible here to allow for smaller dependencies with ILRepack
            public static int Latency { get; internal set; } = 150;
            public static int BufferCount { get; internal set; } = 8;
            public static int DeviceNumber { get; internal set; } = -1;
            public static string DeviceDisplayName { get => HighPriorityWaveOutEvent.GetCapabilities(DeviceNumber).ProductName; }

            // Applies config changes to the output
            internal static void ResetOutput(int? latency = null, int? bufferCount = null, int? deviceNumber = null)
            {
                Latency = latency ?? Latency;
                BufferCount = bufferCount ?? BufferCount;
                DeviceNumber = deviceNumber ?? DeviceNumber;

                AudioManager.Output.Dispose();
                AudioManager.Output = GetOutput();
                AudioManager.Output.Init(AudioManager.MasterMixer);
                AudioManager.Output.Play();
            }

            internal static HighPriorityWaveOutEvent GetOutput() => new() { DesiredLatency = Latency, DeviceNumber = DeviceNumber, NumberOfBuffers = BufferCount };
        }

        public static bool ModRunning { get; set; } = false;

        public static MonoStereoMod Instance { get; private set; } = null;

        internal static void Instance_Exiting(object sender, System.EventArgs e) => ModRunning = false;

        internal static void StartEngine() =>
            AudioManager.InitializeCustomOutput(
                Config.GetOutput(),
                () => !ModRunning || Main.instance is null,
                musicVolume: Main.musicVolume,
                soundEffectVolume: Main.soundVolume);

        public override void Load()
        {
            ModRunning = true;
            Main.instance.Exiting += Instance_Exiting;

            if (Main.audioSystem is not LegacyAudioSystem system)
                return;

            StartEngine();

            On_ActiveSound.Update += On_ActiveSound_Update;
            On_SoundInstanceGarbageCollector.Update += On_SoundInstanceGarbageCollector_Update;

            On_LegacyAudioSystem.Update += On_LegacyAudioSystem_Update;
            On_LegacyAudioSystem.FindReplacementTrack += On_LegacyAudioSystem_FindReplacementTrack;
            On_LegacyAudioSystem.PauseAll += On_LegacyAudioSystem_PauseAll;
            On_LegacyAudioSystem.ResumeAll += On_LegacyAudioSystem_ResumeAll;

            MusicLoader_LoadMusic_Hook = new(MusicLoader_LoadMusic_Method, On_MusicLoader_LoadMusic);

            SoundEffect_CreateInstance_Hook = new(SoundEffect_CreateInstance_Method, On_SoundEffect_CreateInstance);
            SoundEffect_Play_Hook = new(SoundEffect_Play_Method, On_SoundEffect_Play);

            // SoundEffectInstance property hooks
            SoundEffectInstance_set_IsLooped_Hook = new(SoundEffectInstance_set_IsLooped_Method, set_SoundEffectInstance_IsLooped);
            SoundEffectInstance_set_Pan_Hook = new(SoundEffectInstance_set_Pan_Method, set_SoundEffectInstance_Pan);
            SoundEffectInstance_set_Pitch_Hook = new(SoundEffectInstance_set_Pitch_Method, set_SoundEffectInstance_Pitch);
            SoundEffectInstance_set_Volume_Hook = new(SoundEffectInstance_set_Volume_Method, set_SoundEffectInstance_Volume);
            SoundEffectInstance_get_State_Hook = new(SoundEffectInstance_get_State_Method, get_SoundEffectInstance_State);

            // SoundEffectInstance method hooks
            SoundEffectInstance_Dispose_Hook = new(SoundEffectInstance_Dispose_Method, On_SoundEffectInstance_Dispose);
            SoundEffectInstance_Apply3D_Hook = new(SoundEffectInstance_Apply3D_Method, On_SoundEffectInstance_Apply3D);
            SoundEffectInstance_Play_Hook = new(SoundEffectInstance_Play_Method, On_SoundEffectInstance_Play);
            SoundEffectInstance_Pause_Hook = new(SoundEffectInstance_Pause_Method, On_SoundEffectInstance_Pause);
            SoundEffectInstance_Resume_Hook = new(SoundEffectInstance_Resume_Method, On_SoundEffectInstance_Resume);
            SoundEffectInstance_Stop_Hook = new(SoundEffectInstance_Stop_Method, On_SoundEffectInstance_Stop);

            // Hook application
            MusicLoader_LoadMusic_Hook.Apply();

            SoundEffect_CreateInstance_Hook.Apply();
            SoundEffect_Play_Hook.Apply();

            SoundEffectInstance_set_IsLooped_Hook.Apply();
            SoundEffectInstance_set_Pan_Hook.Apply();
            SoundEffectInstance_set_Pitch_Hook.Apply();
            SoundEffectInstance_set_Volume_Hook.Apply();
            SoundEffectInstance_get_State_Hook.Apply();

            SoundEffectInstance_Dispose_Hook.Apply();
            SoundEffectInstance_Apply3D_Hook.Apply();
            SoundEffectInstance_Play_Hook.Apply();
            SoundEffectInstance_Pause_Hook.Apply();
            SoundEffectInstance_Resume_Hook.Apply();
            SoundEffectInstance_Stop_Hook.Apply();

            Instance = this;

            system.UseSources(system.FileSources.InsertMonoStereoSource()); // This overrides the vanilla wave bank reader
            LoaderManager.Get<MusicLoader>().ResizeArrays(); // This ensures that all music tracks are reloaded to use MonoStereo sources
        }

        public override void Unload()
        {
            ModRunning = false;

            // This is not a detour, we need to unhook manually
            Main.instance.Exiting -= Instance_Exiting;
            Instance = null;

            SoundCache.Unload();

            if (Main.audioSystem is not LegacyAudioSystem system)
                return;

            foreach (var track in system.AudioTracks.Where(s => s is MonoStereoAudioTrack))
                track.Dispose();

            system.UseSources(system.FileSources.RemoveMonoStereoSource());
        }

        #region API

        /// <summary>
        /// Attempts to retrieve the <see cref="MonoStereoAudioTrack"/> associated with the music track at the specified index.
        /// </summary>
        /// <param name="musicIndex">The index of the music you want to retrieve.</param>
        /// <returns>The <see cref="MonoStereoAudioTrack"/> associated with the music track at the specified index,<br/>
        /// or <see langword="null"/> if the track could not be resolved.</returns>
        public static MonoStereoAudioTrack GetSong(int musicIndex)
            => Main.audioSystem is LegacyAudioSystem system ? system.AudioTracks[musicIndex] is MonoStereoAudioTrack track ? track : null : null;

        /// <summary>
        /// Attempts to get the <see cref="MonoStereoSoundEffect"/> associated with the sound at the specified <paramref name="slotId"/>
        /// </summary>
        /// <param name="slotId">The <see cref="SlotId"/> of the sound you want to retrieve.</param>
        /// <param name="sound">The resulting <see cref="MonoStereoSoundEffect"/>, or <see langword="null"/> if it could not be resolved.</param>
        /// <returns>Whether the retrieval was successful.</returns>
        public static bool TryGetActiveSound(SlotId slotId, out MonoStereoSoundEffect sound)
        {
            sound = null;
            return SoundEngine.TryGetActiveSound(slotId, out var activeSound)
                && activeSound is not null
                && activeSound.Sound is not null
                && SoundCache.TryGetMonoStereo(activeSound.Sound, out sound);
        }

        /// <summary>
        /// Attempts to get the <see cref="MonoStereoSoundEffect"/> associated with the sound at the specified <paramref name="activeSound"/>
        /// </summary>
        /// <param name="activeSound">The <see cref="ActiveSound"/> component of the sound you want to retrieve.</param>
        /// <param name="sound">The resulting <see cref="MonoStereoSoundEffect"/>, or <see langword="null"/> if it could not be resolved.</param>
        /// <returns>Whether the retrieval was successful.</returns>
        public static bool TryGetActiveSound(ActiveSound activeSound, out MonoStereoSoundEffect sound)
        {
            sound = null;
            return activeSound is not null
                && activeSound.Sound is not null
                && SoundCache.TryGetMonoStereo(activeSound.Sound, out sound);
        }

        /// <summary>
        /// Plays a sound and returns the associated <see cref="MonoStereoSoundEffect"/>
        /// </summary>
        /// <returns>The <see cref="MonoStereoSoundEffect"/> that is being played.</returns>
        public static MonoStereoSoundEffect PlaySound(in SoundStyle style, Vector2? position = null, SoundUpdateCallback callback = null)
            => TryGetActiveSound(SoundEngine.PlaySound(style, position, callback), out var sound) ? sound : null;

        #endregion
    }
}
