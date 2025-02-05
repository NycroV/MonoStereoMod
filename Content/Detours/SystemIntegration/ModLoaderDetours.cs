﻿using MonoMod.RuntimeDetour;
using MonoStereo;
using System.Reflection;
using System.Threading;
using Terraria.ModLoader;

namespace MonoStereoMod.Detours
{
    internal static partial class Detours
    {
        #region Hooks, Delegates, and Reflection, Oh My!

        public static Hook ModLoader_UnloadModContent_Hook;

        public static MethodInfo ModLoader_UnloadModContent_Method = typeof(ModLoader).GetMethod("UnloadModContent", BindingFlags.NonPublic | BindingFlags.Static);

        public delegate void ModLoader_UnloadModContent_OrigDelegate();

        #endregion

        // We ensure that the audio engine stops running before content is unloaded so that tracks
        // aren't being played and unloaded at the same time, as this can happen in rare edge cases with buffered
        // audio reading.
        public static void On_ModLoader_UnloadModContent(ModLoader_UnloadModContent_OrigDelegate orig)
        {
            MonoStereoMod.ModRunning = false;

            // Check to see if the audio engine has shut down yet.
            while (AudioManager.IsRunning)
                Thread.Sleep(100);

            orig();
        }
    }
}
