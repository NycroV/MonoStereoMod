using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria.UI;
using Terraria.Localization;

namespace MonoStereoMod.Config
{
    internal class CustomUIFocusInputTextField(string hintText) : UIElement
    {
        private readonly string _hintText = hintText;

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            string displayString = MonoStereoConfig.DeviceDisplayName;

            if (displayString == "Microsoft Sound Mapper")
                displayString = Language.GetTextValue("Mods.MonoStereoMod.Configs.MonoStereoConfig.DeviceDefault");

            if (displayString.Length >= 31)
                displayString += "...";

            CalculatedStyle space = GetDimensions();

            if (displayString.Length == 0)
                Terraria.Utils.DrawBorderString(spriteBatch, _hintText, new Vector2(space.X, space.Y), Color.Gray, anchorx: 1f);

            else
                Terraria.Utils.DrawBorderString(spriteBatch, displayString, new Vector2(space.Width + space.X, space.Y), Color.White, anchorx: 1f);
        }
    }
}
