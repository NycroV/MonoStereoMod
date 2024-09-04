using MonoStereo;
using MonoStereo.Encoding;
using NAudio.Wave;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace MonoStereoMod.Utils
{
    internal static partial class MonoStereoUtils
    {
        public static CachedSoundEffect LoadSoundEffect(Stream stream, string fileName, string extension)
        {
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

            return new CachedSoundEffect(sampleProvider, fileName, comments);
        }
    }
}
