# 🎨 DIRECTIONAL SPRINT VISUAL REFERENCE
## Quick Guide for Animation Artists

---

## 📐 HAND BEHAVIOR MATRIX

```
┌─────────────────┬──────────────────┬──────────────────┐
│  INPUT KEYS     │   LEFT HAND      │   RIGHT HAND     │
├─────────────────┼──────────────────┼──────────────────┤
│  W (Forward)    │  Normal Sprint   │  Normal Sprint   │
│  Shift          │  (Value: 0)      │  (Value: 0)      │
│                 │  Standard swing  │  Standard swing  │
└─────────────────┴──────────────────┴──────────────────┘

┌─────────────────┬──────────────────┬──────────────────┐
│  A (Left)       │  💪 EMPHASIZED   │  Subdued         │
│  + Shift        │  (Value: 1)      │  (Value: 2)      │
│                 │  WIDE SWING!     │  Subtle motion   │
└─────────────────┴──────────────────┴──────────────────┘

┌─────────────────┬──────────────────┬──────────────────┐
│  D (Right)      │  Subdued         │  💪 EMPHASIZED   │
│  + Shift        │  (Value: 2)      │  (Value: 1)      │
│                 │  Subtle motion   │  WIDE SWING!     │
└─────────────────┴──────────────────┴──────────────────┘

┌─────────────────┬──────────────────┬──────────────────┐
│  S (Backward)   │  🔙 Backward     │  🔙 Backward     │
│  + Shift        │  (Value: 3)      │  (Value: 3)      │
│                 │  Defensive pose  │  Defensive pose  │
└─────────────────┴──────────────────┴──────────────────┘
```

---

## 🎬 ANIMATION INTENSITY GUIDE

### **Animation 0: Normal Sprint**
```
Left Hand:  ━━━━━━━ (Medium intensity)
Right Hand: ━━━━━━━ (Medium intensity)

Usage: Forward sprint (W key)
Arm Swing: Normal arc, balanced
Shoulder: Moderate rotation
```

### **Animation 1: Emphasized Sprint**
```
Hand: ━━━━━━━━━━━━━━━━ (HIGH INTENSITY!)

Usage: Active strafe hand (LEFT when A, RIGHT when D)
Arm Swing: WIDE arc, 30-40% more extension
Shoulder: STRONG rotation
Tips: 
  - Extend arm further forward
  - Bigger backswing
  - More aggressive motion
  - Hand leads the body
```

### **Animation 2: Subdued Sprint**
```
Hand: ━━━━ (LOW INTENSITY)

Usage: Passive hand during strafe
Arm Swing: TIGHT arc, 30-40% less extension
Shoulder: Minimal rotation
Tips:
  - Keep closer to body
  - Subtle movements
  - Controlled motion
  - Hand follows body
```

### **Animation 3: Backward Sprint**
```
Left Hand:  ━━━━━━━━━━━ (SPECIAL!)
Right Hand: ━━━━━━━━━━━ (SPECIAL!)

Usage: Backpedaling (S key)
Arm Swing: DIFFERENT from forward
Shoulder: Defensive rotation
Tips:
  - More protective posture
  - Hands may be lower
  - Cautious movement
  - Unique animation feel
```

---

## 🎯 KEY ANIMATION PRINCIPLES

### **Emphasized Animation (Value 1):**
- **Goal**: Show the player is pushing hard in that direction
- **Visual**: Arm EXTENDS and SWEEPS wide
- **Timing**: Slightly faster swing (more urgent)
- **Reference**: Think "swimming stroke" or "pushing through water"

### **Subdued Animation (Value 2):**
- **Goal**: Show the opposite hand supporting/balancing
- **Visual**: Arm stays CLOSE to body, minimal motion
- **Timing**: Follows emphasized hand (secondary motion)
- **Reference**: Think "tucked in" or "running with a football under one arm"

### **Backward Sprint (Value 3):**
- **Goal**: Player can't see behind - cautious movement
- **Visual**: More DEFENSIVE hand positioning
- **Timing**: Could be slightly slower (uncertainty)
- **Reference**: Think "retreat" or "tactical withdrawal"

---

## 📊 MOTION CURVES

### **Forward Sprint (Value 0):**
```
Hand Position (Forward/Back axis):

  Forward ↑
         │   ╱╲      ╱╲
         │  ╱  ╲    ╱  ╲
  Rest   │ ╱    ╲  ╱    ╲
         │╱      ╲╱      ╲
  Back   └──────────────────→ Time
         0s   0.25s   0.5s
         
  Standard sine wave motion
```

### **Emphasized Sprint (Value 1):**
```
Hand Position (Forward/Back axis):

  Forward ↑
         │    ╱╲       ╱╲
         │   ╱  ╲     ╱  ╲
  Rest   │  ╱    ╲   ╱    ╲
         │ ╱      ╲ ╱      ╲
  Back   └──────────────────→ Time
         0s   0.25s   0.5s
         
  AMPLIFIED sine wave - 40% more range
```

### **Subdued Sprint (Value 2):**
```
Hand Position (Forward/Back axis):

  Forward ↑
         │  ╱╲  ╱╲
  Rest   │ ╱  ╲╱  ╲
         │╱        ╲
  Back   └──────────────────→ Time
         0s   0.25s   0.5s
         
  DAMPENED sine wave - 40% less range
```

---

## 🎨 ANIMATOR SETUP CHECKLIST

### **Step 1: Add Parameter**
```
Unity Animator Window:
  Parameters Tab → Click "+"
  → Integer
  → Name: "sprintDirection"
  → Default: 0
```

### **Step 2: Create Sprint Blend Tree**
```
In Sprint State:
  Right-click Sprint motion → "Create New Blend Tree"
  
Blend Tree Settings:
  Blend Type: 1D
  Parameter: sprintDirection
  Compute Thresholds: OFF (manual)
```

### **Step 3: Add Animation Clips**
```
In Blend Tree:
  1. Add Motion → Sprint_Forward    (Threshold: 0)
  2. Add Motion → Sprint_Emphasized (Threshold: 1)
  3. Add Motion → Sprint_Subdued    (Threshold: 2)
  4. Add Motion → Sprint_Backward   (Threshold: 3)
```

### **Step 4: Set Blend Graph**
```
Blend Graph should look like:

0        1        2        3
|        |        |        |
Forward  Emph.    Subd.    Back

No blending between values!
Each is a discrete state.
```

---

## 🔧 ANIMATION NAMING CONVENTIONS

**Recommended file names:**

**Left Hand:**
- `LeftHand_Sprint_Forward.anim`
- `LeftHand_Sprint_Emphasized.anim`
- `LeftHand_Sprint_Subdued.anim`
- `LeftHand_Sprint_Backward.anim`

**Right Hand:**
- `RightHand_Sprint_Forward.anim`
- `RightHand_Sprint_Emphasized.anim`
- `RightHand_Sprint_Subdued.anim`
- `RightHand_Sprint_Backward.anim`

**OR** (if mirrored):
- `Hand_Sprint_Forward.anim` (same for both)
- `Hand_Sprint_Emphasized.anim` (mirrored)
- `Hand_Sprint_Subdued.anim` (mirrored)
- `Hand_Sprint_Backward.anim` (same for both)

---

## 💡 ANIMATION TIPS

### **For Emphasized Animation:**
1. **Start with normal sprint**
2. **Scale up forward/backward keyframes by 1.4x**
3. **Add 5-10° more shoulder rotation**
4. **Slightly increase animation speed (1.1x)**
5. **Offset hand position outward slightly (away from body)**

### **For Subdued Animation:**
1. **Start with normal sprint**
2. **Scale down forward/backward keyframes by 0.6x**
3. **Reduce shoulder rotation by half**
4. **Keep animation speed same**
5. **Offset hand position inward slightly (toward body)**

### **For Backward Animation:**
1. **Mirror forward sprint? NO! Make unique!**
2. **Lower hand positions (more protective)**
3. **Less vertical bounce (more stability)**
4. **Arms stay more forward (ready to catch fall)**
5. **Reference real backpedaling footage**

---

## 🎬 TESTING IN UNITY

### **Quick Test Setup:**

1. Enter Play Mode
2. Enable debug logs: `IndividualLayeredHandController.enableDebugLogs = true`
3. Watch Console for logs like:
   ```
   [LeftHand_Model] 🏃 LEFT hand sprint: StrafeLeft -> animation direction: 1
   ```
4. Open Animator window during play
5. Watch `sprintDirection` parameter change (0-3)

### **What to Look For:**
- ✅ Smooth transitions between states
- ✅ Left hand emphasizes when pressing A+Shift
- ✅ Right hand emphasizes when pressing D+Shift
- ✅ Both hands backward when pressing S+Shift
- ✅ No jittering between states
- ✅ Continuity preserved when jumping

---

## 📚 REFERENCE MATERIALS

### **Real-World References:**
- **Normal Sprint**: Marathon runners, track athletes
- **Emphasized Sprint**: Basketball players driving left/right
- **Subdued Sprint**: Holding something while running to one side
- **Backward Sprint**: Defensive players backpedaling in football/soccer

### **Game References:**
- **The Last of Us Part II**: Excellent strafe animation variety
- **Apex Legends**: Strong directional movement feel
- **Titanfall 2**: Dynamic hand animations during movement
- **Call of Duty MW**: Tactical sprint with hand emphasis

---

## ⚡ QUICK START (3 Minutes)

### **Absolute Minimum Implementation:**

**If you only have time for 2 animations:**

1. **Normal Sprint (0)**: Use for 0, 1, 2 (all forward/strafe)
2. **Backward Sprint (3)**: Use for backward only

**Blend Tree:**
```
0: Normal
1: Normal (same)
2: Normal (same)
3: Backward
```

This gives you **backward sprint** (the most important) immediately!

**Later, you can:**
- Replace animation 1 with real Emphasized
- Replace animation 2 with real Subdued
- Polish and refine

---

## ✅ FINAL CHECKLIST

Animation Artist Tasks:
- [ ] Create/import base sprint animation
- [ ] Create emphasized variation (wider swing)
- [ ] Create subdued variation (tighter swing)
- [ ] Create backward sprint animation
- [ ] Add `sprintDirection` parameter to animator
- [ ] Set up blend tree with 4 motions
- [ ] Set manual thresholds (0, 1, 2, 3)
- [ ] Test in Play Mode
- [ ] Verify smooth transitions
- [ ] Polish timing and easing

**Estimated Time**: 2-4 hours for full implementation

---

**Need Help?** Check `AAA_DIRECTIONAL_SPRINT_ANIMATIONS.md` for full technical details!
