# Keycard Chest Integration Guide

## Overview
This guide explains how keycards integrate with the chest system to spawn as loot.

---

## How It Works

### Chest Loot Generation Flow

```
Player Opens Chest
       ↓
ChestInteractionSystem.OpenChest()
       ↓
GenerateChestItems()
       ↓
Randomly selects 3-6 items from "Possible Items" list
       ↓
Keycards included in random selection
       ↓
Items displayed in chest UI
       ↓
Player drags keycard to inventory
       ↓
Keycard now available for door interaction
```

---

## Configuration

### ChestInteractionSystem Component

Located on a GameObject in your scene (usually named "ChestInteractionSystem")

**Key Settings:**
```
Items and Generation:
  └─ Possible Items (List)
      ├─ [Existing items...]
      ├─ Building21_Keycard  ← Add these
      ├─ Green_Keycard       ← Add these
      ├─ Blue_Keycard        ← Add these
      ├─ Black_Keycard       ← Add these
      └─ Red_Keycard         ← Add these
```

---

## Spawn Mechanics

### Random Selection
- Each chest generates **3-6 random items**
- Items are selected from the **Possible Items** list
- Each item has an **equal chance** of being selected
- Keycards compete with other items for slots

### Rarity System
Keycards use the standard rarity system:
- **Rarity 1**: Common (Gray)
- **Rarity 2**: Uncommon (Green) - Green Keycard default
- **Rarity 3**: Rare (Blue) - Building21 & Blue Keycard default
- **Rarity 4**: Epic (Purple) - Red Keycard default
- **Rarity 5**: Legendary (Orange) - Black Keycard default

Higher rarity = more valuable, but doesn't affect spawn rate in basic system

---

## Controlling Keycard Spawn Rates

### Method 1: Duplicate Entries (Easy)
Add the same keycard multiple times to increase spawn chance:

```
Possible Items:
  ├─ Item A
  ├─ Item B
  ├─ Green_Keycard  ← 3x more likely than others
  ├─ Green_Keycard
  ├─ Green_Keycard
  ├─ Blue_Keycard   ← Normal chance
  └─ Red_Keycard    ← Normal chance
```

### Method 2: Fewer Entries (Easy)
Only add rare keycards once to make them uncommon:

```
Possible Items:
  ├─ [20 common items...]
  ├─ Black_Keycard  ← 1 in 21 chance
  └─ Red_Keycard    ← 1 in 21 chance
```

### Method 3: Separate Spawn System (Advanced)
Modify `ChestInteractionSystem.GenerateChestItems()` to add keycard-specific spawn logic:

```csharp
// After normal item generation
if (Random.Range(0f, 100f) <= keycardSpawnChance)
{
    ChestItemData randomKeycard = keycards[Random.Range(0, keycards.Length)];
    currentChestItems.Add(randomKeycard);
}
```

---

## Testing Checklist

### Setup Verification
- [ ] ChestInteractionSystem exists in scene
- [ ] All 5 keycards added to Possible Items
- [ ] Keycards have proper names and icons
- [ ] Keycards have correct rarity values

### In-Game Testing
- [ ] Open multiple chests
- [ ] Verify keycards appear in some chests
- [ ] Collect keycard from chest to inventory
- [ ] Open inventory (Tab) to verify keycard is there
- [ ] Use keycard on matching door
- [ ] Verify keycard is consumed after door opens

---

## Troubleshooting

### Keycards Not Spawning in Chests

**Problem**: Opened multiple chests, no keycards found

**Solutions**:
1. Check ChestInteractionSystem has keycards in Possible Items list
2. Verify keycard ScriptableObjects are not null
3. Increase number of keycard entries in Possible Items
4. Check that keycards are not filtered out (not GemItemData type)

### Keycards Not Transferring to Inventory

**Problem**: Can see keycard in chest but can't collect it

**Solutions**:
1. Verify InventoryManager.Instance exists in scene
2. Check inventory is not full (press Tab to see)
3. Ensure keycard is ChestItemData type (not a different type)
4. Try dragging keycard to inventory slot manually

### Keycard Collected But Can't Open Door

**Problem**: Have keycard in inventory but door won't open

**Solutions**:
1. Verify door's Required Keycard matches the one in inventory
2. Check that keycard ScriptableObject is the same instance
3. Ensure KeycardDoor script is properly configured
4. Test with Debug: Right-click KeycardDoor → Test Open Door

---

## Advanced: Custom Keycard Loot Tables

### Create Keycard-Specific Chest

If you want certain chests to ONLY contain keycards:

1. Create a new ChestItemSet ScriptableObject
2. Add only keycards to the items array
3. Modify specific chest to use this item set
4. This chest will only spawn keycards

### Guaranteed Keycard Spawn

To guarantee a specific keycard spawns in a chest:

1. Create a unique ChestInteractionSystem for that chest
2. Set Possible Items to only contain that keycard
3. Set min/max item count to 1
4. Player will always get that keycard from this chest

---

## Integration with Existing Systems

### Chest Persistence
- Chest loot is generated **once** when first opened
- Loot persists even if player closes and reopens chest
- Keycards will remain in chest until collected
- Uses `persistentChestLoot` dictionary to track loot per chest

### Inventory System
- Keycards use standard InventoryManager
- Stack with other keycards of same type
- Can be dropped, traded, or stored
- Persist across scene loads (if inventory persists)

### Save System
- If your game saves inventory, keycards are saved
- Chest loot persistence is per-session (not saved between game sessions)
- Consider adding save/load for chest loot if needed

---

## Example Configurations

### Balanced Setup (Recommended)
```
Possible Items (30 total):
  ├─ [20 common items]
  ├─ [5 uncommon items]
  ├─ Building21_Keycard (1x) - ~3% chance
  ├─ Green_Keycard (1x) - ~3% chance
  ├─ Blue_Keycard (1x) - ~3% chance
  ├─ Black_Keycard (1x) - ~3% chance
  └─ Red_Keycard (1x) - ~3% chance

Result: ~15% chance to find A keycard per chest
```

### Common Keycards Setup
```
Possible Items (20 total):
  ├─ [10 common items]
  ├─ Building21_Keycard (2x) - ~10% chance
  ├─ Green_Keycard (2x) - ~10% chance
  ├─ Blue_Keycard (2x) - ~10% chance
  ├─ Black_Keycard (2x) - ~10% chance
  └─ Red_Keycard (2x) - ~10% chance

Result: ~50% chance to find A keycard per chest
```

### Rare Keycards Setup
```
Possible Items (50 total):
  ├─ [45 common items]
  ├─ Building21_Keycard (1x) - ~2% chance
  ├─ Green_Keycard (1x) - ~2% chance
  ├─ Blue_Keycard (1x) - ~2% chance
  ├─ Black_Keycard (1x) - ~2% chance
  └─ Red_Keycard (1x) - ~2% chance

Result: ~10% chance to find A keycard per chest
```

---

## Summary

✓ Keycards integrate seamlessly with chest system  
✓ Add to ChestInteractionSystem Possible Items list  
✓ Spawn randomly alongside other loot  
✓ Can be collected from chests like any other item  
✓ Work with doors after collection  
✓ Spawn rates configurable via list entries  

**No code changes required - just configuration!**
