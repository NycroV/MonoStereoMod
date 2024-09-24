using MonoStereo.Outputs;
using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Terraria.ModLoader.Config;

namespace MonoStereoMod.Config
{
    internal class MonoStereoConfig : ModConfig
    {
        [JsonIgnore] private int latency = MonoStereoMod.Config.Latency;
        [JsonIgnore] private int bufferCount = MonoStereoMod.Config.BufferCount;
        [JsonIgnore] private int outputDevice = MonoStereoMod.Config.DeviceNumber;

        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Header("Playback")]
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
                    MonoStereoMod.Config.ResetOutput(latency: latency);
            }
        }

        [DefaultValue(8)]
        [Range(2, 16)]
        [Slider]
        public int BufferCount
        {
            get => bufferCount;
            set
            {
                bufferCount = value;

                if (MonoStereoMod.Config.BufferCount != bufferCount && MonoStereoMod.ModRunning)
                    MonoStereoMod.Config.ResetOutput(bufferCount: bufferCount);
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
                    outputDevice = HighPriorityWaveOutEvent.DeviceCount - 1;

                else if (value >= HighPriorityWaveOutEvent.DeviceCount)
                    outputDevice = -1;

                else
                    outputDevice = value;

                if (MonoStereoMod.Config.DeviceNumber != outputDevice && MonoStereoMod.ModRunning)
                    MonoStereoMod.Config.ResetOutput(deviceNumber: outputDevice);
            }
        }

#pragma warning disable CA1822 // Mark members as static
        [JsonIgnore]
        [CustomModConfigItem(typeof(CustomStringElement))]
        public string DeviceName => MonoStereoMod.Config.DeviceDisplayName; // Display value is calculated per-frame in the UI code
#pragma warning restore CA1822 // Mark members as static

        public override void OnChanged()
        {
            MonoStereoMod.Config.Latency = Latency;
            MonoStereoMod.Config.BufferCount = BufferCount;
            MonoStereoMod.Config.DeviceNumber = OutputDevice;
        }
    }
}
