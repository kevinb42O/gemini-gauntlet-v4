# 🔫 3D Shooting Sounds - Quick Setup Guide

## ⚡ 30-Second Setup

### Step 1: Assign AudioSources in Inspector
1. Select **PlayerShooterOrchestrator** GameObject
2. Find **3D Audio** section
3. Assign:
   - **Primary Hand Audio Source** → LEFT hand AudioSource
   - **Secondary Hand Audio Source** → RIGHT hand AudioSource

### Step 2: Test
- Fire LEFT hand → sound from LEFT ✅
- Fire RIGHT hand → sound from RIGHT ✅
- Move hands → sounds follow ✅

**That's it!** 🎉

---

## 📋 What You Need

### AudioSources (Already Exist)
These are the SAME AudioSources used by `PlayerOverheatManager`:
- LEFT hand AudioSource (primary)
- RIGHT hand AudioSource (secondary)

### Inspector Fields (Already Added)
```
PlayerShooterOrchestrator
├── [Header: 3D Audio]
├── primaryHandAudioSource   ← Drag LEFT hand here
└── secondaryHandAudioSource ← Drag RIGHT hand here
```

---

## 🎯 How It Works

**Old Way (2D):**
```csharp
// Sound at fixed position - doesn't follow hand
GameSounds.PlayShotgunBlast(position, level, volume);
```

**New Way (3D):**
```csharp
// Sound follows hand in 3D space!
GameSounds.PlayShotgunBlastOnHand(handAudioSource, level, volume);
```

---

## ✅ Benefits

- ✅ Proper 3D directional audio
- ✅ Sounds follow hands as they move
- ✅ Left/right panning works correctly
- ✅ Distance attenuation works
- ✅ Same pattern as overheat sounds
- ✅ Optimized for potato PCs

---

## 🔊 Audio Settings

The AudioSources should have:
- **Spatial Blend:** 1.0 (full 3D)
- **Doppler Level:** 0 (no doppler effect)
- **Min Distance:** 1-5 units
- **Max Distance:** 50-100 units
- **Rolloff:** Logarithmic

*(These are likely already configured from overheat system)*

---

## 🐛 Troubleshooting

### No sound?
- Check AudioSources are assigned in Inspector
- Verify AudioSources have 3D settings configured
- Ensure sound clips are assigned in SoundEvents

### Sound not following hands?
- Verify AudioSources are on the hand GameObjects
- Check Spatial Blend is set to 1.0 (full 3D)

### Sound too quiet/loud?
- Adjust volume in HandLevelSO configs
- Check AudioSource volume settings

---

## 🎮 Testing Commands

1. **Fire left hand** (LMB) → Listen for sound from left
2. **Fire right hand** (RMB) → Listen for sound from right
3. **Move while firing** → Sound should follow hands
4. **Rapid fire** → Multiple sounds should overlap cleanly

---

*Pattern: Same as overheat sounds - proven and optimized*
*Performance: Potato-friendly 🥔*
