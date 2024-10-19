using MonoStereo.Filters;
using MonoStereo.SampleProviders;
using System;
using System.Linq;

namespace MonoStereoMod.Testing
{
    public class AudioTimeController
    {
        private readonly SpeedChangeFilter speedFilter = new();
        private readonly ReverseFilter reverseFilter = new();
        private float timeSpeed = 1f;

        public float TimeSpeed
        {
            get => timeSpeed;
            set
            {
                if (timeSpeed == value)
                    return;

                if (value != 0f)
                    reverseFilter.Reversing = value < 0f;

                speedFilter.Speed = Math.Abs(value);
                timeSpeed = value;
            }
        }

        public void ApplyTo(MonoStereoProvider provider)
        {
            if (provider.Filters.Contains(reverseFilter))
                return;

            provider.AddFilter(speedFilter);
            provider.AddFilter(reverseFilter);
        }

        public void RemoveFrom(MonoStereoProvider provider)
        {
            while (provider.Filters.Contains(reverseFilter))
            {
                provider.RemoveFilter(speedFilter);
                provider.RemoveFilter(reverseFilter);
            }
        }

        public bool IsAppliedTo(MonoStereoProvider provider) => provider.Filters.Contains(reverseFilter);
    }
}
