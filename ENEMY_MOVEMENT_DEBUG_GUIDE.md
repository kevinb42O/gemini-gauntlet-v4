# Enemy Movement Debug Guide 🔍

## Critical Bugs Fixed

### Bug #1: NavMeshAgent Never Enabled/Disabled ❌
**Problem:** `SetComponentsActive()` disabled CompanionMovement but never touched the NavMeshAgent
**Fix:** Added NavMeshAgent enable/disable in `SetComponentsActive()` (line 907-917)

### Bug #2: CompanionMovement Not Re-initialized ❌  
**Problem:** When enemy activates, CompanionMovement was enabled but its coroutine was stopped
**Fix:** Re-initialize CompanionMovement when enemy activates (line 896-901)

### Bug #3: Early Return Prevented Movement ❌
**Problem:** `HandleCombatMovement()` returned early for enemies (fixed earlier)
**Fix:** Removed early return, enemies now execute tactical movement

---

## How Enemy Movement Works

### Activation System
1. **Start:** All enemies are DISABLED by default (`_isActive = false`)
2. **Update:** Every `activationCheckInterval` (default 1 second), check player distance
3. **Activate:** If player within `activationRadius` (default 15000 units), enable everything
4. **Deactivate:** If player moves away, disable everything

### Movement Flow (When Active)
```
Update() → CheckActivation() → SetComponentsActive(true)
                                      ↓
                                NavMeshAgent.enabled = true
                                CompanionMovement.enabled = true
                                CompanionMovement.Initialize() ← RE-INIT!
                                      ↓
CompanionMovement.UpdateMovement() → HandleCombatMovement()
                                      ↓
                                PerformTacticalMovement()
                                RepositionInCombat()
                                Jump()
```

---

## Debug Checklist

### 1. Check Enemy Configuration
Open enemy GameObject in Inspector:

**EnemyCompanionBehavior:**
- [ ] `isEnemy` = true
- [ ] `activationRadius` = 15000 (or higher for testing)
- [ ] `enableTacticalMovement` = true
- [ ] `showDebugInfo` = true (ENABLE THIS!)

**CompanionMovement:**
- [ ] `moveWhileShooting` = true
- [ ] `jumpChance` = 0.3 (or 0 to disable jumping)
- [ ] `repositionInterval` = 1.0

**NavMeshAgent:**
- [ ] Component exists
- [ ] Speed > 0
- [ ] Angular Speed > 0
- [ ] Acceleration > 0

### 2. Check Console Logs

When you approach an enemy, you should see:

```
[EnemyCompanionBehavior] 🔍 CheckActivation() CALLED on EnemyName
[EnemyCompanionBehavior] 🔥 SetComponentsActive(True) CALLED on EnemyName!
[EnemyCompanionBehavior] 🗺️ NavMeshAgent ENABLED
[EnemyCompanionBehavior] 🔄 Re-initialized CompanionMovement after activation
[EnemyCompanionBehavior] ⚡ EnemyName ACTIVATED - H:5000, V:100
```

When enemy enters combat:

```
[EnemyCompanionBehavior] 👁️ PLAYER DETECTED at 5000 units!
[EnemyCompanionBehavior] ⚔️ Set CompanionCore to ATTACKING state - tactical movement enabled!
[CompanionMovement] 🎯 TACTICAL MOVEMENT PATTERNS
[CompanionMovement] 🦘 JUMP ROLL: 0.25 vs 0.30
[CompanionMovement] 🏃‍♂️🦘 PARKOUR JUMP while moving!
```

### 3. Check NavMesh

**In Unity Editor:**
- Window → AI → Navigation
- Click "Bake" tab
- Verify NavMesh exists in your scene (blue overlay)
- Enemy must be ON the NavMesh to move

**Test:**
1. Select enemy GameObject
2. Check if it has a NavMeshAgent component
3. In Play mode, check if `NavMeshAgent.isOnNavMesh` is true
4. Check if `NavMeshAgent.enabled` is true when player is close

### 4. Check Distance

**Default activation radius:** 15000 units

If your player is 320 units tall, 15000 units = ~47 player heights

**To test:**
1. Set `activationRadius` to 50000 (very large)
2. Set `showDebugInfo` to true
3. Play and watch console for activation messages

### 5. Manual Activation Test

Add this to `EnemyCompanionBehavior.Start()` for testing:

```csharp
// TEMPORARY TEST: Force activate immediately
SetComponentsActive(true);
_isActive = true;
if (_companionMovement != null && _companionCore != null)
{
    _companionMovement.Initialize(_companionCore);
}
Debug.Log($"[TEST] {gameObject.name} FORCE ACTIVATED!");
```

This bypasses the activation system to test if movement works at all.

---

## Common Issues

### Issue: "Enemy activates but doesn't move"

**Possible Causes:**
1. **NavMeshAgent disabled:** Check `navAgent.enabled` in Inspector during play
2. **Not on NavMesh:** Check `navAgent.isOnNavMesh` - if false, enemy can't move
3. **CompanionCore not in Attacking state:** Check `_companionCore.CurrentState`
4. **No target:** Check if `_core.PlayerTransform` is valid

**Debug:**
```csharp
// Add to EnemyCompanionBehavior.Update()
if (showDebugInfo && Time.frameCount % 60 == 0)
{
    NavMeshAgent nav = GetComponent<NavMeshAgent>();
    Debug.Log($"NavAgent: enabled={nav.enabled}, onNavMesh={nav.isOnNavMesh}, " +
              $"hasPath={nav.hasPath}, velocity={nav.velocity.magnitude:F2}");
}
```

### Issue: "Enemy never activates"

**Possible Causes:**
1. **Player too far:** Increase `activationRadius`
2. **Vertical distance too large:** Increase `maxVerticalActivationDistance`
3. **Player not found:** Check `_realPlayerTransform` is not null

**Debug:**
```csharp
// Add to EnemyCompanionBehavior.Update()
if (Time.frameCount % 60 == 0)
{
    float dist = Vector3.Distance(transform.position, _realPlayerTransform.position);
    Debug.Log($"{gameObject.name}: Distance to player: {dist:F0} / {activationRadius:F0}, Active: {_isActive}");
}
```

### Issue: "Enemy moves but doesn't use tactical movement"

**Possible Causes:**
1. **CompanionCore not in Attacking state:** Must be `Attacking` for tactical movement
2. **moveWhileShooting = false:** Check CompanionMovement settings
3. **Target is null:** Check if target is being found

**Debug:**
```csharp
// Add to CompanionMovement.HandleCombatMovement()
Debug.Log($"[Combat] State: {_core.CurrentState}, Target: {(target != null ? "FOUND" : "NULL")}, " +
          $"moveWhileShooting: {moveWhileShooting}");
```

---

## Expected Behavior

### When Player Approaches (Within activationRadius):
1. ✅ Console: "ACTIVATED" message
2. ✅ Enemy renderers appear (visible)
3. ✅ Enemy animator starts playing
4. ✅ NavMeshAgent enabled

### When Player Detected (Within playerDetectionRadius):
1. ✅ Console: "PLAYER DETECTED" message
2. ✅ Enemy starts moving toward player
3. ✅ State changes to Hunting → Attacking

### During Combat (Within attackRange):
1. ✅ Console: "Set CompanionCore to ATTACKING state"
2. ✅ Enemy strafes/circles around player
3. ✅ Enemy repositions every 0.5-1 second
4. ✅ Enemy jumps occasionally (if enabled)
5. ✅ Enemy shoots at player

### Movement Patterns:
- **Close range (<300):** Backpedal while shooting
- **Mid range (300-800):** Circle strafe
- **Long range (>800):** Aggressive advance

---

## Performance Notes

- Enemies only run AI when `_isActive = true`
- Activation check runs every `activationCheckInterval` (default 1 second)
- When inactive, ALL components are disabled (zero CPU cost)
- When active, full AI + movement + rendering

**If you have 210 enemies:**
- Only enemies near player are active
- Rest are completely disabled
- Adjust `activationRadius` to control how many are active at once

---

## Quick Test Script

Add this to test enemy movement manually:

```csharp
[ContextMenu("Force Enemy Movement Test")]
public void ForceMovementTest()
{
    // Force activate
    SetComponentsActive(true);
    _isActive = true;
    
    // Initialize movement
    if (_companionMovement != null && _companionCore != null)
    {
        _companionMovement.Initialize(_companionCore);
    }
    
    // Set to attacking state
    if (_companionCore != null)
    {
        _companionCore.SetState(CompanionCore.CompanionState.Attacking);
    }
    
    // Position fake player at real player
    if (_fakePlayerTransform != null && _realPlayerTransform != null)
    {
        _fakePlayerTransform.position = _realPlayerTransform.position;
    }
    
    Debug.Log("🧪 FORCE MOVEMENT TEST - Enemy should now move!");
}
```

Right-click the EnemyCompanionBehavior component in Inspector and select "Force Enemy Movement Test".

---

**Status:** All critical bugs fixed - enemies should now move during combat!
**Date:** 2025-10-03
**Files Modified:**
- `Assets/scripts/CompanionAI/EnemyCompanionBehavior.cs`
- `Assets/scripts/CompanionAI/CompanionMovement.cs`
