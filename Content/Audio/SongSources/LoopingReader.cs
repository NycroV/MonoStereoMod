using MonoStereo.Structures;
using NAudio.Wave;
using System;

namespace MonoStereoMod.Audio
{
    /// <summary>
    /// Loops the reading of a provided source dependent on looping tags.
    /// </summary>
    internal class LoopingReader(WaveStream waveStream, long loopStart, long loopEnd) : ISampleProvider, ISeekable, ILoopTags
    {
        public readonly WaveStream WaveSource = waveStream;

        // Some classes implement both WaveStream and ISampleProvider (like the Ogg reader.)
        // We want to avoid as much unnecessary conversion as possible.
        public readonly ISampleProvider OutputSource = waveStream is ISampleProvider output ? output : waveStream.ToSampleProvider();

        public WaveFormat WaveFormat => OutputSource.WaveFormat;

        public long LoopStart { get; set; } = loopStart;

        public long LoopEnd { get; set; } = loopEnd;

        public bool IsLooped { get; set; } = false;

        public long Position
        {
            get => WaveSource.Position / WaveSource.WaveFormat.BlockAlign * WaveSource.WaveFormat.Channels;
            set => WaveSource.Position = value / WaveSource.WaveFormat.Channels * WaveSource.WaveFormat.BlockAlign;
        }

        public long Length => WaveSource.Length / WaveSource.WaveFormat.BlockAlign * WaveSource.WaveFormat.Channels;

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesCopied = 0;

            do
            {
                long endIndex = Length;

                if (IsLooped && LoopEnd != -1 && Position < LoopEnd)
                    endIndex = LoopEnd;

                long samplesAvailable = endIndex - Position;
                long samplesRemaining = count - samplesCopied;

                int samplesToCopy = (int)Math.Min(samplesAvailable, samplesRemaining);

                if (samplesToCopy > 0)
                    samplesCopied += OutputSource.Read(buffer, offset + samplesCopied, samplesToCopy);

                if (IsLooped && Position >= endIndex)
                {
                    long startIndex = Math.Max(0, LoopStart);
                    Position = startIndex;
                }
            }
            while (IsLooped && samplesCopied < count);

            return samplesCopied;
        }
    }
}
