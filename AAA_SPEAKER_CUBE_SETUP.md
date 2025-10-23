# 🔊 Speaker Cube Music System - Complete Setup Guide

## 📋 Overview
Created a proximity-based music playback system for speaker cubes, following the exact same pattern as `ChestSoundManager` and `ForgeSoundManager`.

## ✅ What Was Done

### 1. **Added `speakerMusic` to SoundEvents.cs**
- **Location**: `Assets/scripts/Audio/FIXSOUNDSCRIPTS/SoundEvents.cs`
- **Added**: New `speakerMusic` SoundEvent field in the Collectibles section
- **Tooltip**: "Music that plays from speaker cubes (proximity-based, looped)"

### 2. **Created SpeakerMusicManager.cs**
- **Location**: `Assets/scripts/SpeakerMusicManager.cs`
- **Pattern**: Exact copy of `ChestSoundManager` structure
- **Features**:
  - ✅ Proximity-based music playback
  - ✅ No Doppler effect (dopplerLevel = 0f)
  - ✅ Linear rolloff for smooth distance falloff
  - ✅ Fallback AudioSource for reliability
  - ✅ Auto-starts music on Start()
  - ✅ Advanced sound system integration
  - ✅ Debug context menu commands
  - ✅ Longer fade out (1 second) for music

## 🎮 Quick Setup (3 Steps)

### Step 1: Configure SoundEvents
1. Open `SoundEvents` asset in Unity
2. Find **"► COLLECTIBLES: Speaker Cube"** section
3. Assign your music audio clip to `speakerMusic`
4. Configure settings:
   - **Volume**: 0.7 (default for music)
   - **Pitch**: 1.0
   - **Loop**: ✅ TRUE (must be checked!)
   - **Category**: SFX or Music

### Step 2: Create Speaker Cube GameObject
1. Create a new Cube in your scene (GameObject → 3D Object → Cube)
2. Rename it to "SpeakerCube" or "MusicSpeaker"
3. Position it where you want music to play
4. Add Component → `SpeakerMusicManager`

### Step 3: Configure Settings (Optional)
Default settings work great, but you can adjust:
- **Music Volume**: 0.7 (default)
- **Min Music Distance**: 5 (fade in starts)
- **Max Music Distance**: 20 (full volume)
- **Max Audible Distance**: 50 (cleanup distance)
- **Fallback Music Clip**: (Optional) Backup clip
- **Enable Debug Logs**: ✅ TRUE (for testing)

## 🎛️ Default Settings

### Audio Configuration (AAA Standard)
```csharp
spatialBlend = 1f;              // Full 3D
rolloffMode = Linear;           // Smooth falloff
dopplerLevel = 0f;              // NO DOPPLER (perfect for music)
spread = 0f;                    // Directional
priority = 128;                 // Medium priority
minDistance = 5f;               // Fade in starts
maxDistance = 50f;              // Audible range
fadeOut = 1f;                   // Smooth 1-second fade
```

### Distance Ranges
- **0-5 units**: Fade in begins
- **5-20 units**: Reaches full volume
- **20-50 units**: Gradual linear falloff
- **50+ units**: Auto-cleanup (music stops)

## 🎯 Key Features

### 1. **Music-Optimized**
- Longer fade out (1 second) for smooth transitions
- No Doppler effect to preserve music quality
- Volume optimized for background music (0.7 default)

### 2. **Dual Audio System**
- Primary: Advanced spatial audio with tracking
- Fallback: Direct AudioSource for reliability

### 3. **Auto-Start**
- Music begins automatically in `Start()`
- No manual triggering needed
- Perfect for ambient music zones

### 4. **Runtime Control**
```csharp
// Get the manager
SpeakerMusicManager speaker = GetComponent<SpeakerMusicManager>();

// Start music
speaker.StartSpeakerMusic();

// Stop music
speaker.StopSpeakerMusic();

// Adjust volume (0-1)
speaker.SetMusicVolume(0.5f);

// Check if playing
bool isPlaying = speaker.IsPlaying;
```

## 🎨 Use Cases

### Ambient Music Zones
Place speaker cubes around your level to create music zones:
```
- Town Square: Peaceful ambient music
- Combat Arena: Intense battle music
- Safe Room: Calm relaxing music
- Boss Room: Epic boss music
```

### Multiple Speakers
You can have multiple speaker cubes playing different music:
1. Create multiple cubes
2. Each gets its own `SpeakerMusicManager`
3. Assign different music clips in SoundEvents or fallback
4. Players hear the closest/loudest music

### Dynamic Music Control
```csharp
// Find all speakers in scene
SpeakerMusicManager[] speakers = FindObjectsOfType<SpeakerMusicManager>();

// Stop all music
foreach (var speaker in speakers)
{
    speaker.StopSpeakerMusic();
}

// Start specific speaker
speakers[0].StartSpeakerMusic();
```

## 🐛 Troubleshooting

### No Music Playing?
1. Check `SoundEvents` has `speakerMusic` clip assigned
2. Verify `speakerMusic.loop` is set to TRUE
3. Enable debug logs and check console
4. Use context menu "Check Audio Status"

### Music Too Quiet/Loud?
- Adjust `musicVolume` in Inspector (0-1 range)
- Adjust `speakerMusic.volume` in SoundEvents
- Final volume = `musicVolume * speakerMusic.volume`

### Wrong Distance Range?
- Adjust `minMusicDistance` (fade in start)
- Adjust `maxMusicDistance` (full volume point)
- Adjust `maxAudibleDistance` (cleanup distance)

### Music Sounds Weird When Moving?
- Verify `dopplerLevel = 0f` (should be default)
- Check `spatialBlend = 1f` for full 3D
- Ensure `rolloffMode = Linear`

## 🎮 Context Menu Commands
Right-click `SpeakerMusicManager` component:
- 🎵 **Start Music NOW** - Manual start
- 🛑 **Stop Music NOW** - Manual stop
- 🔍 **Check Audio Status** - Full diagnostic

## 📊 Comparison Table

| Feature | ChestSoundManager | ForgeSoundManager | SpeakerMusicManager |
|---------|------------------|-------------------|---------------------|
| Max Distance | 20 units | 1500 units | 50 units |
| Fade Out Time | 0.5s | 0.5s | 1.0s (music) |
| Auto-Start | No | Yes | Yes |
| Doppler Effect | None | None | None |
| Primary Use | Chest humming | Forge humming | Music playback |
| Volume Default | 0.6 | 0.6 | 0.7 |

## 🎨 Visual Setup Example

```
Scene Hierarchy:
├── Player
├── Environment
├── SpeakerCube_TownSquare
│   └── SpeakerMusicManager (peaceful_music.mp3)
├── SpeakerCube_Arena
│   └── SpeakerMusicManager (battle_music.mp3)
└── SpeakerCube_BossRoom
    └── SpeakerMusicManager (boss_music.mp3)
```

## ✨ Advanced Tips

### Tip 1: Music Zones with Triggers
Combine with trigger volumes for precise control:
```csharp
void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Player"))
    {
        speakerManager.StartSpeakerMusic();
    }
}
```

### Tip 2: Dynamic Volume Based on Events
```csharp
// Lower music during combat
speakerManager.SetMusicVolume(0.3f);

// Restore after combat
speakerManager.SetMusicVolume(0.7f);
```

### Tip 3: Crossfade Between Speakers
```csharp
// Fade out old speaker
oldSpeaker.SetMusicVolume(0f);
yield return new WaitForSeconds(1f);
oldSpeaker.StopSpeakerMusic();

// Fade in new speaker
newSpeaker.StartSpeakerMusic();
newSpeaker.SetMusicVolume(0.7f);
```

## ✅ Summary

The speaker cube music system is complete and production-ready:
- ✅ Integrated with `SoundEvents.cs`
- ✅ Uses dual audio system (advanced + fallback)
- ✅ Auto-starts with smooth distance falloff
- ✅ No Doppler effect for clean music playback
- ✅ 1-second fade out for smooth transitions
- ✅ Debug tools for testing

**Next Steps:**
1. Assign music clip in SoundEvents
2. Create cube GameObject
3. Add SpeakerMusicManager component
4. Test in-game!

🎵 **Enjoy your ambient music system!**
