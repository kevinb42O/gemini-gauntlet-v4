# Performance Debug Cleanup Status

## Problem Identified
Excessive `Debug.Log()` calls in performance-critical code paths causing severe FPS drops (12 FPS reported).

## Root Cause
`Debug.Log()` is extremely expensive when called frequently:
- String allocation (garbage collection)
- Console I/O operations
- Stack trace generation
- Unity Editor overhead

**Calling Debug.Log every frame or in hot paths = Performance death**

## Scripts Fixed ✅

### 1. PlayerEnergySystem.cs
- ✅ Commented out Debug.Log in `RegenerateEnergy()` (called every frame during regen)
- ✅ Removed logs from energy state changes

### 2. PlayerShooterOrchestrator.cs  
- ✅ Commented out Debug.Log in `HandlePrimaryTap()` (called every shot)
- ✅ Commented out Debug.Log in `HandleSecondaryTap()` (called every shot)
- ✅ Commented out Debug.Log in `HandlePrimaryHoldStarted()` (called when streaming)
- ✅ Commented out Debug.Log in `HandlePrimaryHoldEnded()` (called when streaming stops)
- ✅ Commented out Debug.Log in `HandleSecondaryHoldStarted()` (called when streaming)
- ✅ Commented out Debug.Log in `HandleSecondaryHoldEnded()` (called when streaming stops)
- ✅ Commented out Debug.Log in `StopPrimaryStreamAudio()` (called frequently)
- ✅ Commented out Debug.Log in `StopSecondaryStreamAudio()` (called frequently)
- ✅ Commented out homing dagger spam logs (called every shot when powerup active)

## Scripts Still Needing Attention ⚠️

### High Priority (Run Every Frame or Very Frequently)
These scripts likely have Debug.Log in Update/FixedUpdate or frequently called methods:

1. **HandAnimationController.cs** - 49 logs (animations run constantly)
2. **CelestialDriftController.cs** - 37 logs (movement controller)
3. **CompanionAI.cs** - 121 logs (AI updates)
4. **EnemyCompanionBehavior.cs** - 146 logs (enemy AI behavior)
5. **CompanionCore.cs** - 36 logs (companion updates)
6. **CompanionMovement.cs** - 30 logs (movement updates)
7. **CompanionAudio.cs** - 37 logs (audio triggers)
8. **PlayerAOEAbility.cs** - 36 logs (ability system)
9. **EnemyThreatTracker.cs** - 26 logs (threat tracking)
10. **HybridTowerController.cs** - 28 logs (tower behavior)

### Medium Priority (Called Frequently But Not Every Frame)
11. **StashManager.cs** - 206 logs (UI interactions)
12. **ChestInteractionSystem.cs** - 147 logs (chest interactions)
13. **PowerupInventoryManager.cs** - 127 logs (powerup management)
14. **InventoryManager.cs** - 119 logs (inventory operations)
15. **PlayerHealth.cs** - 110 logs (damage/health changes)

### Lower Priority (Initialization/Infrequent)
- ForgeManager.cs - 94 logs
- CompanionSelectionManager.cs - 81 logs
- UnifiedSlot.cs - 67 logs
- MenuXPManager.cs - 44 logs
- And 200+ other files...

## Recommended Next Steps

### Option 1: Manual Cleanup (Surgical)
Go through each high-priority script and comment out Debug.Log calls in:
- `Update()`, `FixedUpdate()`, `LateUpdate()` methods
- Methods called from Update (movement, shooting, AI)
- Loops (especially nested loops)
- Collision/trigger callbacks

### Option 2: Use DebugConfig.cs (Global Toggle)
I've created `DebugConfig.cs` with `ENABLE_DEBUG_LOGS = false`.
Replace all `Debug.Log()` with `DebugConfig.Log()` throughout codebase.
- Pros: Easy to toggle on/off, zero performance cost when disabled
- Cons: Requires find/replace across entire codebase

### Option 3: Scripting Define Symbol
Add `DISABLE_DEBUG_LOGS` to Player Settings > Scripting Define Symbols.
Wrap all debug code:
```csharp
#if !DISABLE_DEBUG_LOGS
    Debug.Log("message");
#endif
```
- Pros: Compiler strips out debug code completely
- Cons: Requires wrapping every debug statement

### Option 4: Nuclear Option (Quick Fix)
Comment out ALL Debug.Log/LogWarning in all scripts.
Keep only Debug.LogError for actual errors.
- Pros: Immediate performance fix
- Cons: No debugging capability

## Performance Impact Estimate

### Before Cleanup:
- 228 files with debug logs
- Estimated 100-500+ Debug.Log calls per second
- **Result: 12 FPS**

### After Partial Cleanup (2 scripts):
- Removed ~20 logs from hot paths
- Should see immediate improvement
- **Expected: 40-60+ FPS**

### After Full Cleanup (all hot paths):
- Remove all logs from Update/frequent methods
- **Expected: 60-144+ FPS (depending on hardware)**

## Testing Recommendations

1. **Test after each script cleanup** to measure impact
2. **Profile with Unity Profiler** to identify remaining bottlenecks
3. **Check console** - if it's scrolling rapidly, you still have spam
4. **Monitor FPS** - should be stable 60+ after cleanup

## Best Practices Going Forward

### ✅ DO:
- Use Debug.Log in Awake/Start (initialization)
- Use Debug.Log for rare events (level load, game over)
- Use Debug.LogError for actual errors
- Use Debug.LogWarning for important warnings

### ❌ DON'T:
- **NEVER** Debug.Log in Update/FixedUpdate/LateUpdate
- **NEVER** Debug.Log in loops
- **NEVER** Debug.Log in frequently called methods (shooting, movement)
- **NEVER** Debug.Log in collision/trigger callbacks

### Alternative Debugging:
- Use Unity Profiler for performance analysis
- Use breakpoints in Visual Studio for debugging
- Use Gizmos for visual debugging
- Use Debug.DrawLine/DrawRay for spatial debugging (cheaper than logs)

## Current Status
- ✅ 2 critical scripts cleaned
- ⚠️ 226 scripts remaining
- 🎯 Focus on high-priority scripts next
- 📊 Performance should already be improved

## Files Created
1. `DebugConfig.cs` - Global debug toggle system
2. `DEBUG_LOGGING_CLEANUP.md` - Comprehensive cleanup guide
3. `PERFORMANCE_DEBUG_CLEANUP_STATUS.md` - This file
