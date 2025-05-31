using Microsoft.Xna.Framework;
using MonoStereoMod.Systems;
using Terraria;
using Terraria.Audio;

namespace MonoStereoMod.Detours
{
    internal static partial class Detours
    {
        // Vanilla behavior, but we apply volume to the master mixers instead of each individual sound.
        public static void On_ActiveSound_Update(On_ActiveSound.orig_Update orig, ActiveSound self)
        {
            var sound = self.Sound;

            if (sound is null || sound.IsDisposed || !SoundCache.TryGetMonoStereo(sound, out _))
            {
                orig(self);
                return;
            }

            if (!Program.IsMainThread)
            {
                RunOnMainThreadAndWait(() => On_ActiveSound_Update(orig, self));
                return;
            }

            if (sound.IsDisposed)
                return;

            if (self.Callback?.Invoke(self) == false)
            {
                sound.Stop(immediate: true);
                return;
            }

            Vector2 value = Main.screenPosition + new Vector2(Main.screenWidth / 2, Main.screenHeight / 2);
            float num = 1f;

            //if (!IsGlobal) {
            if (self.Position.HasValue)
            {
                float value2 = (self.Position.Value.X - value.X) / (Main.screenWidth * 0.5f);
                value2 = MathHelper.Clamp(value2, -1f, 1f);

                sound.Pan = value2;
                float num2 = Vector2.Distance(self.Position.Value, value);
                num = 1f - num2 / (Main.screenWidth * 1.5f);
            }

            num *= self.Style.Volume * self.Volume;

            // Rather than individually set the volume of each song, it is applied in the
            // MonoStereoAudioSystem with AudioManager.MusicVolume and AudioManager.SoundVolume

            /*switch (self.Style.Type)
            {
                case SoundType.Sound:
                    num *= Main.soundVolume;
                    break;

                case SoundType.Ambient:
                    num *= Main.ambientVolume;
                    // Added by TML to mimic the behavior of the LegacySoundPlayer code.
                    if (Main.gameInactive)
                        num = 0f;
                    break;

                case SoundType.Music:
                    num *= Main.musicVolume;
                    break;
            }*/

            // The only difference is we manually apply ambient volume.
            // While MonoStereo contains the ability to mix sounds and ambience differently,
            // FNA does not, and since we are detouring the FNA methods for playing, there
            // is no way for us to make distinctions between the two sound types.
            if (self.Style.Type == SoundType.Ambient)
            {
                // Ambient tracks go into the sound player
                num *= Main.ambientVolume / Main.soundVolume;

                if (Main.gameInactive)
                    num = 0f;
            }

            num = MathHelper.Clamp(num, 0f, 1f);
            sound.Volume = num;
            sound.Pitch = self.Pitch;
        }
    }
}
