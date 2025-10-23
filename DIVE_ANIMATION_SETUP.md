# 🎯 DIVE ANIMATION SETUP - Layered System

## ✅ Your Animator Setup is Perfect!

You've set up the dive animation correctly in Unity Animator:
- **Sprint → Dive** transition
- **Dive → Idle** transition with **Exit Time = 1** (plays full animation)

This works perfectly with the layered animation system!

---

## 🎮 How Dive Works

### 1. **Player Input (CleanAAACrouch)**
```
Press X while sprinting →
  CleanAAACrouch.StartTacticalDive() →
    Sets isDiving = true
```

### 2. **Automatic State Detection (PlayerAnimationStateManager)**
```csharp
if (crouchController.IsDiving)
{
    return PlayerAnimationState.Dive; // State = 6
}
```

### 3. **Hand Animation (LayeredHandAnimationController)**
```csharp
SetMovementState((int)MovementState.Dive); // Converts 6 → 7 for hand animator
```

### 4. **Unity Animator (Base Layer)**
```
movementState parameter = 7 →
  Sprint state → Dive state (your transition) →
  Dive animation plays fully (Exit Time = 1) →
  Dive state → Idle state (your transition)
```

---

## 📋 State Value Mapping

**IMPORTANT:** There's a state number difference between systems!

### PlayerAnimationStateManager (Brain):
```csharp
Idle = 0
Walk = 1
Sprint = 2
Jump = 3
Land = 4
Slide = 5
Dive = 6     ← PlayerAnimationStateManager uses 6
Flight = 7
TakeOff = 8
```

### IndividualLayeredHandController (Hands):
```csharp
Idle = 0
Walk = 1
Sprint = 2
Jump = 3
Land = 4
TakeOff = 5
Slide = 6
Dive = 7     ← Unity Animator parameter uses 7
FlyForward = 8
// etc...
```

The system automatically converts between these! When PlayerAnimationStateManager detects Dive (6), it gets converted to the hand animator value (7).

---

## 🔧 Unity Animator Setup Checklist

### ✅ Base Layer (Movement Layer):

**States Needed:**
- [ ] Idle (movementState == 0)
- [ ] Walk (movementState == 1)
- [ ] Sprint (movementState == 2)
- [ ] Jump (movementState == 3)
- [ ] Land (movementState == 4)
- [ ] TakeOff (movementState == 5)
- [ ] Slide (movementState == 6)
- [ ] **Dive (movementState == 7)** ← Your new state!

**Transitions:**
1. **Sprint → Dive**
   - Condition: `movementState == 7`
   - Has Exit Time: ✅ (recommended for smooth blend)
   - Exit Time: 0.8 or similar (near end of sprint cycle)
   - Transition Duration: 0.1-0.2 seconds

2. **Dive → Idle**
   - Has Exit Time: ✅ **YES (Exit Time = 1)** ← Your setting is correct!
   - Exit Time: 1.0 (plays full animation)
   - Transition Duration: 0.2 seconds (smooth blend to idle)

**Optional Transitions** (for flexibility):
- **Any State → Dive** (if movementState == 7) - For emergency dive from any state
- **Dive → Sprint** (if movementState == 2 AND exit time > 0.8) - Quick recovery

---

## 🎯 Dive Animation Flow

```
1. Player sprints (W + Shift)
   └─> Sprint animation plays (movementState = 2)

2. Player presses X
   └─> CleanAAACrouch.StartTacticalDive()
       └─> isDiving = true
           └─> PlayerAnimationStateManager detects dive
               └─> Sets movementState = 7
                   └─> Unity Animator: Sprint → Dive transition triggers!

3. Dive animation plays
   └─> Full animation plays (Exit Time = 1)
       └─> Player dives forward with physics
           └─> Lands and slides

4. Dive animation completes
   └─> Exit Time reaches 1.0
       └─> Unity Animator: Dive → Idle transition triggers!
           └─> Idle animation plays
               └─> Player can move again
```

---

## 🔥 Integration with Layered System

### **Base Layer (Movement):**
- Sprint, Dive, Idle animations all play on Base Layer
- Dive plays **full body animation** (weights controlled by blend mask)

### **Shooting Layer (Additive):**
- **You CAN shoot while diving!** 🎯
- Shooting gestures blend on top of dive animation
- Layer weight transitions smoothly

### **Emote Layer (Override):**
- Emotes are blocked during dive (dive takes priority)
- System automatically prevents emotes during tactical actions

### **Ability Layer (Override):**
- Armor plates blocked during dive
- Dive is a tactical maneuver that locks out heavy actions

---

## 🚀 Testing the Dive

### Test Sequence:
1. **Start Game**
2. **Sprint** (W + Shift)
3. **Press X** while sprinting
4. **Watch:**
   - Sprint → Dive transition (smooth blend)
   - Dive animation plays fully
   - Physics: Forward dive with arc
   - Dive → Idle transition (at end of animation)

### Console Logs to Watch For:
```
[DIVE DEBUG] STARTING DIVE NOW!
[DIVE] Dive started: forward force...
[PlayerAnimationStateManager] Movement state changed: Sprint → Dive
[IndividualLayeredHandController] RobotArmII_R movement: Dive (offset: 0.XX)
```

### Expected Behavior:
- ✅ Smooth Sprint → Dive transition
- ✅ Dive animation plays fully (no interruption)
- ✅ Player launches forward with physics
- ✅ Smooth Dive → Idle transition when animation completes
- ✅ Can shoot during dive (if enabled)
- ✅ Dive blocks other tactical actions

---

## 🔧 Tuning the Dive

### Animation Timing:
```csharp
// In CleanAAACrouch.cs
private float diveDuration = 0.8f; // How long the dive lasts
```

**Unity Animator:**
- Make sure your **Dive animation clip** length matches or is close to `diveDuration`
- Exit Time = 1.0 means animation plays to 100% completion
- Transition Duration controls blend to next state (0.2s recommended)

### Physics Parameters:
```csharp
[SerializeField] private float diveForwardForce = 720f;  // Forward launch
[SerializeField] private float diveUpwardArc = 120f;     // Upward arc
[SerializeField] private float diveDuration = 0.8f;      // Animation/physics length
```

Adjust in CleanAAACrouch Inspector to match your animation feel!

---

## 🎯 Advanced: Dive Recovery Options

### Option 1: Dive → Sprint Recovery
If you want player to **resume sprinting** after dive:

**Transition:**
- Dive → Sprint
- Condition: `movementState == 2` (player still holding Shift)
- Has Exit Time: ✅ YES
- Exit Time: 0.8-0.9 (near end of dive)
- Allows smooth dive-to-sprint flow

### Option 2: Dive → Idle Recovery (Your Current Setup)
**Transition:**
- Dive → Idle
- Has Exit Time: ✅ YES
- Exit Time: 1.0 (full animation)
- **This is safer and more controlled!**

Your current setup (Dive → Idle) is the **recommended approach**!

---

## ✅ Summary

**Your Setup:**
```
Sprint → Dive (when X pressed)
Dive → Idle (Exit Time = 1)
```

**Result:**
- ✅ Dive triggers from sprint
- ✅ Dive animation plays fully (no interruption)
- ✅ Returns to idle when complete
- ✅ Integrates with layered system
- ✅ Can shoot while diving (if desired)

**Status:** PERFECT! Your animator setup is exactly right for the layered hand animation system! 🎯

---

## 🔍 Debugging

If dive animation doesn't play:

1. **Check Console** for dive trigger logs
2. **Check movementState parameter** in Unity Animator window (should be 7)
3. **Verify transition conditions** (Sprint → Dive needs movementState == 7)
4. **Check dive animation clip** is assigned to Dive state
5. **Verify Base Layer** has Dive state with correct parameter value

Most common issue: **Transition condition missing or wrong parameter value**

---

**File Setup Complete!** Your dive animation will work perfectly with the new layered hand system! 🚀
