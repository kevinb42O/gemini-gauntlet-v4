# 🚫 Enemy Companion Wall-Shooting Fix

## Problem
EnemyCompanions were **shooting through walls** even though `requireLineOfSight = true` was enabled.

## Root Cause

### The Bug
The line-of-sight check was **only used for initial detection**, but once the enemy started attacking, they would **continue shooting even after losing LOS**.

### Why It Happened
1. **HuntPlayer()** - Checked LOS but only stopped movement, **NOT shooting**
2. **AttackPlayer()** - Checked LOS and switched to hunting, but **didn't stop the combat system**
3. **_isBeamActive** flag stayed true → `CompanionCombat` kept firing

### Code Flow (Before Fix)
```
Player hides behind wall
  ↓
CheckLineOfSight() returns FALSE
  ↓
AttackPlayer() switches to Hunting state
  ↓
BUT CompanionCombat.StopAttacking() NEVER CALLED!
  ↓
Enemy keeps shooting through wall 🤬
```

## The Fix

### Changes Made

**1. HuntPlayer() - Stop shooting when no LOS**
```csharp
// 🚫 CRITICAL FIX: STOP SHOOTING if no line of sight!
if (!hasLineOfSight)
{
    // NO LINE OF SIGHT - STOP ATTACKING IMMEDIATELY!
    if (_companionCombat != null && _isBeamActive)
    {
        _companionCombat.StopAttacking();
        _isBeamActive = false;
        _overrideTargeting = false;
    }
    // ... then chase logic
}
```

**2. AttackPlayer() - Stop shooting when LOS lost**
```csharp
// 🚫 CRITICAL FIX: STOP SHOOTING if no line of sight!
if (!hasLineOfSight)
{
    // LOST LINE OF SIGHT - STOP ATTACKING IMMEDIATELY!
    if (_companionCombat != null && _isBeamActive)
    {
        _companionCombat.StopAttacking();
        _isBeamActive = false;
        _overrideTargeting = false;
    }
    
    // GO BACK TO HUNTING
    SetState(EnemyState.Hunting);
    return;
}
```

### Code Flow (After Fix)
```
Player hides behind wall
  ↓
CheckLineOfSight() returns FALSE
  ↓
CompanionCombat.StopAttacking() CALLED ✅
  ↓
_isBeamActive = false ✅
  ↓
_overrideTargeting = false ✅
  ↓
Enemy stops shooting! 🎉
  ↓
Enemy chases to regain visual
```

## Your Raycast Settings Question

### "I put my LOD raycast on 45° - is this the problem?"

**No, the angle isn't the problem!** The issue was that the LOS check wasn't stopping the shooting.

### Current Raycast Settings
- **`losRaycastCount`**: Default is **1** (center ray only)
- **`losRaycastSpread`**: Default is **30 units**

### Recommended Settings

#### For Performance (Weak PC)
```
losRaycastCount = 1  (single center ray)
losRaycastSpread = 30  (doesn't matter with 1 ray)
```
- **Pros**: Fastest, minimal CPU cost
- **Cons**: Can miss player if they're partially behind cover

#### For Accuracy (Better PC)
```
losRaycastCount = 3  (center + left + right)
losRaycastSpread = 30-50  (spread around player)
```
- **Pros**: More accurate, harder to cheese by peeking
- **Cons**: 3x raycast cost

#### For Maximum Accuracy (Strong PC)
```
losRaycastCount = 5  (center + left + right + up + down)
losRaycastSpread = 30-50
```
- **Pros**: Very accurate, covers player's full body
- **Cons**: 5x raycast cost

### How Raycast Spread Works

With `losRaycastCount = 3` and `losRaycastSpread = 30`:
```
Enemy → [Ray 1: Center]    → Player
Enemy → [Ray 2: +30 right] → Player's right side
Enemy → [Ray 3: -30 left]  → Player's left side
```

**If ANY ray hits the player directly**, LOS is confirmed!

### Your 45° Setting
If you set `losRaycastSpread = 45`, the rays spread **45 units** around the player (not 45 degrees). This is fine! Player is 50 units wide, so 45 is good coverage.

## Inspector Settings

### For Your Weak PC (Recommended)
```
✅ Require Line Of Sight: TRUE
🎯 Los Raycast Count: 1
📏 Los Raycast Spread: 30 (doesn't matter with 1 ray)
🔍 Detection Interval: 1.0s
⚡ Activation Check Interval: 2.0s
```

### If You Want More Accuracy
```
✅ Require Line Of Sight: TRUE
🎯 Los Raycast Count: 3
📏 Los Raycast Spread: 45
🔍 Detection Interval: 1.0s
⚡ Activation Check Interval: 2.0s
```

## Testing

### How to Test
1. **Stand in open area** → Enemy should shoot you ✅
2. **Hide behind wall** → Enemy should STOP shooting immediately ✅
3. **Peek around corner** → Enemy should resume shooting ✅
4. **Run behind cover** → Enemy should chase but not shoot ✅

### Debug Visualization
Enable `showDebugInfo = true` and `showAimDebug = true` to see:
- **Green rays** = Clear line of sight
- **Red rays** = Blocked by wall
- **Yellow rays** = Raycast missed (shouldn't happen)

## Files Modified
- `Assets/scripts/CompanionAI/EnemyCompanionBehavior.cs`
  - Line 1755-1769: Added LOS check and stop shooting in `HuntPlayer()`
  - Line 1849-1867: Added LOS check and stop shooting in `AttackPlayer()`

## Performance Impact
- ✅ **No performance cost** - we're just calling existing methods
- ✅ **Actually IMPROVES performance** - enemies stop shooting sooner
- ✅ **Fewer particles spawned** when behind cover

## Notes
- The fix works with **any raycast count** (1, 3, or 5)
- More raycasts = more accurate but more expensive
- For weak PCs, stick with `losRaycastCount = 1`
- The angle spread is in **units**, not degrees
- `lineOfSightBlockers` LayerMask determines what blocks vision (walls, obstacles)
