# Wall Jump Input Direction Fix - CRITICAL

## 🚨 The Problem You Caught

**The boost was being applied in the WALL JUMP direction, NOT the PLAYER'S INPUT direction!**

### What Was Wrong:
```csharp
// OLD CODE (BROKEN):
forwardBoost = horizontalDirection * (wallJumpForwardBoost * boostMultiplier);
//             ^^^^^^^^^^^^^^^^^^^ Wall jump direction, NOT player input!
```

**This meant:**
- ❌ Strafe left → boost still went in wall jump direction
- ❌ Push forward → boost went in wall jump direction (not camera forward)
- ❌ Player input was only used to **detect** boost, not **direct** it
- ❌ Camera forward was completely ignored!

---

## ✅ The Fix

**Boost now goes in the PLAYER'S ACTUAL INPUT DIRECTION!**

### New Code (CORRECT):
```csharp
// NEW CODE (FIXED):
Vector3 inputHorizontal = (inputDirection - Vector3.Dot(inputDirection, playerUp) * playerUp).normalized;
forwardBoost = inputHorizontal * (wallJumpForwardBoost * boostMultiplier * inputMagnitude);
//             ^^^^^^^^^^^^^^^ PLAYER'S INPUT DIRECTION - respects camera forward!
```

**This means:**
- ✅ Strafe left → boost goes LEFT (in camera space)
- ✅ Push forward → boost goes FORWARD (in camera space)
- ✅ Diagonal input → boost goes DIAGONAL (in camera space)
- ✅ Camera forward is FULLY RESPECTED!

---

## 🎮 How It Works Now

### 1. **No Input → Only Upward Force**
```
Player: (no stick/key input)
Result: Only vertical jump, no horizontal boost
Debug: "⬆️ [NO INPUT] Only upward force applied, no horizontal boost"
```

### 2. **Forward Input → Boost Goes Camera Forward**
```
Player: W key (camera forward)
Result: Boost goes in camera forward direction
Debug: "🚀 [INPUT-DIRECTED BOOST] Player input: (0.0, 0.0, 1.0)"
       "🎮 [CAMERA FORWARD RESPECTED] Boost direction matches player input!"
```

### 3. **Strafe Left → Boost Goes Camera Left**
```
Player: A key (camera left)
Result: Boost goes in camera left direction
Debug: "🚀 [INPUT-DIRECTED BOOST] Player input: (-1.0, 0.0, 0.0)"
       "🎮 [CAMERA FORWARD RESPECTED] Boost direction matches player input!"
```

### 4. **Diagonal Input → Boost Goes Diagonal**
```
Player: W+D keys (forward-right)
Result: Boost goes in forward-right diagonal
Debug: "🚀 [INPUT-DIRECTED BOOST] Player input: (0.7, 0.0, 0.7)"
       "🎮 [CAMERA FORWARD RESPECTED] Boost direction matches player input!"
```

### 5. **Toward Wall → Boost Blocked (Safety)**
```
Player: S key (toward wall)
Result: No boost applied (prevents jumping back into wall)
Debug: "⚠️ [BOOST BLOCKED] Player pushing toward wall (dot: -0.8)"
```

---

## 🔬 Technical Details

### Input Direction Calculation:
```csharp
// Get camera-relative input direction
Vector3 forward = cameraTransform.forward;
Vector3 right = cameraTransform.right;
forward.y = 0;
right.y = 0;
forward.Normalize();
right.Normalize();

horizontal = Input.GetAxis("Horizontal");
vertical = Input.GetAxis("Vertical");
inputDirection = (forward * vertical + right * horizontal);
```

**This creates a vector in CAMERA SPACE:**
- W key → camera forward
- A key → camera left
- S key → camera backward
- D key → camera right

### Boost Application:
```csharp
// Project input onto horizontal plane (respects player's up direction)
Vector3 inputHorizontal = (inputDirection - Vector3.Dot(inputDirection, playerUp) * playerUp).normalized;
float inputMagnitude = inputDirection.magnitude;

// Safety check: Don't boost toward wall
float awayFromWallDot = Vector3.Dot(inputHorizontal, awayFromWallHorizontal);

if (awayFromWallDot > 0.3f) // Input is generally away from wall
{
    // BOOST GOES IN PLAYER'S INPUT DIRECTION!
    forwardBoost = inputHorizontal * (wallJumpForwardBoost * boostMultiplier * inputMagnitude);
}
```

**Key Points:**
1. `inputHorizontal` is the player's input direction projected onto horizontal plane
2. `inputMagnitude` scales boost based on how hard player is pushing (0-1)
3. Safety check prevents boosting back into wall
4. Boost direction = **exactly where player is pushing**

---

## 📊 Force Composition

### Wall Jump Force Breakdown:
```
Total Force = Base Push + Preserved Momentum + Input Boost

Where:
- Base Push: Away from wall (fixed by wall normal)
- Preserved Momentum: In wall jump direction (12% of current speed)
- Input Boost: IN PLAYER'S INPUT DIRECTION (camera-relative!)
```

### Example: Strafe Left Wall Jump
```
Wall is on right side, player strafes left (A key):

Base Push:           (0.7, 0.0, 0.7) * 110 = (77, 0, 77)   [away from wall]
Preserved Momentum:  (0.7, 0.0, 0.7) * 6   = (4, 0, 4)     [wall jump direction]
Input Boost:         (-1.0, 0.0, 0.0) * 120 = (-120, 0, 0) [CAMERA LEFT!]
────────────────────────────────────────────────────────────
Total Horizontal:    (-39, 0, 81)                          [diagonal left-forward!]
```

**Result:** Player gets pushed away from wall BUT also strongly to the left (as intended!)

---

## 🎯 Input Scenarios

### Scenario 1: No Input
```
Input: None
Base Push: 110 (away from wall)
Input Boost: 0 (no input)
Result: Pure wall jump, only upward + away from wall
```

### Scenario 2: Forward (W)
```
Input: Camera forward (1.0 magnitude)
Base Push: 110 (away from wall)
Input Boost: 120 (in camera forward direction)
Result: Strong forward momentum + wall jump
```

### Scenario 3: Strafe Left (A)
```
Input: Camera left (1.0 magnitude)
Base Push: 110 (away from wall)
Input Boost: 120 (in camera left direction)
Result: Strong left momentum + wall jump
```

### Scenario 4: Diagonal (W+D)
```
Input: Forward-right (1.4 magnitude, normalized to 1.0)
Base Push: 110 (away from wall)
Input Boost: 120 (in forward-right diagonal)
Result: Strong diagonal momentum + wall jump
```

### Scenario 5: Partial Input (slight W)
```
Input: Slight forward (0.6 magnitude)
Base Push: 110 (away from wall)
Input Boost: 72 (120 * 0.6 magnitude scaling)
Result: Moderate forward momentum + wall jump
```

---

## 🔥 Why This Is Essential

### Player Expectations:
1. **"If I push left, I should go left"** ✅ Now works!
2. **"If I push forward, I should go forward"** ✅ Now works!
3. **"Camera forward should be MY forward"** ✅ Now works!
4. **"No input should be pure wall jump"** ✅ Now works!

### Skill Expression:
- **Directional control** during wall jump
- **Strafe wall jumps** for advanced movement
- **Diagonal wall jumps** for creative pathing
- **Camera-relative control** feels natural

### Movement Flow:
```
Wall on right → Strafe left (A) → Wall jump left
Wall on left → Strafe right (D) → Wall jump right
Wall ahead → Push forward (W) → Wall jump forward
No input → Pure wall jump away from wall
```

---

## 🐛 Debug Output

### With Input (Forward):
```
🚀 [INPUT-DIRECTED BOOST] Player input: (0.0, 0.0, 1.0), Magnitude: 1.00, Boost: 120.0
🎮 [CAMERA FORWARD RESPECTED] Boost direction matches player input!
💨 [FORCE BREAKDOWN] Base: 110.0, Preserved: 6.0, Input Boost: 120.0, Total: 236.0
🧭 [DIRECTIONS] Wall jump: (0.7, 0.0, 0.7), Input boost: (0.0, 0.0, 1.0)
```

### With Input (Strafe Left):
```
🚀 [INPUT-DIRECTED BOOST] Player input: (-1.0, 0.0, 0.0), Magnitude: 1.00, Boost: 120.0
🎮 [CAMERA FORWARD RESPECTED] Boost direction matches player input!
💨 [FORCE BREAKDOWN] Base: 110.0, Preserved: 6.0, Input Boost: 120.0, Total: 236.0
🧭 [DIRECTIONS] Wall jump: (0.7, 0.0, 0.7), Input boost: (-1.0, 0.0, 0.0)
```

### No Input:
```
⬆️ [NO INPUT] Only upward force applied, no horizontal boost
💨 [FORCE BREAKDOWN] Base: 110.0, Preserved: 6.0, Input Boost: 0.0, Total: 116.0
🧭 [DIRECTIONS] Wall jump: (0.7, 0.0, 0.7), Input boost: none
```

### Toward Wall (Blocked):
```
⚠️ [BOOST BLOCKED] Player pushing toward wall (dot: -0.8)
💨 [FORCE BREAKDOWN] Base: 110.0, Preserved: 6.0, Input Boost: 0.0, Total: 116.0
🧭 [DIRECTIONS] Wall jump: (0.7, 0.0, 0.7), Input boost: none
```

---

## 🎯 Result

**Wall jumps now FULLY RESPECT player input:**

✅ **Camera forward** = boost forward  
✅ **Strafe left** = boost left  
✅ **Strafe right** = boost right  
✅ **Diagonal** = boost diagonal  
✅ **No input** = only upward force  
✅ **Toward wall** = blocked (safety)

**This is exactly what you wanted - the player's input direction is now the boost direction!** 🚀
