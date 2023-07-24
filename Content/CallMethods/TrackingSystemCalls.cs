using RescueFairies.Common.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using TeaFramework.API.Features.ModCall;
using TeaFramework.Features.ModCall;
using Terraria;

namespace RescueFairies.Content.CallMethods;

/// <summary>
/// Allows other mods to make their NPCs trackable by purple fairies, or to prevent tracking.
/// </summary>
internal sealed class TrackingSystemCalls : ModCallHandler
{
	/*
	 * Call arguments:
	 *  - int (npc type)
	 *  - Func<NPC, bool> (condition)
	 */

	public override IEnumerable<string> HandleableMessages => new[] { "AddTrackingCondition", "AddBlacklist" };

	public override bool ValidateArgs(List<object> parsedArgs, out IModCallManager.ArgParseFailureType failureType)
	{
		if (parsedArgs.Count != 1)
		{
			failureType = IModCallManager.ArgParseFailureType.ArgLength;
			return false;
		}

		if (parsedArgs[0] is not int && parsedArgs[0] is not Func<NPC, bool>)
		{
			failureType = IModCallManager.ArgParseFailureType.ArgType;
			return false;
		}

		failureType = IModCallManager.ArgParseFailureType.None;
		return true;
	}

	public override object Call(string message, List<object> args)
	{
		Func<NPC, bool> condition = args[0] is int type ? (npc => npc.type == type) : (Func<NPC, bool>)args[0];
		if (message == HandleableMessages.ToArray()[0])
		{
			TrackableNPCSystem.AddTrackingCondition(condition);
		}
		else
		{
			TrackableNPCSystem.AddToBlacklist(condition);
		}

		return null;
	}
}