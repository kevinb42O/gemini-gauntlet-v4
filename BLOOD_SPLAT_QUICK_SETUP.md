# Blood Splat System - Quick Setup Guide

## 🎯 Setup Your 3 Hit Sounds

### Step 1: Find Your SoundEvents Asset
1. In Unity, locate your `SoundEvents` ScriptableObject asset
2. Usually found in `Assets/Audio/` or similar folder
3. Select it to view in Inspector

### Step 2: Configure Player Hit Sounds Array
1. Find the **"► PLAYER: State"** section
2. Locate the **"Player Hit"** field (it's now an array)
3. Set **Size** to `3`
4. You'll see 3 slots appear: Element 0, Element 1, Element 2

### Step 3: Assign Your Audio Clips
For each of the 3 elements:

**Element 0:**
- Expand the element
- **Clip**: Drag your first hit sound audio file here
- **Category**: SFX
- **Volume**: 1.0
- **Pitch**: 1.0
- **Pitch Variation**: 0.05 (adds slight randomness)
- **Cooldown Time**: 0.3 (prevents spam)

**Element 1:**
- Repeat above with your second hit sound
- Same settings as Element 0

**Element 2:**
- Repeat above with your third hit sound
- Same settings as Element 0

### Step 4: Test It!
1. Enter Play Mode
2. Take damage from an enemy
3. You should hear:
   - ✅ Random hit sound from your 3 sounds
   - ✅ Smooth blood splat fade in/out
   - ✅ No flickering or spam
   - ✅ Different sound each time (variety!)

## 🎨 Visual Settings (Optional)

If you want to adjust the blood splat appearance:

1. Select your **Player** GameObject
2. Find the **PlayerHealth** component
3. Scroll to **"AAA Blood Splat Feedback System"** section
4. Adjust these values:

```
Fade In Speed: 3.0 (faster = quicker blood appearance)
Fade Out Speed: 1.5 (slower = blood lingers longer)
Max Alpha: 0.8 (higher = more intense blood)
Low Health Threshold: 0.3 (when to show intense blood)
Hit Cooldown: 0.3 (time between visual triggers)
```

## 🔊 Sound Tips

### Recommended Sound Types:
- **Impact sounds**: Flesh impact, body hit
- **Grunt sounds**: Short pain grunts
- **Shield sounds**: Energy shield hit (if you have shields)

### Variety Tips:
- Use different pitch variations for each sound
- Mix impact and grunt sounds
- Keep sounds short (< 0.5 seconds)
- Normalize volume levels so they're consistent

### Example Setup:
```
Element 0: "hit_impact_01.wav" (low grunt)
Element 1: "hit_impact_02.wav" (medium grunt)
Element 2: "hit_impact_03.wav" (high grunt)
```

## ✅ Verification Checklist

After setup, verify:
- [ ] All 3 sound clips are assigned
- [ ] Each has cooldown set to 0.3s
- [ ] Sounds play randomly (not always the same one)
- [ ] Blood splat fades smoothly
- [ ] No flickering during rapid hits
- [ ] Blood is more intense at low health
- [ ] Sounds don't spam/overlap

## 🐛 Common Issues

### "No sound playing"
- Check that all 3 array elements have clips assigned
- Verify SoundSystemCore exists in scene
- Check audio mixer isn't muted

### "Same sound every time"
- Make sure you assigned **different** clips to each element
- Check that array size is actually 3

### "Sounds overlapping/spamming"
- Increase cooldown time to 0.5s
- Check that you didn't assign the same clip multiple times

### "Blood still flickering"
- Increase Hit Cooldown to 0.5s in PlayerHealth
- Check that only one PlayerHealth component exists

---

**That's it!** Your blood splat system is now set up with variety and AAA polish! 🎮✨
