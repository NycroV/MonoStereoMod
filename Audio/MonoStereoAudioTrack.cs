using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using MonoStereo;
using MonoStereo.AudioSources;
using MonoStereo.Filters;
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
            set => soundControl.PitchFactor = MathHelper.Clamp(value, -1f, 1f);
        }

        public float Pan
        {
            get => soundControl.Panning;
            set => soundControl.Panning = value;
        }
        
        // Volume exists as a property of Song

        public bool IsPlaying => !IsDisposed && PlaybackState == PlaybackState.Playing;

        public bool IsPaused => !IsDisposed && PlaybackState == PlaybackState.Paused;

        public bool IsStopped => IsDisposed || PlaybackState == PlaybackState.Stopped;

        public override WaveFormat WaveFormat => Source.WaveFormat;

        public bool IsDisposed { get; private set; } = false;

        // This ensures the track is only disposed if we actually want to dispose it.
        public override void Close()
        {
            if (IsDisposed)
                base.Close();

            else
                AudioManager.RemoveSongInput(this);
        }

        public override void Dispose()
        {
            IsDisposed = true;
            base.Dispose();
        }

        public void Stop(AudioStopOptions options) => Stop();

        public override void Stop()
        {
            base.Stop();
            ClearFilters();
        }

        public void Reuse() => Source.Position = 0L;

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
