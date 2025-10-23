# IsGrounded Flickering Fix - Summary

## Problem Identified

Your `IsGrounded` state was flickering rapidly, causing:
- **Footsteps playing at super-fast speed** (grounded/not-grounded spam)
- **Inconsistent behavior on moving platforms** (suddenly losing grounding without apparent cause)
- **Console spam** with IsGrounded state changes

## Root Causes

1. **No Debouncing**: `groundedHysteresisSeconds` was set to `0`, so every single-frame flicker was immediately reflected
2. **CharacterController.isGrounded Flickering**: The built-in Unity property can flicker on edges, slopes, and moving platforms due to collision detection timing
3. **No Frame Consistency Check**: Single-frame glitches could instantly change the grounded state

## Solution Implemented

### 1. **Added Frame-Based Debouncing**
```csharp
[SerializeField] private int groundedDebounceFrames = 2; // Require 2 consecutive frames
```
- Requires **2 consecutive frames** in the same raw state before changing `IsGrounded`
- Prevents single-frame glitches from affecting gameplay

### 2. **Enabled Hysteresis Timer**
```csharp
[SerializeField] private float groundedHysteresisSeconds = 0.1f; // 100ms grace period
```
- When transitioning from grounded → airborne, waits **0.1 seconds** before confirming
- Smooths out brief detection misses (like on platform edges)

### 3. **State Tracking**
```csharp
private bool _rawGroundedThisFrame = false;
private int _consecutiveGroundedFrames = 0;
private int _consecutiveAirborneFrames = 0;
```
- Tracks raw `controller.isGrounded` separately from debounced `IsGrounded`
- Counts consecutive frames in each state
- Only changes public `IsGrounded` after meeting debounce requirements

### 4. **Debug Logging**
```csharp
[SerializeField] private bool showGroundingDebug = false;
```
- Optional detailed logging to see raw state vs debounced state
- Shows frame counters and hysteresis timer status
- Enable this in the Inspector if you need to troubleshoot further

## How It Works

```
Frame 1: Raw=true,  Grounded=false, GroundedFrames=1 → Stay airborne (need 2 frames)
Frame 2: Raw=true,  Grounded=false, GroundedFrames=2 → ✅ TRANSITION TO GROUNDED
Frame 3: Raw=false, Grounded=true,  AirborneFrames=1 → Stay grounded (need 2 frames)
Frame 4: Raw=false, Grounded=true,  AirborneFrames=2 → Check hysteresis timer
         If < 0.1s since last raw grounded → ⏳ STAY GROUNDED (hysteresis)
         If > 0.1s since last raw grounded → ✈️ TRANSITION TO AIRBORNE
```

## Expected Results

✅ **Stable grounding on flat surfaces** - no more rapid flickering  
✅ **Smooth platform riding** - hysteresis prevents edge detection issues  
✅ **Consistent footsteps** - only play when truly grounded for 2+ frames  
✅ **No console spam** - state changes only when genuinely transitioning  
✅ **Better moving platform support** - brief detection misses are ignored  

## Tuning Parameters

If you still experience issues, adjust these in the Inspector:

- **`groundedDebounceFrames`** (default: 2)
  - Increase to 3-4 for even more stability (at cost of slight input delay)
  - Decrease to 1 for more responsive but potentially flickery behavior

- **`groundedHysteresisSeconds`** (default: 0.1)
  - Increase to 0.15-0.2 for more forgiveness on platform edges
  - Decrease to 0.05 for tighter grounding (may flicker more)

- **`showGroundingDebug`** (default: false)
  - Enable to see detailed frame-by-frame grounding state in console
  - Useful for diagnosing remaining issues

## Testing Checklist

- [ ] Walk on flat ground - no flickering
- [ ] Jump and land - clean transitions
- [ ] Walk on moving platforms - stays grounded
- [ ] Stand on platform edges - no rapid state changes
- [ ] Sprint and slide - footsteps play correctly
- [ ] Check console - no IsGrounded spam

## Technical Notes

- The fix maintains **backward compatibility** - all existing systems continue to work
- `IsGrounded` is now a **debounced state**, while `_rawGroundedThisFrame` is the instant truth
- The hysteresis timer only applies when transitioning **grounded → airborne** (not the reverse)
- Frame-based debouncing works at any framerate (doesn't depend on Time.deltaTime)
