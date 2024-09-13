using MonoStereo;
using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Terraria.ModLoader.Config;

namespace MonoStereoMod.Config
{
    public class MonoStereoConfig : ModConfig
    {
        [JsonIgnore] public static int LatencyConfig { get; private set; } = 150;
        [JsonIgnore] public static int DeviceNumberConfig { get; private set; } = -1;
        [JsonIgnore] public static string DeviceDisplayName { get => AudioManager.GetCapabilities(DeviceNumberConfig).ProductName; }

        [JsonIgnore] private int latency = LatencyConfig;
        [JsonIgnore] private int outputDevice = DeviceNumberConfig;

        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Header("Latency")]
        [DefaultValue(150)]
        [Range(50, 250)]
        [Slider]
        public int Latency
        {
            get => latency;
            set
            {
                latency = value;

                if (LatencyConfig != latency && MonoStereoMod.ModRunning)
                {
                    AudioManager.ResetOutput(latency);
                    LatencyConfig = latency;
                }
            }
        }

        [Header("Output")]
        [DefaultValue(-1)]
        [Range(-2, 100)]
        public int OutputDevice
        {
            get => outputDevice;
            set
            {
                if (value == -2)
                    outputDevice = AudioManager.DeviceCount - 1;

                else if (value >= AudioManager.DeviceCount)
                    outputDevice = -1;

                else
                    outputDevice = value;

                if (DeviceNumberConfig != outputDevice && MonoStereoMod.ModRunning)
                {
                    AudioManager.ResetOutput(LatencyConfig, outputDevice);
                    DeviceNumberConfig = outputDevice;
                }
            }
        }

        [JsonIgnore]
        [CustomModConfigItem(typeof(CustomStringElement))]
        public string DeviceName; // Display value is calculated per-frame in the UI code

        public override void OnChanged()
        {
            LatencyConfig = Latency;
            DeviceNumberConfig = OutputDevice;
        }
    }
}
