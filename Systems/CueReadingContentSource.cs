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
            string path = Main.instance.Content.GetPath("Wave Bank.xwb");
            Stream cueBankStream = File.OpenRead(path);
            BinaryReader reader = new(cueBankStream);

            // Verify the header of the WaveBank file to make
            // sure we're actually reading what we're supposed to.
            var head = Encoding.ASCII.GetBytes("WBND");
            var bytes = reader.ReadBytes(4);

            if (!head.SequenceEqual(bytes))
                throw new ArgumentException("Could not parse XWB header!", nameof(cueBankStream));

            // Yes, we do need readers for each individual track.
            BinaryReader[] readers = new BinaryReader[Main.maxMusic];
            readers[0] = reader;

            for (int i = 1; i < readers.Length; i++)
                readers[i] = new BinaryReader(File.OpenRead(path));

            // Read the cues (tracks) from the WaveBank, and create a dictionary
            // from those entries that is indexed by track name.
            Cues = ReadCues(readers, Main.audioSystem as LegacyAudioSystem).Select<WaveBankCue, KeyValuePair<string, WaveBankCue>>(cue => new("Music" + Path.DirectorySeparatorChar + cue.Name, cue)).ToDictionary();

            // Sets the asset names to include the file extension.
            SetAssetNames(Cues.Keys.Select(cue => cue + ".xwb"));
            readers[0].Close();

            // Impending doom approaches...
            string music1 = $"Music{Path.DirectorySeparatorChar}Music_1";
            string music3 = $"Music{Path.DirectorySeparatorChar}Music_3";

            // I have absolutely ZERO clue why, but tracks 1 and 3 seem to be swapped.
            // Like, seriously, I don't know how this isn't an issue in vanilla. The logic is IDENTICAL.
            // If anyone has any ideas or answers, please, for the love of God, fix this.
            //         |             |
            //         V             V
            (Cues[music1], Cues[music3]) =
            (Cues[music3], Cues[music1]);
        }

        public override Stream OpenStream(string assetName) => new CueReader(Cues[assetName]);
    }
}
