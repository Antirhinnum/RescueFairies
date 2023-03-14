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
	private static readonly List<Func<NPC, bool>> _trackingConditions = new(8);

	/// <summary>
	/// Conditions to blacklist an NPC from being tracked. Intended for cases where modded NPCs use vanilla conditions without the same intent as vanilla.
	/// <br/> For example, vanilla only uses <see cref="NPCAIStyleID.FaceClosestPlayer"/> for bound NPCs. A mod may use it for other reasons that wouldn't make sense for a fairy to track.
	/// </summary>
	private static readonly List<Func<NPC, bool>> _trackingBlacklist = new(4);

	/// <summary>
	/// The indicies in <see cref="Main.npc"/> of trackable (<see cref="ValidNPCToTrack(NPC)"/>) NPCs this frame. Only contains active (<see cref="Entity.active"/>) NPCs.
	/// <br/> Always empty on multiplayer clients (<see cref="NetmodeID.MultiplayerClient"/>).
	/// </summary>
	private static readonly List<int> _trackedNpcIndices = new(20);

	/// <summary>
	/// A collection of all trackable (<see cref="ValidNPCToTrack(NPC)"/>) NPC indicies (in <see cref="Main.npc"/>) this frame. Only contains active (<see cref="Entity.active"/>) NPCs.
	/// <br/> Safe to call in <see cref="ModNPC.AI"/> and <see cref="ModNPC.SpawnChance(NPCSpawnInfo)"/>.
	/// <br/> Always empty on multiplayer clients (<see cref="NetmodeID.MultiplayerClient"/>).
	/// </summary>
	public static IEnumerable<int> TrackedNPCIndicies => _trackedNpcIndices;

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

	/// <summary>
	/// Determines if a trackable NPC is near the given player.
	/// <br/> Should not be called on multiplayer clients (<see cref="NetmodeID.MultiplayerClient"/>).
	/// </summary>
	/// <param name="player">The player to check near.</param>
	/// <param name="maximumDistance">The maximum distance to check in world coordinates. Defaults to 50 tiles (<c>800f</c>).</param>
	/// <returns>
	/// <see langword="true"/> if any trackable NPC is within <paramref name="maximumDistance"/> of <paramref name="player"/>.
	/// <br/> Always returns <see langword="false"/> on multiplayer clients.
	/// </returns>
	public static bool AnyTrackableNPCNearPlayer(Player player, float maximumDistance = 50f * 16f)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			return false;
		}

		foreach (int npcIndex in _trackedNpcIndices)
		{
			if (player.WithinRange(Main.npc[npcIndex].Center, maximumDistance))
			{
				return true;
			}
		}

		return false;
	}

	public override void PreUpdateNPCs()
	{
		// NPC.SpawnNPC() doesn't run on clients, so don't bother filling the tracking list.
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			return;
		}

		// Keep track of every trackable NPC in the world.
		// Purple fairies can only spawn if there are any trackable NPCs near the player.
		_trackedNpcIndices.Clear();
		for (int i = 0; i < Main.maxNPCs; i++)
		{
			NPC npc = Main.npc[i];
			if (npc.active && ValidNPCToTrack(npc))
			{
				_trackedNpcIndices.Add(npc.whoAmI);
			}
		}
	}
}