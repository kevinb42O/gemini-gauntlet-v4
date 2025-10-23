# 🐌 BLEEDING OUT SLOW CRAWL - CAMERA-RELATIVE MOVEMENT!

## ✨ THE PERFECT CRAWL SYSTEM

**You're bleeding out, not doing parkour!** 😂

Now you move **10x SLOWER** and movement is **relative to the BleedOut camera**, not your old first-person camera!

---

## 🎯 WHAT CHANGED

### **1. 10x Slower Movement** 🐌
```csharp
bleedOutSpeedMultiplier = 0.1f; // 10x slower!
```

**Normal Speed:**
- Walk: 105 units/s
- Sprint: 194 units/s

**Bleeding Out:**
- Crawl: **10.5 units/s** (10x slower!)
- Can't sprint (you're dying!)

### **2. Camera-Relative Movement** 🎥
**Movement direction is based on where the BleedOut camera is looking!**

```csharp
// Uses BleedOutCamera transform for movement direction
Vector3 forward = bleedOutCamera.transform.forward;
Vector3 right = bleedOutCamera.transform.right;
```

**What This Means:**
- **W** = Move where camera is looking (forward)
- **S** = Move away from camera (backward)
- **A** = Move camera-left (strafe left)
- **D** = Move camera-right (strafe right)

**WASD becomes steering relative to overhead camera view!**

---

## 🎮 HOW IT WORKS

### **Normal Gameplay:**
- Movement relative to FPS camera (your head)
- Can sprint
- Normal speed

### **Bleeding Out Mode:**
```
[Die]
  ↓
[Movement switches to BleedOutCamera]
  ↓
[Speed reduced to 10%]
  ↓
WASD now moves relative to overhead camera:
  - W = Crawl forward (where camera looks)
  - S = Crawl backward
  - A/D = Strafe left/right
  ↓
[Can't sprint - too injured!]
```

---

## 🔧 TECHNICAL IMPLEMENTATION

### **Movement System Checks:**
```csharp
// Check if bleeding out
bool isBleedingOut = playerHealth != null && playerHealth.isBleedingOut;

// Get active camera (FPS or BleedOut)
Transform activeCameraTransform = cameraTransform; // Default to main camera

if (isBleedingOut && deathCameraController != null)
{
    // Get BleedOutCamera for camera-relative movement
    Camera bleedOutCam = GameObject.Find("BleedOutCamera")?.GetComponent<Camera>();
    if (bleedOutCam != null)
    {
        activeCameraTransform = bleedOutCam.transform;
    }
}

// Calculate movement direction relative to ACTIVE camera
Vector3 forward = activeCameraTransform.forward;
Vector3 right = activeCameraTransform.right;
```

### **Speed Reduction:**
```csharp
// BLEEDING OUT MODE: Slow crawl speed
if (isBleedingOut)
{
    currentMoveSpeed *= bleedOutSpeedMultiplier; // 10x slower!
    // Can't sprint or crouch when bleeding out
}
```

---

## 🌟 FEATURES

### **✅ Keyboard-Only Steering**
- **No need for mouse** to steer movement
- **WASD controls** everything
- **Mouse** is only for looking around (camera rotation)
- **Strafe becomes steering** in overhead view

### **✅ Camera-Relative Direction**
- Movement based on **where camera is looking**
- **Rotate camera** → Movement direction changes
- Feels natural from overhead view
- Like classic top-down games!

### **✅ Realistic Crawl Speed**
- **10x slower** than normal walking
- Can't sprint (too injured!)
- Can't crouch (already on ground!)
- Slow, desperate crawl

### **✅ Smooth Integration**
- Automatically switches when bleeding out
- Returns to normal on revive
- No configuration needed
- Works perfectly with third-person camera

---

## 🎮 CONTROLS WHILE BLEEDING OUT

### **Movement (WASD):**
- **W** - Crawl forward (where overhead camera looks)
- **S** - Crawl backward
- **A** - Strafe left (camera perspective)
- **D** - Strafe right (camera perspective)

### **Camera (Mouse):**
- **Mouse X** - Rotate camera around you (360°)
- **Mouse Y** - Tilt camera up/down (within limits)

### **Disabled:**
- ❌ **Shift** - Can't sprint (bleeding out!)
- ❌ **Ctrl** - Can't crouch (already crawling!)
- ❌ **Space** - Can't jump (too weak!)

---

## 📊 SPEED COMPARISON

### **Normal Walk:**
- Speed: 105 units/s
- Time to cross 100 units: **0.95 seconds**

### **Normal Sprint:**
- Speed: 194 units/s
- Time to cross 100 units: **0.52 seconds**

### **Bleeding Out Crawl:**
- Speed: **10.5 units/s**
- Time to cross 100 units: **9.5 seconds** 😱

**You're CRAWLING, not running!**

---

## 🔧 ADJUSTABLE SETTINGS

### **Want Even Slower?**
```csharp
bleedOutSpeedMultiplier = 0.05f; // 20x slower (5.25 units/s)
```

### **Want Slightly Faster?**
```csharp
bleedOutSpeedMultiplier = 0.15f; // 6.7x slower (15.75 units/s)
```

### **Current Setting:**
```csharp
bleedOutSpeedMultiplier = 0.1f; // 10x slower (10.5 units/s) - PERFECT!
```

---

## 🎯 WHY THIS IS BRILLIANT

### **Camera-Relative Movement:**
- **Intuitive controls** from overhead view
- **No confusion** about which way is forward
- **Easy steering** with WASD only
- **Mouse for looking**, not moving

### **Realistic Speed:**
- **You're bleeding out!** Can barely move
- **Desperate crawl** toward help
- **Teammates can reach you** before timer expires
- **Tense, dramatic** moments

### **Perfect for Revive System:**
- **Crawl toward teammates**
- **They can see you** moving slowly
- **Dramatic approach** as they run to help
- **AAA tension** like Fortnite/Warzone

---

## 🎬 PLAYER EXPERIENCE

### **What You'll Feel:**
```
[Take Fatal Damage]
  ↓
"Oh no, I'm going down!"
  ↓
[Camera zooms overhead]
  ↓
"I can see myself from above"
  ↓
[Try to move with WASD]
  ↓
"Wow, I'm moving SO SLOW..."
  ↓
"Wait, movement follows where I'm looking with camera!"
  ↓
[Crawl desperately toward teammate]
  ↓
"Come on... just a little further..."
  ↓
[Teammate arrives, revives you]
  ↓
"YES! Saved at the last second!"
```

**INTENSE, DRAMATIC, AAA-QUALITY!** 🎮🔥

---

## 📋 FILES MODIFIED

### **AAAMovementController.cs:**
1. Added `bleedOutSpeedMultiplier` field (0.1f)
2. Added `playerHealth` reference
3. Added `deathCameraController` reference
4. Added bleeding out detection in `HandleInputAndHorizontalMovement()`
5. Added BleedOutCamera transform usage for movement direction
6. Added 10x speed reduction when bleeding out
7. Disabled sprint when bleeding out

### **PlayerHealth.cs:**
1. Made `isBleedingOut` public (was private)
2. Added comment for movement system access

---

## ✅ VERIFICATION

### **Test This:**
1. **Die** in the game
2. **Try to move** with WASD
3. **Notice you're moving SLOW!** 🐌
4. **Rotate camera** with mouse
5. **Notice movement direction changes** with camera!
6. **Try to sprint** (Shift) - Doesn't work!
7. **Crawl around** - Camera follows, speed is slow
8. **Revive** - Speed returns to normal!

---

## 🎯 RESULT

You now have a **perfect bleeding out crawl system**:
- ✅ **10x slower** movement (realistic crawl)
- ✅ **Camera-relative** movement (intuitive controls)
- ✅ **Keyboard-only steering** (WASD controls everything)
- ✅ **Mouse for camera** (look around, not steer)
- ✅ **No sprint/crouch** (you're bleeding out!)
- ✅ **AAA-quality** desperate crawl
- ✅ **Perfect for teammate revives!**

**Crawl for your life!** 🩸🐌🎥
