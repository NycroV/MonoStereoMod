using MonoStereo.AudioSources;
using MonoStereo.AudioSources.Songs;
using MonoStereo.Filters;
using MonoStereo.SampleProviders;
using NAudio.Utils;
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
                // Assign every recording to match the state specified.
                foreach (var kvp in RecordedBuffers)
                {
                    // If the states are already equal, we don't want to
                    // reverse the recorded audio when it shouldn't be reversed.
                    if (kvp.Value.Reversing == value)
                        continue;

                    // Reverse the recorded samples, and mark
                    //that this source is now reading in reverse.
                    kvp.Value.Reverse();
                    kvp.Value.Reversing = value;

                    // Seek to the current position, accounting for how many samples are recorded
                    kvp.Key.Position = kvp.Value.RecordingStart + kvp.Value.Buffer.Count;
                }

                reversing = value;
            }
        }

        private bool reversing = false;

        // The ReversableRecording collection represents the
        // cached samples for all of the sources this filter is applied to.
        private readonly Dictionary<ISeekableSampleProvider, ReversableRecording> RecordedBuffers = [];

        // This allows access to specific reversable recordings for outside modification
        // Ex: only reversing certain sources
        public ReversableRecording this[ISeekableSampleProvider provider]
        {
            get
            {
                if (GetRecording(provider, out var recording))
                    return recording;

                return null;
            }
        }

        /// <summary>
        /// This class is only intended to be used with the <see cref="ReverseFilter"/><br/>
        /// This stores data for all of the sources this filter is applied to.
        /// </summary>
        public class ReversableRecording
        {
            public ConcurrentQueue<float> Buffer { get; set; } = [];
            public float[] InsuranceBuffer = null;
            public long RecordingStart = -1;
            public bool Reversing = false;

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

        // This is basically just a RecordedBuffers.TryGet() with some extra null safety
        private bool GetRecording(out ReversableRecording recording) => GetRecording(Source is ISeekableSampleProvider seeker ? seeker : null, out recording);

        private bool GetRecording(ISeekableSampleProvider sampleProvider, out ReversableRecording recording)
        {
            if (RecordedBuffers.TryGetValue(sampleProvider, out var foundRecording))
                recording = foundRecording;

            else
                recording = null;

            return recording is not null;
        }

        // Ensure an entry exists whenever this filter is applied
        public override void Apply(MonoStereoProvider provider)
        {
            if (provider is not ISeekableSampleProvider seekable || RecordedBuffers.ContainsKey(seekable))
                return;

            RecordedBuffers.Add(seekable, new());
        }

        // Whenever this filter is removed from a source, we no longer
        // need to keep track of cached samples for that source
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
            // Ensure that we have an entry for this source
            if (!GetRecording(out var recording))
                return;

            // If we are actively reversing, we don't want to "record" reversed samples.
            if (recording.Reversing)
                return;

            // Add all of the samples that we just read to the "recording"
            var sampleQueue = recording.Buffer;
            float volume = Source.Volume;

            for (int i = 0; i < samplesRead; i++)
                sampleQueue.Enqueue(buffer[offset++] / volume);
        }

        public override int ModifyRead(float[] buffer, int offset, int count)
        {
            // Ensure that we have an entry for this source
            if (!GetRecording(out var recording))
                return base.ModifyRead(buffer, offset, count);

            // Mark the start of the recording
            if (recording.RecordingStart == -1)
                recording.RecordingStart = (Source as ISeekableSampleProvider).Position;

            // If we are not actively reversing, don't modify how the reading happens
            if (!recording.Reversing)
                return base.ModifyRead(buffer, offset, count);

            // The sampleQueue is literally a queue of float samples
            var sampleQueue = recording.Buffer;
            int samplesRead = 0;
            float volume = Source.Volume;

            lock (sampleQueue)
            {
                // Before we try manually seek backwards, try reading from
                // recorded audio. If the filter is set to reversing mode instead of recording,
                // the sample queue should already be reversed.
                for (int i = 0; i < count; i++)
                {
                    if (sampleQueue.TryDequeue(out float sample))
                    {
                        buffer[offset++] = sample * volume;
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

            // If the source cannot be seeked, reversing won't be able to do anything past the cached samples.
            // We are only going to do the seeking if this provider is not only seekable, but loopable as well, since in this context,
            // we know that this filter will only be applied to one of the mod's song sources.
            // (All of the mod's sources are loopable by default.)
            //
            // If you want, you can also make this account for `ISeekableSampleProvider`s, and just have the position default
            // to 0 or do a sort of fake loop by seeking backwards from Length - although, again, in our context, it's
            // pretty safe to assume the source will be a song.
            MonoStereoAudioTrack seekSource = null;
            ITerrariaSongSource loopSource = null;

            if (Source is MonoStereoAudioTrack track)
            {
                seekSource = track;

                // Although all of the default song sources actually use BufferedReaders,
                // it is possible that another modder may have implemented their own
                // source that doesn't. We check both cases to be safe.
                if (track.Source is ITerrariaSongSource source)
                    loopSource = source;

                else if (track.Source is BufferedSongReader bufferedReader && bufferedReader.Source is ITerrariaSongSource bufferedSource)
                    loopSource = bufferedSource;
            }

            // Alternatively, you can make this method return simply `samplesRead` instead of disabling reversal for this specific source.
            // Be careful though - if samplesRead is 0, this will mark this source as ready for garbage collection.
            if (seekSource is null || loopSource is null)
            {
                recording.Reversing = false;
                return samplesRead + base.ModifyRead(buffer, offset, samplesRemaining);
            }

            // When we reverse audio, we want to take both loop start and end into account.
            long startIndex = long.Max(loopSource.LoopStart, 0);
            long samplesAvailable = seekSource.Position - startIndex;

            // If this read is expected to cross over the beginning of the loop,
            // we want to seek to just before the end of the loop instead. That way,
            // when audio is read, it reads around the loop.
            if (samplesAvailable < samplesRemaining)
            {
                long endIndex = seekSource.Length;

                if (seekSource.IsLooped && loopSource.LoopEnd != -1)
                    endIndex = loopSource.LoopEnd;

                seekSource.Position = endIndex - (samplesRemaining - samplesAvailable);
            }

            // If this is the first time we are real-time seeking to reverse,
            // we need to seek to where the recording started. At this point,
            // our source's position is still wherever we stopped the recording.
            else if (seekSource.Position > recording.RecordingStart)
                seekSource.Position = recording.RecordingStart - samplesRemaining;

            // If our position has already been properly aligned, and
            // we don't need to account for looping, seeking backwards
            // is very easy.
            else
                seekSource.Position -= samplesRemaining;

            // We record where we're going to start the reading, because we're going to want to seek
            // back here after the reading is done. That way our "end position" is technically at the
            // "beginning" of our read section.
            long readStartPosition = seekSource.Position;
            recording.RecordingStart = readStartPosition;

            // "InsuranceBuffer" is an intermediary buffer used to read and reverse audio in real-time.
            recording.InsuranceBuffer = BufferHelpers.Ensure(recording.InsuranceBuffer, samplesRemaining);
            float[] insuranceBuffer = recording.InsuranceBuffer;

            // Read the source
            int sourceSamples = base.ModifyRead(insuranceBuffer, 0, samplesRemaining);

            for (int i = 0; i < sourceSamples; i += 2)
            {
                // Make sure that the channels don't end up flipped by doing i + 1 first.
                //
                // 0 left, 1 right, 2 left, etc... when reversed gives:
                // 0 right, 1 left, 2 right, etc...
                //
                // We still want the audio in the correct channel, so we just flip flop every 2 samples.
                //
                // Place the samples into the final buffer in reverse order
                // We use --offset instead of offset-- as a neat shortcut since sourceSamples is not 0-based indexing.
                buffer[sourceSamples + --offset] = insuranceBuffer[i + 1];
                buffer[sourceSamples + --offset] = insuranceBuffer[i];
            }

            // Seek back to where we "started" the reverse reading, as if
            // the reading were REALLY reversed, this is technically where it would
            // end, not begin.
            seekSource.Position = readStartPosition;
            return samplesRead + sourceSamples;
        }
    }
}
