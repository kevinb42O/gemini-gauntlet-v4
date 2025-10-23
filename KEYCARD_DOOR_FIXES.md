# Keycard Door System - Bug Fixes

## Issues Fixed

### Issue 1: Keycard not detected from persistent inventory
**Problem:** When starting the game with a keycard already in inventory (from persistent save), the first interaction attempt fails. After picking up another keycard from a chest, the door works.
**Problem:** When a door uses the `Scale` open type and has `autoClose` enabled, the door disappears completely after closing instead of returning to its original state.

**Root Cause:** 
- The door's original scale was not being stored (only position and rotation)
- When opening with `Scale` type, the door scales to `Vector3.zero`
- When closing, it tried to restore to `Vector3.one` instead of the original scale
- If the door had a different scale (e.g., 0.5, 2.0, etc.), it would be lost

**Solution:**
- Added `closedScale` private field to store the original `transform.localScale` in `Start()`
- Modified `OpenDoor()` coroutine to lerp from `closedScale` to `Vector3.zero` (line 434)
- Modified `CloseDoor()` coroutine to lerp from current scale back to `closedScale` (line 503)
- Updated final scale restoration to use `closedScale` instead of `Vector3.one` (line 516)

**Files Modified:** `Assets/scripts/KeycardDoor.cs`

---

### 3. **No Auto-Reopen After Unlocking**
**Problem:** After using the Building21 keycard (infinite use) to unlock a door, the door doesn't automatically reopen when the player re-enters the trigger zone.

**Root Cause:** The door system didn't track whether it had been permanently unlocked with an infinite-use keycard.

**Solution:**
- Added `isUnlocked` private bool field to track permanent unlock state
- Set `isUnlocked = true` when Building21 keycard is used (line 329)
- Modified `HandlePlayerInteraction()` to auto-open the door when player is in range and door is unlocked (lines 278-283)
- Updated `OnTriggerEnter()` to not show interaction UI for unlocked doors (line 651)

**Files Modified:** `Assets/scripts/KeycardDoor.cs`

---

## Testing Checklist

### Test 1: First Interaction Message
1. Start a new game with a keycard already in inventory (from persistent save)
2. Approach a door requiring that keycard
3. Press E to interact
4. **Expected:** Message "Requires [keycard name] to open!" should display immediately
5. Check console logs for debug messages showing keycard detection

### Test 2: Door Scale Restoration
1. Create a door with `Scale` open type
2. Set the door's scale to something other than (1,1,1), e.g., (0.5, 0.5, 0.5)
3. Enable `autoClose` with a short delay (e.g., 3 seconds)
4. Use the keycard to open the door
5. Wait for the door to auto-close
6. **Expected:** Door should return to its original scale (0.5, 0.5, 0.5), not disappear or change size

### Test 3: Auto-Reopen After Unlock
1. Use a Building21 keycard (infinite use) to open a door
2. Walk through and let the door auto-close
3. Walk back into the door's trigger zone
4. **Expected:** Door should automatically open without pressing E
5. **Expected:** No interaction UI should appear for unlocked doors

---

## Debug Logging

The following debug logs have been added to help diagnose issues:

- `[KeycardDoor] InventoryManager.Instance not found yet (attempt X/10), retrying...`
- `[KeycardDoor] InventoryManager.Instance found successfully on attempt X`
- `[KeycardDoor] HasRequiredKeycard check for [keycard]: [true/false]`
- `[KeycardDoor] TryOpenDoor called for door requiring [keycard]`
- `[KeycardDoor] Showing keycard required message: [message]`

These logs will help identify timing issues with inventory loading and keycard detection.

---

## Technical Details

### Initialization Order
The door now uses a coroutine-based initialization for the InventoryManager to handle cases where:
- Persistent inventory is still loading
- Scene objects are initializing in different orders
- InventoryManager singleton is created after the door

### Scale Animation Fix
```csharp
// Before (BROKEN):
transform.localScale = Vector3.Lerp(Vector3.one, targetScale, t);  // Assumes scale is always 1
transform.localScale = Vector3.one;  // Forces scale to 1 on close

// After (FIXED):
transform.localScale = Vector3.Lerp(closedScale, targetScale, t);  // Uses original scale
transform.localScale = closedScale;  // Restores original scale
```

### Unlock State Tracking
```csharp
private bool isUnlocked = false;  // New field

// Set when ANY keycard is used (both infinite-use and one-time):
isUnlocked = true;
// Door will now auto-open when player enters trigger and stay open permanently
```

---

## Keycard System Behavior

### Building21 Keycard (Infinite Use)
- **NOT consumed** from inventory when used
- Can be used multiple times on different doors
- Identified by checking if `requiredKeycard.itemName` contains "Building21", "Building 21", or "building21"
- Door becomes permanently unlocked and auto-opens when player enters trigger

### All Other Keycards (One-Time Use)
- **Consumed from inventory** when used - removes exactly **1 item from the stack**
- If you have multiple keycards of the same type stacked, only 1 is removed
- Door becomes permanently unlocked after first use
- Door stays open and auto-opens when player enters trigger
- You need another keycard to open another door of the same color

---

## Notes

- `TryRemoveItem(requiredKeycard, 1)` ensures only 1 keycard is removed from a stack, not the entire stack
- Unlocked doors (both infinite-use and one-time) will auto-open whenever the player enters the trigger zone
- Unlocked doors will NOT auto-close (autoClose is disabled for unlocked doors)
- The door properly handles all open types: SlideUp, SlideDown, SlideLeft, SlideRight, SlideForward, SlideBackward, RotateLeft, RotateRight, and Scale
