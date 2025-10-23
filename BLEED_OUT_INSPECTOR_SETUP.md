# 🎯 BLEEDING OUT SYSTEM - CORRECT INSPECTOR SETUP

## ⚠️ **ISSUES FOUND IN YOUR SETUP**

Looking at your Inspector screenshot, here are the problems:

### **1. BleedOutMovementController** ❌
- **Controller:** Shows "None" - Should auto-find CharacterController
- **Bleed Out Camera:** Shows "None" - This is OK (auto-assigned at runtime)
- **Crawl Speed:** 5 - **TOO FAST!** Should be 2.5
- **Gravity:** -300 - **WAY TOO STRONG!** Should be -20

### **2. DeathCameraController** ⚠️
- **Main Camera:** ✅ Correctly assigned
- **Player Transform:** Shows "None" - **NEEDS TO BE ASSIGNED!**
- **All Movement Controllers:** Show "None" - Should auto-find, but manual assignment is safer
- **Camera Height:** 250 - **TOO LOW!** Should be 500

---

## ✅ **CORRECT INSPECTOR SETTINGS**

### **BleedOutMovementController:**

```
=== BLEED OUT MOVEMENT ===
Crawl Speed: 2.5 (slow crawling)
Input Smoothing: 8 (smooth feel)
Gravity: -20 (normal gravity, NOT -300!)

=== REFERENCES ===
Controller: [Will auto-find CharacterController]
Bleed Out Camera: [Auto-assigned at runtime - leave empty]
```

**IMPORTANT:** If Controller shows "None" after entering Play Mode, your Player doesn't have a CharacterController component!

### **BleedOutUIManager:**

```
=== BLEED OUT SETTINGS ===
Bleed Out Duration: 30 (seconds)
Hold E Speed Multiplier: 8 (faster skip)
Skip Key: E

=== ICON SPRITES ===
Self Revive Icon: CIRCULARGOLD_SELFREVIVE_0 ✅
Skull Icon Sprite: CIRCULARRED_SELFREVIVE_0 ✅

=== VISUAL SETTINGS ===
Circular Progress Color: RED (255, 0, 0, 255)
Circular Progress Size: 50 ✅
Enable Rotation Animation: ✓
Rotation Speed: 30 ✅
```

### **DeathCameraController:**

```
=== CAMERA SETTINGS ===
Main Camera: MainCamera_ ✅ (your FPS camera)
Player Transform: [DRAG PLAYER GAMEOBJECT HERE!] ❌ MISSING!

=== MOVEMENT CONTROLLER REFERENCES ===
Aaa Movement Controller: [Auto-finds, but DRAG Player's AAAMovementController]
Clean AAA Crouch: [Auto-finds, but DRAG Player's CleanAAACrouch]
Bleed Out Movement Controller: [Auto-finds, but DRAG Player's BleedOutMovementController]
Aaa Camera Controller: [Auto-finds, but DRAG MainCamera_'s AAACameraController]

=== DEATH SEQUENCE SETTINGS ===
Camera Height: 500 (NOT 250!) ❌ TOO LOW!
Zoom Out Duration: 2 (smooth transition)
Pitch Angle: 60 (look down angle)

=== THIRD-PERSON FOLLOW SETTINGS ===
Enable Camera Follow: ✓
Follow Smoothness: 8 (smooth follow)

=== AAA VISUAL EFFECTS ===
Enable Breathing Effect: ✗ (disabled - causes twitching)
Enable Struggling Shake: ✗ (disabled - causes jitter)
Enable Wall Avoidance: ✓ (can leave on)
```

---

## 🔧 **HOW TO FIX YOUR SETUP**

### **Step 1: Fix BleedOutMovementController**

1. Select **Player** GameObject
2. Find **BleedOutMovementController** component
3. Change these values:
   - **Crawl Speed:** 2.5 (currently 5 - too fast!)
   - **Gravity:** -20 (currently -300 - way too strong!)
4. **Controller** should auto-find - if it shows "None" in Play Mode, you have a problem

### **Step 2: Fix DeathCameraController**

1. Select **Player** GameObject
2. Find **DeathCameraController** component
3. **CRITICAL:** Drag **Player** GameObject into **Player Transform** field
4. **Change Camera Height:** 500 (currently 250 - too low!)
5. **Optional but recommended:** Manually assign all movement controllers:
   - Drag Player's **AAAMovementController** → **Aaa Movement Controller**
   - Drag Player's **CleanAAACrouch** → **Clean AAA Crouch**
   - Drag Player's **BleedOutMovementController** → **Bleed Out Movement Controller**
   - Drag MainCamera_'s **AAACameraController** → **Aaa Camera Controller**

### **Step 3: Verify CharacterController Exists**

1. Select **Player** GameObject
2. Look for **Character Controller** component in Inspector
3. **If it doesn't exist:**
   - Click **Add Component**
   - Search: **Character Controller**
   - Click **Add**
4. **If it exists:** Make sure it's enabled (checkbox checked)

---

## 🧪 **TESTING CHECKLIST**

After fixing the Inspector values:

1. **Enter Play Mode**
2. **Check Console** - should see:
   ```
   [BleedOutMovement] Found CharacterController: [name]
   [DeathCameraController] Initialized - Original camera state saved
   ```
3. **Take damage until bleeding out**
4. **Check Console** - should see:
   ```
   [DeathCameraController] Starting bleed out camera mode
   [DeathCameraController] 🔴 DISABLED AAACameraController - NO MORE SPINNING!
   [DeathCameraController] Main camera DISABLED - hands stay attached to body
   [DeathCameraController] BleedOutCamera ENABLED
   [DeathCameraController] ✅ BleedOutMovementController ACTIVATED
   ```
5. **Press WASD** - should move slowly (not too fast!)
6. **Camera should be high overhead** - not too close

---

## 🚨 **COMMON PROBLEMS**

### **Problem: "Controller" shows "None"**
**Cause:** Player doesn't have CharacterController component
**Fix:** Add CharacterController component to Player GameObject

### **Problem: Can't move when bleeding out**
**Cause:** Gravity is -300 (way too strong) or CharacterController is missing
**Fix:** 
1. Change Gravity to -20
2. Verify CharacterController exists

### **Problem: Camera too close**
**Cause:** Camera Height is 250 (too low)
**Fix:** Change Camera Height to 500

### **Problem: Moving too fast**
**Cause:** Crawl Speed is 5 (too fast)
**Fix:** Change Crawl Speed to 2.5

### **Problem: "Player Transform is null" error**
**Cause:** Player Transform not assigned in DeathCameraController
**Fix:** Drag Player GameObject into Player Transform field

---

## 📊 **QUICK REFERENCE - CORRECT VALUES**

| Component | Setting | Correct Value | Your Value | Status |
|-----------|---------|---------------|------------|--------|
| **BleedOutMovementController** | Crawl Speed | 2.5 | 5 | ❌ TOO FAST |
| **BleedOutMovementController** | Gravity | -20 | -300 | ❌ TOO STRONG |
| **BleedOutMovementController** | Controller | Auto-find | None | ⚠️ CHECK |
| **DeathCameraController** | Camera Height | 500 | 250 | ❌ TOO LOW |
| **DeathCameraController** | Player Transform | Player | None | ❌ MISSING |
| **DeathCameraController** | Main Camera | MainCamera_ | MainCamera_ | ✅ CORRECT |

---

## 💎 **AFTER FIXING**

Once you fix these values:

1. ✅ **Crawl speed will be slow** (not too fast)
2. ✅ **Gravity will be normal** (not pulling you through floor)
3. ✅ **Camera will be high overhead** (not too close)
4. ✅ **Movement will work** (CharacterController found)
5. ✅ **No errors in console** (all references assigned)

**Fix these values and test again!** 🛡️
