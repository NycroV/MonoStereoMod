using MonoStereo;
using MonoStereo.Filters;
using MonoStereoMod.Audio;
using NAudio.Dsp;
using System;

namespace MonoStereoMod
{
    internal class TerrariaFilter : AudioFilter
    {
        public TerrariaFilter(float pitch = 0f, float pan = 0f)
        {
            resampler.SetMode(true, 2, false);
            resampler.SetFilterParms();
            resampler.SetFeedMode(false); // output driven

            PitchFactor = pitch;
            Panning = pan;
        }

        public override FilterPriority Priority => FilterPriority.ApplyFirst;
        private readonly WdlResampler resampler = new();

        public float Panning { get; set; }

        // Init to NaN to force resample prep
        private float _pitch = float.NaN;
        private float _rate = float.NaN;

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
                _rate = (float)Math.Pow(2d, value);

                if (_rate != 1f)
                    resampler.SetRates(AudioStandards.SampleRate, AudioStandards.SampleRate / _rate);
            }
        }

        public override void PostProcess(float[] buffer, int offset, int samplesRead)
        {
            if (Panning != 0f)
                Pan(buffer, offset, samplesRead);
        }

        public override int ModifyRead(float[] buffer, int offset, int count)
        {
            // We only utilize real-time resampling if the rate is not 1 (no pitch adjustment)
            // AND the source is not a TerrariaCachedSoundEffectReader. This is because the Terraria reader
            // resamples the data in its entirety whenever pitch is changed to help reduce
            // artifacts when placing resampled buffers end to end (in some scenarios).
            //
            // This should be investigated to figure out where those artifacts are coming from.
            // Performance? WinMM? I have no idea, but this system works for now. I'll try to
            // find the exact cause and fix it if possible in the near future. Live resampling
            // would be much easier, not to mention more intuitive.

            if (_rate != 1f && (Source is not MonoStereoSoundEffect effect || effect.Source is not TerrariaCachedSoundEffectReader))
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
