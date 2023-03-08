# Rescue Fairies

![Rescue Fairies mod icon](icon.png)

**Rescue Fairies** is a tModLoader mod that adds in a new type of fairy: Purple fairies!

These new fairies seek out bound NPCs instead of treasure. Though be warned, not every NPC they find is friendly...

The idea for this mod was suggested by a friend.

[Download the mod on Steam!](https://steamcommunity.com/sharedfiles/filedetails/?id=2943805073)

## Mod Calls

| Call | Description | Example
| --- | --- | --- |
| `"AddTrackingCondition", int npcId : void` | Registers a single NPC type to be tracked by purple fairies. | `mod.Call("AddTrackingCondition", ModContent.NPCType<T>());` | 
| `"AddTrackingCondition", Func<NPC, bool> condition : void` | Registers a general condition for NPCs to be tracked. The `NPC` passed into `condition` is the NPC that should be tracked. | `mod.Call("AddTrackingCondition", npc => npc.boss);` | 
| `"AddBlacklist", int npcId : void` | Registers a single NPC type to *never* be tracked by purple fairies. |`mod.Call("AddBlacklist", ModContent.NPCType<T>());` | 
| `"AddBlacklist", Func<NPC, bool> condition : void` | Registers a general condition for NPCs to not be tracked. The `NPC` passed into `condition` is the NPC that shouldn't be tracked. | `mod.Call("AddBlacklist", npc => npc.aiStyle == NPCAIStyleID.FaceNearestPlayer && npc.ModNPC?.Mod is MyMod);` |

The blacklist takes priority over tracking conditions. The default conditions can be found in `RescueFairies.Common.Systems.TrackableNPCSystem`.