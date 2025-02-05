using Microsoft.Xna.Framework.Graphics;
using Terraria.Localization;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;

namespace MonoStereoMod.Config
{
    internal class CustomStringElement : ConfigElement<string>
    {
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            this.SetBackgroundColor(MonoStereoMod.Config.DeviceAvailable ? UICommon.DefaultUIBlue : new(150, 50, 50));
            base.DrawSelf(spriteBatch);
        }

        public override void OnBind()
        {
            base.OnBind();

            CustomUIPanel textBoxBackground = new();
            textBoxBackground.SetPadding(0);
            textBoxBackground.Top.Set(0f, 0f);
            textBoxBackground.Left.Set(-385, 1f);
            textBoxBackground.Width.Set(375, 0f);
            textBoxBackground.Height.Set(30, 0f);

            Append(textBoxBackground);

            CustomUIFocusInputTextField uIInputTextField = new(Language.GetTextValue("tModLoader.ModConfigTypeHere"));
            uIInputTextField.Top.Set(5, 0f);
            uIInputTextField.Left.Set(10, 0f);
            uIInputTextField.Width.Set(-20, 1f);
            uIInputTextField.Height.Set(20, 0);

            textBoxBackground.Append(uIInputTextField);
        }
    }
}
