using MonoStereo;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MonoStereoMod.Systems
{
    internal static class SoundEffectConverter
    {
        // Turns an FNA SoundEffect into a MonoStereo CachedSoundEffect
        public unsafe static CachedSoundEffect GetMonoStereoEffect(this Microsoft.Xna.Framework.Audio.SoundEffect xnaEffect)
        {
            // These pointers allow us access to the FAudio data
            // that is contained within an FNA SoundEffect.
            FAudio.FAudioBuffer handle = xnaEffect.GetHandle();
            IntPtr formatPtr = xnaEffect.GetFormatPtr();

            // Convert the FAudio WaveFormatExtensible into a NAudio WaveFormat.
            FAudio.FAudioWaveFormatEx* tag = (FAudio.FAudioWaveFormatEx*)formatPtr;
            WaveFormat format = WaveFormat.CreateCustomFormat(
                (WaveFormatEncoding)tag->wFormatTag,
                (int)tag->nSamplesPerSec,
                tag->nChannels,
                (int)tag->nAvgBytesPerSec,
                tag->nBlockAlign,
                tag->wBitsPerSample);

            // Copy all of the sound's data into our own buffer, so we can
            // convert it to a standardized format (IEEE floats, 44.1kHz SR, 2 channels)
            // regardless of the source format.
            byte[] audioData = new byte[handle.AudioBytes];
            Marshal.Copy(handle.pAudioData, audioData, 0, audioData.Length);

            // Provides a way to read the raw PCM stream.
            using var rawStream = new RawSourceWaveStream(audioData, 0, audioData.Length, format);
            ISampleProvider provider = rawStream.ToSampleProvider();

            // Creates comments for LoopStart/LoopLength tags, if they exist
            // AND they are not the default values (0 for start, the length of the data in frames for end)
            Dictionary<string, string> comments = [];

            if (xnaEffect.GetLoopStart() != 0)
                comments.Add("LOOPSTART", xnaEffect.GetLoopStart().ToString());

            if (xnaEffect.GetLoopLength() != 0 && xnaEffect.GetLoopLength() != audioData.Length / format.BlockAlign)
                comments.Add("LOOPLENGTH", xnaEffect.GetLoopLength().ToString());

            // Reformat the data into a new CachedSoundEffect.
            return new CachedSoundEffect(provider, xnaEffect.Name, comments);
        }
    }
}
