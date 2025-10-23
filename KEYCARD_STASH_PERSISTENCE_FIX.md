# Keycard Stash Persistence Fix

## Problem
Keycards were not persisting in the StashManager when placed in stash slots. They would disappear after closing and reopening the stash menu.

## Root Cause
The `StashManager.GetItemResourcePath()` method was only looking for items in the `Items/` folder, but keycards are stored in `Assets/Resources/Keycards/`. This caused:
1. **Save Issue**: When saving, the wrong path was stored (e.g., `Items/Red_Keycard` instead of `Keycards/Red_Keycard`)
2. **Load Issue**: When loading, Unity couldn't find the keycard at the wrong path, so slots appeared empty

## Solution
Updated `StashManager.cs` with enhanced path resolution logic that matches `InventoryManager.cs`:

### Changes Made

#### 1. Enhanced `GetItemResourcePath()` Method
- Added keycard detection by checking `item.itemType == "keycard"`
- Added multiple keycard-specific path attempts:
  - `Keycards/{itemName}`
  - `Keycards/{itemName.Replace(" ", "")}`
  - `Items/Keycards/{itemName}`
  - And more fallback paths
- Tests each path with `Resources.Load<ChestItemData>()` to find the correct one
- Logs warnings if keycard assets are not in Resources folder

#### 2. Enhanced `LoadSlotFromData()` Method
- Added fallback path loading for keycards
- If primary path fails, tries multiple alternative paths
- Detects keycards by checking if item name contains "keycard"
- Provides clear error messages if keycard cannot be loaded

## Technical Details

### Keycard Asset Locations
Keycards are stored in: `Assets/Resources/Keycards/`
- Red_Keycard.asset
- Blue_Keycard.asset
- Green_Keycard.asset
- Black_Keycard.asset
- Building21_Keycard.asset

### Resource Path Resolution
Unity's `Resources.Load()` requires paths relative to any `Resources/` folder:
- ✅ Correct: `Keycards/Red_Keycard`
- ❌ Wrong: `Items/Red_Keycard`

### Save/Load Flow
1. **Save**: `GetItemResourcePath()` detects keycard type → tests paths → finds `Keycards/Red_Keycard` → saves correct path
2. **Load**: `LoadSlotFromData()` reads path → tries to load → if fails, tries alternatives → successfully loads keycard

## Testing
To verify the fix:
1. Place a keycard in any stash slot
2. Close the stash menu
3. Reopen the stash menu
4. ✅ Keycard should still be there

Check Unity Console for logs:
- `[StashManager] Found keycard at path: 'Keycards/Red_Keycard'` (during save)
- `[StashManager] Successfully loaded Red Keycard x1 into slot` (during load)

## Files Modified

### 1. `Assets/scripts/StashManager.cs`
- **`GetItemResourcePath()`** - Enhanced with keycard path detection
  - Detects keycard items by checking `itemType == "keycard"`
  - Tests multiple keycard-specific paths
  - Falls back to standard item paths if not a keycard
- **`LoadSlotFromData()`** - Enhanced with fallback path loading
  - Tries primary path first
  - If failed, detects keycards by name and tries alternative paths
  - Provides clear error messages for missing keycards

### 2. `Assets/scripts/PersistentItemInventoryManager.cs`
- **`ApplyToInventoryManager()`** - Enhanced item loading with fallback paths
  - Added same fallback logic when loading items from persistent storage
  - Ensures keycards persist across game sessions (not just in stash)
  - Matches the enhanced path resolution in `GetItemResourcePath()`

## Notes
- This fix matches the existing implementation in `InventoryManager.cs`
- All other items (gems, consumables, etc.) continue to work as before
- The fix is backward compatible - old save files will still work with fallback path loading
- **IMPORTANT**: Keycards must be in `Assets/Resources/Keycards/` folder to persist properly
  - The system will log warnings if keycards are not found in Resources folder
