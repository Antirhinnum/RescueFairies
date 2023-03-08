using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using RescueFairies.Common.Hooks;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace RescueFairies.Common.GlobalTiles;

/// <summary>
/// Draws a glow mask for a tile.
/// </summary>
[Autoload(Side = ModSide.Client)]
internal sealed class GlowMaskTile : GlobalTile, IHookGetTileDrawData
{
	private static readonly Dictionary<int, Asset<Texture2D>> _glowMasksByTileType = new();
	private static readonly Dictionary<int, Func<Color>> _glowColorsByTileType = new();

	/// <summary>
	/// Registers a tile's glow mask. Glow masks are assumed to be at <see cref="ModTexturedType.Texture"/> + "_Glow".
	/// </summary>
	/// <param name="type">The tile type to register.</param>
	/// <param name="colorFunction">
	/// If not <see langword="null"/>, the function to use to determine glow color.
	/// <br/> If <see langword="null"/>, the glow mask will be drawn in <see cref="Color.White"/>.
	/// </param>
	internal static void RegisterTile(int type, Func<Color> colorFunction = null)
	{
		Asset<Texture2D> glowAsset = ModContent.Request<Texture2D>(TileLoader.GetTile(type).Texture + "_Glow");
		_glowMasksByTileType.Add(type, glowAsset);
		if (colorFunction != null)
		{
			_glowColorsByTileType.Add(type, colorFunction);
		}
	}

	void IHookGetTileDrawData.ModifyTileDrawData(int x, int y, Tile tileCache, ushort typeCache, ref short tileFrameX, ref short tileFrameY, ref int tileWidth, ref int tileHeight, ref int tileTop, ref int halfBrickHeight, ref int addFrX, ref int addFrY, ref SpriteEffects tileSpriteEffect, ref Texture2D glowTexture, ref Rectangle glowSourceRect, ref Color glowColor)
	{
		if (_glowMasksByTileType.TryGetValue(typeCache, out Asset<Texture2D> glowAsset))
		{
			glowTexture = glowAsset.Value;
			glowSourceRect = new Rectangle(tileFrameX, tileFrameY + addFrY, tileWidth, tileHeight);

			glowColor = _glowColorsByTileType.TryGetValue(typeCache, out var colorFunction) ? colorFunction.Invoke() : Color.White;
		}
	}
}