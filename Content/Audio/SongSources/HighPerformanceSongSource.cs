using MonoStereo;
using MonoStereo.Sources;
using MonoStereo.Sources.Songs;
using MonoStereo.Structures;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MonoStereoMod.Audio
{
    internal class HighPerformanceSongSource : ITerrariaSongSource, IDisposable
    {
        public HighPerformanceSongSource(ISeekableSongSource source)
        {
            Source = source;
            Audio = new(source);

            IsLooped = source.IsLooped;
            source.IsLooped = false;

            Position = source.Position;
            source.Position = 0;

            if (source is ILoopTags loopTags)
            {
                LoopStart = loopTags.LoopStart;
                LoopEnd = loopTags.LoopEnd;
            }
        }

        private readonly ISeekableSongSource Source;
        public ISongSource BaseSource => Source;

        public SongCache Audio { get; }
        public Dictionary<string, string> Comments => Source.Comments;

        public WaveFormat WaveFormat { get; }
        public PlaybackState PlaybackState { get; set; } = PlaybackState.Stopped;

        public long LoopStart { get; } = -1;
        public long LoopEnd { get; } = -1;
        public bool IsLooped { get; set; }

        public long Position { get => Audio.Position; set => Audio.Position = value; }
        public long Length => Audio.Length;

        public int Read(float[] buffer, int offset, int count)
        {
            if (!Audio.IsLoaded)
                Audio.Load();

            return Audio.LoopedRead(buffer, offset, count, this, IsLooped, Length, LoopStart, LoopEnd);
        }

        public void OnRemoveInput() => Audio.Unload();

        public void Dispose() => Audio.Dispose();

        public class SongCache(ISeekableSongSource source) : ISampleProvider, ISeekable, IDisposable
        {
            public float[] AudioData = null;

            public ISeekableSongSource Source = source;

            public WaveFormat WaveFormat { get; } = source.WaveFormat;

            public long Length { get; } = source.Length;

            public long Position { get; set; } = 0L;

            private long SamplesCached = 0;

            private int WriteIndex = 0;

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
                Source.Position = Position;
                AudioData = new float[Source.Length];

                // Read one second of data into memory right now.
                // We will offload the rest of the reading to a separate thread so that playback
                // isn't halted by this expensive IO operation.
                SamplesCached = Source.Read(AudioData, (int)Position, (int)Math.Min(AudioStandards.SampleRate * AudioStandards.ChannelCount, Length - Position));
                WriteIndex = (int)(Position + SamplesCached);

                IsLoaded = true;

                ThreadPool.QueueUserWorkItem(new(sender =>
                {
                    // The song cache is passed to this WaitCallback.
                    SongCache audio = sender as SongCache;
                    ISeekableSongSource source = audio.Source;

                    while (audio.SamplesCached < audio.Length)
                    {
                        // Attempt to cache 5 seconds at a time...
                        int count = AudioStandards.SampleRate * AudioStandards.ChannelCount * 5;

                        // But only cache enough samples to reach the end of the stream...
                        int samplesAvailable = (int)Math.Min(count, audio.Length - audio.WriteIndex);

                        // And even less than that if we've already cached everything else.
                        int samplesToCache = (int)Math.Min(samplesAvailable, audio.Length - audio.SamplesCached);

                        // Read the data to the cache buffer.
                        int cached = source.Read(audio.AudioData, audio.WriteIndex, samplesToCache);
                        audio.SamplesCached += cached;
                        audio.WriteIndex += cached;

                        // If we reached the end of the stream, circle back to the beginning to cache
                        // any data that we might have skipped over by starting the read halfway through.
                        if (audio.WriteIndex == audio.Length)
                        {
                            audio.WriteIndex = 0;
                            source.Position = 0;
                        }
                    }
                }),
                this);
            }

            public void Unload()
            {
                IsLoaded = false;
                AudioData = null;
                SamplesCached = 0;
                WriteIndex = 0;
            }

            public void Dispose()
            {
                Unload();
                Source.Dispose();
            }
        }
    }
}
