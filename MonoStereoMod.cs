global using static MonoStereoMod.MonoStereoUtil;
using static MonoStereoMod.ActiveSoundDetours;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace MonoStereoMod
{
	public class MonoStereoMod : Mod
	{
        public static bool ModRunning { get; set; } = false;

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

            MonoStereoAudioSystem.Initialize();
        }

        public override void Unload()
        {
            ModRunning = false;
            Main.instance.Exiting -= Instance_Exiting;
        }
    }
}
