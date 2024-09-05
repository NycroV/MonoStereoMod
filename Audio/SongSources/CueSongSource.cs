using MonoStereo.AudioSources;
using MonoStereoMod.Systems;
using NAudio.Wave;
using System.Collections.Generic;

namespace MonoStereoMod.Audio.SongSources
{
    internal class CueSongSource : ISongSource
    {
        public CueSongSource(WaveBankCue cue)
        {
            Cue = cue;

            long loopStart = cue.LoopStart;
            long loopEnd = cue.LoopEnd;

            Source = WaveFormatConversionStream.CreatePcmStream(cue).ToSampleProvider().Reformat(ref loopStart, ref loopEnd);

            Comments = [];

            if (loopStart >= 0)
                Comments.Add("LOOPSTART", loopStart.ToString());

            if (loopEnd >= 0)
                Comments.Add("LOOPEND", loopEnd.ToString());

            LoopStart = loopStart;
            LoopEnd = loopEnd;
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

        public int Read(float[] buffer, int offset, int count) => Source.LoopedRead(buffer, offset, count, this, IsLooped, Length, LoopStart, LoopEnd);
    }
}
