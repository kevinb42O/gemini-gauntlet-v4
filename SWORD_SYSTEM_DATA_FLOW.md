# Sword System - Complete Data Flow Diagram

## 🎮 User Actions → System Responses

### ⚔️ ACTION 1: Press Mouse4 (Manual Toggle)
```
┌─────────────┐
│ Press Mouse4│
└──────┬──────┘
       │
       ▼
┌────────────────────────────────────────┐
│ PlayerShooterOrchestrator.Update()     │
│ Input.GetMouseButtonDown(3)            │
└──────┬─────────────────────────────────┘
       │
       ▼
┌────────────────────────────────────────┐
│ ToggleSwordMode()                      │
│ • Check availability (_isSwordAvailable)│
│ • Toggle IsSwordModeActive flag        │
│ • If ON: Play unsheath sound           │
│ • If ON: Show sword visual             │
│ • If ON: Trigger reveal animation      │
│ • If OFF: Hide sword visual            │
│ • If OFF: Reset to idle animation      │
└────────────────────────────────────────┘
```

---

### 🎒 ACTION 2: Double-Click Weapon in Inventory (Equip)
```
┌────────────────────────┐
│ Double-Click Weapon    │
│ (in inventory slot)    │
└──────┬─────────────────┘
       │
       ▼
┌────────────────────────────────────────┐
│ UnifiedSlot.OnPointerClick()           │
│ • Detect double-click on weapon        │
│ • Find WeaponEquipmentManager          │
│ • Get rightHandWeaponSlot              │
└──────┬─────────────────────────────────┘
       │
       ▼
┌────────────────────────────────────────┐
│ weaponSlot.SetItem(weapon)             │
│ • Move weapon to equipment slot        │
│ • Clear source inventory slot          │
│ • bypassValidation = true              │
└──────┬─────────────────────────────────┘
       │
       ▼
┌────────────────────────────────────────┐
│ 🔔 OnSlotChanged Event Fires           │
└──────┬─────────────────────────────────┘
       │
       ▼
┌────────────────────────────────────────┐
│ WeaponEquipmentManager                 │
│ .OnRightHandSlotChanged()              │
└──────┬─────────────────────────────────┘
       │
       ▼
┌────────────────────────────────────────┐
│ CheckRightHandEquipment()              │
│ • Get currentItem from slot            │
│ • Check if sword type                  │
│ • SetSwordAvailable(true)              │
└──────┬─────────────────────────────────┘
       │
       ▼
┌────────────────────────────────────────┐
│ ⚔️ AUTO-ACTIVATION LOGIC (NEW!)        │
│ if (!IsSwordModeActive)                │
│     ToggleSwordMode()                  │
│     → Same as Mouse4 press!            │
│ else                                   │
│     (Already active, skip)             │
└────────────────────────────────────────┘
```

---

### 🎒 ACTION 3: Double-Click Weapon Slot (Unequip)
```
┌────────────────────────┐
│ Double-Click Weapon    │
│ (in weapon slot)       │
└──────┬─────────────────┘
       │
       ▼
┌────────────────────────────────────────┐
│ UnifiedSlot.OnPointerClick()           │
│ • Detect double-click on weapon slot   │
│ • Find InventoryManager                │
│ • Get first empty inventory slot       │
└──────┬─────────────────────────────────┘
       │
       ▼
┌────────────────────────────────────────┐
│ emptySlot.SetItem(weapon)              │
│ weaponSlot.ClearSlot()                 │
│ • Move weapon to inventory             │
│ • Clear weapon slot                    │
└──────┬─────────────────────────────────┘
       │
       ▼
┌────────────────────────────────────────┐
│ 🔔 OnSlotChanged Event Fires           │
│ (with item = null)                     │
└──────┬─────────────────────────────────┘
       │
       ▼
┌────────────────────────────────────────┐
│ WeaponEquipmentManager                 │
│ .OnRightHandSlotChanged()              │
└──────┬─────────────────────────────────┘
       │
       ▼
┌────────────────────────────────────────┐
│ CheckRightHandEquipment()              │
│ • Detect currentItem = null            │
│ • SetSwordAvailable(false)             │
└──────┬─────────────────────────────────┘
       │
       ▼
┌────────────────────────────────────────┐
│ PlayerShooterOrchestrator              │
│ .SetSwordAvailable(false)              │
│ • if (IsSwordModeActive)               │
│   → FORCE deactivate mode              │
│   → Hide sword visual                  │
│   → Reset to idle animation            │
└────────────────────────────────────────┘
```

---

### 🌍 ACTION 4: Pick Up Sword from World
```
┌────────────────────────┐
│ Press E near sword     │
│ (world pickup)         │
└──────┬─────────────────┘
       │
       ▼
┌────────────────────────────────────────┐
│ WorldSwordPickup.OnPickup()            │
│ OR ChestInteractionSystem              │
│ OR InventoryManager.TryAddItem()       │
└──────┬─────────────────────────────────┘
       │
       ▼
┌────────────────────────────────────────┐
│ rightHandWeaponSlot.SetItem(sword)     │
│ • Direct equip to weapon slot          │
│ • bypassValidation = true              │
└──────┬─────────────────────────────────┘
       │
       ▼
┌────────────────────────────────────────┐
│ 🔔 OnSlotChanged Event Fires           │
│ (SAME PATH AS DOUBLE-CLICK EQUIP)     │
└──────┬─────────────────────────────────┘
       │
       ▼
┌────────────────────────────────────────┐
│ CheckRightHandEquipment()              │
│ → Calls ToggleSwordMode()              │
│ → IMMEDIATE ACTIVATION ✅              │
└──────┬─────────────────────────────────┘
       │
       ▼
┌────────────────────────────────────────┐
│ ⏱️ Next Frame (Coroutine)              │
│ InventoryManager                       │
│ .ActivateSwordModeNextFrame()          │
│ • if (!IsSwordModeActive)              │
│   → Already active from above!         │
│   → Skip (prevents double activation)  │
└────────────────────────────────────────┘
```

---

## 🔄 State Machine

### Sword Mode States
```
┌─────────────────────┐
│ NO SWORD EQUIPPED   │ ◄──────────┐
│ _isSwordAvailable=F │            │
│ IsSwordModeActive=F │            │
└──────┬──────────────┘            │
       │                            │
       │ Equip Sword               │
       │ (inventory/pickup)        │
       │                            │
       ▼                            │
┌─────────────────────┐            │
│ SWORD EQUIPPED +    │            │
│ MODE ACTIVE (NEW!)  │            │ Unequip
│ _isSwordAvailable=T │            │
│ IsSwordModeActive=T │────────────┘
└──────┬──────────────┘
       │      ▲
       │      │
       │      │ Press Mouse4
       │      │ (toggle ON)
       │      │
       ▼      │
┌─────────────────────┐
│ SWORD EQUIPPED      │
│ MODE INACTIVE       │
│ _isSwordAvailable=T │
│ IsSwordModeActive=F │
└──────┬──────────────┘
       │      ▲
       │      │
       └──────┘
     Press Mouse4
     (toggle OFF)
```

---

## 📊 Before vs After Fix

### BEFORE FIX ❌
```
Equip Sword (inventory)
  → rightHandWeaponSlot.SetItem()
  → OnSlotChanged
  → SetSwordAvailable(true)
  → ❌ STOPS HERE
  → User must press Mouse4 manually

Result: INCONSISTENT with manual toggle
```

### AFTER FIX ✅
```
Equip Sword (inventory)
  → rightHandWeaponSlot.SetItem()
  → OnSlotChanged
  → SetSwordAvailable(true)
  → ✅ ToggleSwordMode() (if not active)
  → Full activation with animation/sound

Result: IDENTICAL to manual toggle
```

---

## 🎯 Key Design Principles

### 1. Single Source of Truth
- `IsSwordModeActive` in `PlayerShooterOrchestrator`
- All systems read from this property
- Only two methods modify it:
  - `ToggleSwordMode()` - User-initiated toggle
  - `SetSwordAvailable(false)` - Force deactivate on unequip

### 2. Event-Driven Architecture
- `UnifiedSlot.OnSlotChanged` event
- WeaponEquipmentManager subscribes
- Decoupled components, clean separation

### 3. Guard Clauses
- `ToggleSwordMode()` checks availability first
- `SetSwordAvailable(false)` only deactivates if active
- `CheckRightHandEquipment()` only activates if not already active
- No redundant operations, no conflicts

### 4. Zero Magic Numbers
- All checks use named properties
- Clear, readable conditions
- Self-documenting code

---

## ✅ Final Verification

### All Paths Lead to Same Result
1. **Manual Toggle (Mouse4)** → `ToggleSwordMode()`
2. **Inventory Equip** → `SetSwordAvailable(true)` → `ToggleSwordMode()`
3. **World Pickup** → `SetSwordAvailable(true)` → `ToggleSwordMode()`
4. **All Unequip** → `SetSwordAvailable(false)` → Force deactivate

**Result:** Perfect symmetry across all systems ✅
