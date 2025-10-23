# 🔫 Shooting Sounds 3D Implementation - TODO

## What Needs to Be Done
Make shooting sounds play through the hand's AudioSource (same as overheat sounds).

---

## 🎯 The Pattern (Already Working for Overheat)

```csharp
// Get the correct hand's AudioSource
AudioSource handAudioSource = isPrimary ? primaryHandAudioSource : secondaryHandAudioSource;

// Play through the AudioSource (sound follows hand!)
handAudioSource.PlayOneShot(soundEvent.clip);
```

---

## 📝 Files to Modify

### 1. HandFiringMechanics.cs
This is where shooting happens. Find where shooting sounds are played.

**Look for:**
```csharp
soundEvent.Play2D();  // ❌ Current (2D, doesn't follow)
```

**Replace with:**
```csharp
handAudioSource.PlayOneShot(soundEvent.clip);  // ✅ New (3D, follows hand)
```

### 2. PlayerShooterOrchestrator.cs
If this handles shooting audio coordination.

---

## 🔧 Implementation Steps

### Step 1: Add AudioSource References
In your shooting script, add:
```csharp
[Header("3D Audio")]
public AudioSource primaryHandAudioSource;   // LEFT hand
public AudioSource secondaryHandAudioSource; // RIGHT hand
```

### Step 2: Find Shooting Sound Calls
Search for where shooting sounds are triggered:
- `Play2D()`
- `Play3D()`
- Sound event calls

### Step 3: Replace with PlayOneShot
```csharp
// OLD (doesn't follow hand):
shootSound.Play2D();

// NEW (follows hand):
AudioSource handSource = isPrimary ? primaryHandAudioSource : secondaryHandAudioSource;
if (handSource != null)
{
    handSource.PlayOneShot(shootSound.clip);
}
```

### Step 4: Assign in Inspector
- Drag LEFT hand → Primary Hand Audio Source
- Drag RIGHT hand → Secondary Hand Audio Source

---

## 🎮 What to Test

1. Fire LEFT hand → sound comes from LEFT
2. Fire RIGHT hand → sound comes from RIGHT
3. Move hands while firing → sounds follow hands
4. Rapid fire → multiple sounds overlap correctly

---

## 💡 Quick Reference

### For Single Shot:
```csharp
handAudioSource.PlayOneShot(soundClip);
```

### For Shot with Volume/Pitch:
```csharp
handAudioSource.pitch = soundEvent.pitch;
handAudioSource.volume = soundEvent.volume;
handAudioSource.PlayOneShot(soundEvent.clip);
```

### For Looping Sounds (like beam):
```csharp
handAudioSource.clip = soundClip;
handAudioSource.loop = true;
handAudioSource.Play();
```

---

## ✅ Result

Once implemented:
- ✅ Shooting sounds come from actual hand position
- ✅ Sounds follow hands as they move
- ✅ Proper 3D directional audio
- ✅ Consistent with overheat sounds

---

*Same pattern as overheat - just apply it to shooting!* 🔫🔊
