using Microsoft.Xna.Framework;
using RescueFairies.Common.GlobalTiles;
using RescueFairies.Content.Items;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace RescueFairies.Content.Tiles;

/// <summary>
/// The pllaced tile of <see cref="PurpleFairyJar"/>.
/// </summary>
public sealed class PurpleFairyJarTile : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileLavaDeath[Type] = true;
		Main.tileLighted[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileFrameImportant[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(192, 104, 227), CreateMapEntryName());

		CritterCageAnimationTile.RegisterTile(Type, Main.fairyJarFrame);
		GlowMaskTile.RegisterTile(Type);

		VanillaFallbackOnModDeletion = TileID.PinkFairyJar;
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		r = 0.5f;
		g = 0.25f;
		b = 0.5f;
	}

	public override bool CreateDust(int i, int j, ref int type)
	{
		type = DustID.Glass;
		return WorldGen.genRand.NextBool(3);
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		Item.NewItem(new EntitySource_TileBreak(i, j), new Rectangle(i * 16, j * 16, 36, 36), ModContent.ItemType<PurpleFairyJar>());
	}
}