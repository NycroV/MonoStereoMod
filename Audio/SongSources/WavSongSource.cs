using MonoStereo.AudioSources;
using MonoStereo.Encoding;
using NAudio.Wave;
using System.Collections.Generic;
using System.IO;

namespace MonoStereoMod.Audio.Reading
{
    internal class WavSongSource : ISongSource
    {
        public WavSongSource(Stream stream, string fileName)
        {
            FileName = fileName;
            Comments = stream.ReadComments();
            Comments.ParseLoop(out long loopStart, out long loopEnd);

            readerStream = new WaveFileReader(stream);
            if (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm && readerStream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
                readerStream = new BlockAlignReductionStream(readerStream);
            }

            sourceBytesPerSample = readerStream.WaveFormat.BitsPerSample / 8 * readerStream.WaveFormat.Channels;
            destBytesPerSample = 4 * source.WaveFormat.Channels;

            Length = SourceToDest(readerStream.Length);
            source = readerStream.ToSampleProvider().Reformat(ref loopStart, ref loopEnd);

            LoopStart = loopStart;
            LoopEnd = loopEnd;
        }

        public string FileName { get; }

        internal readonly WaveStream readerStream;

        public PlaybackState PlaybackState { get; set; } = PlaybackState.Stopped;

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

        public WaveFormat WaveFormat => source.WaveFormat;

        public void Close()
        {
            readerStream.Dispose();
        }

        public int Read(float[] buffer, int offset, int count) => source.LoopedRead(buffer, offset, count, this, IsLooped, Length, LoopStart, LoopEnd);

        readonly ISampleProvider source;
        readonly int destBytesPerSample;
        readonly int sourceBytesPerSample;

        private long SourceToDest(long sourceBytes) => destBytesPerSample * (sourceBytes / sourceBytesPerSample);
        private long DestToSource(long destBytes) => sourceBytesPerSample * (destBytes / destBytesPerSample);
    }
}
