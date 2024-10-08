﻿using Terraria;
using Terraria.ModLoader;

namespace MonoStereoMod.Testing
{
    internal class DebugPlayer : ModPlayer
    {
        public static readonly AudioTimeController timeController = new();

        public bool slowingDown = false;
        public bool reversed = false;
        public int slowdownFrame = slowdownTime;
        public const int slowdownTime = 90;

        public static void Apply()
        {
            var currentTrack = MonoStereoMod.GetSong(Main.curMusic);
            timeController.ApplyTo(currentTrack);
        }

        public override void PostUpdate()
        {
            if (slowingDown)
            {
                if (--slowdownFrame <= 0)
                {
                    reversed = !reversed;
                    slowingDown = false;
                    slowdownFrame = 0;
                }
            }

            else
            {
                if (slowdownFrame < slowdownTime)
                    slowdownFrame++;
            }

            timeController.TimeSpeed = slowdownFrame / (float)slowdownTime * (reversed ? -1f : 1f);
        }
    }
}
