
// Add this to the same namespace as the normal ISongSource
namespace MonoStereo.AudioSources
{
    public interface ITerrariaSongSource : ISongSource
    {
        public long LoopStart { get; set; }

        public long LoopEnd { get; set; }
    }
}
