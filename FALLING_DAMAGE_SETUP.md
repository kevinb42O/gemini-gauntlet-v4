# Falling Damage System Setup Guide

## Overview
The falling damage system automatically tracks when the player falls from high places and applies damage when landing.

## Features
- **Automatic fall tracking**: Detects when player leaves ground and starts falling
- **Height threshold**: Only applies damage when falling from 2000+ units
- **Fixed damage**: Always deals 500 HP damage on high falls
- **Bypasses armor plates**: Damage goes directly to health (armor cannot absorb fall damage)
- **Integrated damage feedback**: Uses existing hit effect overlay (0.15 sec duration)
- **Fall damage sound**: Plays special fall damage sound on landing (together with normal land sound)
- **Falling wind loop**: Plays looping wind sound while falling (starts after 0.3s delay)
- **Smart tracking**: Tracks highest point during fall (handles jumps/bounces mid-fall)

## Setup Instructions

### 1. Add Component to Player
1. Select your **Player** GameObject in the hierarchy
2. Click **Add Component**
3. Search for **Falling Damage System**
4. Add it to the player

### 2. Configure Sound Events (Required)
1. Open your **SoundEvents** asset in the inspector
2. Configure **Fall Damage Sound**:
   - Find **"PLAYER: State"** section
   - Assign sound clips to the **Fall Damage** array
   - Recommended: Heavy impact/thud sounds
   - These play together with the normal land sound
3. Configure **Falling Wind Sound**:
   - Find **"PLAYER: Movement"** section
   - Assign your wind sound to **Falling Wind Loop**
   - **IMPORTANT**: Set the sound to **Loop = true** in the SoundEvent settings
   - Recommended volume: 0.5-0.8
4. Save the SoundEvents asset

### 3. How It Works

**Fall Detection:**
- System monitors when player leaves the ground
- Tracks the highest point reached during the fall
- Calculates total fall distance when landing

**Wind Sound:**
- Starts playing after 0.3 seconds of falling (avoids playing on small jumps)
- Loops continuously while falling
- Stops immediately when landing
- Volume adjustable in inspector (default: 0.7)

**Damage Application:**
- If fall distance ≥ 2000 units → Apply 500 damage
- If fall distance < 2000 units → No damage
- **Damage bypasses armor plates** - goes directly to health
- Automatically shows red hit overlay for 0.15 seconds
- Plays fall damage sound + normal land sound together
- Respects god mode (no damage if god mode active)

**Example Scenarios:**
- Fall from 1500 units → No damage
- Fall from 2000 units → 500 damage
- Fall from 5000 units → 500 damage (same as 2000)
- Jump during fall → Tracks highest point, not start point

## Debug Logging

The system logs useful information:
```
[FallingDamageSystem] Started fall from height: 1234.5
[FallingDamageSystem] Landed! Fall distance: 2500.0 units (threshold: 2000)
[FallingDamageSystem] Applying fall damage: 500 HP
```

### 4. Optional Configuration
The component has sensible defaults, but you can adjust in the inspector:

**Fall Damage Settings:**
- **Fall Damage Threshold**: `2000` units (minimum height to trigger damage)
- **Fall Damage**: `500` HP (damage dealt on landing)

**Wind Sound Settings:**
- **Wind Sound Start Delay**: `0.3` seconds (delay before wind starts - prevents sound on small jumps)
- **Wind Sound Volume**: `0.7` (volume multiplier for the wind loop)

## Integration with Existing Systems

**Works with:**
- ✅ PlayerHealth damage system (bypasses armor plates)
- ✅ Hit effect overlay (red flash on damage)
- ✅ Health regeneration (fall damage triggers regen delay)
- ✅ God mode (fall damage blocked when god mode active)
- ✅ AAAMovementController ground detection
- ✅ Sound system (fall damage sound + land sound + wind loop)

**No conflicts with:**
- Movement systems (flight/ground modes)
- Jump mechanics
- Dash/slide mechanics

## Testing

1. **Test normal fall (no damage):**
   - Jump from a platform ~1000 units high
   - Wind sound should NOT play (too short)
   - Should land without damage

2. **Test high fall (damage):**
   - Jump from a platform 2500+ units high
   - Wind sound should start after 0.3 seconds of falling
   - Wind sound should loop while falling
   - Wind sound should stop when landing
   - Should see red damage overlay on landing
   - Should hear fall damage sound + normal land sound
   - Should lose 500 HP directly from health bar
   - Health should start regenerating after 5 seconds

3. **Test with armor plates:**
   - Equip 1-3 armor plates
   - Fall from 2500+ units
   - **Armor plates should NOT absorb the damage** (bypassed)
   - Health should decrease by 500 HP directly

4. **Test with god mode:**
   - Activate god mode powerup
   - Fall from any height
   - Should take no damage

## Customization

### Change Damage Amount
Edit `fallDamage` in inspector or code (line 11):
```csharp
[SerializeField] private float fallDamage = 500f; // Change to desired damage
```

### Change Height Threshold
Edit `fallDamageThreshold` in inspector or code (line 10):
```csharp
[SerializeField] private float fallDamageThreshold = 2000f; // Change to desired height
```

### Change Fall Damage Sound
1. Open your **SoundEvents** asset
2. Find **"PLAYER: State" → Fall Damage** array
3. Replace with your preferred impact sounds
4. Supports multiple clips (plays random)

### Variable Damage Based on Height
Replace the `ApplyFallDamage()` method with:
```csharp
private void ApplyFallDamage()
{
    if (playerHealth == null) return;
    
    float fallDistance = highestPointDuringFall - transform.position.y;
    float excessFall = fallDistance - fallDamageThreshold;
    float scaledDamage = fallDamage + (excessFall * 0.1f); // +10% per 100 units
    
    Debug.Log($"[FallingDamageSystem] Applying fall damage: {scaledDamage} HP (bypassing armor)");
    playerHealth.TakeDamageBypassArmor(scaledDamage);
    GameSounds.PlayFallDamage(transform.position);
}
```

## Troubleshooting

**Fall damage not triggering:**
- Check that component is attached to player
- Verify `PlayerHealth` component exists on same GameObject
- Check console for error messages
- Ensure fall height is actually ≥ 2000 units

**Damage triggers too early:**
- Reduce `fallDamageThreshold` value
- Check if world scale is correct (system uses Unity units)

**Damage triggers too late:**
- Increase `fallDamageThreshold` value

**No hit overlay showing:**
- Verify `PlayerHealth.hitEffectCanvasGroup` is assigned
- Check that hit effect duration is set correctly (0.15s default)

**No fall damage sound:**
- Check that SoundEvents asset has fall damage sounds assigned
- Verify SoundEventsManager is in the scene
- Check console for sound system warnings

**Wind sound not playing:**
- Check that **Falling Wind Loop** is assigned in SoundEvents
- Verify the SoundEvent has **Loop = true** enabled
- Check that fall duration is > 0.3 seconds
- Adjust **Wind Sound Start Delay** if needed

**Wind sound doesn't stop:**
- This shouldn't happen - check console for errors
- Verify landing detection is working (check IsGrounded)

**Armor plates absorbing damage:**
- This is now fixed - damage bypasses armor plates
- Uses `TakeDamageBypassArmor()` method instead of `TakeDamage()`

## Notes

- System uses `CharacterController.isGrounded` or `AAAMovementController.IsGrounded` for accurate detection
- Fall distance is measured from **highest point** during fall, not just start point
- **Damage bypasses armor plates** - goes directly to health (logical for fall damage)
- **Wind sound has 0.3s delay** - prevents playing on small jumps/hops
- Wind sound loops smoothly while falling and stops cleanly on landing
- Fall damage sound plays together with normal land sound for better feedback
- System automatically resets after each landing
- Compatible with both flight and ground movement modes
- No performance impact (only runs Update when falling)
