using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TeaFramework.Features.Patching;
using TeaFramework.Utilities;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using On_Chest = On.Terraria.Chest;

namespace RescueFairies.Common.Systems;

/// <summary>
/// Allows <see cref="ModNPC"/>s to unlock the <see cref="ItemID.FairyGlowstick"/> on discovery.
/// <br/> Client-side only.
/// </summary>
// NPC shops are not loaded on servers, so this system only needs to exist on clients.
[Autoload(Side = ModSide.Client)]
internal sealed class FairyTorchUnlockSystem : ModSystem
{
	internal sealed class BestiaryGirl_IsFairyTorchAvailablePatch : Patch<BestiaryGirl_IsFairyTorchAvailablePatch.BestiaryGirl_IsFairyTorchAvailable>
	{
		internal delegate bool BestiaryGirl_IsFairyTorchAvailable(On_Chest.orig_BestiaryGirl_IsFairyTorchAvailable orig);

		public override MethodBase ModifiedMethod { get; } = typeof(Chest).GetCachedMethod(nameof(BestiaryGirl_IsFairyTorchAvailable));

		protected override BestiaryGirl_IsFairyTorchAvailable PatchMethod { get; } = static (orig) =>
		{
			bool originalReturn = orig();

			// Only check custom entries if no vanilla entry succeeded.
			if (!originalReturn)
			{
				for (int i = 0; i < CanUnlockFairyTorch.Length; i++)
				{
					if (CanUnlockFairyTorch[i] && DidDiscoverBestiaryEntry(i))
					{
						return true;
					}
				}
			}

			return originalReturn;
		};
	}

	/// <summary>
	/// If an <see cref="NPC.type"/> is included in this set, then it unlocks the <see cref="ItemID.FairyGlowstick"/> in the Zoologist's shop when discovered.
	/// </summary>
	internal static readonly bool[] CanUnlockFairyTorch = NPCID.Sets.Factory.CreateBoolSet();

	// From Terraria.Chest.
	/// <summary>
	/// Determines if a given NPC's Bestiary info has been unlocked in the current world.
	/// </summary>
	/// <param name="npcId">The <see cref="NPC.type"/> to check.</param>
	/// <returns><see langword="true"/> if the provided NPC's Bestiary entry is visible (<see cref="BestiaryEntryUnlockState.CanShowPortraitOnly_1"/> or higher), <see langword="false"/> otherwise..</returns>
	private static bool DidDiscoverBestiaryEntry(int npcId)
	{
		return (bool)typeof(Chest).GetCachedMethod(nameof(DidDiscoverBestiaryEntry)).Invoke(null, new object[] { npcId });
	}
}