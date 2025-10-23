# Energy Burst Regeneration System

## Overview
Updated `PlayerEnergySystem.cs` to match the health regeneration burst mechanic. This motivates players to stop sprinting for a cooldown period to receive a faster refill boost.

## Key Changes

### 1. Added Burst Regeneration Parameters
```csharp
[SerializeField] private float regenBurstMultiplier = 3f;  // 3x speed initially
[SerializeField] private float regenBurstDuration = 2f;    // 2 seconds of burst
```

### 2. Added State Tracking
```csharp
private float regenStartTime = 0f;      // Track when regen started
private bool isRegenerating = false;    // Track regen state
```

### 3. Updated Regeneration Logic
The `RegenerateEnergy()` method now:
- **Tracks regeneration start time** when regen begins
- **Applies burst multiplier** for the first 2 seconds (3x speed)
- **Smoothly transitions** from 3x speed to 1x speed using `Mathf.Lerp`
- **Continues at normal rate** after burst duration expires

### 4. Reset Regeneration on Sprint
When the player starts sprinting again, the regeneration state is reset, ensuring the burst only applies when they stop sprinting for the full cooldown period.

## How It Works

### Timeline Example:
1. **Player stops sprinting** → `lastSprintTime` recorded
2. **Wait 0.5s** (regenDelay) → Regeneration begins
3. **First 2 seconds** → Energy refills at **3x speed** (75 energy/sec)
4. **After 2 seconds** → Energy refills at **1x speed** (25 energy/sec)
5. **If player sprints again** → Burst resets, must wait full cooldown to get burst again

## Benefits for Gameplay

### Player Motivation
- **Encourages tactical sprint management** - Players are rewarded for stopping sprint completely
- **Faster recovery** - Initial burst helps players get back into action quickly
- **Strategic depth** - Players must decide: keep sprinting slowly or stop for burst recovery

### Consistency with Health System
- **Matches health regeneration** - Same burst mechanic for consistency
- **Familiar feel** - Players understand both systems work the same way
- **Unified design** - Coherent game feel across all regeneration systems

## Inspector Settings (Tunable)

| Parameter | Default | Description |
|-----------|---------|-------------|
| `energyRegenRate` | 25 | Base energy per second (normal rate) |
| `regenDelay` | 0.5s | Delay before regen starts after sprint stops |
| `regenBurstMultiplier` | 3x | Speed multiplier during burst phase |
| `regenBurstDuration` | 2s | How long the burst phase lasts |

## Debug Logging
Added debug logs to track:
- When regeneration starts
- Current energy levels
- When energy is fully recharged

## Testing Recommendations
1. Sprint until energy depletes
2. Stop sprinting and observe fast initial refill
3. Notice slower refill after 2 seconds
4. Sprint again briefly and stop - burst should reset
5. Verify UI slider reflects the burst speed visually
