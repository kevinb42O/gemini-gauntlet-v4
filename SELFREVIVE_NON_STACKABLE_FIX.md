# Self-Revive Non-Stackable Fix

## Issue
Self-revive items were stacking in inventory slots, which shouldn't happen. Each self-revive should occupy its own slot since they're unique items.

## Root Cause
The base `ChestItemData` class uses `IsSameItem()` to determine if items can stack. Self-revive items were inheriting this behavior and stacking like regular items.

## Solution

### 1. SelfReviveItemData.cs - Override Stacking Behavior
**Added two method overrides:**

```csharp
/// <summary>
/// Self-revive items should NEVER stack - each one is unique
/// </summary>
public override bool CanStackWith(ChestItemData other)
{
    return false; // Self-revives NEVER stack
}

/// <summary>
/// Self-revive items are NOT the same for stacking purposes (each is unique)
/// </summary>
public override bool IsSameItem(ChestItemData other)
{
    return false; // Self-revives are always treated as different items for stacking
}
```

**Why this works:**
- `IsSameItem()` is used by the unified stacking system throughout the codebase
- By returning `false`, self-revives will never stack with each other
- Each self-revive will occupy its own inventory slot
- Works automatically with all existing stacking logic (inventory, chest, stash)

### 2. InventoryManager.cs - Better Equip Feedback
**Updated double-click equip handler:**
- Now explicitly checks if revive slot is full BEFORE attempting to equip
- Provides clear feedback: "revive slot already has a self-revive equipped!"
- Prevents confusion when trying to equip multiple self-revives

### 3. ReviveSlotController.cs - Better Drag-Drop Feedback
**Updated drag-drop handler:**
- Enhanced feedback messages with emojis for clarity
- Added debug log when attempting to equip via drag-drop
- Consistent messaging with double-click handler

## How It Works Now

### Acquiring Multiple Self-Revives
✅ **Each self-revive occupies its own slot**
- Pick up 3 self-revives from chests → takes 3 inventory slots
- Cannot stack them together
- Each one is treated as a unique item

### Equipping Self-Revives
✅ **Only ONE can be equipped at a time**
- Double-click or drag-drop a self-revive → equips to revive slot
- Try to equip another → shows message "revive slot already has a self-revive equipped!"
- Must unequip current one first (double-click revive slot)

### Inventory Management
✅ **Self-revives work like unique items**
- Can carry multiple in inventory (each in separate slots)
- Can store multiple in stash (each in separate slots)
- Cannot stack them together
- All persist correctly across scenes

## Testing Results

### ✅ Non-Stacking Behavior
- [x] Pick up 2 self-revives → occupy 2 separate slots
- [x] Try to drag one onto another → doesn't stack
- [x] System uses `IsSameItem()` which returns false for self-revives

### ✅ Equip Awareness
- [x] Equip first self-revive → works
- [x] Try to equip second → shows "already equipped" message
- [x] Unequip first → can now equip second
- [x] Works for both double-click and drag-drop

### ✅ Persistence
- [x] Multiple self-revives in inventory persist across scenes
- [x] Multiple self-revives in stash persist across sessions
- [x] Each self-revive maintains its own slot

## Technical Details

### Why Override IsSameItem()?
The unified stacking system uses `IsSameItem()` to determine if items can stack:
- `InventoryManager.GetSlotWithItem()` - finds existing stacks
- `InventoryManager.CanStackInventoryItems()` - validates stacking
- `ChestInteractionSystem.TryTransferToChest()` - chest stacking
- `StashManager` - stash stacking

By overriding `IsSameItem()` to return `false`, self-revives automatically become non-stackable everywhere.

### Why Also Override CanStackWith()?
- Provides explicit intent in code
- Future-proof if stacking logic changes
- Makes it clear that self-revives are special

## Backwards Compatibility
✅ **Fully compatible**
- Existing self-revives in inventory will work correctly
- If player somehow has stacked self-revives from old saves, they'll work but won't stack further
- No breaking changes

## Summary
Self-revive items are now properly treated as unique, non-stackable items. Each one occupies its own inventory/stash slot, and the system prevents equipping multiple self-revives at once with clear feedback messages.
