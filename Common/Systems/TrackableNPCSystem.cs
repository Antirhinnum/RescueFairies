using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RescueFairies.Common.Systems;

/// <summary>
/// Determines which NPCs can be tracked by purple fairies.
/// </summary>
public sealed class TrackableNPCSystem : ModSystem
{
	/// <summary>
	/// The conditions for an NPC to be tracked by purple fairies.
	/// <br/> If any condition in this list is <see langword="true"/> and no conditions in <see cref="_trackingBlacklist"/> are <see langword="true"/>, the NPC can be tracked.
	/// </summary>
	private static readonly List<Func<NPC, bool>> _trackingConditions = new();

	/// <summary>
	/// Conditions to blacklist an NPC from being tracked. Intended for cases where modded NPCs use vanilla conditions without the same intent as vanilla.
	/// <br/> For example, vanilla only uses <see cref="NPCAIStyleID.FaceClosestPlayer"/> for bound NPCs. A mod may use it for other reasons that wouldn't make sense for a fairy to track.
	/// </summary>
	private static readonly List<Func<NPC, bool>> _trackingBlacklist = new();

	public override void SetStaticDefaults()
	{
		// Bound NPCs
		_trackingConditions.Add(npc => npc.aiStyle == NPCAIStyleID.FaceClosestPlayer);
		_trackingConditions.Add(npc => npc.type == NPCID.SkeletonMerchant);

		// """Friendly""" NPCs
		_trackingConditions.Add(npc => npc.type == NPCID.LostGirl);
		_trackingConditions.Add(npc => npc.aiStyle == NPCAIStyleID.Mimic && npc.ai[0] == 0f);
		_trackingConditions.Add(npc => npc.aiStyle == NPCAIStyleID.BiomeMimic && npc.ai[0] == 0f);

		// 1.4.4:
		//_trackingConditionByNPCType.Add(NPCID.BoundTownSlimeOld, _alwaysTrack);
		//_trackingConditionByNPCType.Add(NPCID.BoundTownSlimeYellow, _alwaysTrack);
		// No purple since it moves around a lot.
	}

	/// <summary>
	/// Adds a specific NPC type to the tracking list.
	/// </summary>
	/// <param name="npcId">The NPC type to track.</param>
	public static void AddTrackingCondition(int npcId)
	{
		_trackingConditions.Add(npc => npc.type == npcId);
	}

	/// <summary>
	/// Adds a tracking condition.
	/// </summary>
	/// <param name="condition">The condition to add. Cannot be <see langword="null"/>.</param>
	public static void AddTrackingCondition(Func<NPC, bool> condition)
	{
		ArgumentNullException.ThrowIfNull(condition);
		_trackingConditions.Add(condition);
	}

	/// <summary>
	/// Adds a specific NPC type to the tracking blacklist.
	/// </summary>
	/// <param name="npcId">The NPC type to blacklist.</param>
	public static void AddToBlacklist(int npcId)
	{
		_trackingBlacklist.Add(npc => npc.type == npcId);
	}

	/// <summary>
	/// Adds a condition to the blacklist.
	/// </summary>
	/// <param name="blacklistCondition">The condition to add. Cannot be <see langword="null"/>.</param>
	public static void AddToBlacklist(Func<NPC, bool> blacklistCondition)
	{
		ArgumentNullException.ThrowIfNull(blacklistCondition);
		_trackingBlacklist.Add(blacklistCondition);
	}

	/// <summary>
	/// Determines if a given NPC can be tracked by purple fairies.
	/// </summary>
	/// <param name="npc">The NPC to check.</param>
	/// <returns><see langword="true"/> for bound NPCs or specific exceptions, <see langword="false"/> otherwise.</returns>
	public static bool ValidNPCToTrack(NPC npc)
	{
		foreach (Func<NPC, bool> blacklistCondition in _trackingBlacklist)
		{
			if (blacklistCondition(npc))
			{
				return false;
			}
		}

		foreach (Func<NPC, bool> trackingCondition in _trackingConditions)
		{
			if (trackingCondition(npc))
			{
				return true;
			}
		}

		return false;
	}
}