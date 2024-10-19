using MonoStereo;
using MonoStereo.AudioSources;
using MonoStereo.Encoding;
using NAudio.Wave;
using System.Collections.Generic;
using System.IO;

namespace MonoStereoMod.Audio
{
    internal class WavSongSource : ITerrariaSongSource
    {
        public WavSongSource(Stream stream, string fileName)
        {
            FileName = fileName;
            Comments = stream.ReadComments();

            readerStream = new WaveFileReader(stream);
            Comments.ParseLoop(out long loopStart, out long loopEnd, AudioStandards.ChannelCount);

            if (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm && readerStream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
                readerStream = new BlockAlignReductionStream(readerStream);
            }

            source = readerStream.ToSampleProvider().Reformat(ref loopStart, ref loopEnd);

            LoopStart = loopStart;
            LoopEnd = loopEnd;

            long sampleLength = readerStream.Length / readerStream.WaveFormat.BlockAlign * readerStream.WaveFormat.Channels;

            if (readerStream.WaveFormat.SampleRate != WaveFormat.SampleRate)
            {
                sampleLength = (long)((float)sampleLength / readerStream.WaveFormat.SampleRate * WaveFormat.SampleRate);
                sampleLength -= (sampleLength % WaveFormat.Channels);
            }

            if (readerStream.WaveFormat.Channels != WaveFormat.Channels)
                sampleLength *= AudioStandards.ChannelCount;

            Length = sampleLength;
        }

        public string FileName { get; }

        internal readonly WaveStream readerStream;

        internal readonly ISampleProvider source;

        public PlaybackState PlaybackState { get; set; } = PlaybackState.Stopped;

        public Dictionary<string, string> Comments { get; }

        public long Length { get; }

        public bool IsLooped { get; set; } = true;

        public long LoopStart { get; set; }

        public long LoopEnd { get; set; }

        public long Position
        {
            get
            {
                long samplePos = SamplePosition;

                if (readerStream.WaveFormat.SampleRate != WaveFormat.SampleRate)
                {
                    samplePos = (long)((float)samplePos / readerStream.WaveFormat.SampleRate * WaveFormat.SampleRate);
                    samplePos -= (samplePos % WaveFormat.Channels);
                }

                if (readerStream.WaveFormat.Channels != WaveFormat.Channels)
                    samplePos *= AudioStandards.ChannelCount;

                return samplePos;
            }

            set
            {
                if (value != Position)
                {
                    if (readerStream.WaveFormat.SampleRate != WaveFormat.SampleRate)
                    {
                        value = (long)((float)value / WaveFormat.SampleRate * readerStream.WaveFormat.SampleRate);
                        value -= (value % WaveFormat.Channels);
                    }

                    if (readerStream.WaveFormat.Channels != WaveFormat.Channels)
                        value /= 2;

                    SamplePosition = value;
                }
            }
        }

        private long SamplePosition
        {
            get => readerStream.Position / readerStream.BlockAlign * source.WaveFormat.Channels;
            set => readerStream.Position = value / source.WaveFormat.Channels * readerStream.BlockAlign;
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        public void Close()
        {
            readerStream.Dispose();
        }

        public int Read(float[] buffer, int offset, int count) => source.LoopedRead(buffer, offset, count, this, IsLooped, Length, LoopStart, LoopEnd);
    }
}
