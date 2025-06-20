﻿using MonoStereo;
using MonoStereo.Filters;
using NAudio.Dsp;

namespace MonoStereoMod.Audio
{
    internal class TerrariaFilter : AudioFilter
    {
        public TerrariaFilter(float pitch = 1f, float pan = 0f, float volume = 1f)
        {
            resampler.SetMode(true, 2, false);
            resampler.SetFilterParms();
            resampler.SetFeedMode(false); // output driven

            PitchFactor = pitch;
            Panning = pan;
            Volume = volume;
        }

        public override FilterPriority Priority => FilterPriority.ApplyFirst;
        private readonly WdlResampler resampler = new();

        public float Panning { get; set; }
        public float Volume { get; set; }

        // Init to NaN to force resample prep
        private float _pitch = float.NaN;

        public float PitchFactor
        {
            get
            {
                return _pitch;
            }
            set
            {
                if (_pitch == value)
                    return;

                _pitch = value;

                if (_pitch != 1f)
                    resampler.SetRates(AudioStandards.SampleRate, AudioStandards.SampleRate / _pitch);
            }
        }

        public override void PostProcess(float[] buffer, int offset, int samplesRead)
        {
            if (Panning != 0f)
                Pan(buffer, offset, samplesRead);

            if (Volume != 1f)
            {
                for (int i = offset; i < samplesRead; i++)
                    buffer[i] *= Volume;
            }
        }

        public override int ModifyRead(float[] buffer, int offset, int count)
        {
            if (_pitch != 1f)
            {
                int framesRequested = count / AudioStandards.ChannelCount;
                int inNeeded = resampler.ResamplePrepare(framesRequested, AudioStandards.ChannelCount, out float[] inBuffer, out int inBufferOffset);

                int inAvailable = base.ModifyRead(inBuffer, inBufferOffset, inNeeded * AudioStandards.ChannelCount) / AudioStandards.ChannelCount;
                int outAvailable = resampler.ResampleOut(buffer, offset, inAvailable, framesRequested, AudioStandards.ChannelCount);

                return outAvailable * AudioStandards.ChannelCount;
            }

            return base.ModifyRead(buffer, offset, count);
        }

        private void Pan(float[] buffer, int offset, int samplesRead)
        {
            // The below panning strategy is the same panning strategy used by FAudio.
            // Volume is not only adjusted on left/right channels, but channels are mixed
            // in accordance with where the sound should actually be coming from.

            float leftChannelLeftMultiplier;
            float leftChannelRightMultiplier;

            float rightChannelLeftMultiplier;
            float rightChannelRightMultiplier;

            // On the left...
            if (Panning < 0f)
            {
                leftChannelLeftMultiplier = 0.5f * Panning + 1f;
                leftChannelRightMultiplier = 0.5f * -Panning;

                rightChannelLeftMultiplier = 0f;
                rightChannelRightMultiplier = Panning + 1f;
            }

            // On the right...
            else
            {
                leftChannelLeftMultiplier = -Panning + 1f;
                leftChannelRightMultiplier = 0f;

                rightChannelLeftMultiplier = 0.5f * Panning;
                rightChannelRightMultiplier = 0.5f * -Panning + 1f;
            }

            for (int i = 0; i < samplesRead; i += 2)
            {
                float leftChannel = buffer[offset + i];
                float rightChannel = buffer[offset + i + 1];

                buffer[offset + i] = (leftChannel * leftChannelLeftMultiplier) + (rightChannel * leftChannelRightMultiplier);
                buffer[offset + i + 1] = (leftChannel * rightChannelLeftMultiplier) + (rightChannel * rightChannelRightMultiplier);
            }
        }
    }
}
