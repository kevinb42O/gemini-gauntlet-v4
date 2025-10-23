# ROOT CAUSE: HAND ASSIGNMENTS IN UNITY INSPECTOR

## THE REAL PROBLEM

Your `PlayerShooterOrchestrator` component in Unity Inspector has **BACKWARDS hand assignments**!

### Current (WRONG) Assignment:
```
PlayerShooterOrchestrator:
  ├─ primaryHandMechanics → RIGHT hand GameObject ❌
  └─ secondaryHandMechanics → LEFT hand GameObject ❌
```

### Should Be:
```
PlayerShooterOrchestrator:
  ├─ primaryHandMechanics → LEFT hand GameObject ✅
  └─ secondaryHandMechanics → RIGHT hand GameObject ✅
```

---

## WHY THIS CAUSES THE BUG

### When you shoot LEFT hand (LMB):
1. Input system fires `OnPrimaryTapAction`
2. `PlayerShooterOrchestrator.HandlePrimaryTap()` is called
3. It calls `primaryHandMechanics.TryFireShotgun()`
4. But `primaryHandMechanics` is assigned to **RIGHT hand** in Inspector! ❌
5. RIGHT hand shoots and adds heat
6. **WRONG hand overheats!**

---

## FIX IN UNITY

1. Select your **Player** GameObject in hierarchy
2. Find `PlayerShooterOrchestrator` component in Inspector
3. **SWAP the assignments**:
   - `Primary Hand Mechanics` → Drag your **LEFT hand** GameObject here
   - `Secondary Hand Mechanics` → Drag your **RIGHT hand** GameObject here

---

## HOW TO FIND YOUR HAND GAMEOBJECTS

Search for GameObjects with `HandFiringMechanics` component:
- One should be on/under LEFT hand
- One should be on/under RIGHT hand

Then assign them correctly!

---

## EVIDENCE

`MenuReturnSaveHandler.cs` line 148 shows the old mapping:
```csharp
Debug.Log($"... Primary (RIGHT): {primaryLevel}, Secondary (LEFT): ...");
```

This proves the Inspector assignments were never updated after your refactor!
