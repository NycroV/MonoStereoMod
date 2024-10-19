using MonoStereo;
using MonoStereo.AudioSources;
using MonoStereo.Encoding;
using NAudio.Wave;
using System.Collections.Generic;
using System.IO;

namespace MonoStereoMod.Audio
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

            long sampleLength = reader.Length / reader.WaveFormat.BlockAlign * reader.WaveFormat.Channels;

            if (reader.WaveFormat.SampleRate != WaveFormat.SampleRate)
            {
                sampleLength = (long)((float)sampleLength / reader.WaveFormat.SampleRate * WaveFormat.SampleRate);
                sampleLength -= (sampleLength % WaveFormat.Channels);
            }

            if (reader.WaveFormat.Channels != WaveFormat.Channels)
                sampleLength *= AudioStandards.ChannelCount;

            Length = sampleLength;
        }

        public string FileName { get; }

        public PlaybackState PlaybackState { get; set; } = PlaybackState.Stopped;

        public Dictionary<string, string> Comments { get; }

        public bool IsLooped { get; set; } = true;

        public long LoopStart { get; set; }

        public long LoopEnd { get; set; }

        public long Length { get; }

        private long SamplePosition
        {
            get => reader.Position / reader.WaveFormat.BlockAlign * provider.WaveFormat.Channels;
            set => reader.Position = value / provider.WaveFormat.Channels * reader.WaveFormat.BlockAlign;
        }

        public long Position
        {
            get
            {
                long samplePos = SamplePosition;

                if (reader.WaveFormat.SampleRate != WaveFormat.SampleRate)
                {
                    samplePos = (long)((float)samplePos / reader.WaveFormat.SampleRate * WaveFormat.SampleRate);
                    samplePos -= (samplePos % WaveFormat.Channels);
                }

                if (reader.WaveFormat.Channels != WaveFormat.Channels)
                    samplePos *= AudioStandards.ChannelCount;

                return samplePos;
            }

            set
            {
                if (value != Position)
                {
                    if (reader.WaveFormat.SampleRate != WaveFormat.SampleRate)
                    {
                        value = (long)((float)value / WaveFormat.SampleRate * reader.WaveFormat.SampleRate);
                        value -= (value % WaveFormat.Channels);
                    }

                    if (reader.WaveFormat.Channels != WaveFormat.Channels)
                        value /= 2;

                    SamplePosition = value;
                }
            }
        }

        public WaveFormat WaveFormat => provider.WaveFormat;

        public void Close()
        {
            reader.Dispose();
        }

        public int Read(float[] buffer, int offset, int count) => provider.LoopedRead(buffer, offset, count, this, IsLooped, Length, LoopStart, LoopEnd);
    }
}
