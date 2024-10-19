using Microsoft.Xna.Framework;
using MonoStereo;
using MonoStereo.AudioSources;
using MonoStereo.Filters;
using MonoStereoMod.Audio;
using MonoStereoMod.Systems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MonoStereoMod
{
    public class MonoStereoSoundEffect : SoundEffect
    {
        public override IEnumerable<AudioFilter> Filters
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
            set
            {
                value = MathHelper.Clamp(value, -1f, 1f);
                soundControl.PitchFactor = value;

                if (SoundCache.TryGetFNA(this, out var sound))
                    sound.set_Pitch(Pitch);

                // Resample the data for pitch adjustment here
                if (Source is TerrariaCachedSoundEffectReader reader)
                    reader.EffectivePitch = (float)Math.Pow(2d, value); ;
            }
        }

        public float Pan
        {
            get => soundControl.Panning;
            set
            {
                soundControl.Panning = value;

                if (SoundCache.TryGetFNA(this, out var sound))
                    sound.set_Pan(Pan);
            }
        }

        public override float Volume
        {
            get => base.Volume;
            set
            {
                base.Volume = value;

                if (SoundCache.TryGetFNA(this, out var sound))
                    sound.set_Volume(Volume);
            }
        }

        public override bool IsLooped
        {
            get => base.IsLooped;
            set
            {
                base.IsLooped = value;

                if (SoundCache.TryGetFNA(this, out var sound))
                    sound.set_IsLooped(IsLooped);
            }
        }

        public bool IsDisposed { get; private set; } = false;

        private readonly TerrariaFilter soundControl = new();

        public MonoStereoSoundEffect(ISoundEffectSource source) : base(source)
        {
            AddFilter(soundControl);
        }

        // This ensures the track is only disposed if we actually want to dispose it.
        public override void Close()
        {
            if (IsDisposed)
                base.Close();

            else
                AudioManager.RemoveSoundInput(this);
        }

        public override void Dispose()
        {
            IsDisposed = true;
            base.Dispose();
        }
    }
}
