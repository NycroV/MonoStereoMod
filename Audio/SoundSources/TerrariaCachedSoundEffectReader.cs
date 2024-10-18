using MonoStereo;
using MonoStereo.AudioSources;
using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MonoStereoMod.Audio
{
    internal class TerrariaCachedSoundEffectReader : ISoundEffectSource
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

        public long Position
        {
            get
            {
                long pos = (long)(effectivePosition / safeScalar);
                return pos + (pos % AudioStandards.ChannelCount);
            }
            set
            {
                long pos = (long)(value * safeScalar);
                effectivePosition = pos - (pos % AudioStandards.ChannelCount);
            }
        }

        public long LoopStart { get => CachedSoundEffect.LoopStart; }

        public long LoopEnd { get => CachedSoundEffect.LoopEnd; }

        public bool IsLooped { get; set; } = false;

        #endregion

        // Although live resampling is an option, for some sounds it can create "buzzing"
        // in between buffer reads. While the source of this buzzing is under investigation,
        // we can get around this issue by resampling the entire buffer at once, and then
        // reading from the resampled buffer like normal.
        //
        // This *should* NOT be an option for songs, due to how long their sample arrays are.

        #region Resampling

        public float EffectivePitch
        {
            get => safeScalar;
            set
            {
                // Check for if the load is complete only once.
                // After that, we can directly access the underlying scalar.
                if (safeScalar == value)
                    return;

                long pos = Position;

                if (value == 1f)
                {
                    scalar = 1f;
                    resampledData = CachedSoundEffect.AudioData;

                    effectiveLength = Length;
                    Position = pos;

                    effectiveLoopStart = LoopStart;
                    effectiveLoopEnd = LoopEnd;
                }

                scalar = value;
                WdlResampler resampler = new();

                resampler.SetMode(true, 2, false);
                resampler.SetFilterParms();
                resampler.SetFeedMode(false); // output driven
                resampler.SetRates(AudioStandards.SampleRate, AudioStandards.SampleRate / scalar);

                int count = (int)(CachedSoundEffect.AudioData.Length / scalar) + 2;
                count -= count % AudioStandards.ChannelCount;

                int framesRequested = count / AudioStandards.ChannelCount;
                int inNeeded = resampler.ResamplePrepare(framesRequested, AudioStandards.ChannelCount, out float[] inBuffer, out int inBufferOffset);
                int samplesToCopy = Math.Min(inNeeded * AudioStandards.ChannelCount, CachedSoundEffect.AudioData.Length);

                float[] buffer = new float[count];
                Array.Copy(CachedSoundEffect.AudioData, 0, inBuffer, inBufferOffset, samplesToCopy);
                resampler.ResampleOut(buffer, 0, samplesToCopy / AudioStandards.ChannelCount, framesRequested, AudioStandards.ChannelCount);

                resampledData = buffer;
                effectiveLength = resampledData.Length;
                Position = pos;

                effectiveLoopStart = (long)(LoopStart / scalar);
                effectiveLoopStart -= effectiveLoopStart % AudioStandards.ChannelCount;

                effectiveLoopEnd = (long)(LoopEnd / scalar);
                effectiveLoopEnd += effectiveLoopEnd % AudioStandards.ChannelCount;

                // Sometimes rounding results in 0 or -2.
                // This misalignment isn't an issue when the looping tags are actually implemented.

                if (effectiveLoopStart <= 0)
                    effectiveLoopStart = -1;

                if (effectiveLoopEnd <= 0)
                    effectiveLoopEnd = -1;
            }
        }

        // This allows for asynchronous loading if possible,
        // but still allows synchronous access if it is required.
        private readonly TaskCompletionSource readySource = new();

        private float scalar = 1f;
        private float safeScalar
        {
            get
            {
                readySource.Task.GetAwaiter().GetResult();
                return scalar;
            }

            set
            {
                readySource.Task.GetAwaiter().GetResult();
                scalar = value;
            }
        }

        private long effectiveLength;

        private long effectivePosition = 0;

        private long effectiveLoopStart;

        private long effectiveLoopEnd;

        private float[] resampledData;

        #endregion

        private void Load(CachedSoundEffect sound)
        {
            Comments = sound.Comments.ToDictionary();
            CachedSoundEffect = sound;

            effectiveLength = sound.AudioData.Length;
            effectiveLoopStart = sound.LoopStart;
            effectiveLoopEnd = sound.LoopEnd;
            resampledData = sound.AudioData;

            readySource.SetResult();
        }

        // Loads the sound synchronously
        public TerrariaCachedSoundEffectReader(CachedSoundEffect cachedSound)
        {
            Load(cachedSound);
        }

        // Loads the sound asynchronously
        internal TerrariaCachedSoundEffectReader(Func<CachedSoundEffect> generatorFunc)
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
            // If the sound hasn't finished loading yet, fill the buffer with silence.
            // It's better to delay the playback by a few milliseconds than it is to hear stuttering.
            //
            // Often times, even if the load is asynchronous, it will still finish before the first read call,
            // so this code is typically very unlikely to run.
            if (!readySource.Task.IsCompleted)
            {
                for (int i = 0; i < count; i++)
                    buffer[offset + i] = 0;

                return count;
            }

            int samplesCopied = 0;

            do
            {
                long endIndex = effectiveLength;

                if (IsLooped && effectiveLoopEnd != -1)
                    endIndex = effectiveLoopEnd;

                long samplesAvailable = endIndex - effectivePosition;
                long samplesRemaining = count - samplesCopied;

                int samplesToCopy = (int)Math.Min(samplesAvailable, samplesRemaining);
                if (samplesToCopy > 0)
                {
                    Array.Copy(resampledData, effectivePosition, buffer, offset + samplesCopied, samplesToCopy);
                    samplesCopied += samplesToCopy;
                    effectivePosition += samplesToCopy;
                }

                if (IsLooped && effectivePosition == endIndex)
                {
                    long startIndex = Math.Max(0, effectiveLoopStart);
                    effectivePosition = startIndex;
                }
            }
            while (IsLooped && samplesCopied < count);

            return samplesCopied;
        }

        public void Close() { }
    }
}
