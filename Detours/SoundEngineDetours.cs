using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace MonoStereoMod.Detours
{
    internal static class SoundEngineDetours
    {
        public static void On_SoundEngine_Update(On_SoundEngine.orig_Update orig)
        {
            if (SoundEngine.IsAudioSupported)
            {
                Main.audioSystem?.UpdateAudioEngine();
                SoundCache.CollectGarbage();

                bool flag = (!Main.hasFocus || Main.gamePaused) && Main.netMode == NetmodeID.SinglePlayer;

                if (!SoundEngine.AreSoundsPaused && flag)
                    SoundEngine.SoundPlayer.PauseAll();

                else if (SoundEngine.AreSoundsPaused && !flag)
                    SoundEngine.SoundPlayer.ResumeAll();

                SoundEngine.AreSoundsPaused = flag;
                SoundEngine.SoundPlayer.Update();
            }
        }
    }
}
