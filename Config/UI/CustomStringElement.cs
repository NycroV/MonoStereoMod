using Terraria.Localization;
using Terraria.ModLoader.Config.UI;

namespace MonoStereoMod.Config
{
    internal class CustomStringElement : ConfigElement<string>
    {
        public override void OnBind()
        {
            base.OnBind();

            CustomUIPanel textBoxBackground = new();
            textBoxBackground.SetPadding(0);
            CustomUIFocusInputTextField uIInputTextField = new(Language.GetTextValue("tModLoader.ModConfigTypeHere"));
            textBoxBackground.Top.Set(0f, 0f);
            textBoxBackground.Left.Set(-385, 1f);
            textBoxBackground.Width.Set(375, 0f);
            textBoxBackground.Height.Set(30, 0f);

            Append(textBoxBackground);

            uIInputTextField.Top.Set(5, 0f);
            uIInputTextField.Left.Set(10, 0f);
            uIInputTextField.Width.Set(-20, 1f);
            uIInputTextField.Height.Set(20, 0);

            textBoxBackground.Append(uIInputTextField);
        }
    }
}
