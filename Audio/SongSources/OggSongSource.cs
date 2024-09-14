﻿using MonoStereo;
using MonoStereo.AudioSources;
using MonoStereo.Encoding;
using NAudio.Wave;
using System.Collections.Generic;
using System.IO;

namespace MonoStereoMod.Audio.Reading
{
    internal class OggSongSource : ITerrariaSongSource
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
                    OggReader.Position = value;
            }
        }

        public long LoopStart { get; set; }

        public long LoopEnd { get; set; }

        public bool IsLooped { get; set; } = true;

        internal OggSongSource(Stream stream, string fileName)
        {
            FileName = fileName;

            OggReader = new(stream, true);
            Comments = OggReader.Comments.ComposeComments();
            Comments.ParseLoop(out long loopStart, out long loopEnd, AudioStandards.ChannelCount);

            Provider = OggReader.Reformat(ref loopStart, ref loopEnd);
            LoopStart = loopStart;
            LoopEnd = loopEnd;
        }

        public int Read(float[] buffer, int offset, int count) => Provider.LoopedRead(buffer, offset, count, this, IsLooped, Length, LoopStart, LoopEnd);

        public void Close() => OggReader.Dispose();
    }
}
