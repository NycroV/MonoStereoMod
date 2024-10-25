using MonoStereo;
using Terraria.ModLoader;

namespace MonoStereoMod.src.Systems
{
    // This forwards any errors thrown on the audio thread to tML for logging.
    internal class ErrorReportingSystem : ModSystem
    {
        public override void PostUpdateEverything() => AudioManager.ThrowIfErrored();
    }
}
