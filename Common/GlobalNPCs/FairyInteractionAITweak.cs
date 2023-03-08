using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RescueFairies.Common.GlobalNPCs;

/// <summary>
/// Makes vanilla fairies flutter around modded fairies.
/// </summary>
internal sealed class FairyInteractionAITweak : GlobalNPC
{
	///// <summary>
	///// Patches AI style 112 (<see cref="NPCAIStyleID.Fairy"/>) to flutter with custom fairies.
	///// </summary>
	//internal sealed class FairyAIPatch : Patch<ILContext.Manipulator>
	//{
	//    public override MethodBase ModifiedMethod { get; } = typeof(NPC).GetCachedMethod("AI_112_FairyCritter");

	//    protected override ILContext.Manipulator PatchMethod { get; } = static (il) =>
	//    {
	//        ILCursor c = new(il);

	//        // Match (C#):
	//        //	if (k != whoAmI && ...
	//        // Match (IL):
	//        //	ldloc.s 59 // k
	//        //	ldarg.0
	//        //	ldfld int32 Terraria.Entity::whoAmI
	//        //	beq LABEL // to "k++"

	//        int loopIndex = -1;
	//        ILLabel loopIterateLabel = null;
	//        c.GotoNext(MoveType.Before,
	//            i => i.MatchLdloc(out loopIndex),
	//            i => i.MatchLdarg(0),
	//            i => i.MatchLdfld(typeof(Entity).GetCachedField(nameof(Entity.whoAmI))),
	//            i => i.MatchBeq(out loopIterateLabel)
	//        );

	//        // Code (C#):
	//        //	if (FairyInteractionSystem.CanFlutterAround(this, Main.npc[i]))

	//        ILLabel flutterLogicLabel = c.DefineLabel();
	//        ILLabel customCheckLabel = c.MarkLabel();
	//        c.Emit(OpCodes.Ldarg_0);
	//        c.Emit(OpCodes.Ldloc, loopIndex);
	//        c.Emit(OpCodes.Ldsfld, typeof(Main).GetCachedField(nameof(Main.npc)));
	//        c.Emit(OpCodes.Ldelem_Ref);
	//        c.EmitDelegate(CanFlutterAround);
	//        c.Emit(OpCodes.Brfalse, loopIterateLabel);
	//        c.Emit(OpCodes.Br, flutterLogicLabel);

	//        // Match (C#):
	//        //	if (position.Y < Main.npc[k].position.Y)
	//        // Match (IL):
	//        //	ldarg.0
	//        //	ldflda valuetype [FNA]Microsoft.Xna.Framework.Vector2 Terraria.Entity::position
	//        //	ldfld float32 [FNA]Microsoft.Xna.Framework.Vector2::Y
	//        //	ldsfld class Terraria.NPC[] Terraria.Main::npc
	//        //	ldloc.s 59
	//        //	ldelem.ref
	//        //	ldflda valuetype [FNA]Microsoft.Xna.Framework.Vector2 Terraria.Entity::position
	//        //	ldfld float32 [FNA]Microsoft.Xna.Framework.Vector2::Y
	//        //	bge.un.s LABEL
	//        c.GotoNext(MoveType.Before,
	//            i => i.MatchLdarg(0),
	//            i => i.MatchLdflda(typeof(Entity).GetCachedField(nameof(Entity.position))),
	//            i => i.MatchLdfld(typeof(Vector2).GetCachedField(nameof(Vector2.Y))),
	//            i => i.MatchLdsfld(typeof(Main).GetCachedField(nameof(Main.npc))),
	//            i => i.MatchLdloc(loopIndex),
	//            i => i.MatchLdelemRef(),
	//            i => i.MatchLdflda(typeof(Entity).GetCachedField(nameof(Entity.position))),
	//            i => i.MatchLdfld(typeof(Vector2).GetCachedField(nameof(Vector2.Y))),
	//            i => i.MatchBgeUn(out _));
	//        c.MarkLabel(flutterLogicLabel);

	//        // Match (C#):
	//        //	...; k < 200; ...
	//        // Match (IL):
	//        //	ldloc.s 59 // k
	//        //	ldc.i4 200
	//        //	blt LABEL
	//        // Redirect this to the custom check.

	//        ILLabel loopStartLabel = null;
	//        c.GotoNext(MoveType.After,
	//            i => i.MatchLdloc(loopIndex),
	//            i => i.MatchLdcI4(Main.maxNPCs),
	//            i => i.MatchBlt(out loopStartLabel)
	//            );
	//        c.Prev.Operand = customCheckLabel;
	//    };
	//}

	/// <summary>
	/// If <see langword="true"/> for a given <see cref="NPC.type"/>, then that NPC is one of vanilla's fairies.
	/// <br/> Vanilla fairies don't get any additional movement with other vanilla fairies, as <see cref="NPCAIStyleID.Fairy"/> handles that.
	/// <br/> Defaults to <see langword="false"/>.
	/// </summary>
	private static readonly bool[] VanillaFairies = NPCID.Sets.Factory.CreateBoolSet(NPCID.FairyCritterPink, NPCID.FairyCritterGreen, NPCID.FairyCritterBlue);

	/// <summary>
	/// If <see langword="true"/> for a given <see cref="NPC.type"/>, then that NPC can flutter around with other NPCs in this set.
	/// <br/> Only handled through <see cref="NPCAIStyleID.Fairy"/> -- you'll have to write your own implementation for custom AIs.
	/// <br/> Defaults to <see langword="false"/>.
	/// </summary>
	internal static readonly bool[] Fairies = NPCID.Sets.Factory.CreateBoolSet(NPCID.FairyCritterPink, NPCID.FairyCritterGreen, NPCID.FairyCritterBlue);

	// Only vanilla fairies need the extra movement.
	// Modded fairies should use CanFlutterAround and handle their own movement.
	public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
	{
		return VanillaFairies[entity.type];
	}

	public override void AI(NPC npc)
	{
		for (int i = 0; i < Main.maxNPCs; i++)
		{
			// Only care about modded fairies for the three vanilla fairies.
			if (VanillaFairies[Main.npc[i].type])
			{
				continue;
			}

			if (CanFlutterAround(npc, Main.npc[i]))
			{
				if (npc.position.Y < Main.npc[i].position.Y)
					npc.velocity.Y -= 0.05f;
				else
					npc.velocity.Y += 0.05f;
			}
		}
	}

	/// <summary>
	/// Determines if <paramref name="npc"/> can flutter around <paramref name="other"/>.
	/// </summary>
	/// <param name="npc">The first NPC to check.</param>
	/// <param name="other">The second NPC to check.</param>
	/// <returns><see langword="true"/> if both NPCs are in the <see cref="Fairies"/> set, <see cref="Entity.active"/>, and are close to each other.</returns>
	internal static bool CanFlutterAround(NPC npc, NPC other)
	{
		return npc.whoAmI != other.whoAmI
			&& npc.active && other.active
			&& Fairies[npc.type] && Fairies[other.type]
			&& Math.Abs(npc.position.X - other.position.X) + Math.Abs(npc.position.Y - other.position.Y) < npc.width * 1.5f;
	}
}