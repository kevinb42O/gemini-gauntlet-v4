# 🪝 ROPE SWING - JUST CAUSE MODE

**Status:** ✅ IMPLEMENTED  
**Date:** 2025-10-23  
**Mode:** Just Cause Style Grapple (Click to Shoot, Hold to Retract)

---

## 🎮 JUST CAUSE CONTROLS

### **Perfect Hybrid System:**

1. **CLICK Mouse4** → Shoot rope and attach
   - Single click to shoot
   - Rope attaches to surface
   - Natural pendulum swing begins

2. **HOLD Mouse4** → Pull yourself toward anchor
   - Active retraction while holding
   - Release button = natural swing resumes
   - Best of both worlds!

3. **JUMP** → Disconnect and keep momentum
   - Disconnects rope instantly
   - Preserves all momentum
   - Continue with aerial movement

---

## 🚀 HOW IT WORKS

### **The Just Cause Flow:**

```
1. Running on ground
   ↓
2. CLICK Mouse4 (aim at surface)
   ↓
3. Rope shoots and attaches
   ↓
4a. RELEASE button = Natural pendulum swing
    OR
4b. HOLD button = Pull toward anchor
   ↓
5. Build speed (swing or retract)
   ↓
6. Press JUMP to release
   ↓
7. Keep all momentum!
   ↓
8. Aerial tricks, wall jumps, etc.
```

---

## ⚙️ NEW FEATURES IMPLEMENTED

### **1. Hold-to-Retract System** ✅
**What it does:** Pulls player toward anchor ONLY when holding mouse button

**Parameters (in Inspector):**
- `ropeMouseButton` = **3** (Mouse4 button)
- `retractionForce` = **8000f** (how fast you get pulled)
- `targetRetractionDistance` = **300f** (stops pulling when this close)
- `allowNaturalSwing` = **true** (enables pendulum when not holding)

**How it works:**
- **HOLD button:** Applies force toward anchor
- **RELEASE button:** Natural pendulum swing resumes
- Force scales with distance (farther = stronger pull)
- Best of both worlds: control AND physics!

---

### **2. Click-to-Shoot, Hold-to-Retract** ✅
**What changed:**
- **BEFORE:** Press G to toggle rope on/off
- **AFTER:** Mouse4 click to shoot, hold to retract

**Why this is perfect:**
- Click = attach and swing naturally (pendulum physics)
- Hold = pull yourself toward target (active control)
- Release = resume natural swing (best of both!)
- Exactly like Just Cause grappling hook

---

### **3. Jump-to-Release** ✅
**What it does:** Pressing jump while swinging releases rope

**Benefits:**
- Natural transition from swing → aerial movement
- Immediate momentum preservation
- Can chain into wall jumps, aerial tricks, etc.
- Feels responsive and skill-based

---

## 🎯 USAGE EXAMPLES

### **Example 1: Natural Swing**
```
1. See distant building
2. CLICK Mouse4 at top
3. Rope attaches
4. DON'T HOLD - let physics swing you
5. Build pendulum momentum
6. JUMP at peak to release
7. Natural arc movement!
```

### **Example 2: Active Pull (Just Cause Style)**
```
1. See tall cliff
2. CLICK Mouse4 at top
3. Immediately HOLD Mouse4
4. Rope pulls you straight up!
5. JUMP near top to release
6. Launch over the ledge!
```

### **Example 3: Hybrid Movement**
```
1. CLICK at distant point
2. Natural swing starts
3. HOLD button at bottom of arc
4. Pull adds speed to swing!
5. RELEASE button at peak
6. Natural momentum continues
7. JUMP to disconnect
8. Perfect trajectory!
```

### **Example 4: Combat Mobility**
```
1. CLICK at wall
2. HOLD to pull toward it (dodge!)
3. RELEASE near wall
4. Natural swing around corner
5. HOLD again for speed boost
6. JUMP to aerial attack
7. Chain into next grapple
```

---

## 🔧 TUNING GUIDE

### **If retraction (when holding) feels too slow:**
Increase `retractionForce` (try 10000-12000)

### **If retraction feels too fast/aggressive:**
Decrease `retractionForce` (try 5000-6000)

### **If you want to get closer before stopping:**
Decrease `targetRetractionDistance` (try 200-250)

### **If natural swing feels too weak:**
Decrease `swingAirDrag` (less resistance)
Increase `swingGravityMultiplier` (more pendulum energy)

### **If you want stronger pull (zipline style):**
Increase `retractionForce` to 15000+
Decrease `targetRetractionDistance` to 150
You'll shoot straight to the target!

### **If you want more swing, less pull:**
Decrease `retractionForce` to 4000-5000
This makes holding feel like "assistance" rather than "pulling"

---

## 🎨 VISUAL FEEDBACK

The rope still shows:
- ✅ Tension-based appearance (taut when retracting)
- ✅ Color gradient (cyan → magenta based on force)
- ✅ Dynamic width (thicker under tension)
- ✅ Realistic physics curve

---

## 💡 PRO TIPS

1. **Aim High:** Shoot at surfaces above you for maximum momentum gain
2. **Release Early:** Jump before reaching target for horizontal speed boost
3. **Release Late:** Jump near target for vertical launch
4. **Chain Grapples:** Immediately shoot new rope after releasing for continuous movement
5. **Combine with Boost:** Use boost while retracting for even faster movement
6. **Wall Jump Combo:** Release near wall → wall jump for extreme height

---

## 🐛 TROUBLESHOOTING

### **"Rope doesn't pull me!"**
- Check `enableAutoRetraction` is **true** in Inspector
- Check `retractionForce` is set (try 5000)
- Make sure distance to anchor > `targetRetractionDistance`

### **"Can't release with jump!"**
- Make sure jump button is properly mapped in Controls
- Check that you're actually swinging (rope attached)
- Look for debug log: "[ROPE SWING] 🕷️ SPIDER-MAN RELEASE!"

### **"Retraction is too weak!"**
- Your character might be heavy (320 units!)
- Increase `retractionForce` to 8000-10000
- Or decrease `swingAirDrag` for less resistance

---

## ✅ IMPLEMENTATION CHECKLIST

- [x] Auto-retraction force added to physics
- [x] Retraction scales with distance
- [x] Single-press G activation
- [x] Jump-to-release system
- [x] Removed G-to-release
- [x] Momentum preservation on release
- [x] Inspector parameters for tuning
- [x] Physics phases renumbered (4-9)
- [x] Zero compilation errors

---

## 🎮 FINAL CONTROL SCHEME

| Action | Button | Effect |
|--------|--------|--------|
| **Shoot Rope** | CLICK Mouse4 | Attach to surface (natural swing) |
| **Retract** | HOLD Mouse4 | Pull toward anchor (active control) |
| **Stop Retracting** | RELEASE Mouse4 | Resume natural swing physics |
| **Disconnect** | JUMP | Release rope, keep momentum |
| **Move While Swinging** | WASD | Air control (limited) |
| **Pump Swing** | W (at bottom) | Inject energy for more speed |
| **Boost Pump** | Shift + W | Massive energy boost |

---

---

## 🌟 THE PERFECT SYSTEM

**This is the BEST of both worlds:**

✅ **Natural Physics** - Click and let gravity do the work (pendulum swing)  
✅ **Active Control** - Hold to pull yourself toward target (grappling)  
✅ **Hybrid Mastery** - Combine both for advanced movement  
✅ **Just Cause Feel** - Exactly like the famous grappling hook  

**Enjoy your perfect grappling hook system! 🪝🚀**

---

**END OF DOCUMENT**
