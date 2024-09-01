using Microsoft.Xna.Framework.Audio;
using MonoStereo.SampleProviders;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Audio;

namespace MonoStereoMod
{
    public class MonoStereoAudioTrack : MonoStereoProvider, IAudioTrack
    {
        public bool IsPlaying => throw new NotImplementedException();

        public bool IsStopped => throw new NotImplementedException();

        public bool IsPaused => throw new NotImplementedException();

        public override WaveFormat WaveFormat => throw new NotImplementedException();

        public override PlaybackState PlaybackState { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public new void Dispose()
        {
            base.Dispose();

            GC.SuppressFinalize(this);
        }

        public void Play()
        {
            
        }

        public void Stop(AudioStopOptions options)
        {
            
        }

        public override int ReadSource(float[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public void Reuse()
        {
            
        }

        public void SetVariable(string variableName, float value)
        {
            
        }

        public void Update()
        {
            
        }
    }
}
