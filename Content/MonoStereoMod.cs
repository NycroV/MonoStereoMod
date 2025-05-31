global using static MonoStereoMod.Utils.MonoStereoUtils;
using static MonoStereoMod.Detours.Detours;
using Microsoft.Xna.Framework;
using MonoStereo;
using MonoStereo.Sources;
using MonoStereo.Structures;
using MonoStereo.Structures.SampleProviders;
using MonoStereoMod.Systems;
using MonoStereoMod.Utils;
using PortAudioSharp;
using ReLogic.Utilities;
using System;
using System.Linq;
using System.Threading;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace MonoStereoMod
{
    public class MonoStereoMod : Mod
    {
        /// <summary>
        /// Allows other mods to force "high performance mode" to be active.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class)]
        public class ForceHighPerformanceAttribute : Attribute
        { }

        // Used to force only specific tracks to be high performance.
        // Stands for:
        //
        // M(ono)
        // S(tereo)
        // H(igh)
        // P(erformance)
        internal const string HighPerformanceExtensionPrefix = ".mshp-";

        public static class Config
        {
            // Configs are accessible here to allow for smaller dependencies with ILRepack.
            // ILRepack is used to compile MonoStereoMod.Dependencies.dll, which consolidates all
            // package references neatly into a single project reference.
            //
            // For some reason, including references to most tMod types does not cause problems, but
            // including the config class causes ILRepack to require a reference to tModLoader.dll,
            // when it otherwise wouldn't. Possibly because of the property attributes? Not 100% sure.

            public static bool ForceHighPerformance { get; internal set; } = false;
            public static int DeviceNumber { get; internal set; } = -1;
            public static float Latency { get; internal set; } = -1;

            public static string DeviceDisplayName { get; internal set; }
            public static bool DeviceAvailable { get; internal set; }
            internal static readonly QueuedLock OutputLock = new();

            // Applies config changes to the output.
            // We do this on a separate thread so the game doesn't lag whenever switching outputs.
            internal static void ResetOutput(int? deviceNumber = null, float? latency = null)
            {
                ThreadPool.QueueUserWorkItem((streamParams) =>
                {
                    OutputLock.Execute(() =>
                    {
                        var stream = ((int? deviceNumber, float? latency))streamParams;

                        int device = stream.deviceNumber ?? DeviceNumber;
                        float latency = stream.latency ?? Latency;

                        if (device == DeviceNumber && latency == Latency)
                            return;

                        DeviceNumber = device;
                        Latency = latency;
                        DeviceInfo deviceInfo = device >= 0 ? PortAudio.GetDeviceInfo(device) : PortAudio.GetDeviceInfo(PortAudio.DefaultOutputDevice);

                        string displayName = ModRunning && device >= 0 ? (deviceInfo.name ?? "???") : "Default";
                        DeviceDisplayName = displayName[..int.Min(displayName.Length, 31)];

                        if (!ModRunning)
                            return;

                        MonoStereoEngine.Output.Dispose();
                        MonoStereoEngine.Output = GenerateOutput();

                        try
                        {
                            MonoStereoEngine.Output.Init(MonoStereoEngine.MasterMixer);
                            MonoStereoEngine.Output.Play();
                            DeviceAvailable = true;
                        }
                        catch
                        {
                            DeviceAvailable = false;
                        }
                    });
                }, (deviceNumber, latency));
            }

            // Generates the output based on the user's config.
            internal static IMonoStereoOutput GenerateOutput()
            {
                int? device = DeviceNumber == -1 ? null : DeviceNumber;
                double? latency = Latency == -1 ? null : Latency;
                return MonoStereoEngine.DefaultOutput(device, latency);
            }
        }

        // Used to determine whether the engine should be active.
        public static bool ModRunning { get; internal set; } = false;

        /// <summary>
        /// Reference to <see cref="MonoStereoEngine.MasterMixer"/>. Only here for consistency’s sake with <see cref="MusicMixer"/> and <see cref="SoundEffectMixer"/>.
        /// </summary>
        public static AudioMixer<AudioMixer> MasterMixer { get; private set; }

        /// <summary>
        /// The static accessor for <see cref="MonoStereoEngine.AudioMixers{Song}"/>.<br/>
        /// Accessing this can slightly improve performance over indexing the active mixers multiple times.
        /// </summary>
        public static AudioMixer<Song> MusicMixer { get; private set; }

        /// <summary>
        /// The static accessor for <see cref="MonoStereoEngine.AudioMixers{SoundEffect}"/>.<br/>
        /// Accessing this can slightly improve performance over indexing the active mixers multiple times.
        /// </summary>
        public static AudioMixer<SoundEffect> SoundEffectMixer { get; private set; }

        // Starts the MonoStereo engine
        internal static void StartEngine()
        {
            try
            {
                MonoStereoEngine.InitializeCustomOutput(
                    Config.GenerateOutput(),
                    CheckEngine,
                    masterVolume: 1f,
                    audioMixerTypesAndVolumes: new()
                    {
                        [typeof(Song)] = Main.musicVolume,
                        [typeof(SoundEffect)] = Main.soundVolume
                    });

                DeviceInfo deviceInfo = Config.DeviceNumber >= 0 ? PortAudio.GetDeviceInfo(Config.DeviceNumber) : PortAudio.GetDeviceInfo(PortAudio.DefaultOutputDevice);
                string displayName = Config.DeviceNumber >= 0 ? (deviceInfo.name ?? "???") : "Default";
                Config.DeviceDisplayName = displayName[..int.Min(displayName.Length, 31)];

                Thread.Sleep(100);
                MonoStereoEngine.ThrowIfErrored();

                Config.DeviceAvailable = true;
            }
            catch
            {
                Config.DeviceAvailable = false;
            }
        }

        // Checks to see if the MonoStereo engine should shut down.
        internal static bool CheckEngine() => !ModRunning || Main.instance is null;

        // Start the engine and detour TML/FNA to use our engine instead
        public override void Load()
        {
            // Do not implement audio engine detours on the server.
            if (Main.dedServ)
                return;

            // First, we need to make sure that PortAudio is loadable.
            // We do this by copying an embedded library file depending on the current OS to an external directory,
            // and then loading that copied file directly.
            LoadPortAudio();

            ModRunning = true;
            Main.instance.Exiting += Instance_Exiting; // Sets ModRunning to false whenever the game exits

            if (Main.audioSystem is not LegacyAudioSystem system)
                return;

            StartEngine();

            MasterMixer = MonoStereoEngine.MasterMixer;
            MusicMixer = MonoStereoEngine.AudioMixers<Song>();
            SoundEffectMixer = MonoStereoEngine.AudioMixers<SoundEffect>();

            // Each detour below contains a short comment explaining why we need it/what it does.

            On_ActiveSound.Update += On_ActiveSound_Update; // We use master volume control instead of individual
            On_SoundInstanceGarbageCollector.Update += On_SoundInstanceGarbageCollector_Update; // We need to collect garbage for our FNA => MS mappings

            On_LegacyAudioSystem.Update += On_LegacyAudioSystem_Update; // Updates master volume controls
            On_LegacyAudioSystem.FindReplacementTrack += On_LegacyAudioSystem_FindReplacementTrack; // Use MonoStereo tracks instead of vanilla
            On_LegacyAudioSystem.PauseAll += On_LegacyAudioSystem_PauseAll; // Also call pause on master outputs (performance)
            On_LegacyAudioSystem.ResumeAll += On_LegacyAudioSystem_ResumeAll; // Also call resume on master outputs (performance)

            ModContent_UnloadModContent_Hook = new(ModContent_UnloadModContent_Method, On_ModContent_UnloadModContent); // Stop the audio engine before unloading content

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

            // Application for custom hooks
            ModContent_UnloadModContent_Hook.Apply();

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

            // Other mods that implement certain custom audio filters may choose to force "high performance mode" to be active.
            // High performance mode sacrifices a certain amount of memory overhead to cache all data for any songs that are currently playing,
            // rather than utilizing buffered IO reading. This GREATLY improves performance for playback, and is all but necessary
            // if any custom filters want to seek an underlying stream multiple times in rapid succession. Typically, this memory overhead is
            // not that large - usually ~50mb. However, there is no limit or accurate estimate for this number in every case, as
            // tracks can be VERY long, and any number of tracks can be playing at a time. Thus, it is off by default. Although I would
            // be very surprised if it ever even came remotely close to 1gb.
            //
            // As an alternative to forcing ALL tracks to be high performance, developers can register
            // specific high performance tracks that they might expect certain filters to be added to. This is a good middle ground
            // that ensures that filters which need high performance, but are only used in certain scenarios (like a boss fight),
            // get that high performance, while all other tracks remain unaffected.
            //
            // Players can also opt to force high performance mode in the mod's configs.
            //
            // There is no method that is called before all mods are loaded, so we get around this by
            // having developers mark their mods with the ForceHighPerformanceAttribute. These attributes
            // are embedded into the mod class, so they will always be readable.
            //
            // This implementation *should* be suitable for most use cases, but if any arise
            // where this is not (ex. dynamically determining whether high performance should be enabled),
            // I will come back and implement a better solution.
            bool anyModsWantHighPerformance = ModLoader.Mods.Any(mod => Attribute.GetCustomAttribute(mod.GetType(), typeof(ForceHighPerformanceAttribute)) is not null);
            Config.ForceHighPerformance |= anyModsWantHighPerformance;

            // The MonoStereo source acts as a psuedo-texture pack. This allows us to directly
            // override the vanilla tracks, rather than needing custom scenes for every vanilla song.
            // We need this to override the vanilla wavebank reader.
            system.UseSources(system.FileSources.InsertMonoStereoSource());

            // This ensures that all music tracks that have already been
            // loaded are reloaded to use MonoStereo sources.

            var setFactories = SetFactories();
            var factoryToRemove = setFactories.FirstOrDefault(s => s.ContainingClassName() == nameof(MusicID), null);

            if (factoryToRemove is not null)
                setFactories.Remove(factoryToRemove);

            LoaderManager.Get<MusicLoader>().ResizeArrays();
        }

        public override void Unload()
        {
            // OF NOTE:
            // ModRunning is not set to false in this method in the event that other mods attempt to unload
            // tracks before the music engine is fully shut down. We detour the mod unloading process to ensure
            // the music engine shuts down before any tracks are unloaded.

            // This is not a detour, we need to unhook manually.
            Main.instance.Exiting -= Instance_Exiting;

            // Set this back to false just in case another mod has flagged "force high performance."
            // I'm pretty sure this does nothing, as it should be reset by the next load regardless, but this is safety.
            Config.ForceHighPerformance = false;

            // Clears all sound mappings, both for cached sounds and sound instances.
            SoundCache.Unload();

            if (Main.audioSystem is not LegacyAudioSystem system)
                return;

            while (MonoStereoEngine.IsRunning)
                Thread.Sleep(100);

            // These tracks are not included in the mappings, nor the automatic unloading
            // whenever this mod is unloaded by TML (they are not registered with the MusicLoader)
            foreach (var track in system.AudioTracks.Where(s => s is MonoStereoAudioTrack))
                track.Dispose();

            // Remove the MonoStereo source (which is a psuedo-texture pack) from the available sources
            system.UseSources(system.FileSources.RemoveMonoStereoSource());
        }

        internal static void Instance_Exiting(object sender, EventArgs e) => ModRunning = false;

        #region API

        /// <summary>
        /// Attempts to retrieve the <see cref="MonoStereoAudioTrack"/> associated with the music track at the specified index.
        /// </summary>
        /// <param name="musicIndex">The index of the music you want to retrieve.</param>
        /// <returns>The <see cref="MonoStereoAudioTrack"/> associated with the music track at the specified index,<br/>
        /// or <see langword="null"/> if the track could not be resolved.</returns>
        public static MonoStereoAudioTrack GetSong(int musicIndex)
            => Main.audioSystem is LegacyAudioSystem system && system.AudioTracks[musicIndex] is MonoStereoAudioTrack track ? track : null;


        /// <summary>
        /// Replacement for <see cref="MusicLoader.AddMusic(Mod, string)"/> that allows you to utilize your own <see cref="ISongSource"/> implementation,<br/>
        /// instead of being forced to only use file readers.<br/><br/>
        /// </summary>
        /// <param name="mod">The <see cref="Mod"/> that is adding this custom music.</param>
        /// <param name="musicName">The name to associate with the custom music.<br/>
        /// You will use this for <see cref="MusicLoader.GetMusic(Mod, string)"/> and <see cref="MusicLoader.GetMusicSlot(Mod, string)"/></param>
        /// <param name="source">The custom song source you want to register.</param>
        public static void AddCustomMusic(Mod mod, string musicName, ISongSource source)
            => AddCustomMusic(mod, musicName, new MonoStereoAudioTrack(source));


        /// <summary>
        /// Replacement for <see cref="MusicLoader.AddMusic(Mod, string)"/> that allows you to utilize your own <see cref="Song"/> implementation,<br/>
        /// instead of being forced to only use file readers.<br/><br/>
        /// In most cases, you should be fine with simply creating a custom <see cref="ISongSource"/> and using <see cref="AddCustomMusic(Mod, string, ISongSource)"/> instead.<br/>
        /// However, in cases where you need more control wrapping that source, you can inherit from <see cref="MonoStereoAudioTrack"/> and add extra properties/methods.
        /// </summary>
        /// <param name="mod">The <see cref="Mod"/> that is adding this custom music.</param>
        /// <param name="musicName">The name to associate with the custom music.<br/>
        /// You will use this for <see cref="MusicLoader.GetMusic(Mod, string)"/> and <see cref="MusicLoader.GetMusicSlot(Mod, string)"/></param>
        /// <param name="track">The custom track you want to register.</param>
        public static void AddCustomMusic(Mod mod, string musicName, MonoStereoAudioTrack track)
        {
            // This can only be called during mod loading.
            if (!mod.IsLoading())
                throw new Exception($"{nameof(AddCustomMusic)} can only be called during mod loading.");

            string musicPath = $"{mod.Name}/{musicName}";
            SoundCache.CustomMusicSlots[musicPath] = track;

            RegisterCustomMusic(musicPath, ".monostereo");
        }


        /// <summary>
        /// Alternative to <see cref="MusicLoader.AddMusic(Mod, string)"/> which allows you to force a certain track to be "high performance."<br/><br/>
        /// High performance tracks have all of their data cached to memory while they are being played,
        /// which drastically improves performance, at the cost of some extra memory overhead.<br/>
        /// This allows for certain filters to be possible that weren't before (particularly, filters that rapidly seek underlying streams).
        /// </summary>
        /// <param name="mod">The <see cref="Mod"/> that is adding this music.</param>
        /// <param name="musicPath">The name to associate with the custom music.<br/>
        /// You will use this for <see cref="MusicLoader.GetMusic(Mod, string)"/> and <see cref="MusicLoader.GetMusicSlot(Mod, string)"/></param>
        public static void AddHighPerformanceMusic(Mod mod, string musicPath)
        {
            // This can only be called during mod loading.
            if (!mod.IsLoading())
                throw new Exception($"{nameof(AddHighPerformanceMusic)} can only be called during mod loading.");

            // Locate the base extension for the supplied file path.
            string chosenExtension = "";
            string[] supportedExtensions = MusicLoaderSupportedExtensions();

            foreach (string extension in supportedExtensions.Where(extension => mod.FileExists(musicPath + extension)))
                chosenExtension = extension;

            // If no file was found with a supported extension, throw
            if (string.IsNullOrEmpty(chosenExtension))
                throw new ArgumentException($"Given path found no files matching the extensions [ {string.Join(", ", supportedExtensions)} ]");

            // Replace the . in the extension with a new prefix that denotes
            // that this track should be high performance.
            string fileExtension = chosenExtension.Replace(".", HighPerformanceExtensionPrefix);
            musicPath = $"{mod.Name}/{musicPath}";

            RegisterCustomMusic(musicPath, fileExtension);
        }


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
