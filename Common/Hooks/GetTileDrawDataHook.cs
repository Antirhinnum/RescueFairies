using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Terraria;
using Terraria.GameContent.Drawing;
using Terraria.ModLoader;
using On_TileDrawing = On.Terraria.GameContent.Drawing.TileDrawing;

namespace RescueFairies.Common.Hooks;

/// <summary>
/// A consolidated version of <see cref="ModTile.SetSpriteEffects(int, int, ref SpriteEffects)"/>, <see cref="ModTile.SetDrawPositions(int, int, ref int, ref int, ref int, ref short, ref short)"/>, and <see cref="ModTile.AnimateIndividualTile(int, int, int, ref int, ref int)"/>. Usable on any <see cref="ILoadable"/>.
/// </summary>
internal sealed class GetTileDrawDataHook : ModSystem
{
	// Using a Patch<T> here mysteriously causes an IL error, so normal patching is done.

	//internal sealed class GetTileDrawDataPatch : Patch<GetTileDrawDataPatch.GetTileDrawData>
	//{
	//	internal delegate void GetTileDrawData(On_TileDrawing.orig_GetTileDrawData orig, TileDrawing self, int x, int y, Tile tileCache, ushort typeCache, ref short tileFrameX, ref short tileFrameY, out int tileWidth, out int tileHeight, out int tileTop, out int halfBrickHeight, out int addFrX, out int addFrY, out SpriteEffects tileSpriteEffect, out Texture2D glowTexture, out Rectangle glowSourceRect, out Color glowColor);

	//	public override MethodBase ModifiedMethod { get; } = typeof(TileDrawing).GetCachedMethod(nameof(TileDrawing.GetTileDrawData));

	//	protected override GetTileDrawData PatchMethod { get; } = RunHooks;
	//}

	/// <inheritdoc cref="IHookGetTileDrawData.ModifyTileDrawData(int, int, Tile, ushort, ref short, ref short, ref int, ref int, ref int, ref int, ref int, ref int, ref SpriteEffects, ref Texture2D, ref Rectangle, ref Color)"/>
	private delegate void HookModifyTileDrawData(int x, int y, Tile tileCache, ushort typeCache, ref short tileFrameX, ref short tileFrameY, ref int tileWidth, ref int tileHeight, ref int tileTop, ref int halfBrickHeight, ref int addFrX, ref int addFrY, ref SpriteEffects tileSpriteEffect, ref Texture2D glowTexture, ref Rectangle glowSourceRect, ref Color glowColor);

	private static HookModifyTileDrawData[] Hooks;

	public override void Load()
	{
		On_TileDrawing.GetTileDrawData += RunHooks;
	}

	private static void RunHooks(On_TileDrawing.orig_GetTileDrawData orig, TileDrawing self, int x, int y, Tile tileCache, ushort typeCache, ref short tileFrameX, ref short tileFrameY, out int tileWidth, out int tileHeight, out int tileTop, out int halfBrickHeight, out int addFrX, out int addFrY, out SpriteEffects tileSpriteEffect, out Texture2D glowTexture, out Rectangle glowSourceRect, out Color glowColor)
	{
		orig(self, x, y, tileCache, typeCache, ref tileFrameX, ref tileFrameY, out tileWidth, out tileHeight, out tileTop, out halfBrickHeight, out addFrX, out addFrY, out tileSpriteEffect, out glowTexture, out glowSourceRect, out glowColor);

		foreach (HookModifyTileDrawData hook in Hooks)
		{
			hook(x, y, tileCache, typeCache, ref tileFrameX, ref tileFrameY, ref tileWidth, ref tileHeight, ref tileTop, ref halfBrickHeight, ref addFrX, ref addFrY, ref tileSpriteEffect, ref glowTexture, ref glowSourceRect, ref glowColor);
		}
	}

	public override void SetStaticDefaults()
	{
		Hooks = Mod.GetContent()
			.Where(g => g is IHookGetTileDrawData)
			.Select<ILoadable, HookModifyTileDrawData>(g => (g as IHookGetTileDrawData).ModifyTileDrawData)
			.ToArray();
	}
}

/// <inheritdoc cref="GetTileDrawDataHook"/>
internal interface IHookGetTileDrawData
{
	/// <summary>
	/// Directly modifies the draw data of an individual tile.
	/// </summary>
	/// <param name="x">The x-coordinate of the tile in tile coordinates.</param>
	/// <param name="y">The y-coordinate of the tile in tile coordinates.</param>
	/// <param name="tileCache">The tile.</param>
	/// <param name="typeCache">The <see cref="Tile.TileType"/> of the tile.</param>
	/// <param name="tileFrameX">The modified <see cref="Tile.TileFrameX"/> of the tile.</param>
	/// <param name="tileFrameY">The modified <see cref="Tile.TileFrameY"/> of the tile.</param>
	/// <param name="tileWidth">The modified width of this tile's source rectangle.</param>
	/// <param name="tileHeight">The modified height ot this tile's source rectangle.</param>
	/// <param name="tileTop">The vertical offset og this tile.</param>
	/// <param name="halfBrickHeight">The vertical offset to use for half bricks.</param>
	/// <param name="addFrX">The offset from <paramref name="tileFrameX"/> to use without changing the frame.</param>
	/// <param name="addFrY">The offset from <paramref name="tileFrameY"/> to use without changing the frame.</param>
	/// <param name="tileSpriteEffect">The <see cref="SpriteEffects"/> to draw this tile with.</param>
	/// <param name="glowTexture">The glow mask to draw.</param>
	/// <param name="glowSourceRect">The source rectangle of <paramref name="glowTexture"/> to draw.</param>
	/// <param name="glowColor">The color of this tile glow mask.</param>
	void ModifyTileDrawData(int x, int y, Tile tileCache, ushort typeCache, ref short tileFrameX, ref short tileFrameY, ref int tileWidth, ref int tileHeight, ref int tileTop, ref int halfBrickHeight, ref int addFrX, ref int addFrY, ref SpriteEffects tileSpriteEffect, ref Texture2D glowTexture, ref Rectangle glowSourceRect, ref Color glowColor);
}