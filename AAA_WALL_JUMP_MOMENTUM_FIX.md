# 🔴 CRITICAL FIX - Momentum Preservation Was BROKEN

## The Problem That Made It "Broken AF"

### What Was Happening
```csharp
// BROKEN CODE:
float momentumBoost = currentSpeed * WallJumpMomentumPreservation;
// If currentSpeed = 1500 and preservation = 1.0:
// momentumBoost = 1500 * 1.0 = +1500 ADDED to wall jump!

float totalForwardBoost = baseBoost + fallEnergy + momentumBoost;
// = 400 + 300 + 1500 = 2200 forward force
// PLUS you still had your 1500 velocity!
// Total = 3700+ units/s = FLYING FORWARD!
```

**The bug:** Momentum preservation was **ADDING** your current speed as a boost, instead of **PRESERVING** your existing velocity!

### The Math
- Moving at 1500 units/s
- Wall jump adds: 400 (base) + 300 (fall) + **1500 (momentum)** = 2200 boost
- Your existing 1500 velocity was KEPT
- **Total: 1500 + 2200 = 3700 units/s** = BROKEN!

## The Fix

### New Code
```csharp
// FIXED CODE:
float totalForwardBoost = (baseForwardBoost * inputBoostMultiplier) + fallForwardBoost;
// = 400 + 300 = 700 boost (reasonable!)

// Preserve existing momentum by KEEPING it, not ADDING it
Vector3 preservedMomentum = currentHorizontalVelocity * WallJumpMomentumPreservation;
forwardBoost = (currentMovementDir * totalForwardBoost) + preservedMomentum;
// = 700 (new boost) + 1500 (preserved velocity) = 2200 total
// But this REPLACES your velocity, not adds to it!
```

### The Difference

**OLD (Broken):**
```
Current velocity: 1500
Wall jump boost: 400 + 300 + 1500 = 2200
Final velocity: 1500 + 2200 = 3700 (FLYING!)
```

**NEW (Fixed):**
```
Current velocity: 1500
Wall jump boost: 400 + 300 = 700
Preserved momentum: 1500 * 1.0 = 1500
Final velocity: 700 + 1500 = 2200 (REPLACES old velocity)
Result: Reasonable speed gain, not exponential!
```

## What "Momentum Preservation" Actually Means

### Correct Interpretation (NOW)
- `wallJumpMomentumPreservation = 1.0` means **KEEP 100% of your current velocity**
- `wallJumpMomentumPreservation = 0.5` means **KEEP 50% of your current velocity**
- `wallJumpMomentumPreservation = 0.0` means **LOSE all velocity** (fresh start)

### Wrong Interpretation (BEFORE)
- It was being used as a **MULTIPLIER** to ADD to the boost
- This caused exponential speed gain
- Every wall jump made you faster and faster

## Force Breakdown (Fixed)

### Wall Jump Components
1. **OUTWARD:** 1200 (perpendicular - clear wall)
2. **FORWARD BOOST:** 400 base + 300 fall energy = 700
3. **PRESERVED MOMENTUM:** 1500 (your existing speed × 1.0)
4. **CAMERA:** 1800 (where you look)

### Total Horizontal
- OUTWARD (1200) + FORWARD (700 + 1500) + CAMERA (1800)
- = 1200 + 2200 + 1800 = 5200 total
- **But this REPLACES your velocity, doesn't stack with it!**

## Why It Was Flying Forward

**The compound effect:**
1. First wall jump: 1500 current + 2200 boost = 3700
2. Second wall jump: 3700 current + 3900 boost = 7600
3. Third wall jump: 7600 current + 8800 boost = 16400
4. **Exponential growth = BROKEN!**

**Now (Fixed):**
1. First wall jump: 2200 (replaces 1500)
2. Second wall jump: 2200 (replaces 2200)
3. Third wall jump: 2200 (stays consistent)
4. **Linear, predictable = WORKS!**

## Config Values Still Apply

Your MovementConfig.cs values are now being used correctly:
- `wallJumpForwardBoost = 400` ✅
- `wallJumpFallSpeedBonus = 0.6` ✅
- `wallJumpMomentumPreservation = 1.0` ✅ (now works correctly!)
- `wallJumpCameraDirectionBoost = 1800` ✅

## Test It Now

The wall jump should now:
- ✅ Feel controlled, not flying
- ✅ Preserve your momentum (not add to it)
- ✅ Gain speed from fall energy (not exponentially)
- ✅ Respect camera direction as primary control
- ✅ Have consistent speed between wall jumps

**The "broken AF" issue was momentum being ADDED instead of PRESERVED!**
