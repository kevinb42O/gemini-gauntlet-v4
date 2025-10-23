# Enemy Companion Random Sound Variation System

## 🎲 Zero-Performance-Cost Random Beam Sounds

Each enemy companion now picks a **random beam sound at spawn time**, making combat less repetitive without any performance impact.

---

## How It Works

### The Smart Approach
- **At spawn time**: Each companion picks ONE random sound from your array
- **During gameplay**: That sound plays normally (zero extra cost)
- **Result**: Every enemy sounds different, but no performance hit!

### Why This Is Efficient
- ❌ **BAD**: Picking random sound every time beam plays (expensive, happens constantly)
- ✅ **GOOD**: Picking random sound once at spawn (free, happens once)

---

## Setup Instructions

### Step 1: Add Sound Variations to SoundEvents Asset

1. Open your `SoundEvents` ScriptableObject asset
2. Find the **"COMBAT: Companion Weapons"** section
3. You'll see a new field: **"Companion Stream Variations"**
4. Set the **Size** to however many variations you want (e.g., 5)
5. Assign different `SoundEvent` objects for each variation

**Example:**
```
Companion Stream Variations
├─ Element 0: CompanionBeam_Variation1
├─ Element 1: CompanionBeam_Variation2
├─ Element 2: CompanionBeam_Variation3
├─ Element 3: CompanionBeam_Variation4
└─ Element 4: CompanionBeam_Variation5
```

### Step 2: Configure Enemy Companion Prefabs

**Option A: Configure on Prefab (Recommended)**
1. Select your enemy companion prefab
2. Find the `CompanionAudio` component
3. Expand **"🎲 RANDOM SOUND VARIATION"** section
4. Set **"Companion Stream Variations"** array size (e.g., 5)
5. Assign your beam sound variations

**Option B: Leave Empty (Uses Default)**
- If you don't assign variations, it uses the default `companionStreamEvent`
- Friendly companions can use the default sound
- Only enemy companions need variations

---

## How to Create Sound Variations

### Method 1: Pitch Variations (Easiest)
1. Create multiple `SoundEvent` assets
2. Use the **same audio clip** for all
3. Adjust the **pitch** for each:
   - Variation 1: Pitch 0.9
   - Variation 2: Pitch 1.0 (normal)
   - Variation 3: Pitch 1.1
   - Variation 4: Pitch 0.85
   - Variation 5: Pitch 1.15

### Method 2: Different Audio Clips
1. Create multiple `SoundEvent` assets
2. Assign **different audio clips** to each
3. Use different beam sound recordings

### Method 3: Volume Variations
1. Same clip, different volumes
2. Creates depth perception (some enemies sound closer/farther)

---

## Testing

### In Unity Editor
1. Place an enemy companion in the scene
2. Enable `showDebugInfo` on `EnemyCompanionBehavior`
3. Play the game
4. Check console for: `"🎲 Assigned random stream sound variation (index X/5)"`
5. Each enemy should log a different index

### In Game
1. Spawn multiple enemy companions
2. Listen to their beam sounds
3. Each should sound slightly different!

---

## Performance Impact

**Zero.** Literally zero.

- ✅ Random selection happens **once per companion** at spawn
- ✅ No runtime checks during combat
- ✅ No extra memory allocation
- ✅ No performance difference vs single sound

---

## Troubleshooting

### All enemies still use the same sound
- Check that `companionStreamVariations` array is populated in `CompanionAudio`
- Verify the array has multiple elements (not just 1)
- Make sure the `SoundEvent` assets are different

### No sound plays at all
- Check that at least ONE of these is assigned:
  1. `companionStreamVariations` array (new system)
  2. `companionStreamEvent` (fallback)
  3. `streamLoopSFX` AudioClip (last resort)

### Debug logs show "No stream variations configured"
- This is normal if you haven't set up the variations yet
- The system will fall back to the default sound
- To fix: Assign the `companionStreamVariations` array

---

## Advanced: Per-Companion Sound Assignment

If you want specific companions to always use specific sounds:

1. **Don't use the variations array**
2. Instead, assign `companionStreamEvent` directly on each prefab
3. This gives you full control but requires manual setup

---

## Summary

✅ **What Changed:**
- Added `companionStreamVariations` array to `CompanionAudio`
- Each companion picks ONE random sound at spawn
- Zero performance cost during gameplay

✅ **What You Need to Do:**
1. Create 3-5 different `SoundEvent` assets (pitch variations work great)
2. Add them to the `companionStreamVariations` array
3. Done! Each enemy now sounds unique.

✅ **Shotgun Sounds:**
- You said shotgun is fine, so I left it unchanged
- If you want shotgun variations too, let me know!
