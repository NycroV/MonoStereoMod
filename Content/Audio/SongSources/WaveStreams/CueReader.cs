using MonoStereoMod.Systems;
using NAudio.Wave;

namespace MonoStereoMod.Audio
{
    // This wrapper just standardizes the WaveBankCue into a WaveStream.
    // Technically it's not even necessary, but it does help to make things a little nicer to read,
    // and follows the format of all other IO readers (Performance reader => Standardized file reader => Decoder)
    internal class CueReader(WaveBankCue cue) : WaveStream
    {
        public WaveBankCue Cue = cue;

        public override WaveFormat WaveFormat => Cue.WaveFormat;

        public override long Length => Cue.PcmLength;

        public override long Position
        {
            get => Cue.PcmPosition;
            set => Cue.PcmPosition = value;
        }

        public override int Read(byte[] buffer, int offset, int count) => Cue.Read(buffer, offset, count);

        protected override void Dispose(bool disposing) => Cue.Dispose();
    }
}
