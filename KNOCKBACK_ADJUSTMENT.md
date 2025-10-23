# Knockback Force Adjustment - Reduced Pushback 💥

## Problem

Enemies were being pushed back **too hard** even with `knockbackMultiplier` set to 0.01 (minimum).

---

## Root Cause

### Base Forces Were Too High

**Original values:**
```csharp
// Shotgun (close range)
knockbackForce = 2000f * knockbackMultiplier;

// Beam (long range)
knockbackForce = 500f * knockbackMultiplier;
```

**At minimum multiplier (0.01):**
- Shotgun: 2000 * 0.01 = **20 force** ← Still very strong!
- Beam: 500 * 0.01 = **5 force** ← Still noticeable

**At your setting (0.01):**
- Enemies were being pushed back significantly
- Made combat feel unfair
- Disrupted enemy positioning

---

## Solutions Implemented ✅

### Fix #1: Reduced Minimum Multiplier Range
**File:** `EnemyCompanionBehavior.cs` (Line 184)

```csharp
// OLD: Minimum 0.01
[Range(0.01f, 2f)] public float knockbackMultiplier = 0.3f;

// NEW: Minimum 0.001 (10x smaller!)
[Range(0.001f, 2f)] public float knockbackMultiplier = 0.3f;
```

**Result:** You can now set knockback as low as **0.001** for ultra-subtle feedback.

### Fix #2: Reduced Base Knockback Forces
**File:** `EnemyCompanionBehavior.cs` (Line 399, 407)

```csharp
// SHOTGUN (close range < 1600 units)
// OLD: 2000f * multiplier
// NEW: 800f * multiplier (60% reduction!)
knockbackForce = 800f * knockbackMultiplier;

// BEAM (long range > 1600 units)
// OLD: 500f * multiplier
// NEW: 200f * multiplier (60% reduction!)
knockbackForce = 200f * knockbackMultiplier;
```

**Result:** Base forces are now 60% weaker, making knockback much more subtle.

---

## New Knockback Values

### At Different Multiplier Settings:

| Multiplier | Shotgun Force | Beam Force | Feel |
|------------|---------------|------------|------|
| **0.001** | 0.8 | 0.2 | Ultra-subtle (barely noticeable) |
| **0.01** | 8 | 2 | Subtle (light feedback) |
| **0.05** | 40 | 10 | Moderate (noticeable) |
| **0.1** | 80 | 20 | Strong (clear feedback) |
| **0.3** (default) | 240 | 60 | Very strong (dramatic) |
| **1.0** | 800 | 200 | Extreme (ragdoll-like) |

### Recommended Settings:

**For subtle feedback (enemies barely move):**
```
knockbackMultiplier = 0.001 to 0.01
```

**For moderate feedback (enemies stagger slightly):**
```
knockbackMultiplier = 0.05 to 0.1
```

**For dramatic feedback (enemies pushed back visibly):**
```
knockbackMultiplier = 0.3 to 0.5
```

**For ragdoll-like feedback (enemies fly back):**
```
knockbackMultiplier = 1.0 to 2.0
```

---

## How Knockback Works

### Trigger:
Knockback applies when enemy takes damage via `OnCompanionDamaged()` event.

### Force Calculation:
```csharp
// 1. Determine weapon type by distance
if (distanceToPlayer < 1600f)
    baseForce = 800f;  // Shotgun
else
    baseForce = 200f;  // Beam

// 2. Apply multiplier
knockbackForce = baseForce * knockbackMultiplier;

// 3. Calculate direction (away from player)
knockbackDirection = (enemy.position - player.position).normalized;

// 4. Apply force
rigidbody.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);

// 5. Add upward force (30% of knockback)
rigidbody.AddForce(Vector3.up * (knockbackForce * 0.3f), ForceMode.Impulse);
```

### Force Mode:
Uses `ForceMode.Impulse` - instant force applied to Rigidbody mass.

---

## Configuration

### In Inspector:

**EnemyCompanionBehavior Component:**
- `enableKnockback` - Toggle knockback on/off
- `knockbackMultiplier` - Adjust strength (0.001 to 2.0)

### To Disable Knockback Completely:
```
enableKnockback = false
```

### To Make Knockback Ultra-Subtle:
```
enableKnockback = true
knockbackMultiplier = 0.001
```

---

## Testing

### Test Different Settings:

1. **Set multiplier to 0.001** (ultra-subtle)
   - Shoot enemy with shotgun
   - Enemy should barely move

2. **Set multiplier to 0.01** (subtle)
   - Shoot enemy with shotgun
   - Enemy should stagger slightly

3. **Set multiplier to 0.1** (moderate)
   - Shoot enemy with shotgun
   - Enemy should be pushed back noticeably

4. **Set multiplier to 1.0** (extreme)
   - Shoot enemy with shotgun
   - Enemy should fly back dramatically

### Console Logging:

Enable `showDebugInfo = true` to see knockback forces:
```
[EnemyCompanionBehavior] 💥 SHOTGUN KNOCKBACK! Force: 0.8
[EnemyCompanionBehavior] 💥 Beam knockback. Force: 0.2
```

---

## Physics Considerations

### Rigidbody Mass:
Knockback is affected by enemy's Rigidbody mass:
- **Lower mass (0.5-1.0):** More knockback
- **Higher mass (2.0-5.0):** Less knockback

### Drag:
Linear drag affects how quickly enemy stops after knockback:
- **Lower drag (0-0.5):** Enemy slides further
- **Higher drag (1.0-2.0):** Enemy stops quickly

### Gravity:
Your game has **-800 gravity**, so upward force is countered quickly.

---

## Comparison: Old vs New

### Old System (at 0.01 multiplier):
```
Shotgun: 2000 * 0.01 = 20 force ← Too strong!
Beam: 500 * 0.01 = 5 force ← Still noticeable
Minimum: 0.01 (couldn't go lower)
```

### New System (at 0.01 multiplier):
```
Shotgun: 800 * 0.01 = 8 force ← Much better!
Beam: 200 * 0.01 = 2 force ← Very subtle
Minimum: 0.001 (10x more control!)
```

### At 0.001 multiplier (NEW):
```
Shotgun: 800 * 0.001 = 0.8 force ← Ultra-subtle!
Beam: 200 * 0.001 = 0.2 force ← Barely noticeable
```

---

## Additional Options

### To Disable Upward Force:
**File:** `EnemyCompanionBehavior.cs` (Line 417)

```csharp
// Comment out this line:
// rb.AddForce(Vector3.up * (knockbackForce * 0.3f), ForceMode.Impulse);
```

### To Make Knockback Horizontal Only:
```csharp
// Modify knockbackDirection calculation (line 390)
Vector3 knockbackDirection = (transform.position - _realPlayerTransform.position);
knockbackDirection.y = 0; // ← Add this line
knockbackDirection = knockbackDirection.normalized;
```

### To Add Knockback Cooldown:
Prevent rapid knockback stacking by adding a cooldown:

```csharp
private float _lastKnockbackTime = 0f;
private const float KNOCKBACK_COOLDOWN = 0.5f; // 0.5 seconds

private void ApplyKnockback()
{
    // Check cooldown
    if (Time.time - _lastKnockbackTime < KNOCKBACK_COOLDOWN)
        return; // Skip knockback
    
    _lastKnockbackTime = Time.time;
    
    // ... rest of knockback code
}
```

---

## Summary

### Changes Made:
1. ✅ Minimum multiplier: **0.01 → 0.001** (10x more control)
2. ✅ Shotgun base force: **2000 → 800** (60% reduction)
3. ✅ Beam base force: **500 → 200** (60% reduction)

### Result:
- **At 0.001:** Ultra-subtle feedback (barely visible)
- **At 0.01:** Subtle feedback (light stagger)
- **At 0.1:** Moderate feedback (clear pushback)
- **At 0.3:** Strong feedback (dramatic pushback)

### Recommendation:
Try `knockbackMultiplier = 0.01` for subtle but noticeable feedback, or `0.001` for ultra-subtle.

---

**Status:** ✅ FIXED - Knockback is now much more controllable!
**Date:** 2025-10-03
**File Modified:** `Assets/scripts/CompanionAI/EnemyCompanionBehavior.cs`
