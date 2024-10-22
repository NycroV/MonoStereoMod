using MonoStereo;
using MonoStereo.Sources.Songs;
using MonoStereo.Decoding;
using NAudio.Wave;
using System.Collections.Generic;
using System.IO;
using XPT.Core.Audio.MP3Sharp;
using MonoStereoMod.Utils;

namespace MonoStereoMod.Audio
{
    // Standardizes the Mp3Stream into a WaveStream.
    internal class Mp3Reader : WaveStream
    {
        public Mp3Reader(Stream baseStream)
        {
            Stream = new(baseStream);

            // Mp3 reader will always be 16 bit stereo.
            WaveFormat = WaveFormat.CreateCustomFormat(
                WaveFormatEncoding.Pcm,
                Stream.Frequency,
                AudioStandards.ChannelCount,
                Stream.Frequency / 1000 * sizeof(short) * AudioStandards.ChannelCount,
                sizeof(short) * AudioStandards.ChannelCount,
                sizeof(short) * 8);
        }

        private readonly MP3Stream Stream;

        public override WaveFormat WaveFormat { get; }

        public override long Length => Stream.Length;

        public override long Position { get => Stream.Position; set => Stream.Position = value; }

        public override int Read(byte[] buffer, int offset, int count) => Stream.Read(buffer, offset, count);

        protected override void Dispose(bool disposing) => Stream.Dispose();
    }
}
