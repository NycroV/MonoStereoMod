using MonoStereoMod.Systems;
using Terraria.Audio;

namespace MonoStereoMod.Detours
{
    internal static partial class Detours
    {
        // Also trigger our own sound garbage collection after
        // vanilla has finished with their garbage collection.
        public static void On_SoundInstanceGarbageCollector_Update(On_SoundInstanceGarbageCollector.orig_Update orig)
        {
            orig();
            SoundCache.CollectGarbage();
        }
    }
}
