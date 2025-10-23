# 🚀 Crouch Configuration System - Quick Start

## ⚡ 30-Second Setup

### 1. Create Config Asset
```
Right-click in Project → Create > Game > Crouch Configuration
```

### 2. Configure Settings
```
Select asset → Edit in Inspector
Clean, organized settings with logical groups!
```

### 3. Assign to Player
```
Select Player → CleanAAACrouch component
Drag config into "Config" field
```

### 4. Done! ✅
```
Your 80+ settings are now simplified to 1 config asset!
```

---

## 📊 What Changed

**Before:** 80+ inspector fields, duplicates, chaos  
**After:** 1 config asset with ~40 organized settings

**Benefits:**
- ✅ Clean inspector
- ✅ Shareable configs
- ✅ Runtime switching
- ✅ Create presets
- ✅ Version control friendly
- ✅ 100% backward compatible

---

## 🎯 Key Settings to Tweak

### Make Sliding Faster
```
slideGravityAccel: 980 → 1200
slideFrictionSlope: 2 → 1.5
momentumPreservation: 0.85 → 0.90
```

### Better Slide Control
```
slideSteerAcceleration: 350 → 500
steerDriftLerp: 0.85 → 0.70
```

### Slide Further on Flat Ground
```
slideFrictionFlat: 45 → 30
momentumPreservation: 0.85 → 0.92
```

### Stronger Dive
```
diveForwardForce: 720 → 900
diveUpwardForce: 240 → 320
```

---

## 🎮 Runtime Config Switching

```csharp
CleanAAACrouch crouch = player.GetComponent<CleanAAACrouch>();

// Switch to ice physics
crouch.Config = icePhysicsConfig;

// Switch to mountain physics
crouch.Config = mountainConfig;

// Back to default
crouch.Config = defaultConfig;
```

**Use Cases:**
- Different levels/zones
- Weather effects (ice, rain)
- Power-ups that change movement
- Game mode variants

---

## 📋 Complete Settings Reference

### Basic Settings (5)
- `standingHeight`, `crouchedHeight`
- `standingCameraY`, `crouchedCameraY`
- `transitionSpeed`

### Slide Physics (10)
- `slideMinStartSpeed`, `slideGravityAccel`
- `slideFrictionFlat`, `slideFrictionSlope`
- `slideSteerAcceleration`, `slideMaxSpeed`
- `momentumPreservation`, `stickToGroundVelocity`
- `uphillFrictionMultiplier`

### Tactical Dive (5)
- `diveForwardForce`, `diveUpwardForce`
- `diveProneDuration`, `diveMinSprintSpeed`
- `diveSlideFriction`

### Visual Effects (3)
- `slideFOVKick`, `slideFOVAdd`
- `slideFOVLerpSpeed`

### Curve Tuning (3)
- `steerAccelBySpeedCurve`
- `frictionBySpeedCurve`
- `steerDriftLerp`

### Advanced Physics (14)
- `slideAutoStandSpeed`, `slideMinimumDuration`
- `slideGroundCoyoteTime`, `slidingGravity`
- `slidingTerminalDownVelocity`, `minDownYWhileSliding`
- `slideGroundCheckDistance`, `landingSlopeAngleForAutoSlide`
- `rampMinSpeed`, `rampExtraUpBoost`
- And 4 more hidden constants

### Behavior Toggles (9)
- `holdToCrouch`, `enableSlide`, `enableTacticalDive`
- `rampLaunchEnabled`, `autoSlideOnLandingWhileCrouched`
- `autoResumeSlideFromMomentum`, `slideAudioEnabled`
- `diveAudioEnabled`, `slideParticlesEnabled`

### Debug Options (2)
- `showDebugVisualization`
- `verboseDebugLogging`

---

## 🎨 Example Preset Configs

### 1. Fast Arcade
```
Name: CrouchConfig_FastArcade

slideMinStartSpeed: 30
slideGravityAccel: 1200
slideFrictionFlat: 35
slideFrictionSlope: 1.5
slideSteerAcceleration: 450
slideMaxSpeed: 2000
momentumPreservation: 0.90

diveForwardForce: 900
diveUpwardForce: 300
diveProneDuration: 0.6
```

### 2. Realistic Tactical
```
Name: CrouchConfig_Realistic

slideMinStartSpeed: 50
slideGravityAccel: 980
slideFrictionFlat: 60
slideFrictionSlope: 3
slideSteerAcceleration: 250
slideMaxSpeed: 1200
momentumPreservation: 0.75

diveForwardForce: 600
diveUpwardForce: 180
diveProneDuration: 1.0
```

### 3. Slippery Ice
```
Name: CrouchConfig_Ice

slideMinStartSpeed: 25
slideGravityAccel: 1100
slideFrictionFlat: 20
slideFrictionSlope: 0.8
slideSteerAcceleration: 200
slideMaxSpeed: 1800
momentumPreservation: 0.95

diveForwardForce: 720
diveUpwardForce: 240
diveProneDuration: 0.8
```

### 4. Steep Mountain
```
Name: CrouchConfig_Mountain

slideMinStartSpeed: 35
slideGravityAccel: 1300
slideFrictionFlat: 45
slideFrictionSlope: 2.5
slideSteerAcceleration: 350
slideMaxSpeed: 2500
momentumPreservation: 0.80

minDownYWhileSliding: 35
landingSlopeAngleForAutoSlide: 20
```

---

## 💡 Pro Tips

### Tip 1: Lock Inspector for Quick Testing
1. Lock CleanAAACrouch Inspector (🔒 icon)
2. Select different config assets
3. See changes without losing component selection

### Tip 2: Create Test Configs
Make 3-4 variants for quick A/B testing:
- Default, Fast, Realistic, Slippery
- Swap in Inspector during play mode
- Find your perfect feel!

### Tip 3: Use Debug Visualization
Enable in config to see:
- 🔵 **Blue arrows** = Current slide velocity
- 🟢 **Green arrows** = Downhill direction
- 🔴 **Red indicators** = Uphill movement

### Tip 4: Backup Your Config
Before experimenting:
1. Duplicate your config asset
2. Name it `CrouchConfig_Backup`
3. Experiment freely!

---

## 🐛 Common Issues

**Issue:** Config changes don't apply  
**Fix:** Make sure config is assigned in CleanAAACrouch's Config field

**Issue:** Want to use old inspector settings  
**Fix:** Leave Config field empty (backward compatible)

**Issue:** Can't find config asset  
**Fix:** Search Project for `t:CrouchConfig`

**Issue:** Runtime changes don't work  
**Fix:** Use property: `crouch.Config = newConfig;`

---

## 📚 Full Documentation

- **CROUCH_CONFIG_PRESETS.md** - All presets and detailed settings
- **CROUCH_SYSTEM_SIMPLIFIED.md** - Complete migration guide
- **CrouchConfig.cs** - ScriptableObject source code
- **CleanAAACrouch.cs** - Updated with config system

---

## ✅ Final Checklist

- [x] ✅ **Created** CrouchConfig.cs ScriptableObject
- [x] ✅ **Updated** CleanAAACrouch.cs with LoadConfiguration()
- [x] ✅ **Backward compatible** - no breaking changes
- [x] ✅ **Compilation fixed** - all errors resolved
- [ ] 🎯 **Create your config asset** (30 seconds!)
- [ ] 🎯 **Assign to player** and test
- [ ] 🎯 **Create preset variants** for different styles
- [ ] 🎉 **Enjoy simplified configuration!**

---

**That's it! You've gone from 80+ chaotic settings to 1 clean config asset.** 🚀
