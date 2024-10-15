using MonoStereo.AudioSources;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoStereoMod.Audio
{
    internal class HighPerformanceSongSource : ITerrariaSongSource
    {
        public HighPerformanceSongSource(ISongSource source)
        {

        }

        public long LoopStart { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public long LoopEnd { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public PlaybackState PlaybackState { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Dictionary<string, string> Comments => throw new NotImplementedException();

        public bool IsLooped { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public long Length => throw new NotImplementedException();

        public WaveFormat WaveFormat => throw new NotImplementedException();

        public void Close()
        {
            throw new NotImplementedException();
        }

        public int Read(float[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
