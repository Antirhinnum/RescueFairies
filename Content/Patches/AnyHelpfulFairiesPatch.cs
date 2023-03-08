using RescueFairies.Content.NPCs;
using System.Reflection;
using TeaFramework.Features.Patching;
using TeaFramework.Utilities;
using Terraria;
using Terraria.ModLoader;
using On_NPC = On.Terraria.NPC;

namespace RescueFairies.Content.Patches;

/// <summary>
/// Only one helpful fairy can spawn at a time, so make purple fairies prevent other fairy spawns.
/// </summary>
internal sealed class AnyHelpfulFairiesPatch : Patch<AnyHelpfulFairiesPatch.AnyHelpfulFairies>
{
	internal delegate bool AnyHelpfulFairies(On_NPC.orig_AnyHelpfulFairies orig);

	public override MethodBase ModifiedMethod { get; } = typeof(NPC).GetCachedMethod(nameof(NPC.AnyHelpfulFairies));

	protected override AnyHelpfulFairies PatchMethod { get; } = static (orig) =>
	{
		bool originalReturn = orig();
		int purpleFairyType = ModContent.NPCType<FairyCritterPurple>();

		if (!originalReturn)
		{
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				if (Main.npc[i].active && Main.npc[i].type == purpleFairyType && (Main.npc[i].ModNPC as FairyCritterPurple).IsBeingHelpful)
				{
					return true;
				}
			}
		}

		return originalReturn;
	};
}