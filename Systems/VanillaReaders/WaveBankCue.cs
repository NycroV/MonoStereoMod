﻿using NAudio.Wave;
using System;
using System.IO;

namespace MonoStereoMod.Systems
{
    internal class WaveBankCue : IDisposable
    {
        public readonly string Name;

        private readonly BinaryReader Reader;

        public bool IsLoaded;

        private readonly bool Adpcm;

        internal byte[] Buffer;

        private readonly WaveFormat SourceFormat;

        public WaveFormat WaveFormat { get; }

        private readonly long SourceLength;

        public long Length { get; private set; }

        private readonly long Offset;

        public long LoopStart { get; }

        public long LoopEnd { get; }

        public WaveBankCue(BinaryReader reader, WaveFormat waveFormat, string name, long offset, long length, long loopStart, long loopEnd)
        {
            Name = name;
            Reader = reader;
            Offset = offset;
            SourceLength = length;
            LoopStart = loopStart;
            LoopEnd = loopEnd;
            IsLoaded = false;

            SourceFormat = waveFormat;
            WaveFormat = waveFormat;

            if (waveFormat.Encoding == WaveFormatEncoding.Adpcm)
            {
                // 16 bits or 2 byte samples
                int blockAlign = 2 * waveFormat.Channels;
                WaveFormat = WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm, waveFormat.SampleRate, waveFormat.Channels, waveFormat.SampleRate * blockAlign, blockAlign, 16);
                Adpcm = true;
            }
        }

        internal void Load()
        {
            Reader.BaseStream.Seek(Offset, SeekOrigin.Begin);
            byte[] buffer = Reader.ReadBytes((int)SourceLength);

            if (Adpcm)
                buffer = ConvertMsAdpcmToPcm(buffer, 0, buffer.Length, WaveFormat.Channels, (SourceFormat.BlockAlign + 22) * WaveFormat.Channels);

            Length = buffer.LongLength;
            Buffer = buffer;
            IsLoaded = true;
        }

        internal void Unload()
        {
            Buffer = null;
            IsLoaded = false;
        }

        public void Dispose()
        {
            Reader.Close();
            Buffer = null;
        }
    }
}
