using MonoStereo;
using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Terraria.ModLoader.Config;

namespace MonoStereoMod.Config
{
    internal class MonoStereoConfig : ModConfig
    {
        [JsonIgnore] private int latency = MonoStereoMod.Config.Latency;
        [JsonIgnore] private int outputDevice = MonoStereoMod.Config.DeviceNumber;

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

                if (MonoStereoMod.Config.Latency != latency && MonoStereoMod.ModRunning)
                {
                    AudioManager.ResetOutput(latency);
                    MonoStereoMod.Config.Latency = latency;
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

                if (MonoStereoMod.Config.DeviceNumber != outputDevice && MonoStereoMod.ModRunning)
                {
                    AudioManager.ResetOutput(MonoStereoMod.Config.Latency, outputDevice);
                    MonoStereoMod.Config.DeviceNumber = outputDevice;
                }
            }
        }

        [JsonIgnore]
        [CustomModConfigItem(typeof(CustomStringElement))]
        public string DeviceName => MonoStereoMod.Config.DeviceDisplayName; // Display value is calculated per-frame in the UI code

        public override void OnChanged()
        {
            MonoStereoMod.Config.Latency = Latency;
            MonoStereoMod.Config.DeviceNumber = OutputDevice;
        }
    }
}
