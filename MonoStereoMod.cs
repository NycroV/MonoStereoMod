global using static MonoStereoMod.Utils.MonoStereoUtils;
using static MonoStereoMod.Detours.Detours;
using Microsoft.Xna.Framework;
using MonoStereo;
using MonoStereo.Outputs;
using ReLogic.Utilities;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace MonoStereoMod
{
    public class MonoStereoMod : Mod
    {
        public static class Config
        {
            // Configs are accessible here to allow for smaller dependencies with ILRepack.
            // ILRepack is used to compile MonoStereoMod.Dependencies.dll, which consolidates all
            // package references neatly into a single project reference.

            // For some reason, including references to most tMod types does not cause problems, but
            // including the config class causes ILRepack to require a reference to tModLoader.dll,
            // when it otherwise wouldn't. Possible because of the property attributes? Not 100% sure.
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

        // Used to determine whether the engine should be active.
        public static bool ModRunning { get; set; } = false;

        // Starts the MonoStereo engine
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

            // Each detour below contains a short comment explaining why we need it/what it does.

            On_ActiveSound.Update += On_ActiveSound_Update; // We use master volume control instead of individual
            On_SoundInstanceGarbageCollector.Update += On_SoundInstanceGarbageCollector_Update; // We need to collect garbage for our FNA => MS mappings

            On_LegacyAudioSystem.Update += On_LegacyAudioSystem_Update; // Updates master volume controls
            On_LegacyAudioSystem.FindReplacementTrack += On_LegacyAudioSystem_FindReplacementTrack; // Use MonoStereo tracks instead of vanilla
            On_LegacyAudioSystem.PauseAll += On_LegacyAudioSystem_PauseAll; // Also call pause on master outputs
            On_LegacyAudioSystem.ResumeAll += On_LegacyAudioSystem_ResumeAll; // Also call resume on master outputs

            MusicLoader_LoadMusic_Hook = new(MusicLoader_LoadMusic_Method, On_MusicLoader_LoadMusic); // Use MonoStereo tracks instead of vanilla

            SoundEffect_CreateInstance_Hook = new(SoundEffect_CreateInstance_Method, On_SoundEffect_CreateInstance); // Maps FNA sounds to an underlying MonoStereo sound
            SoundEffect_Play_Hook = new(SoundEffect_Play_Method, On_SoundEffect_Play); // Maps FNA sounds to an underlying MonoStereo sound

            // SoundEffectInstance property hooks
            // Each of these property hooks does the exact same thing - gets the state of the underlying MonoStereo instance instead
            // of the FNA instance. Since they all do exactly the same thing, there isn't a need to individually comment each line.
            SoundEffectInstance_set_IsLooped_Hook = new(SoundEffectInstance_set_IsLooped_Method, set_SoundEffectInstance_IsLooped);
            SoundEffectInstance_set_Pan_Hook = new(SoundEffectInstance_set_Pan_Method, set_SoundEffectInstance_Pan);
            SoundEffectInstance_set_Pitch_Hook = new(SoundEffectInstance_set_Pitch_Method, set_SoundEffectInstance_Pitch);
            SoundEffectInstance_set_Volume_Hook = new(SoundEffectInstance_set_Volume_Method, set_SoundEffectInstance_Volume);
            SoundEffectInstance_get_State_Hook = new(SoundEffectInstance_get_State_Method, get_SoundEffectInstance_State);

            // SoundEffectInstance method hooks
            // Each of these property hooks does the exact same thing - forwards method calls to the underlying MonoStereo instance instead
            // of the FNA instance. Since they all do exactly the same thing, there isn't a need to individually comment each line.
            SoundEffectInstance_Dispose_Hook = new(SoundEffectInstance_Dispose_Method, On_SoundEffectInstance_Dispose);
            SoundEffectInstance_Apply3D_Hook = new(SoundEffectInstance_Apply3D_Method, On_SoundEffectInstance_Apply3D);
            SoundEffectInstance_Play_Hook = new(SoundEffectInstance_Play_Method, On_SoundEffectInstance_Play);
            SoundEffectInstance_Pause_Hook = new(SoundEffectInstance_Pause_Method, On_SoundEffectInstance_Pause);
            SoundEffectInstance_Resume_Hook = new(SoundEffectInstance_Resume_Method, On_SoundEffectInstance_Resume);
            SoundEffectInstance_Stop_Hook = new(SoundEffectInstance_Stop_Method, On_SoundEffectInstance_Stop);

            // Hook application for custom hooks
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

            // The MonoStereo source acts as a psuedo-texture pack. This allows us to directly
            // override the vanilla tracks, rather than needing custom scenes for every vanilla song.
            // We need this to override the vanilla wavebank reader.
            system.UseSources(system.FileSources.InsertMonoStereoSource());

            // This ensures that all music tracks that have already been
            // loadedare reloaded to use MonoStereo sources.
            LoaderManager.Get<MusicLoader>().ResizeArrays();
        }

        public override void Unload()
        {
            ModRunning = false;

            // This is not a detour, we need to unhook manually
            Main.instance.Exiting -= Instance_Exiting;

            // Clears all sound mappings, both for cached sounds and sound instances.
            SoundCache.Unload();

            if (Main.audioSystem is not LegacyAudioSystem system)
                return;

            // These tracks are not included in the mappings
            foreach (var track in system.AudioTracks.Where(s => s is MonoStereoAudioTrack))
                track.Dispose();

            // Remove the MonoStereo source (which is a psuedo-texture pack) from the available sources
            system.UseSources(system.FileSources.RemoveMonoStereoSource());
        }

        internal static void Instance_Exiting(object sender, System.EventArgs e) => ModRunning = false;

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
