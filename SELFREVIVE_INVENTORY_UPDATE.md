# Self-Revive Inventory System Update

## Overview
Updated the self-revive item system to allow self-revive items to be stored in normal inventory slots and stash slots, while maintaining the special persistent revive slot functionality.

## Changes Made

### 1. InventoryManager.cs
**Removed hardcoded routing** (Line ~365-376)
- Self-revive items are no longer automatically forced into the revive slot
- They now behave like regular items and can be stored in any inventory slot
- Added `IsSelfReviveItem()` helper method to identify self-revive items

**Added double-click equip functionality** (Line ~1488-1508)
- Double-clicking a self-revive item in inventory now auto-equips it to the revive slot (if empty)
- Provides quick equip functionality similar to other RPG games
- Shows appropriate feedback messages

### 2. ReviveSlotController.cs
**Added drag-drop support** (Line ~273-311)
- Implemented `OnDrop()` handler to accept self-revive items dragged from inventory
- Validates that only self-revive items can be dropped
- Enforces the "only 1 revive at a time" rule
- Automatically saves changes after equipping

**Added double-click unequip functionality** (Line ~265-342)
- Implemented `IPointerClickHandler` interface
- Double-clicking the revive slot moves the self-revive back to inventory (if space available)
- Finds the self-revive item asset using multiple fallback paths
- Provides bidirectional equip/unequip workflow

### 3. PersistentItemInventoryManager.cs
**Enhanced resource path lookup** (Line ~430-454)
- Added special handling for self-revive items in `GetItemResourcePath()`
- Tries multiple common paths for self-revive items:
  - `Items/{itemName}`
  - `Items/SelfRevive/{itemName}`
  - `SelfRevive/{itemName}`
- Ensures self-revive items can be properly saved and loaded across scenes

## How It Works Now

### Acquiring Self-Revive Items
1. **From Chests**: Self-revive items spawn in chests (15% chance by default)
2. **Pickup**: They go into regular inventory slots like any other item
3. **Stash Storage**: Can be stored in stash for safekeeping

### Equipping Self-Revive Items
**Method 1: Double-Click**
- Double-click a self-revive item in inventory
- Automatically equips to revive slot if empty
- Shows feedback if slot is full

**Method 2: Drag-Drop**
- Drag self-revive item from inventory
- Drop onto the revive slot
- Visual feedback during drag operation

### Unequipping Self-Revive Items
**Method 1: Double-Click**
- Double-click the equipped revive slot
- Moves self-revive back to first empty inventory slot
- Shows feedback if inventory is full

**Method 2: Drag-Drop** (future enhancement)
- Could drag from revive slot back to inventory
- Not yet implemented but architecture supports it

### Persistence
- **In Inventory/Stash**: Saved as regular items with full persistence
- **In Revive Slot**: Saved separately as `selfReviveCount` (max 1)
- **Cross-Scene**: Both systems work across scene transitions
- **On Death**: 
  - Inventory items are cleared
  - Revive slot is consumed if used, otherwise cleared

## Testing Checklist

### Basic Functionality
- [ ] Self-revive items appear in chests
- [ ] Can pick up self-revive into regular inventory slots
- [ ] Can store self-revive in stash slots
- [ ] Can stack multiple self-revives in inventory (if stackable)

### Equipping
- [ ] Double-click self-revive in inventory → equips to revive slot
- [ ] Drag self-revive from inventory → drop on revive slot → equips
- [ ] Cannot equip when revive slot is full (shows message)
- [ ] Revive slot shows icon when equipped

### Unequipping
- [ ] Double-click revive slot → moves to inventory
- [ ] Cannot unequip when inventory is full (shows message)
- [ ] Revive slot clears after unequipping

### Persistence
- [ ] Self-revive in inventory persists across scene transitions
- [ ] Self-revive in stash persists across game sessions
- [ ] Equipped self-revive persists across scenes
- [ ] Self-revive in inventory is cleared on death
- [ ] Equipped self-revive is consumed on death (if used)

### Edge Cases
- [ ] Multiple self-revives in inventory work correctly
- [ ] Self-revive in stash + inventory + revive slot all work together
- [ ] Dying with self-revive in inventory (not equipped) → items lost
- [ ] Dying with self-revive equipped → revive triggers, slot cleared
- [ ] Exfiltrating with self-revives → all persist correctly

## Important Notes

### For Players
- **You can now carry multiple self-revives!** Store extras in inventory or stash
- **Only ONE can be equipped** in the special revive slot at a time
- **Unequipped self-revives are lost on death** - make sure to equip one before combat!
- **Equipped self-revive persists across scenes** - it's always ready when needed

### For Developers
- Self-revive items must be in a Resources folder for persistence to work
- Recommended path: `Assets/Resources/Items/Self-Revive` or `Assets/Resources/SelfRevive/`
- The system uses multiple fallback paths to find the asset
- Self-revive items inherit from `SelfReviveItemData` which inherits from `ChestItemData`

## Backwards Compatibility
✅ **Fully backwards compatible**
- Existing self-revive slot functionality unchanged
- Old saves with equipped self-revives will load correctly
- No breaking changes to existing systems

## Future Enhancements (Optional)
- Add drag-drop FROM revive slot back to inventory (currently only double-click)
- Add visual indicator showing how many self-revives are in inventory
- Add hotkey to quick-equip self-revive from inventory
- Add confirmation dialog when unequipping during combat
