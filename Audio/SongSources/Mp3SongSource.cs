using MonoStereo;
using MonoStereo.AudioSources;
using MonoStereo.Encoding;
using NAudio.Wave;
using System.Collections.Generic;
using System.IO;

namespace MonoStereoMod.Audio.Reading
{
    internal class Mp3SongSource : ITerrariaSongSource
    {
        internal readonly Mp3FileReader reader;

        internal readonly ISampleProvider provider;

        internal Mp3SongSource(Stream stream, string fileName)
        {
            FileName = fileName;

            Comments = stream.ReadComments();
            Comments.ParseLoop(out long loopStart, out long loopEnd, AudioStandards.ChannelCount);

            reader = new(stream);
            provider = reader.ToSampleProvider().Reformat(ref loopStart, ref loopEnd);

            LoopStart = loopStart;
            LoopEnd = loopEnd;
        }

        public string FileName { get; }

        public PlaybackState PlaybackState { get; set; } = PlaybackState.Stopped;

        public Dictionary<string, string> Comments { get; }

        public long Length => reader.Length / reader.WaveFormat.BlockAlign * reader.WaveFormat.Channels;

        public bool IsLooped { get; set; } = true;

        public long LoopStart { get; set; }

        public long LoopEnd { get; set; }

        public long Position
        {
            get => reader.Position / reader.WaveFormat.BlockAlign * provider.WaveFormat.Channels;
            set => reader.Position = value / provider.WaveFormat.Channels * reader.WaveFormat.BlockAlign;
        }

        public WaveFormat WaveFormat => provider.WaveFormat;

        public void Close()
        {
            reader.Dispose();
        }

        public int Read(float[] buffer, int offset, int count) => provider.LoopedRead(buffer, offset, count, this, IsLooped, Length, LoopStart, LoopEnd);
    }
}
