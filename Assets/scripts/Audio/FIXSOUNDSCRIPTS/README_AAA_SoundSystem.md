# AAA Sound System - Setup & Usage Guide

## 🎵 Overview
This is a complete, professional-grade sound system redesign that fixes all the major audio issues in your game:

### ✅ Issues Fixed:
- **Stretched/distorted sounds** - Proper pitch handling and time-scale compensation
- **Hearing distant 2D sounds** - Correct spatial audio categorization (2D vs 3D)
- **Memory leaks** - Pooled AudioSource system with automatic cleanup
- **Audio performance** - Sound prioritization and concurrent limit management
- **Inconsistent spatialization** - Standardized distance settings per sound category

---

## 🚀 Quick Setup (5 minutes)

### Step 1: Initialize the System
1. Create an empty GameObject in your main scene
2. Add the `SoundSystemBootstrap` component
3. Assign your AudioMixer to the "Main Audio Mixer" field
4. Set the mixer group names (Master, SFX, Music, Ambient, UI)

### Step 2: Create Sound Events Database
1. Right-click in Project → Create → Audio → Sound Events
2. Name it "GameSoundEvents"
3. Assign all your AudioClips to the appropriate categories
4. Assign this to the SoundSystemBootstrap

### Step 3: Replace Old Audio Calls
The system provides automatic compatibility, but for best results:

**Old Code:**
```csharp
AudioManager.Instance.PlaySound3DAtPoint(clip, position, volume);
```

**New Code:**
```csharp
SoundSystemCore.Instance.PlaySound3D(clip, position, SoundCategory.Combat, volume);
```

---

## 🎯 Sound Categories & Spatial Settings

| Category | Spatial | Min Distance | Max Distance | Priority | Use Case |
|----------|---------|--------------|--------------|----------|----------|
| **UI** | 2D | - | - | High | Menus, buttons, HUD |
| **Combat** | 3D | 5m | 80m | Medium | Weapons, hits, explosions |
| **Enemy** | 3D | 8m | 120m | Medium | Enemy sounds, AI chatter |
| **Environment** | 3D | 10m | 200m | Low | Ambient, background effects |
| **Movement** | 3D | 2m | 50m | High | Footsteps, jumps, player actions |
| **Music** | 2D | - | - | Critical | Background music, stingers |

---

## 🎮 Usage Examples

### Basic Sound Playback
```csharp
// 2D UI sound
SoundSystemCore.Instance.PlaySound2D(clickSound, SoundCategory.UI, 0.8f);

// 3D positional sound
SoundSystemCore.Instance.PlaySound3D(explosionSound, transform.position, SoundCategory.Combat, 1.0f);

// 3D sound that follows an object
SoundSystemCore.Instance.PlaySoundAttached(engineSound, transform, SoundCategory.Movement, 0.6f, 1.0f, true);
```

### Using Sound Events (Recommended)
```csharp
// Quick access to common sounds
SoundEventsManager.GameSounds.PlayPlayerJump(transform.position);
SoundEventsManager.GameSounds.PlaySkullKill(enemy.transform.position);
SoundEventsManager.GameSounds.PlayUIClick();

// Custom sound events
yourSoundEvent.Play3D(transform.position, 0.8f);
```

### Advanced Control with Sound Handles
```csharp
// Get a handle for advanced control
SoundHandle musicHandle = SoundSystemCore.Instance.PlaySound2D(musicClip, SoundCategory.Music, 0.5f);

// Control the sound
musicHandle.SetVolume(0.3f);
musicHandle.FadeOutAndStop(2.0f);

// Check if still playing
if (musicHandle.IsPlaying())
{
    Debug.Log("Music is still playing");
}
```

---

## 🔧 Configuration Options

### Sound System Settings
- **Max Concurrent Sounds**: 32 (total)
- **Pool Initial Size**: 16 sources
- **Priority Limits**: Critical (unlimited), High (8), Medium (16), Low (8)

### Per-Category Distance Settings
Adjust in `SoundSystemCore.InitializeCategorySettings()`:
```csharp
categorySettings[SoundCategory.Combat] = new SoundCategorySettings
{
    minDistance = 5f,        // Start rolloff at 5 meters
    maxDistance = 80f,       // Completely silent at 80 meters
    rolloffMode = AudioRolloffMode.Logarithmic,
    dopplerLevel = 0.5f
};
```

---

## 🐛 Troubleshooting

### "No sound playing"
1. Check if SoundSystemCore.Instance is not null
2. Verify AudioMixer groups are assigned correctly
3. Check if sound category limits are reached
4. Ensure AudioListener is present in scene

### "Sounds still stretching"
- Make sure you're using the new system, not the old AudioManager
- Verify pitchVariance values are reasonable (0.05f = 5% variation)
- Check that Time.timeScale is being handled correctly

### "Can't hear 3D sounds"
- Verify AudioListener position relative to sound source
- Check minDistance/maxDistance settings for the sound category
- Ensure spatialBlend is set to 1.0f for 3D sounds

### "Memory leaks"
- The new system handles cleanup automatically
- Old temporary AudioSource GameObjects should no longer be created
- Use the pooled system instead of creating new AudioSources

---

## 📊 Performance Monitoring

### Runtime Debugging
```csharp
// Check system status
Debug.Log($"Active sounds: {SoundSystemCore.Instance.GetActiveSoundCount()}");
Debug.Log($"Available sources: {SoundSystemCore.Instance.GetAvailableSourceCount()}");

// List all sound events
SoundEventsManager.Instance.ListAllSoundEvents();
```

### Performance Tips
- Use `SoundEvent.cooldownTime` to prevent spam
- Set proper priority levels for different sound types
- Use `SoundCategory.Environment` for ambient sounds (lower priority)
- Use `SoundCategory.Critical` sparingly (only for essential sounds)

---

## 🔄 Migration Guide

### Files Modified
- `SkullEnemy.cs` - Updated to use new sound system
- `DaggerProjectile.cs` - Replaced problematic sound methods
- `AudioManager.cs` - Can be safely disabled/removed after testing

### Compatibility Layer
The `AudioManagerLegacyBridge` provides compatibility for existing code:
```csharp
// This will still work, but shows deprecation warnings
AudioManager.Instance.PlaySound3DAtPoint(clip, position, volume);
```

### Recommended Migration Steps
1. Test the new system alongside the old one
2. Gradually replace AudioManager calls with SoundSystemCore calls
3. Create SoundEvent assets for organized management
4. Remove old AudioManager when fully migrated

---

## 🎵 Best Practices

1. **Categorize sounds correctly** - UI sounds should be 2D, combat sounds 3D
2. **Use appropriate priorities** - Critical for essential sounds only
3. **Set reasonable distances** - Combat sounds shouldn't be heard across the entire map
4. **Use pitch variation sparingly** - 5% (0.05f) is usually enough
5. **Pool and reuse** - The system handles this automatically
6. **Test with different Time.timeScale values** - Ensure sounds work during slow-motion

---

## 📞 Support

If you encounter issues:
1. Check the Console for error messages
2. Use the debugging methods shown above
3. Verify your AudioMixer setup
4. Ensure proper GameObject hierarchy

The new system is designed to be robust and provide clear error messages to help with debugging.

---

*AAA Sound System v1.0 - Professional game audio redefined* 🎮🔊
