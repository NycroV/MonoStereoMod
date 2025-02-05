using MonoStereo;
using MonoStereo.Sources;
using MonoStereo.Structures;
using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MonoStereoMod.Audio
{
    internal class TerrariaCachedSoundEffectReader : ISoundEffectSource, ILoopTags
    {
        #region Metadata

        public string FileName { get => CachedSoundEffect.FileName; }

        public WaveFormat WaveFormat => CachedSoundEffect.WaveFormat;

        public Dictionary<string, string> Comments { get; private set; }

        #endregion

        #region Playback fields

        public CachedSoundEffect CachedSoundEffect { get; private set; }

        public PlaybackState PlaybackState { get; set; } = PlaybackState.Playing;

        #endregion

        #region Play region

        public long Length => CachedSoundEffect.AudioData.Length;

        public long Position { get; set; } = 0L;

        public long LoopStart { get => CachedSoundEffect.LoopStart; }

        public long LoopEnd { get => CachedSoundEffect.LoopEnd; }

        public bool IsLooped { get; set; } = false;

        #endregion

        private void Load(CachedSoundEffect sound)
        {
            Comments = sound.Comments.ToDictionary();
            CachedSoundEffect = sound;
        }

        // Loads the sound synchronously
        public TerrariaCachedSoundEffectReader(CachedSoundEffect cachedSound)
        {
            Load(cachedSound);
        }

        // Loads the sound asynchronously
        public TerrariaCachedSoundEffectReader(Func<CachedSoundEffect> generatorFunc)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(sender =>
            {
                var creation = sender as object[];

                var sound = creation[0] as TerrariaCachedSoundEffectReader;
                var cachedSound = (creation[1] as Func<CachedSoundEffect>)();

                sound.Load(cachedSound);
            }),
            new object[] { this, generatorFunc });
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesCopied = 0;

            do
            {
                long endIndex = Length;

                if (IsLooped && LoopEnd != -1 && Position < LoopEnd)
                    endIndex = LoopEnd;

                long samplesAvailable = endIndex - Position;
                long samplesRemaining = count - samplesCopied;

                int samplesToCopy = (int)Math.Min(samplesAvailable, samplesRemaining);
                if (samplesToCopy > 0)
                {
                    Array.Copy(CachedSoundEffect.AudioData, Position, buffer, offset + samplesCopied, samplesToCopy);
                    samplesCopied += samplesToCopy;
                    Position += samplesToCopy;
                }

                if (IsLooped && Position >= endIndex)
                {
                    long startIndex = Math.Max(0, LoopStart);
                    Position = startIndex;
                }
            }
            while (IsLooped && samplesCopied < count);

            return samplesCopied;
        }

        public void Close() { }
    }
}
