using Microsoft.Xna.Framework;
using MonoStereo;
using MonoStereo.AudioSources;
using MonoStereo.Filters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MonoStereoMod
{
    public class MonoStereoSoundEffect : SoundEffect
    {
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
            set => soundControl.PitchFactor = MathHelper.Clamp(value, -1f, 1f);
        }

        public float Pan
        {
            get => soundControl.Panning;
            set => soundControl.Panning = value;
        }

        public bool IsDisposed { get; private set; } = false;

        public bool IsPlaying => !IsDisposed && PlaybackState == NAudio.Wave.PlaybackState.Playing;

        public bool IsStopped => IsDisposed || PlaybackState == NAudio.Wave.PlaybackState.Stopped;

        private readonly TerrariaFilter soundControl = new();

        public MonoStereoSoundEffect(ISoundEffectSource source) : base(source)
        {
            AddFilter(soundControl);
        }

        public override void Close()
        {
            base.Close();
            IsDisposed = true;
        }
    }
}
