using MonoStereoMod.Audio;
using ReLogic.Content.Sources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.Audio;

namespace MonoStereoMod.Systems
{
    internal class CueReadingContentSource : ContentSource
    {
        public readonly Dictionary<string, WaveBankCue> Cues = [];

        internal CueReadingContentSource()
        {
            // Skip loading assets if this is a dedicated server
            if (Main.dedServ)
                return;

            // Open the reader stream for the WaveBank file
            string waveBankPath = Main.instance.Content.GetPath("Wave Bank.xwb");
            Stream waveBankStream = File.OpenRead(waveBankPath);
            BinaryReader waveBankReader = new(waveBankStream);

            // Verify the header of the WaveBank file to make
            // sure we're actually reading what we're supposed to.
            var head = Encoding.ASCII.GetBytes("WBND");
            var bytes = waveBankReader.ReadBytes(4);

            if (!head.SequenceEqual(bytes))
                throw new ArgumentException("Could not parse XWB header!", nameof(waveBankStream));

            // Open the reader stream for the SoundBank file (file names)
            string bankPath = Main.instance.Content.GetPath("Sound Bank.xsb");
            Stream soundBankStream = File.OpenRead(bankPath);
            BinaryReader soundBankReader = new(soundBankStream);

            // Verify the header.
            head = Encoding.ASCII.GetBytes("SDBK");
            bytes = soundBankReader.ReadBytes(4);

            if (!head.SequenceEqual(bytes))
                throw new ArgumentException("Could not parse XSB header!", nameof(soundBankStream));

            // Yes, we do need readers for each individual track.
            // maxMusic is 92 - but there are only 91 actual tracks.
            BinaryReader[] readers = new BinaryReader[Main.maxMusic - 1];

            for (int i = 0; i < readers.Length; i++)
                readers[i] = new BinaryReader(File.OpenRead(waveBankPath));

            // Read the cues (tracks) from the WaveBank, and create a dictionary
            // from those entries that is indexed by track name.
            Cues = ReadCues(waveBankReader, soundBankReader, readers, Main.audioSystem as LegacyAudioSystem).Select<WaveBankCue, KeyValuePair<string, WaveBankCue>>(cue => new("Music" + Path.DirectorySeparatorChar + cue.Name, cue)).ToDictionary();

            // Sets the asset names to include the file extension.
            SetAssetNames(Cues.Keys.Select(cue => cue + ".xwb"));

            // Dispose info streams.
            waveBankReader.Close();
            soundBankReader.Close();
        }

        public override Stream OpenStream(string assetName) => new CueReader(Cues[assetName]);
    }
}
