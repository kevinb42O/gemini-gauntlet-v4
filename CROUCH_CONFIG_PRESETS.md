# 🎮 CROUCH CONFIGURATION SYSTEM - QUICK REFERENCE

## 📋 Overview

The **CleanAAACrouch** system has been simplified from **80+ inspector settings** down to a **single ScriptableObject asset** with organized, logical groups.

---

## 🔧 Setup (One-Time)

### Step 1: Create Configuration Asset
1. Right-click in Project window
2. **Create > Game > Crouch Configuration**
3. Name it something like `CrouchConfig_Default`
4. Place in `Assets/Configs/` folder

### Step 2: Assign to CleanAAACrouch
1. Select your Player GameObject
2. Find **CleanAAACrouch** component
3. Drag your config asset into the `Config` slot
4. Done! ✅

---

## 🎯 RECOMMENDED PRESETS

### 🏃 **FAST ARCADE** (High-speed, responsive)
```
Slide Physics:
  slideMinStartSpeed: 30
  slideGravityAccel: 1200
  slideFrictionFlat: 35
  slideFrictionSlope: 1.5
  slideSteerAcceleration: 450
  slideMaxSpeed: 2000
  momentumPreservation: 0.90

Tactical Dive:
  diveForwardForce: 900
  diveUpwardForce: 300
  diveProneDuration: 0.6
```

### 🎯 **REALISTIC TACTICAL** (Grounded, deliberate)
```
Slide Physics:
  slideMinStartSpeed: 50
  slideGravityAccel: 980
  slideFrictionFlat: 60
  slideFrictionSlope: 3
  slideSteerAcceleration: 250
  slideMaxSpeed: 1200
  momentumPreservation: 0.75

Tactical Dive:
  diveForwardForce: 600
  diveUpwardForce: 180
  diveProneDuration: 1.0
```

### 🧊 **SLIPPERY SNOW** (Low friction, momentum-heavy)
```
Slide Physics:
  slideMinStartSpeed: 25
  slideGravityAccel: 1100
  slideFrictionFlat: 20
  slideFrictionSlope: 0.8
  slideSteerAcceleration: 200
  slideMaxSpeed: 1800
  momentumPreservation: 0.95
```

### ⛰️ **STEEP MOUNTAIN** (Steep slopes, controlled descent)
```
Slide Physics:
  slideMinStartSpeed: 35
  slideGravityAccel: 1300
  slideFrictionFlat: 45
  slideFrictionSlope: 2.5
  slideSteerAcceleration: 350
  slideMaxSpeed: 2500
  momentumPreservation: 0.80
  
Advanced:
  minDownYWhileSliding: 35
  landingSlopeAngleForAutoSlide: 20
```

---

## 🔑 KEY SETTINGS EXPLAINED

### **Slide Physics Group**
| Setting | What It Does | Increase = | Decrease = |
|---------|-------------|-----------|------------|
| `slideMinStartSpeed` | Speed needed to start sliding | Harder to start | Easier to start |
| `slideGravityAccel` | Downhill acceleration | Faster on slopes | Slower on slopes |
| `slideFrictionFlat` | Friction on flat ground | Stop faster | Slide further |
| `slideFrictionSlope` | Friction on slopes | Slower downhill | Faster downhill |
| `slideSteerAcceleration` | How fast you can turn | More responsive | More drift |
| `slideMaxSpeed` | Top speed cap | Go faster | Cap lower |
| `momentumPreservation` | Keep speed through turns | Keep speed | Lose speed |

### **Tactical Dive**
| Setting | What It Does |
|---------|-------------|
| `diveForwardForce` | How far you dive forward |
| `diveUpwardForce` | How high you arc |
| `diveProneDuration` | How long you're locked prone |
| `diveMinSprintSpeed` | Sprint speed required to dive |

### **Visual Effects**
| Setting | What It Does |
|---------|-------------|
| `slideFOVKick` | Zoom camera while sliding (true/false) |
| `slideFOVAdd` | Extra FOV added (15 = subtle, 30 = extreme) |
| `slideFOVLerpSpeed` | How fast FOV transitions |

### **Behavior Toggles**
| Setting | What It Does |
|---------|-------------|
| `holdToCrouch` | true = hold key, false = toggle |
| `enableSlide` | Enable sliding system |
| `enableTacticalDive` | Enable dive system |
| `autoSlideOnLandingWhileCrouched` | Auto-slide on slopes |
| `autoResumeSlideFromMomentum` | Resume slide after jumping |

---

## 🎛️ CURVE TUNING (Advanced)

### **Steering Acceleration Curve**
Controls how steering responsiveness changes with speed.
- **X-axis**: Current slide speed
- **Y-axis**: Steering multiplier

**Example Presets:**
```
Linear (Constant):
  (0, 1.0) → (200, 1.0)

Speed Boost (Faster at high speed):
  (0, 1.0) → (100, 1.0) → (200, 1.5)

Slow Control (Better control at low speed):
  (0, 1.5) → (100, 1.0) → (200, 0.8)
```

### **Friction Curve**
Controls how friction changes with speed.
- **X-axis**: Current slide speed
- **Y-axis**: Friction multiplier

**Example Presets:**
```
Constant Friction:
  (0, 1.0) → (200, 1.0)

High-Speed Reduction (Less friction when fast):
  (0, 1.0) → (50, 1.0) → (200, 0.5)

Low-Speed Grip (More friction when slow):
  (0, 1.5) → (100, 1.0) → (200, 0.8)
```

---

## ⚡ QUICK TWEAKS

### "Sliding feels too slow"
- ↑ `slideGravityAccel` (980 → 1200)
- ↓ `slideFrictionSlope` (2 → 1.5)
- ↑ `momentumPreservation` (0.85 → 0.90)

### "Can't control slide direction"
- ↑ `slideSteerAcceleration` (350 → 500)
- ↓ `steerDriftLerp` (0.85 → 0.70)

### "Sliding stops too quickly on flat"
- ↓ `slideFrictionFlat` (45 → 30)
- ↑ `momentumPreservation` (0.85 → 0.92)

### "Dive feels weak"
- ↑ `diveForwardForce` (720 → 900)
- ↑ `diveUpwardForce` (240 → 320)

### "Auto-slide too aggressive"
- ↑ `landingSlopeAngleForAutoSlide` (12 → 20)
- Set `autoSlideOnLandingWhileCrouched = false`

---

## 🚀 BENEFITS OF NEW SYSTEM

✅ **80+ settings → 1 config asset**
✅ **Create presets for different gameplay styles**
✅ **Switch configs at runtime (for different levels/game modes)**
✅ **Share configs across multiple characters**
✅ **Easy to version control (no scene changes)**
✅ **Inspector-friendly with categories and tooltips**
✅ **Validates values to prevent errors**
✅ **Advanced settings hidden by default**

---

## 🎮 RUNTIME CONFIG SWITCHING (Optional)

You can switch configs at runtime for dynamic gameplay:

```csharp
// Switch to ice physics for snow level
crouchComponent.Config = iceConfig;

// Switch to steep mountain for vertical sections
crouchComponent.Config = mountainConfig;

// Switch back to default
crouchComponent.Config = defaultConfig;
```

This allows:
- Different physics per level/zone
- Power-ups that change slide behavior
- Game modes with different movement rules
- Dynamic weather effects (ice/rain = slippery)

---

## 📊 COMPARISON

### Before (80+ Inspector Fields)
```
❌ Overwhelming inspector
❌ Hard to find settings
❌ Duplicate settings doing same thing
❌ Can't save presets easily
❌ Scene-dependent configuration
❌ Hard to share between characters
```

### After (1 Config Asset)
```
✅ Clean, organized groups
✅ Logical setting names
✅ Create unlimited presets
✅ Asset-based (version control friendly)
✅ Share across characters
✅ Runtime switching support
```

---

## 🔧 MIGRATION GUIDE

Your existing CleanAAACrouch will automatically use its old inspector values if no config is assigned. To migrate:

1. Create new CrouchConfig asset
2. Copy your current inspector values to the config
3. Assign config to CleanAAACrouch
4. Old inspector values will be ignored
5. Delete old inspector fields from code (optional)

No breaking changes! ✅
