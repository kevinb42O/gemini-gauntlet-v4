# HandAnimationController - Quick Reference

## Core Concepts

### 1. Lock Mechanism
**Purpose:** Prevent interruptions during critical animations

```csharp
// Automatically locked by:
- Emotes (both hands)
- Armor Plate (right hand only)

// Automatically unlocked when:
- Animation completes
- Emote finishes
```

### 2. Priority Hierarchy
```
Highest → Emotes & ArmorPlate (can interrupt anything)
       → Beam (can interrupt movement)
       → Shotgun (can interrupt movement, not beam)
Lowest  → Movement: Walk/Sprint/Idle
```

### 3. State Flow

#### Movement States (Automatic)
```
Player Input → UpdateMovementAnimations() → Idle/Walk/Sprint
```
- Automatically transitions based on WASD + Shift
- Respects locks and high-priority states
- No manual intervention needed

#### One-Shot States (Manual Trigger)
```
External Call → Play Animation → Complete → Unlock → Movement Takes Over
```
- Jump, Shotgun, Dive, Slide, etc.
- Play fully without interruption
- Natural return to movement state

#### Continuous States (Manual Start/Stop)
```
StartBeam() → Loop Until → StopBeam() → Idle
```
- Beam animations
- Must be explicitly stopped
- Can be interrupted by emotes

## Common Usage Patterns

### Playing Animations

#### Movement (Automatic)
```csharp
// No code needed - handled automatically by UpdateMovementAnimations()
// Just move the player with WASD + Shift
```

#### Jump
```csharp
handAnimationController.PlayJumpBoth();
// Plays jump animation, then naturally returns to movement
```

#### Shotgun
```csharp
handAnimationController.PlayShootShotgun(isPrimaryHand: true);  // Right hand
handAnimationController.PlayShootShotgun(isPrimaryHand: false); // Left hand
// Plays shotgun animation, protected from interruption
```

#### Beam
```csharp
// Start
handAnimationController.StartBeamLeft();
handAnimationController.StartBeamRight();

// Stop
handAnimationController.StopBeamLeft();
handAnimationController.StopBeamRight();
```

#### Emotes
```csharp
handAnimationController.PlayEmote(1); // Keys 1-4
// Locks both hands, plays fully, unlocks automatically
```

#### Armor Plate
```csharp
handAnimationController.PlayApplyPlateAnimation();
// Locks right hand, plays fully, unlocks automatically
```

### Integration Points

#### From Shooting System
```csharp
// When beam starts
handAnimationController.OnBeamStarted(isPrimaryHand);

// When beam stops
handAnimationController.OnBeamStopped(isPrimaryHand);

// When shotgun fires
handAnimationController.OnShotgunFired(isPrimaryHand);
```

#### From Movement System
```csharp
// Jump
handAnimationController.OnPlayerJumped();

// Land
handAnimationController.OnPlayerLanded();

// Slide
handAnimationController.OnSlideStarted();
handAnimationController.OnSlideStopped();

// Dive
handAnimationController.PlayDiveAnimation();
handAnimationController.StopDiveAnimation();
```

## Debugging

### Enable Debug Logs
```csharp
// In Inspector
enableDebugLogs = true;

// Logs will show:
// - State transitions: "LEFT: Walk → Sprint"
// - Lock rejections: "Left LOCKED - rejecting Idle"
// - Animation triggers: "L Beam started"
```

### Common Issues

#### Animation Won't Play
**Check:**
1. Is hand locked? (Emote/armor plate playing?)
2. Is higher priority animation active? (Beam blocks shotgun)
3. Is animation clip assigned in Inspector?

#### Animation Interrupted
**Check:**
1. Is lock mechanism working? (Should auto-lock for emotes/plates)
2. Is priority correct? (Movement shouldn't interrupt beam)
3. Are you calling StopBeam too early?

#### Stuck in State
**Check:**
1. Did you forget to call StopBeam?
2. Is completion coroutine running? (Check enableDebugLogs)
3. Is hand still locked? (Should auto-unlock)

## Best Practices

### DO ✅
- Let movement animations happen automatically
- Use locks for critical animations (already built-in)
- Call StopBeam when beam actually stops
- Trust the priority system

### DON'T ❌
- Manually force idle transitions
- Interrupt locked animations
- Call PlayIdle after every animation
- Fight the state machine

## State Machine Internals

### HandState Structure
```csharp
currentState              // Current animation state
beamWasActiveBeforeInterruption  // For beam resume after emote
animationCompletionCoroutine     // Tracks one-shot completion
stateStartTime           // When state started
isEmotePlaying           // Emote flag
isLocked                 // Protection flag
```

### Transition Logic
```csharp
RequestStateTransition()
  ↓
Check if locked → Reject if locked
  ↓
Check priority → Allow if higher/equal priority
  ↓
TransitionToState()
  ↓
Play animation + Schedule completion if one-shot
```

## Performance Notes

- **Lock checks:** Simple boolean, negligible cost
- **Priority checks:** Explicit comparisons, very fast
- **No reflection:** All validation removed
- **Minimal coroutines:** One per hand maximum

## Migration from Old System

### Removed Settings
- ~~idleFallbackDelay~~ - No longer needed
- ~~movementSpeedThreshold~~ - No longer needed
- ~~sprintSpeedThreshold~~ - No longer needed

### Kept Settings
- `crossFadeDuration` - Smooth transitions (default: 0.2s)
- `animationSpeed` - Animation playback speed (default: 1.0)
- `enableDebugLogs` - Debug output (default: false)

### Behavior Changes
- **Idle fallbacks removed** - Animations play fully
- **Validation removed** - No auto-correction
- **Priority enforced** - Clear hierarchy respected
- **Locks added** - Critical animations protected

## Quick Troubleshooting

| Problem | Solution |
|---------|----------|
| Animations revert to idle | Fixed - idle fallbacks removed |
| Beam interrupted by movement | Fixed - priority system prevents this |
| Shotgun animation cut short | Fixed - one-shot protection added |
| Emote interrupted | Fixed - lock mechanism added |
| Walk/Sprint not transitioning | Check if hands are locked or in high-priority state |
| Animation not starting | Check clip assignment in Inspector |

## Summary

The new HandAnimationController is **simple, robust, and predictable**:
1. Movement happens automatically
2. One-shots play fully and unlock
3. Beams loop until stopped
4. Emotes lock and play fully
5. Priority system prevents unwanted interruptions

**Just call the animation methods and let the system handle the rest!**
