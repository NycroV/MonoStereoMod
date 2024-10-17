using MonoStereo.AudioSources;
using MonoStereo.SampleProviders;
using NAudio.Wave;
using System;
using System.Collections.Generic;

namespace MonoStereoMod.Audio
{
    internal class HighPerformanceSongSource : ITerrariaSongSource
    {
        public HighPerformanceSongSource(ITerrariaSongSource source)
        {
            Source = source;

            IsLooped = source.IsLooped;
            source.IsLooped = false;

            Position = source.Position;
            source.Position = 0;

            LoopStart = source.LoopStart;
            LoopEnd = source.LoopEnd;

            Audio = new(source);
        }

        private readonly ITerrariaSongSource Source;
        public ISongSource BaseSource => Source;

        public long LoopStart { get; }
        public long LoopEnd { get; }
        public PlaybackState PlaybackState { get; set; } = PlaybackState.Stopped;

        public SongCache Audio { get; }
        public Dictionary<string, string> Comments => Source.Comments;

        public bool IsLooped { get; set; }
        public long Position { get => Audio.Position; set => Audio.Position = value; }

        public long Length => Audio.Length;
        public WaveFormat WaveFormat { get; }

        public void Close() => Audio.Dispose();

        public void OnStop() => Audio.Unload();

        public int Read(float[] buffer, int offset, int count)
        {
            if (!Audio.IsLoaded)
                Audio.Load();

            return Audio.LoopedRead(buffer, offset, count, this, IsLooped, Length, LoopStart, LoopEnd);
        }

        public class SongCache(ITerrariaSongSource source) : ISampleProvider, ISeekable
        {
            public float[] AudioData = null;

            public ITerrariaSongSource Source = source;

            public WaveFormat WaveFormat { get; } = source.WaveFormat;

            public long Length { get; } = source.Length;

            public long Position { get; set; } = 0L;

            public bool IsLoaded { get; set; } = false;

            public int Read(float[] buffer, int offset, int count)
            {
                long samplesAvailable = Length - Position;
                int samplesToCopy = Math.Min((int)samplesAvailable, count);
                Array.Copy(AudioData, Position, buffer, offset, samplesToCopy);

                Position += samplesToCopy;

                return samplesToCopy;
            }

            public void Load()
            {
                Source.Position = 0;
                AudioData = new float[Source.Length];
                Source.Read(AudioData, 0, AudioData.Length);
            }

            public void Unload() => AudioData = null;

            public void Dispose()
            {
                Unload();
                Source.Close();
            }
        }
    }
}
