using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using MonoStereo;
using MonoStereo.Filters;
using MonoStereo.Sources;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;

namespace MonoStereoMod
{
    public class MonoStereoAudioTrack : Song, IAudioTrack
    {
        private readonly TerrariaFilter soundControl = new();

        public MonoStereoAudioTrack(ISongSource source) : base(source)
        {
            IsLooped = true;
            AddFilter(soundControl);
        }

        // Returns all filters currently applied minus
        // our filter to control FAudio properties. Wouldn't
        // want that getting removed on accident!
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
            set => soundControl.PitchFactor = value;
        }

        public float Pan
        {
            get => soundControl.Panning;
            set => soundControl.Panning = value;
        }

        // Override volume to apply after resampling instead of before.
        // This prevents weird quirks with resampling audio at very low volumes.
        public override float Volume
        {
            get => soundControl.Volume;
            set => soundControl.Volume = value;
        }

        public bool IsPlaying => !IsDisposed && PlaybackState == PlaybackState.Playing;

        public bool IsPaused => !IsDisposed && PlaybackState == PlaybackState.Paused;

        public bool IsStopped => IsDisposed || PlaybackState == PlaybackState.Stopped;

        public bool IsDisposed { get; private set; } = false;

        public void Stop(AudioStopOptions options) => Stop();

        public override void Stop()
        {
            PlaybackState = PlaybackState.Stopped;
        }

        public void Reuse()
        {
            if (Source is ISeekableSongSource seekable)
                seekable.Position = 0L;
        }

        public void SetVariable(string variableName, float value)
        {
            switch (variableName)
            {
                case "Volume":
                    {
                        // The vanilla curve is applied to the music mixer
                        if (value == Main.musicVolume)
                        {
                            Volume = 1f;
                            break;
                        }

                        // Offset the value to still follow the vanilla curve
                        // (which is applied to the music mixer)
                        value /= Main.musicVolume;
                        Volume = value;
                        break;
                    }

                case "Pitch":
                    value = MathHelper.Clamp(value, -1f, 1f);
                    Pitch = (float)Math.Pow(2d, value);
                    break;

                case "Pan":
                    Pan = value;
                    break;
            }
        }

        public void Update() { }

        public override void RemoveInput()
        {
            base.RemoveInput();
            ClearFilters();
        }

        public override void Dispose()
        {
            IsDisposed = true;
            base.Dispose();
        }
    }
}
