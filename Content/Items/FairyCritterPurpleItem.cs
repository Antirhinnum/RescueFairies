using RescueFairies.Content.NPCs;
using Terraria;
using Terraria.Enums;
using Terraria.ModLoader;

namespace RescueFairies.Content.Items;

/// <summary>
/// The caught item version of <see cref="FairyCritterPurple"/>.
/// </summary>
public sealed class FairyCritterPurpleItem : ModItem
{
	public override void SetStaticDefaults()
	{
		//Main.RegisterItemAnimation(Type, new DrawAnimationVertical(6, 4)
		//{
		//	NotActuallyAnimating = true
		//});
	}

	public override void SetDefaults()
	{
		Item.DefaultToCapturedCritter(ModContent.NPCType<FairyCritterPurple>());
		Item.SetShopValues(ItemRarityColor.Blue1, Item.sellPrice(gold: 1));
	}
}