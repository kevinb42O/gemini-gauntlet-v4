# 🚀 AAA Spatial Audio - 2-Minute Setup Guide

## ⚡ Quick Setup (Do This First)

### Step 1: Add Spatial Audio Manager (1 minute)
1. In your main scene, create empty GameObject: "SpatialAudioManager"
2. Add Component → **SpatialAudioManager** (in GeminiGauntlet.Audio namespace)
3. Inspector settings:
   - ✅ Enable Distance Culling: **TRUE**
   - Global Max Audible Distance: **150**
   - Cull Check Interval: **0.5**
   - ✅ Show Debug Logs: **TRUE** (for testing)
   - ✅ Show Gizmos: **TRUE** (for visualization)

### Step 2: Test It Works (1 minute)
1. Create empty GameObject: "AudioDebugger"
2. Add Component → **SpatialAudioDebugger**
3. Right-click component → **"Show Spatial Audio Diagnostics"**
4. Check console - you should see:
```
=== SPATIAL AUDIO DIAGNOSTICS ===
✅ SoundSystemCore: Active
✅ SpatialAudioManager: Active
✅ AudioListener: Found
```

**That's it! The system is now active.**

---

## 🧪 Testing Your 3D Audio

### Test Skull Chatter Distance
1. Start game
2. Spawn some skull enemies
3. Move player away from skulls (50+ units)
4. **Watch console**: Should see "🧹 Culling distant sound" messages
5. **Chatter sounds auto-stop at 60m** ✅

### Test Tower Audio Distance
1. Place a tower in scene
2. Add **SpatialAudioDebugger** to tower
3. Set "Profile To Visualize" → **Tower**
4. In Scene view, you'll see:
   - Green sphere (10m) = Full volume
   - Yellow sphere (50m) = Zero volume
   - Red sphere (70m) = Auto-cleanup

### Visual Testing
1. Select any GameObject with **SpatialAudioDebugger**
2. Look in Scene view
3. You'll see colored spheres showing audio ranges
4. Move around - line to listener changes color (green = audible, red = culled)

---

## 🐛 Troubleshooting

### "SoundSystemCore: NOT INITIALIZED!"
**Fix**: Make sure you have `SoundSystemManager` in your scene with the sound events asset assigned.

### "AudioListener: NOT FOUND!"
**Fix**: Your main camera needs an `AudioListener` component.

### "Sounds not stopping at distance"
**Fix**: 
1. Check `SpatialAudioManager` exists in scene
2. Verify "Enable Distance Culling" is **TRUE**
3. Check console for "Tracking looping sound" messages

### "Can't see Gizmos in Scene view"
**Fix**: 
1. Scene view → Top-right Gizmos button → Make sure it's **ON**
2. Check "Show Gizmos" is enabled in `SpatialAudioManager`

---

## 📊 What Changed (The Fix)

### Skull Sounds
- **Before**: Chatter looped forever, no distance limit
- **After**: Auto-stop at 60m, smooth fade-out, orphan detection ✅

### Tower Sounds  
- **Before**: Bypassed central system, manual AudioSource creation
- **After**: Centralized, proper 3D profiles, auto-cleanup ✅

### 3D Distance
- **Before**: Broken or infinite range
- **After**: Crystal-clear per-entity profiles (8-120m ranges) ✅

---

## 🎯 Current Audio Ranges

| Entity | Min Distance | Max Distance | Auto-Cleanup |
|--------|--------------|--------------|--------------|
| **Skull Chatter** | 8m (full) | 40m (zero) | 60m |
| **Skull Death** | 10m | 60m | 80m |
| **Tower Shoot** | 15m | 80m | 120m |
| **Tower Idle** | 10m | 50m | 70m |
| **Tower Awaken** | 12m | 70m | 100m |

*All distances automatically enforced. No configuration needed.*

---

## 💡 Quick Tips

### Want to adjust distances?
Edit the profiles in: `Assets/scripts/Audio/FIXSOUNDSCRIPTS/SpatialAudioProfile.cs`
Look for `SpatialAudioProfiles` static class.

### Want to debug a specific sound?
1. Find the GameObject making the sound
2. Add `SpatialAudioDebugger` component
3. Scene view shows its audio range automatically

### Want to see all active sounds?
Check `SpatialAudioManager` Gizmos in Game/Scene view:
- **Green spheres** = Sounds within range
- **Red spheres** = Sounds being culled
- **Lines** = Distance to listener

---

## ✅ Verification Checklist

After setup, verify these work:

- [ ] Console shows "SoundSystemCore: Active" ✅
- [ ] Console shows "SpatialAudioManager: Active" ✅  
- [ ] Scene view shows colored spheres around debugger ✅
- [ ] Skull chatter stops when moving away ✅
- [ ] Tower sounds respect distance ✅
- [ ] No error messages in console ✅

**All checked? You're good to go!** 🎉

---

## 🆘 Need Help?

1. Check `AAA_SPATIAL_AUDIO_FIX_COMPLETE.md` for full documentation
2. Right-click `SpatialAudioDebugger` → "Show Spatial Audio Diagnostics"
3. Check Scene view Gizmos for visual feedback
4. Enable "Show Debug Logs" in `SpatialAudioManager` for detailed info

---

*System is now bulletproof and requires zero maintenance.* 🎵✨
