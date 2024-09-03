using MonoStereo.Encoding;
using MonoStereo.AudioSources;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave.SampleProviders;
using MonoStereo;

namespace MonoStereoMod.Audio.Reading
{
    internal class WavSongSource : ISongSource
    {
        public WavSongSource(Stream stream, string fileName)
        {
            Comments = stream.ReadComments();
            Comments.ParseLoop(out long loopStart, out long loopEnd);

            LoopStart = loopStart;
            LoopEnd = loopEnd;

            readerStream = new WaveFileReader(stream);
            if (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm && readerStream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
                readerStream = new BlockAlignReductionStream(readerStream);
            }            

            sourceBytesPerSample = readerStream.WaveFormat.BitsPerSample / 8 * readerStream.WaveFormat.Channels;
            sampleChannel = new SampleChannel(readerStream, forceStereo: true);

            if (sampleChannel.WaveFormat.SampleRate != AudioStandards.SampleRate)
                sampleChannel = new WdlResamplingSampleProvider(sampleChannel, AudioStandards.SampleRate);

            destBytesPerSample = 4 * sampleChannel.WaveFormat.Channels;

            Length = SourceToDest(readerStream.Length);
        }

        internal readonly WaveStream readerStream;

        public PlaybackState PlaybackState { get; set; } = PlaybackState.Playing;

        public Dictionary<string, string> Comments { get; }

        public long Length { get; }

        public bool IsLooped { get; set; } = true;

        public long LoopStart { get; set; }

        public long LoopEnd { get; set; }

        private readonly object @lock = new();

        public long Position
        {
            get
            {
                return SourceToDest(readerStream.Position);
            }
            set
            {
                lock (@lock)
                {
                    readerStream.Position = DestToSource(value);
                }
            }
        }

        public WaveFormat WaveFormat => sampleChannel.WaveFormat;

        public void Close()
        {
            readerStream.Dispose();
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesCopied = 0;

            do
            {
                long endIndex = Length;

                if (IsLooped && LoopEnd != -1)
                    endIndex = LoopEnd;

                long samplesAvailable = endIndex - Position;
                long samplesRemaining = count - samplesCopied;

                int samplesToCopy = (int)Math.Min(samplesAvailable, samplesRemaining);
                if (samplesToCopy > 0)
                    samplesCopied += sampleChannel.Read(buffer, offset + samplesCopied, samplesToCopy);

                if (IsLooped && Position == endIndex)
                {
                    long startIndex = Math.Max(0, LoopStart);
                    Position = startIndex;
                }
            }
            while (IsLooped && samplesCopied < count);

            return samplesCopied;
        }

        readonly ISampleProvider sampleChannel;
        readonly int destBytesPerSample;
        readonly int sourceBytesPerSample;

        private long SourceToDest(long sourceBytes) => destBytesPerSample * (sourceBytes / sourceBytesPerSample);        
        private long DestToSource(long destBytes) => sourceBytesPerSample * (destBytes / destBytesPerSample);
    }
}
