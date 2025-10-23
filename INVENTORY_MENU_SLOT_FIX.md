# Inventory Menu Slot Display Fix

## Problem
When transitioning from game to menu via ExitZone, **0 inventory slots were being shown** in the menu, even though items were still stored. This made it appear as if all items were lost, but they were actually still present when returning to the game.

## Root Cause
The issue was a **race condition during scene initialization**:

1. **ExitZone** saves inventory to `PersistentItemInventoryManager` ✅
2. **Menu scene loads** → Menu's `InventoryManager` tries to initialize
3. `PersistentItemInventoryManager.ApplyToInventoryManager()` restores backpack data
4. `BackpackSlotController.LoadFromSaveData()` calls `EquipBackpack()`
5. `EquipBackpack()` calls `NotifyInventoryManager()` → `UpdateInventorySlotCount()` → `UpdateSlotVisibility()`
6. **PROBLEM**: `UpdateSlotVisibility()` tries to access `inventorySlots` list, but it's **empty or contains stale references** from the previous scene

The menu's `InventoryManager.inventorySlots` list wasn't properly initialized before the backpack system tried to update slot visibility.

## Solution

### 1. Added Safety Checks in `UpdateInventorySlotCount()`
```csharp
// CRITICAL FIX: Ensure slots are initialized before updating visibility
if (inventorySlots == null || inventorySlots.Count == 0)
{
    Debug.LogWarning($"[InventoryManager] UpdateInventorySlotCount called but inventorySlots is empty - re-initializing slots");
    ValidateAndAssignReferences();
    
    // If still empty after validation, just store the slot count and return
    if (inventorySlots == null || inventorySlots.Count == 0)
    {
        Debug.LogWarning($"[InventoryManager] Cannot update slot visibility - no slots found. Storing slot count {newSlotCount} for later.");
        currentActiveSlots = newSlotCount;
        return;
    }
}
```

### 2. Added Safety Check in `UpdateSlotVisibility()`
```csharp
// CRITICAL FIX: Safety check - don't try to update visibility if slots aren't initialized
if (inventorySlots == null || inventorySlots.Count == 0)
{
    Debug.LogWarning("[InventoryManager] UpdateSlotVisibility called but inventorySlots is empty - skipping");
    return;
}
```

### 3. Added Delayed Slot Visibility Update for Menu
```csharp
// CRITICAL FIX: Ensure slot visibility is updated after loading (especially important for menu scene)
if (inventoryContext == InventoryContext.Menu)
{
    StartCoroutine(EnsureSlotVisibilityAfterLoad());
}
```

This coroutine waits one frame after initialization to ensure all references are properly set up, then updates slot visibility.

### 4. Added Slot Visibility Refresh in `ShowInventoryUI()`
```csharp
// CRITICAL FIX: Ensure slots are properly visible when UI is shown
// This handles the case where the menu loads and needs to show the correct number of slots
if (inventorySlots != null && inventorySlots.Count > 0)
{
    UpdateSlotVisibility();
}
```

### 5. Improved `InitializeInventorySlots()`
- Added `inventorySlots.Clear()` to prevent duplicates when re-initializing
- Added debug logging to track slot initialization

## Testing Steps

1. **Start in game scene** with items in inventory
2. **Equip a backpack** (e.g., Tier 2 or Tier 3)
3. **Collect some items** (weapons, armor, etc.)
4. **Go to ExitZone** to return to menu
5. **Check menu inventory**:
   - ✅ Should show correct number of slots based on equipped backpack
   - ✅ Should display all items that were in inventory
   - ✅ Should show correct backpack tier in backpack slot
6. **Return to game** from menu
7. **Check game inventory**:
   - ✅ All items should still be present
   - ✅ Backpack should be at the same tier

## Files Modified

### `Assets/scripts/InventoryManager.cs`
- Added safety checks in `UpdateInventorySlotCount()`
- Added safety check in `UpdateSlotVisibility()`
- Added `EnsureSlotVisibilityAfterLoad()` coroutine for menu context
- Added slot visibility refresh in `ShowInventoryUI()`
- Improved `InitializeInventorySlots()` with clear and logging

## Expected Behavior After Fix

### In Menu Scene
- **Inventory slots**: Visible based on equipped backpack tier
  - Tier 1 backpack: 5 slots visible
  - Tier 2 backpack: 10 slots visible
  - Tier 3 backpack: 15 slots visible
- **Items**: All items from game scene are displayed in their correct slots
- **Backpack slot**: Shows the equipped backpack with correct tier
- **Gem slot**: Always visible with correct gem count

### In Game Scene
- **Inventory persistence**: All items persist when returning from menu
- **Backpack persistence**: Backpack tier is maintained
- **No item loss**: Items are never lost during scene transitions

## Debug Logging
The fix includes comprehensive debug logging to track:
- Slot initialization status
- Slot visibility updates
- Reference validation
- Persistence system operations

Check the Unity Console for messages like:
- `[InventoryManager] Initialized X inventory slots`
- `[InventoryManager] EnsureSlotVisibilityAfterLoad: Updating visibility for X active slots`
- `[InventoryManager] Updated slot visibility - X regular slots active`

## Additional Notes

- The fix is **non-destructive** - it only adds safety checks and doesn't change core functionality
- The fix handles **both menu and game contexts** properly
- The fix is **backwards compatible** with existing save data
- The fix includes **graceful degradation** - if slots can't be initialized, it stores the slot count for later use
