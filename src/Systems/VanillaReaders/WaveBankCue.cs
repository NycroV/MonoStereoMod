using MonoStereo;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;

namespace MonoStereoMod.Systems
{
    // Provides a way to read data from an XNA WaveBank file.
    internal class WaveBankCue : IDisposable
    {
        #region Metadata

        public readonly string Name;

        // Public facing wave format is different from source.
        public WaveFormat WaveFormat { get; }

        // This is because source uses Adpcm encoding, but we supply the public with
        // decoded pcm data.
        private readonly WaveFormat SourceFormat;

        private readonly BinaryReader Reader;

        #endregion

        #region Play region

        // Cached for performance.
        public long PcmLength { get; private set; }

        // Adpcm is a mess to read.
        // I can tell you what's happening, but have not a clue about
        // what's going on with the encoding constants.
        public long PcmPosition
        {
            get
            {
                // The leftoverCount is the number of bytes we've read from a block
                // that were left over for a given read and have been queued for the next read.
                long byteCount = 0;
                long leftoverCount = 0;

                SeekLock.Execute(() =>
                {
                    byteCount = SourcePosition;
                    leftoverCount = BlockLeftovers.Count;
                });

                int channels = SourceFormat.Channels;
                int blockAlignment = (SourceFormat.BlockAlign + 22) * channels;

                // The number of sample frames (a set of samples, with one for each playback channel) in a full Adpcm block.
                int frameCountFullBlock = ((blockAlignment / channels) - 7) * 2 + 2;
                long frameCountLastBlock = 0;

                // This should never be true, but it's here just in case.
                if ((byteCount % blockAlignment) > 0)
                    frameCountLastBlock = ((byteCount % blockAlignment / channels) - 7) * 2 + 2;

                // Convert sample frames to raw byte count.
                long frameCount = (byteCount / blockAlignment * frameCountFullBlock) + frameCountLastBlock;
                return frameCount * sizeof(short) * channels - leftoverCount;
            }

            set
            {
                int channels = SourceFormat.Channels;
                int blockAlignment = (SourceFormat.BlockAlign + 22) * channels;

                // The number of sample frames (a set of samples, with one for each playback channel) in a full Adpcm block.
                int frameCountFullBlock = ((blockAlignment / channels) - 7) * 2 + 2;

                int frameCount = (int)value / channels / sizeof(short);
                int blocks = (int)Math.Floor((float)frameCount / frameCountFullBlock);

                // We seek to the beginning of the block where we should end up, as we will need
                // to queue any bytes that are within the next block, but past our seek point.
                int seekPosition = blocks * blockAlignment;
                long seekedFrames = blocks * (long)frameCountFullBlock;

                SeekLock.Execute(() =>
                {
                    SourcePosition = seekPosition;
                    BlockLeftovers.Clear();

                    if (seekedFrames - value == 0L)
                        return;

                    int bytesToRead = (int)Math.Min(blockAlignment, SourceLength - SourcePosition);
                    int bytesToDiscard = (int)(value - seekedFrames);

                    byte[] source = Reader.ReadBytes(bytesToRead);
                    byte[] decoded = ConvertMsAdpcmToPcm(source, channels, blockAlignment);

                    // Queue extra bytes from the read block.
                    for (int i = bytesToDiscard; i < decoded.Length; i++)
                        BlockLeftovers.Enqueue(decoded[i]);
                });
            }
        }

        private readonly long SourceLength;

        private readonly long SourceOffset;

        private long SourcePosition
        {
            get => Reader.BaseStream.Position - SourceOffset;
            set => Reader.BaseStream.Position = value + SourceOffset;
        }

        public long LoopStart { get; }

        public long LoopEnd { get; }

        private readonly Queue<byte> BlockLeftovers = [];

        private readonly QueuedLock SeekLock = new();

        #endregion

        public WaveBankCue(BinaryReader reader, WaveFormat waveFormat, string name, long offset, long sourceLength, long sampleLength, long loopStart, long loopEnd)
        {
            Name = name;
            Reader = reader;
            SourceOffset = offset;
            SourceLength = sourceLength;
            LoopStart = loopStart;
            LoopEnd = loopEnd;

            // 2 byte samples (16 bit pcm)
            // We perform the conversion from Adpcm to Pcm in real time.
            int blockAlign = 2 * waveFormat.Channels;
            WaveFormat = WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm, waveFormat.SampleRate, waveFormat.Channels, waveFormat.SampleRate * blockAlign, blockAlign, blockAlign * 8 / waveFormat.Channels);
            SourceFormat = waveFormat;

            PcmLength = sampleLength * WaveFormat.BlockAlign;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            // Adpcm data must be read in blocks at a time.
            // Oftentimes, the block size does not line up with how many bytes we actually need.
            // We get around this by rounding the amount of blocks we need up, and then queueing
            // the extra bytes for the next read.
            List<byte> samples = [];

            SeekLock.Execute(() =>
            {
                // Check for leftover bytes from the last block
                while (BlockLeftovers.TryDequeue(out byte sample) && samples.Count < count)
                    samples.Add(sample);

                int countRemaining = count - samples.Count;

                // If there were enough bytes in the last block, we don't need to read any more.
                if (countRemaining == 0)
                    return;

                int channels = SourceFormat.Channels;
                int blockAlignment = (SourceFormat.BlockAlign + 22) * channels;

                // The number of sample frames (a set of samples, with one for each playback channel) in a full Adpcm block.
                int frameCountFullBlock = ((blockAlignment / channels) - 7) * 2 + 2;

                int frameCount = countRemaining / channels / sizeof(short);
                int byteCount = (int)Math.Ceiling((float)frameCount / frameCountFullBlock) * blockAlignment;

                int bytesToRead = Math.Min(byteCount, (int)(SourceLength - SourcePosition));
                byte[] sourceBytes = Reader.ReadBytes(bytesToRead);

                byte[] decoded = ConvertMsAdpcmToPcm(sourceBytes, channels, blockAlignment);

                for (int i = 0; i < decoded.Length; i++)
                {
                    // Only copy over the amount we need.
                    if (samples.Count < count)
                        samples.Add(decoded[i]);

                    // Anything extra goes into the queue for the next read.
                    else
                        BlockLeftovers.Enqueue(decoded[i]);
                }

                samples.CopyTo(buffer, offset);
            });

            return samples.Count;
        }

        public void Dispose()
        {
            Reader.Close();
        }
    }
}
