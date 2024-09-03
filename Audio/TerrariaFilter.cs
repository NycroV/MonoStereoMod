using MonoStereo.Filters;
using MonoStereo;
using NAudio.Dsp;
using System;

namespace MonoStereoMod
{
    internal class TerrariaFilter(float pitch = 1f, float pan = 0f) : AudioFilter
    {
        // Don't even worry about naming conventions here, they follow audio standards, not coding standards

        private readonly int fftSize = 4096;
        private readonly long osamp = 4L;
        private readonly SmbPitchShifter shifterLeft = new();
        private readonly SmbPitchShifter shifterRight = new();

        //Limiter constants
        const float LIM_THRESH = 0.95f;
        const float LIM_RANGE = (1f - LIM_THRESH);
        const float PiOver2 = 1.57079637f;

        public override FilterPriority Priority => FilterPriority.ApplyFirst;

        public float Panning { get; set; } = pan;
        public float PitchFactor { get; set; } = pitch;

        public override void PostProcess(float[] buffer, int offset, int samplesRead)
        {
            if (PitchFactor != 1f)
                PitchShift(buffer, offset, samplesRead);

            if (Panning == 0f)
                Pan(buffer, offset, samplesRead);
        }

        private void PitchShift(float[] buffer, int offset, int samplesRead)
        {
            int sampleRate = AudioStandards.SampleRate;
            var left = new float[(samplesRead >> 1)];
            var right = new float[(samplesRead >> 1)];
            var index = 0;
            for (var sample = offset; sample <= samplesRead + offset - 1; sample += 2)
            {
                left[index] = buffer[sample];
                right[index] = buffer[sample + 1];
                index += 1;
            }

            shifterLeft.PitchShift(PitchFactor, samplesRead >> 1, fftSize, osamp, sampleRate, left);
            shifterRight.PitchShift(PitchFactor, samplesRead >> 1, fftSize, osamp, sampleRate, right);
            index = 0;

            for (var sample = offset; sample <= samplesRead + offset - 1; sample += 2)
            {
                buffer[sample] = Limiter(left[index]);
                buffer[sample + 1] = Limiter(right[index]);
                index += 1;
            }
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

        private static float Limiter(float sample)
        {
            float res;
            if ((LIM_THRESH < sample))
            {
                res = (sample - LIM_THRESH) / LIM_RANGE;
                res = (float)((Math.Atan(res) / PiOver2) * LIM_RANGE + LIM_THRESH);
            }
            else if ((sample < -LIM_THRESH))
            {
                res = -(sample + LIM_THRESH) / LIM_RANGE;
                res = -(float)((Math.Atan(res) / PiOver2) * LIM_RANGE + LIM_THRESH);
            }
            else
            {
                res = sample;
            }
            return res;
        }
    }
}
