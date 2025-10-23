# Companion Animator Performance Fix

## Problem
All 210 companion hand animators were playing simultaneously, causing massive performance issues. The activation system was enabling companions on all floors instead of just nearby ones.

## Root Causes

### 1. **Activation Radius Too Large**
- **Old value**: `activationRadius = 30000f` (30,000 units)
- **New value**: `activationRadius = 15000f` (15,000 units)
- This was activating companions across the entire building

### 2. **Vertical Distance Check Too Generous**
- **Old value**: Hard-coded `1000f` units vertical tolerance
- **New value**: Configurable `maxVerticalActivationDistance = 500f`
- The old check allowed companions on BASEMENT, Ground Floor, and FIRST FLOOR to all activate simultaneously

### 3. **Animators Not Properly Disabled**
- Animators were being disabled but not reset
- `CompanionHandAnimator` was still trying to play animations on disabled animators

## Changes Made

### EnemyCompanionBehavior.cs

#### 1. Added Configurable Vertical Distance Check
```csharp
[Tooltip("Maximum vertical distance to activate (prevents activating enemies on different floors)")]
[Range(100f, 2000f)] public float maxVerticalActivationDistance = 500f;
```

#### 2. Reduced Default Activation Radius
```csharp
public float activationRadius = 15000f; // Was 30000f
```

#### 3. Updated Activation Logic
```csharp
bool shouldBeActive = horizontalDistance <= activationRadius && verticalDistance <= maxVerticalActivationDistance;
```

#### 4. Added Animator Reset on Disable
```csharp
if (!active)
{
    animator.Rebind(); // Reset to default state
    animator.Update(0f); // Force update to apply reset
}
```

### CompanionHandAnimator.cs

#### Added Animator Enabled Check
```csharp
// CRITICAL: Check if animator is enabled AND active in hierarchy
if (animator == null || !animator.enabled || !animator.gameObject.activeInHierarchy) return;
```

This prevents the script from trying to play animations on disabled animators.

## Performance Impact

### Before
- **210 animators playing** (all companions across all floors)
- Massive CPU usage from animation updates
- Severe frame rate drops

### After
- **Only nearby companions active** (within 15,000 units horizontally AND 500 units vertically)
- Disabled companions have animators fully reset and stopped
- Massive performance improvement

## Tuning Recommendations

You can adjust these values in the Inspector for each companion:

1. **activationRadius** (5000-50000): How far horizontally before activating
   - Lower = better performance, but companions may "pop in"
   - Higher = smoother experience, but more active at once

2. **maxVerticalActivationDistance** (100-2000): Vertical tolerance for activation
   - Lower = only same floor (better performance)
   - Higher = multiple floors (worse performance)

3. **activationCheckInterval** (0.5-5): How often to check distance
   - Higher = better performance, but slower response
   - Lower = more responsive, but more CPU usage

## Testing

Run the game and check:
1. Only companions on your current floor should be active
2. Companions far away should have disabled animators
3. Frame rate should be significantly improved
4. No more spam of activation logs (unless you're near many companions)
