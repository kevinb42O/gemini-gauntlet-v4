# 🎯 WALL JUMP - QUICK REFERENCE GUIDE
**The ONLY place to edit wall jump: MovementConfig.asset**

---

## 📍 WHERE TO EDIT

```
Project Window → Assets/Data/MovementConfig.asset → Inspector
```

**DO NOT** edit wall jump values in `AAAMovementController` Inspector!  
(They're hidden now anyway - only the config shows)

---

## ⚡ FAST TUNING PRESETS

### 🏃 **Responsive Parkour** (Default)
```yaml
wallJumpUpForce: 1900
wallJumpOutForce: 1200
wallJumpCooldown: 0.12
wallJumpCameraDirectionBoost: 1800
wallJumpInputInfluence: 0.8
```

### 🦘 **High & Floaty**
```yaml
wallJumpUpForce: 2400      # +26%
wallJumpOutForce: 1000      # -17%
wallJumpCooldown: 0.15
wallJumpCameraDirectionBoost: 1600
wallJumpInputInfluence: 0.7
```

### 💨 **Fast & Chain-Heavy**
```yaml
wallJumpUpForce: 1700       # -11%
wallJumpOutForce: 1400      # +17%
wallJumpCooldown: 0.08      # -33%
wallJumpCameraDirectionBoost: 2000
wallJumpInputInfluence: 0.9
```

### 🎯 **Camera-Dominant (Titanfall)**
```yaml
wallJumpUpForce: 1900
wallJumpOutForce: 1200
wallJumpCooldown: 0.12
wallJumpCameraDirectionBoost: 2400  # +33%
wallJumpInputInfluence: 0.5         # -37%
wallJumpCameraBoostRequiresInput: false
```

### ⚙️ **Physics-Based (Realistic)**
```yaml
wallJumpUpForce: 1900
wallJumpOutForce: 1200
wallJumpCooldown: 0.15
wallJumpCameraDirectionBoost: 1400
wallJumpInputInfluence: 0.8
wallJumpMomentumPreservation: 0.3   # Keep 30% velocity
wallJumpFallSpeedBonus: 0.8         # More fall→speed
wallJumpCameraBoostRequiresInput: true
```

---

## 🔧 COMMON TWEAKS

| Problem | Solution |
|---------|----------|
| **Jumps too high** | `wallJumpUpForce: 1900 → 1600` |
| **Not pushing off wall** | `wallJumpOutForce: 1200 → 1600` |
| **Too slow between jumps** | `wallJumpCooldown: 0.12 → 0.08` |
| **Camera doesn't control** | `wallJumpCameraDirectionBoost: 1800 → 2400` |
| **Too slidey** | `wallJumpMomentumPreservation: 0 → 0.3` |
| **Not enough speed gain** | `wallJumpFallSpeedBonus: 0.6 → 0.8` |

---

## 🎮 PARAMETER CHEATSHEET

```
FORCES (Higher = Stronger)
├─ wallJumpUpForce:               1900  [Vertical height]
└─ wallJumpOutForce:              1200  [Horizontal push]

MOMENTUM (Fine-tune feel)
├─ wallJumpForwardBoost:           400  [Speed in move direction]
├─ wallJumpCameraDirectionBoost:  1800  [Jump where you look]
├─ wallJumpFallSpeedBonus:         0.6  [Fall→Horizontal (0-1)]
├─ wallJumpInputInfluence:         0.8  [WASD control (0-1)]
└─ wallJumpMomentumPreservation:   0.0  [Keep old velocity (0-1)]

TIMING (Lower = Faster)
├─ wallJumpCooldown:              0.12  [Seconds between jumps]
└─ wallJumpGracePeriod:           0.08  [Anti-re-detection]

LIMITS
└─ maxConsecutiveWallJumps:         99  [Max before ground]
```

---

## ✅ TEST CHECKLIST

After tuning:
- [ ] Jump feels responsive (no delay)
- [ ] Camera controls direction clearly
- [ ] Can chain jumps smoothly
- [ ] Doesn't feel too floaty/heavy
- [ ] Matches your game's speed

---

## 🆘 TROUBLESHOOTING

**Q: Changes not applying?**  
A: Check `AAAMovementController` has `config` assigned in Inspector

**Q: Wall jumps not working at all?**  
A: Set `enableWallJump = true` in MovementConfig

**Q: Want to see what's happening?**  
A: Set `showWallJumpDebug = true` in MovementConfig → Check Scene view

**Q: Values look wrong?**  
A: You're editing the RIGHT config asset? (Could have multiple configs)

---

**💡 TIP:** Create multiple MovementConfig assets for different characters/modes!

**Example:**
```
Assets/Data/MovementConfig_Player.asset     (Default)
Assets/Data/MovementConfig_FastMode.asset   (Speed demon)
Assets/Data/MovementConfig_Tutorial.asset   (Easy mode)
```

Switch configs at runtime by changing the `config` field on `AAAMovementController`!
