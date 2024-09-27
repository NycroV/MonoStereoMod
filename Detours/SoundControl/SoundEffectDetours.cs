using Microsoft.Xna.Framework.Audio;
using MonoMod.RuntimeDetour;
using MonoStereoMod.Audio.Structures;
using System.Reflection;

namespace MonoStereoMod.Detours
{
    internal static partial class Detours
    {
        #region Hooks, Delegates, and Reflection, Oh My!

        public static Hook SoundEffect_CreateInstance_Hook;

        public static Hook SoundEffect_Play_Hook;

        public static MethodInfo SoundEffect_CreateInstance_Method = typeof(SoundEffect).GetMethod("CreateInstance", BindingFlags.Public | BindingFlags.Instance);

        public static MethodInfo SoundEffect_Play_Method = typeof(SoundEffect).GetMethod("Play", BindingFlags.Instance | BindingFlags.Public, [typeof(float), typeof(float), typeof(float)]);

        public delegate SoundEffectInstance SoundEffect_CreateInstance_OrigDelegate(SoundEffect self);

        public delegate bool SoundEffect_Play_OrigDelegate(SoundEffect self, float volume, float pitch, float pan);

        #endregion

        // Vanilla creation behavior, but also created a MonoStereo mapping
        // that all property/method calls for the FNA instance should be forwarded to.
        public static SoundEffectInstance On_SoundEffect_CreateInstance(SoundEffect_CreateInstance_OrigDelegate orig, SoundEffect self)
        {
            var xnaInstance = orig(self);
            var msInstance = new MonoStereoSoundEffect(new TerrariaCachedSoundEffectReader(SoundCache.GetCachedSound(self)));

            SoundCache.Map(xnaInstance, msInstance);
            return xnaInstance;
        }

        // Literally exactly the same as vanilla behavior, but
        // replaces `new SoundEffectInstance(SoundEffect)` with `SoundEffect.CreateInstance()`,
        // that way we can guarantee sound mappings for the SoundEffect exist.
        public static bool On_SoundEffect_Play(SoundEffect_Play_OrigDelegate orig, SoundEffect self, float volume, float pitch, float pan)
        {
            var instance = self.CreateInstance();
            instance.Volume = volume;
            instance.Pitch = pitch;
            instance.Pan = pan;
            instance.Play();
            if (instance.State != SoundState.Playing)
            {
                // Ran out of AL sources, probably.
                instance.Dispose();
                return false;
            }
            return true;
        }
    }
}
