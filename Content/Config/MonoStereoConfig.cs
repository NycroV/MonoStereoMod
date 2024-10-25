using MonoStereo.Outputs;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace MonoStereoMod.Config
{
    internal class MonoStereoConfig : ModConfig
    {
        [JsonIgnore] private bool forceHighPerformance;
        [JsonIgnore] private int[] outputIndexes; // This holds all of the actual output devices, not including input devices.
        private int deviceNumber; // This IS the actual device number.

        public MonoStereoConfig()
        {
            forceHighPerformance = MonoStereoMod.Config.ForceHighPerformance;
            outputIndexes = LoadOutputIndexes();
            deviceNumber = MonoStereoMod.Config.DeviceNumber;
        }

        public override ConfigScope Mode => ConfigScope.ClientSide;


        private static int[] LoadOutputIndexes() => MonoStereoMod.ModRunning ? PortAudioOutput.GetOutputDeviceIndexes() : null;

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
        [ShowDespiteJsonIgnore]
        [JsonIgnore]
        public int OutputIndex
        {
            get
            {
                if (!MonoStereoMod.ModRunning)
                    return -1;

                outputIndexes ??= LoadOutputIndexes();
                return Array.IndexOf(outputIndexes, deviceNumber);
            }

            set
            {
                if (!MonoStereoMod.ModRunning)
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

                if (MonoStereoMod.Config.DeviceNumber != deviceNumber)
                    MonoStereoMod.Config.OutputLock.Execute(() => MonoStereoMod.Config.ResetOutput(deviceNumber));
            }
        }

        [CustomModConfigItem(typeof(CustomStringElement))]
        [ShowDespiteJsonIgnore]
        [JsonIgnore]
        public string DeviceName { get; } // Display value is calculated per-frame in the UI code

        public override void OnChanged()
        {
            MonoStereoMod.Config.ForceHighPerformance = ForceHighPerformance;
            MonoStereoMod.Config.DeviceNumber = deviceNumber;
        }
    }
}
