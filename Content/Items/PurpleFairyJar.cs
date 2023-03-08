using RescueFairies.Content.Tiles;
using Terraria.ID;
using Terraria.ModLoader;

namespace RescueFairies.Content.Items;

/// <summary>
/// A placeable jar version of <see cref="FairyCritterPurpleItem"/>.
/// </summary>
public sealed class PurpleFairyJar : ModItem
{
	public override void SetDefaults()
	{
		Item.Size = new(20, 20);
		Item.DefaultToPlaceableTile(ModContent.TileType<PurpleFairyJarTile>());
		Item.maxStack = 99;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient(ItemID.Bottle)
			.AddIngredient<FairyCritterPurpleItem>()
			.Register();
	}
}