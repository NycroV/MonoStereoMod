using MonoStereo;
using NAudio.Wave;
using OggVorbisEncoder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace MonoStereoMod.Systems.VanillaReaders
{
    internal static class SoundEffectConverter
    {
        public unsafe static CachedSoundEffect GetMonoStereoEffect(this Microsoft.Xna.Framework.Audio.SoundEffect xnaEffect)
        {
            var handle = xnaEffect.GetHandle();
            var formatPtr = xnaEffect.GetFormatPtr();

            FAudio.FAudioWaveFormatEx* tag = (FAudio.FAudioWaveFormatEx*)formatPtr;
            WaveFormat format = WaveFormat.CreateCustomFormat(
                (WaveFormatEncoding)tag->wFormatTag,
                (int)tag->nSamplesPerSec,
                tag->nChannels,
                (int)tag->nAvgBytesPerSec,
                tag->nBlockAlign,
                tag->wBitsPerSample);

            byte[] audioData = new byte[handle.AudioBytes];
            Marshal.Copy(handle.pAudioData, audioData, 0, audioData.Length);

            Dictionary<string, string> comments = [];
            using var rawStream = new RawSourceWaveStream(audioData, 0, audioData.Length, format);
            ISampleProvider provider = rawStream.ToSampleProvider();

            if (xnaEffect.GetLoopStart() != 0)
                comments.Add("LOOPSTART", xnaEffect.GetLoopStart().ToString());

            if (xnaEffect.GetLoopLength() != 0 && xnaEffect.GetLoopLength() != audioData.Length / format.BlockAlign)
                comments.Add("LOOPLENGTH", xnaEffect.GetLoopLength().ToString());

            return new CachedSoundEffect(provider, xnaEffect.Name, comments);
        }
    }
}
