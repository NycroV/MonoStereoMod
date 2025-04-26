using MonoStereo;
using MonoStereoMod.Systems;
using NAudio.Wave;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace MonoStereoMod.Utils
{
    internal static partial class MonoStereoUtils
    {
        public static void LoadPortAudio()
        {
            if (!NativeLibrary.TryLoad("portaudio", out _))
            {
                string platform = "win";
                string file = "portaudio.dll";

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    platform = "linux";
                    file = "libportaudio.so";
                }

                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    platform = "osx";
                    file = "libportaudio.dylib";
                }

                string embeddedFile = $"MonoStereoMod/lib/portaudio/{platform}/{file}";
                string outputDirectory = string.Join(Path.DirectorySeparatorChar, Main.SavePath, "MonoStereoMod", "portaudio", "19.7.0");
                string outputFile = $"{outputDirectory}{Path.DirectorySeparatorChar}{file}";

                // Ensure the directory exists
                if (!Directory.Exists(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);

                // Copy the platform-specific file over to the destination directory.
                if (!File.Exists(outputFile))
                {
                    var bytes = ModContent.GetFileBytes(embeddedFile);
                    File.WriteAllBytes(outputFile, bytes);
                }

                // Load the PortAudio library.
                NativeLibrary.Load(outputFile);
            }
        }

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
        internal static List<WaveBankCue> ReadCues(BinaryReader waveBankReader, BinaryReader soundBankReader, BinaryReader[] cueReaders, LegacyAudioSystem loadedSystem)
        {
            #region Wave Bank Parsing

            int version = waveBankReader.ReadInt32(); // 46 - XACT 3.0
            int headerVersion = waveBankReader.ReadInt32();

            int baseOffset = waveBankReader.ReadInt32(); // Bank data
            int baseSize = waveBankReader.ReadInt32();
            int entryOffset = waveBankReader.ReadInt32(); // Entry name data
            int entrySize = waveBankReader.ReadInt32();

            int extraOffset = waveBankReader.ReadInt32(); // Seektables
            int extraSize = waveBankReader.ReadInt32();
            int namesOffset = waveBankReader.ReadInt32(); // Entry names
            int namesSize = waveBankReader.ReadInt32();

            uint dataOffset = (uint)waveBankReader.ReadInt32();
            int dataSize = waveBankReader.ReadInt32();

            waveBankReader.BaseStream.Seek(baseOffset, SeekOrigin.Begin);

            uint baseFlags = waveBankReader.ReadUInt32();
            int totalSubsongs = waveBankReader.ReadInt32();
            string wavebankName = waveBankReader.ReadNullTerminatedString(64);

            int entryElemSize = waveBankReader.ReadInt32();
            int metaNameEntrySize = waveBankReader.ReadInt32();

            int entryAlignment = waveBankReader.ReadInt32();
            uint format = (uint)waveBankReader.ReadInt32();
            long buildTime = waveBankReader.ReadInt64();

            #endregion

            #region Sound Bank Parsing

            soundBankReader.ReadUInt16(); // toolVersion

            uint formatVersion = soundBankReader.ReadUInt16();
            if (formatVersion != 43)
                Debug.WriteLine("Warning: SoundBank format {0} not supported.", formatVersion);

            soundBankReader.ReadUInt16(); // crc

            soundBankReader.ReadUInt32(); // lastModifiedLow
            soundBankReader.ReadUInt32(); // lastModifiedHigh
            soundBankReader.ReadByte(); // platform ???

            soundBankReader.ReadUInt16(); // numSimpleCues
            soundBankReader.ReadUInt16(); // numComplexCues
            soundBankReader.ReadUInt16(); //unkn
            soundBankReader.ReadUInt16(); // numTotalCues
            soundBankReader.ReadByte(); // numWaveBanks
            soundBankReader.ReadUInt16(); // numSounds
            uint cueNameTableLen = soundBankReader.ReadUInt16();
            soundBankReader.ReadUInt16(); //unkn

            soundBankReader.ReadUInt32(); // simpleCuesOffset
            soundBankReader.ReadUInt32(); //complexCuesOffset
            uint cueNamesOffset = soundBankReader.ReadUInt32();
            //soundBankReader.ReadUInt32(); //unkn
            //soundBankReader.ReadUInt32(); // variationTablesOffset
            //soundBankReader.ReadUInt32(); //unkn
            //uint waveBankNameTableOffset = soundBankReader.ReadUInt32();
            //soundBankReader.ReadUInt32(); // cueNameHashTableOffset
            //soundBankReader.ReadUInt32(); // cueNameHashValsOffset
            //soundBankReader.ReadUInt32(); // soundsOffset

            //parse cue name table
            soundBankReader.BaseStream.Seek(cueNamesOffset, SeekOrigin.Begin);
            string[] cueNames = System.Text.Encoding.UTF8.GetString(soundBankReader.ReadBytes((int)cueNameTableLen), 0, (int)cueNameTableLen).Split('\0');

            #endregion

            List<WaveBankCue> cues = [];

            for (int i = 0; i < Main.maxMusic - 1; i++)
            {
                var reader = cueReaders[i];
                reader.BaseStream.Seek(entryOffset + i * entryElemSize, SeekOrigin.Begin);

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

                string name = cueNames[i];
                WaveFormat waveFormat = WaveFormat.CreateCustomFormat(WaveFormatEncoding.Adpcm, (int)sampleRate, (int)channels, (int)(blockAlign * sampleRate), (int)blockAlign, (int)(blockAlign / channels));

                long loopStart = loopStartSample > 0 ? loopStartSample * AudioStandards.ChannelCount : -1L;
                long loopEnd = loopEndSample > 0 && loopEndSample != numSamples ? loopEndSample * AudioStandards.ChannelCount : -1L;

                reader.BaseStream.Seek(streamOffset, SeekOrigin.Begin);
                WaveBankCue cue = new(reader, waveFormat, name, streamOffset, streamSize, numSamples, loopStart, loopEnd);
                cues.Add(cue);
            }

            return cues;
        }
    }
}
