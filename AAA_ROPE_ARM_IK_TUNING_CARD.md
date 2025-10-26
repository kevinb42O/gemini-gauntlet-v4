# 🎯 ROPE ARM IK - TUNING CARD

## 🔧 FIXED ISSUES

### ✅ Issue #1: Animators Breaking After Roping
**Problem**: Animations stayed disabled after rope released  
**Fix**: Proper state tracking - animators re-enable IMMEDIATELY on rope release

### ✅ Issue #2: Wrong Rotation & Pivot
**Problem**: Arms rotating incorrectly, not respecting shoulder pivot  
**Fix**: Now rotates **L_Arm** and **R_Arm** bones (the actual shoulder pivots)

### ✅ Issue #3: Funky Rotations
**Problem**: Using wrong Transform (RobotArmII parent instead of arm bone)  
**Fix**: Targets the correct bones: `L_Arm` and `R_Arm` inside the model hierarchy

---

## 🎮 SETUP (Updated)

### 1. Auto-Find the Correct Bones
Right-click `RopeArmIK` component → **"Auto-Find Arm Bones"**

Should find:
- **L_Arm** (inside `LinkerHand/RobotArmII_L/L_Arm`)
- **R_Arm** (inside `LinkerHand/RobotArmII_R/R_Arm`)

These are the **shoulder pivot bones** - NOT the parent containers!

### 2. Tune In Play Mode
Start with these values:
- **IK Weight**: 0.85
- **Rotation Speed**: 15
- **Pitch Offset**: -45° (points arms up toward rope)
- **Yaw Offset**: 0°

---

## 🎨 TUNING GUIDE

### Pitch Offset (Up/Down)
Controls vertical aim direction:
- **-90°** = Arms point straight down (default pose)
- **-45°** = Arms point diagonally up (GOOD for ropes)
- **0°** = Arms point straight forward
- **+45°** = Arms point diagonally up-forward

**Adjust this first!** Start at -45° and tune ±10° increments in Play Mode.

### Yaw Offset (Left/Right)
Controls horizontal aim direction:
- **Negative** = Arms point inward (toward body center)
- **0°** = Arms point straight at anchor (DEFAULT)
- **Positive** = Arms point outward (away from body)

Usually **leave at 0°** unless arms look unnatural.

### IK Weight
Blends between animator and IK:
- **1.0** = Full IK (arms always point at rope, ignores animator)
- **0.85** = Mostly IK with slight animator blend (RECOMMENDED)
- **0.5** = Half and half (too animator-heavy, looks weird)

### Rotation Speed
How fast arms rotate to target:
- **5-10** = Slow, smooth, laggy
- **15** = Balanced (DEFAULT)
- **20-30** = Fast, snappy, responsive

---

## 🐛 TROUBLESHOOTING

### "Arms still doing falling animation"
**Check**: Is `L_Arm` and `R_Arm` assigned? (Not RobotArmII_L/R!)  
**Fix**: Re-run "Auto-Find Arm Bones"

### "Arms pointing wrong direction"
**Tune**: Adjust **Pitch Offset** in Play Mode  
- Too low? Increase (e.g., -45° → -30°)  
- Too high? Decrease (e.g., -45° → -60°)

### "Arms look robotic/stiff"
**Tune**: Lower **IK Weight** to 0.6-0.7  
**Tune**: Lower **Rotation Speed** to 10-12

### "Animations broken after roping"
**Check**: Component should auto-enable animators on rope release  
**Debug**: Check console for "[RopeArmIK] Re-enabling animators"

### "Arms rotating around wrong point"
**Check**: You're targeting **L_Arm** / **R_Arm** bones (NOT RobotArmII containers)  
**Fix**: Clear fields and re-run Auto-Find

---

## 📊 DEFAULT VALUES (RECOMMENDED START)

```
Left Arm Bone: L_Arm (auto-found)
Right Arm Bone: R_Arm (auto-found)

IK Weight: 0.85
Rotation Speed: 15
Pitch Offset: -45°  ← TUNE THIS FIRST
Yaw Offset: 0°
Show Debug Lines: True
```

---

## 🎯 QUICK TEST

1. **Enter Play Mode**
2. **Shoot rope** (Mouse3 + LMB)
3. **Arms should point toward anchor** (cyan debug line)
4. **Adjust Pitch Offset** slider while roping
5. **Release rope** (release LMB)
6. **Arms should return to falling animation** immediately
7. **Exit Play Mode** - settings saved!

---

## ✅ IT WORKS WHEN...

- [ ] Arms point toward rope anchors while roping
- [ ] Arms respect shoulder pivot (rotate around L_Arm/R_Arm)
- [ ] No funky rotations or spinning
- [ ] Animations resume IMMEDIATELY after rope release
- [ ] Debug lines show (cyan = left, magenta = right)
- [ ] Smooth transitions (no snapping)

---

**Now your arms should look awesome while rope swinging!** 🕷️
