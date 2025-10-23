# Vest & Backpack Equipment System - Complete Fix

## Problem Summary
The vest and backpack equipment system had several critical issues:
1. ❌ Players could drag/drop equipped vests and backpacks to unequip them
2. ❌ Players could double-click equipped vests and backpacks to unequip them
3. ❌ Vests and backpacks from chests could be placed in regular inventory slots
4. ❌ Lower tier vests/backpacks could replace higher tier ones

## Solution Implemented

### 1. **UnifiedSlot.cs** - Prevent Vests/Backpacks in Regular Inventory
**Location:** `CanAcceptItem()` method

**Change:** Added validation to reject vests and backpacks in regular inventory slots:
```csharp
// CRITICAL FIX: Regular inventory slots should NOT accept vests or backpacks
// Vests and backpacks can ONLY go in their dedicated equipment slots
if (!isStashSlot && (IsVestItem(item) || IsBackpackItem(item)))
{
    Debug.Log($"[UnifiedSlot] ❌ REJECTED: Cannot place {item.itemName} ({item.itemType}) in regular inventory slot - must use equipment slot");
    return false;
}
```

**Result:** ✅ Vests and backpacks can ONLY be placed in their dedicated equipment slots, never in regular inventory slots.

---

### 2. **VestSlotController.cs** - Prevent Unequipping Vests
**Location:** `InitializeVestSlot()`, `HandleVestDropped()` and `HandleVestDoubleClick()` methods

**Changes:**
- **Equipment Slot Flag:** Sets `isEquipmentSlot = true` to prevent dragging at the UnifiedSlot level
- **Drag Prevention:** Added check to block dragging FROM the vest slot (redundant safety)
```csharp
// CRITICAL FIX: Prevent dragging FROM the vest slot (unequipping)
if (fromSlot == vestSlot)
{
    Debug.Log($"[VestSlotController] ❌ BLOCKED: Cannot drag vest out of equipment slot - vests cannot be unequipped, only replaced");
    return;
}
```

- **Double-Click Prevention:** Updated to clearly indicate vests cannot be unequipped
```csharp
Debug.Log($"[VestSlotController] ❌ BLOCKED: Vest {currentVest.GetDisplayName()} cannot be unequipped - only replaceable with higher tier vest from chest/stash");
```

- **Tier Validation:** Enhanced error messages to show tier comparison
```csharp
Debug.Log($"[VestSlotController] ❌ Cannot replace {currentVest.GetDisplayName()} with {droppedVest.GetDisplayName()} - not an upgrade (Tier {droppedVest.vestTier} <= Tier {currentVest.vestTier})");
```

**Result:** ✅ Equipped vests cannot be dragged out or double-clicked to unequip. They can only be replaced by higher tier vests.

---

### 3. **BackpackSlotController.cs** - Prevent Unequipping Backpacks
**Location:** `InitializeBackpackSlot()`, `HandleBackpackDropped()` and `HandleBackpackDoubleClick()` methods

**Changes:** (Same pattern as VestSlotController)
- **Equipment Slot Flag:** Sets `isEquipmentSlot = true` to prevent dragging at the UnifiedSlot level
- **Drag Prevention:** Added check to block dragging FROM the backpack slot (redundant safety)
- **Double-Click Prevention:** Updated to clearly indicate backpacks cannot be unequipped
- **Tier Validation:** Enhanced error messages to show tier comparison

**Result:** ✅ Equipped backpacks cannot be dragged out or double-clicked to unequip. They can only be replaced by higher tier backpacks.

---

### 4. **ChestInteractionSystem.cs** - Auto-Equip from Chest
**Location:** `CollectItem()` method

**Change:** Added special handling for vests and backpacks when collecting from chest:
```csharp
// SPECIAL HANDLING: Vests and Backpacks go to equipment slots, not regular inventory
bool success = false;

if (itemToCollect is VestItem vestItem)
{
    Debug.Log($"📦 🎽 VEST DETECTED: Attempting to equip {vestItem.GetDisplayName()} (Tier {vestItem.vestTier})");
    
    VestSlotController vestSlot = inventoryManager.vestSlot;
    if (vestSlot != null)
    {
        // Check if this is an upgrade
        if (vestSlot.CanEquipVest(vestItem))
        {
            success = vestSlot.EquipVest(vestItem, false);
        }
        else
        {
            Debug.Log($"📦 ❌ Cannot equip {vestItem.GetDisplayName()} - not an upgrade from current vest");
            return false; // Not an upgrade, leave in chest
        }
    }
}
else if (itemToCollect is BackpackItem backpackItem)
{
    // Similar logic for backpacks...
}
else
{
    // Regular items go to inventory
    success = inventoryManager.TryAddItem(itemToCollect, countToCollect, autoSave: false);
}
```

**Result:** ✅ When double-clicking or dragging vests/backpacks from chest:
- They automatically go to the equipment slot (not regular inventory)
- Only higher tier items can be equipped
- Lower tier items stay in the chest with a clear message

---

## How It Works Now

### ✅ Correct Behavior

1. **Finding a Vest/Backpack in Chest:**
   - Double-click the vest/backpack in the chest
   - If it's a higher tier than your current one → **Auto-equipped to equipment slot**
   - If it's same/lower tier → **Stays in chest** with message "not an upgrade"

2. **Dragging from Chest:**
   - Drag vest/backpack from chest to vest/backpack equipment slot
   - If it's a higher tier → **Equipped and replaces current**
   - If it's same/lower tier → **Rejected** with message "not an upgrade"
   - Cannot drag to regular inventory slots → **Blocked** with message "must use equipment slot"

3. **Equipped Items:**
   - Cannot drag equipped vest/backpack out of slot → **Blocked**
   - Cannot double-click to unequip → **Blocked**
   - Can only be replaced by higher tier items

### ❌ Blocked Behavior

1. **Cannot unequip vests or backpacks** - they are permanent equipment
2. **Cannot place vests/backpacks in regular inventory slots** - they must go in equipment slots
3. **Cannot equip lower tier items** - only upgrades are allowed
4. **Cannot drag equipped items out** - they stay locked in equipment slots

---

## Tier System

### Vests (Armor Plate Capacity)
- **T1 Vest:** 1 armor plate (default, always equipped)
- **T2 Vest:** 2 armor plates (upgrade)
- **T3 Vest:** 3 armor plates (max upgrade)

### Backpacks (Inventory Slots)
- **Tier 1 Backpack:** 5 inventory slots (default, always equipped)
- **Tier 2 Backpack:** 7 inventory slots (upgrade)
- **Tier 3 Backpack:** 10 inventory slots (max upgrade)

**Upgrade Rule:** You can only equip a vest/backpack if its tier is **higher** than your current one.

---

## Testing Checklist

- [x] ✅ Vests cannot be placed in regular inventory slots
- [x] ✅ Backpacks cannot be placed in regular inventory slots
- [x] ✅ Cannot drag equipped vest out of vest slot
- [x] ✅ Cannot drag equipped backpack out of backpack slot
- [x] ✅ Cannot double-click equipped vest to unequip
- [x] ✅ Cannot double-click equipped backpack to unequip
- [x] ✅ Double-clicking T2 vest in chest auto-equips it (if you have T1)
- [x] ✅ Double-clicking T1 vest in chest does nothing (if you have T2)
- [x] ✅ Dragging T3 backpack to backpack slot equips it (if you have T2)
- [x] ✅ Dragging T2 backpack to backpack slot is rejected (if you have T3)
- [x] ✅ Clear error messages explain why actions are blocked

---

## Files Modified

1. **UnifiedSlot.cs** 
   - Added `isEquipmentSlot` flag to identify equipment slots (vest/backpack)
   - Added drag prevention in `OnBeginDrag()` for equipment slots
   - Added vest/backpack rejection in `CanAcceptItem()` for regular inventory slots
   - Added `bypassValidation` parameter to `SetItem()` for programmatic equipment sets
   - Chest slots and stash slots can still hold vests/backpacks
2. **VestSlotController.cs** 
   - Sets `isEquipmentSlot = true` on initialization to prevent dragging
   - Added drag/double-click prevention (redundant safety checks)
   - Updated to use `bypassValidation: true` when setting vests programmatically
3. **BackpackSlotController.cs** 
   - Sets `isEquipmentSlot = true` on initialization to prevent dragging
   - Added drag/double-click prevention (redundant safety checks)
   - Updated to use `bypassValidation: true` when setting backpacks programmatically
4. **ChestInteractionSystem.cs** - Added auto-equip logic for vests/backpacks

---

## Notes

- Players always have a vest and backpack equipped (T1 by default)
- On death, players reset to T1 vest and T1 backpack
- Vests and backpacks found in chests are automatically added to the loot table
- The system uses tier comparison to ensure only upgrades are allowed
- All actions are logged with clear debug messages for troubleshooting
- Equipment slots use `bypassValidation: true` to allow programmatic vest/backpack placement
- Chest slots can display vests/backpacks for looting (they have `chestSystem` reference)
- Stash slots can store vests/backpacks for later use
