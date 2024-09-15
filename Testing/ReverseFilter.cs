using Microsoft.Xna.Framework.Media;
using MonoStereo.AudioSources;
using MonoStereo.Filters;
using MonoStereo.SampleProviders;
using NAudio.Utils;
using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MonoStereoMod.Testing
{
    public class ReverseFilter : AudioFilter
    {
        // Apply first as we are going to directly seek the audio in some cases
        public override FilterPriority Priority => FilterPriority.ApplyFirst;

        /// <summary>
        /// If this is true, the recorded audio will be played back in reverse.<br/>
        /// Otherwise, the audio will be recorded into memory for later reversal.<br/>
        /// <br/>
        /// If <see cref="Reversing"/> is <see langword="true"/>, but there is no recorded audio available,
        /// the filter will attempt to seek backwards and reverse the audio in real-time.
        /// </summary>
        public bool Reversing
        {
            get => reversing;
            set
            {
                if (reversing == value)
                    return;

                // Reverse the stored samples for playback
                foreach (var kvp in RecordedBuffers)
                {
                    kvp.Value.Reverse();

                    // Seek to the current position, accounting for how many samples are recorded
                    kvp.Key.Position = kvp.Value.RecordingStart + kvp.Value.Buffer.Count; 
                }

                reversing = value;
            }
        }

        private bool reversing = false;

        private class ReversableRecording
        {
            public ConcurrentQueue<float> Buffer { get; set; } = [];
            public float[] InsuranceBuffer = null;
            public long RecordingStart = -1;

            public void Reverse()
            {
                // Reversing the stored samples whenever the playback mode is toggled
                // means that reverse can be switched on and off without lots of memory allocation overhead
                var buffer = Buffer.Reverse().ToArray();

                // Make sure that the channels don't end up flipped
                for (int i = 0; i < buffer.Length; i += 2)
                    (buffer[i], buffer[i + 1]) = (buffer[i + 1], buffer[i]);

                lock (Buffer)
                {
                    Buffer = new(buffer);
                }
            }
        }

        private readonly Dictionary<ISeekableSampleProvider, ReversableRecording> RecordedBuffers = [];

        private bool GetRecording(out ReversableRecording recording)
        {
            if (RecordedBuffers.TryGetValue(Source is ISeekableSampleProvider seeker ? seeker : null, out ReversableRecording foundRecording))
                recording = foundRecording;

            else
                recording = null;

            return recording is not null;
        }

        public override void Apply(MonoStereoProvider provider)
        {
            if (provider is not ISeekableSampleProvider seekable || RecordedBuffers.ContainsKey(seekable))
                return;

            RecordedBuffers.Add(seekable, new());
        }

        public override void Unapply(MonoStereoProvider provider)
        {
            if (provider is not ISeekableSampleProvider seekable)
                return;

            // Ensure that we have an entry for this source
            if (!GetRecording(out var recording))
                return;

            RecordedBuffers.Remove(seekable);
            seekable.Position = recording.RecordingStart + recording.Buffer.Count;
        }

        public override void PostProcess(float[] buffer, int offset, int samplesRead)
        {
            // If we are actively reversing, we don't want to "record" reversed samples.
            if (Reversing)
                return;

            // Ensure that we have an entry for this source
            if (!GetRecording(out var recording))
                return;

            // Add all of the samples that we just read to the "recording"
            var sampleQueue = recording.Buffer;
            for (int i = 0; i < samplesRead; i++)
                sampleQueue.Enqueue(buffer[offset++]);
        }

        public override int ModifyRead(float[] buffer, int offset, int count)
        {
            try
            {
                // Ensure that we have an entry for this source
                if (!GetRecording(out var recording))
                    return base.ModifyRead(buffer, offset, count);

                // Mark the start of the recording
                if (recording.RecordingStart == -1)
                    recording.RecordingStart = (Source as ISeekableSampleProvider).Position;

                // If we are not actively reversing, don't modify how the reading happens
                if (!Reversing)
                    return base.ModifyRead(buffer, offset, count);

                // The sampleQueue is literally a queue of float samples
                var sampleQueue = recording.Buffer;
                int samplesRead = 0;

                lock (sampleQueue)
                {
                    // Before we try manually seek backwards, try reading from
                    // recorded audio. If the filter is set to reversing mode instead of recording,
                    // the sample queue should already be reversed.
                    for (int i = 0; i < count; i++)
                    {
                        if (sampleQueue.TryDequeue(out float sample))
                        {
                            buffer[offset++] = sample;
                            samplesRead++;
                        }

                        else
                            break;
                    }
                }

                // If there was enough recorded audio to fulfill requirements, stop caring
                if (samplesRead == count)
                    return samplesRead;

                int samplesRemaining = count - samplesRead;

                // If the source cannot be seeked, reversing won't be able to do anything.
                // We are only going to do the seeking if this provider is actually loopable, since in this context, we know that this filter
                // will only be applied to one of the mod's song sources. Therefore, we can count on it being loopable.
                //
                // Alternatively, you can make this method return simply `samplesRead` instead of doing default reading.
                // Be careful though - if samplesRead is 0, this will mark this source as ready for garbage collection.
                if (Source is not MonoStereoAudioTrack track || track.Source is not ITerrariaSongSource source)
                {
                    Reversing = false;
                    return samplesRead + base.ModifyRead(buffer, offset, samplesRemaining);
                }

                // We will only read backwards if this is an ILoopableSampleProvider
                // If you want, you can also make this account for ISeekableSampleProvider's, and just have the position default
                // to 0 (or do a sort of fake loop by seeking backwards from Length), although 99% of the time, the source will be a looped
                // provider regardless.
                if (source.Position < samplesRemaining)
                {
                    long endIndex = source.Length;

                    if (source.IsLooped && source.LoopEnd != -1)
                        endIndex = source.LoopEnd;

                    source.Position = endIndex - (samplesRemaining - source.Position);
                }

                else
                    source.Position -= samplesRemaining;

                // "InsuranceBuffer" is an intermediary buffer used to read and reverse audio in real-time
                long readStartPosition = source.Position;
                recording.RecordingStart = readStartPosition;

                recording.InsuranceBuffer = BufferHelpers.Ensure(recording.InsuranceBuffer, samplesRemaining);
                float[] insuranceBuffer = recording.InsuranceBuffer;

                // Read the source
                int sourceSamples = base.ModifyRead(insuranceBuffer, 0, samplesRemaining);

                for (int i = 0; i < sourceSamples; i += 2)
                {
                    // Make sure that the channels don't end up flipped by doing i + 1 first
                    //
                    // 0 left, 1 right, 2 left, etc... when reversed gives:
                    // 0 right, 1 left, 2 right, etc...
                    // We still want the audio in the correct channel
                    //
                    // Place the samples into the final buffer in reverse order
                    // We use --offset instead of offset-- as a neat shortcut since sourceSamples is not 0-based indexing
                    buffer[sourceSamples + --offset] = insuranceBuffer[i + 1];
                    buffer[sourceSamples + --offset] = insuranceBuffer[i];
                }

                // Seek back to where we finished the reverse reading
                source.Position = readStartPosition;
                return samplesRead + sourceSamples;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }
    }
}
