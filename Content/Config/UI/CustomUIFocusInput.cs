using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Localization;
using Terraria.UI;

namespace MonoStereoMod.Config
{
    internal class CustomUIFocusInputTextField(string hintText) : UIElement
    {
        private readonly string _hintText = hintText;

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            string displayString = MonoStereoModAPI.Config.DeviceDisplayName;
            const int maxNameLength = 31;
            const int maxDisplayLength = 46;

            if (!MonoStereoModAPI.Config.DeviceAvailable)
            {
                string device = displayString;
                string unavailable = $"[{Language.GetTextValue("Mods.MonoStereoMod.Configs.MonoStereoConfig.DeviceUnavailable")}] ";

                int charsAvailable = maxDisplayLength - unavailable.Length;

                if (device.Length < charsAvailable)
                    displayString = unavailable + device;

                else
                    displayString = (unavailable + device)[..(maxDisplayLength - 3)] + "...";
            }

            else if (displayString.Length >= maxNameLength)
            {
                displayString = displayString[..maxNameLength] + "...";
            }

            CalculatedStyle space = GetDimensions();

            if (displayString.Length == 0)
                Terraria.Utils.DrawBorderString(spriteBatch, _hintText, new Vector2(space.X, space.Y), Color.Gray, anchorx: 1f);

            else
                Terraria.Utils.DrawBorderString(spriteBatch, displayString, new Vector2(space.Width + space.X, space.Y), Color.White, anchorx: 1f);
        }
    }
}
