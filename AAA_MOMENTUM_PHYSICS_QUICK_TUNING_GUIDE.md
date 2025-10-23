# 🎮 MOMENTUM PHYSICS QUICK TUNING GUIDE
## Get the Perfect Feel in Minutes

---

## 🎯 INSPECTOR PARAMETERS (In Order of Importance)

### **1. Enable Momentum Physics** ⭐⭐⭐⭐⭐
```
Location: 🎪 MOMENTUM PHYSICS SYSTEM
Default: TRUE
```
**What it does:** Master toggle for momentum system
- TRUE = Skate game physics (flick and spin)
- FALSE = Direct control (old system)

**Tune this if:** You want to compare old vs new

---

### **2. Angular Acceleration** ⭐⭐⭐⭐⭐
```
Location: 🎪 MOMENTUM PHYSICS SYSTEM
Default: 12.0
Range: 8.0 - 20.0
```
**What it does:** How fast velocity builds from input
- **Lower (8-10):** Slower buildup, smoother, cinematic
- **Higher (15-20):** Fast response, snappy, competitive

**Tune this if:** Flicks feel too slow or too twitchy

**Pro Tip:** Start at 12, adjust by ±2 until it feels right

---

### **3. Angular Drag** ⭐⭐⭐⭐⭐
```
Location: 🎪 MOMENTUM PHYSICS SYSTEM
Default: 4.0
Range: 2.0 - 8.0
```
**What it does:** How fast momentum decays
- **Lower (2-3):** Long spin duration, floaty
- **Higher (6-8):** Short spin duration, tight control

**Tune this if:** Spins last too long or stop too quick

**Pro Tip:** Balance with acceleration (high accel = high drag)

---

### **4. Flick Burst Multiplier** ⭐⭐⭐⭐
```
Location: 🎪 MOMENTUM PHYSICS SYSTEM
Default: 2.8
Range: 1.5 - 4.0
```
**What it does:** Initial power boost on new flick
- **Lower (1.5-2.0):** Gentle, smooth
- **Higher (3.0-4.0):** Explosive, impactful

**Tune this if:** Flicks don't feel satisfying enough

**Pro Tip:** 2.5-3.0 for Skate-style feel

---

### **5. Roll Strength** ⭐⭐⭐⭐
```
Location: 🎪 AERIAL TRICK CAMERA
Default: 0.35
Range: 0.0 - 1.0
```
**What it does:** How much diagonal input causes roll
- **0.0:** No roll (2-axis only)
- **0.2-0.4:** Subtle varial flips
- **0.6-1.0:** Aggressive varial flips

**Tune this if:** Varial flips are too subtle or too extreme

**Pro Tip:** 0.3-0.4 for realistic skate feel

---

### **6. Max Angular Velocity** ⭐⭐⭐
```
Location: 🎪 MOMENTUM PHYSICS SYSTEM
Default: 720.0 (deg/s)
Range: 360.0 - 1440.0
```
**What it does:** Maximum rotation speed cap
- **Lower (360-540):** Can't spin too fast, controlled
- **Higher (900-1440):** Blazing fast spins possible

**Tune this if:** Want to limit or increase max spin speed

**Pro Tip:** 720 = 2 full rotations per second (good default)

---

### **7. Counter-Rotation Strength** ⭐⭐⭐
```
Location: 🎪 MOMENTUM PHYSICS SYSTEM
Default: 0.85
Range: 0.0 - 1.0
```
**What it does:** How easy to reverse rotation mid-air
- **Lower (0.3-0.5):** Harder to reverse, momentum dominates
- **Higher (0.8-1.0):** Easy to reverse, responsive

**Tune this if:** Too hard or too easy to change direction

**Pro Tip:** 0.8-0.9 for player-friendly feel

---

### **8. Flick Input Smoothing** ⭐⭐
```
Location: 🎪 MOMENTUM PHYSICS SYSTEM
Default: 0.03 (30ms)
Range: 0.01 - 0.1
```
**What it does:** Input filtering (noise reduction)
- **Lower (0.01-0.02):** Raw, instant (may jitter)
- **Higher (0.05-0.1):** Smoother (may feel sluggish)

**Tune this if:** Input feels jittery or laggy

**Pro Tip:** 0.03 is optimal for most mice

---

## 🔥 PRESET CONFIGURATIONS

### **COMPETITIVE PRESET** (Fast & Responsive)
```
enableMomentumPhysics = true
angularAcceleration = 16.0
angularDrag = 6.0
maxAngularVelocity = 900.0
flickBurstMultiplier = 3.2
flickBurstDuration = 0.10
flickInputSmoothing = 0.02
counterRotationStrength = 0.95
rollStrength = 0.40
```
**Feel:** Snappy, precise, skill-based

---

### **CINEMATIC PRESET** (Smooth & Floaty)
```
enableMomentumPhysics = true
angularAcceleration = 9.0
angularDrag = 2.5
maxAngularVelocity = 540.0
flickBurstMultiplier = 2.0
flickBurstDuration = 0.18
flickInputSmoothing = 0.05
counterRotationStrength = 0.65
rollStrength = 0.25
```
**Feel:** Graceful, cinematic, easy

---

### **SKATE GAME PRESET** (Tony Hawk/Skate Style) ⭐ RECOMMENDED
```
enableMomentumPhysics = true
angularAcceleration = 12.0
angularDrag = 4.0
maxAngularVelocity = 720.0
flickBurstMultiplier = 2.8
flickBurstDuration = 0.12
flickInputSmoothing = 0.03
counterRotationStrength = 0.85
rollStrength = 0.35
```
**Feel:** Authentic skate physics, balanced

---

### **CHAOS PRESET** (Maximum Freedom)
```
enableMomentumPhysics = true
angularAcceleration = 20.0
angularDrag = 2.0
maxAngularVelocity = 1440.0
flickBurstMultiplier = 4.0
flickBurstDuration = 0.15
flickInputSmoothing = 0.02
counterRotationStrength = 1.0
rollStrength = 0.50
```
**Feel:** Extreme, crazy, fun

---

## 🧪 TESTING WORKFLOW

### **Step 1: Load Default Preset**
1. Use "SKATE GAME PRESET" above
2. Enter play mode
3. Do a few tricks

### **Step 2: Tune Acceleration**
1. Flick mouse and observe response
2. Too slow? → Increase `angularAcceleration` by +2
3. Too fast? → Decrease `angularAcceleration` by -2
4. Repeat until perfect

### **Step 3: Tune Drag**
1. Flick mouse, release, observe spin duration
2. Spins too long? → Increase `angularDrag` by +1
3. Stops too quick? → Decrease `angularDrag` by -1
4. Repeat until perfect

### **Step 4: Tune Flick Burst**
1. Do sharp flick
2. Not impactful? → Increase `flickBurstMultiplier` by +0.3
3. Too explosive? → Decrease `flickBurstMultiplier` by -0.3
4. Repeat until satisfying

### **Step 5: Tune Roll (Varial Flips)**
1. Move mouse diagonally (up-right)
2. Not rolling enough? → Increase `rollStrength` by +0.1
3. Rolling too much? → Decrease `rollStrength` by -0.1
4. Repeat until natural

### **Step 6: Fine-Tune Counter-Rotation**
1. Spin right, then flick left mid-air
2. Can't reverse? → Increase `counterRotationStrength` by +0.1
3. Too easy? → Decrease `counterRotationStrength` by -0.1
4. Repeat until balanced

---

## 🎯 TROUBLESHOOTING

### **Problem: "Flicks don't feel impactful"**
**Solution:**
- Increase `flickBurstMultiplier` to 3.0-3.5
- Decrease `flickBurstDuration` to 0.08-0.10 (shorter, more punch)
- Increase `angularAcceleration` to 14-16

### **Problem: "Can't control the spin, too wild"**
**Solution:**
- Decrease `angularAcceleration` to 10 or lower
- Increase `angularDrag` to 5-6 (stops faster)
- Increase `counterRotationStrength` to 0.9-1.0 (easier to reverse)

### **Problem: "Momentum stops too quickly"**
**Solution:**
- Decrease `angularDrag` to 2.5-3.0
- Increase `maxAngularVelocity` to 900 or higher
- Check `flickInputSmoothing` isn't too high (should be 0.03 or lower)

### **Problem: "Input feels laggy or delayed"**
**Solution:**
- Decrease `flickInputSmoothing` to 0.01-0.02
- Make sure `enableMomentumPhysics` is TRUE
- Increase `angularAcceleration` to 14-16

### **Problem: "Diagonal input doesn't roll"**
**Solution:**
- Make sure `enableDiagonalRoll` is TRUE
- Increase `rollStrength` to 0.4-0.5
- Move mouse MORE diagonally (45° angle)

### **Problem: "Landing camera still twists diagonal"**
**Solution:**
- This should be FIXED now (yaw preservation)
- If still happening, check you're on latest code
- Debug log should show "Target Yaw: XX°" on landing

---

## 📊 PARAMETER RELATIONSHIP CHART

```
                    RESPONSIVENESS
                         ↑
                    High Accel (16+)
                         |
         TIGHT ←─────────┼─────────→ FLOATY
    High Drag (6+)       |       Low Drag (2-3)
                         |
                    Low Accel (8-)
                         ↓
                      SLUGGISH
```

**Balance Point:** Accel=12, Drag=4 (default)

---

## 🎮 FEEL TARGETS

### **"I want Skate 3 feel"**
```
angularAcceleration = 12-14
angularDrag = 3.5-4.5
flickBurstMultiplier = 2.5-3.0
rollStrength = 0.3-0.4
```

### **"I want Tony Hawk feel"**
```
angularAcceleration = 10-12
angularDrag = 3.0-4.0
flickBurstMultiplier = 2.0-2.5
rollStrength = 0.2-0.3
```

### **"I want Spider-Man trick feel"**
```
angularAcceleration = 14-16
angularDrag = 5.0-6.0
flickBurstMultiplier = 3.0-3.5
rollStrength = 0.4-0.5
```

### **"I want realistic skateboarding"**
```
angularAcceleration = 11-13
angularDrag = 4.0-5.0
flickBurstMultiplier = 2.5-2.8
rollStrength = 0.3-0.35
```

---

## 🔍 DEBUG TIPS

### **Watch Console for:**
```
"🎪 [FLICK] Burst activated! Magnitude: X.XX"
→ Check magnitude is > 0.3 for flicks

"🎪 [MOMENTUM] Input: X.XX | Velocity: XXX°/s"
→ Watch velocity build and decay

"🎯 [RECONCILIATION] Target Yaw: XX.X°"
→ Should match where you're looking
```

### **If momentum seems broken:**
1. Check `enableMomentumPhysics` is TRUE
2. Check console for FLICK messages
3. Try emergency reset (R key)
4. Check angularVelocity isn't zero (debug log)

---

## ⏱️ 5-MINUTE TUNING SESSION

**Quick path to perfection:**

1. **Minute 1:** Load SKATE GAME PRESET
2. **Minute 2:** Test flick response, adjust `angularAcceleration` ±2
3. **Minute 3:** Test spin duration, adjust `angularDrag` ±1
4. **Minute 4:** Test flick impact, adjust `flickBurstMultiplier` ±0.3
5. **Minute 5:** Test varial flips, adjust `rollStrength` ±0.1

**Done! You now have perfectly tuned momentum physics.**

---

## 🏆 FINAL CHECKLIST

- [ ] Loaded a preset (SKATE GAME recommended)
- [ ] Tested flick and release (does it keep spinning?)
- [ ] Tested diagonal input (does it roll?)
- [ ] Tested landing direction (camera stays where you look?)
- [ ] Tested counter-rotation (can reverse mid-air?)
- [ ] Adjusted 1-3 parameters to personal taste
- [ ] Feels better than any game you've played? ✅

---

## 🎪 YOU'RE READY!

**The GEM is tuned. Go make some highlight reels!** 🔥

**Remember:**
- Start with SKATE GAME PRESET
- Adjust ONE parameter at a time
- Trust the physics
- The default values are **very good**

**Your aerial tricks are now industry-leading.** 🚀

---

**Tuning Guide Version:** 1.0  
**For:** Momentum Physics System v2.0  
**Date:** October 17, 2025  

🎮✨🏆
