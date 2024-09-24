using Microsoft.Xna.Framework.Audio;
using MonoMod.RuntimeDetour;
using NAudio.Wave;
using System.Reflection;

namespace MonoStereoMod.Detours
{
    internal static partial class Detours
    {
        #region Property Hooks

        public static Hook SoundEffectInstance_set_IsLooped_Hook;

        public static Hook SoundEffectInstance_set_Pan_Hook;

        public static Hook SoundEffectInstance_set_Pitch_Hook;

        public static Hook SoundEffectInstance_set_Volume_Hook;

        public static Hook SoundEffectInstance_get_State_Hook;

        public static readonly MethodInfo SoundEffectInstance_set_IsLooped_Method = typeof(SoundEffectInstance).GetProperty("IsLooped", BindingFlags.Instance | BindingFlags.Public).GetSetMethod();

        public static readonly MethodInfo SoundEffectInstance_set_Pan_Method = typeof(SoundEffectInstance).GetProperty("Pan", BindingFlags.Instance | BindingFlags.Public).GetSetMethod();

        public static readonly MethodInfo SoundEffectInstance_set_Pitch_Method = typeof(SoundEffectInstance).GetProperty("Pitch", BindingFlags.Instance | BindingFlags.Public).GetSetMethod();

        public static readonly MethodInfo SoundEffectInstance_set_Volume_Method = typeof(SoundEffectInstance).GetProperty("Volume", BindingFlags.Instance | BindingFlags.Public).GetSetMethod();

        public static readonly MethodInfo SoundEffectInstance_get_State_Method = typeof(SoundEffectInstance).GetProperty("State", BindingFlags.Instance | BindingFlags.Public).GetGetMethod();

        public delegate void SoundEffectInstance_set_IsLooped_OrigDelegate(SoundEffectInstance self, bool value);

        public delegate void SoundEffectInstance_set_Pan_OrigDelegate(SoundEffectInstance self, float value);

        public delegate void SoundEffectInstance_set_Pitch_OrigDelegate(SoundEffectInstance self, float value);

        public delegate void SoundEffectInstance_set_Volume_OrigDelegate(SoundEffectInstance self, float value);

        public delegate SoundState SoundEffectInstance_get_State_OrigDelegate(SoundEffectInstance self);

        #endregion

        #region Method Hooks

        public static Hook SoundEffectInstance_Dispose_Hook;

        public static Hook SoundEffectInstance_Apply3D_Hook;

        public static Hook SoundEffectInstance_Play_Hook;

        public static Hook SoundEffectInstance_Pause_Hook;

        public static Hook SoundEffectInstance_Resume_Hook;

        public static Hook SoundEffectInstance_Stop_Hook;

        public static readonly MethodInfo SoundEffectInstance_Dispose_Method = typeof(SoundEffectInstance).GetMethod("Dispose", BindingFlags.Instance | BindingFlags.NonPublic, [typeof(bool)]);

        public static readonly MethodInfo SoundEffectInstance_Apply3D_Method = typeof(SoundEffectInstance).GetMethod("Apply3D", BindingFlags.Instance | BindingFlags.Public, [typeof(AudioListener), typeof(AudioEmitter)]);

        public static readonly MethodInfo SoundEffectInstance_Play_Method = typeof(SoundEffectInstance).GetMethod("Play", BindingFlags.Instance | BindingFlags.Public);

        public static readonly MethodInfo SoundEffectInstance_Pause_Method = typeof(SoundEffectInstance).GetMethod("Pause", BindingFlags.Instance | BindingFlags.Public);

        public static readonly MethodInfo SoundEffectInstance_Resume_Method = typeof(SoundEffectInstance).GetMethod("Resume", BindingFlags.Instance | BindingFlags.Public);

        public static readonly MethodInfo SoundEffectInstance_Stop_Method = typeof(SoundEffectInstance).GetMethod("Stop", BindingFlags.Instance | BindingFlags.Public, [typeof(bool)]);

        public delegate void SoundEffectInstance_Dispose_OrigDelegate(SoundEffectInstance self, bool disposing);

        public delegate void SoundEffectInstance_Apply3D_OrigDelegate(SoundEffectInstance self, AudioListener listener, AudioEmitter emitter);

        public delegate void SoundEffectInstance_Play_OrigDelegate(SoundEffectInstance self);

        public delegate void SoundEffectInstance_Pause_OrigDelegate(SoundEffectInstance self);

        public delegate void SoundEffectInstance_Resume_OrigDelegate(SoundEffectInstance self);

        public delegate void SoundEffectInstance_Stop_OrigDelegate(SoundEffectInstance self, bool immediate);

        #endregion

        #region Property Overrides

        // The below methods change the behavior of any SoundEffectInstance whose creation was overwritten by MonoStereo
        // FNA uses property wrappers over underlying fields for most of their properties. Anywhere you see self.set_XXXX, reflection
        // has been used to directly modify these underlying fields.

        public static void set_SoundEffectInstance_IsLooped(SoundEffectInstance_set_IsLooped_OrigDelegate orig, SoundEffectInstance self, bool value)
        {
            if (!SoundCache.TryGetMonoStereo(self, out var instance))
            {
                orig(self, value);
                return;
            }

            instance.IsLooped = value;
        }

        public static void set_SoundEffectInstance_Pan(SoundEffectInstance_set_Pan_OrigDelegate orig, SoundEffectInstance self, float value)
        {
            if (!SoundCache.TryGetMonoStereo(self, out var instance))
            {
                orig(self, value);
                return;
            }

            instance.Pan = value;
        }

        public static void set_SoundEffectInstance_Pitch(SoundEffectInstance_set_Pitch_OrigDelegate orig, SoundEffectInstance self, float value)
        {
            if (!SoundCache.TryGetMonoStereo(self, out var instance))
            {
                orig(self, value);
                return;
            }

            instance.Pitch = value;
        }

        public static void set_SoundEffectInstance_Volume(SoundEffectInstance_set_Volume_OrigDelegate orig, SoundEffectInstance self, float value)
        {
            if (!SoundCache.TryGetMonoStereo(self, out var instance))
            {
                orig(self, value);
                return;
            }

            instance.Volume = value;
        }

        public static SoundState get_SoundEffectInstance_State(SoundEffectInstance_get_State_OrigDelegate orig, SoundEffectInstance self)
        {
            if (!SoundCache.TryGetMonoStereo(self, out var instance))
            {
                return orig(self);
            }

            return instance.PlaybackState switch
            {
                PlaybackState.Playing => SoundState.Playing,
                PlaybackState.Stopped => SoundState.Stopped,
                PlaybackState.Paused => SoundState.Paused,
                _ => orig(self) // This is impossible to hit
            };
        }

        #endregion

        #region Method Overrides

        public static void On_SoundEffectInstance_Dispose(SoundEffectInstance_Dispose_OrigDelegate orig, SoundEffectInstance self, bool disposing)
        {
            if (disposing && SoundCache.TryGetMonoStereo(self, out var instance))
                instance.Dispose();

            orig(self, disposing);
        }

        public static void On_SoundEffectInstance_Apply3D(SoundEffectInstance_Apply3D_OrigDelegate orig, SoundEffectInstance self, AudioListener listener, AudioEmitter emitter)
        {
            // We don't support 3D sound...
            // Might be worth integrating this as a filter in the future

            if (!SoundCache.TryGetMonoStereo(self, out _))
            {
                orig(self, listener, emitter);
                return;
            }
        }

        public static void On_SoundEffectInstance_Play(SoundEffectInstance_Play_OrigDelegate orig, SoundEffectInstance self)
        {
            if (!SoundCache.TryGetMonoStereo(self, out var instance))
            {
                orig(self);
                return;
            }

            instance.Play();
        }

        public static void On_SoundEffectInstance_Pause(SoundEffectInstance_Pause_OrigDelegate orig, SoundEffectInstance self)
        {
            if (!SoundCache.TryGetMonoStereo(self, out var instance))
            {
                orig(self);
                return;
            }

            instance.Pause();
        }

        public static void On_SoundEffectInstance_Resume(SoundEffectInstance_Resume_OrigDelegate orig, SoundEffectInstance self)
        {
            if (!SoundCache.TryGetMonoStereo(self, out var instance))
            {
                orig(self);
                return;
            }

            instance.Resume();
        }

        public static void On_SoundEffectInstance_Stop(SoundEffectInstance_Stop_OrigDelegate orig, SoundEffectInstance self, bool immediate)
        {
            if (!SoundCache.TryGetMonoStereo(self, out var instance))
            {
                orig(self, immediate);
                return;
            }

            instance.Stop();
        }

        #endregion
    }
}
