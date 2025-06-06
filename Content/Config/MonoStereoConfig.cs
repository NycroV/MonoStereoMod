using MonoStereo;
using MonoStereo.Outputs;
using Newtonsoft.Json;
using PortAudioSharp;
using System;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace MonoStereoMod.Config
{
    internal class MonoStereoConfig : ModConfig
    {
        [JsonIgnore] private int[] outputIndexes; // This holds all of the actual output devices, not including input devices.

        // There is a custom UI element here to hide these fields from the user.
        // They are not meant to be manually changed, only accessed through the wrapping properties.
        // However, making them private excludes them from json serialization.
        // This workaround ensures these values are serialized, but still only modified by the wrapping properties.
        // These three fields are actually the only objects that are serialized; none of the properties are.

        [CustomModConfigItem(typeof(HiddenBoolElement))][DefaultValue(false)] public bool forceHighPerformance;
        [CustomModConfigItem(typeof(HiddenFloatElement))][DefaultValue(-1)] public float latency;
        [CustomModConfigItem(typeof(HiddenIntElement))][DefaultValue(-1)] public int deviceNumber; // This IS the actual device number.

        public MonoStereoConfig()
        {
            outputIndexes = LoadOutputIndexes();
        }

        public override ConfigScope Mode => ConfigScope.ClientSide;

        private static int[] LoadOutputIndexes() => MonoStereoModAPI.ModRunning ? PortAudioOutput.GetOutputDeviceIndexes() : null;

        [Header("Playback")]
        [ReloadRequired]
        [ShowDespiteJsonIgnore]
        [JsonIgnore]
        public bool ForceHighPerformance
        {
            get => forceHighPerformance;
            set => forceHighPerformance = value;
        }

        [Header("Output")]
        [ShowDespiteJsonIgnore]
        [JsonIgnore]
        [Increment(0.001f)]
        [Range(0.001f, 0.3f)]
        public float Latency
        {
            get
            {
                if (!MonoStereoModAPI.ModRunning)
                    return 0;

                if (latency == -1f && MonoStereoEngine.Output is PortAudioOutput portaudio && (portaudio.PlaybackStream?.outputParameters.HasValue ?? false))
                {
                    int? device = deviceNumber >= 0 ? deviceNumber : null;
                    var deviceInfo = PortAudio.GetDeviceInfo(device ?? PortAudio.DefaultOutputDevice);
                    return (float)deviceInfo.defaultLowOutputLatency;
                }

                return latency;
            }

            set
            {
                if (!MonoStereoModAPI.ModRunning)
                    return;

                latency = (float)Math.Round(value, 3);
                MonoStereoModAPI.Config.ResetOutput(latency: latency);
            }
        }

        [Range(-2, 100)]
        [ShowDespiteJsonIgnore]
        [JsonIgnore]
        public int OutputIndex
        {
            get
            {
                if (!MonoStereoModAPI.ModRunning)
                    return -1;

                outputIndexes ??= LoadOutputIndexes();
                return Array.IndexOf(outputIndexes, deviceNumber);
            }

            set
            {
                if (!MonoStereoModAPI.ModRunning)
                    return;

                outputIndexes ??= LoadOutputIndexes();

                // Loop to the end of the array if the value is -2.
                // -1 means to use the default device.
                if (value < -1)
                    deviceNumber = outputIndexes[^1];

                // Loop to -1 if going outside the array's bounds.
                // -1 means to use the default device.
                else if (value >= outputIndexes.Length || value == -1)
                    deviceNumber = -1;

                else
                    deviceNumber = outputIndexes[value];

                MonoStereoModAPI.Config.ResetOutput(deviceNumber: deviceNumber);
            }
        }

        [CustomModConfigItem(typeof(CustomStringElement))]
        [ShowDespiteJsonIgnore]
        [JsonIgnore]
        public string DeviceName { get; } // Display value is calculated per-frame in the UI code

        public override void OnChanged()
        {
            MonoStereoModAPI.Config.ForceHighPerformance = ForceHighPerformance;
            MonoStereoModAPI.Config.DeviceNumber = deviceNumber;
            MonoStereoModAPI.Config.Latency = latency;
        }
    }
}
