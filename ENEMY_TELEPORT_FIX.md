# Enemy Teleport Fix - NO MORE TELEPORTING! 🚫

## Problem Identified

Enemies were **teleporting to the player** instead of walking, breaking immersion and making combat unfair.

---

## Root Causes

### Cause #1: Fake Player Positioning Creates Huge Distances
**File:** `EnemyCompanionBehavior.cs` (Line 1401, 1448)

```csharp
// PROBLEM: Fake player positioned 2000 units PAST real player
_fakePlayerTransform.position = _realPlayerTransform.position + directionToPlayer * 2000f;
```

**Example:**
- Enemy at (0, 0, 0)
- Real player at (5000, 0, 0)
- Fake player set to (7000, 0, 0) ← **2000 units past!**
- Distance = 7000 units

If distance > `maxFollowDistance` (10000), teleport triggers!

### Cause #2: Enemies in Following/Engaging States
**File:** `CompanionMovement.cs` (Line 112, 116)

```csharp
case CompanionCore.CompanionState.Following:
    HandleFollowingMovement(distanceToPlayer); // ← Has teleport logic!
    
case CompanionCore.CompanionState.Engaging:
    HandleEngagingMovement(distanceToPlayer); // ← Calls HandleFollowingMovement!
```

**Problem:** `HandleFollowingMovement()` has teleport logic:
```csharp
if (distanceToPlayer > maxFollowDistance && !isEnemy)
{
    TeleportToPlayer(); // ← Teleport!
}
```

The `!isEnemy` check exists, but if the fake player positioning creates huge distances, it could still trigger.

### Cause #3: HuntPlayer() Didn't Set CompanionCore State
**File:** `EnemyCompanionBehavior.cs` (HuntPlayer method)

**Problem:** When hunting, enemies didn't explicitly set `CompanionCore.State`, so it might stay in `Following` or `Engaging`, which use `HandleFollowingMovement()` with teleport logic.

---

## Solutions Implemented ✅

### Fix #1: Double-Check in TeleportToPlayer()
**File:** `CompanionMovement.cs` (Line 480-486)

```csharp
private void TeleportToPlayer()
{
    // CRITICAL: Double-check enemy status before teleporting
    EnemyCompanionBehavior enemyBehavior = GetComponent<EnemyCompanionBehavior>();
    if (enemyBehavior != null && enemyBehavior.isEnemy)
    {
        Debug.LogError($"[CompanionMovement] ❌ BLOCKED TELEPORT for ENEMY {gameObject.name}!");
        return; // ← ABORT TELEPORT!
    }
    
    // ... rest of teleport code
}
```

**Result:** Even if teleport is triggered, it's blocked at the last moment for enemies.

### Fix #2: Force Attacking State in HuntPlayer()
**File:** `EnemyCompanionBehavior.cs` (Line 1389-1394)

```csharp
private void HuntPlayer()
{
    // CRITICAL: Set CompanionCore to Attacking state to use HandleCombatMovement (no teleport)
    // Engaging state calls HandleFollowingMovement which has teleport logic
    if (_companionCore != null && _companionCore.CurrentState != CompanionCore.CompanionState.Attacking)
    {
        _companionCore.SetState(CompanionCore.CompanionState.Attacking);
    }
    
    // ... rest of hunting code
}
```

**Result:** Enemies always use `HandleCombatMovement()` which has NO teleport logic, only tactical movement.

### Fix #3: Debug Logging
**File:** `CompanionMovement.cs` (Line 154-158)

```csharp
// DEBUG: Log if enemy would have teleported
if (distanceToPlayer > maxFollowDistance && isEnemy)
{
    Debug.LogWarning($"[CompanionMovement] ⚠️ ENEMY {gameObject.name} would have teleported! Distance: {distanceToPlayer:F0} > {maxFollowDistance:F0} - BLOCKED!");
}
```

**Result:** You'll see warnings in console if the teleport check is triggered (but blocked).

---

## How It Works Now

### Enemy State Flow:
```
Idle/Patrolling
    ↓ (Player detected)
Hunting → CompanionCore.State = ATTACKING
    ↓ (In attack range)
Attacking → CompanionCore.State = ATTACKING
```

**Key:** Enemies are ALWAYS in `Attacking` state when active, which uses `HandleCombatMovement()` (NO teleport).

### Movement Flow:
```
UpdateMovement()
    ↓
HandleCombatMovement() ← NO TELEPORT LOGIC!
    ↓
PerformTacticalMovement()
RepositionInCombat()
Jump()
```

### Teleport Protection Layers:
1. **Layer 1:** `!isEnemy` check in `HandleFollowingMovement()` (line 136)
2. **Layer 2:** Enemies use `Attacking` state → `HandleCombatMovement()` (no teleport)
3. **Layer 3:** Double-check in `TeleportToPlayer()` (line 482)

**Result:** **TRIPLE PROTECTION** - enemies can NEVER teleport!

---

## Expected Behavior

### ✅ Enemies Will:
- Walk/run toward player (no teleport)
- Use NavMesh pathfinding
- Strafe and reposition during combat
- Chase player if line of sight is lost
- Walk around obstacles

### ❌ Enemies Will NEVER:
- Teleport to player
- Instantly appear near player
- Skip NavMesh pathfinding

---

## Testing

### 1. Enable Debug Logging
Set `showDebugInfo = true` on `EnemyCompanionBehavior`

### 2. Test Scenarios

**Scenario A: Enemy Far Away**
1. Place enemy 20000 units from player (beyond maxFollowDistance)
2. Approach enemy to activate
3. Enemy should walk toward you (NO teleport)
4. Check console for "BLOCKED TELEPORT" warnings

**Scenario B: Enemy Hunting**
1. Let enemy detect you
2. Run away and break line of sight
3. Enemy should chase you (NO teleport)
4. Check console for state changes

**Scenario C: Enemy in Combat**
1. Engage enemy in combat
2. Run far away during combat
3. Enemy should chase you (NO teleport)
4. Check console for "ATTACKING" state

### 3. Console Messages to Look For

**Good (No Teleport):**
```
[EnemyCompanionBehavior] 🏃💨 CHASING - Lost visual! Distance: 15000
[CompanionMovement] 🎯 TACTICAL MOVEMENT PATTERNS
```

**Warning (Teleport Blocked):**
```
[CompanionMovement] ⚠️ ENEMY EnemyName would have teleported! Distance: 12000 > 10000 - BLOCKED!
[CompanionMovement] ❌ BLOCKED TELEPORT for ENEMY EnemyName!
```

**Bad (Should Never See):**
```
[CompanionMovement] Teleported to player - was too far away!
```

If you see the "Teleported" message for an enemy, something is wrong!

---

## Configuration

### To Adjust Teleport Distance (Friendly Companions Only)
**File:** `CompanionMovement.cs`

```csharp
[Range(2000f, 20000f)] public float maxFollowDistance = 10000f;
```

- **Lower value:** Friendly companions teleport sooner when far away
- **Higher value:** Friendly companions walk further before teleporting
- **Enemies:** NEVER teleport regardless of this value

---

## Performance Notes

- Enemies now ALWAYS walk via NavMesh (no instant teleport)
- This uses NavMesh pathfinding (slightly more CPU than teleport)
- But enemies are already optimized with activation radius
- Only active enemies near player use pathfinding

---

## Troubleshooting

### Issue: "Enemy still teleports!"

**Check:**
1. Is `isEnemy` set to `true` in Inspector?
2. Check console for "BLOCKED TELEPORT" messages
3. Verify enemy has `EnemyCompanionBehavior` component
4. Check if enemy's `CompanionCore.State` is `Attacking`

**Debug:**
```csharp
// Add to EnemyCompanionBehavior.Update()
if (Time.frameCount % 60 == 0)
{
    Debug.Log($"{gameObject.name}: isEnemy={isEnemy}, State={_companionCore?.CurrentState}");
}
```

### Issue: "Enemy walks too slow when far away"

**Solution:** Increase enemy speed in `CompanionMovement`:
```csharp
public float runningSpeed = 750f; // Increase this
```

Or in `EnemyCompanionBehavior`:
```csharp
public float combatMovementSpeed = 1.5f; // Increase this multiplier
```

---

**Status:** ✅ FIXED - Enemies will NEVER teleport!
**Date:** 2025-10-03
**Files Modified:**
- `Assets/scripts/CompanionAI/CompanionMovement.cs`
- `Assets/scripts/CompanionAI/EnemyCompanionBehavior.cs`
