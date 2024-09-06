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

            string path = Main.instance.Content.GetPath("Wave Bank.xwb");
            Stream cueBankStream = File.OpenRead(path);
            BinaryReader reader = new(cueBankStream);

            var head = Encoding.ASCII.GetBytes("WBND");
            var bytes = reader.ReadBytes(4);

            if (!head.SequenceEqual(bytes))
                throw new ArgumentException("Could not parse XWB header!", nameof(cueBankStream));

            BinaryReader[] readers = new BinaryReader[Main.maxMusic];
            readers[0] = reader;

            for (int i = 1; i < readers.Length; i++)
                readers[i] = new BinaryReader(File.OpenRead(path));

            Cues = ReadCues(readers, Main.audioSystem as LegacyAudioSystem).Select<WaveBankCue, KeyValuePair<string, WaveBankCue>>(cue => new("Music" + Path.DirectorySeparatorChar + cue.Name, cue)).ToDictionary();

            // Only collects audio files that can be played, no other content type is included
            SetAssetNames(Cues.Keys.Select(cue => cue + ".xwb"));
            readers[0].Close();
        }

        public override Stream OpenStream(string assetName) => new CueReader(Cues[assetName]);
    }
}
