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

        public PlaybackState PlaybackState { get; set; } = PlaybackState.Stopped;

        public long Length => OggReader.Length;

        public long Position
        {
            get => OggReader.Position;
            set
            {
                if (value != OggReader.Position)
                    OggReader.Seek(value, SeekOrigin.Begin);
            }
        }

        public long LoopStart { get; set; }

        public long LoopEnd { get; set; }

        public bool IsLooped { get; set; } = false;

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
            {
                Provider = new WdlResamplingSampleProvider(Provider, AudioStandards.SampleRate);
                float scalar = AudioStandards.SampleRate / (float)Provider.WaveFormat.SampleRate;

                LoopStart = LoopStart <= 0 ? LoopStart : (long)(LoopStart * scalar);
                LoopEnd = LoopEnd <= 0 ? LoopEnd : (long)(LoopEnd * scalar);

                LoopStart = LoopStart <= 0 ? LoopStart : LoopStart - (LoopStart % Provider.WaveFormat.Channels);
                LoopEnd = LoopEnd <= 0 ? LoopEnd : LoopEnd - (LoopEnd % Provider.WaveFormat.Channels);
            }

            if (WaveFormat.Channels != AudioStandards.ChannelCount)
            {
                if (WaveFormat.Channels == 1)
                    Provider = new MonoToStereoSampleProvider(Provider);

                else
                    throw new ArgumentException("Song file must be in either mono or stereo!", fileName);
            }

            LoopStart = LoopStart <= 0 ? LoopStart : LoopStart * WaveFormat.Channels;
            LoopEnd = LoopEnd <= 0 ? LoopEnd : LoopEnd * WaveFormat.Channels;
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
                if (samplesToCopy % OggReader.WaveFormat.Channels != 0)
                    samplesToCopy--;

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
