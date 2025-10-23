# 🎯 WALL JUMP + SPRINT COHERENCE FIX

**Date:** 2025-01-13  
**Issue:** Sprint input during wall jumps negatively affects trajectory  
**Status:** ✅ FIXED

---

## 🔥 THE PROBLEM

When holding sprint (Shift) during wall jumps, the trajectory was **significantly worse** than releasing all inputs. This created a counterintuitive situation where:

- **Holding sprint + direction** = Poor, degraded wall jump trajectory
- **Releasing all inputs** = Clean, optimal wall jump trajectory

This violated the principle of "more input = more control" and felt broken.

---

## 🔍 ROOT CAUSE ANALYSIS

### The Conflict Chain

1. **Sprint Input Detection (Line 1578)**
   - Sprint key held + movement input detected
   - `currentMoveSpeed *= sprintMultiplier` (1.65x speed boost)

2. **Target Velocity Calculation (Line 1588)**
   - `targetHorizontalVelocity = moveDirection * currentMoveSpeed`
   - This creates a HIGH-SPEED target velocity in the camera direction

3. **Air Control Application (Lines 1639-1667)**
   - Air control tries to steer toward `targetHorizontalVelocity`
   - This creates a steering force that **fights against wall jump momentum**

4. **Wall Jump Momentum Boost (Lines 2996-3016)**
   - Wall jump system detects input and applies `wallJumpInputBoostMultiplier`
   - But the sprint-inflated target velocity is pulling in the wrong direction

### Why It Felt Bad

The sprint input created a **tug-of-war** between:
- **Wall jump trajectory:** Pushing you away from wall at optimal angle
- **Sprint air control:** Pulling you toward camera direction at 1.65x speed

Result: Degraded trajectory, reduced distance, inconsistent feel.

---

## ✅ THE FIX

### Implementation (AAAMovementController.cs, Lines 1565-1578)

```csharp
// CRITICAL FIX: Disable sprint effect on air control during wall jump trajectory preservation
// Sprint input while airborne creates targetHorizontalVelocity that fights wall jump momentum
// Only allow sprint to affect movement when GROUNDED or when wall jump trajectory is fully released
bool allowSprintInAir = IsGrounded || (Time.time > lastWallJumpTime + 0.5f);

// Debug: Log when sprint is blocked during wall jump
if (showWallJumpDebug && Input.GetKey(Controls.Boost) && !allowSprintInAir && !IsGrounded)
{
    float timeSinceWallJump = Time.time - lastWallJumpTime;
    Debug.Log($"[WALL JUMP] Sprint blocked during trajectory preservation ({timeSinceWallJump:F2}s since wall jump)");
}

// Can't sprint while crouching OR bleeding out OR during wall jump trajectory preservation
if (Input.GetKey(Controls.Boost) && hasMovementInput && !isCrouching && !isBleedingOut && allowSprintInAir)
{
    // Sprint logic...
}
```

### How It Works

1. **Trajectory Preservation Window:** 0.5 seconds after wall jump
2. **Sprint Blocked:** During this window, sprint input does NOT affect `currentMoveSpeed`
3. **Air Control Uses Base Speed:** `targetHorizontalVelocity` calculated with normal move speed
4. **Wall Jump Momentum Preserved:** No conflicting high-speed steering forces

### Timing Coordination

- **Wall Jump Velocity Protection:** `wallJumpAirControlLockoutTime` (default 0.0s)
- **Air Control Restoration:** Gradual over 0.25s (lines 1646-1661)
- **Sprint Block Duration:** 0.5s (matches existing trajectory preservation)

All three systems now work in harmony to preserve wall jump feel.

---

## 🎮 PLAYER EXPERIENCE

### Before Fix
- ❌ Holding sprint during wall jump = degraded trajectory
- ❌ Counterintuitive: "Less input = better result"
- ❌ Inconsistent wall jump distances
- ❌ Fighting against your own inputs

### After Fix
- ✅ Sprint input ignored during trajectory preservation (0.5s)
- ✅ Clean, consistent wall jump trajectories
- ✅ Directional input still works for steering (without sprint multiplier)
- ✅ Sprint resumes automatically after trajectory window
- ✅ Intuitive: "Wall jump first, sprint after"

---

## 🔧 TECHNICAL DETAILS

### Sprint Blocking Logic

**Condition:** `allowSprintInAir = IsGrounded || (Time.time > lastWallJumpTime + 0.5f)`

**Blocks sprint when:**
- Airborne AND
- Within 0.5 seconds of last wall jump

**Allows sprint when:**
- Grounded (normal sprint)
- Airborne but >0.5s since wall jump (trajectory fully released)

### Energy System Integration

Sprint blocking also prevents energy consumption during the window:
- No `energySystem.ConsumeSprint()` calls
- Energy preserved for post-wall-jump sprint
- Clean energy management

### Debug Logging

Enable `showWallJumpDebug` in Inspector to see:
```
[WALL JUMP] Sprint blocked during trajectory preservation (0.23s since wall jump)
```

---

## 🎯 RELATED SYSTEMS

### Air Control System (Lines 2622-2672)
- Receives `targetHorizontalVelocity` without sprint multiplier during window
- Applies normal air control strength
- Preserves wall jump momentum

### Wall Jump Momentum Boost (Lines 2996-3016)
- Still detects input direction
- Applies `wallJumpInputBoostMultiplier` correctly
- No longer fighting against sprint-inflated target velocity

### Energy System (PlayerEnergySystem.cs)
- Sprint detection unaffected
- Energy consumption paused during trajectory window
- Resumes normally after 0.5s

---

## 📊 TESTING CHECKLIST

- [x] Wall jump with sprint held = clean trajectory
- [x] Wall jump with no input = clean trajectory
- [x] Wall jump with directional input only = steerable trajectory
- [x] Sprint resumes after 0.5s in air
- [x] Sprint works normally on ground
- [x] Energy consumption paused during window
- [x] Debug logging shows sprint blocking
- [x] Multiple consecutive wall jumps work correctly

---

## 🎓 DESIGN PHILOSOPHY

**Core Principle:** Wall jump trajectory should be **predictable and consistent**, not degraded by unrelated input (sprint).

**Input Hierarchy During Wall Jump:**
1. **Wall jump direction** (primary, from wall normal)
2. **Player steering** (secondary, from directional input)
3. **Sprint** (disabled during trajectory preservation)

This creates a **skill-based movement system** where:
- Wall jump timing and positioning matter most
- Directional input provides subtle steering
- Sprint doesn't interfere with core mechanics

---

## 🚀 FUTURE ENHANCEMENTS

Potential improvements to consider:

1. **Visual Feedback:** UI indicator when sprint is blocked during wall jump
2. **Sound Cue:** Subtle audio feedback for trajectory preservation window
3. **Tutorial:** Teach players that sprint resumes after wall jump
4. **Advanced Option:** Allow sprint during wall jump for experienced players (Inspector toggle)

---

## 📝 CHANGELOG

**v1.0 - 2025-01-13**
- Initial fix implemented
- Sprint blocked during 0.5s trajectory preservation window
- Debug logging added
- Documentation created

---

## 🔗 RELATED FILES

- `AAAMovementController.cs` - Main movement controller (lines 1565-1578)
- `PlayerEnergySystem.cs` - Sprint energy management
- `AAACameraController.cs` - Wall jump camera tilt
- `MovementConfig.cs` - Wall jump configuration values

---

## 💡 KEY INSIGHT

**The fix teaches us:** Sometimes the best solution is to **temporarily disable conflicting systems** rather than trying to make them work together. Sprint and wall jump trajectory have fundamentally different goals - sprint wants speed in camera direction, wall jump wants optimal trajectory away from wall. By giving wall jump priority during its critical window, we preserve both systems' integrity.

**Result:** Clean, predictable wall jumps with sprint that "just works" without player confusion.
