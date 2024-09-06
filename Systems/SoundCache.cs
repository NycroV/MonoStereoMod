using MonoStereo;
using MonoStereo.AudioSources.Sounds;
using MonoStereoMod.Systems;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MonoStereoMod
{
    internal static class SoundCache
    {
        internal static readonly Dictionary<string, CachedSoundEffect> FileCache = [];

        internal static readonly Dictionary<ActiveSound, MonoStereoSoundEffect> ActiveSounds = [];

        internal static MonoStereoSoundEffect Get(ActiveSound activeSound) => TryGet(activeSound, out var sound) ? sound : null;

        internal static bool TryGet(ActiveSound sound, out MonoStereoSoundEffect effect) => ActiveSounds.TryGetValue(sound, out effect);

        internal static void Set(ActiveSound sound, MonoStereoSoundEffect effect) => ActiveSounds[sound] = effect;

        internal static CachedSoundEffect Cache(string path, bool forceReload = false)
        {
            if (!forceReload && FileCache.TryGetValue(path, out CachedSoundEffect value))
                return value;

            SplitName(path, out string modName, out string subName);
            string assetName = AssetPathHelper.CleanPath(subName);

            if (modName == "Terraria")
            {
                using var stream = ((AssetRepository)Main.Assets).Sources().First(s => s.GetExtension(assetName) is not null).OpenStream(assetName + ".xnb");
                XnbSoundEffectReader reader = new(stream, path);
                value = reader.Read();
            }

            else if (ModLoader.TryGetMod(modName, out var mod))
            {
                var contentSource = mod.Assets.Sources().First(s => s.GetExtension(assetName) is not null);
                string extension = contentSource.GetExtension(assetName);

                using var stream = contentSource.OpenStream(assetName + extension);
                value = LoadSoundEffect(stream, path, extension);
            }

            else
                throw new Terraria.ModLoader.Exceptions.MissingResourceException(Language.GetTextValue("tModLoader.LoadErrorModNotFoundDuringAsset", modName, path));

            FileCache.Add(path, value);
            return value;
        }

        public static void CollectGarbage()
        {
            var kvps = ActiveSounds.ToArray();
            for (int i = 0; i < kvps.Length; ++i)
            {
                if (kvps[i].Value.IsDisposed)
                    ActiveSounds.Remove(kvps[i].Key);
            }
        }

        public static MonoStereoSoundEffect CreateInstance(this CachedSoundEffect soundEffect) => new(new CachedSoundEffectReader(soundEffect));
    }
}
