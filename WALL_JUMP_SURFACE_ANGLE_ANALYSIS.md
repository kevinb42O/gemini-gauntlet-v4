# 🔍 WALL JUMP SURFACE ANGLE ANALYSIS & TRICK INTEGRATION

## 📊 CURRENT SYSTEM ANALYSIS

### **Question 1: Does surface angle affect wall jumps?**

**ANSWER: YES, but only for DETECTION, not TRAJECTORY**

#### **How It Currently Works:**

**Wall Detection (Lines 2823-2921):**
```csharp
// Uses ground normal as reference for tilted platforms
Vector3 playerUp = groundNormal; // Player's "up" relative to surface

// Wall must be 60-120° from player's up direction
float angleFromPlayerUp = Vector3.Angle(hit.normal, playerUp);
if (angleFromPlayerUp > 60f && angleFromPlayerUp < 120f)
{
    // Valid wall detected!
}
```

**What This Means:**
- ✅ **Tilted walls ARE detected** (e.g., 45° angled panels)
- ✅ **Works on slopes and ramps** (uses ground normal as reference)
- ❌ **Trajectory is NOT affected by wall angle** (always uses wall normal)

**Wall Jump Execution (Lines 2928-3113):**
```csharp
// Calculate base direction away from wall
Vector3 awayFromWall = wallNormal.normalized; // ALWAYS perpendicular to wall

// Forces applied:
Vector3 primaryPush = horizontalDirection * wallJumpOutForce; // Away from wall
Vector3 finalVelocity = totalHorizontalPush + (upDirection * dynamicUpForce);
```

**What This Means:**
- Wall jump ALWAYS pushes **perpendicular to wall surface**
- A 90° wall pushes you straight back
- A 45° tilted wall pushes you at 45° angle (perpendicular to surface)
- **Surface angle DOES affect trajectory** (indirectly via wall normal)

---

## 🎯 YOUR SPECIFIC QUESTIONS ANSWERED

### **Q1: "If panel is tilted upwards, should it perform better than 90°?"**

**CURRENT BEHAVIOR:**
- **45° tilted panel (leaning toward you):** Pushes you UP and AWAY (better vertical gain)
- **90° vertical wall:** Pushes you straight BACK (pure horizontal)
- **135° overhanging wall:** Pushes you DOWN and AWAY (worse trajectory)

**VERDICT:** ✅ **YES, tilted panels already perform better!**
- Tilted upward = more vertical component in wall normal
- Wall normal determines push direction
- **This is physically accurate and intuitive!**

### **Q2: "If I jump at certain angle toward surface, will that affect trajectory?"**

**CURRENT BEHAVIOR:**
```csharp
// Your approach velocity is preserved via momentum boost
Vector3 currentHorizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
float currentSpeed = currentHorizontalVelocity.magnitude;

// Movement boost preserves your momentum
movementBoost = currentMovementDir * (baseBoost + speedBonus);
```

**VERDICT:** ✅ **YES, approach angle matters!**
- Fast approach = more momentum preserved
- Approach direction = influences final trajectory
- **But wall normal is still the primary force**

### **Q3: "If I run straight into wall, jump, no input - will I fly perfectly backward?"**

**CURRENT BEHAVIOR:**
```csharp
// With NO input during wall jump:
if (inputDirection.magnitude < 0.2f)
{
    // No input - use pure wall direction
    horizontalDirection = (awayFromWall - Vector3.Dot(awayFromWall, playerUp) * playerUp).normalized;
}

// Camera boost ALWAYS applies (even with no input!)
cameraBoost = cameraForward * wallJumpCameraDirectionBoost;

// Final velocity = Wall push + Camera boost
```

**VERDICT:** ❌ **NO, you will NOT fly perfectly backward!**
- Wall push: ← (away from wall)
- Camera boost: → (camera direction) ← **ALWAYS ACTIVE**
- **Result: Trajectory influenced by camera direction even with no input!**

---

## 🚨 CRITICAL ISSUE IDENTIFIED

### **Problem: Camera Boost Breaks "No Input" Trajectory**

**Your Vision:**
> "If I do no input in the jump itself (only jump), will I fly perfectly backward?"

**Current Reality:**
- Camera boost is ALWAYS applied (800 units of force)
- Even with zero input, camera direction affects trajectory
- **You can't get a "pure" wall reflection**

**Why This Matters for Tricks:**
- Backflips need predictable trajectory
- Camera should control VIEW, not FORCE (during tricks)
- Trick system needs consistent physics

---

## 🎪 TRICK SYSTEM INTEGRATION ANALYSIS

### **Current Trick Jump System:**

**Trigger (AAACameraController.cs, Line 1487):**
```csharp
// Middle mouse click triggers trick jump
if (Input.GetMouseButtonDown(2)) // Middle mouse button
{
    movementController.TriggerTrickJumpFromExternalSystem();
    EnterFreestyleMode(); // Activates aerial camera control
}
```

**Freestyle Mode Features:**
- Full 360° camera rotation while airborne
- Mouse controls camera freely
- Stays active until landing
- Perfect for backflips, barrel rolls, etc.

### **The Problem with Current Wall Jump + Tricks:**

**Scenario: Wall Jump Backflip**
1. Run toward wall (facing wall)
2. Middle-click trick jump off wall
3. Camera rotates 180° for backflip view
4. **PROBLEM:** Camera boost pushes you FORWARD (camera direction)
5. **Result:** Trajectory changes mid-flip, breaks physics consistency

**What You Want:**
- Wall jump trajectory determined by wall normal (pure reflection)
- Camera controls VIEW only (not force)
- Tricks look cool without affecting physics
- Predictable, skill-based movement

---

## 💡 RECOMMENDED SOLUTION

### **Option A: Disable Camera Boost During Trick Jumps (RECOMMENDED)**

**Pros:**
- ✅ Pure wall reflection physics
- ✅ Predictable backflip trajectories
- ✅ Camera controls view, not force
- ✅ Skill-based trick system

**Cons:**
- ❌ Less camera-directed control during tricks
- ❌ Two different wall jump behaviors (normal vs trick)

**Implementation:**
```csharp
// In PerformWallJump(), check if trick jump is active
bool isTrickJump = (cameraController != null && cameraController.IsPerformingAerialTricks);

if (!isTrickJump && cameraTransform != null && wallJumpCameraDirectionBoost > 0)
{
    // Only apply camera boost for NORMAL wall jumps
    cameraBoost = cameraForward * wallJumpCameraDirectionBoost;
}
```

### **Option B: Reduce Camera Boost Based on Surface Angle**

**Pros:**
- ✅ Tilted surfaces get more boost (skill reward)
- ✅ Vertical walls get pure reflection
- ✅ Smooth gradient between angles

**Cons:**
- ❌ More complex to tune
- ❌ Less predictable for players

**Implementation:**
```csharp
// Scale camera boost based on wall angle
float wallAngleFromVertical = Vector3.Angle(wallNormal, Vector3.up);
float angleMultiplier = Mathf.InverseLerp(90f, 45f, wallAngleFromVertical); // 0 at 90°, 1 at 45°

cameraBoost = cameraForward * (wallJumpCameraDirectionBoost * angleMultiplier);
```

### **Option C: Input-Based Camera Boost (MOST FLEXIBLE)**

**Pros:**
- ✅ No input = pure reflection (perfect for tricks)
- ✅ Input = camera-directed (skill-based aiming)
- ✅ Best of both worlds

**Cons:**
- ❌ Slightly more complex logic

**Implementation:**
```csharp
// Only apply camera boost if player is giving input
float inputMagnitude = inputDirection.magnitude;

if (inputMagnitude > 0.1f)
{
    // Player is steering - apply camera boost
    float inputScale = Mathf.Clamp01(inputMagnitude);
    cameraBoost = cameraForward * (wallJumpCameraDirectionBoost * inputScale);
}
else
{
    // No input - pure wall reflection (perfect for tricks!)
    cameraBoost = Vector3.zero;
}
```

---

## 🎯 MY RECOMMENDATION: **OPTION C (Input-Based)**

### **Why This Is Perfect for Your Trick System:**

**Normal Wall Jump (with input):**
- Player steers with WASD
- Camera boost applies
- Skill-based aiming works
- Full control

**Trick Wall Jump (no input):**
- Player only presses Space (jump)
- Middle-click activates trick mode
- Camera boost = ZERO
- **Pure wall reflection = predictable backflip trajectory!**

**Backflip Combo Example:**
1. Run toward wall (W key held)
2. Release W key (no input)
3. Middle-click (trick jump + freestyle)
4. Press Space when touching wall
5. **Result:** Pure backward trajectory + 360° camera control
6. **Perfect backflip physics!**

### **Surface Angle Benefits:**
- 90° wall: Pure horizontal reflection (standard backflip)
- 45° tilted wall: Upward reflection (higher backflip)
- 135° overhang: Downward reflection (diving backflip)
- **Surface angle naturally affects trick trajectory!**

---

## 📋 IMPLEMENTATION CHECKLIST

If you want Option C (Input-Based Camera Boost):

### **Changes Needed:**

1. **MovementConfig.cs:**
   - Add `wallJumpCameraBoostRequiresInput` bool (default: true)

2. **AAAMovementController.cs:**
   - Modify camera boost calculation in `PerformWallJump()`
   - Only apply boost when `inputDirection.magnitude > 0.1f`
   - Add debug logging for testing

3. **Testing Scenarios:**
   - ✅ Wall jump with WASD input → Camera boost active
   - ✅ Wall jump with no input → Pure reflection
   - ✅ Trick jump (middle-click) → Pure reflection + freestyle camera
   - ✅ Tilted surface → Natural trajectory variation

### **Tuning Values:**

```
// For pure reflection (no camera boost):
wallJumpCameraDirectionBoost = 0f (disabled)

// For input-based (recommended):
wallJumpCameraDirectionBoost = 800f (active only with input)

// For always-on (current):
wallJumpCameraDirectionBoost = 800f (always active)
```

---

## 🎮 FINAL VERDICT

### **Current System:**
- ✅ Surface angle affects detection (works on tilted walls)
- ✅ Surface angle affects trajectory (via wall normal)
- ❌ Camera boost breaks "no input" reflection
- ❌ Not ideal for trick system integration

### **Recommended System (Option C):**
- ✅ Surface angle affects trajectory naturally
- ✅ No input = pure reflection (perfect for tricks)
- ✅ Input = camera-directed (skill-based)
- ✅ Trick system gets predictable physics
- ✅ Backflips work perfectly!

### **Your Dream Combo:**
```
1. Run toward tilted wall (45° angle)
2. Release movement keys (no input)
3. Middle-click (trick jump + freestyle)
4. Press Space (wall jump)
5. Result: Pure upward reflection + backflip camera
6. THE WORLD IS YOURS! 🌍
```

---

## 🚀 READY TO IMPLEMENT?

Let me know if you want:
- **Option A:** Disable camera boost during tricks only
- **Option B:** Scale camera boost by surface angle
- **Option C:** Input-based camera boost (RECOMMENDED)

I'll implement whichever you prefer! 🎪
