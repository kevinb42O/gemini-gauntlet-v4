# Debug Logging Cleanup Guide

## Problem
Your game has **excessive debug logging** across 228+ files causing severe performance issues (12 FPS drops).

## Immediate Fix - Scripts with Most Debug Logs

### Top 10 Worst Offenders (by debug log count):
1. **StashManager.cs** - 206 logs
2. **ChestInteractionSystem.cs** - 147 logs  
3. **EnemyCompanionBehavior.cs** - 146 logs
4. **PowerupInventoryManager.cs** - 127 logs
5. **CompanionAI.cs** - 121 logs
6. **InventoryManager.cs** - 119 logs
7. **PersistentItemInventoryManager.cs** - 114 logs
8. **PlayerHealth.cs** - 110 logs
9. **ForgeManager.cs** - 94 logs
10. **CompanionSelectionManager.cs** - 81 logs

## Critical Performance Impact Scripts
These run every frame or very frequently:
- **PlayerShooterOrchestrator.cs** - 66 logs (fires every shot)
- **HandAnimationController.cs** - 49 logs (every frame)
- **CelestialDriftController.cs** - 37 logs (movement, every frame)
- **CompanionAI.cs** - 121 logs (AI updates)
- **EnemyCompanionBehavior.cs** - 146 logs (enemy AI)
- **CompanionCore.cs** - 36 logs (companion updates)
- **CompanionMovement.cs** - 30 logs (movement updates)
- **CompanionAudio.cs** - 37 logs (audio triggers)

## Solution Options

### Option 1: Quick Nuclear Option (RECOMMENDED)
Comment out ALL Debug.Log/LogWarning in performance-critical scripts:
- Any script with Update(), FixedUpdate(), LateUpdate()
- Any script called from Update (shooting, movement, AI)
- Keep only Debug.LogError for actual errors

### Option 2: Use DebugConfig.cs (Created)
Replace `Debug.Log()` with `DebugConfig.Log()` throughout your codebase.
- Set `ENABLE_DEBUG_LOGS = false` in DebugConfig.cs
- All logs disabled with zero performance cost
- Easy to re-enable for debugging specific issues

### Option 3: Scripting Define Symbol
Add `DISABLE_DEBUG_LOGS` to Player Settings > Scripting Define Symbols
Then wrap all debug code:
```csharp
#if !DISABLE_DEBUG_LOGS
    Debug.Log("message");
#endif
```

## Performance Impact of Debug.Log

### Why Debug.Log Kills Performance:
1. **String allocation** - Creates garbage every call
2. **Console writing** - Expensive I/O operation  
3. **Stack trace generation** - Captures call stack
4. **Editor overhead** - Unity console processing

### Example Impact:
- 1 Debug.Log per frame = ~60 logs/second = Minor impact
- 10 Debug.Logs per frame = ~600 logs/second = Noticeable lag
- 100 Debug.Logs per frame = ~6000 logs/second = **Game unplayable**

With 228 files containing debug logs, you likely have **hundreds of logs per frame**.

## Immediate Action Required

### Scripts to Clean First (Update/Frame-based):
1. PlayerShooterOrchestrator.cs - Remove logs from Fire methods
2. HandAnimationController.cs - Remove logs from Update
3. CelestialDriftController.cs - Remove logs from movement
4. CompanionAI.cs - Remove logs from AI updates
5. EnemyCompanionBehavior.cs - Remove logs from behavior updates
6. PlayerEnergySystem.cs - ✅ Already fixed

## Best Practices Going Forward

### When to Use Debug.Log:
- ✅ Initialization (Awake, Start) - once per object
- ✅ State changes - infrequent events
- ✅ Error conditions - when something goes wrong
- ❌ **NEVER in Update/FixedUpdate/LateUpdate**
- ❌ **NEVER in frequently called methods** (shooting, movement, collision)
- ❌ **NEVER in loops** (especially nested loops)

### Use Debug.LogError Instead:
For actual errors that need attention, use `Debug.LogError` - it's more visible and indicates real problems.

## Quick Cleanup Commands

### Find all Update methods with Debug.Log:
Search pattern: `void Update.*\n.*Debug.Log`

### Find all FixedUpdate methods with Debug.Log:
Search pattern: `void FixedUpdate.*\n.*Debug.Log`

### Find Debug.Log in loops:
Search pattern: `for.*\n.*Debug.Log|while.*\n.*Debug.Log`

## Status
- ✅ PlayerEnergySystem.cs - Debug logs commented out
- ⏳ 227 other files need review
- 🎯 Priority: Scripts with Update/FixedUpdate/LateUpdate methods
