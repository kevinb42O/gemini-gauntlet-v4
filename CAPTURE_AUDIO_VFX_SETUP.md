# Platform Capture - Audio & VFX Setup Guide

## ✅ What Was Added

### 🔊 Sound Events
Added two new sound events to `SoundEvents.cs`:

1. **`platformCaptureStart`** - Plays when capture begins (player enters radius)
2. **`platformCaptureComplete`** - Plays when capture finishes successfully

### ✨ Particle Effects
Added support for ground particle effect that activates on capture complete.

---

## 🎵 Sound Setup

### Step 1: Assign Audio Clips to SoundEvents

1. **Find your SoundEvents ScriptableObject:**
   - In Project window: Search for "SoundEvents"
   - Should be in `Assets/Audio/` or similar

2. **Assign your audio clips:**
   - Select the SoundEvents asset
   - Scroll to **"ENVIRONMENT: Platform Capture"** section
   - **Platform Capture Start**: Assign your capture start sound
   - **Platform Capture Complete**: Assign your capture complete sound

3. **Configure sound settings (optional):**
   ```
   Volume: 1.0 (adjust as needed)
   Pitch: 1.0
   Pitch Variation: 0.05 (adds variety)
   Category: SFX
   3D Settings: Use defaults or customize
   ```

### Step 2: Assign SoundEvents to PlatformCaptureSystem

1. **Select your platform's PlatformCaptureSystem component**
2. **In Inspector, find "Audio & VFX" section:**
   - **Sound Events**: Drag your SoundEvents ScriptableObject here

---

## ✨ Particle Effects Setup

### Step 1: Create Particle Effect

1. **Create a particle system:**
   - Right-click in Hierarchy → Effects → Particle System
   - Name it: `CaptureCompleteParticles`

2. **Position it:**
   - Place it at ground level near Central Tower
   - Or make it a child of Central Tower

3. **Configure particles:**
   - **Shape**: Circle or Sphere (ground burst effect)
   - **Emission**: Burst on start
   - **Color**: Green/cyan (capture success colors)
   - **Size**: Scale appropriately for your scene
   - **Lifetime**: 2-5 seconds

4. **Important: Start INACTIVE**
   - Uncheck the GameObject in Inspector
   - The system will activate it when capture completes

### Step 2: Assign to PlatformCaptureSystem

1. **Select your platform's PlatformCaptureSystem component**
2. **In Inspector, find "Audio & VFX" section:**
   - **Capture Complete Particles**: Drag your particle GameObject here

---

## 🎮 How It Works

### Capture Start:
```
Player enters Central Tower radius
    ↓
Sound plays: platformCaptureStart
    ↓
Visual feedback on Central Tower (material change)
```

### Capture Complete:
```
120 seconds elapsed
    ↓
Sound plays: platformCaptureComplete
    ↓
Particle effect activates (ground burst)
    ↓
All enemies destroyed
    ↓
Towers sink into ground
    ↓
Gems spawn from Central Tower
```

---

## 🔍 Debug Logs

You'll see these in Console:

**On Capture Start:**
```
[PlatformCaptureSystem] 🎯 CAPTURE STARTED - Player in radius!
[PlatformCaptureSystem] 🔊 Playing capture START sound
```

**On Capture Complete:**
```
[PlatformCaptureSystem] 🔊 Playing capture COMPLETE sound
[PlatformCaptureSystem] ✨ Activated capture complete particles!
```

---

## 📋 Quick Checklist

### Audio:
- [ ] Found SoundEvents ScriptableObject
- [ ] Assigned capture start audio clip
- [ ] Assigned capture complete audio clip
- [ ] Configured volume/pitch settings
- [ ] Assigned SoundEvents to PlatformCaptureSystem

### VFX:
- [ ] Created particle system
- [ ] Positioned at ground level
- [ ] Configured particle settings
- [ ] Set GameObject to INACTIVE
- [ ] Assigned to PlatformCaptureSystem

---

## 🎨 Particle Effect Suggestions

### Ground Burst Effect:
```
Shape: Circle (radius: 10-20)
Emission: Burst of 50-100 particles
Start Lifetime: 2-3 seconds
Start Speed: 5-10
Start Size: 0.5-2
Color: Gradient (cyan → green → white)
Gravity: -1 (particles rise slightly)
```

### Energy Wave Effect:
```
Shape: Sphere (radius: 5)
Emission: Burst of 200 particles
Start Lifetime: 1-2 seconds
Start Speed: 15-25 (expanding wave)
Start Size: 0.2-0.5
Color: Bright cyan/green
Size over Lifetime: Grows then fades
```

### Celebration Sparks:
```
Shape: Cone (angle: 45°, pointing up)
Emission: Burst of 100 particles
Start Lifetime: 1.5-2.5 seconds
Start Speed: 10-20
Start Size: 0.1-0.3
Color: Gold/yellow sparks
Gravity: 9.8 (falls like fireworks)
```

---

## 🎵 Sound Suggestions

### Capture Start Sound:
- **Style**: Activation/power-up sound
- **Duration**: 1-2 seconds
- **Feel**: Energizing, building tension
- **Examples**: Charging sound, activation beep, energy buildup

### Capture Complete Sound:
- **Style**: Victory/success sound
- **Duration**: 2-4 seconds
- **Feel**: Triumphant, rewarding
- **Examples**: Victory fanfare, success chime, power surge

---

## 🚀 Testing

1. **Start game**
2. **Enter platform** (UI appears)
3. **Enter Central Tower radius** (capture starts, sound plays)
4. **Wait 120 seconds** (or reduce duration for testing)
5. **Capture completes:**
   - Complete sound plays
   - Particles activate
   - Enemies destroyed
   - Towers sink
   - Gems spawn

---

## 💡 Pro Tips

### Multiple Platforms:
- Each platform can have different particle effects
- All platforms share the same SoundEvents asset
- Customize particles per platform for variety

### Sound Variation:
- Use pitch variation (0.05-0.1) for organic feel
- Adjust volume based on platform size
- Consider 3D distance settings for large platforms

### Particle Performance:
- Keep particle count reasonable (< 200 per effect)
- Use simple shapes for better performance
- Set max particles limit to prevent lag

---

## Summary

**Audio:**
1. Assign clips to SoundEvents asset
2. Assign SoundEvents to PlatformCaptureSystem
3. Done! Sounds play automatically

**VFX:**
1. Create particle system (start inactive)
2. Assign to PlatformCaptureSystem
3. Done! Activates on capture complete

**Zero code needed!** Everything is handled automatically. 🎊
