# 🗡️ EQUIPPABLE SWORD SYSTEM - VISUAL FLOW DIAGRAM
**Reference:** AAA_EQUIPPABLE_SWORD_ITEM_SYSTEM_COMPLETE.md

---

## 📊 SYSTEM ARCHITECTURE OVERVIEW

```
┌─────────────────────────────────────────────────────────────────────┐
│                    EQUIPPABLE SWORD ITEM SYSTEM                      │
│                     (Integrated with Existing)                       │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                ┌───────────────────┼───────────────────┐
                │                   │                   │
                ▼                   ▼                   ▼
        ┌──────────────┐    ┌──────────────┐   ┌──────────────┐
        │ ACQUISITION  │    │  EQUIPMENT   │   │  GAMEPLAY    │
        │   SOURCES    │    │    SYSTEM    │   │  MECHANICS   │
        └──────────────┘    └──────────────┘   └──────────────┘
```

---

## 🎮 ACQUISITION FLOW

```
┌─────────────────────────────────────────────────────────────────┐
│                    3 WAYS TO GET SWORD                          │
└─────────────────────────────────────────────────────────────────┘

1️⃣ WORLD PICKUP (E Key)
   ┌────────────────────┐
   │ WorldSwordPickup   │
   │   (250 units)      │
   └────────┬───────────┘
            │ Press E
            ▼
   ┌────────────────────┐
   │  TryAddItem()      │◄──── InventoryManager
   └────────┬───────────┘
            │ Success
            ▼
   ┌────────────────────┐
   │ FloatingText:      │
   │ "⚔️ Acquired!"     │
   └────────────────────┘

2️⃣ CHEST LOOT (10% Chance)
   ┌────────────────────┐
   │  ChestController   │
   │   OpenChest()      │
   └────────┬───────────┘
            │ Roll 10%
            ▼
   ┌────────────────────┐
   │ SpawnSwordItem()   │
   └────────┬───────────┘
            │
            ▼
   ┌────────────────────┐
   │  Inventory Slot    │
   └────────────────────┘

3️⃣ FORGE CRAFTING (4 Ingredients)
   ┌────────────────────┐
   │  ForgeManager      │
   │  Input Slots [4]   │
   └────────┬───────────┘
            │ Match Recipe
            ▼
   ┌────────────────────┐
   │  Craft Button      │
   │  (5 sec timer)     │
   └────────┬───────────┘
            │
            ▼
   ┌────────────────────┐
   │  Output Slot [1]   │
   │  → Inventory       │
   └────────────────────┘
```

---

## ⚙️ EQUIPMENT SYSTEM FLOW

```
┌─────────────────────────────────────────────────────────────────┐
│              EQUIPMENT SLOT → SWORD MODE GATING                 │
└─────────────────────────────────────────────────────────────────┘

INVENTORY FLOW:
┌────────────────┐     Drag      ┌─────────────────────┐
│ Normal Slot    │──────────────►│ Right Hand Weapon   │
│ (No Effect)    │               │   Equipment Slot    │
└────────────────┘               └──────────┬──────────┘
                                            │
                                            │ OnSlotChanged Event
                                            ▼
                                 ┌─────────────────────┐
                                 │ WeaponEquipment     │
                                 │    Manager          │
                                 └──────────┬──────────┘
                                            │
                      ┌─────────────────────┼─────────────────────┐
                      │ CheckRightHand      │                     │
                      │  Equipment()        │                     │
                      ▼                     ▼                     ▼
            ┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐
            │ IF Sword Item   │   │ IF No Item      │   │ IF Wrong Type   │
            └────────┬────────┘   └────────┬────────┘   └────────┬────────┘
                     │                     │                      │
                     ▼                     ▼                      ▼
         SetSwordAvailable(true)  SetSwordAvailable(false)  SetSwordAvailable(false)
                     │                     │                      │
                     ▼                     ▼                      ▼
         ┌─────────────────────┐  ┌─────────────────────┐  ┌─────────────────────┐
         │ PlayerShooter       │  │ PlayerShooter       │  │ PlayerShooter       │
         │ Orchestrator        │  │ Orchestrator        │  │ Orchestrator        │
         │                     │  │                     │  │                     │
         │ _isSwordAvailable   │  │ _isSwordAvailable   │  │ _isSwordAvailable   │
         │      = TRUE ✅      │  │      = FALSE ❌     │  │      = FALSE ❌     │
         └─────────────────────┘  └─────────────────────┘  └─────────────────────┘
```

---

## 🎯 SWORD MODE ACTIVATION FLOW

```
┌─────────────────────────────────────────────────────────────────┐
│           MOUSE BUTTON 3 PRESS → MODE CHECK                     │
└─────────────────────────────────────────────────────────────────┘

USER INPUT:
┌─────────────────┐
│ Press Mouse 3/4 │
└────────┬────────┘
         │
         ▼
┌─────────────────────────┐
│ PlayerShooterOrchestrator│
│   ToggleSwordMode()      │
└────────┬────────────────┘
         │
         ▼
┌─────────────────────────┐
│ Check:                  │
│ _isSwordAvailable?      │
└────┬────────────────┬───┘
     │                │
     │ FALSE          │ TRUE
     ▼                ▼
┌─────────────┐   ┌─────────────────────┐
│ BLOCK!      │   │ Toggle Mode         │
│ Show Error  │   │ IsSwordModeActive   │
│ Message:    │   │    = !current       │
│ "⚠️ No      │   └──────────┬──────────┘
│  Sword!"    │              │
└─────────────┘              ▼
                  ┌──────────────────────┐
                  │ IF Activating:       │
                  │ • Play unsheath      │
                  │ • Show sword model   │
                  │ • Enable attacks     │
                  │                      │
                  │ IF Deactivating:     │
                  │ • Hide sword model   │
                  │ • Back to shooting   │
                  └──────────────────────┘
```

---

## 🔄 COMPLETE LIFECYCLE DIAGRAM

```
┌───────────────────────────────────────────────────────────────────────┐
│                     SWORD ITEM COMPLETE LIFECYCLE                     │
└───────────────────────────────────────────────────────────────────────┘

START: Player Spawns
        │
        ▼
┌───────────────────┐
│ 1️⃣ Find Sword      │ ◄───┐
│ - World Pickup    │     │
│ - Chest Loot      │     │
│ - Forge Craft     │     │
└────────┬──────────┘     │
         │                │
         ▼                │
┌───────────────────┐     │
│ 2️⃣ Inventory       │     │
│ (Normal Slot)     │     │
│ NO EFFECT YET     │     │
└────────┬──────────┘     │
         │                │
         │ Drag to        │
         │ Equipment      │
         ▼                │
┌───────────────────┐     │
│ 3️⃣ Equipment Slot  │     │
│ (Right Hand)      │     │
│ ✅ SWORD ENABLED   │     │
└────────┬──────────┘     │
         │                │
         ▼                │
┌───────────────────┐     │
│ 4️⃣ Activate Mode   │     │
│ Press Mouse 3     │     │
│ ⚔️ SWORD COMBAT    │     │
└────────┬──────────┘     │
         │                │
         ├────────────────┤
         │                │
         ▼                │
┌───────────────────┐     │
│ 5️⃣ Unequip Options │     │
│                   │     │
│ A) Drag to        │     │
│    Inventory      │     │
│    → Store        │     │
│                   │     │
│ B) Drag to        │     │
│    World          │     │
│    → Drop         │─────┘
│                   │
│ C) Die            │
│    → Lose All     │
└───────────────────┘
         │
         ▼
┌───────────────────┐
│ 6️⃣ Mode Disabled   │
│ Can't use sword   │
│ until re-equipped │
└───────────────────┘
```

---

## 🔗 INTEGRATION POINTS MAP

```
┌─────────────────────────────────────────────────────────────────┐
│              SYSTEM INTEGRATION DEPENDENCY GRAPH                │
└─────────────────────────────────────────────────────────────────┘

                    ┌────────────────────────┐
                    │ EquippableWeaponItemData│ (NEW)
                    │  extends ChestItemData │
                    └───────────┬────────────┘
                                │
                  ┌─────────────┼─────────────┐
                  │             │             │
                  ▼             ▼             ▼
        ┌──────────────┐  ┌──────────┐  ┌──────────┐
        │ ChestController│ │ ForgeManager│ │WorldItem │
        │ (EXISTING)    │  │ (EXISTING) │  │(EXISTING)│
        └──────────────┘  └──────────┘  └──────────┘
                  │             │             │
                  └─────────────┼─────────────┘
                                │
                                ▼
                    ┌────────────────────┐
                    │ InventoryManager   │ (EXISTING)
                    │  TryAddItem()      │
                    └───────────┬────────┘
                                │
                                ▼
                    ┌────────────────────┐
                    │   UnifiedSlot      │ (EXISTING)
                    │ isEquipmentSlot    │
                    └───────────┬────────┘
                                │ OnSlotChanged Event
                                ▼
                    ┌────────────────────────┐
                    │ WeaponEquipmentManager │ (NEW)
                    │  CheckRightHandEquip() │
                    └───────────┬────────────┘
                                │ SetSwordAvailable()
                                ▼
                    ┌────────────────────────┐
                    │PlayerShooterOrchestrator│ (MODIFIED)
                    │   ToggleSwordMode()    │
                    │   _isSwordAvailable    │
                    └────────────────────────┘
                                │
                    ┌───────────┼───────────┐
                    ▼           ▼           ▼
            ┌──────────┐ ┌──────────┐ ┌──────────┐
            │SwordDamage│ │Animation │ │Audio     │
            │(EXISTING) │ │(EXISTING)│ │(EXISTING)│
            └──────────┘ └──────────┘ └──────────┘

LEGEND:
• (NEW) = Scripts to create
• (EXISTING) = Don't touch, perfect already
• (MODIFIED) = Small additions only
```

---

## 🧪 TEST FLOW DIAGRAM

```
┌─────────────────────────────────────────────────────────────────┐
│                 TESTING VALIDATION SEQUENCE                     │
└─────────────────────────────────────────────────────────────────┘

TEST 1: PICKUP
├─ Place WorldSword in scene
├─ Walk within 250 units
├─ Press E
├─ Expected: Sword in inventory ✅
└─ Expected: "⚔️ Acquired!" message ✅

TEST 2: EQUIPMENT
├─ Open inventory (TAB)
├─ Drag sword to right hand slot
├─ Expected: Sword moves to equipment ✅
├─ Expected: "⚔️ Equipped!" message ✅
└─ Expected: Console log "✅ Sword equipped" ✅

TEST 3: MODE ACTIVATION
├─ Press Mouse Button 3
├─ Expected: Sword mode activates ✅
├─ Expected: Sword visual appears ✅
├─ Expected: Unsheath sound plays ✅
└─ Expected: Can attack with RMB ✅

TEST 4: UNEQUIPPED BLOCK
├─ Drag sword OUT of equipment slot
├─ Expected: Mode auto-deactivates ✅
├─ Press Mouse Button 3
├─ Expected: "⚠️ No Sword!" message ✅
└─ Expected: Mode doesn't activate ✅

TEST 5: CHEST SPAWN
├─ Open 10-20 chests (10% rate)
├─ Expected: Sword appears in chest ✅
└─ Expected: Can take to inventory ✅

TEST 6: FORGE CRAFT
├─ Place 4 ingredients in forge
├─ Click craft button
├─ Expected: 5 second countdown ✅
├─ Expected: Sword in output slot ✅
└─ Expected: Can take to inventory ✅

TEST 7: DEATH DROP
├─ Equip sword
├─ Take damage until dead
├─ Expected: All items lost ✅
├─ Expected: Sword gone from equipment ✅
└─ Expected: Respawn with empty inventory ✅

TEST 8: DRAG TO WORLD
├─ Have sword in normal inventory slot
├─ Drag out of inventory UI
├─ Drop in scene
├─ Expected: WorldSword spawns on ground ✅
└─ Expected: Can walk up and press E to pickup ✅

ALL TESTS PASS = SYSTEM COMPLETE ✅
```

---

## 📐 DATA FLOW: EQUIPMENT STATE

```
┌─────────────────────────────────────────────────────────────────┐
│           EQUIPMENT STATE SYNCHRONIZATION FLOW                  │
└─────────────────────────────────────────────────────────────────┘

SOURCE OF TRUTH: UnifiedSlot.CurrentItem
         │
         │ OnSlotChanged Event
         ▼
┌────────────────────────┐
│ WeaponEquipmentManager │
│                        │
│ _rightHandWeapon       │◄──── Cached State
└────────┬───────────────┘
         │
         │ SetSwordAvailable(bool)
         ▼
┌────────────────────────┐
│PlayerShooterOrchestrator│
│                        │
│ _isSwordAvailable      │◄──── Derived State
└────────┬───────────────┘
         │
         │ Checked in ToggleSwordMode()
         ▼
┌────────────────────────┐
│   IsSwordModeActive    │◄──── Active State
└────────────────────────┘

STATE TRANSITIONS:
Slot Empty     → _isSwordAvailable = FALSE → Can't activate
Sword Equipped → _isSwordAvailable = TRUE  → Can activate
Mode Active    → Sword Unequipped          → Auto-deactivate
```

---

## 🎨 UI/UX FEEDBACK FLOW

```
┌─────────────────────────────────────────────────────────────────┐
│              PLAYER FEEDBACK AT EVERY STEP                      │
└─────────────────────────────────────────────────────────────────┘

ACTION                    FEEDBACK
──────────────────────────────────────────────────────
Pickup E                → "⚔️ Sword of Artorias Acquired!"
                          (Purple/Epic color, 24pt)

Equip to Slot           → "⚔️ Sword of Artorias Equipped!"
                          (Cyan, 24pt)

Activate Mode           → Sword visual appears
                          Unsheath sound
                          Hand animation

Try Without Equipped    → "⚠️ No Sword Equipped!"
                          (Red, 20pt)

Inventory Full          → "⚠️ Inventory Full!"
                          (Red, 20pt)

Chest Spawn             → Normal chest opening effects

Forge Complete          → Sword appears in output slot

Death                   → All items lost (existing system)

Drop to World           → WorldSword spawns with bobbing/rotation

ALL FEEDBACK USES: FloatingTextManager (Cognitive Messaging System)
```

---

## ✅ SUCCESS VALIDATION CHECKLIST

```
┌─────────────────────────────────────────────────────────────────┐
│              IMPLEMENTATION VALIDATION MATRIX                   │
└─────────────────────────────────────────────────────────────────┘

CODE CREATED:
□ EquippableWeaponItemData.cs
□ WeaponEquipmentManager.cs  
□ WorldSwordPickup.cs
□ PlayerShooterOrchestrator.cs (modified)
□ ChestController.cs (modified)

UNITY ASSETS:
□ SwordOfArtoriasWeapon ScriptableObject
□ Right Hand Weapon Slot (UI)
□ WorldSword_SwordOfArtorias Prefab
□ Forge Recipe (4 ingredients → sword)

INSPECTOR SETUP:
□ WeaponEquipmentManager component added to scene
□ rightHandWeaponSlot assigned
□ playerShooter reference assigned
□ Chest swordItemData assigned
□ World sword swordItemData assigned

TESTING PASSED:
□ Pickup with E key (250 units)
□ Equip to right hand slot
□ Mode activation with equipment
□ Mode blocked without equipment
□ Chest spawning (10% rate)
□ Forge crafting
□ Death drops all items
□ Drag to world spawns pickup

CONSOLE LOGS:
□ "✅ Sword equipped!" on equip
□ "❌ No weapon equipped" on unequip
□ "✅ Picked up Sword of Artorias"
□ "❌ Cannot activate - no sword!" when blocked

INTEGRATION VERIFIED:
□ FloatingTextManager messages working
□ GameSounds pickup sound working
□ Existing sword combat still perfect
□ No errors in console during full cycle

ALL CHECKBOXES = READY FOR PRODUCTION ✅
```

---

**VISUAL GUIDE COMPLETE** 🎉  
All flows documented, all integrations mapped, all tests defined.
