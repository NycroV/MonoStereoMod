using Microsoft.Xna.Framework.Content;
using MonoStereo;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Collections.Generic;
using System.IO;

namespace MonoStereoMod.Systems
{
    internal class XnbSoundEffectReader(Stream inputStream, string fileName)
    {
        public readonly Stream Stream = inputStream;

        public readonly string FileName = fileName;

        public CachedSoundEffect Read()
        {
            byte[] xnbHeader = new byte[4];
            Stream.Read(xnbHeader, 0, xnbHeader.Length);

            if (xnbHeader[0] == 'X' &&
                xnbHeader[1] == 'N' &&
                xnbHeader[2] == 'B' &&
                targetPlatformIdentifiers.Contains((char)xnbHeader[3]))
            {
                char platform = (char)xnbHeader[3];

                #region Setup XnbReader

                using BinaryReader xnbReader = new(Stream);

                byte version = xnbReader.ReadByte();
                byte flags = xnbReader.ReadByte();
                bool compressed = (flags & 0x80) != 0;
                if (version != 5 && version != 4)
                {
                    throw new ContentLoadException("Invalid XNB version");
                }

                // The next int32 is the length of the XNB file
                Stream readStream = Stream;
                int xnbLength = xnbReader.ReadInt32();
                if (compressed)
                {
                    /* Decompress the XNB
                     * Thanks to ShinAli (https://bitbucket.org/alisci01/xnbdecompressor)
                     */
                    int compressedSize = xnbLength - 14;
                    int decompressedSize = xnbReader.ReadInt32();

                    // This will replace the XNB stream at the end
                    MemoryStream decompressedStream = new(
                        new byte[decompressedSize],
                        0,
                        decompressedSize,
                        true,
                        true // This MUST be true! Readers may need GetBuffer()!
                    );

                    /* Read in the whole XNB file at once, into a temp buffer.
                     * For slow disks, the extra malloc is more than worth the
                     * performance improvement from not constantly fread()ing!
                     */
                    MemoryStream compressedStream = new(
                        new byte[compressedSize],
                        0,
                        compressedSize,
                        true,
                        true
                    );

                    Stream.Read(compressedStream.GetBuffer(), 0, compressedSize);

                    // Default window size for XNB encoded files is 64Kb (need 16 bits to represent it)
                    var dec = LzxDecoder(16);
                    int decodedBytes = 0;
                    long pos = 0;

                    while (pos < compressedSize)
                    {
                        /* The compressed stream is separated into blocks that will
                         * decompress into 32kB or some other size if specified.
                         * Normal, 32kB output blocks will have a short indicating
                         * the size of the block before the block starts. Blocks
                         * that have a defined output will be preceded by a byte of
                         * value 0xFF (255), then a short indicating the output size
                         * and another for the block size. All shorts for these
                         * cases are encoded in big endian order.
                         */
                        int hi = compressedStream.ReadByte();
                        int lo = compressedStream.ReadByte();
                        int block_size = (hi << 8) | lo;
                        int frame_size = 0x8000; // Frame size is 32kB by default
                                                 // Does this block define a frame size?
                        if (hi == 0xFF)
                        {
                            hi = lo;
                            lo = (byte)compressedStream.ReadByte();
                            frame_size = (hi << 8) | lo;
                            hi = (byte)compressedStream.ReadByte();
                            lo = (byte)compressedStream.ReadByte();
                            block_size = (hi << 8) | lo;
                            pos += 5;
                        }
                        else
                        {
                            pos += 2;
                        }
                        // Either says there is nothing to decode
                        if (block_size == 0 || frame_size == 0)
                        {
                            break;
                        }

                        dec.LzxDecoderDecompress(compressedStream, block_size, decompressedStream, frame_size);
                        pos += block_size;
                        decodedBytes += frame_size;
                        /* Reset the position of the input just in case the bit
                         * buffer read in some unused bytes.
                         */
                        compressedStream.Seek(pos, SeekOrigin.Begin);
                    }

                    if (decompressedStream.Position != decompressedSize)
                    {
                        throw new ContentLoadException(
                            "Decompression of XnaAsset failed. "
                        );
                    }

                    decompressedStream.Seek(0, SeekOrigin.Begin);
                    readStream = decompressedStream;
                }

                #endregion

                #region Read XnbReader Audio Data

                using BinaryReader reader = new(readStream);
                int numberOfReaders = reader.Read7BitEncodedInt();

                for (int i = 0; i < numberOfReaders; i++)
                {
                    string originalReaderTypeString = reader.ReadString();

                    /* I think the next 4 bytes refer to the "Version" of the type reader,
                    * although it always seems to be zero.
                    */
                    reader.ReadInt32();
                }

                int sharedResourceCount = reader.Read7BitEncodedInt();
                int typeReaderindex = reader.Read7BitEncodedInt();

                // FINALLY
                // Everything below is the actual SoundEffect parsing

                /* Swap endian - this is one of the very few places requiring this!
                * Note: This only affects the fmt chunk that's glued into the file.
                */
                bool se = platform == 'x';

                // Format block length
                uint formatLength = reader.ReadUInt32();

                // WaveFormatEx data
                ushort wFormatTag = Swap(se, reader.ReadUInt16());
                ushort nChannels = Swap(se, reader.ReadUInt16());
                uint nSamplesPerSec = Swap(se, reader.ReadUInt32());
                uint nAvgBytesPerSec = Swap(se, reader.ReadUInt32());
                ushort nBlockAlign = Swap(se, reader.ReadUInt16());
                ushort wBitsPerSample = Swap(se, reader.ReadUInt16());

                byte[] extra = null;
                if (formatLength > 16)
                {
                    ushort cbSize = Swap(se, reader.ReadUInt16());

                    if (wFormatTag == 0x166 && cbSize == 34)
                    {
                        // XMA2 has got some nice extra crap.
                        extra = new byte[34];
                        using (MemoryStream extraStream = new(extra))
                        using (BinaryWriter extraWriter = new(extraStream))
                        {
                            // See FAudio.FAudioXMA2WaveFormatEx for the layout.
                            extraWriter.Write(Swap(se, reader.ReadUInt16()));
                            extraWriter.Write(Swap(se, reader.ReadUInt32()));
                            extraWriter.Write(Swap(se, reader.ReadUInt32()));
                            extraWriter.Write(Swap(se, reader.ReadUInt32()));
                            extraWriter.Write(Swap(se, reader.ReadUInt32()));
                            extraWriter.Write(Swap(se, reader.ReadUInt32()));
                            extraWriter.Write(Swap(se, reader.ReadUInt32()));
                            extraWriter.Write(Swap(se, reader.ReadUInt32()));
                            extraWriter.Write(reader.ReadByte());
                            extraWriter.Write(reader.ReadByte());
                            extraWriter.Write(Swap(se, reader.ReadUInt16()));
                        }
                        // Is there any crap that needs skipping? Eh whatever.
                        reader.ReadBytes((int)(formatLength - 18 - 34));
                    }
                    else
                    {
                        // Seek past the rest of this crap (cannot seek though!)
                        reader.ReadBytes((int)(formatLength - 18));
                    }
                }

                // Wavedata
                byte[] data = reader.ReadBytes(reader.ReadInt32());

                // Loop information
                int loopStart = reader.ReadInt32();
                int loopLength = reader.ReadInt32();

                // Sound duration in milliseconds, unused
                reader.ReadUInt32();

                #endregion

                WaveFormat sourceFormat = WaveFormat.CreateCustomFormat(
                    (WaveFormatEncoding)wFormatTag,
                    (int)nSamplesPerSec,
                    nChannels,
                    (int)nAvgBytesPerSec,
                    nBlockAlign,
                    wBitsPerSample);

                WaveStream waveProvider = new RawSourceWaveStream(data, 0, data.Length, sourceFormat);
                waveProvider = new BlockAlignReductionStream(waveProvider);
                ISampleProvider sampleProvider = waveProvider.ConvertWaveProviderIntoSampleProvider();

                Dictionary<string, string> comments = [];
                
                if (loopStart != 0)
                    comments.Add("LOOPSTART", loopStart.ToString());

                if (loopLength != 0 && loopLength != data.Length / sourceFormat.BlockAlign)
                    comments.Add("LOOPLENGTH", loopLength.ToString());

                return new CachedSoundEffect(sampleProvider, FileName, comments);
            }

            throw new ContentLoadException("Could not load " + FileName + " asset!");
        }

        private static readonly List<char> targetPlatformIdentifiers =
        [
            'w', // Windows (DirectX)
			'x', // Xbox360
			'm', // WindowsPhone
			'i', // iOS
			'a', // Android
			'd', // DesktopGL
			'X', // MacOSX
			'W', // WindowsStoreApp
			'n', // NativeClient
			'u', // Ouya
			'p', // PlayStationMobile
			'M', // WindowsPhone8
			'r', // RaspberryPi
			'P', // Playstation 4
			'g', // WindowsGL (deprecated for DesktopGL)
			'l', // Linux (deprecated for DesktopGL)
		];

        private static ushort Swap(bool swap, ushort x)
        {
            return !swap ? x : (ushort)(
                ((x >> 8) & 0x00FF) |
                ((x << 8) & 0xFF00)
            );
        }

        private static uint Swap(bool swap, uint x)
        {
            return !swap ? x : (
                ((x >> 24) & 0x000000FF) |
                ((x >> 8) & 0x0000FF00) |
                ((x << 8) & 0x00FF0000) |
                ((x << 24) & 0xFF000000)
            );
        }
    }
}
