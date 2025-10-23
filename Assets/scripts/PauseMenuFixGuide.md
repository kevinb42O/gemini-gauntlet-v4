# Pause Menu Fix Guide

## Quick Diagnosis Steps

1. **Add the Diagnostic Script**
   - Add `UIManagerDiagnostic.cs` to any GameObject in your scene
   - Press F1 in Play mode to run full diagnostics
   - Check the Console for detailed information

2. **Common Issues & Solutions**

### Issue 1: Pause Menu Panel Not Assigned
**Symptoms:** Error message "pauseMenuPanel is null"
**Solution:** 
- Select your UIManager GameObject in the scene
- In the Inspector, find the "Game State UI Panels" section
- Drag your pause menu panel GameObject into the "Pause Menu Panel" field

### Issue 2: Canvas Group Alpha is 0
**Symptoms:** Panel exists but is invisible
**Solution:**
- Find your pause menu panel GameObject
- Check if it has a CanvasGroup component
- Set the Alpha to 1.0
- Ensure "Interactable" and "Blocks Raycasts" are checked

### Issue 3: Canvas Sort Order Too Low
**Symptoms:** Pause menu appears behind other UI elements
**Solution:**
- Select the Canvas containing your pause menu
- Increase the "Sort Order" value (try 100 or higher)

### Issue 4: Multiple UIManager Instances
**Symptoms:** Inconsistent behavior, some features work others don't
**Solution:**
- Search for all UIManager components in your scene
- Remove duplicate instances, keep only one

### Issue 5: Missing EventSystem
**Symptoms:** Buttons don't respond to clicks
**Solution:**
- Right-click in Hierarchy → UI → Event System
- Or check if EventSystem exists and is enabled

### Issue 6: Time Scale Issues
**Symptoms:** Game doesn't pause properly
**Solution:**
- Check if Time.timeScale is being set elsewhere
- Look for other scripts that might override Time.timeScale

## Debug Controls (Added to UIManager)

- **Shift + P**: Force show pause menu (for testing)
- **F1**: Run full diagnostic (with diagnostic script)
- **F2**: Test pause menu toggle
- **F3**: Force show pause menu
- **F4**: Check pause menu hierarchy

## Step-by-Step Fix Process

1. **Run Diagnostics**
   ```
   - Add UIManagerDiagnostic script to scene
   - Press F1 in Play mode
   - Read Console output carefully
   ```

2. **Check Basic Setup**
   ```
   - UIManager exists and is enabled
   - Pause menu panel is assigned in inspector
   - Pause menu panel GameObject exists in scene
   ```

3. **Verify Canvas Setup**
   ```
   - Canvas render mode is "Screen Space - Overlay"
   - Canvas sort order is high enough (100+)
   - Canvas is enabled
   ```

4. **Check Panel Configuration**
   ```
   - Panel GameObject is active in hierarchy
   - No CanvasGroup with Alpha = 0
   - No Layout Group components causing issues
   ```

5. **Test Input**
   ```
   - Press Escape key
   - Check Console for debug messages
   - Try Shift+P to force show menu
   ```

## Common Unity Inspector Issues

1. **"Missing" Reference**: The pause menu panel field shows "Missing" or "None"
   - Re-assign the GameObject reference

2. **Wrong GameObject**: You assigned the wrong GameObject
   - Make sure you're assigning the actual pause menu panel, not a parent or child

3. **Prefab Issues**: If using prefabs, the reference might be broken
   - Try assigning the instance in the scene, not the prefab asset

## If Nothing Works

1. **Create New Pause Menu Panel**
   ```
   - Right-click in Hierarchy → UI → Panel
   - Name it "PauseMenuPanel"
   - Add some buttons for testing
   - Assign it to UIManager
   ```

2. **Reset UIManager**
   ```
   - Remove UIManager component
   - Re-add it
   - Re-assign all references
   ```

3. **Check for Script Errors**
   ```
   - Look for any compilation errors in Console
   - Fix any missing references or null exceptions
   ```

## Verification

After applying fixes:
1. Press Escape - pause menu should appear
2. Time should freeze (Time.timeScale = 0)
3. Cursor should become visible and unlocked
4. Buttons should be clickable
5. Press Escape again - menu should disappear and game resume

## Debug Output

The updated UIManager now includes extensive debug logging. Check the Console for messages like:
- `[UIManager] TogglePauseMenu called`
- `[UIManager] Setting pauseMenuPanel active: true`
- `[UIManager] Game paused - cursor unlocked`

If you don't see these messages when pressing Escape, the input isn't being detected properly.