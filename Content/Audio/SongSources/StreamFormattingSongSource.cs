using MonoStereo;
using MonoStereo.Decoding;
using MonoStereo.Sources.Songs;
using MonoStereoMod.Utils;
using NAudio.Wave;
using System.Collections.Generic;

namespace MonoStereoMod.Audio
{
    public class StreamFormattingSongSource : ITerrariaSongSource
    {
        public StreamFormattingSongSource(WaveStream waveStream, Dictionary<string, string> comments)
        {
            // Metadata
            WaveStream = waveStream;
            Comments = comments;
            comments.ParseLoop(out long loopStart, out long loopEnd, AudioStandards.ChannelCount);

            // Source reading
            SourceLoopStart = loopStart;
            SourceLoopEnd = loopEnd;

            LoopedSource = new(WaveStream, LoopStart, LoopEnd);
            SourceLength = LoopedSource.Length;

            // Resampled reading
            long scaledLoopStart = SourceLoopStart;
            long scaledLoopEnd = SourceLoopEnd;
            long scaledLength = SourceLength;

            OutputSource = LoopedSource.Reformat(ref scaledLoopStart, ref scaledLoopEnd, ref scaledLength);
            sampleScalar = (float)LoopedSource.WaveFormat.SampleRate / OutputSource.WaveFormat.SampleRate;

            LoopStart = scaledLoopStart;
            LoopEnd = scaledLoopEnd;
            Length = scaledLength;
        }

        // Source Stream
        public WaveStream WaveStream { get; private set; }

        // Looped Source Stream
        private readonly LoopingReader LoopedSource;

        // Resampled Stream
        private readonly ISampleProvider OutputSource;

        public WaveFormat WaveFormat => OutputSource.WaveFormat;

        public Dictionary<string, string> Comments { get; }

        public PlaybackState PlaybackState { get; set; } = PlaybackState.Stopped;

        public bool IsLooped { get => LoopedSource.IsLooped; set => LoopedSource.IsLooped = value; }

        #region Source Stream

        public long SourcePosition { get => LoopedSource.Position; set => LoopedSource.Position = value; }

        public long SourceLength { get; }

        public long SourceLoopStart { get; }

        public long SourceLoopEnd { get; }

        #endregion

        #region Resampled Stream

        private readonly float sampleScalar;

        public long Position
        {
            get
            {
                long samplePos = (long)(SourcePosition / sampleScalar);
                samplePos -= samplePos % AudioStandards.ChannelCount;

                return samplePos;
            }

            set
            {
                long samplePos = (long)(value * sampleScalar);
                samplePos -= samplePos % AudioStandards.ChannelCount;

                SourcePosition = samplePos;
            }
        }

        public long Length { get; }

        public long LoopStart { get; }

        public long LoopEnd { get; }

        #endregion

        public void Close()
        {
            WaveStream.Dispose();
        }

        public int Read(float[] buffer, int offset, int count) => OutputSource.Read(buffer, offset, count);
    }
}