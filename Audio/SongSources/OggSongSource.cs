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
    internal class OggSongSource : ISongSource
    {
        public string FileName { get; private set; }

        internal OggReader OggReader { get; private set; }

        internal ISampleProvider Provider { get; private set; }

        public Dictionary<string, string> Comments { get; private set; }

        public WaveFormat WaveFormat { get => Provider.WaveFormat; }

        public PlaybackState PlaybackState { get; set; } = PlaybackState.Playing;

        public long Length => OggReader.Length;

        public long Position
        {
            get => OggReader.Position;
            set => OggReader.Position = value;
        }

        public long LoopStart { get; set; }

        public long LoopEnd { get; set; }

        public bool IsLooped { get; set; } = true;

        internal OggSongSource(Stream stream, string fileName)
        {
            FileName = fileName;

            OggReader = new(stream);
            Provider = OggReader;

            Comments = OggReader.Comments.ComposeComments();
            Comments.ParseLoop(out long loopStart, out long loopEnd);

            LoopStart = loopStart;
            LoopEnd = loopEnd;

            if (WaveFormat.SampleRate != AudioStandards.SampleRate)
                throw new ArgumentException("Song file must have a 44.1kHz sample rate!", fileName);

            if (WaveFormat.Channels != AudioStandards.ChannelCount)
            {
                if (WaveFormat.Channels == 1)
                {
                    Provider = new MonoToStereoSampleProvider(Provider);
                    LoopStart = LoopStart <= 0 ? LoopStart : LoopStart * 2;
                    LoopEnd = LoopEnd <= 0 ? LoopEnd : LoopEnd * 2;
                }

                else
                    throw new ArgumentException("Song file must be in either mono or stereo!", fileName);
            }
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
                    samplesCopied += OggReader.Read(buffer, offset + samplesCopied, samplesToCopy);

                if (IsLooped && Position == endIndex)
                {
                    long startIndex = Math.Max(0, LoopStart);
                    Position = startIndex;
                }
            }
            while (IsLooped && samplesCopied < count);

            return samplesCopied;
        }

        public void Close() => OggReader.Dispose();
    }
}
