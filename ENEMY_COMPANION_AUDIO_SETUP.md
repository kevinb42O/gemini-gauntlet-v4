# Enemy Companion Audio Setup Guide

## Overview
Added hitmarker and death sound effects for the EnemyCompanion system. Both sounds are 3D spatial audio that plays at the companion's position.

## Changes Made

### 1. SoundEvents.cs
Added two new sound event arrays in the **ENEMIES** section:

- **`companionHitmarker`** - Plays when the enemy companion is hit by the player
- **`companionDeath`** - Plays when the enemy companion dies

Location: `Assets/scripts/Audio/FIXSOUNDSCRIPTS/SoundEvents.cs` (lines 82-86)

### 2. EnemyCompanionBehavior.cs
Added audio integration:

- **New field**: `soundEvents` (SoundEvents reference) - Inspector field under "🔊 AUDIO SETTINGS"
- **New method**: `PlayHitmarkerSound()` - Plays 3D hitmarker sound at companion position
- **New method**: `PlayDeathSound()` - Plays 3D death sound at companion position
- **Integration**: Sounds automatically play when companion is damaged or dies

## Unity Setup Instructions

### Step 1: Configure SoundEvents Asset
1. Open your **SoundEvents** ScriptableObject asset in the Inspector
2. Navigate to the **"► ENEMIES: Companion"** section
3. Configure the following arrays:

#### Companion Hitmarker
- **Array Size**: Set to the number of hitmarker sound variations you want
- **For each element**:
  - Assign an **AudioClip** (your hitmarker sound)
  - Set **Category**: `SFX`
  - Set **Volume**: `0.8` - `1.0` (adjust to taste)
  - Set **Pitch**: `1.0`
  - Set **Pitch Variation**: `0.1` (adds variety)
  - Enable **use3DOverride**: `true`
  - Set **minDistance3D**: `500` (units)
  - Set **maxDistance3D**: `3000` (units)

#### Companion Death
- **Array Size**: Set to the number of death sound variations you want
- **For each element**:
  - Assign an **AudioClip** (your death sound)
  - Set **Category**: `SFX`
  - Set **Volume**: `1.0` - `1.2` (death should be louder)
  - Set **Pitch**: `0.9` (slightly lower for dramatic effect)
  - Set **Pitch Variation**: `0.05`
  - Enable **use3DOverride**: `true`
  - Set **minDistance3D**: `800` (units)
  - Set **maxDistance3D**: `5000` (units - death sound travels farther)

### Step 2: Configure EnemyCompanionBehavior
1. Select your **EnemyCompanion** GameObject in the scene
2. Find the **EnemyCompanionBehavior** component
3. Scroll to **"🔊 AUDIO SETTINGS"**
4. Drag your **SoundEvents** ScriptableObject into the **Sound Events** field

### Step 3: Test
1. Enter Play Mode
2. Shoot the enemy companion
   - You should hear the **hitmarker sound** at the companion's position
3. Kill the enemy companion
   - You should hear the **death sound** at the companion's position

## Sound Recommendations

### Hitmarker Sound
- **Type**: Short, punchy impact sound
- **Duration**: 0.1 - 0.3 seconds
- **Examples**: Metal hit, flesh impact, shield hit
- **Variations**: 2-4 different sounds for variety

### Death Sound
- **Type**: Dramatic death vocalization or mechanical shutdown
- **Duration**: 0.5 - 2.0 seconds
- **Examples**: Death cry, explosion, mechanical failure
- **Variations**: 2-3 different sounds

## Technical Details

### 3D Audio Configuration
Both sounds use **3D spatial audio** with the following characteristics:

- **Hitmarker**:
  - Plays at companion's position when damaged
  - Audible range: 500-3000 units
  - Volume: 80-100%
  - Pitch variation for variety

- **Death**:
  - Plays at companion's position when health reaches 0
  - Audible range: 800-5000 units (travels farther)
  - Volume: 100-120%
  - Lower pitch for dramatic effect

### Performance
- Sounds are played through the existing `SoundSystemCore`
- Uses object pooling for efficient audio source management
- No performance impact - sounds are triggered only on damage/death events

## Debugging

If sounds don't play:

1. **Check SoundEvents reference**:
   - Ensure `soundEvents` field is assigned in EnemyCompanionBehavior
   
2. **Check audio clips**:
   - Ensure `companionHitmarker` and `companionDeath` arrays have audio clips assigned
   
3. **Enable debug logging**:
   - Set `showDebugInfo = true` in EnemyCompanionBehavior
   - Check console for audio-related messages:
     - `🔊 Playing hitmarker sound at [position]`
     - `💀 Playing death sound at [position]`
     - `⚠️ No hitmarker/death sound configured!`

4. **Check SoundSystemCore**:
   - Ensure SoundSystemCore is active in the scene
   - Check audio mixer settings

## Integration Notes

- Sounds automatically play when companion takes damage or dies
- No additional scripting required
- Works with existing hit effects and knockback systems
- Compatible with all companion AI states (Idle, Patrolling, Hunting, Attacking)
- Sounds play even if visual hit effects are disabled

## Future Enhancements

Potential additions:
- Footstep sounds for companion movement
- Attack sounds (shotgun/beam firing)
- Aggro/detection sounds
- Low health warning sounds
- Tactical callout sounds
