using MonoStereo.AudioSources;
using MonoStereoMod.Systems;
using NAudio.Wave;
using System;
using System.Collections.Generic;

namespace MonoStereoMod.Audio.SongSources
{
    internal class CueSongSource : ISongSource
    {
        public CueSongSource(WaveBankCue cue)
        {
            Cue = cue;
            Source = WaveFormatConversionStream.CreatePcmStream(cue).ToSampleProvider();

            Comments = [];

            if (cue.LoopStart >= 0)
                Comments.Add("LOOPSTART", cue.LoopStart.ToString());

            if (cue.LoopEnd >= 0)
                Comments.Add("LOOPEND", cue.LoopEnd.ToString());

            LoopStart = cue.LoopStart;
            LoopEnd = cue.LoopEnd;
        }

        public WaveBankCue Cue { get; private set; }

        private readonly ISampleProvider Source;

        public PlaybackState PlaybackState { get; set; }

        public Dictionary<string, string> Comments { get; }

        public long Length => Cue.Length / Cue.WaveFormat.BlockAlign;

        public bool IsLooped { get; set; }

        public long LoopStart { get; set; }

        public long LoopEnd { get; set; }

        public long Position { get => Cue.Position / Cue.WaveFormat.BlockAlign; set => Cue.Position = value * Cue.WaveFormat.BlockAlign; }

        public WaveFormat WaveFormat => Cue.WaveFormat;

        public void Close()
        {
            Cue.Dispose();
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
                    samplesCopied += Source.Read(buffer, offset + samplesCopied, samplesToCopy);

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
