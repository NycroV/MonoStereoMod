﻿using MonoStereo.Encoding;
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
            destBytesPerSample = 4 * source.WaveFormat.Channels;

            Length = SourceToDest(readerStream.Length);
            source = readerStream.ConvertWaveProviderIntoSampleProvider();

            if (source.WaveFormat.SampleRate != AudioStandards.SampleRate)
                throw new ArgumentException("Song file must have a 44.1kHz sample rate!", fileName);

            if (source.WaveFormat.Channels != AudioStandards.ChannelCount)
            {
                if (WaveFormat.Channels == 1)
                {
                    source = new MonoToStereoSampleProvider(source);
                    LoopStart = LoopStart <= 0 ? LoopStart : LoopStart * 2;
                    LoopEnd = LoopEnd <= 0 ? LoopEnd : LoopEnd * 2;
                }

                else
                    throw new ArgumentException("Song file must be in either mono or stereo!", fileName);
            }
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

        public WaveFormat WaveFormat => source.WaveFormat;

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
                    samplesCopied += source.Read(buffer, offset + samplesCopied, samplesToCopy);

                if (IsLooped && Position == endIndex)
                {
                    long startIndex = Math.Max(0, LoopStart);
                    Position = startIndex;
                }
            }
            while (IsLooped && samplesCopied < count);

            return samplesCopied;
        }

        readonly ISampleProvider source;
        readonly int destBytesPerSample;
        readonly int sourceBytesPerSample;

        private long SourceToDest(long sourceBytes) => destBytesPerSample * (sourceBytes / sourceBytesPerSample);        
        private long DestToSource(long destBytes) => sourceBytesPerSample * (destBytes / destBytesPerSample);
    }
}
