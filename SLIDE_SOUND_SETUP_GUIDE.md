# Slide Sound Setup Guide

## Overview
The slide sound system has been integrated into the SoundEvents framework. The slide sound plays as a **oneshot** (non-looping) effect when you start sliding, and **footstep sounds are completely disabled** while sliding.

## Changes Made

### 1. SoundEvents.cs
- **Added**: `slideSound` field under "PLAYER: Movement" section
- This is a single `SoundEvent` (not an array) for the slide sound

### 2. CleanAAACrouch.cs
- **Removed**: Old `AudioClip` fields (`slideStartSound`, `slideLoopSound`)
- **Removed**: Old `AudioSource slideAudioSource` 
- **Added**: `SoundHandle slideAudioHandle` for proper sound system integration
- **Updated**: `PlaySlideStartSound()` now uses `SoundEventsManager.Events.slideSound.Play2D()`
- **Updated**: `StopSlideAudio()` properly stops the sound handle
- **Added**: `enableDebugLogs` field for debugging slide audio

### 3. PlayerFootstepController.cs
- **Added**: `CleanAAACrouch crouchSystem` reference (auto-found)
- **Updated**: `Update()` now checks `crouchSystem.IsSliding` and skips all footstep logic while sliding
- **Benefit**: No footstep sounds play during slides, only the slide sound

## Unity Inspector Setup

### Step 1: Configure SoundEvents Asset
1. Locate your `SoundEvents` asset in the Project window (usually in `Assets/Audio/` or similar)
2. Select it to view in Inspector
3. Find the **"PLAYER: Movement"** section
4. Locate the new **"Slide Sound"** field
5. Configure it:
   - **Clip**: Assign your slide audio clip
   - **Category**: Set to `SFX`
   - **Volume**: Recommended `0.7` - `1.0`
   - **Pitch**: `1.0` (or adjust for effect)
   - **Pitch Variation**: `0.05` for slight variation
   - **Loop**: **MUST BE FALSE** (oneshot sound)
   - **Cooldown Time**: `0` (no cooldown needed)

### Step 2: Verify Player Setup
1. Select your Player GameObject
2. Find the `CleanAAACrouch` component
3. Verify settings:
   - **Slide Audio Enabled**: ✅ Checked
   - **Enable Debug Logs**: ✅ Check this temporarily to see audio feedback in console
4. Find the `PlayerFootstepController` component
5. Verify:
   - **Crouch System**: Should auto-populate with `CleanAAACrouch` reference
   - If not, manually assign it

### Step 3: Test
1. Enter Play mode
2. Start sliding (crouch while moving)
3. **Expected behavior**:
   - ✅ Slide sound plays once when slide starts
   - ✅ No footstep sounds play during slide
   - ✅ Footsteps resume when slide ends
4. Check Console for debug messages:
   - `[CleanAAACrouch] Playing slide sound (oneshot)`
   - No footstep messages while sliding

## Sound Design Notes

### Slide Sound Characteristics
- **Type**: Oneshot (plays once, doesn't loop)
- **Duration**: Recommended 0.5 - 2.0 seconds
- **Style**: Friction/scraping sound, whoosh, or impact
- **Volume**: Should be prominent but not overpowering

### Why Oneshot?
The slide is a short burst action. A oneshot sound:
- ✅ Provides immediate feedback
- ✅ Doesn't become repetitive
- ✅ Works for slides of any duration
- ✅ Cleaner audio mix (no loop management)

## Troubleshooting

### No Slide Sound Playing
1. Check `SoundEvents` asset has `slideSound` assigned
2. Verify `slideSound.clip` is not null
3. Check `CleanAAACrouch` has "Slide Audio Enabled" checked
4. Enable "Enable Debug Logs" to see console messages
5. Verify `SoundEventsManager.Events` is initialized

### Footsteps Still Playing During Slide
1. Check `PlayerFootstepController` has `crouchSystem` reference assigned
2. Verify `CleanAAACrouch.IsSliding` property is working (check in Inspector during play)
3. Enable debug logs in `PlayerFootstepController` to see footstep messages

### Slide Sound Loops (Unwanted)
1. Select your `SoundEvents` asset
2. Find the `slideSound` entry
3. Ensure **Loop** is **unchecked** (false)

## Advanced Configuration

### Adjusting Slide Sound Volume
In `SoundEvents` asset → `slideSound`:
- Increase `volume` field (0.0 - 2.0)
- Or adjust in `CleanAAACrouch.PlaySlideStartSound()` by changing the volume multiplier:
  ```csharp
  slideAudioHandle = SoundEventsManager.Events.slideSound.Play2D(1.5f); // 150% volume
  ```

### Adding Pitch Variation
In `SoundEvents` asset → `slideSound`:
- Increase `pitchVariation` (e.g., 0.1 for ±10% variation)
- Makes each slide sound slightly different

### Re-enabling Slide Loop (If Desired)
If you want a looping slide sound instead:
1. This requires additional code changes
2. Not recommended - oneshot is cleaner
3. Contact developer if needed

## Code Architecture

### Sound Flow
```
Player starts sliding
    ↓
CleanAAACrouch.TryStartSlide()
    ↓
CleanAAACrouch.PlaySlideStartSound()
    ↓
SoundEventsManager.Events.slideSound.Play2D()
    ↓
SoundSystemCore plays audio (oneshot)
```

### Footstep Suppression Flow
```
PlayerFootstepController.Update()
    ↓
Check: crouchSystem.IsSliding?
    ↓ YES
Skip all footstep logic, reset state
    ↓ NO
Continue normal footstep playback
```

## Files Modified
- `Assets/scripts/Audio/FIXSOUNDSCRIPTS/SoundEvents.cs`
- `Assets/scripts/CleanAAACrouch.cs`
- `Assets/scripts/PlayerFootstepController.cs`

---

**Setup Complete!** Your slide sound should now play as a oneshot when sliding starts, and footsteps will be silent during slides.
