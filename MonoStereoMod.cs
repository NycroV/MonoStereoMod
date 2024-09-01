using MonoStereoMod.AudioSytem;
using Terraria;
using Terraria.ModLoader;

namespace MonoStereoMod
{
	public class MonoStereoMod : Mod
	{
        public static bool ModRunning { get; set; } = false;

        public override void Load()
        {
            ModRunning = true;
            Main.instance.Exiting += Instance_Exiting;

            MonoStereoAudioSystem.Initialize();
        }

        public override void Unload()
        {
            ModRunning = false;
            Main.instance.Exiting -= Instance_Exiting;
        }

        private void Instance_Exiting(object sender, System.EventArgs e) => ModRunning = false;
    }
}
