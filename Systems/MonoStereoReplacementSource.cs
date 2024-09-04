using ReLogic.Content.Sources;
using System;
using System.IO;
using System.Linq;
using Terraria.ModLoader.Core;
using Terraria;

namespace MonoStereoMod.Systems
{
    /// <summary>
    /// This is essentially the same as a tMod content source,<br/>
    /// but it only collects supported audio files.
    /// </summary>
    internal class MonoStereoReplacementSource : ContentSource
    {
        private readonly TmodFile file;

        internal MonoStereoReplacementSource(TmodFile file)
        {
            this.file = file ?? throw new ArgumentNullException(nameof(file));

            // Skip loading assets if this is a dedicated server
            if (Main.dedServ)
                return;

            // Only collects audio files that can be played, no other content type is included
            SetAssetNames(file.Select(fileEntry => fileEntry.Name).Where(fileEntry => Path.GetExtension(fileEntry).IsSupported()));
        }

        public override Stream OpenStream(string assetName) => file.GetStream(assetName, newFileStream: true); //todo, might be sloww
    }
}
