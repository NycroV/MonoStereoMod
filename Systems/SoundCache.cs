using MonoStereo;
using MonoStereo.AudioSources.Sounds;
using MonoStereo.Encoding;
using MonoStereoMod.Systems;
using NAudio.Wave;
using System;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MonoStereoMod
{
    public static class SoundCache
    {
        private static readonly Dictionary<string, CachedSoundEffect> FileCache = [];

        public static readonly Dictionary<ActiveSound, TerrariaSoundEffect> ActiveSounds = [];

        public static TerrariaSoundEffect Get(ActiveSound sound) => ActiveSounds.TryGetValue(sound, out var effect) ? effect : null;

        public static void Set(ActiveSound sound, TerrariaSoundEffect effect) => ActiveSounds[sound] = effect;

        public static CachedSoundEffect Cache(string path, bool forceReload = false)
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
                        sampleProvider = wav.ConvertWaveProviderIntoSampleProvider();
                        break;

                    case ".mp3":
                        comments = stream.ReadComments();
                        Mp3FileReader mp3 = new(stream);
                        sampleProvider = mp3.ConvertWaveProviderIntoSampleProvider();
                        break;

                    default:
                        throw new NotSupportedException("Audio file type is not supported: " + extension);
                }

                value = new CachedSoundEffect(sampleProvider, path, comments);
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

        public static TerrariaSoundEffect CreateInstance(this CachedSoundEffect soundEffect) => new(new CachedSoundEffectReader(soundEffect));
    }
}
