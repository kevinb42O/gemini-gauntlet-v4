# Keycard Persistence Fix

## Problem
Keycards were not persisting when closing the game or losing focus, even though other items were saving correctly.

## Root Cause
Unity's `Resources.Load()` method **only works with assets located in a `Resources` folder**. The keycard assets were located in:
```
Assets/prefabs_made/KEYCARDS/Keycards/
```

This is NOT a Resources folder, so the inventory system couldn't reload keycards after saving them.

## Solution Overview
1. **Updated save/load code** to detect and handle keycards specially
2. **Created an Editor tool** to copy keycards to the proper Resources folder
3. **Added warning messages** when keycards can't be found during save/load

## Files Modified

### 1. `InventoryManager.cs`
- **Updated `GetItemResourcePath()`**: Now checks if item is a keycard and tries keycard-specific paths first
- **Updated `LoadSlotFromData()`**: Enhanced with keycard-specific path fallbacks
- **Added warnings**: Clear console messages when keycards aren't in Resources folder

### 2. `PersistentItemInventoryManager.cs`
- **Updated `GetItemResourcePath()`**: Mirrors the keycard handling from InventoryManager
- **Added path validation**: Tests multiple possible keycard locations
- **Enhanced logging**: Better debugging for keycard save/load operations

### 3. `KeycardResourceMover.cs` (NEW)
- **Editor utility window**: Accessible via `Tools > Keycard Setup > Move Keycards to Resources Folder`
- **Automated copying**: Safely copies keycard assets to `Assets/Resources/Keycards/`
- **Preserves originals**: Uses copy instead of move to avoid breaking existing references

## How to Fix (Choose ONE method)

### Method 1: Use the Editor Tool (RECOMMENDED)
1. In Unity, go to menu: **Tools > Keycard Setup > Move Keycards to Resources Folder**
2. Click the **"Copy Keycards to Resources Folder"** button
3. Wait for confirmation dialog
4. Done! Keycards will now persist

### Method 2: Manual Setup
1. Create folder: `Assets/Resources/Keycards/`
2. Navigate to: `Assets/prefabs_made/KEYCARDS/Keycards/`
3. Select all `.asset` files (Black_Keycard, Blue_Keycard, etc.)
4. Copy them (Ctrl+C)
5. Navigate to: `Assets/Resources/Keycards/`
6. Paste (Ctrl+V)
7. Unity will automatically update references

## Verification

### Test in Unity Editor
1. Start the game
2. Collect a keycard
3. Open inventory (TAB) and verify keycard is there
4. Exit to menu or close the game
5. Start the game again
6. Open inventory - keycard should still be there!

### Check Console Logs
When saving a keycard, you should see:
```
[InventoryManager] Found keycard at path: 'Keycards/Black_Keycard'
```

If you see warnings like:
```
⚠️ KEYCARD 'Black_Keycard' NOT FOUND in Resources folder!
```
Then the keycards haven't been moved to Resources yet.

## Technical Details

### Why Resources Folder?
Unity's `Resources.Load<T>(path)` is a runtime loading system that ONLY works with assets in folders named "Resources". This is a Unity engine limitation, not a bug in the code.

### Keycard Paths Checked (in order)
1. `Keycards/{itemName}`
2. `Keycards/{itemName without spaces}`
3. `Items/Keycards/{itemName}`
4. `Items/Keycards/{itemName without spaces}`

### Affected Keycards
- Black_Keycard.asset
- Blue_Keycard.asset
- Building21_Keycard.asset
- Green_Keycard.asset
- Red_Keycard.asset

## Troubleshooting

### Keycards still not persisting?
1. Check Unity console for error messages
2. Verify keycards are in `Assets/Resources/Keycards/`
3. Make sure the `.asset` files were copied, not just shortcuts
4. Try deleting `persistent_inventory.json` and `inventory_data.json` from `%AppData%\..\LocalLow\[YourCompany]\[YourGame]\` and test again

### Editor tool not showing up?
1. Make sure `KeycardResourceMover.cs` is in `Assets/scripts/Editor/` folder
2. Wait for Unity to recompile scripts
3. Check for compilation errors in Console

### Keycards disappear after moving to Resources?
This shouldn't happen because we use `AssetDatabase.CopyAsset()` which preserves references. If it does:
1. Check the original keycards in `prefabs_made/KEYCARDS/Keycards/` still exist
2. Update any KeycardItem prefabs to reference the new Resources location

## Future Considerations

### Adding New Keycards
When adding new keycards:
1. Create the keycard asset in `Assets/Resources/Keycards/` directly, OR
2. Create it anywhere, then use the Editor tool to copy it to Resources

### Alternative Solutions (Not Recommended)
- **Addressables System**: More complex, overkill for this use case
- **ScriptableObject Database**: Would require refactoring the entire item system
- **Serialization of Full Item Data**: Would bloat save files significantly

## Summary
✅ Keycards now have proper persistence support  
✅ Code updated to handle keycard-specific paths  
✅ Editor tool provided for easy setup  
✅ Clear warnings when keycards aren't in Resources folder  
✅ All other items continue to work as before  

**Action Required**: Run the Editor tool once to copy keycards to Resources folder, then keycards will persist forever!
