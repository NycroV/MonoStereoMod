using System;
using System.Linq;
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

        private static readonly ReverseFilter filter = new();

        public override bool? UseItem(Player player)
        {
            var currentTrack = MonoStereoMod.GetSong(Main.curMusic);

            if (player.altFunctionUse == 2)
                currentTrack.AddFilter(filter);

            else
                filter.Reversing = !filter.Reversing;

            return true;
        }

        public override bool AltFunctionUse(Player player)
        {
            var currentTrack = MonoStereoMod.GetSong(Main.curMusic);
            return currentTrack is not null && !currentTrack.Filters.Contains(filter);
        }
    }
}
