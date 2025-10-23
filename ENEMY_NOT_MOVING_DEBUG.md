# 🐛 Enemy Not Moving - Debug Guide

## I've Added Debug Logging

The enemy will now log information every few seconds to help diagnose the issue.

---

## 🔍 What to Check

### 1. Enable Debug Logs
```
EnemyCompanionBehavior:
└─ Show Debug Info: TRUE ✅
```

### 2. Run Game and Check Console

You should see these logs every ~5 seconds:

```
[EnemyCompanionBehavior] State: Patrolling, PlayerDetected: False, Patrolling: True
[EnemyCompanionBehavior] 📏 Distance to player: 15000 / 25000
```

---

## 📊 Diagnostic Scenarios

### Scenario 1: "Real player transform is NULL"
```
[EnemyCompanionBehavior] ❌ Real player transform is NULL!
```
**Problem:** Can't find player  
**Fix:** Make sure player has `AAAMovementController` component

### Scenario 2: Distance Too Large
```
[EnemyCompanionBehavior] 📏 Distance to player: 50000 / 25000
```
**Problem:** Player is too far away (50000 > 25000)  
**Fix:** Either get closer OR increase `playerDetectionRadius` to 50000+

### Scenario 3: LOS Check Failing
```
[EnemyCompanionBehavior] 📏 Distance to player: 15000 / 25000
[EnemyCompanionBehavior] 👁️ LOS Check: False
```
**Problem:** Line of sight blocked by walls  
**Fix:** 
- Set `requireLineOfSight = FALSE` (temporary test)
- OR check `lineOfSightBlockers` layer mask
- OR make sure walls are on correct layer

### Scenario 4: State is Idle
```
[EnemyCompanionBehavior] State: Idle, PlayerDetected: False, Patrolling: False
```
**Problem:** Not patrolling  
**Fix:** 
- Check `enablePatrol = TRUE`
- Check `patrolPoints` are assigned OR `randomPatrolRadius` is set

### Scenario 5: No Logs At All
**Problem:** Script not running  
**Fix:**
- Check `isEnemy = TRUE`
- Check enemy is not dead
- Check script is enabled

---

## 🔧 Quick Fixes to Try

### Fix 1: Disable Line of Sight (Test)
```
EnemyCompanionBehavior:
└─ Require Line Of Sight: FALSE
```
**This will make enemy detect you through walls - good for testing**

### Fix 2: Increase Detection Range
```
EnemyCompanionBehavior:
└─ Player Detection Radius: 50000 (or higher)
```

### Fix 3: Check NavMeshAgent
```
Enemy GameObject:
└─ NavMeshAgent component:
   ├─ Enabled: TRUE
   ├─ Speed: > 0
   └─ Stopping Distance: < 1000
```

### Fix 4: Check Patrol Setup
```
EnemyCompanionBehavior:
├─ Enable Patrol: TRUE
├─ Patrol Points: Assign some OR
└─ Random Patrol Radius: 8000
```

---

## 🎯 Most Likely Issues

### Issue #1: Scale Problem
Your game uses 320-unit tall player. Detection might be:
- Too small radius
- Wrong eye height
- LOS blocked

**Test:** Set `playerDetectionRadius = 50000` and `requireLineOfSight = FALSE`

### Issue #2: Layer Mask Wrong
`lineOfSightBlockers` might be blocking the player itself!

**Test:** Set `requireLineOfSight = FALSE`

### Issue #3: NavMesh Not Baked
Enemy can't move without NavMesh!

**Fix:** 
1. Window → AI → Navigation
2. Bake NavMesh for your scene

---

## 📝 Debug Checklist

Run game and check console for:

- [ ] "State: Patrolling" (should be patrolling)
- [ ] "Distance to player: XXXX / XXXX" (shows detection)
- [ ] Distance is LESS than detection radius
- [ ] "LOS Check: True" (if requireLineOfSight is true)
- [ ] "PLAYER DETECTED" when you get close
- [ ] "Hunting player" when detected

**Copy the console logs and let me know what you see!**

---

## 🚀 Quick Test Setup

### Guaranteed to Work Setup:
```
EnemyCompanionBehavior:
├─ Is Enemy: TRUE
├─ Player Detection Radius: 50000 (huge!)
├─ Attack Range: 10000
├─ Require Line Of Sight: FALSE (disable for test)
├─ Enable Patrol: TRUE
├─ Random Patrol Radius: 8000
├─ Show Debug Info: TRUE
└─ Enable Tactical Movement: FALSE (for testing)
```

**This should make enemy detect and chase you no matter what!**
