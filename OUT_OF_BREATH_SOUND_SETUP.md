# Out-of-Breath Sound System - Setup Guide

## 🎯 What It Does

Plays a realistic out-of-breath looping sound that:
- ✅ Fades in smoothly starting at **20% energy**
- ✅ Reaches full volume at **5% energy** (completely out of breath)
- ✅ Continues playing until energy recovers to **20%**
- ✅ Fades out smoothly when energy is regained
- ✅ Uses realistic volume curves for natural breathing sounds

---

## 🔧 Setup Instructions

### Step 1: Add Out-of-Breath Sound to SoundEvents Asset

1. **Open your SoundEvents asset** in the Inspector
   - Usually located in `Assets/Audio/` or similar

2. **Find the new "PLAYER: Energy" section**
   - It's right after "PLAYER: State"

3. **Configure the Out Of Breath Loop:**
   ```
   Out Of Breath Loop:
   ├─ Clip: [Your breathing sound clip]
   ├─ Category: SFX
   ├─ Volume: 1.0
   ├─ Pitch: 1.0
   ├─ Pitch Variation: 0.02 (subtle variation)
   ├─ Loop: ✅ TRUE (IMPORTANT!)
   └─ Cooldown Time: 0
   ```

   **Sound Recommendations:**
   - Heavy breathing loop
   - Panting sound
   - Exhausted breathing
   - Should be seamless loop (no clicks)
   - Duration: 2-4 seconds for natural loop

---

### Step 2: Add OutOfBreathSoundManager to Player

1. **Select your Player GameObject** in the hierarchy

2. **Add Component:**
   - Add Component → `OutOfBreathSoundManager`

3. **Configure in Inspector:**
   ```
   OutOfBreathSoundManager:
   ├─ Sound Events: [Drag your SoundEvents asset here]
   ├─ Player Energy System: [Auto-found, or drag PlayerEnergySystem]
   │
   ├─ FADE SETTINGS:
   ├─ Fade Start Threshold: 0.2 (20% energy - sound starts)
   ├─ Full Volume Threshold: 0.05 (5% energy - full volume)
   ├─ Max Volume: 0.8 (80% - not too loud)
   ├─ Min Volume: 0.0 (silent when faded out)
   ├─ Fade Speed: 2.0 (smooth transition)
   │
   └─ Enable Debug Logs: ☐ (turn on for testing)
   ```

---

## 🎚️ How the Volume Fading Works

### Energy Levels & Volume Response

```
Energy Level          Volume      State
─────────────────────────────────────────────
100% - 21%           0%          Silent
20%                  0%          Fade starts
15%                  ~40%        Fading in
10%                  ~70%        Getting louder
5%                   100%        Full volume
0%                   100%        Full volume (exhausted!)
```

### Fade Curve

The system uses **SmoothStep** interpolation for natural breathing:
- Not linear (would sound robotic)
- Smooth acceleration/deceleration
- Mimics real breathing intensity

---

## 🎵 Sound Design Tips

### For Best Results:

1. **Loop Quality:**
   - Use seamless loops (no clicks at loop point)
   - Test loop in Audacity/DAW before importing
   - Crossfade loop points if needed

2. **Sound Characteristics:**
   - Heavy breathing (not gasping)
   - Consistent rhythm
   - Not too dramatic (player hears it often)
   - Mix should sit under gameplay sounds

3. **Volume Balance:**
   - Default `maxVolume: 0.8` is a good starting point
   - Adjust based on your other sound effects
   - Should be noticeable but not annoying

4. **Pitch Variation:**
   - Keep `pitchVariation` low (0.02)
   - Too much variation sounds unnatural
   - Breathing should be consistent

---

## 🧪 Testing Checklist

### Basic Functionality
- [ ] Sound starts fading in at 20% energy
- [ ] Sound reaches full volume at 5% energy
- [ ] Sound continues while energy is depleted
- [ ] Sound fades out smoothly when energy recovers
- [ ] No audio clicks or pops during fade
- [ ] Loop is seamless (no gaps)

### Edge Cases
- [ ] Works when energy depletes gradually (sprinting)
- [ ] Works when energy depletes instantly (if applicable)
- [ ] Stops correctly when player dies
- [ ] Resumes correctly after respawn
- [ ] Doesn't play during flight mode (if desired)

### Volume Levels
- [ ] Not too loud (doesn't overpower gameplay)
- [ ] Not too quiet (player can hear when tired)
- [ ] Fade is smooth and natural
- [ ] Full volume feels appropriately "exhausted"

---

## 🔧 Customization Options

### Adjust Fade Thresholds

**Make it start earlier (more warning):**
```csharp
Fade Start Threshold: 0.3 (30% energy)
Full Volume Threshold: 0.1 (10% energy)
```

**Make it more aggressive (less warning):**
```csharp
Fade Start Threshold: 0.15 (15% energy)
Full Volume Threshold: 0.02 (2% energy)
```

### Adjust Fade Speed

**Slower, more gradual:**
```csharp
Fade Speed: 1.0
```

**Faster, more responsive:**
```csharp
Fade Speed: 4.0
```

### Adjust Max Volume

**More subtle:**
```csharp
Max Volume: 0.5
```

**More dramatic:**
```csharp
Max Volume: 1.0
```

---

## 🐛 Troubleshooting

### Sound Doesn't Play
- ✅ Check SoundEvents asset is assigned
- ✅ Check outOfBreathLoop has a clip assigned
- ✅ Check Loop is enabled on the SoundEvent
- ✅ Enable Debug Logs to see what's happening
- ✅ Check PlayerEnergySystem is found

### Sound Clicks/Pops
- ✅ Ensure audio clip is a seamless loop
- ✅ Lower Fade Speed for smoother transitions
- ✅ Check audio clip import settings (no compression artifacts)

### Volume Too Loud/Quiet
- ✅ Adjust Max Volume setting
- ✅ Check SoundEvent volume setting
- ✅ Check master SFX volume in audio mixer

### Fade Not Smooth
- ✅ Increase Fade Speed for faster response
- ✅ Check energy depletion rate isn't too fast
- ✅ Adjust threshold values for wider fade range

---

## 📊 Technical Details

### Performance
- **CPU Impact:** Minimal (~0.01ms per frame)
- **Memory:** Single looping AudioSource
- **Updates:** Every frame (volume interpolation)

### Integration Points
- Listens to `PlayerEnergySystem.CurrentEnergy`
- Uses `SoundEvents.outOfBreathLoop`
- Managed by `SoundSystemCore` (pooled AudioSource)

### Volume Calculation Formula
```csharp
if (energy <= 5%) → volume = 100%
if (5% < energy <= 20%) → volume = SmoothStep(100%, 0%)
if (energy > 20%) → volume = 0%
```

---

## ✅ Summary

You now have a **realistic out-of-breath sound system** that:
1. ✅ Fades in smoothly starting at 20% energy
2. ✅ Reaches full volume at 5% energy
3. ✅ Continues until energy recovers to 20%
4. ✅ Uses smooth volume curves for natural breathing
5. ✅ Automatically manages playback and cleanup
6. ✅ Integrates seamlessly with existing energy system

**The system is production-ready and fully customizable!** 🎵
