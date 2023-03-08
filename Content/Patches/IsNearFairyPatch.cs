using RescueFairies.Content.NPCs;
using System.Reflection;
using TeaFramework.Features.Patching;
using TeaFramework.Utilities;
using Terraria;
using Terraria.ModLoader;
using On_Player = On.Terraria.Player;

namespace RescueFairies.Content.Patches;

/// <summary>
/// Players have decreased spawn rates when near fairies, so Purple Fairies should also give this buff.
/// </summary>
internal sealed class IsNearFairyPatch : Patch<IsNearFairyPatch.IsNearFairy>
{
	internal delegate bool IsNearFairy(On_Player.orig_isNearFairy orig, Player self);

	public override MethodBase ModifiedMethod { get; } = typeof(Player).GetCachedMethod(nameof(Player.isNearFairy));

	protected override IsNearFairy PatchMethod { get; } = static (orig, self) =>
	{
		bool originalReturn = orig(self);
		int purpleFairyType = ModContent.NPCType<FairyCritterPurple>();

		// Only check for purple fairies if no other fairies are nearby.
		if (!originalReturn && NPC.npcsFoundForCheckActive[purpleFairyType])
		{
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				if (Main.npc[i].active && Main.npc[i].type == purpleFairyType && self.WithinRange(Main.npc[i].Center, NPC.sWidth))
					return true;
			}
		}

		return originalReturn;
	};
}