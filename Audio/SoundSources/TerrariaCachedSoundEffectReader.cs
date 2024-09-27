using MonoStereo;
using MonoStereo.AudioSources;
using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MonoStereoMod.Audio.Structures
{
    internal class TerrariaCachedSoundEffectReader(CachedSoundEffect cachedSound) : ISoundEffectSource
    {
        #region Metadata

        public string FileName { get => CachedSoundEffect.FileName; }

        public WaveFormat WaveFormat => CachedSoundEffect.WaveFormat;

        public Dictionary<string, string> Comments { get; } = cachedSound.Comments.ToDictionary();

        #endregion

        #region Playback fields

        public CachedSoundEffect CachedSoundEffect { get; private set; } = cachedSound;

        public PlaybackState PlaybackState { get; set; } = PlaybackState.Playing;

        #endregion

        #region Play region

        public long Length => CachedSoundEffect.AudioData.Length;

        public long Position
        {
            get
            {
                long pos = (long)(effectivePosition / sampleScalar);
                return pos + (pos % AudioStandards.ChannelCount);
            }
            set
            {
                long pos = (long)(value * sampleScalar);
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
            get => sampleScalar;
            set
            {
                if (sampleScalar == value)
                    return;

                long pos = Position;

                if (value == 1f)
                {
                    sampleScalar = 1f;
                    resampledData = CachedSoundEffect.AudioData;

                    effectiveLength = Length;
                    Position = pos;

                    effectiveLoopStart = LoopStart;
                    effectiveLoopEnd = LoopEnd;
                }

                sampleScalar = value;
                WdlResampler resampler = new();

                resampler.SetMode(true, 2, false);
                resampler.SetFilterParms();
                resampler.SetFeedMode(false); // output driven
                resampler.SetRates(AudioStandards.SampleRate, AudioStandards.SampleRate / sampleScalar);

                int count = (int)(CachedSoundEffect.AudioData.Length / sampleScalar) + 2;
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

                effectiveLoopStart = (long)(LoopStart / sampleScalar);
                effectiveLoopStart -= effectiveLoopStart % AudioStandards.ChannelCount;

                effectiveLoopEnd = (long)(LoopEnd / sampleScalar);
                effectiveLoopEnd += effectiveLoopEnd % AudioStandards.ChannelCount;

                // Sometimes rounding results in 0 or -2.
                // This misalignment isn't an issue when the looping tags are actually implemented.

                if (effectiveLoopStart <= 0)
                    effectiveLoopStart = -1;

                if (effectiveLoopEnd <= 0)
                    effectiveLoopEnd = -1;
            }
        }

        private float sampleScalar = 1f;

        private long effectiveLength = cachedSound.AudioData.Length;

        private long effectivePosition = 0;

        private long effectiveLoopStart = cachedSound.LoopStart;

        private long effectiveLoopEnd = cachedSound.LoopEnd;

        private float[] resampledData = cachedSound.AudioData;

        #endregion

        public int Read(float[] buffer, int offset, int count)
        {
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
