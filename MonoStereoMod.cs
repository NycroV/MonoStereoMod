global using static MonoStereoMod.Utils.MonoStereoUtils;
using static MonoStereoMod.Detours.ActiveSoundDetours;
using static MonoStereoMod.Detours.SoundPlayerDetours;
using static MonoStereoMod.Detours.SoundEngineDetours;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using MonoStereoMod.Systems;

namespace MonoStereoMod
{
    public class MonoStereoMod : Mod
	{
        public static bool ModRunning { get; set; } = false;

        public static MonoStereoMod Instance { get; private set; } = null;

        internal RootlessTmodContentSource RootlessSource => new(this.File());

        static void Instance_Exiting(object sender, System.EventArgs e) => ModRunning = false;

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
    }
}
