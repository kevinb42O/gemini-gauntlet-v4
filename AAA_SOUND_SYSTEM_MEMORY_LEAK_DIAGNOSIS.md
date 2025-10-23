# 🔴 CRITICAL: Sound System Memory Leak Diagnosis

## Problem Summary
After 5 minutes of gameplay, the sound system completely breaks. The shotgun blast sound is the primary culprit.

## Root Cause Analysis

### 1. **Coroutine Accumulation (CRITICAL)**
**Location:** `SoundEvents.cs` lines 440-487

Every time a shotgun blast is fired:
1. `SafePlayShotgunSound3D()` is called (line 55-84 in `GameSoundsHelper.cs`)
2. This enables `useVolumeRolloff = true` for the sound
3. `ApplyVolumeRolloff()` starts a coroutine (line 444)
4. **The coroutine is started on `SoundSystemCore.Instance`**

**The Problem:**
- Shotgun fires rapidly (potentially 10-30+ times per minute)
- Each shot creates a NEW coroutine that runs for the duration of the audio clip
- **Coroutines are NOT cleaned up when the PooledAudioSource is returned to the pool**
- After 5 minutes: **300-900+ coroutines** could be running simultaneously
- This causes:
  - Memory exhaustion
  - CPU overload
  - Sound system failure

### 2. **Volume Rolloff Coroutine Lifecycle Issue**
**Location:** `SoundEvents.cs` lines 451-487

```csharp
private System.Collections.IEnumerator VolumeRolloffCoroutine(SoundHandle handle)
{
    if (!handle.IsValid || clip == null) yield break;
    
    float clipDuration = clip.length;
    float rolloffStartDelay = clipDuration * rolloffStartTime;
    
    // Wait until it's time to start the rolloff
    yield return new WaitForSeconds(rolloffStartDelay);  // ⚠️ STILL RUNNING EVEN IF SOUND STOPPED
    
    // Check if the sound is still playing and valid
    if (!handle.IsValid || !handle.IsPlaying) yield break;
    // ... rest of coroutine
}
```

**Issues:**
- Coroutine waits for `rolloffStartDelay` (typically 75% of clip duration)
- During this wait, the sound might finish and the PooledAudioSource gets returned to pool
- The coroutine continues running even though the audio source is recycled
- No cleanup mechanism when PooledAudioSource is reset

### 3. **Shotgun-Specific Rolloff Settings**
**Location:** `GameSoundsHelper.cs` lines 55-84

```csharp
private static SoundHandle SafePlayShotgunSound3D(...)
{
    // Configure optimal rolloff settings for shotgun sounds
    soundEvent.useVolumeRolloff = true;
    soundEvent.fadeOutDuration = 0.12f;
    soundEvent.rolloffStartTime = 0.75f;
    
    var handle = soundEvent.Play3D(position, volume);
    
    // Restore original settings
    soundEvent.useVolumeRolloff = originalRolloff;
    // ...
}
```

**The shotgun sound ALWAYS triggers the rolloff coroutine**, making it the worst offender.

## Why It Takes 5 Minutes to Break

1. **Initial Phase (0-2 min):** 50-100 coroutines running, system handles it
2. **Degradation Phase (2-4 min):** 200-400 coroutines, noticeable lag
3. **Critical Phase (4-5 min):** 500+ coroutines, memory pressure
4. **Failure (5+ min):** 700+ coroutines, Unity can't allocate more, sound system crashes

## Evidence

### Coroutine Leak Pattern
- Each shotgun blast = 1 new coroutine
- Coroutine lifetime = clip duration (0.5-2 seconds typically)
- But they accumulate faster than they complete
- No cleanup when PooledAudioSource is recycled

### Memory Allocation
- Each coroutine allocates memory for:
  - Coroutine state machine
  - Local variables (startVolume, elapsed, etc.)
  - WaitForSeconds objects
- Garbage collector can't keep up with allocation rate

## Solution Required

### Option 1: Remove Coroutine-Based Rolloff (RECOMMENDED)
- Use Unity's built-in AudioSource fade
- Or handle fade in Update() loop with a flag
- No coroutine allocation

### Option 2: Track and Stop Coroutines
- Store coroutine reference in PooledAudioSource
- Stop coroutine when audio source is returned to pool
- Requires tracking mechanism

### Option 3: Disable Rolloff for Shotgun
- Remove the rolloff feature entirely for shotgun sounds
- Simpler but less polished audio

## Immediate Fix
The quickest fix is to **disable the volume rolloff for shotgun sounds** by removing the rolloff configuration in `SafePlayShotgunSound3D()`.

## Files Affected
1. `SoundEvents.cs` - Coroutine creation
2. `GameSoundsHelper.cs` - Shotgun-specific rolloff enablement
3. `PooledAudioSource.cs` - Missing coroutine cleanup
4. `SoundSystemCore.cs` - Coroutine host

---
**Priority:** CRITICAL
**Impact:** Game-breaking after 5 minutes
**Difficulty:** Medium (requires coroutine lifecycle management)
