using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RescueFairies.Common.Hooks;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace RescueFairies.Common.GlobalTiles;

/// <summary>
/// Animates a tile like a critter cages.
/// </summary>
[Autoload(Side = ModSide.Client)]
internal sealed class CritterCageAnimationTile : GlobalTile, IHookGetTileDrawData
{
	private static readonly Dictionary<int, int[]> _framingArrayByTileType = new();

	/// <summary>
	/// Registers a tile to be animated.
	/// </summary>
	/// <param name="type">The tile type to register.</param>
	/// <param name="framingArray">The array of frames to use. Should be <see cref="Main.cageFrames"/> long.</param>
	internal static void RegisterTile(int type, int[] framingArray)
	{
		ArgumentNullException.ThrowIfNull(framingArray);

		_framingArrayByTileType.Add(type, framingArray);
	}

	void IHookGetTileDrawData.ModifyTileDrawData(int x, int y, Tile tileCache, ushort typeCache, ref short tileFrameX, ref short tileFrameY, ref int tileWidth, ref int tileHeight, ref int tileTop, ref int halfBrickHeight, ref int addFrX, ref int addFrY, ref SpriteEffects tileSpriteEffect, ref Texture2D glowTexture, ref Rectangle glowSourceRect, ref Color glowColor)
	{
		if (!_framingArrayByTileType.TryGetValue(typeCache, out var framingArray))
		{
			return;
		}
		TileObjectData data = TileObjectData.GetTileData(tileCache);
		(int width, int height) = (data.Width, data.Height);
		int frame = GetCageFrame(x, y, tileFrameX, tileFrameY, width, height);

		tileTop = 2;
		Main.critterCage = true;
		addFrY = framingArray[frame] * height * 18;
	}

	/// <summary>
	/// Determines the frame a critter cage should use.
	/// </summary>
	/// <param name="x">The x-coordinate of the cage in tile coordinates. Any point on the cage works.</param>
	/// <param name="y">The y-coordinate of the cage in tile coordinates. Any point on the cage works.</param>
	/// <param name="tileFrameX">The <see cref="Tile.TileFrameX"/> of the current tile.</param>
	/// <param name="tileFrameY">The <see cref="Tile.TileFrameY"/> of the current tile.</param>
	/// <param name="width">The width of the tile in tiles.</param>
	/// <param name="height">The height of the tile in tiles.</param>
	/// <returns>A frame number from 0 to <see cref="Main.cageFrames"/>.</returns>
	private static int GetCageFrame(int x, int y, int tileFrameX, int tileFrameY, int width, int height)
	{
		int left = x - (tileFrameX / 18);
		int top = y - (tileFrameY / 18);
		return left / width * (top / (height + 1)) % Main.cageFrames;
	}
}