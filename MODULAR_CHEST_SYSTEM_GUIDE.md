# Modular Chest System Guide

## Overview

The chest system now supports **two distinct chest types** with different behaviors:

1. **Spawned Chests** - Dynamically created when platforms are conquered (all towers destroyed)
2. **Manual Chests** - Pre-placed in the scene by level designers

Both chest types share the same interaction system but have different spawning and gem behavior.

---

## Chest Types

### 1. Spawned Chests (ChestType.Spawned)

**Behavior:**
- ✅ Spawned by `ChestManager` when all towers on a platform are destroyed
- ✅ Start in `Hidden` state (underground)
- ✅ Emerge from the ground with animation
- ✅ **Spawn gems** when opened (configurable count)
- ✅ Automatically marked as "Spawned" by `ChestManager.SetPlatformAssociation()`
- ✅ Grant XP for platform conquest
- ✅ Track mission progress

**Use Case:**
- Reward for conquering platforms
- Dynamic gameplay progression
- Gem economy integration

---

### 2. Manual Chests (ChestType.Manual)

**Behavior:**
- ✅ Placed manually in the Unity scene
- ✅ Start in `Closed` state (visible, no emergence)
- ✅ **Stay closed until player interacts** (press E to open lid)
- ✅ **Do NOT spawn gems** (inventory items only)
- ✅ After opening, press E again to access inventory
- ✅ Grant XP for opening
- ✅ Track mission progress

**Use Case:**
- Secret areas and exploration rewards
- Story/quest chests
- Fixed loot locations
- Level design flexibility

---

## How to Use

### Creating a Manual Chest

1. **Drag chest prefab** into your scene
2. **Position it** where you want players to find it
3. **Configure the ChestController component:**
   - Set `Chest Type` to **Manual**
   - Configure chest parts (body, lid)
   - Set opening animations
   - **Leave gem settings alone** (they're ignored for Manual chests)
4. **Done!** The chest will automatically:
   - Be visible on scene start (closed)
   - **Stay closed until player presses E**
   - Open its lid when player interacts
   - Allow inventory access after opening (press E again)
   - Show inventory items (no gems)

### Creating a Spawned Chest (Automatic)

**You don't need to do anything!** The `ChestManager` handles this automatically:

1. Player destroys all towers on a platform
2. `ChestManager` spawns a chest prefab at the platform center
3. `ChestManager` calls `SetPlatformAssociation()` which:
   - Marks the chest as `ChestType.Spawned`
   - Enables gem spawning
   - Associates it with the platform
4. Chest emerges from ground and opens
5. Gems spawn and scatter

---

## Configuration Reference

### ChestController Inspector Settings

#### Chest Configuration
- **Chest Type** - `Manual` or `Spawned`
  - Manual: Pre-placed, no gems
  - Spawned: Dynamic, spawns gems
- **Current State** - Current chest state (read-only in play mode)

#### Emergence Settings (Spawned Only)
- **Emergence Depth** - How deep underground the chest starts
- **Emergence Duration** - Time to emerge from ground
- **Emergence Curve** - Animation curve for emergence

#### Opening Settings
- **Opening Delay** - Delay before lid opens after emerging
- **Opening Duration** - Time for lid to open
- **Opening Curve** - Animation curve for lid opening

#### Chest Parts
- **Chest Body** - Main chest mesh
- **Chest Lid** - Lid that opens
- **Lid Open Rotation** - Rotation when fully open

#### Gem Spawning (Spawned Chests Only)
- **Gem Prefab** - Gem to spawn (ignored for Manual chests)
- **Min Gem Count** - Minimum gems to spawn
- **Max Gem Count** - Maximum gems to spawn
- **Gem Ejection Force** - Force applied to gems
- **Gem Spawn Height** - Height offset for gem spawn
- **Should Spawn Gems** - Auto-set based on chest type (read-only)

---

## Technical Details

### Chest State Machine

```
Manual Chest Flow:
Closed (waiting for player) → [Player presses E] → Opening → Open → 
[Player presses E] → Interacted (inventory open) → Open (repeatable)

Spawned Chest Flow:
Hidden → Emerging → Closed → Opening → Open → 
[Player presses E] → Interacted (inventory open) → Open (repeatable)
```

### Key Methods

#### `ConfigureChestType()`
- Called in `Awake()`
- Sets `shouldSpawnGems` based on `chestType`
- Configures chest behavior

#### `SetPlatformAssociation(Transform platform)`
- Called by `ChestManager` for spawned chests
- Marks chest as `ChestType.Spawned`
- Enables gem spawning
- Associates chest with platform

#### `StartImmediateEmergence()`
- Called by `ChestManager` to start emergence
- Only works for `Hidden` state chests

---

## Integration with Existing Systems

### ChestManager
- ✅ Automatically marks spawned chests as `Spawned` type
- ✅ No changes needed to existing spawn logic
- ✅ Continues to handle platform conquest detection

### ChestInteractionSystem
- ✅ Works identically for both chest types
- ✅ No changes needed
- ✅ Handles inventory UI, item transfer, XP grants

### Mission System
- ✅ Both chest types count toward mission progress
- ✅ Platform conquest tracked separately
- ✅ Chest looting tracked when opened

### XP System
- ✅ Both chest types grant XP when opened
- ✅ Platform conquest grants additional XP (Spawned only)

---

## Best Practices

### For Manual Chests

1. **Placement**
   - Place in interesting/hidden locations
   - Consider player exploration paths
   - Use as rewards for puzzle solving

2. **Visual Feedback**
   - Ensure chest is visible (not underground)
   - Add particle effects or glow for visibility
   - Consider adding a marker or hint

3. **Loot Configuration**
   - Configure loot in `ChestInteractionSystem.possibleItems`
   - Manual chests use same loot pool as spawned chests
   - Self-revive items can spawn in both types

### For Spawned Chests

1. **Platform Design**
   - Ensure platform has clear center for chest spawn
   - Avoid obstacles at spawn location
   - Consider chest visibility after emergence

2. **Gem Configuration**
   - Balance gem count with game economy
   - Adjust ejection force for platform size
   - Test gem physics on different platform types

---

## Troubleshooting

### Manual Chest Not Opening
- ✅ Check `Chest Type` is set to `Manual`
- ✅ Verify `chestLid` is assigned
- ✅ Check lid rotation settings
- ✅ Enable debug logs to see state transitions

### Manual Chest Spawning Gems
- ✅ Verify `Chest Type` is `Manual` (not `Spawned`)
- ✅ Check `shouldSpawnGems` is false in inspector
- ✅ If true, chest was incorrectly marked as Spawned

### Spawned Chest Not Spawning Gems
- ✅ Check `gemPrefab` is assigned
- ✅ Verify `ChestManager` called `SetPlatformAssociation()`
- ✅ Check `shouldSpawnGems` is true in inspector
- ✅ Enable debug logs to see gem spawn attempts

### Chest Not Interactable
- ✅ Chest must be in `Open` state
- ✅ Check `ChestInteractionSystem` is in scene
- ✅ Verify player is looking at chest (raycast detection)
- ✅ Ensure chest has collider for detection

---

## Example Scenarios

### Scenario 1: Hidden Treasure Chest
```
1. Place chest prefab in secret cave
2. Set Chest Type to Manual
3. Configure opening animation
4. Add particle effect for discovery
5. Player finds closed chest
6. Player presses E → chest lid opens
7. Player presses E again → inventory opens, gets loot (no gems)
```

### Scenario 2: Platform Conquest Reward
```
1. Player destroys all towers on platform
2. ChestManager spawns chest automatically
3. Chest emerges from ground
4. Chest opens, gems scatter
5. Player presses E, gets loot + gems
```

### Scenario 3: Quest Reward Chest
```
1. Place chest in quest location
2. Set Chest Type to Manual
3. Initially disable GameObject (hidden)
4. Enable when quest completes (script)
5. Player finds closed chest
6. Player presses E → chest opens
7. Player presses E again → gets quest reward
```

---

## Code Example: Spawning a Manual Chest at Runtime

```csharp
// Spawn a manual chest at a specific location
public void SpawnManualChest(Vector3 position)
{
    GameObject chestObj = Instantiate(chestPrefab, position, Quaternion.identity);
    ChestController chest = chestObj.GetComponent<ChestController>();
    
    if (chest != null)
    {
        // Configure as manual chest
        chest.chestType = ChestController.ChestType.Manual;
        
        // Chest will automatically configure itself in Awake()
        // No need to call SetPlatformAssociation() for manual chests
    }
}
```

---

## Summary

The modular chest system provides flexibility for both dynamic gameplay rewards (Spawned) and level design (Manual) while maintaining a unified interaction system. Both types work seamlessly with existing systems (XP, missions, inventory) without breaking any current functionality.

**Key Takeaway:** 
- **Spawned chests** = Platform conquest rewards with gems (auto-open after emergence)
- **Manual chests** = Level design flexibility without gems (stay closed until player interacts)
- **Interaction flow** = Manual chests require E to open, then E again for inventory
- **Same loot system** = Both use the same inventory interaction once open
