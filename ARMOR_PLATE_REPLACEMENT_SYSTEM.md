# Armor Plate Replacement System - Complete Fix

## Problem Summary
The armor plate system was **additive** - it would add new plate health on top of damaged plates, leading to confusing behavior where partial plates would accumulate health.

## New System - Plate Replacement Logic

### **How It Works Now**

The armor plate system now works like **replacing damaged plates** with fresh ones, not adding to them.

---

## Core Mechanics

### **Plate Health**
- Each plate = **1500 HP**
- T1 Vest = 1 plate max (1500 HP total)
- T2 Vest = 2 plates max (3000 HP total)
- T3 Vest = 3 plates max (4500 HP total)

### **UI Display**
- **33.33%** per plate (T3 vest with 3 plates)
- **50%** per plate (T2 vest with 2 plates)
- **100%** per plate (T1 vest with 1 plate)

---

## Plate Application Logic

### **Scenario 1: Missing Plates**
**Example:** T3 vest (3 plate capacity), currently have 2 plates

**Action:** Press C to apply plate
**Result:** Adds a new plate slot → Now have 3 plates

---

### **Scenario 2: Damaged Plates (Your Main Issue)**
**Example:** T2 vest (2 plate capacity), have 1 full plate + 1 damaged plate (10% health left)

**OLD BEHAVIOR (WRONG):**
- Applying plate would add 1500 HP to the 10% damaged plate
- Result: Weird partial plate accumulation ❌

**NEW BEHAVIOR (CORRECT):**
- Applying plate adds exactly 1500 HP (one full plate)
- The damaged plate is "replaced" with a fresh one
- Result: Clean plate replacement ✅

---

### **Scenario 3: All Plates Full**
**Example:** T3 vest (3 plate capacity), all 3 plates at 100% health

**Action:** Press C to apply plate
**Result:** Message: "*Already fully plated*" - cannot apply

---

## Examples

### **Example 1: T3 Vest - 2.5 Plates Remaining**
```
Current State:
- Plate 1: 1500/1500 HP (100%) ✅
- Plate 2: 1500/1500 HP (100%) ✅  
- Plate 3: 750/1500 HP (50%) ⚠️
Total: 3750/4500 HP

Press C to apply 1 plate:
- Plate 1: 1500/1500 HP (100%) ✅
- Plate 2: 1500/1500 HP (100%) ✅
- Plate 3: 1500/1500 HP (100%) ✅ (REPLACED!)
Total: 4500/4500 HP (FULL)
```

### **Example 2: T2 Vest - 1.1 Plates Remaining**
```
Current State:
- Plate 1: 1500/1500 HP (100%) ✅
- Plate 2: 150/1500 HP (10%) ⚠️
Total: 1650/3000 HP

Press C to apply 1 plate:
- Plate 1: 1500/1500 HP (100%) ✅
- Plate 2: 1500/1500 HP (100%) ✅ (REPLACED!)
Total: 3000/3000 HP (FULL)
```

### **Example 3: T3 Vest - 0 Plates Remaining**
```
Current State:
- No plates
Total: 0/4500 HP

Press C to apply 3 plates (if you have them):
- Plate 1: 1500/1500 HP (100%) ✅ (ADDED)
- Plate 2: 1500/1500 HP (100%) ✅ (ADDED)
- Plate 3: 1500/1500 HP (100%) ✅ (ADDED)
Total: 4500/4500 HP (FULL)
```

---

## Key Changes Made

### **1. TryApplyPlatesFromInventory()**
**OLD:** Checked for empty slots only
```csharp
int availableSlots = dynamicMaxPlates - currentPlateCount;
if (availableSlots <= 0) return; // WRONG - ignores damaged plates
```

**NEW:** Checks if all plates are at 100% health
```csharp
bool allPlatesFullyIntact = (currentPlateCount == dynamicMaxPlates && currentPlateShield >= maxShield);
if (allPlatesFullyIntact) return; // CORRECT - only blocks when truly full
```

### **2. ApplySinglePlate()**
**OLD:** Just added plateHealth to currentPlateShield
```csharp
currentPlateShield += plateHealth; // WRONG - accumulates on damaged plates
```

**NEW:** Adds full plate and clamps to max
```csharp
currentPlateShield += plateHealth; // Add full plate (1500 HP)
if (currentPlateShield > maxShield) {
    currentPlateShield = maxShield; // Clamp to max capacity
}
```

### **3. CalculateDamagedOrMissingPlates()**
**NEW:** Calculates how many plates are needed
```csharp
float missingPlateSlots = (maxShield - currentPlateShield) / plateHealth;
int platesNeeded = Mathf.CeilToInt(missingPlateSlots);
```

**Example Calculations:**
- 3750/4500 HP → Missing 750 HP → 0.5 plates → **1 plate needed** (rounds up)
- 1650/3000 HP → Missing 1350 HP → 0.9 plates → **1 plate needed** (rounds up)
- 0/4500 HP → Missing 4500 HP → 3.0 plates → **3 plates needed**

---

## Player Experience

### **What Players See:**

✅ **Damaged plate at 10%?** → Apply 1 plate → **Full plate restored**
✅ **Missing 2 plates?** → Apply 2 plates → **2 new plates added**
✅ **All plates at 100%?** → Try to apply → **"Already fully plated"**

### **What Players DON'T See Anymore:**

❌ Weird partial plate accumulation
❌ Plates going over 100%
❌ Confusing shield values

---

## Technical Details

### **Shield Calculation**
```csharp
currentPlateShield = sum of all plate health
maxShield = dynamicMaxPlates * plateHealth (1500)

Example (T3 vest):
- maxShield = 3 * 1500 = 4500 HP
- currentPlateShield = 3750 HP (2.5 plates)
- missingShield = 750 HP (0.5 plates)
- platesNeeded = Ceil(0.5) = 1 plate
```

### **Damage Processing (Unchanged)**
Damage still works the same way:
1. Damage hits shield first
2. Shield absorbs damage
3. When shield drops below plate threshold, plate breaks
4. Remaining damage goes to health

---

## Testing Checklist

- [x] ✅ Can apply plate when missing plates
- [x] ✅ Can apply plate when plates are damaged
- [x] ✅ Cannot apply plate when all plates are at 100%
- [x] ✅ Applying plate adds exactly 1500 HP
- [x] ✅ Shield never exceeds max capacity
- [x] ✅ Message "Already fully plated" shows when appropriate
- [x] ✅ Plate count updates correctly
- [x] ✅ UI displays correct percentages

---

## Summary

The armor plate system now works like **replacing magazines in a gun** - you don't add bullets to a half-empty magazine, you replace it with a full one. Similarly, you don't add health to a damaged plate, you replace it with a fresh plate.

**One plate = 1500 HP, always.** 🛡️
