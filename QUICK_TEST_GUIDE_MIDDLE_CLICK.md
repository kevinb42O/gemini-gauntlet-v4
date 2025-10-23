# ⚡ QUICK TEST GUIDE - MIDDLE CLICK TRICK JUMP

## 🎮 INSTANT TESTING (30 SECONDS)

### **Step 1: Enter Play Mode**
- Hit Play in Unity
- Find an open area

### **Step 2: Test Middle Click Jump**
```
1. MIDDLE CLICK (scroll wheel button)
   → Should jump instantly
   → Console: "🎮 [TRICK JUMP] Middle click detected"
   → Console: "🎪 [FREESTYLE] TRICK MODE ACTIVATED!"
```

### **Step 3: Test Mouse Rotation**
```
1. While in air, MOVE MOUSE DOWN
   → Camera starts backflip
   → See rotation counter in UI
```

### **Step 4: Test Scroll Nudges**
```
1. While flipping, SCROLL UP
   → Console: "🔥 [NUDGE] FORWARD nudge: 45.0°"
   → UI shows: "↑ NUDGE ↑"
   → Rotation increases by 45°

2. SCROLL UP AGAIN
   → Another 45° nudge
   → Total rotation increases
```

### **Step 5: Land**
```
1. Let gravity bring you down
   → Console: "✨ [FREESTYLE] CLEAN LANDING!" or "💥 [FREESTYLE] CRASH LANDING!"
   → Camera snaps back to reality
```

---

## 🔥 ADVANCED TEST SEQUENCE

### **The Half-Backflip Combat Test:**
```
1. MIDDLE CLICK → Jump
2. MOUSE DOWN → Start backflip
3. STOP MOUSE at 180° (upside down)
4. Try to SHOOT (should work!)
5. SCROLL UP once → 225°
6. SCROLL UP again → 270°
7. SCROLL UP again → 315°
8. SCROLL UP again → 360° (complete!)
9. LAND → Should be clean!
```

### **The Nudge Control Test:**
```
1. MIDDLE CLICK → Jump
2. MOUSE DOWN FAST → Fast backflip
3. SCROLL DOWN → Nudge backward (slow down)
4. SCROLL DOWN again → More backward nudge
5. SCROLL UP → Forward nudge
6. Fine-tune with MOUSE
7. LAND
```

### **The Style Test:**
```
1. MIDDLE CLICK → Jump high
2. CIRCULAR MOUSE MOTION → Cork screw
3. SCROLL UP/DOWN randomly → Adjust rotation
4. STOP MOUSE → Hold position
5. SCROLL to complete → Style points!
6. LAND
```

---

## 📊 WHAT TO LOOK FOR

### **✅ SUCCESS INDICATORS:**
- Middle click triggers jump instantly
- Freestyle activates automatically
- Scroll nudges apply immediately
- UI shows nudge arrows (↑ or ↓)
- Console logs show nudge amounts
- Can shoot while flipping
- Landing reconciliation works

### **❌ POTENTIAL ISSUES:**
- Middle click doesn't jump → Check `middleClickTrickJump` enabled
- No nudges → Check `enableScrollNudges` enabled
- Nudges too weak → Increase `scrollNudgeDegrees`
- Nudges too fast → Increase `nudgeCooldown`
- Can't control → Adjust `trickInputSensitivity`

---

## 🔧 QUICK TUNING

### **If Nudges Feel Too Weak:**
```
Inspector → AAACameraController
→ Scroll Nudge Degrees: 45 → 60
```

### **If Nudges Feel Too Strong:**
```
Inspector → AAACameraController
→ Scroll Nudge Degrees: 45 → 30
```

### **If Nudges Spam Too Fast:**
```
Inspector → AAACameraController
→ Nudge Cooldown: 0.08 → 0.12
```

### **If Rotation Too Slow:**
```
Inspector → AAACameraController
→ Max Trick Rotation Speed: 360 → 540
→ Trick Input Sensitivity: 3.5 → 4.5
```

---

## 🎯 CONSOLE LOG REFERENCE

### **Expected Logs:**
```
🎮 [TRICK JUMP] Middle click detected - Jump triggered + Freestyle queued!
🎪 [FREESTYLE] TRICK MODE ACTIVATED! Initial burst: 2.5x speed!
🔥 [NUDGE] FORWARD nudge: 45.0° | Total X: 45.0°
🔥 [NUDGE] FORWARD nudge: 45.0° | Total X: 90.0°
🔥 [NUDGE] BACKWARD nudge: -45.0° | Total X: 45.0°
✨ [FREESTYLE] CLEAN LANDING! Deviation: 12.3° - Smooth recovery
🎪 [FREESTYLE] LANDED - Total flips: X=1.0 Y=0.0 Z=0.0
```

---

## 🎮 CONTROL SUMMARY

| Input | Action |
|-------|--------|
| **Middle Click** | Jump + Engage Freestyle |
| **Mouse Up/Down** | Backflip / Frontflip |
| **Mouse Left/Right** | 360° Spins |
| **Scroll Up** | Nudge Forward (+45°) |
| **Scroll Down** | Nudge Backward (-45°) |
| **Spacebar** | Normal Jump (no tricks) |
| **LEFT ALT** | Legacy Freestyle (still works) |

---

## 💡 PRO TIPS

1. **Middle click is easier than spacebar** for trick jumps
2. **Scroll nudges are discrete** - each scroll = fixed amount
3. **Smart scaling helps** - nudges stronger when slow
4. **Can shoot while flipping** - tactical advantage!
5. **Stop mouse to hold position** - then nudge to adjust
6. **Combine mouse + scroll** - ultimate control

---

## 🚀 READY TO TEST!

**Just:**
1. Play
2. Middle click
3. Flip
4. Scroll
5. Land
6. Repeat forever

**It's that simple. It's that powerful. It's that revolutionary.**

**GO! 🔥**
