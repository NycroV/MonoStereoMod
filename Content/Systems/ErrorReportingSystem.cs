using MonoStereo;
using Terraria.ModLoader;

namespace MonoStereoMod.Systems
{
    // This forwards any errors thrown on the audio thread to tML for logging.
    internal class ErrorReportingSystem : ModSystem
    {
        public override void PostUpdateEverything() => MonoStereoEngine.ThrowIfErrored();
    }
}
