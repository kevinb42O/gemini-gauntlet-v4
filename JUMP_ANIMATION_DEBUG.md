# 🐛 JUMP ANIMATION DEBUG INSTRUCTIONS

## Changes Made

### 1. Enhanced Debug Logging
Added detailed logging to track one-shot animation protection:
- 🔒 Shows when Jump/Land animations are LOCKED
- Shows exact duration remaining
- Shows when protection expires

### 2. Increased Jump Animation Duration
- **Changed from:** 0.3 seconds
- **Changed to:** 0.6 seconds
- **Reason:** Ensure full animation plays before auto-detection resumes

## How to Test

1. **Open Unity Console** and clear it
2. **Make sure** `PlayerAnimationStateManager` has `enableDebugLogs = true` (should be default)
3. **Stand still** and press **Space** to jump
4. **Watch the Console** for these logs:

### Expected Log Sequence:
```
[BULLETPROOF JUMP] Jump animation triggered IMMEDIATELY!
🔒 [JUMP LOCK] Jump animation LOCKED for 0.6s until [TIME]
[PlayerAnimationStateManager] Movement State: [PreviousState] → Jump (MANUAL)
[ONE-SHOT PROTECTION] Blocking auto-detection - 0.xxx remaining
[ONE-SHOT PROTECTION] Blocking auto-detection - 0.xxx remaining
... (repeats for 0.6 seconds)
[ONE-SHOT PROTECTION] One-shot animation completed, resuming auto-detection
[PlayerAnimationStateManager] Movement State: Jump → [NextState] (AUTO)
```

## What to Look For

### ✅ GOOD Signs:
- You see the 🔒 [JUMP LOCK] message
- You see multiple "Blocking auto-detection" messages
- Jump animation plays for the full duration
- Only transitions AFTER "One-shot animation completed" message

### 🚨 BAD Signs:
- No 🔒 [JUMP LOCK] message (means SetMovementState never called)
- State changes to Sprint/Walk BEFORE "One-shot animation completed"
- "Blocking auto-detection" only shows once or not at all

## Potential Issues

### Issue #1: Jump Not Triggering at All
**Symptom:** No 🔒 [JUMP LOCK] message
**Cause:** `AAAMovementController` not calling `SetMovementState(Jump)`
**Check:** Make sure `_animationStateManager` is assigned in scene

### Issue #2: Jump Immediately Overridden
**Symptom:** 🔒 [JUMP LOCK] shows but state changes immediately
**Cause:** Auto-detection running before protection kicks in
**Solution:** This should be fixed now, but if it persists check Update() order

### Issue #3: Sprint Overriding Jump
**Symptom:** State changes from Jump → Sprint while in air
**Cause:** `energySystem.IsCurrentlySprinting` returning true while airborne
**Check:** Logs should show "Sprint check: sprinting=X, grounded=false"

## Next Steps Based on Results

### If logs show proper protection but animation still doesn't play:
- The problem is in `IndividualLayeredHandController` or Unity Animator
- Check if Jump animation clip is assigned
- Check if movementState parameter is being set correctly
- Verify Jump transition exists in Animator

### If logs show protection being bypassed:
- There's another system calling SetMovementState
- Check for duplicate controllers (old HandAnimationController)
- Verify only PlayerAnimationStateManager is controlling states

### If no logs appear at all:
- `enableDebugLogs` is off in Inspector
- PlayerAnimationStateManager not in scene
- Console filter is hiding logs

## Test Matrix

Test these scenarios and report results:

- [ ] **Idle Jump:** Stand still, press Space
- [ ] **Walking Jump:** Move forward, press Space
- [ ] **Sprint Jump:** Sprint forward, press Space (most likely to fail)
- [ ] **Bunny Hop:** Jump, jump, jump repeatedly

For each, note:
1. Does animation play?
2. What logs appear?
3. Does state change immediately or after 0.6s?
