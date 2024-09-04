using Microsoft.Xna.Framework;
using MonoStereo;
using MonoStereoMod.Utils;
using Terraria;
using Terraria.Audio;

namespace MonoStereoMod.Detours
{
    internal static partial class Detours
    {
        public static void On_ActiveSound_Play(On_ActiveSound.orig_Play orig, ActiveSound self)
        {
            if (!Program.IsMainThread)
            {
                RunOnMainThreadAndWait(() => On_ActiveSound_Play(orig, self));
                return;
            }

            var soundEffectInstance = self.Style.GetRandomSoundEffect().CreateInstance();

            soundEffectInstance.Pitch += self.Style.GetRandomPitch();
            self.Pitch = soundEffectInstance.Pitch;

            soundEffectInstance.IsLooped = self.Style.IsLooped;
            soundEffectInstance.Play();

            SoundCache.Set(self, soundEffectInstance);
            self.Update();
        }

        public static void On_ActiveSound_Stop(On_ActiveSound.orig_Stop orig, ActiveSound self)
        {
            if (!Program.IsMainThread)
            {
                RunOnMainThreadAndWait(() => On_ActiveSound_Stop(orig, self));
                return;
            }

            SoundCache.Get(self)?.Stop();
        }

        public static void On_ActiveSound_Pause(On_ActiveSound.orig_Pause orig, ActiveSound self)
        {
            if (!Program.IsMainThread)
            {
                RunOnMainThreadAndWait(() => On_ActiveSound_Pause(orig, self));
                return;
            }

            SoundCache.Get(self)?.Pause();
        }

        public static void On_ActiveSound_Resume(On_ActiveSound.orig_Resume orig, ActiveSound self)
        {
            if (!Program.IsMainThread)
            {
                RunOnMainThreadAndWait(() => On_ActiveSound_Resume(orig, self));
                return;
            }

            SoundCache.Get(self)?.Resume();
        }

        public static void On_ActiveSound_Update(On_ActiveSound.orig_Update orig, ActiveSound self)
        {
            var sound = SoundCache.Get(self);
            if (sound is null)
                return;

            if (!Program.IsMainThread)
            {
                RunOnMainThreadAndWait(() => On_ActiveSound_Update(orig, self));
                return;
            }

            if (sound.IsDisposed)
                return;

            if (self.Callback?.Invoke(self) == false)
            {
                sound.Stop();
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
            switch (self.Style.Type)
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
            }

            num = MathHelper.Clamp(num, 0f, 1f);
            sound.Volume = num;
            sound.Pitch = self.Pitch;
        }
    }
}
