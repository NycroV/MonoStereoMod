using NAudio.Wave;
using System.IO;

namespace MonoStereoMod.Systems
{
    internal class WaveBankCue(BinaryReader reader, WaveFormat waveFormat, string name, long offset, long length, long loopStart, long loopEnd) : WaveStream
    {
        public readonly string Name = name;

        private readonly BinaryReader Reader = reader;

        public override WaveFormat WaveFormat { get; } = waveFormat;

        private readonly long Offset = offset;

        public override long Length { get; } = length;

        public override long Position { get => Reader.BaseStream.Position - Offset; set => Reader.BaseStream.Position = value + Offset; }

        public long LoopStart { get; } = loopStart;

        public long LoopEnd { get; } = loopEnd;

        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                return Reader.Read(buffer, offset, count);
            }
            catch (EndOfStreamException)
            {
                return 0;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Reader.Close();
        }
    }
}
