using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Terraria;
using Terraria.Audio;

namespace MonoStereoMod
{
    public static class ActiveSoundDetours
    {
        public static void On_ActiveSound_Play(On_ActiveSound.orig_Play orig, ActiveSound self)
        {
            if (!Program.IsMainThread)
            {
                RunOnMainThreadAndWait(() => On_ActiveSound_Play(orig, self));
                return;
            }

            SoundEffectInstance soundEffectInstance = self.Style.GetRandomSound().CreateInstance();
            soundEffectInstance.Pitch += self.Style.GetRandomPitch();
            self.Pitch = soundEffectInstance.Pitch;
            soundEffectInstance.IsLooped = self.Style.IsLooped;
            soundEffectInstance.Play();
            SoundInstanceGarbageCollector.Track(soundEffectInstance);
            self.GetType().GetProperty(nameof(self.Sound)).SetMethod.Invoke(self, [soundEffectInstance]);
            self.Update();
        }

        public static void On_ActiveSound_Stop(On_ActiveSound.orig_Stop orig, ActiveSound self)
        {
            if (!Program.IsMainThread)
            {
                RunOnMainThreadAndWait(() => On_ActiveSound_Stop(orig, self));
                return;
            }

            if (self.Sound != null)
                self.Sound.Stop();
        }

        public static void On_ActiveSound_Pause(On_ActiveSound.orig_Pause orig, ActiveSound self)
        {
            if (!Program.IsMainThread)
            {
                RunOnMainThreadAndWait(() => On_ActiveSound_Pause(orig, self));
                return;
            }

            if (self.Sound != null && self.Sound.State == SoundState.Playing)
                self.Sound.Pause();
        }

        public static void On_ActiveSound_Resume(On_ActiveSound.orig_Resume orig, ActiveSound self)
        {
            if (!Program.IsMainThread)
            {
                RunOnMainThreadAndWait(() => On_ActiveSound_Resume(orig, self));
                return;
            }

            if (self.Sound != null && self.Sound.State == SoundState.Paused)
                self.Sound.Resume();
        }

        public static void On_ActiveSound_Update(On_ActiveSound.orig_Update orig, ActiveSound self)
        {
            if (self.Sound == null)
                return;

            if (!Program.IsMainThread)
            {
                RunOnMainThreadAndWait(() => On_ActiveSound_Update(orig, self));
                return;
            }

            if (self.Sound.IsDisposed)
                return;

            if (self.Callback?.Invoke(self) == false)
            {
                self.Sound.Stop(immediate: true);
                return;
            }

            Vector2 value = Main.screenPosition + new Vector2(Main.screenWidth / 2, Main.screenHeight / 2);
            float num = 1f;
            //if (!IsGlobal) {
            if (self.Position.HasValue)
            {
                float value2 = (self.Position.Value.X - value.X) / ((float)Main.screenWidth * 0.5f);
                value2 = MathHelper.Clamp(value2, -1f, 1f);
                self.Sound.Pan = value2;
                float num2 = Vector2.Distance(self.Position.Value, value);
                num = 1f - num2 / ((float)Main.screenWidth * 1.5f);
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
                    {
                        num = 0f;
                    }

                    break;
                case SoundType.Music:
                    num *= Main.musicVolume;
                    break;
            }

            num = MathHelper.Clamp(num, 0f, 1f);
            self.Sound.Volume = num;
            self.Sound.Pitch = self.Pitch;
        }
    }
}
