using Terraria.Audio;

namespace MonoStereoMod.Detours
{
    internal static partial class Detours
    {
        public static void On_SoundInstanceGarbageCollector_Update(On_SoundInstanceGarbageCollector.orig_Update orig)
        {
            orig();
            SoundCache.CollectGarbage();
        }
    }
}
