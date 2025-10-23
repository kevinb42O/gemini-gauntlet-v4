# MaxHandUpgrade Timer UI Fix

## Problem
The MaxHandUpgrade powerup timer was not updating in the UI because it was excluded from the `UpdateActivePowerupTimers()` method. The exclusion was intentional to prevent the standard timer system from removing the powerup, since a separate coroutine (`MaxHandUpgradeReversionCoroutine`) handles the actual hand level reversion logic.

## Solution
Modified the timer update system to handle MaxHandUpgrade specially:

### Changes Made

#### 1. Updated `UpdateActivePowerupTimers()` Method
**File:** `PowerupInventoryManager.cs` (lines 1068-1101)

**Before:**
```csharp
// Update duration-based powerups (EXCLUDE MaxHandUpgrade - it manages itself via coroutine)
if (powerup.isActive && powerup.duration > 0 && powerup.powerupType != PowerUpType.MaxHandUpgrade)
{
    powerup.duration -= Time.unscaledDeltaTime;
    
    if (powerup.duration <= 0)
    {
        DeactivatePowerup(i);
        RemovePowerup(i);
        needsUpdate = true;
    }
    else
    {
        UpdateSlotDisplay(i);
    }
}
```

**After:**
```csharp
// Update duration-based powerups
if (powerup.isActive && powerup.duration > 0)
{
    // STANDARDIZED: Use unscaled time for all powerup timers to prevent SlowTime interference
    powerup.duration -= Time.unscaledDeltaTime;
    
    // MaxHandUpgrade: Update UI timer but don't remove (coroutine handles removal)
    if (powerup.powerupType == PowerUpType.MaxHandUpgrade)
    {
        // Just update the UI display, coroutine handles actual expiration
        UpdateSlotDisplay(i);
    }
    else if (powerup.duration <= 0)
    {
        // CRITICAL FIX: Deactivate powerup and its particle effects when duration expires
        DeactivatePowerup(i);
        RemovePowerup(i);
        needsUpdate = true;
    }
    else
    {
        // INDIVIDUAL: Only update this specific powerup's timer display
        UpdateSlotDisplay(i);
    }
}
```

#### 2. Enhanced `MaxHandUpgradeReversionCoroutine()` Method
**File:** `PowerupInventoryManager.cs` (lines 1696-1704)

Added powerup removal at the end of the coroutine:

```csharp
// Remove powerup from inventory
if (slotIndex >= 0 && slotIndex < activePowerups.Count)
{
    if (activePowerups[slotIndex].powerupType == PowerUpType.MaxHandUpgrade)
    {
        RemovePowerup(slotIndex);
        Debug.Log($"[PowerupInventoryManager] MaxHandUpgrade removed from slot {slotIndex}", this);
    }
}
```

## How It Works

### Timer Update Flow
1. **Every Frame:** `UpdateActivePowerupTimers()` runs in `Update()`
2. **For MaxHandUpgrade:**
   - Duration decrements normally (`powerup.duration -= Time.unscaledDeltaTime`)
   - UI display updates via `UpdateSlotDisplay(i)`
   - **Does NOT remove** the powerup when duration reaches 0
3. **For Other Powerups:**
   - Duration decrements normally
   - UI display updates
   - **Removes** powerup when duration reaches 0

### Expiration Flow
1. **MaxHandUpgradeReversionCoroutine** runs in parallel
2. Uses its own timer with `Time.unscaledDeltaTime`
3. When duration expires:
   - Stops shooting if active
   - Reverts hand levels to stored values
   - Deactivates particle effects
   - **Removes powerup from inventory**
   - Shows end message

## Benefits

✅ **UI Timer Updates:** MaxHandUpgrade now shows countdown in UI like other powerups  
✅ **Proper Separation:** UI timer system handles display, coroutine handles game logic  
✅ **No Race Conditions:** Only the coroutine removes the powerup  
✅ **Consistent Behavior:** All powerup timers use `Time.unscaledDeltaTime`  
✅ **Clean Architecture:** Each system has a single responsibility  

## Technical Details

### Why Two Timers?
- **UI Timer:** Updates every frame for smooth countdown display
- **Coroutine Timer:** Handles complex hand level reversion logic

### Why Not Merge Them?
The coroutine needs to:
1. Stop shooting before reverting
2. Store and restore hand levels
3. Wait frames between operations
4. Resume shooting if buttons still held
5. Deactivate effects properly

This complex logic can't be handled in the simple `UpdateActivePowerupTimers()` loop.

### Synchronization
Both timers use `Time.unscaledDeltaTime` and decrement at the same rate, ensuring they stay synchronized. The UI timer provides visual feedback while the coroutine handles the actual game state changes.

## Testing Checklist

- [ ] MaxHandUpgrade timer counts down in UI
- [ ] Timer shows correct remaining time
- [ ] Powerup removes from inventory when expired
- [ ] Hand levels revert correctly when expired
- [ ] Particle effects deactivate when expired
- [ ] No duplicate removal or errors in console
- [ ] Works correctly with SlowTime powerup active
- [ ] Multiple MaxHandUpgrade powerups stack correctly

## Related Systems

- **PowerupInventoryManager.cs:** Main inventory and timer system
- **MaxHandUpgradePowerUp.cs:** Powerup pickup script
- **PlayerProgression.cs:** Hand level management
- **PowerupEffectManager.cs:** Particle effect management
