using MonoStereo.Sources;
using MonoStereo.Sources.Songs;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoStereoMod.Content.Audio
{
    internal class TerrariaUniversalAudioSource(UniversalAudioSource source) : ITerrariaSongSource
    {
        private readonly UniversalAudioSource _source = source;

        public PlaybackState PlaybackState { get => _source.PlaybackState; set => _source.PlaybackState = value; }

        public Dictionary<string, string> Comments => _source.Comments;

        public bool IsLooped { get => _source.IsLooped; set => _source.IsLooped = value; }

        public WaveFormat WaveFormat => _source.WaveFormat;

        public long Position { get => _source.Position; set => _source.Position = value; }

        public long Length => _source.Length;

        public long LoopStart => _source.LoopStart;

        public long LoopEnd => _source.LoopEnd;

        public void Dispose() => _source.Dispose();

        public int Read(float[] buffer, int offset, int count) => _source.Read(buffer, offset, count);
    }
}
