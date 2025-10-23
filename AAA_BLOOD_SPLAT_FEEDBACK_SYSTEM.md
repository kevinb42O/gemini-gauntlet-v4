# AAA Blood Splat Feedback System

## Overview
Professional-grade damage feedback system that provides smooth, health-based visual and audio cues when the player takes damage. Designed to eliminate flickering and provide intuitive health status feedback.

## Features

### 1. **Smooth Fade System**
- **No more flickering!** Blood splat fades in and out smoothly instead of instant show/hide
- Configurable fade speeds for both fade-in and fade-out
- Uses coroutine-based animation for buttery smooth transitions

### 2. **Health-Based Intensity**
- **Low Health (< 30%)**: Blood splat becomes more intense and persistent
  - At 0% health: Maximum opacity (80% alpha)
  - At 30% health: Half maximum opacity (40% alpha)
  - Smooth lerp between these values
- **Normal Health (> 30%)**: Brief, subtle flash (24% alpha)
- Provides intuitive visual feedback about current health status

### 3. **Anti-Flicker Protection**
- **Cooldown system** prevents rapid flickering from stream damage
- Default cooldown: 0.3 seconds between blood splat triggers
- Hits during cooldown are ignored for visual feedback (but still deal damage)

### 4. **Audio Feedback with Variety**
- New `playerHit[]` sound array plays random sounds when taking damage
- **Supports multiple sounds** (e.g., 3 different hit sounds) for variety
- Built-in cooldown in the sound system prevents audio spam
- Synchronized with visual feedback for cohesive experience
- No more monotonous repetitive sounds!

## Configuration

### Inspector Settings (PlayerHealth.cs)

```
AAA Blood Splat Feedback System:
├── Blood Splat Fade In Speed: 3.0 (how fast blood appears)
├── Blood Splat Fade Out Speed: 1.5 (how fast blood fades away)
├── Blood Splat Min Alpha: 0.0 (fully transparent when not hit)
├── Blood Splat Max Alpha: 0.8 (maximum opacity at critical health)
├── Blood Splat Low Health Threshold: 0.3 (30% health threshold)
└── Blood Splat Hit Cooldown: 0.3 (seconds between visual triggers)
```

### Recommended Values
- **Fade In Speed**: 3.0 - 5.0 (fast response to damage)
- **Fade Out Speed**: 1.0 - 2.0 (slower fade out for lingering effect)
- **Max Alpha**: 0.7 - 0.9 (visible but not overwhelming)
- **Low Health Threshold**: 0.2 - 0.4 (20-40% health)
- **Hit Cooldown**: 0.2 - 0.5 (balance between responsiveness and flicker prevention)

## Technical Implementation

### Key Components

1. **TriggerBloodSplatFeedback()**
   - Called whenever player takes damage
   - Checks cooldown to prevent spam
   - Calculates target alpha based on health percentage
   - Plays hit sound
   - Starts fade coroutine

2. **BloodSplatFadeCoroutine()**
   - Smoothly fades blood splat to target alpha
   - Holds briefly at peak intensity
   - Smoothly fades out to transparent
   - Uses Time.deltaTime for frame-rate independent animation

3. **Health-Based Alpha Calculation**
   ```csharp
   if (healthPercent <= threshold)
   {
       // Low health - intense blood
       float progress = healthPercent / threshold;
       alpha = Lerp(maxAlpha, maxAlpha * 0.5f, progress);
   }
   else
   {
       // Normal health - brief flash
       alpha = maxAlpha * 0.3f;
   }
   ```

## Audio Setup

### SoundEvents.cs
New sound event array added under **Player: State** section:
```
playerHit[]: Sounds played randomly when player takes damage
- Tooltip: "Sounds played randomly when player takes damage (with cooldown to prevent spam)"
- Supports multiple sounds for variety (e.g., 3 different hit sounds)
- Recommended: Impact/grunt sounds with 0.3s cooldown on each
- System automatically picks random sound from array
```

### GameSoundsHelper.cs
New method added:
```csharp
GameSounds.PlayPlayerHit(position, volume)
```

## Integration Points

### Damage Methods
The blood splat feedback is automatically triggered in:
- `TakeDamage(float amount)` - Standard damage
- `TakeDamageBypassArmor(float amount)` - Environmental damage

### Death System
The death system uses a separate `ShowBloodOverlay()` method that:
- Shows blood instantly at full opacity (for dramatic effect)
- Bypasses the smooth fade system
- Used for death and self-revive states

## Benefits

### Player Experience
✅ **Clear visual feedback** - Always know when you're being hit
✅ **Health status awareness** - Blood intensity indicates danger level
✅ **No visual spam** - Smooth fading prevents eye strain
✅ **Professional feel** - AAA-quality polish

### Performance
✅ **Efficient** - Single coroutine, minimal overhead
✅ **Optimized** - Cooldown prevents unnecessary calculations
✅ **Smooth** - Frame-rate independent animation

## Testing Checklist

- [ ] Blood splat fades in smoothly when taking damage
- [ ] Blood splat fades out smoothly after hit
- [ ] No flickering during rapid stream damage
- [ ] Blood splat is more intense at low health (< 30%)
- [ ] Blood splat is subtle at high health (> 30%)
- [ ] Hit sound plays with each visual trigger
- [ ] Hit sound doesn't spam during rapid hits
- [ ] Death system still shows full blood overlay
- [ ] Self-revive blood blink still works correctly

## Troubleshooting

### Blood splat not showing
1. Check that `bloodOverlayImage` is assigned in PlayerHealth inspector
2. Verify `bloodOverlayCanvasGroup` component exists on the blood overlay UI
3. Check that blood overlay is a child of the Canvas and properly layered

### Still flickering
1. Increase `bloodSplatHitCooldown` value (try 0.5s)
2. Increase `bloodSplatFadeOutSpeed` for faster fade out
3. Check that only one PlayerHealth component exists

### No sound playing
1. Expand the `playerHit` array to size 3 in SoundEvents asset
2. Assign your 3 audio clips to the array slots
3. Set cooldown to 0.3 seconds on each SoundEvent
4. Verify SoundSystemCore is initialized in scene
5. System automatically picks a random sound from the array

### Blood too intense/subtle
1. Adjust `bloodSplatMaxAlpha` (0.7-0.9 range)
2. Adjust `bloodSplatLowHealthThreshold` (0.2-0.4 range)
3. Tweak fade speeds for different feel

## Future Enhancements

Possible improvements:
- Directional blood splatter based on hit direction
- Different blood patterns for different damage types
- Vignette effect at critical health
- Screen shake integration
- Heartbeat audio at low health
- Color grading shifts at low health

---

**Status**: ✅ Implemented and Ready for Testing
**Version**: 1.0
**Date**: 2025-10-04
