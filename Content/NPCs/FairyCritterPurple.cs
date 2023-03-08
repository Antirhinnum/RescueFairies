using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RescueFairies.Common.GlobalNPCs;
using RescueFairies.Common.Systems;
using RescueFairies.Content.Items;
using RescueFairies.Content.Packets;
using System;
using TeaFramework;
using TeaFramework.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.Achievements;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace RescueFairies.Content.NPCs;

/// <summary>
/// A custom fairy. This is meant to act as closely to vanilla's fairies as possible, except that it seeks out rare NPCs rather than treasure.
/// </summary>
public sealed class FairyCritterPurple : ModNPC
{
	/*
	 * This NPC is mostly accurate to vanilla fairies. Some explicit differences:
	 *  - Instead of treasure, this NPC seeks out other NPCs: Usually lost Town NPCs, but also some other "friendly" NPCs.
	 *  - This NPC doesn't spawn from Fairy Logs. They still need a log in the world to spawn, but won't spawn as part of the log event.
	 *  - This NPC cannot be shaken out of trees. Rescue Fairies only appear underground, after all.
	 *     - Maybe they could be shaken out of Gem Trees?
	 *  - This NPC doesn't flutter around with other Fairies. This behaviour only really happens at Fairy Logs, which these Fairies never appear at.
	 */

	public enum FairyAIState
	{
		/// <summary>
		/// The fairy hovers in place and waits for a player to get close.
		/// </summary>
		WaitForPlayer,

		/// <summary>
		/// The fairy wanders around, hovering over tiles and liquids. Fairies in this state will never find treasure/NPCs.
		/// </summary>
		RunAway,

		/// <summary>
		/// This fairy has noticed a player and is now chasing after them.
		/// </summary>
		ChasePlayer,

		/// <summary>
		/// This fairy has found a target, but there isn't a player close enough to follow it. Circle in place for a bit, then start tracking the target position.
		/// </summary>
		CatchAttention,

		/// <summary>
		/// Slowly lead a nearby player to the found target.
		/// </summary>
		LeadToTarget,

		/// <summary>
		/// This fairy has found its target. It will now flutter around and eventually  vanish.
		/// </summary>
		FoundTarget,

		/// <summary>
		/// This fairy is hovering close to its target player.
		/// </summary>
		HoverAroundPlayer,

		/// <summary>
		/// The fairy flies offscreen and despawns.
		/// </summary>
		Despawn
	}

	/// <summary>
	/// The number of ticks fairies wait before despawning.
	/// </summary>
	private const int DESPAWN_TIME_TICKS = 18_000;

	/// <summary>
	/// A general per-AI state timer. Should be reset back to <c>0f</c> if <see cref="AIState"/> changes.
	/// </summary>
	public ref float GeneralTimer => ref NPC.ai[3];

	/// <summary>
	/// The number of ticks this fairy has spent waiting for a player. If this reaches <see cref="DESPAWN_TIME_TICKS"/>, this fairy despawns (<see cref="FairyAIState.Despawn"/>).
	/// </summary>
	public ref float DespawnTimer => ref NPC.localAI[1];

	/// <summary>
	/// The current state of this fairy's AI.
	/// </summary>
	public FairyAIState AIState
	{
		get => (FairyAIState)(int)NPC.ai[2];
		set => NPC.ai[2] = (float)value;
	}

	/// <summary>
	/// The world position this fairy is trying to go to.
	/// </summary>
	public Vector2 TargetPosition
	{
		get => new(NPC.ai[0], NPC.ai[1]);
		set
		{
			NPC.ai[0] = value.X;
			NPC.ai[1] = value.Y;
		}
	}

	/// <summary>
	/// If <see langword="false"/>, this fairy hasn't been initialized and will do so the next time <see cref="AIState"/> is <see cref="FairyAIState.WaitForPlayer"/>.
	/// </summary>
	public bool Initialized
	{
		get => NPC.localAI[0] != 0f;
		set => NPC.localAI[0] = value ? 1f : 0f;
	}

	/// <summary>
	/// If <see langword="true"/>, this fairy is currently helping a player find something.
	/// </summary>
	public bool IsBeingHelpful => AIState != FairyAIState.WaitForPlayer && AIState != FairyAIState.RunAway;

	public override void SetStaticDefaults()
	{
		Main.npcCatchable[Type] = true;
		Main.npcFrameCount[Type] = 4;

		NPCID.Sets.DebuffImmunitySets[Type] = new()
		{
			ImmuneToAllBuffsThatAreNotWhips = true,
			ImmuneToWhips = true
		};
		NPCID.Sets.TakesDamageFromHostilesWithoutBeingFriendly[Type] = true;
		NPCID.Sets.TownCritter[Type] = true;
		NPCID.Sets.CountsAsCritter[Type] = true;
		NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers(0)
		{
			Velocity = 1f
		});

		FairyInteractionAITweak.Fairies[Type] = true;
		FairyTorchUnlockSystem.CanUnlockFairyTorch[Type] = true;
	}

	public override void SetDefaults()
	{
		NPC.width = 18;
		NPC.height = 20;
		NPC.aiStyle = -1;
		NPC.damage = 0;
		NPC.defense = 0;
		NPC.lifeMax = 5;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath1;
		NPC.catchItem = ModContent.ItemType<FairyCritterPurpleItem>();
		NPC.noGravity = true;
		NPC.rarity = 2;
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
	{
		bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
		{
			BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Underground,
			new FlavorTextBestiaryInfoElement($"Mods.RescueFairies.Bestiary.{Name}")
		});
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo)
	{
		if (NPC.fairyLog && !NPC.AnyHelpfulFairies() && spawnInfo.SpawnTileY >= (Main.worldSurface + Main.rockLayer) / 2 && spawnInfo.SpawnTileY < Main.UnderworldLayer)
		{
			return (Main.tenthAnniversaryWorld ? 0.05f : 0.0125f) + (spawnInfo.Player.RollLuck(3) - 1) * 0.005f;
		}

		return 0f;
	}

	public override void OnSpawn(IEntitySource source)
	{
		AIState = FairyAIState.ChasePlayer;
		NPC.TargetClosest();
		GeneralTimer = 3f;
		NPC.netUpdate = true;
	}

	public override bool? CanBeCaughtBy(Item item, Player player)
	{
		return !IsBeingHelpful;
	}

	public override bool CheckActive()
	{
		bool flag = false;
		if (!Main.dayTime && AIState == FairyAIState.WaitForPlayer)
		{
			NPC.timeLeft = NPC.activeTime;
			flag = true;
		}
		return !flag;
	}

	public override void AI()
	{
		// A recreation of AI style 112, modified to fit this Fairy's unique behavior.

		bool manualDirectionControl = false;
		NPC.lavaImmune = true;

		if (Main.netMode != NetmodeID.MultiplayerClient && IsBeingHelpful)
		{
			DespawnTimer += 1f;
			if (DespawnTimer >= DESPAWN_TIME_TICKS)
			{
				AIState = FairyAIState.Despawn;
				NPC.direction = (Main.player[NPC.target].Center.X < NPC.Center.X).ToDirectionInt();
				NPC.netUpdate = true;
			}
		}

		switch (AIState)
		{
			case FairyAIState.WaitForPlayer:
			{
				NPC.lavaImmune = false;
				NPC.noTileCollide = false;
				if (TargetPosition == Vector2.Zero)
				{
					TargetPosition = NPC.Center;
				}

				// Add a small burst of velocity on spawn.
				if (!Initialized)
				{
					Initialized = true;
					NPC.velocity = new Vector2(
						MathHelper.Lerp(2f, 4f, Main.rand.NextFloat()) * ((Main.rand.Next(2) * 2) - 1),
						MathHelper.Lerp(1f, 2f, Main.rand.NextFloat()) * ((Main.rand.Next(2) * 2) - 1));
					NPC.velocity *= 0.7f;
					NPC.netUpdate = true;
				}

				// Hover in place until a player shows up.
				Vector2 toTargetPosition = TargetPosition - NPC.Center;
				if (toTargetPosition.LengthSquared() > 20f * 20f)
				{
					Vector2 direction = new((toTargetPosition.X > 0f).ToDirectionInt(), (toTargetPosition.Y > 0f).ToDirectionInt());
					NPC.velocity += direction * 0.04f;
					if (Math.Abs(NPC.velocity.Y) > 2f)
						NPC.velocity.Y *= 0.95f;
				}

				NPC.TargetClosest();
				Player player = Main.player[NPC.target];
				if (!player.DeadOrGhost && player.WithinRange(NPC.Center, 250f))
				{
					AIState = FairyAIState.RunAway;
					NPC.direction = (player.Center.X <= NPC.Center.X).ToDirectionInt();
					if (NPC.velocity.X * NPC.direction < 0f)
					{
						NPC.velocity.X = NPC.direction * 2f;
					}

					GeneralTimer = 0f;
					NPC.netUpdate = true;
				}

				break;
			}
			case FairyAIState.RunAway:
			{
				NPC.lavaImmune = false;
				NPC.noTileCollide = false;

				if (NPC.collideX)
				{
					NPC.direction *= -1;
					NPC.velocity.X = NPC.direction * 2f;
				}

				if (NPC.collideY)
				{
					NPC.velocity.Y = (NPC.oldVelocity.Y > 0f).ToDirectionInt();
				}

				// Accelerate in the faced direction.
				float maxSpeed = 4.5f;
				if (Math.Sign(NPC.velocity.X) != NPC.direction || Math.Abs(NPC.velocity.X) < maxSpeed)
				{
					NPC.velocity.X += NPC.direction * 0.04f;
					if (NPC.velocity.X * NPC.direction < 0f)
					{
						if (Math.Abs(NPC.velocity.X) > maxSpeed)
							NPC.velocity.X += NPC.direction * 0.4f;
						else
							NPC.velocity.X += NPC.direction * 0.2f;
					}
					else if (Math.Abs(NPC.velocity.X) > maxSpeed)
					{
						NPC.velocity.X = NPC.direction * maxSpeed;
					}
				}

				// Hover above tiles and liquids.
				// Search a 20x8 region in front of and below this fairy.
				Point bottomTileCoordinates = NPC.Bottom.ToTileCoordinates();
				int leftTileSearchBound = bottomTileCoordinates.X;
				int tileSearchWidth = 20;
				if (NPC.direction < 0)
					leftTileSearchBound -= tileSearchWidth;

				int topTileSearchBound = bottomTileCoordinates.Y;
				int tileSearchHeight = 8;

				bool shouldMoveDown = true;
				bool tooCloseTooGround = false;
				for (int i = leftTileSearchBound; i <= leftTileSearchBound + tileSearchWidth; i++)
				{
					for (int j = topTileSearchBound; j < topTileSearchBound + tileSearchHeight; j++)
					{
						// Fly upwards if any solid tiles or liquids are found.
						if ((Main.tile[i, j].HasUnactuatedTile && Main.tileSolid[Main.tile[i, j].TileType]) || Main.tile[i, j].LiquidAmount > 0)
						{
							// Fly up much faster if very close to the ground.
							if (j < topTileSearchBound + 5)
								tooCloseTooGround = true;

							shouldMoveDown = false;
							break;
						}
					}
				}

				if (shouldMoveDown)
					NPC.velocity.Y += 0.05f;
				else
					NPC.velocity.Y -= 0.2f;

				if (tooCloseTooGround)
					NPC.velocity.Y -= 0.3f;

				NPC.velocity.Y = Math.Clamp(NPC.velocity.Y, -5f, 3f);

				break;
			}
			case FairyAIState.ChasePlayer:
			{
				NPC.noTileCollide = true;

				// Start wandering if the player has died.
				NPCAimedTarget targetData = NPC.GetTargetData();
				bool playerDead = false;
				if (targetData.Type == NPCTargetType.Player)
					playerDead = Main.player[NPC.target].dead;

				if (playerDead)
				{
					AIState = FairyAIState.RunAway;
					NPC.direction = (targetData.Center.X <= NPC.Center.X).ToDirectionInt();
					if (NPC.velocity.X * NPC.direction < 0f)
						NPC.velocity.X = NPC.direction * 2f;

					GeneralTimer = 0f;
					NPC.netUpdate = true;
					break;
				}

				// If a player is close, switch AI states.
				// If a target is found, then notify the player. Otherwise, hover around them.
				Rectangle searchAllowedRange = Utils.CenteredRectangle(targetData.Center, new Vector2(targetData.Width + 60, targetData.Height / 2));
				if (Main.netMode != NetmodeID.MultiplayerClient && NPC.Hitbox.Intersects(searchAllowedRange))
				{
					if (FindBoundNPCs(out Vector2 targetPosition))
					{
						TargetPosition = targetPosition;
						AIState = FairyAIState.CatchAttention;
					}
					else
					{
						AIState = FairyAIState.HoverAroundPlayer;
					}

					GeneralTimer = 0f;
					NPC.netUpdate = true;

					break;
				}

				// Approach the player's range.
				Vector2 toPlayerRange = searchAllowedRange.ClosestPointInRect(NPC.Center);
				Vector2 desiredVelocity = NPC.DirectionTo(toPlayerRange) * 2f;
				float distanceSquaredToRange = NPC.DistanceSQ(toPlayerRange);
				if (distanceSquaredToRange > 150f * 150f)
					desiredVelocity *= 2f;
				else if (distanceSquaredToRange > 80f * 80f)
					desiredVelocity *= 1.5f;
				NPC.velocity = Vector2.Lerp(NPC.velocity, desiredVelocity, 0.07f);

				// Hover if not stuck in a tile.
				Point centerTileCoordinates = NPC.Center.ToTileCoordinates();
				if (GeneralTimer < 300f)
				{
					GetBirdFlightRecommendation(6, 3, centerTileCoordinates, out bool goDownwards, out bool goUpwards);
					if (goDownwards)
						NPC.velocity.Y += 0.05f;

					if (goUpwards)
						NPC.velocity.Y -= 0.02f;

					NPC.velocity.Y = Math.Clamp(NPC.velocity.Y, -4f, 2f);
				}

				if (WorldGen.InWorld(centerTileCoordinates.X, centerTileCoordinates.Y))
				{
					if (WorldGen.SolidTile(centerTileCoordinates))
						GeneralTimer = Math.Min(GeneralTimer + 2f, 400f);
					else
						GeneralTimer = Math.Max(GeneralTimer - 1f, 0f);
				}

				break;
			}
			case FairyAIState.CatchAttention:
			{
				NPC.noTileCollide = true;
				if (GeneralTimer == 15f)
				{
					SoundEngine.PlaySound(SoundID.Pixie, NPC.position);
				}

				// Slow down and start circling in place.
				if (GeneralTimer <= 15f)
				{
					NPC.velocity *= 0.9f;
				}
				else
				{
					NPC.spriteDirection = (Main.player[NPC.target].Center.X <= NPC.Center.X).ToDirectionInt();

					manualDirectionControl = true;

					// Loop in progressively bigger circles.
					float circleRotation = 0f;
					float circlingProgress = GeneralTimer - 15f;
					float circleHeight = 22f;
					if (circlingProgress <= 65f)
					{
						circleRotation = MathF.PI / 8f;
						circleHeight = 14f;
					}
					else if (circlingProgress <= 130f)
					{
						circleRotation = -MathF.PI / 8f;
						circleHeight = 18f;
					}

					circleRotation *= NPC.direction;
					Vector2 offset1 = GetFairyCircleOffset(circlingProgress / 65f, circleRotation, circleHeight);
					Vector2 offset2 = GetFairyCircleOffset((circlingProgress / 65f) + (0.005f * MathF.PI), circleRotation, circleHeight);
					NPC.velocity = offset2 - offset1;
				}

				// After a few seconds, start leading the player to the found target.
				GeneralTimer += 1f;
				if (GeneralTimer >= 210f)
				{
					AIState = FairyAIState.LeadToTarget;
					NPC.TargetClosest();
					GeneralTimer = 0f;
					NPC.netUpdate = true;
				}
				break;
			}
			case FairyAIState.HoverAroundPlayer:
			{
				NPC.noTileCollide = true;

				// If the player is too far, chase after them.
				Vector2 toPlayer = Main.player[NPC.target].Center - NPC.Center;
				if (toPlayer.LengthSquared() > 100f * 100f)
				{
					AIState = FairyAIState.ChasePlayer;
					NPC.TargetClosest();
					GeneralTimer = 0f;
					NPC.netUpdate = true;
					break;
				}

				// Start bouncing off tiles if not stuck in them.
				if (!Collision.SolidCollision(NPC.position, NPC.width, NPC.height))
				{
					NPC.noTileCollide = false;
					if (NPC.collideX)
						NPC.velocity.X *= -1f;

					if (NPC.collideY)
						NPC.velocity.Y *= -1f;
				}

				// Slowly approach the player.
				if (toPlayer.LengthSquared() > 20f * 20f)
				{
					Vector2 direction = new((toPlayer.X > 0f).ToDirectionInt(), (toPlayer.Y > 0f).ToDirectionInt());
					NPC.velocity += direction * 0.04f;
					if (Math.Abs(NPC.velocity.Y) > 2f)
						NPC.velocity.Y *= 0.95f;
				}

				// If any targets are found, notify the player.
				if (Main.netMode != NetmodeID.MultiplayerClient && FindBoundNPCs(out Vector2 targetPosition))
				{
					TargetPosition = targetPosition;
					AIState = FairyAIState.CatchAttention;
					GeneralTimer = 0f;
					NPC.netUpdate = true;
				}

				break;
			}
			case FairyAIState.LeadToTarget:
			{
				NPC.noTileCollide = true;

				// Wander away if the player is dead.
				NPCAimedTarget targetData = NPC.GetTargetData();
				bool playerDead = false;
				if (targetData.Type == NPCTargetType.Player)
					playerDead = Main.player[NPC.target].dead;

				if (playerDead)
				{
					AIState = FairyAIState.RunAway;
					NPC.direction = (targetData.Center.X <= NPC.Center.X).ToDirectionInt();
					if (NPC.velocity.X * NPC.direction < 0f)
						NPC.velocity.X = NPC.direction * 2f;

					GeneralTimer = 0f;
					NPC.netUpdate = true;
					break;
				}

				// Switch AI states if the target is found.
				Rectangle targetRange = Utils.CenteredRectangle(TargetPosition, Vector2.One * 5f);
				if (NPC.Hitbox.Intersects(targetRange))
				{
					AIState = FairyAIState.FoundTarget;
					GeneralTimer = 0f;
					NPC.netUpdate = true;
					break;
				}

				// If the player is too far away, nudge towards them.
				float playerDistance = NPC.Distance(targetData.Center);
				float nudgeRange = 300f;
				if (playerDistance > nudgeRange)
				{
					if (playerDistance < nudgeRange + 100f && !Collision.SolidCollision(NPC.position, NPC.width, NPC.height))
					{
						NPC.noTileCollide = false;
						if (NPC.collideX)
							NPC.velocity.X *= -1f;

						if (NPC.collideY)
							NPC.velocity.Y *= -1f;
					}

					manualDirectionControl = true;
					NPC.spriteDirection = (Main.player[NPC.target].Center.X <= NPC.Center.X).ToDirectionInt();

					if (playerDistance > nudgeRange + 60f)
					{
						NPC.velocity += NPC.DirectionTo(targetData.Center) * 0.1f;
						if (Main.rand.NextBool(30))
						{
							SoundEngine.PlaySound(SoundID.Pixie, NPC.position);
						}
					}
					else if (playerDistance < nudgeRange + 30f)
					{
						Vector2 destination = targetRange.ClosestPointInRect(NPC.Center);
						NPC.velocity += NPC.DirectionTo(destination) * 0.1f;
					}

					float speed = NPC.velocity.Length();
					if (speed > 1f)
						NPC.velocity *= 1f / speed;

					break;
				}

				// Approach the target position's range.
				Vector2 toTargetRange = targetRange.ClosestPointInRect(NPC.Center);
				Vector2 desiredVelocity = NPC.DirectionTo(toTargetRange);
				float distanceSQToRange = NPC.DistanceSQ(toTargetRange);
				if (distanceSQToRange > 150f * 150f)
					desiredVelocity *= 3f;
				else if (distanceSQToRange > 80f * 80f)
					desiredVelocity *= 2f;
				NPC.velocity = Vector2.Lerp(NPC.velocity, desiredVelocity, 0.07f);

				// Hover if not stuck in a time.
				Point centerTileCoordinates = NPC.Center.ToTileCoordinates();
				if (GeneralTimer < 300f)
				{
					GetBirdFlightRecommendation(4, 2, centerTileCoordinates, out bool goDownwards, out bool goUpwards);
					if (goDownwards)
						NPC.velocity.Y += 0.05f;

					if (goUpwards)
						NPC.velocity.Y -= 0.05f;

					NPC.velocity.Y = Math.Clamp(NPC.velocity.Y, -1f, 1f);
				}

				if (WorldGen.SolidTile(centerTileCoordinates))
					GeneralTimer = Math.Min(GeneralTimer + 2f, 400f);
				else
					GeneralTimer = Math.Max(GeneralTimer - 1f, 0f);

				break;
			}
			case FairyAIState.FoundTarget:
			{
				DespawnTimer = 0f;
				NPC.noTileCollide = true;

				if (GeneralTimer == 15f)
				{
					SoundEngine.PlaySound(SoundID.Pixie, NPC.position);
				}

				if (GeneralTimer <= 15f)
				{
					NPC.velocity *= 0.9f;
				}
				else
				{
					manualDirectionControl = true;
					float circlingProgress = GeneralTimer - 15f;
					int currentCircleNumber = (int)(circlingProgress / 50f);
					float circleHeight = (MathF.Cos(currentCircleNumber * 2f) * 10f) + 8f;
					float circleRotation = MathF.Cos(currentCircleNumber) * MathF.PI / 8f * NPC.direction;

					Vector2 offset1 = GetFairyCircleOffset(circlingProgress / 50f, circleRotation, circleHeight);
					Vector2 offset2 = GetFairyCircleOffset((circlingProgress / 50f) + 0.02f, circleRotation, circleHeight);
					NPC.velocity = offset2 - offset1;
					NPC.spriteDirection = (Main.player[NPC.target].Center.X <= NPC.Center.X).ToDirectionInt();
				}

				GeneralTimer += 1f;
				if (Main.netMode != NetmodeID.MultiplayerClient && (GeneralTimer > 200f))
				{
					NPC.active = false;
					if (Main.netMode == NetmodeID.SinglePlayer)
					{
						PurpleFairyEffects(NPC.Center);
					}
					else if (Main.netMode == NetmodeID.Server)
					{
						NPC.netSkip = -1;
						NPC.life = 0;
						NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
						NetUtils.WriteAndSendPacket<PurpleFairyEffectsPacket>(Mod as TeaMod, new PurpleFairyEffectsPacketData { Position = NPC.Center });
					}
				}

				break;
			}
			case FairyAIState.Despawn:
			{
				NPC.noTileCollide = true;
				NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X + (0.05f * NPC.direction), -10f, 10f);
				NPC.velocity.Y = MathHelper.Clamp(NPC.velocity.Y - 0.025f, -5f, 5f);
				NPC.EncourageDespawn(10);
				break;
			}
		}

		NPC.dontTakeDamage = NPC.dontTakeDamageFromHostiles = IsBeingHelpful;

		// Vanilla fairies flutter around each other. Purple fairies shouldn't ever be near many fairies, so they don't.
		// If this feature was ever implemented, it'd require an IL edit to make vanilla fairies recognize custom ones (aiStyle check).

		for (int i = 0; i < Main.maxNPCs; i++)
		{
			if (FairyInteractionAITweak.CanFlutterAround(NPC, Main.npc[i]))
			{
				if (NPC.position.Y < Main.npc[i].position.Y)
					NPC.velocity.Y -= 0.05f;
				else
					NPC.velocity.Y += 0.05f;
			}
		}


		if (!manualDirectionControl)
		{
			NPC.direction = (NPC.velocity.X >= 0f).ToDirectionInt();
			NPC.spriteDirection = -NPC.direction;
		}

		Color dustColor1 = Color.Purple;
		Color dustColor2 = Color.Lavender;

		if ((int)Main.timeForVisualEffects % 2 == 0)
		{
			int dustRadius = 4;
			NPC.position += NPC.netOffset;
			Vector2 dustPosition = NPC.Center - (new Vector2(dustRadius) * 0.5f);
			Dust dust = Dust.NewDustDirect(dustPosition, dustRadius + 4, dustRadius + 4, DustID.FireworksRGB, 0f, 0f, 200, Color.Lerp(dustColor1, dustColor2, Main.rand.NextFloat()), 0.65f);
			dust.velocity = NPC.velocity * 0.3f;
			dust.noGravity = true;
			dust.noLight = true;
			NPC.position -= NPC.netOffset;
		}

		Lighting.AddLight(NPC.Center, dustColor1.ToVector3() * 0.7f);
		if (Main.netMode != NetmodeID.Server)
		{
			Player localPlayer = Main.LocalPlayer;
			if (!localPlayer.dead && localPlayer.HitboxForBestiaryNearbyCheck.Intersects(NPC.Hitbox))
				AchievementsHelper.HandleSpecialEvent(localPlayer, AchievementHelperID.Special.FindAFairy);
		}
	}

	/// <summary>
	/// Finds the closest immobile NPC to this fairy.
	/// </summary>
	/// <param name="targetPosition">The position of the found NPC.</param>
	/// <returns><see langword="true"/> if an NPC was found, <see langword="false"/> otherwise.</returns>
	private bool FindBoundNPCs(out Vector2 targetPosition)
	{
		targetPosition = -Vector2.One;

		// The checked region in tiles.
		Point checkCenter = NPC.Center.ToTileCoordinates();
		Rectangle checkRegion = new(checkCenter.X, checkCenter.Y, 1, 1);
		checkRegion.Inflate(75, 50);
		Rectangle allValidCoordinates = new(0, 0, Main.maxTilesX, Main.maxTilesY);
		allValidCoordinates.Inflate(-Main.offLimitBorderTiles, -Main.offLimitBorderTiles);
		checkRegion = Rectangle.Intersect(checkRegion, allValidCoordinates);

		// Find a target.
		float closestDistanceSquared = -1f;
		for (int i = 0; i < Main.maxNPCs; i++)
		{
			NPC testNPC = Main.npc[i];
			if (i == NPC.whoAmI || !testNPC.active || !checkRegion.Contains(testNPC.Center.ToTileCoordinates()) || !TrackableNPCSystem.ValidNPCToTrack(testNPC))
			{
				continue;
			}

			float distanceSquared = NPC.DistanceSQ(testNPC.Center);
			if (closestDistanceSquared == -1f || distanceSquared < closestDistanceSquared)
			{
				targetPosition = testNPC.Center;
				closestDistanceSquared = distanceSquared;
			}
		}

		return targetPosition != -Vector2.One;
	}

	/// <summary>
	/// Visual effects for purple fairies.
	/// <br/> Vanilla's method, while public, is hardcoded to only accept the three vanilla types.
	/// </summary>
	/// <param name="position">The position to spawn the effects.</param>
	public static void PurpleFairyEffects(Vector2 position)
	{
		Color color1 = Color.Purple;
		Color color2 = Color.Lavender;
		int dustRadius = 4;

		for (int i = 0; i < 40; i++)
		{
			Dust dust = Dust.NewDustDirect(position - (new Vector2(dustRadius) * 0.5f), dustRadius + 4, dustRadius + 4, DustID.FireworksRGB, 0f, 0f, 200, Color.Lerp(color1, color2, Main.rand.NextFloat()), 0.65f);
			dust.velocity *= 1.5f;
			if (i >= 30)
				dust.velocity *= 3.5f;
			else if (i >= 20)
				dust.velocity *= 2f;

			dust.fadeIn = Main.rand.Next(0, 17) * 0.1f;
			dust.noGravity = true;
		}

		SoundEngine.PlaySound(SoundID.Item4, position);
	}

	// From Terraria.NPC.
	/// <summary>
	/// Recommends how a bird-like NPC should move to hover above solid tiles and liquids.
	/// </summary>
	/// <param name="downScanRange">The number of tiles downwards to check.</param>
	/// <param name="upRange">The maximum number of tiles there can be between this NPC and a surface before it starts to move upwards.</param>
	/// <param name="tCoords">The tile coordinates to start checking from.</param>
	/// <param name="goDownwards">If <see langword="true"/>, this NPC should move downwards.</param>
	/// <param name="goUpwards">If <see langword="true"/>, this NPC should move upwars.</param>
	private void GetBirdFlightRecommendation(int downScanRange, int upRange, Point tCoords, out bool goDownwards, out bool goUpwards)
	{
		object[] parameters = new object[] { downScanRange, upRange, tCoords, null, null };
		typeof(NPC).GetCachedMethod(nameof(GetBirdFlightRecommendation)).Invoke(NPC, parameters);
		(goDownwards, goUpwards) = ((bool)parameters[3], (bool)parameters[4]);
	}

	// From Terraria.NPC.
	/// <summary>
	/// Determines the offset from this NPC's position to use to make this NPC naturally circle a point.
	/// </summary>
	/// <param name="elapsedTime">How long this NPC has been circling.</param>
	/// <param name="circleRotation"></param>
	/// <param name="circleHeight"></param>
	/// <returns>The desired offset.</returns>
	private Vector2 GetFairyCircleOffset(float elapsedTime, float circleRotation, float circleHeight)
	{
		return (Vector2)typeof(NPC).GetCachedMethod(nameof(GetFairyCircleOffset)).Invoke(NPC, new object[] { elapsedTime, circleRotation, circleHeight });
	}

	public override bool? CanBeHitByProjectile(Projectile projectile)
	{
		return !IsBeingHelpful;
	}

	public override void HitEffect(int hitDirection, double damage)
	{
		Color dustColor1 = Color.Purple;
		Color dustColor2 = Color.Lavender;
		int dustRadius = 4;
		Vector2 dustPosition = NPC.Center - (new Vector2(dustRadius) * 0.5f);

		if (NPC.life > 0)
		{
			for (int k = 0; k < damage / NPC.lifeMax * 50.0; k++)
			{
				Dust dust16 = Dust.NewDustDirect(dustPosition, dustRadius + 4, dustRadius + 4, DustID.FireworksRGB, 0f, 0f, 200, Color.Lerp(dustColor1, dustColor2, Main.rand.NextFloat()), 0.65f);
				dust16.noGravity = true;
			}
		}
		else
		{
			for (int k = 0; k < 20; k++)
			{
				Dust dust = Dust.NewDustDirect(dustPosition, dustRadius + 4, dustRadius + 4, DustID.FireworksRGB, 0f, 0f, 200, Color.Lerp(dustColor1, dustColor2, Main.rand.NextFloat()), 0.65f);
				dust.velocity *= 1.5f;
				dust.noGravity = true;
			}
		}
	}

	public override Color? GetAlpha(Color drawColor)
	{
		return Color.Lerp(drawColor, Color.White, 0.5f);
	}

	public override void FindFrame(int frameHeight)
	{
		NPC.rotation = NPC.velocity.X * 0.1f;
		if ((NPC.frameCounter += 1.0) >= 4.0)
		{
			NPC.frame.Y += frameHeight;
			NPC.frameCounter = 0.0;
			if (NPC.frame.Y >= frameHeight * 4)
				NPC.frame.Y = 0;
		}
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		SpriteEffects spriteEffects = NPC.spriteDirection != 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
		drawColor = NPC.GetNPCColorTintedByBuffs(drawColor);
		drawColor = NPC.GetAlpha(drawColor);

		Texture2D texture = TextureAssets.Npc[Type].Value;
		Vector2 halfSize = texture.Frame(1, Main.npcFrameCount[Type]).Center();
		Vector2 drawPosition = NPC.Center - screenPos;
		drawPosition -= new Vector2(texture.Width, texture.Height / Main.npcFrameCount[Type]) * NPC.scale / 2f;
		drawPosition += (halfSize * NPC.scale) + new Vector2(0f, Main.NPCAddHeight(NPC) + NPC.gfxOffY);

		spriteBatch.Draw(texture, drawPosition, NPC.frame, drawColor, NPC.rotation, halfSize, NPC.scale, spriteEffects, 0f);
		return false;
	}
}