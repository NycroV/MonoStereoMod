using MonoStereoMod.Systems;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;

namespace MonoStereoMod.Utils
{
    internal static partial class MonoStereoUtils
    {
        // Vanilla WaveBank files have strings that read "null terminated", meaning
        // they will always be a certain number of bytes, but the actual string data
        // ends at the first instance of a null character (literal '\0').
        public static string ReadNullTerminatedString(this BinaryReader reader, int bytes)
        {
            byte[] buffer = reader.ReadBytes(bytes);
            List<char> chars = [];

            for (int i = 0; i < buffer.Length; i++)
            {
                char letter = (char)buffer[i];

                if (letter != '\0')
                    chars.Add(letter);

                else
                    break;
            }

            return string.Concat(chars);
        }

        // You know, I spent an embarassingly long time reading through VGMStream/FAudio source
        // and translating the C code into C#, only to realize after the fact that logic for reading
        // WaveBanks in C# was already available in MonoGame. That was not a fun day.
        //
        // Reads a WaveBank file, and returns a list of all "cues" (tracks) contained within that bank.
        internal static List<WaveBankCue> ReadCues(BinaryReader[] readers, LegacyAudioSystem loadedSystem)
        {
            BinaryReader reader = readers[0];
            int version = reader.ReadInt32(); // 46 - XACT 3.0
            int headerVersion = reader.ReadInt32();

            int baseOffset = reader.ReadInt32(); // Bank data
            int baseSize = reader.ReadInt32();
            int entryOffset = reader.ReadInt32(); // Entry name data
            int entrySize = reader.ReadInt32();

            int extraOffset = reader.ReadInt32(); // Seektables
            int extraSize = reader.ReadInt32();
            int namesOffset = reader.ReadInt32(); // Entry names
            int namesSize = reader.ReadInt32();

            uint dataOffset = (uint)reader.ReadInt32();
            int dataSize = reader.ReadInt32();

            reader.BaseStream.Seek(baseOffset, SeekOrigin.Begin);

            uint baseFlags = reader.ReadUInt32();
            int totalSubsongs = reader.ReadInt32();
            string wavebankName = reader.ReadNullTerminatedString(64);

            int entryElemSize = reader.ReadInt32();
            int metaNameEntrySize = reader.ReadInt32();

            int entryAlignment = reader.ReadInt32();
            uint format = (uint)reader.ReadInt32();
            long buildTime = reader.ReadInt64();

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

                uint bitsPerSample = (format >> 31) & 0x1; // Yes, this is only 1 bit
                uint blockAlign = (format >> 23) & 0xFF;  // 8 bytes
                uint sampleRate = /*(format >> 5) & 0x7FFFF; // 18 bytes*/ 44100; // For some reason, some vanilla tracks are 44,101 or 44,099. I don't know. They're all supposed to be 44.1 kHz.
                uint channels = (format >> 2) & 0x7; // 3 bytes
                uint tag = format & 0x3; // 2 bytes

                // This is technically what we should be doing, but ALL vanilla tracks
                // are Adpcm format. We can just skip over this for optimization purposes.
                //
                //WaveFormatEncoding encoding = tag switch
                //{
                //    0 => WaveFormatEncoding.Pcm,
                //    2 => WaveFormatEncoding.Adpcm,
                //    _ => throw new ArgumentException("Unknown audio codec type!", nameof(readers))
                //};

                // We can skip reading from the .xsb file because tMod has already done it for us :)
                string name = loadedSystem.TrackNamesByIndex[i];
                WaveFormat waveFormat = WaveFormat.CreateCustomFormat(WaveFormatEncoding.Adpcm, (int)sampleRate, (int)channels, (int)(blockAlign * sampleRate), (int)blockAlign, (int)(blockAlign / channels));

                long loopStart = loopStartSample > 0 ? loopStartSample : -1L;
                long loopEnd = loopEndSample > 0 && loopEndSample != numSamples ? loopEndSample : -1L;

                reader.BaseStream.Seek(streamOffset, SeekOrigin.Begin);
                WaveBankCue cue = new(reader, waveFormat, name, streamOffset, streamSize, numSamples, loopStart, loopEnd);
                cues.Add(cue);
            }

            return cues;
        }
    }
}
