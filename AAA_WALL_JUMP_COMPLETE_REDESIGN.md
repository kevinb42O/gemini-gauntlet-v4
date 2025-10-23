# ✅ AAA WALL JUMP - COMPLETE REDESIGN

## Analysis - What Was Broken

### Issue 1: Config Values Not Being Read
```csharp
// Line 3111 - BROKEN:
horizontalDirection = Vector3.Lerp(..., wallJumpInputInfluence).normalized;
//                                      ^^^^^^^^^^^^^^^^^^^^^^
//                                      LOWERCASE = Inspector fallback, NOT config!
```

### Issue 2: Momentum Being Added Multiple Times
```csharp
// OLD BROKEN CODE:
Vector3 preservedMomentum = currentVelocity * 1.0;  // 1500
forwardBoost = boost + preservedMomentum;           // 700 + 1500 = 2200
totalPush = outward + forwardBoost + camera;        // 1200 + 2200 + 1800 = 5200
velocity = totalPush;                               // FLYING AT 5200!
```

### Issue 3: Three Directions Being Combined
- **OUTWARD** (perpendicular to wall)
- **FORWARD** (tangent to wall)  
- **CAMERA** (where you look)

These were all being ADDED together, creating unpredictable diagonal forces!

### Issue 4: Wrong Direction Priority
The system tried to blend wall normal + input + camera + movement direction, creating confusion.

## The Solution - AAA Clean Design

### Simple Direction Logic
```csharp
// Priority: Camera > Wall Normal
if (has camera direction && camera boost > 0)
    direction = camera direction  // Where you look
else
    direction = wall normal       // Away from wall
```

**ONE direction. No blending. No confusion.**

### Simple Force Calculation
```csharp
// UPWARD: Constant
upForce = 1900

// HORIZONTAL: Base + Camera Boost + Fall Energy
horizontalForce = 1200              // Base (outward from wall)
if (using camera)
    horizontalForce += 1800         // Camera boost
horizontalForce += fallSpeed * 0.6  // Fall energy

// FINAL VELOCITY
velocity = (direction * horizontalForce) + (up * upForce)
```

**Simple addition. No triple-stacking. Predictable.**

### Momentum Preservation (Fixed)
```csharp
// OLD (BROKEN): Add momentum as boost
forwardBoost += preservedMomentum  // Stacks with everything!

// NEW (FIXED): Blend with current velocity
newVelocity = Lerp(wallJumpVelocity, wallJumpVelocity + currentVelocity, preservation)
// preservation = 1.0 → keep 100% of current velocity
// preservation = 0.0 → use only wall jump velocity
```

**Momentum is blended, not added. No exponential growth.**

## Force Breakdown (NEW)

### Scenario: Wall Jump While Moving Fast

**Config Values (from MovementConfig.cs):**
- `wallJumpUpForce = 1900`
- `wallJumpOutForce = 1200`
- `wallJumpCameraDirectionBoost = 1800`
- `wallJumpFallSpeedBonus = 0.6`
- `wallJumpMomentumPreservation = 1.0`

**Calculation:**
```
Current velocity: 1500 units/s
Fall speed: 800 units/s

UPWARD:
  upForce = 1900

HORIZONTAL:
  base = 1200
  camera boost = 1800 (if using camera)
  fall energy = 800 * 0.6 = 480
  total = 1200 + 1800 + 480 = 3480

DIRECTION:
  Camera forward (where you're looking)

NEW VELOCITY:
  wallJumpVelocity = (cameraDir * 3480) + (up * 1900)
  = ~3480 horizontal + 1900 vertical
  
MOMENTUM PRESERVATION (1.0):
  finalVelocity = Lerp(wallJumpVelocity, wallJumpVelocity + currentVelocity, 1.0)
  = wallJumpVelocity + currentVelocity
  = 3480 + 1500 = 4980 in camera direction

Result: 4980 units/s in camera direction
```

**But wait - that's still high!** Let me check the Lerp logic...

Actually, I see the issue. The Lerp is wrong. Let me fix it:

