﻿using MonoStereo;
using MonoStereo.Filters;
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

        // Init to NaN to force resample prep
        private float _pitch = float.NaN;
        private float _rate = float.NaN;

        public override void PostProcess(float[] buffer, int offset, int samplesRead)
        {
            if (Panning != 0f) 
                Pan(buffer, offset, samplesRead);
        }

        public override int ModifyRead(float[] buffer, int offset, int count)
        {
            // FIX ME: resampling is causing a lot of audio artifacts, but only with specific sounds (???)
            // There isn't a pattern with which sounds it happens to, and it is caused by slight misalignmnet between consecutive buffers
            // It may be worth to implement a custom resampling algorithm to fix this, but I have other things I need to do first.

            //if (_rate != 1f)
            //{
            //    int framesRequested = count / AudioStandards.ChannelCount;
            //    int inNeeded = resampler.ResamplePrepare(framesRequested, AudioStandards.ChannelCount, out float[] inBuffer, out int inBufferOffset);

            //    int inAvailable = Provider.Read(inBuffer, inBufferOffset, inNeeded * AudioStandards.ChannelCount) / AudioStandards.ChannelCount;
            //    int outAvailable = resampler.ResampleOut(buffer, offset, inAvailable, framesRequested, AudioStandards.ChannelCount);

            //    return outAvailable * AudioStandards.ChannelCount;
            //}

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