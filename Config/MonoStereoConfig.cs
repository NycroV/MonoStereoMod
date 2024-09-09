using MonoStereo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader.Config;

namespace MonoStereoMod.Config
{
    internal class MonoStereoConfig : ModConfig
    {
        public static int LatencyConfig = 150;

        public static int DeviceNumberConfig = -1;

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
                    UpdateDeviceDisplayName();
                    DeviceNumberConfig = outputDevice;
                }
            }
        }

        private int latency = LatencyConfig;
        private int outputDevice = DeviceNumberConfig;

        [JsonIgnore]
        public string DeviceName => AudioManager.GetCapabilities(outputDevice).ProductName;

        public override void OnChanged()
        {
            LatencyConfig = Latency;
            DeviceNumberConfig = OutputDevice;
        }

        private void UpdateDeviceDisplayName()
        {
            // I know this is scuffed, but for some reason UI strings do not
            // update automatically. Here we just navigate through the UI to the correct
            // element and manually re-assign the name.
            //
            // This hasn't caused any issues for me so far, but if it begins to I will
            // use more dynamic search funtions.
            Main.MenuUI.CurrentState.Children.First()
                .Children.ElementAt(5)
                .Children.First()
                .Children.First()
                .Children.Last()
                .Children.First()
                .Children.First()
                .Children.First()
                .SetCurrentString(DeviceName);
        }
    }
}
