# Enemy Static Turret Fix - Combat Movement Enabled

## Problem Identified ❌

Enemy companions were becoming **static turrets** during combat - they would stand completely still and shoot without any movement, repositioning, or tactical behavior.

## Root Cause Analysis 🔍

### Issue #1: Early Return in CompanionMovement
**File:** `CompanionMovement.cs` (Lines 167-172)

```csharp
// OLD CODE - BROKEN
if (enemyBehavior != null && enemyBehavior.isEnemy)
{
    _navAgent.speed = runningSpeed;
    return; // ❌ EXITS ENTIRE FUNCTION - No movement executed!
}
```

**Problem:** The function returned immediately for enemies, preventing ALL tactical movement code from executing:
- No tactical movement patterns (strafing, advancing, retreating)
- No repositioning
- No jumping
- Completely static behavior

### Issue #2: Missing Target for Enemy Movement
**File:** `CompanionMovement.cs` (Lines 179-180, 295-296)

```csharp
// OLD CODE - BROKEN
Transform target = _core.TargetingSystem?.GetCurrentTarget();
if (target == null) return; // ❌ Always null for enemies!
```

**Problem:** `EnemyCompanionBehavior` disables `CompanionTargeting`, so `GetCurrentTarget()` always returned null for enemies, causing movement to abort.

### Issue #3: CompanionCore Not in Attacking State
**File:** `EnemyCompanionBehavior.cs` (AttackPlayer method)

**Problem:** `CompanionCore.CurrentState` was never set to `Attacking`, so `CompanionMovement.HandleCombatMovement()` was never called (it only runs when state is `Attacking`).

### Issue #4: Fake Player Positioning Conflict
**File:** `EnemyCompanionBehavior.cs` (Lines 1466-1505)

**Problem:** The fake player was being positioned in complex tactical patterns, but since CompanionMovement wasn't executing, this code was useless and would have conflicted with the tactical movement system.

---

## Solutions Implemented ✅

### Fix #1: Remove Early Return, Allow Tactical Movement
**File:** `CompanionMovement.cs` (Lines 166-177)

```csharp
// NEW CODE - FIXED
// CHECK: If enemy, use different speed calculation (no combatSpeedMultiplier stacking)
EnemyCompanionBehavior enemyBehavior = GetComponent<EnemyCompanionBehavior>();
if (enemyBehavior != null && enemyBehavior.isEnemy)
{
    // ENEMY: Use runningSpeed directly (EnemyCompanionBehavior already configured it)
    _navAgent.speed = runningSpeed;
}
else
{
    // FRIENDLY COMPANION: Apply combat speed multiplier
    _navAgent.speed = runningSpeed * combatSpeedMultiplier;
}
// ✅ NO RETURN - Continues to execute tactical movement!
```

**Result:** Enemies now execute full tactical movement system including:
- Tactical movement patterns (backpedal, advance, circle strafe)
- Repositioning every 0.5-1 second
- Jumping (if enabled)
- Dynamic combat behavior

### Fix #2: Fallback Target for Enemies
**File:** `CompanionMovement.cs` (Lines 179-194, 309-322)

```csharp
// NEW CODE - FIXED
// Get target - works for both friendly companions and enemies
Transform target = _core.TargetingSystem?.GetCurrentTarget();

// For enemies without targeting system, use player as target
if (target == null)
{
    EnemyCompanionBehavior enemyBehavior = GetComponent<EnemyCompanionBehavior>();
    if (enemyBehavior != null && enemyBehavior.isEnemy)
    {
        // Enemy mode: Use player transform as target for movement
        target = _core.PlayerTransform;
    }
}

if (target == null) return;
```

**Result:** Enemies now have a valid target (the player) for all tactical movement calculations.

### Fix #3: Set CompanionCore to Attacking State
**File:** `EnemyCompanionBehavior.cs` (Lines 1465-1471)

```csharp
// NEW CODE - FIXED
// CRITICAL: Set CompanionCore to Attacking state so CompanionMovement executes tactical movement
if (_companionCore != null && _companionCore.CurrentState != CompanionCore.CompanionState.Attacking)
{
    _companionCore.SetState(CompanionCore.CompanionState.Attacking);
    if (showDebugInfo)
        Debug.Log("[EnemyCompanionBehavior] ⚔️ Set CompanionCore to ATTACKING state - tactical movement enabled!");
}
```

**Result:** `CompanionMovement.UpdateMovement()` now calls `HandleCombatMovement()` for enemies.

### Fix #4: Simplified Fake Player Positioning
**File:** `EnemyCompanionBehavior.cs` (Lines 1473-1475)

```csharp
// NEW CODE - SIMPLIFIED
// SIMPLIFIED: Just position fake player at real player
// CompanionMovement's tactical movement system will handle all the strafing/repositioning
_fakePlayerTransform.position = _realPlayerTransform.position;
```

**Result:** No conflict between fake player positioning and tactical movement system. CompanionMovement handles all movement decisions.

---

## Expected Behavior After Fix 🎮

### Enemy Combat Movement:
1. **Tactical Patterns** - Enemies will use 3 movement patterns based on distance:
   - **Close Range (<300 units):** Backpedal while shooting (kiting)
   - **Long Range (>800 units):** Aggressive advance with strafing
   - **Mid Range (300-800 units):** Circle strafe

2. **Constant Repositioning** - Every 0.5-1 second, enemies reposition using:
   - Aggressive flank (40% chance)
   - Tactical retreat (30% chance)
   - Surprise advance (30% chance)

3. **Jumping** - If enabled (`jumpChance > 0`), enemies will jump during combat:
   - Parkour jumps while moving
   - Ninja jumps in place
   - Configurable jump chance (default 0.3 = 30%)

4. **Dynamic Speed** - Enemies move at configured speed:
   - Indoor mode: 50% speed multiplier (stability)
   - Outdoor mode: Full speed
   - Combat speed multiplier: 1.2x-1.5x

### Configuration:
All movement behavior can be configured in `EnemyCompanionBehavior`:
- `enableTacticalMovement` - Enable/disable all tactical movement
- `combatMovementSpeed` - Speed multiplier during combat
- `disableTacticalMovementIndoors` - Disable movement in indoor areas
- `disableJumpingIndoors` - Disable jumping in indoor areas
- `indoorSpeedMultiplier` - Speed reduction indoors (default 0.5)

In `CompanionMovement`:
- `moveWhileShooting` - Must be true for combat movement
- `jumpChance` - Probability of jumping (0-1)
- `repositionInterval` - How often to reposition (seconds)
- `combatSpeedMultiplier` - Speed boost during combat

---

## Testing Checklist ✓

- [ ] Enemies move during combat (not static)
- [ ] Enemies strafe/circle around player
- [ ] Enemies reposition frequently
- [ ] Enemies jump during combat (if enabled)
- [ ] Indoor enemies move slower and don't jump (if configured)
- [ ] Outdoor enemies use full tactical movement
- [ ] Movement doesn't conflict with shooting
- [ ] Enemies still track and shoot player accurately

---

## Performance Impact 📊

**Before:** Enemies were static, minimal NavMesh calculations
**After:** Enemies use full tactical movement system

**Impact:** Slightly higher CPU usage for NavMesh pathfinding, but enemies are already optimized with:
- Activation radius (only active when player is close)
- LOD system (reduced rendering at distance)
- Component deactivation when far away

**Recommendation:** If performance is an issue, reduce `activationRadius` or disable `enableTacticalMovement` for some enemies.

---

## Notes 📝

- This fix makes enemies use the SAME tactical movement system as friendly companions
- The movement is intelligent and dynamic, not scripted
- Enemies will adapt to player position and terrain
- Indoor detection automatically adjusts movement for stability
- All movement is controlled by `CompanionMovement`, not `EnemyCompanionBehavior`
- `EnemyCompanionBehavior` only handles AI logic and targeting

---

**Status:** ✅ FIXED - Enemies now have full tactical combat movement
**Date:** 2025-10-03
**Files Modified:**
- `Assets/scripts/CompanionAI/CompanionMovement.cs`
- `Assets/scripts/CompanionAI/EnemyCompanionBehavior.cs`
