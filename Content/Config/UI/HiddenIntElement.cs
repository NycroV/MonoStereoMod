using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader.Config.UI;

namespace MonoStereoMod.Config
{
    internal class HiddenIntElement : ConfigElement<int>
    {
        protected override void DrawSelf(SpriteBatch spriteBatch) { }

        public override void OnBind()
        {
            Width.Set(0, 0);
            Height.Set(0, 0);
        }

        public override bool ContainsPoint(Vector2 point)
        {
            return false;
        }
    }
}
