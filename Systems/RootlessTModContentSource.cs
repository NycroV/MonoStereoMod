using ReLogic.Content.Sources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Initializers;
using Terraria.ModLoader.Core;
using Terraria;

namespace MonoStereoMod.Systems
{
    internal class RootlessTmodContentSource : ContentSource
    {
        private readonly TmodFile file;

        internal RootlessTmodContentSource(TmodFile file)
        {
            this.file = file ?? throw new ArgumentNullException(nameof(file));

            // Skip loading assets if this is a dedicated server
            if (Main.dedServ)
                return;

            // Filter assets based on the current reader set. Custom mod asset readers will need to be added before content sources are initialized
            // Unfortunately this means that if a reader is missing, the asset will be missing, causing a misleading error message, but there's little
            // we can do about that while still supporting multiple files with the same extension. Unless we provided a hardcoded exclusion for .cs files...
            SetAssetNames(file.Select(fileEntry => DeRoot(fileEntry.Name)));
        }

        private static string DeRoot(string file)
        {
            if (file.StartsWith("MonoStereo", StringComparison.OrdinalIgnoreCase))
                file = file["MonoStereo".Length..];

            while (file.StartsWith('\\') || file.StartsWith('/'))
                file = file[1..];

            return file;
        }

        public override Stream OpenStream(string assetName) => file.GetStream("MonoStereo/" + assetName, newFileStream: true); //todo, might be sloww
    }
}
