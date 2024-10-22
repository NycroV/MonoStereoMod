using MonoStereo.Outputs;
using PortAudioSharp;
using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Terraria.ModLoader.Config;

namespace MonoStereoMod.Config
{
    internal class MonoStereoConfig : ModConfig
    {
        [JsonIgnore] private bool forceHighPerformance = MonoStereoMod.Config.ForceHighPerformance;
        [JsonIgnore] private int outputDevice = MonoStereoMod.Config.DeviceNumber;

        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Header("Playback")]
        [DefaultValue(false)]
        [ReloadRequired]
        public bool ForceHighPerformance
        {
            get => forceHighPerformance;
            set => forceHighPerformance = value;
        }

        [Header("Output")]
        [DefaultValue(-1)]
        [Range(-2, 100)]
        public int OutputDevice
        {
            get => outputDevice;
            set
            {
                if (!MonoStereoMod.ModRunning)
                {
                    outputDevice = value;
                    return;
                }

                if (value < -1)
                    outputDevice = PortAudio.DeviceCount - 1;

                else if (value >= PortAudio.DeviceCount)
                    outputDevice = -1;

                else
                    outputDevice = value;

                if (MonoStereoMod.Config.DeviceNumber != outputDevice)
                    MonoStereoMod.Config.ResetOutput(outputDevice);
            }
        }

#pragma warning disable CA1822 // Mark members as static
        [JsonIgnore]
        [CustomModConfigItem(typeof(CustomStringElement))]
        public string DeviceName => MonoStereoMod.Config.DeviceDisplayName; // Display value is calculated per-frame in the UI code
#pragma warning restore CA1822 // Mark members as static

        public override void OnChanged()
        {
            MonoStereoMod.Config.ForceHighPerformance = ForceHighPerformance;
            MonoStereoMod.Config.DeviceNumber = OutputDevice;
        }
    }
}
