using Microsoft.Xna.Framework.Audio;
using MonoStereo;
using MonoStereo.AudioSources;
using MonoStereo.Filters;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.Audio;

namespace MonoStereoMod
{
    public class MonoStereoAudioTrack : Song, IAudioTrack
    {
        public ISongSource Source { get; }

        private readonly TerrariaFilter soundControl = new();

        public MonoStereoAudioTrack(ISongSource source) : base(source)
        {
            Source = source;
            AddFilter(soundControl);
        }

        public new IEnumerable<AudioFilter> Filters
        {
            get
            {
                var filters = base.Filters;
                return filters.TakeLast(filters.Count() - 1);
            }
        }

        public float Pitch
        {
            get => soundControl.PitchFactor;
            set => soundControl.PitchFactor = value;
        }

        public float Pan
        {
            get => soundControl.Panning;
            set => soundControl.Panning = value;
        }

        public bool IsPlaying => !IsDisposed && PlaybackState == PlaybackState.Playing;

        public bool IsStopped => IsDisposed || PlaybackState == PlaybackState.Stopped;

        public bool IsPaused => !IsDisposed && PlaybackState == PlaybackState.Paused;

        public override WaveFormat WaveFormat => Source.WaveFormat;

        public override PlaybackState PlaybackState { get; set; } = PlaybackState.Playing;

        public bool IsDisposed { get; private set; } = false;

        public new void Dispose()
        {
            base.Dispose();
            IsDisposed = true;
            GC.SuppressFinalize(this);
        }

        public override void Play() => base.Play();

        public void Stop(AudioStopOptions options) => base.Stop();

        public override int ReadSource(float[] buffer, int offset, int count) => base.ReadSource(buffer, offset, count);

        public void Reuse()
        {
            Source.Position = 0;
        }

        public void SetVariable(string variableName, float value)
        {
            switch (variableName)
            {
                case "Volume":
                    {
                        double num = 31.0 * (double)value - 25.0 - 11.94;
                        float volume = (float)Math.Pow(10.0, num / 20.0);
                        Volume = volume;
                        break;
                    }
                case "Pitch":
                    Pitch = value;
                    break;
                case "Pan":
                    Pan = value;
                    break;
            }
        }

        public void Update() { }
    }
}
