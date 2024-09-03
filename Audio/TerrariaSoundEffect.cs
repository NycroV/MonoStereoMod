using MonoStereo;
using MonoStereo.AudioSources;
using MonoStereo.Filters;
using NAudio.Dsp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MonoStereoMod
{
    public class TerrariaSoundEffect : SoundEffect
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
            set => soundControl.PitchFactor = value;
        }

        public float Pan
        {
            get => soundControl.Panning;
            set => soundControl.Panning = value;
        }

        public bool IsDisposed { get; private set; } = false;

        public bool IsPlaying => !IsDisposed && PlaybackState == NAudio.Wave.PlaybackState.Playing;

        private readonly TerrariaFilter soundControl = new();

        public TerrariaSoundEffect(ISoundEffectSource source) : base(source)
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
