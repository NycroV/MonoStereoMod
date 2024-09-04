using MonoStereo;
using MonoStereo.AudioSources;
using MonoStereo.Encoding;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;

namespace MonoStereoMod.Audio.Reading
{
    internal class Mp3SongSource : ISongSource
    {
        internal readonly Mp3FileReader reader;

        internal readonly ISampleProvider provider;

        internal Mp3SongSource(Stream stream, string fileName)
        {
            FileName = fileName;

            Comments = stream.ReadComments();
            Comments.ParseLoop(out long loopStart, out long loopEnd);

            LoopStart = loopStart;
            LoopEnd = loopEnd;

            reader = new(stream);

            ISampleProvider sampleProvider = reader.ConvertWaveProviderIntoSampleProvider();

            if (WaveFormat.SampleRate != AudioStandards.SampleRate)
            {
                float scalar = AudioStandards.SampleRate / (float)sampleProvider.WaveFormat.SampleRate;

                LoopStart = LoopStart <= 0 ? LoopStart : (long)(LoopStart * scalar);
                LoopEnd = LoopEnd <= 0 ? LoopEnd : (long)(LoopEnd * scalar);

                LoopStart = LoopStart <= 0 ? LoopStart : LoopStart - (LoopStart % sampleProvider.WaveFormat.Channels);
                LoopEnd = LoopEnd <= 0 ? LoopEnd : LoopEnd - (LoopEnd % sampleProvider.WaveFormat.Channels);
            }

            if (WaveFormat.Channels != AudioStandards.ChannelCount)
            {
                if (WaveFormat.Channels == 1)
                    sampleProvider = new MonoToStereoSampleProvider(sampleProvider);

                else
                    throw new ArgumentException("Song file must be in either mono or stereo!", fileName);
            }

            LoopStart = LoopStart <= 0 ? LoopStart : LoopStart * WaveFormat.Channels;
            LoopEnd = LoopEnd <= 0 ? LoopEnd : LoopEnd * WaveFormat.Channels;
            provider = sampleProvider;
        }

        public string FileName { get; }

        public PlaybackState PlaybackState { get; set; } = PlaybackState.Stopped;

        public Dictionary<string, string> Comments { get; }

        public long Length => reader.Length;

        public bool IsLooped { get; set; } = false;

        public long LoopStart { get; set; }

        public long LoopEnd { get; set; }

        public long Position { get => reader.Position; set => reader.Position = value; }

        public WaveFormat WaveFormat => provider.WaveFormat;

        public void Close()
        {
            reader.Dispose();
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
                    samplesCopied += provider.Read(buffer, offset + samplesCopied, samplesToCopy);

                if (IsLooped && Position == endIndex)
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
