global using static MonoStereoMod.Utils.MonoStereoUtils;
using static MonoStereoMod.Detours.ActiveSoundDetours;
using static MonoStereoMod.Detours.SoundPlayerDetours;
using static MonoStereoMod.Detours.SoundEngineDetours;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using MonoStereoMod.Systems;
using MonoStereo;
using System.IO;
using ReLogic.Utilities;
using Microsoft.Xna.Framework;

namespace MonoStereoMod
{
    public class MonoStereoMod : Mod
    {
        public static bool ModRunning { get; set; } = false;

        public static MonoStereoMod Instance { get; private set; } = null;

        internal RootlessTmodContentSource RootlessSource => new(this.File());

        internal static void Instance_Exiting(object sender, System.EventArgs e) => ModRunning = false;

        public override void Load()
        {
            ModRunning = true;
            Main.instance.Exiting += Instance_Exiting;

            On_ActiveSound.Play += On_ActiveSound_Play;
            On_ActiveSound.Stop += On_ActiveSound_Stop;
            On_ActiveSound.Pause += On_ActiveSound_Pause;
            On_ActiveSound.Resume += On_ActiveSound_Resume;
            On_ActiveSound.Update += On_ActiveSound_Update;

            On_SoundPlayer.Play_Inner += On_SoundPlayer_Play_Inner;
            On_SoundPlayer.Update += On_SoundPlayer_Update;

            On_SoundEngine.Update += On_SoundEngine_Update;

            MonoStereoAudioSystem.Initialize();
            Instance = this;
        }

        public override void Unload()
        {
            ModRunning = false;
            Main.instance.Exiting -= Instance_Exiting;
            Instance = null;
        }

        #region API

        /// <summary>
        /// Loads a sound at the designated file path into memory.<br/>
        /// Provide the same path that you would normally give to <see cref="ModContent.Request{T}(string, ReLogic.Content.AssetRequestMode)"/>,
        /// with the file extension added to the end.
        /// </summary>
        /// <param name="filePathWithExtension">The path to the sound file, with the extension included.</param>
        /// <param name="forceReload">Force the sound to be re-read from the file as opposed to being fetched from a cache.</param>
        /// <returns>A sound effect that has been loaded into memory from the specified path.</returns>
        public static CachedSoundEffect LoadSound(string filePathWithExtension, bool forceReload = false)
            => SoundCache.Cache(filePathWithExtension, forceReload);

        /// <summary>
        /// Loads a sound from the provided stream into memory.
        /// </summary>
        /// <param name="sourceStream">The stream to load the sound from.</param>
        /// <param name="fileName">The file name you want to be associated with the resulting sound effect.</param>
        /// <param name="fileExtensionWithDotAtTheBeginning">The file extension prefixed with a <c>.</c><br/>
        /// Example: <c>.ogg</c></param>
        /// <param name="disposeAfterRead">Whether the <paramref name="sourceStream"/> should automatically be closed after reading.</param>
        /// <returns>A sound effect that has been loaded into memory from the provided stream.</returns>
        public static CachedSoundEffect LoadSound(Stream sourceStream, string fileName, string fileExtensionWithDotAtTheBeginning, bool disposeAfterRead = false)
        {
            var soundEffect = LoadSoundEffect(sourceStream, fileName, fileExtensionWithDotAtTheBeginning);

            if (disposeAfterRead)
                soundEffect.Dispose();

            return soundEffect;
        }

        /// <summary>
        /// Attempts to retrieve the <see cref="MonoStereoAudioTrack"/> associated with the music track at the specified index.
        /// </summary>
        /// <param name="musicIndex">The index of the music you want to retrieve.</param>
        /// <returns>The <see cref="MonoStereoAudioTrack"/> associated with the music track at the specified index,<br/>
        /// or <see langword="null"/> if the track could not be resolved.</returns>
        public static MonoStereoAudioTrack GetSong(int musicIndex)
            => Main.audioSystem is MonoStereoAudioSystem system ? system.AudioTracks[musicIndex] is MonoStereoAudioTrack track ? track : null : null;

        /// <summary>
        /// Attempts to get the <see cref="TerrariaSoundEffect"/> associated with the sound at the specified <paramref name="slotId"/>
        /// </summary>
        /// <param name="slotId">The <see cref="SlotId"/> of the sound you want to retrieve.</param>
        /// <param name="sound">The resulting <see cref="TerrariaSoundEffect"/>, or <see langword="null"/> if it could not be resolved.</param>
        /// <returns>Whether the retrieval was successful.</returns>
        public static bool TryGetActiveSound(SlotId slotId, out TerrariaSoundEffect sound)
        {
            sound = null;
            return SoundEngine.TryGetActiveSound(slotId, out var activeSound) && SoundCache.TryGet(activeSound, out sound);
        }

        /// <summary>
        /// Attempts to get the <see cref="TerrariaSoundEffect"/> associated with the sound at the specified <paramref name="activeSound"/>
        /// </summary>
        /// <param name="activeSound">The <see cref="ActiveSound"/> component of the sound you want to retrieve.</param>
        /// <param name="sound">The resulting <see cref="TerrariaSoundEffect"/>, or <see langword="null"/> if it could not be resolved.</param>
        /// <returns>Whether the retrieval was successful.</returns>
        public static bool TryGetActiveSound(ActiveSound activeSound, out TerrariaSoundEffect sound)
            => SoundCache.TryGet(activeSound, out sound);

        /// <summary>
        /// Plays a sound and returns the associated <see cref="TerrariaSoundEffect"/>
        /// </summary>
        /// <returns>The <see cref="TerrariaSoundEffect"/> that is being played.</returns>
        public static TerrariaSoundEffect PlaySound(in SoundStyle style, Vector2? position = null, SoundUpdateCallback callback = null)
            => TryGetActiveSound(SoundEngine.PlaySound(style, position, callback), out var sound) ? sound : null;

        #endregion
    }
}
