# Keycard Door System - Bug Fixes (Updated)

## Issues Fixed

### Issue 1: Keycard Not Detected from Persistent Inventory
**Problem:** When starting the game with a keycard already in inventory (from persistent save), the first interaction attempt fails. After picking up another keycard from a chest, the door works.

**Root Cause:** 
1. `InventoryManager.Instance` may not be fully initialized when door's `Start()` runs
2. **More importantly:** Keycards loaded from persistent storage via `Resources.Load()` create different ScriptableObject instances than the door's inspector reference
3. The old code relied on reference equality instead of `IsSameItem()` comparison

**Solution:**
- Added `InitializeInventoryManager()` coroutine that retries finding InventoryManager up to 10 times with 0.1s delays
- **ENHANCED `HasRequiredKeycard()`:** Now manually iterates all slots and uses `IsSameItem()` to match keycards by ID/name instead of reference equality
- **ENHANCED keycard removal:** Manually finds matching slot using `IsSameItem()` and removes exactly 1 from stack
- Added comprehensive debug logging with ✅/❌ emojis to track keycard detection

**Files Modified:** `Assets/scripts/KeycardDoor.cs`

---

### Issue 2: Door Disappears After Auto-Close
**Problem:** When a door uses the `Scale` open type and has `autoClose` enabled, the door disappears completely after closing instead of returning to its original state.

**Root Cause:** 
- The door's original scale was not being stored (only position and rotation)
- When opening with `Scale` type, the door scales to `Vector3.zero`
- When closing, it tried to restore to `Vector3.one` instead of the original scale
- If the door had a different scale (e.g., 0.5, 2.0, etc.), it would be lost

**Solution:**
- Added `closedScale` private field to store the original `transform.localScale` in `Start()`
- Modified `OpenDoor()` coroutine to lerp from `closedScale` to `Vector3.zero`
- Modified `CloseDoor()` coroutine to lerp from current scale back to `closedScale`
- Updated final scale restoration to use `closedScale` instead of `Vector3.one`

**Files Modified:** `Assets/scripts/KeycardDoor.cs`

---

### Issue 3: Door Not Auto-Closing After Unlock
**Problem:** After unlocking a door with a keycard, the door never auto-closes, even when the player leaves the area.

**Root Cause:** The code was preventing auto-close for unlocked doors (`if (autoClose && !isUnlocked)`), which meant once unlocked, the door would stay open forever.

**Solution:**
- Removed the `!isUnlocked` check from auto-close logic
- Unlocked doors now auto-close normally when player leaves
- Unlocked doors auto-reopen when player re-enters the trigger zone (via `HandlePlayerInteraction()`)
- This creates a smooth experience: door closes behind you, opens automatically when you return

**Files Modified:** `Assets/scripts/KeycardDoor.cs`

---

## Keycard System Behavior

### Building21 Keycard (Infinite Use)
- **NOT consumed** from inventory when used
- Can be used multiple times on different doors
- Identified by checking if `requiredKeycard.itemName` contains "Building21", "Building 21", or "building21"
- Door becomes permanently unlocked and auto-opens when player enters trigger
- Door still auto-closes when player leaves (if `autoClose` is enabled)

### All Other Keycards (One-Time Use)
- **Consumed from inventory** when used - removes exactly **1 item from the stack**
- If you have multiple keycards of the same type stacked, only 1 is removed
- Door becomes permanently unlocked after first use
- Door auto-opens when player enters trigger
- Door still auto-closes when player leaves (if `autoClose` is enabled)
- You need another keycard to open another door of the same color

---

## Testing Checklist

### Test 1: Persistent Inventory Keycard Detection
1. Start a new game with a keycard already in inventory (from persistent save)
2. Approach a door requiring that keycard
3. Press E to interact
4. **Expected:** Door should open immediately, keycard should be consumed (or retained for Building21)
5. Check console logs for "✅ Found matching keycard" message

### Test 2: Stack Removal (Only 1 Keycard Consumed)
1. Get multiple keycards of the same type (e.g., 3x Red Keycard)
2. Use one on a door
3. **Expected:** Only 1 keycard is removed, you should have 2 remaining
4. Check inventory to verify count decreased by exactly 1

### Test 3: Door Auto-Close and Auto-Reopen
1. Use a keycard to unlock a door (enable `autoClose` with 3-5 second delay)
2. Walk through the door
3. **Expected:** Door should close behind you after the delay
4. Walk back into the door's trigger zone
5. **Expected:** Door should automatically reopen without pressing E
6. **Expected:** No interaction UI should appear for unlocked doors

### Test 4: Door Scale Restoration
1. Create a door with `Scale` open type
2. Set the door's scale to something other than (1,1,1), e.g., (0.5, 0.5, 0.5)
3. Enable `autoClose` with a short delay
4. Use the keycard to open the door
5. Wait for the door to auto-close
6. **Expected:** Door should return to its original scale (0.5, 0.5, 0.5), not disappear

---

## Debug Logging

The following debug logs help diagnose issues:

- `[KeycardDoor] ✅ Found matching keycard: [name] (ID: [id]) matches required [name] (ID: [id])`
- `[KeycardDoor] ❌ No matching keycard found for [name] (ID: [id])`
- `[KeycardDoor] Consumed 1x [keycard] to unlock door permanently (one-time use)`
- `[KeycardDoor] Used [keycard] to open door (keycard retained - infinite use)`
- `[KeycardDoor] InventoryManager.Instance found successfully on attempt X`

---

## Technical Details

### Enhanced Keycard Detection
```csharp
// OLD (BROKEN): Used reference equality
var slot = inventoryManager.GetSlotWithItem(requiredKeycard);
return slot != null && !slot.IsEmpty;

// NEW (FIXED): Uses IsSameItem() for proper matching
var allSlots = inventoryManager.GetAllInventorySlots();
foreach (var slot in allSlots)
{
    if (!slot.IsEmpty && slot.CurrentItem != null)
    {
        if (slot.CurrentItem.IsSameItem(requiredKeycard))
        {
            return true;  // Match found by ID/name, not reference
        }
    }
}
```

### Enhanced Keycard Removal
```csharp
// Manually find matching slot using IsSameItem()
var allSlots = inventoryManager.GetAllInventorySlots();
UnifiedSlot matchingSlot = null;

foreach (var slot in allSlots)
{
    if (!slot.IsEmpty && slot.CurrentItem != null && slot.CurrentItem.IsSameItem(requiredKeycard))
    {
        matchingSlot = slot;
        break;
    }
}

// Remove exactly 1 from stack
if (matchingSlot.ItemCount > 1)
{
    matchingSlot.SetItem(matchingSlot.CurrentItem, matchingSlot.ItemCount - 1);
}
else
{
    matchingSlot.ClearSlot();
}
```

### Auto-Close Behavior
```csharp
// Unlocked doors now auto-close normally
if (autoClose)  // No longer checks !isUnlocked
{
    StartCoroutine(AutoCloseDoor());
}

// Auto-reopen when player enters
if (isUnlocked && playerInRange && !isOpen && !isOpening && !isClosing)
{
    StartCoroutine(OpenDoor());
}
```

---

## Notes

- `IsSameItem()` compares items by `itemID` first, then falls back to `itemName` + `itemType`
- This handles keycards loaded from different sources (inspector vs Resources.Load)
- Unlocked doors will auto-open whenever the player enters the trigger zone
- Unlocked doors will auto-close when player leaves (if `autoClose` is enabled)
- The door properly handles all open types: SlideUp, SlideDown, SlideLeft, SlideRight, SlideForward, SlideBackward, RotateLeft, RotateRight, and Scale
