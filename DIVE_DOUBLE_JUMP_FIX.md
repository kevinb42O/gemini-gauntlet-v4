# DIVE DOUBLE JUMP CONFLICT FIX

## Problem
When player pressed Space for a double jump while in mid-air during a tactical dive, the system triggered the Dive animation instead of the Jump animation!

## Root Cause
**CleanAAACrouch.UpdateDive()** only checked for landing but **didn't check for jump input**. When player pressed Space during a dive:

1. `AAAMovementController` detected double jump and correctly called:
   ```csharp
   _animationStateManager.SetMovementState((int)PlayerAnimationStateManager.PlayerAnimationState.Jump);
   ```

2. BUT `isDiving` remained `true` because `UpdateDive()` never cleared it

3. `PlayerAnimationStateManager.DetermineMovementState()` auto-detection saw:
   ```csharp
   if (crouchController.IsDiving) // TRUE!
       return PlayerAnimationState.Dive;
   ```

4. Auto-detection immediately **overrode** the Jump animation back to Dive!

## Solution Applied
Added jump input detection in `UpdateDive()` to allow canceling the dive with Space (double jump):

```csharp
// CRITICAL FIX: Allow canceling dive with jump input (for double jump)
if (Input.GetKeyDown(Controls.UpThrustJump))
{
    Debug.Log("[DIVE DEBUG] Jump pressed during dive - CANCELING DIVE!");
    isDiving = false; // Clear dive state immediately
    
    // Disable dive override to restore movement input
    if (movement != null)
        movement.DisableDiveOverride();
    
    // Stop dive particles
    StopDiveParticles();
    
    // CRITICAL: Tell animation system to clear dive state so Sprint can resume!
    // This clears the diveManuallyTriggered flag that blocks auto-detection
    if (animationStateManager != null)
    {
        animationStateManager.SetMovementState(PlayerAnimationState.Idle);
        Debug.Log("[DIVE DEBUG] Animation state cleared - Sprint can now resume!");
    }
    
    // Let the jump happen naturally through AAAMovementController
    return;
}
```

**Sprint Frequency Fix:** The animation state manager must be notified to clear the `diveManuallyTriggered` flag, otherwise auto-detection remains blocked and Sprint won't work after canceling a dive!

## Result
✅ **Double jump now works correctly during a dive**
✅ **Jump animation plays properly** (not overridden by Dive)
✅ **Dive state cleared instantly** when Space is pressed
✅ **Dive particles stopped** to avoid visual conflicts
✅ **Movement control restored** so jump can execute

## Key Mechanics
- **Dive key**: X (dedicated tactical dive)
- **Jump key**: Space (regular jump + double jump)
- **Dive can now be canceled** by pressing Space during airborne dive
- **Jump animation priority** is respected when dive state is cleared

## Testing
1. Sprint and press X to initiate tactical dive
2. While in mid-air during dive, press Space
3. **Expected**: Jump animation plays, dive is canceled
4. **Previous bug**: Dive animation continued, jump didn't work
