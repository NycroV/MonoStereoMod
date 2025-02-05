using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MonoStereoMod.Testing
{
    internal class DebugItem : ModItem
    {
        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.TiedEighthNote}";

        public override void SetDefaults()
        {
            Item.useStyle = ItemUseStyleID.Swing;
            Item.width = 22;
            Item.height = 24;
            Item.maxStack = Item.CommonMaxStack;
            Item.UseSound = SoundID.Item1;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = Item.buyPrice(0, 0, 20, 0);
            Item.rare = ItemRarityID.Blue;
        }

        public override bool? UseItem(Player player)
        {
            var currentTrack = MonoStereoMod.GetSong(Main.curMusic);

            if (currentTrack.Pitch == 0f)
                currentTrack.Pitch = -1f;

            else if (currentTrack.Pitch == -1f)
                currentTrack.Pitch = 1f;

            else if (currentTrack.Pitch == 1f)
                currentTrack.Pitch = 0f;

            return true;
        }
    }
}
