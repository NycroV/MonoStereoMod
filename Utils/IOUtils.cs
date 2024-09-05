using MonoStereo;
using MonoStereo.Encoding;
using MonoStereoMod.Systems;
using NAudio.Wave;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;

namespace MonoStereoMod.Utils
{
    internal static partial class MonoStereoUtils
    {
        public static CachedSoundEffect LoadSoundEffect(Stream stream, string fileName, string extension)
        {
            ISampleProvider sampleProvider;
            IDictionary<string, string> comments;

            switch (extension)
            {
                case ".ogg":
                    var ogg = new OggReader(stream);
                    sampleProvider = ogg;
                    comments = ogg.Comments.ComposeComments();
                    break;

                case ".wav":
                    comments = stream.ReadComments();
                    var wav = new WaveFileReader(stream);
                    sampleProvider = wav.ToSampleProvider();
                    break;

                case ".mp3":
                    comments = stream.ReadComments();
                    Mp3FileReader mp3 = new(stream);
                    sampleProvider = mp3.ToSampleProvider();
                    break;

                default:
                    throw new NotSupportedException("Audio file type is not supported: " + extension);
            }

            return new CachedSoundEffect(sampleProvider, fileName, comments);
        }

        public static string ReadNullTerminatedString(this BinaryReader reader)
        {
            List<char> chars = [];
            char currentCharacter;

            do
            {
                currentCharacter = (char)reader.ReadByte();

                if (currentCharacter != '\0')
                    chars.Add(currentCharacter);
            }
            while (currentCharacter != '\0');

            return string.Concat(chars);
        }

        internal static List<WaveBankCue> ReadCues(BinaryReader[] readers, LegacyAudioSystem loadedSystem)
        {
            BinaryReader reader = readers[0];
            int version = reader.ReadInt32(); // 46 - XACT 3.0

            reader.BaseStream.Seek(12, SeekOrigin.Begin);

            int baseOffset = reader.ReadInt32(); // Bank data
            int baseSize = reader.ReadInt32();
            int entryOffset = reader.ReadInt32(); // Entry name data
            int entrySize = reader.ReadInt32();

            int extraOffset = reader.ReadInt32(); // Seektables
            int extraSize = reader.ReadInt32();
            int namesOffset = reader.ReadInt32(); // Entry names
            int namesSize = reader.ReadInt32();
            int namesEntrySize = 64;

            uint dataOffset = (uint)reader.ReadInt32();
            int dataSize = reader.ReadInt32();

            reader.BaseStream.Seek(baseOffset, SeekOrigin.Begin);

            uint baseFlags = reader.ReadUInt32();
            int totalSubsongs = reader.ReadInt32();
            string wavebankName = reader.ReadNullTerminatedString();

            // This seeks to the end of the max name length
            // (64 bytes + the 8 bytes we read for baseFlags and totalSubsongs)
            reader.BaseStream.Seek(baseOffset + namesEntrySize * 8, SeekOrigin.Begin);

            int entryElemSize = reader.ReadInt32();
            int metaNameEntrySize = reader.ReadInt32();

            int entryAlignment = reader.ReadInt32();
            uint format = (uint)reader.ReadInt32();
            int buildTime = reader.ReadInt32();

            List<WaveBankCue> cues = [];

            for (int i = 1; i < Main.maxMusic; i++)
            {
                reader = readers[i];
                reader.BaseStream.Seek(entryOffset + (i - 1) * entryElemSize, SeekOrigin.Begin);

                uint entryInfo = reader.ReadUInt32();
                uint entryFlags = entryInfo & 0xF;
                uint numSamples = (entryInfo >> 4) & 0x0FFFFFFF;

                format = reader.ReadUInt32();
                uint streamOffset = dataOffset + reader.ReadUInt32();
                uint streamSize = reader.ReadUInt32();

                uint loopStartSample = reader.ReadUInt32();
                uint loopEndSample = reader.ReadUInt32() + loopStartSample;

                uint bitsPerSample = (format >> 31) & 0x1; // Yes, this is only 1 byte
                uint blockAlign = (format >> 23) & 0xFF;  // 8 bytes
                uint sampleRate = (format >> 5) & 0x7FFFF; // 18 bytes
                uint channels = (format >> 2) & 0x7; // 3 bytes
                uint tag = format & 0x3; // 2 bytes

                WaveFormatEncoding encoding = tag switch
                {
                    0 => WaveFormatEncoding.Pcm,
                    2 => WaveFormatEncoding.Adpcm,
                    _ => throw new ArgumentException("Unknown audio codec type!", nameof(readers))
                };

                // We can skip reading from the .xsb file because tMod has already done it for us :)
                string name = loadedSystem.TrackNamesByIndex[i];
                WaveFormat waveFormat = WaveFormat.CreateCustomFormat(encoding, (int)sampleRate, (int)channels, (int)(blockAlign * sampleRate), (int)blockAlign, (int)(blockAlign / channels));

                long loopStart = loopStartSample > 0 ? loopStartSample : -1L;
                long loopEnd = loopEndSample > 0 && loopEndSample != numSamples ? loopEndSample : -1L;

                reader.BaseStream.Seek(streamOffset, SeekOrigin.Begin);
                WaveBankCue cue = new(reader, waveFormat, name, streamOffset, streamSize, loopStart, loopEnd);
                cues.Add(cue);
            }

            return cues;
        }
    }
}
