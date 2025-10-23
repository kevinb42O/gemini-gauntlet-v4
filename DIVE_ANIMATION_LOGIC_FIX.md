# DIVE ANIMATION LOGIC FIX - CRITICAL BUG

## Problem
1. **Pressing X (dive key)**: Dive animation DOESN'T play
2. **Double jumping**: Dive animation DOES play (WRONG!)
3. Sprint frequency broken after any dive interaction

## Root Cause: Backwards Logic

The animation state manager had **completely backwards logic** for handling dive/slide states:

```csharp
// BROKEN CODE (line 253)
if (crouchController.IsDiving && !diveManuallyTriggered)
    return PlayerAnimationState.Dive;
```

**Translation:** "Only auto-detect Dive if it was NOT manually triggered"

**What Actually Happened:**

### When Pressing X (Dive Key):
1. CleanAAACrouch: `StartTacticalDive()` → sets `isDiving = true`
2. CleanAAACrouch: Calls `animationStateManager.SetMovementState(Dive)`
3. PlayerAnimationStateManager: Sets `diveManuallyTriggered = true`
4. **NEXT FRAME** auto-detection runs:
   - Checks: `IsDiving && !diveManuallyTriggered`
   - Result: `true && false` = **FALSE**
   - **SKIPS DIVE** and returns Sprint instead!
5. Dive animation gets immediately overridden by Sprint!

### When Double Jumping:
1. Player jumps during dive
2. Dive cancel code: Sets `isDiving = false`
3. Dive cancel code: Calls `SetMovementState(Idle)`
4. This clears `diveManuallyTriggered = false`
5. But there's timing confusion and Jump gets overridden

## Solution: Remove Backwards Logic

### Change 1: Simplified Auto-Detection
```csharp
// NEW CODE - Simple and correct
if (crouchController.IsDiving)
    return PlayerAnimationState.Dive;

if (crouchController.IsSliding)
    return PlayerAnimationState.Slide;
```

**NO MORE FLAGS!** Just check the actual state. If diving, play dive. Simple.

### Change 2: Removed Manual Trigger Flags
Deleted these entirely:
- `diveManuallyTriggered`
- `slideManuallyTriggered`

These were causing the backwards logic and confusion.

### Change 3: Removed Unnecessary SetMovementState Calls
When canceling dive or exiting dive prone:
- **OLD**: Manually call `SetMovementState(Idle)` to clear flags
- **NEW**: Just set `isDiving = false` and let auto-detection handle it

Auto-detection will naturally transition to:
- Sprint (if still holding Shift + moving fast)
- Walk (if moving)
- Idle (if no input)

## How It Works Now

### Dive Flow (X Key):
1. Press X while sprinting
2. `StartTacticalDive()` → `isDiving = true`
3. Calls `SetMovementState(Dive)` → triggers dive animation ONCE
4. Every frame: Auto-detection sees `IsDiving = true` → **maintains Dive state**
5. Until landing → `isDiving = false` → auto-detection switches to appropriate state

### Double Jump During Dive:
1. Press Space during dive
2. `UpdateDive()` detects jump input → `isDiving = false`
3. Disables dive override
4. **Auto-detection** sees not diving → checks for Sprint/Walk/Idle
5. AAAMovementController triggers Jump animation
6. Jump plays correctly without dive interfering!

### After Landing:
1. Dive ends → `isDiving = false`, `isDiveProne = false`
2. Auto-detection checks:
   - Not diving ✓
   - Not sliding ✓
   - Is sprinting? → Yes → **Sprint state**
3. Sprint animation plays with proper offset (no rocking horse!)

## Key Principles

**One-Shot Animations (Jump, Land):**
- Triggered manually with timer lock
- Auto-detection blocked during lock period
- Lock expires or unlocks when grounded

**Continuous State Animations (Dive, Slide, Sprint):**
- Maintained by auto-detection checking state flags
- No manual triggers needed
- Natural transitions based on actual state

**State Priority:**
```
Dive (highest)
  ↓
Slide
  ↓
Sprint
  ↓
Walk
  ↓
Idle (lowest)
```

## Result

✅ **X key triggers dive** animation and maintains it until landing
✅ **Double jump works** - dive cancels cleanly, jump plays
✅ **Sprint frequency maintained** - offset re-applied correctly
✅ **Auto-detection handles all transitions** - no manual state management
✅ **Simple, predictable logic** - if diving, play dive. Done.

## Testing Checklist

1. Sprint → Press X → **Dive animation plays** ✅
2. Dive → Press Space → **Jump animation plays** ✅  
3. Land from dive → **Sprint resumes with de-synced arms** ✅
4. Double jump while NOT diving → **Jump animation plays** ✅
5. Sprint → Jump → Land → **Sprint continues normally** ✅
