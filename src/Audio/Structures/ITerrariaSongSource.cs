
// Add this to the same namespace as the normal ISongSource
using MonoStereo.SampleProviders;

namespace MonoStereo.AudioSources
{
    public interface ITerrariaSongSource : ISeekableSongSource, ILoopTags
    { }
}
