using MonoStereoMod.Utils;
using ReLogic.Utilities;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;

namespace MonoStereoMod.Detours
{
    internal static partial class Detours
    {
        public static SlotId On_SoundPlayer_Play_Inner(On_SoundPlayer.orig_Play_Inner orig, SoundPlayer self, ref SoundStyle style, Microsoft.Xna.Framework.Vector2? position, SoundUpdateCallback updateCallback)
        {
            // Handle the MaxInstances & RestartIfPlaying properties
            int maxInstances = style.MaxInstances;

            if (maxInstances > 0)
            {
                int instanceCount = 0;

                foreach (var pair in SoundCache.ActiveSounds)
                {
                    var activeSound = pair.Key;
                    var soundEffect = pair.Value;

                    if (!soundEffect.IsPlaying || !style.IsTheSameAs(activeSound.Style) || ++instanceCount < maxInstances)
                    {
                        continue;
                    }

                    switch (style.SoundLimitBehavior)
                    {
                        case SoundLimitBehavior.ReplaceOldest: //TODO: Make this actually true to its name -- replace the *oldest* sound.
                            activeSound.Stop();
                            break;
                        default:
                            return SlotId.Invalid;
                    }
                }
            }

            SoundStyle styleCopy = style;

            // Handle 'UsesMusicPitch'.. This property is a weird solution for keeping vanilla's old instruments' behavior alive, and is currently internal.
            if (style.UsesMusicPitch())
            {
                styleCopy.Pitch += Main.musicPitch;
            }

            ActiveSound value = new(styleCopy, position, updateCallback);
            return self.TrackedSounds().Add(value);
        }

        public static void On_SoundPlayer_Update(On_SoundPlayer.orig_Update orig, SoundPlayer self)
        {
            var trackedSounds = self.TrackedSounds();

            foreach (SlotVector<ActiveSound>.ItemPair item in (IEnumerable<SlotVector<ActiveSound>.ItemPair>)trackedSounds)
            {
                try
                {
                    item.Value.Update();
                    var soundEffect = SoundCache.Get(item.Value);

                    if (soundEffect is not null && !soundEffect.IsPlaying)
                        trackedSounds.Remove(item.Id);
                }
                catch
                {
                    trackedSounds.Remove(item.Id);
                }
            }
        }
    }
}
