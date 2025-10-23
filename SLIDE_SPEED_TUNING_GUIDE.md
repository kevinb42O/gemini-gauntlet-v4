# 🎯 SLIDE SPEED TUNING GUIDE

## 📊 NEW CONFIGURABLE PARAMETERS

### **Landing Momentum Damping** (Default: 0.65)
**Location:** `CrouchConfig` → Landing Momentum Control → `landingMomentumDamping`

**What it does:** Controls how much of your landing speed is preserved when starting a slide.

**Range:** 0.3 - 1.0
- **0.3** = Very slow (30% speed preserved) - Gentle, controlled slides
- **0.5** = Moderate (50% speed preserved) - Balanced feel
- **0.65** = Default (65% speed preserved) - Smooth but not overwhelming
- **0.8** = Fast (80% speed preserved) - High-speed action
- **1.0** = Full speed (100% preserved) - Maximum momentum, very fast!

**When to adjust:**
- **Too fast?** Lower the value (try 0.5 or 0.55)
- **Too slow?** Raise the value (try 0.75 or 0.8)
- **Just right?** Leave at 0.65!

---

### **Landing Max Preserved Speed** (Default: 400)
**Location:** `CrouchConfig` → Landing Momentum Control → `landingMaxPreservedSpeed`

**What it does:** Hard cap on maximum slide speed when landing with momentum.

**Typical values:**
- **300** = Conservative cap - prevents runaway speed
- **400** = Default - good balance
- **500** = High cap - allows very fast slides
- **600+** = Extreme - only for high-speed gameplay

**When to adjust:**
- **Slides feel out of control?** Lower the cap (try 300 or 350)
- **Want more speed potential?** Raise the cap (try 500)
- **Perfect?** Leave at 400!

---

## 🎮 TUNING SCENARIOS

### **Scenario 1: "Too Fast! I'm Flying!"**
**Symptoms:** Landing on slopes launches you at crazy speeds, hard to control

**Solution:**
```
landingMomentumDamping: 0.5 (was 0.65)
landingMaxPreservedSpeed: 350 (was 400)
```

**Result:** 50% speed preservation, capped at 350 units = controlled slides

---

### **Scenario 2: "Perfect Speed, But Occasional Spikes"**
**Symptoms:** Usually feels great, but sometimes you go way too fast

**Solution:**
```
landingMomentumDamping: 0.65 (keep default)
landingMaxPreservedSpeed: 350 (lower cap)
```

**Result:** Same feel, but prevents extreme speeds

---

### **Scenario 3: "Too Slow Now, Bring Back Some Speed"**
**Symptoms:** Slides feel sluggish, not enough momentum

**Solution:**
```
landingMomentumDamping: 0.75 (was 0.65)
landingMaxPreservedSpeed: 450 (was 400)
```

**Result:** 75% speed preservation, higher cap = more exciting slides

---

### **Scenario 4: "I Want Maximum Speed!"**
**Symptoms:** You love going fast, want full momentum preservation

**Solution:**
```
landingMomentumDamping: 0.9 (near maximum)
landingMaxPreservedSpeed: 600 (high cap)
```

**Result:** 90% speed preservation = ultra-fast slides!

---

## 🔧 HOW IT WORKS

### **Before (100% Preservation):**
```
Landing speed: 250 units
Slide start speed: 250 units (100%)
Result: VERY FAST!
```

### **After (65% Damping):**
```
Landing speed: 250 units
Damping: 250 × 0.65 = 162.5 units
Slide start speed: 162.5 units (65%)
Result: Smooth and controlled!
```

### **With Speed Cap:**
```
Landing speed: 500 units (very fast jump!)
Damping: 500 × 0.65 = 325 units
Cap check: 325 < 400 ✓ (within limit)
Slide start speed: 325 units
Result: Fast but not crazy!

Landing speed: 700 units (insane jump!)
Damping: 700 × 0.65 = 455 units
Cap check: 455 > 400 ✗ (exceeds limit)
Slide start speed: 400 units (capped)
Result: Fast but controlled!
```

---

## 📈 RECOMMENDED SETTINGS BY PLAYSTYLE

### **Casual / Exploration**
```
landingMomentumDamping: 0.5
landingMaxPreservedSpeed: 300
```
- Gentle, predictable slides
- Easy to control
- Good for beginners

### **Balanced / Default**
```
landingMomentumDamping: 0.65
landingMaxPreservedSpeed: 400
```
- Smooth momentum flow
- Not too fast, not too slow
- Recommended for most players

### **Action / Fast-Paced**
```
landingMomentumDamping: 0.8
landingMaxPreservedSpeed: 500
```
- High-speed slides
- Exciting momentum
- Requires good control

### **Speedrun / Extreme**
```
landingMomentumDamping: 0.95
landingMaxPreservedSpeed: 700
```
- Maximum momentum preservation
- Very fast slides
- For experienced players only

---

## 🎯 QUICK TUNING STEPS

1. **Open your CrouchConfig asset** in the Inspector
2. **Find "Landing Momentum Control"** section
3. **Adjust `landingMomentumDamping`** slider (0.3 - 1.0)
4. **Adjust `landingMaxPreservedSpeed`** value (200 - 800)
5. **Test in-game** - jump from ramp to ramp with slide buffered
6. **Iterate** until it feels perfect!

---

## 🔍 DEBUG LOGGING

Enable `verboseDebugLogging` in your CrouchConfig to see:

```
[SLIDE] MOMENTUM PRESERVED! Blend: 0.82, Original: 245.32, Damped: 159.46 (x0.65), Dir: (0.7, -0.5, 0.5)
```

**Reading the log:**
- **Blend:** How much original direction is preserved (0-1)
- **Original:** Your landing speed before damping
- **Damped:** Final slide speed after damping
- **(x0.65):** The damping multiplier applied
- **Dir:** Slide direction vector

---

## ✅ VALIDATION

**Test Case:** Jump from upward ramp to downward ramp
- **Landing speed:** ~250 units
- **With 0.65 damping:** Slide starts at ~162 units
- **Feel:** Smooth, controlled, buttery

**Expected behavior:**
- ✅ Momentum flows naturally
- ✅ Speed feels controlled
- ✅ No overwhelming acceleration
- ✅ Smooth transition from air to slide

---

## 🎊 PERFECT SETTINGS FOUND!

Once you find your perfect settings:
1. **Save your CrouchConfig asset**
2. **Test in various scenarios** (different ramps, speeds, angles)
3. **Share your settings** with your team if multiplayer
4. **Enjoy buttery-smooth slides!** 🧈

---

## 📝 NOTES

- **Low-speed landings (<100 units)** are NOT affected by damping
  - They use the old gentle acceleration system
  - Only high-speed landings (>100 units) use damping

- **Flat ground slides** are NOT affected by damping
  - Damping only applies to slope landings
  - Flat ground uses standard slide physics

- **Changes apply immediately** in Play mode
  - No need to restart
  - Adjust on-the-fly while testing!

---

**Current Status:** System is fully configurable and ready to tune! 🎯
