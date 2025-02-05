using MonoMod.RuntimeDetour;
using MonoStereo;
using System.Reflection;
using System.Threading;
using Terraria.ModLoader;

namespace MonoStereoMod.Detours
{
    internal static partial class Detours
    {
        #region Hooks, Delegates, and Reflection, Oh My!

        public static Hook ModContent_UnloadModContent_Hook;

        public static MethodInfo ModContent_UnloadModContent_Method = typeof(ModContent).GetMethod("UnloadModContent", BindingFlags.NonPublic | BindingFlags.Static);

        public delegate void ModContent_UnloadModContent_OrigDelegate();

        #endregion

        // We ensure that the audio engine stops running before content is unloaded so that tracks
        // aren't being played and unloaded at the same time, as this can happen in rare edge cases with buffered
        // audio reading.
        public static void On_ModContent_UnloadModContent(ModContent_UnloadModContent_OrigDelegate orig)
        {
            MonoStereoMod.ModRunning = false;

            // Check to see if the audio engine has shut down yet.
            while (AudioManager.IsRunning)
                Thread.Sleep(100);

            orig();
        }
    }
}
