# 🔥 Forge Humming - Quick Setup Guide

## 🚀 3-Step Setup

### Step 1: Configure SoundEvents (30 seconds)
1. Open `SoundEvents` asset in Unity
2. Find **"► COLLECTIBLES: Forge"** section
3. Assign your humming audio clip to `forgeHumming`
4. Set `loop = TRUE` ✅
5. Set `volume = 0.6-1.0`

### Step 2: Add Component (10 seconds)
1. Select `ForgeManager` GameObject
2. Add Component → `ForgeSoundManager`
3. Done! (It auto-configures)

### Step 3: Test (5 seconds)
- Play the scene
- Walk near the forge
- Should hear humming from 1500 units away!

## 🎛️ Quick Settings Reference

### Default Settings (Already Configured)
```
Humming Volume: 0.6
Min Distance: 50 units (fade in starts)
Max Distance: 200 units (full volume)
Max Audible: 1500 units (cleanup)
Doppler: 0 (no pitch shift)
```

### Optional: Adjust in Inspector
- **Louder**: Increase `hummingVolume` to 0.8-1.0
- **Closer Range**: Decrease `maxAudibleDistance` to 500-1000
- **Softer Fade**: Increase `minHummingDistance` to 100-150

## 🐛 Quick Troubleshooting

| Problem | Solution |
|---------|----------|
| No sound | Check SoundEvents has clip assigned |
| Too quiet | Increase `hummingVolume` in Inspector |
| Wrong range | Adjust `maxAudibleDistance` |
| Not looping | Set `forgeHumming.loop = TRUE` in SoundEvents |

## 🎮 Context Menu Commands
Right-click `ForgeSoundManager` component:
- 🎵 **Start Humming NOW** - Manual start
- 🛑 **Stop Humming NOW** - Manual stop
- 🔍 **Check Audio Status** - Debug info

## ✅ That's It!
The forge will automatically hum when you're within 1500 units. No code changes needed!
