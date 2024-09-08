using MonoStereo.AudioSources;
using MonoStereoMod.Systems;
using NAudio.Wave;
using System;
using System.Collections.Generic;

namespace MonoStereoMod.Audio
{
    internal class CueSongSource : ISongSource
    {
        public CueSongSource(CueReader cue)
        {
            CueReader = cue;

            long loopStart = cue.Cue.LoopStart;
            long loopEnd = cue.Cue.LoopEnd;

            Source = WaveFormatConversionStream.CreatePcmStream(CueReader).ToSampleProvider().Reformat(ref loopStart, ref loopEnd);
            Comments = [];

            if (loopStart >= 0)
                Comments.Add("LOOPSTART", loopStart.ToString());

            if (loopEnd >= 0)
                Comments.Add("LOOPEND", loopEnd.ToString());

            LoopStart = loopStart;
            LoopEnd = loopEnd;
        }

        public CueReader CueReader { get; private set; }

        private readonly ISampleProvider Source;

        public PlaybackState PlaybackState { get; set; } = PlaybackState.Stopped;

        public Dictionary<string, string> Comments { get; }

        public long Length => CueReader.Length / CueReader.WaveFormat.BlockAlign * CueReader.WaveFormat.Channels;

        public bool IsLooped { get; set; }

        public long LoopStart { get; set; }

        public long LoopEnd { get; set; }

        public long Position {
            get => CueReader.Position / CueReader.WaveFormat.BlockAlign * CueReader.WaveFormat.Channels;
            set => CueReader.Position = value / WaveFormat.Channels * CueReader.WaveFormat.BlockAlign;
        }

        public WaveFormat WaveFormat => CueReader.WaveFormat;

        public void OnStop()
        {
            CueReader.Cue.Unload();
        }

        public void Close()
        {
            CueReader.Dispose();
        }

        public int Read(float[] buffer, int offset, int count)
        {
            if (!CueReader.Cue.IsLoaded)
                CueReader.Cue.Load();

            return Source.LoopedRead(buffer, offset, count, this, IsLooped, Length, LoopStart, LoopEnd);
        }
    }

    internal class CueReader(WaveBankCue cue) : WaveStream
    {
        public WaveBankCue Cue = cue;

        public override WaveFormat WaveFormat => Cue.WaveFormat;

        public override long Length => Cue.Length;

        public override long Position { get; set; } = 0L;

        public override int Read(byte[] buffer, int offset, int count)
        {
            long samplesAvailable = Length - Position;
            int samplesToCopy = Math.Min((int)samplesAvailable, count);
            Array.Copy(Cue.Buffer, Position, buffer, offset, samplesToCopy);

            Position += samplesToCopy;

            return samplesToCopy;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Cue.Unload();
            Cue.Dispose();
        }
    }
}
