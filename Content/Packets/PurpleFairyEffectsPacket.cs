using Microsoft.Xna.Framework;
using RescueFairies.Content.NPCs;
using System.IO;
using TeaFramework.API.Features.Packets;
using Terraria;

namespace RescueFairies.Content.Packets;

/// <summary>
/// The required data for <see cref="PurpleFairyEffectsPacket"/>.
/// </summary>
internal readonly struct PurpleFairyEffectsPacketData : IPacketData
{
	/// <summary>
	/// The world position of the effect.
	/// </summary>
	internal Vector2 Position { get; init; }
}

/// <summary>
/// Syncs a purple fairy vanishing effect.
/// </summary>
internal class PurpleFairyEffectsPacket : IPacketHandlerWithData<PurpleFairyEffectsPacketData>
{
	byte IPacketHandler.Id { get; set; }

	void IPacketHandler.ReadPacket(BinaryReader reader, int whoAmI)
	{
		Vector2 position = reader.ReadVector2();
		FairyCritterPurple.PurpleFairyEffects(position);
	}

	void IPacketHandlerWithData<PurpleFairyEffectsPacketData>.Write(BinaryWriter writer, PurpleFairyEffectsPacketData packetData)
	{
		writer.WriteVector2(packetData.Position.Floor());
	}
}