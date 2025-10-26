# Sword Visual Setup Guide - Quick Fix

## ⚔️ Issue
The sword mesh (`sword.OBJ`) is visible even when sword mode is inactive. It should only appear when sword mode is activated.

## ✅ Solution Applied

### Code Fix
Added initialization in `PlayerShooterOrchestrator.Start()` to ensure sword starts hidden:

```csharp
void Start()
{
    // ⚔️ SWORD VISUAL INITIALIZATION: Ensure sword starts hidden
    if (swordVisualGameObject != null)
    {
        swordVisualGameObject.SetActive(false);
        Debug.Log("[PlayerShooterOrchestrator] ⚔️ Sword visual initialized as HIDDEN on start");
    }
    // ... rest of Start() method
}
```

## 🎯 Unity Inspector Setup (CRITICAL!)

### Step 1: Locate the Sword Mesh
In your hierarchy (as shown in your screenshot):
```
Player
  └─ MainCamera_
      └─ Handen
          └─ RechterHand_PivotPoint
              └─ RechterHand
                  └─ RobotArmII_R
                      └─ R_EmitPoint
                          └─ plasima
                          └─ sword.OBJ  ← THIS IS THE MESH TO ASSIGN
```

### Step 2: Assign Reference in Inspector

1. **Select the Player GameObject** in hierarchy
2. **Find the PlayerShooterOrchestrator component** in Inspector
3. **Locate the "Sword Mode System" section** (should be around line 68-76 in the script)
4. **In "Sword Visual GameObject" field:**
   - **DRAG `sword.OBJ`** (NOT `RechterHand`, NOT the parent - the ACTUAL mesh object)
   - The field should now show: `sword.OBJ`

### Visual Reference
```
PlayerShooterOrchestrator Component:
┌─────────────────────────────────────────┐
│ [Sword Mode System]                     │
│                                         │
│ Sword Damage:        [SwordDamage]      │
│ Sword Visual GameObject: [sword.OBJ] ← │ 
│ Current Sword Attack Index: 1           │
└─────────────────────────────────────────┘
```

## 🔍 Verification Steps

### Before Starting Play Mode:
1. ✅ Check `sword.OBJ` is assigned in `swordVisualGameObject` field
2. ✅ In hierarchy, `sword.OBJ` should be visible (eye icon enabled)
3. ✅ Press Play

### During Play Mode:
1. ✅ Console should show: `⚔️ Sword visual initialized as HIDDEN on start`
2. ✅ In hierarchy, `sword.OBJ` should now be disabled (gray/crossed out)
3. ✅ Visually, no sword should be visible on the player's hand
4. ✅ Press Mouse4 or equip sword → Sword appears with animation
5. ✅ Press Mouse4 again or unequip → Sword disappears

## 🎮 Complete Flow Test

### Test 1: Manual Toggle
```
1. Start game → No sword visible ✅
2. Press Mouse4 → Sword appears with unsheath animation ✅
3. Press Mouse4 → Sword disappears with cleanup ✅
```

### Test 2: Inventory Equip
```
1. Start game → No sword visible ✅
2. Double-click sword in inventory → Sword appears instantly ✅
3. Double-click weapon slot → Sword disappears ✅
```

### Test 3: World Pickup
```
1. Start game → No sword visible ✅
2. Pick up sword from world → Auto-equips, sword appears ✅
3. Drop/unequip → Sword disappears ✅
```

## 🐛 Troubleshooting

### Issue: Sword still visible on start
**Cause:** Wrong GameObject assigned to `swordVisualGameObject`
**Fix:** Make sure you assigned `sword.OBJ` (the mesh), not its parent

### Issue: Sword doesn't appear when activated
**Cause:** `sword.OBJ` itself is disabled in the scene (not via code)
**Fix:** 
1. In hierarchy, find `sword.OBJ`
2. Make sure it's enabled (checkbox next to name is checked)
3. The script will handle showing/hiding via code

### Issue: Console shows "Sword visual GameObject: None"
**Cause:** Reference not assigned in inspector
**Fix:** Follow Step 2 above to assign `sword.OBJ`

## 📝 Technical Details

### What Changed
- **File:** `PlayerShooterOrchestrator.cs`
- **Method:** `Start()` 
- **Lines Added:** 5 lines (initialization block)
- **Breaking Changes:** None
- **Backward Compatible:** ✅ Yes

### Existing Behavior (Unchanged)
- `ToggleSwordMode()` still handles show/hide during gameplay
- `SetSwordAvailable(false)` still hides sword on unequip
- All animation and sound systems unchanged

### New Behavior
- Sword now **guaranteed to start hidden** even if manually enabled in editor
- Prevents "sword floating in air" bug on game start
- Consistent initialization regardless of editor state

## ✅ Summary

**One Inspector Assignment Required:**
- Drag `sword.OBJ` into `PlayerShooterOrchestrator → Sword Visual GameObject` field

**Code automatically handles:**
- ✅ Hide sword on game start
- ✅ Show sword on activation (Mouse4 or inventory equip)
- ✅ Hide sword on deactivation (Mouse4 or inventory unequip)
- ✅ Proper cleanup on all transitions

**Result:** PERFECT sword visibility behavior! 🎯
