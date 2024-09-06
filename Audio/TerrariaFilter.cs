using MonoStereo;
using MonoStereo.Filters;
using NAudio.Dsp;
using System;

namespace MonoStereoMod
{
    internal class TerrariaFilter : AudioFilter
    {
        public TerrariaFilter(float pitch = 0f, float pan = 0f)
        {
            PitchFactor = pitch;
            Panning = pan;

            resampler.SetMode(true, 2, false);
            resampler.SetFilterParms();
            resampler.SetFeedMode(false); // output driven
            resampler.SetRates(AudioStandards.SampleRate, AudioStandards.SampleRate / _rate);
        }

        public override FilterPriority Priority => FilterPriority.ApplyFirst;
        private readonly WdlResampler resampler = new();

        public float Panning { get; set; }
        public float PitchFactor
        {
            get
            {
                return _pitch;
            }
            set
            {
                _pitch = value;
                _rate = (float)Math.Pow(2d, value);
                resampler.SetRates(AudioStandards.SampleRate, AudioStandards.SampleRate / _rate);
            }
        }

        private float _pitch;
        private float _rate;

        public override void PostProcess(float[] buffer, int offset, int samplesRead)
        {
            if (Panning != 0f)
                Pan(buffer, offset, samplesRead);
        }

        public override int ModifyRead(float[] buffer, int offset, int count)
        {
            if (_pitch != 1f)
            {
                int framesRequested = count / AudioStandards.ChannelCount;
                int inNeeded = resampler.ResamplePrepare(framesRequested, AudioStandards.ChannelCount, out float[] inBuffer, out int inBufferOffset);

                int inAvailable = Provider.Read(inBuffer, inBufferOffset, inNeeded * AudioStandards.ChannelCount) / AudioStandards.ChannelCount;
                int outAvailable = resampler.ResampleOut(buffer, offset, inAvailable, framesRequested, AudioStandards.ChannelCount);

                return outAvailable * AudioStandards.ChannelCount;
            }

            return base.ModifyRead(buffer, offset, count);
        }

        private void Pan(float[] buffer, int offset, int samplesRead)
        {
            float normPan = (-Panning + 1f) / 2f;
            float leftChannel = (float)Math.Sqrt(normPan);
            float rightChannel = (float)Math.Sqrt(1 - normPan);

            for (int i = 0; i < samplesRead; i++)
            {
                if (i % 2 == 0)
                {
                    buffer[offset + i] *= leftChannel;
                }

                else
                {
                    buffer[offset + i] *= rightChannel;
                }
            }
        }
    }
}
