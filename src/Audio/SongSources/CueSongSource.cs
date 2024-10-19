using MonoStereo.AudioSources;
using MonoStereo.Encoding;
using MonoStereoMod.Systems;
using NAudio.Wave;
using System.Collections.Generic;

namespace MonoStereoMod.Audio
{
    internal class CueSongSource : ITerrariaSongSource
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

            Comments.ParseLoop(out loopStart, out loopEnd, WaveFormat.Channels);

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

        public long Position
        {
            get => CueReader.Position / CueReader.WaveFormat.BlockAlign * CueReader.WaveFormat.Channels;
            set => CueReader.Position = value / CueReader.WaveFormat.Channels * CueReader.WaveFormat.BlockAlign;
        }

        public WaveFormat WaveFormat => Source.WaveFormat;

        public void Close()
        {
            CueReader.Dispose();
        }

        public int Read(float[] buffer, int offset, int count)
        {
            return Source.LoopedRead(buffer, offset, count, this, IsLooped, Length, LoopStart, LoopEnd);
        }
    }

    // This wrapper just standardizes the WaveBankCue into a WaveStream.
    // Technically it's not even necessary, but it does help to make things a little nicer to read,
    // and follows the format of all other IO readers (Performance reader => File reader => Decoder)
    internal class CueReader(WaveBankCue cue) : WaveStream
    {
        public WaveBankCue Cue = cue;

        public override WaveFormat WaveFormat => Cue.WaveFormat;

        public override long Length => Cue.PcmLength;

        public override long Position { get => Cue.PcmPosition; set => Cue.PcmPosition = value; }

        public override int Read(byte[] buffer, int offset, int count) => Cue.Read(buffer, offset, count);

        protected override void Dispose(bool disposing) => Cue.Dispose();
    }
}
