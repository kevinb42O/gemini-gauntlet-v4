# 🔍 INPUT DETECTION DEBUG GUIDE

**Date:** 2025-10-06  
**Status:** 🔧 **DEBUGGING INPUT DETECTION**

---

## 🎯 Your Observation

> "maybe the sprint input and jump input and landing input isnt properly detected and your script is brilliant but the detection is off"

**You're absolutely right!** The animation system is solid, but if inputs aren't detected properly, nothing works. Let me help you debug this.

---

## 🔍 Current Input Detection System

### **Sprint Detection (in HandAnimationController):**
```csharp
// Line 551 in HandAnimationController.cs
bool isSprintKeyHeld = Input.GetKey(Controls.Boost);

// Line 556
bool isCurrentlySprinting = hasMovementInput && isSprintKeyHeld && isGrounded && hasEnergyToSprint;
```

### **Jump Detection (in AAAMovementController):**
```csharp
// Line 1134 in AAAMovementController.cs
_handAnimationController?.OnPlayerJumped();
```

### **Key Mappings (in Controls.cs):**
```csharp
public const KeyCode Boost = KeyCode.LeftShift;    // Line 15 - Used for sprint
public const KeyCode Sprint = KeyCode.LeftShift;   // Line 30 - Duplicate!
public const KeyCode UpThrustJump = KeyCode.Space; // Line 13 - Used for jump
```

---

## 🚨 Potential Issues Found

### **Issue #1: Duplicate Sprint Keys**
```csharp
Controls.Boost = KeyCode.LeftShift   ← Used by HandAnimationController
Controls.Sprint = KeyCode.LeftShift  ← Duplicate definition!
```
**Impact:** Confusing, but both point to same key so shouldn't break.

### **Issue #2: Complex Sprint Conditions**
```csharp
bool isCurrentlySprinting = hasMovementInput && isSprintKeyHeld && isGrounded && hasEnergyToSprint;
```
**Requirements for Sprint Animation:**
- ✅ Movement input (WASD)
- ✅ Sprint key held (LeftShift)
- ✅ Player grounded
- ✅ Has energy to sprint

**If ANY of these fail, no sprint animation!**

### **Issue #3: High Priority State Blocking**
```csharp
// Line 547
if (IsInHighPriorityState(_leftHandState) || IsInHighPriorityState(_rightHandState)) return;
```
**If either hand is in high priority state, movement updates are SKIPPED!**

---

## 🔧 DEBUG STEPS

### **Step 1: Enable Debug Logs**
In HandAnimationController, set:
```csharp
public bool enableDebugLogs = true; // Make sure this is TRUE
```

### **Step 2: Test Sprint Detection**
Add this temporary debug code to `UpdateMovementAnimations()` (around line 550):

```csharp
// TEMPORARY DEBUG - Add after line 553
if (enableDebugLogs)
{
    Debug.Log($"[SPRINT DEBUG] Movement: {hasMovementInput}, Sprint Key: {isSprintKeyHeld}, Grounded: {isGrounded}, Energy: {hasEnergyToSprint}");
    Debug.Log($"[SPRINT DEBUG] Currently Moving: {isCurrentlyMoving}, Currently Sprinting: {isCurrentlySprinting}");
    Debug.Log($"[SPRINT DEBUG] Left State: {_leftHandState.currentState}, Right State: {_rightHandState.currentState}");
}
```

### **Step 3: Test Jump Detection**
Add this to `OnPlayerJumped()` method (around line 1635):

```csharp
public void OnPlayerJumped()
{
    if (enableDebugLogs)
        Debug.Log("[JUMP DEBUG] OnPlayerJumped called - triggering jump animation");
    PlayJumpBoth();
}
```

---

## 🎮 Testing Procedure

### **Test 1: Sprint Detection**
1. **Hold LeftShift + W**
2. **Check Console for:**
   ```
   [SPRINT DEBUG] Movement: True, Sprint Key: True, Grounded: True, Energy: True
   [SPRINT DEBUG] Currently Sprinting: True
   [HandAnimationController] Movement: Sprint
   ```

### **Test 2: Jump Detection**
1. **Press Space (while grounded)**
2. **Check Console for:**
   ```
   [JUMP DEBUG] OnPlayerJumped called - triggering jump animation
   [HandAnimationController] LEFT: Idle → Jump (P6)
   [HandAnimationController] RIGHT: Idle → Jump (P6)
   ```

### **Test 3: Landing Detection**
1. **Jump and land**
2. **Check Console for:**
   ```
   [HandAnimationController] LEFT: Jump → Idle (P0)
   [HandAnimationController] RIGHT: Jump → Idle (P0)
   ```

---

## 🚨 Common Issues & Solutions

### **Issue: Sprint Not Working**

**Possible Causes:**
1. **No Energy:** `PlayerEnergySystem.CanSprint = false`
   - **Solution:** Check energy bar, wait for regen
2. **Not Grounded:** `aaaMovementController.IsGrounded = false`
   - **Solution:** Make sure player is on ground
3. **No Movement Input:** Not pressing WASD
   - **Solution:** Must hold WASD + LeftShift
4. **High Priority State:** Hand locked in combat/emote
   - **Solution:** Wait for combat to finish

### **Issue: Jump Not Working**

**Possible Causes:**
1. **AAAMovementController not calling OnPlayerJumped()**
   - **Solution:** Check if jump actually happens in movement
2. **HandAnimationController reference missing**
   - **Solution:** Check if `_handAnimationController` is found
3. **Jump blocked by priority system**
   - **Solution:** Check current hand states

### **Issue: Landing Not Working**

**Possible Causes:**
1. **No landing detection in movement system**
   - **Solution:** Movement system needs to call landing method
2. **Jump animation not completing properly**
   - **Solution:** Check if jump is one-shot animation

---

## 🔍 Advanced Debugging

### **Check Energy System:**
```csharp
// In Update(), add temporary debug
if (playerEnergySystem != null && enableDebugLogs)
{
    Debug.Log($"[ENERGY DEBUG] Current: {playerEnergySystem.CurrentEnergy}, Can Sprint: {playerEnergySystem.CanSprint}");
}
```

### **Check Movement Controller:**
```csharp
// In Update(), add temporary debug
if (aaaMovementController != null && enableDebugLogs)
{
    Debug.Log($"[MOVEMENT DEBUG] Speed: {aaaMovementController.CurrentSpeed}, Grounded: {aaaMovementController.IsGrounded}");
}
```

### **Check Input Directly:**
```csharp
// In Update(), add temporary debug
if (enableDebugLogs)
{
    Debug.Log($"[INPUT DEBUG] LeftShift: {Input.GetKey(KeyCode.LeftShift)}, Space: {Input.GetKey(KeyCode.Space)}, WASD: {Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D)}");
}
```

---

## 🎯 Quick Fix Checklist

### **For Sprint Issues:**
- [ ] `enableDebugLogs = true` in HandAnimationController
- [ ] Player has energy (check energy bar)
- [ ] Player is grounded (not jumping/falling)
- [ ] Holding WASD + LeftShift simultaneously
- [ ] No combat actions active (shotgun/beam)

### **For Jump Issues:**
- [ ] Space key actually triggers jump in movement
- [ ] HandAnimationController reference exists
- [ ] Jump animation clips are assigned
- [ ] No hard locks active (emotes/abilities)

### **For Landing Issues:**
- [ ] Movement system detects landing
- [ ] Landing method exists and is called
- [ ] Landing animation clips assigned

---

## 🚀 Expected Debug Output

### **Working Sprint:**
```
[SPRINT DEBUG] Movement: True, Sprint Key: True, Grounded: True, Energy: True
[SPRINT DEBUG] Currently Sprinting: True
[HandAnimationController] Movement: Sprint
[HandAnimationController] LEFT: Idle → Sprint (P8)
[HandAnimationController] RIGHT: Idle → Sprint (P8)
```

### **Working Jump:**
```
[JUMP DEBUG] OnPlayerJumped called - triggering jump animation
[HandAnimationController] LEFT: Sprint → Jump (P6)
[HandAnimationController] RIGHT: Sprint → Jump (P6)
```

### **Working Combat:**
```
[HandAnimationController] LEFT: Sprint → Shotgun (P7)
[HandAnimationController] LEFT brief combat complete - checking for sprint return
[HandAnimationController] LEFT: Shotgun → Sprint (P8)
```

---

## 🏆 Next Steps

1. **Enable debug logs** and test each input
2. **Check console output** against expected patterns
3. **Identify which condition is failing**
4. **Apply appropriate fix**

**The animation system is solid - let's find where the inputs are getting lost!** 🔍

---

**Run these tests and let me know what the console shows!** 🎮✨
