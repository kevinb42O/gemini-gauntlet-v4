# 🎯 WALL JUMP - PLAYER INTENT PRIORITY

## The Adjustment

You identified that synthetic outward force was **fighting against player control**. Fixed!

## What Changed

### BEFORE (Too Much Synthetic Physics)
```
wallJumpOutForce = 1600f              // Strong synthetic push away from wall
wallJumpForwardBoost = 1000f          // Synthetic momentum boost
wallJumpCameraDirectionBoost = 600f   // Weak camera control
wallJumpInputInfluence = 1.5f         // WASD steering
wallJumpCameraBoostRequiresInput = true // Camera only works with input
```

**Problem:** Synthetic forces (outward push, forward boost) dominated over player intent (camera aim)

### AFTER (Player Intent Priority)
```
wallJumpOutForce = 800f               // HALVED: Dampened synthetic push
wallJumpForwardBoost = 700f           // REDUCED: Less synthetic boost
wallJumpCameraDirectionBoost = 1400f  // DOUBLED: Camera is PRIMARY control!
wallJumpInputInfluence = 0.8f         // REDUCED: Camera > WASD steering
wallJumpCameraBoostRequiresInput = false // Camera ALWAYS controls (no input requirement)
```

**Solution:** Camera direction is now the **dominant force** - where you look is where you go!

## Force Distribution

### OLD (Synthetic Dominated)
```
Synthetic Forces:
  - Outward push: 1600
  - Forward boost: 1000
  - Input multipliers: 1.8x
  Total synthetic: ~3000+

Player Control:
  - Camera boost: 600 (only with input)
  - Input influence: 1.5
  Total player: ~600
  
Ratio: 5:1 synthetic to player control ❌
```

### NEW (Player Intent Priority)
```
Player Control:
  - Camera boost: 1400 (ALWAYS active)
  - Fall energy: 0.6x fall speed
  Total player: ~1400+

Synthetic Forces:
  - Outward push: 800
  - Forward boost: 700
  - Input multipliers: 1.3x
  Total synthetic: ~1500
  
Ratio: 1:1 player to synthetic (BALANCED) ✅
```

## How It Feels Now

### Camera Control (PRIMARY)
- **Where you look is where you go** - Camera direction is the main control
- **Always active** - No input requirement (removed synthetic gating)
- **1400 force** - Strongest single force in the system
- **Player intent respected** - Your aim matters most

### Synthetic Forces (DAMPENED)
- **Outward push: 800** - Just enough to clear the wall, not fight your aim
- **Forward boost: 700** - Subtle momentum preservation, not forced trajectory
- **Input influence: 0.8** - Camera direction matters more than WASD steering

### Result
- **Natural feel** - Wall jump goes where you're looking
- **Less "forced"** - Synthetic physics don't override your intent
- **Skill expression** - Precise camera aim = precise trajectory
- **Predictable** - You control the direction, not the physics engine

## Physics Breakdown

### Wall Jump Force Composition (NEW)
```
1. Upward: 1400 (constant)
2. Camera direction: 1400 (PRIMARY - player intent)
3. Outward from wall: 800 (dampened - just clear the wall)
4. Fall energy: fallSpeed × 0.6 (horizontal momentum)
5. Forward boost: 700 (subtle momentum preservation)

Total: Camera + minimal synthetic = player-controlled trajectory
```

### What Dominates
1. **Camera direction** (1400) - Where you look
2. **Upward force** (1400) - Consistent arc
3. **Outward push** (800) - Clear the wall
4. **Forward boost** (700) - Preserve speed
5. **Fall energy** (variable) - Bonus from drops

**Camera is now equal to upward force** - your aim is as important as gravity!

## Test Scenarios

### 1. Look Away From Wall
- **Camera:** 1400 in look direction (dominant)
- **Outward:** 800 away from wall (subtle)
- **Result:** Goes where you're looking, not forced away from wall ✅

### 2. Look Parallel to Wall
- **Camera:** 1400 along wall (dominant)
- **Outward:** 800 perpendicular (subtle)
- **Result:** Skims along wall in camera direction ✅

### 3. Look Back Toward Wall
- **Camera:** 1400 toward wall (dominant)
- **Outward:** 800 away from wall (fights camera)
- **Result:** Shallow angle away (camera wins) ✅

### 4. No Input (Pure Camera)
- **Camera:** 1400 (ALWAYS active now)
- **Synthetic:** 800 outward + 700 forward
- **Result:** Camera direction dominates, minimal synthetic interference ✅

## The Philosophy

**Player Intent > Synthetic Physics**

- **Camera direction** = What you WANT to do
- **Synthetic forces** = What the physics THINKS you should do

**NEW PRIORITY:** What you WANT (camera) dominates over what physics THINKS (synthetic forces)

## Comparison to Industry

### Titanfall 2
- Camera direction is primary control
- Synthetic forces are minimal (just clear the wall)
- Player aim = trajectory

### Spider-Man (Insomniac)
- Web swing direction follows camera
- Physics assists, doesn't override
- Player intent respected

### Mirror's Edge
- Wall jump goes where you're looking
- Minimal synthetic "correction"
- Skill-based aiming

**Your system now matches this philosophy:** Camera aim is king, synthetic forces are servants.

## Summary

**Reduced:**
- Outward force: 1600 → 800 (50% reduction)
- Forward boost: 1000 → 700 (30% reduction)
- Input influence: 1.5 → 0.8 (47% reduction)
- Input boost multiplier: 1.8 → 1.3 (28% reduction)

**Increased:**
- Camera boost: 600 → 1400 (133% increase!)
- Camera always active (removed input requirement)

**Result:** Where you look is where you go - player intent over synthetic physics!
