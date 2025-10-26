# ⚔️⚔️ DUAL-HAND SWORD VISUAL FLOW DIAGRAM

## 🎮 Input → Output Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                    DUAL-HAND SWORD SYSTEM                           │
│             RIGHT HAND (Perfect) + LEFT HAND (NEW!)                 │
└─────────────────────────────────────────────────────────────────────┘

╔═══════════════════════════════════════════════════════════════════╗
║                        RIGHT HAND (Mouse4)                        ║
╚═══════════════════════════════════════════════════════════════════╝

   Mouse4 Toggle                    RMB (Tap)              Hold RMB (1s+)
        ↓                               ↓                        ↓
   ToggleSwordMode()          TriggerSwordAttack()    StartChargingSwordAttack()
        ↓                               ↓                        ↓
   IsSwordModeActive          rightHandController      rightHandController
   = true/false               .TriggerSwordAttack()    .TriggerSwordCharge()
        ↓                               ↓                        ↓
   swordVisualGameObject      Play attack 1 or 2       Play charge animation
   SetActive(true/false)      animation                + charge sound loop
        ↓                               ↓                        ↓
   RIGHT sword visible        swordDamage              Release RMB
   or hidden                  .DealDamage()                     ↓
                                                      ReleaseChargedSwordAttack()
                                                                 ↓
                                                      swordDamage.DealChargedDamage()
                                                      (2x damage + knockback)

╔═══════════════════════════════════════════════════════════════════╗
║                        LEFT HAND (Mouse5) - NEW!                  ║
╚═══════════════════════════════════════════════════════════════════╝

   Mouse5 Toggle                    LMB (Tap)              Hold LMB (1s+)
        ↓                               ↓                        ↓
   ToggleLeftSwordMode()      TriggerLeftSwordAttack()  StartChargingLeftSwordAttack()
        ↓                               ↓                        ↓
   IsLeftSwordModeActive      leftHandController        leftHandController
   = true/false               .TriggerSwordAttack()     .TriggerSwordCharge()
        ↓                               ↓                        ↓
   leftSwordVisualGameObject  Play attack 1 or 2        Play charge animation
   SetActive(true/false)      animation                 + charge sound loop
        ↓                               ↓                        ↓
   LEFT sword visible         leftSwordDamage           Release LMB
   or hidden                  .DealDamage()                      ↓
                                                       ReleaseChargedLeftSwordAttack()
                                                                  ↓
                                                       leftSwordDamage.DealChargedDamage()
                                                       (2x damage + knockback)
```

---

## 🔄 Inventory Integration Flow

```
╔═══════════════════════════════════════════════════════════════════╗
║                    EQUIPMENT SLOT SYSTEM                          ║
╚═══════════════════════════════════════════════════════════════════╝

┌─────────────────────────┐         ┌─────────────────────────┐
│  RIGHT HAND WEAPON SLOT │         │  LEFT HAND WEAPON SLOT  │
│   (UnifiedSlot)         │         │   (UnifiedSlot)         │
└─────────────────────────┘         └─────────────────────────┘
         ↓ SetItem()                         ↓ SetItem()
         ↓                                   ↓
    OnSlotChanged                       OnSlotChanged
    Event Fires                         Event Fires
         ↓                                   ↓
         ↓                                   ↓
  WeaponEquipmentManager              WeaponEquipmentManager
  .CheckRightHandEquipment()          .CheckLeftHandEquipment()
         ↓                                   ↓
         ↓                                   ↓
  PlayerShooterOrchestrator           PlayerShooterOrchestrator
  .SetSwordAvailable(true)            .SetLeftSwordAvailable(true)
         ↓                                   ↓
         ↓                                   ↓
  PlayerShooterOrchestrator           PlayerShooterOrchestrator
  .ToggleSwordMode()                  .ToggleLeftSwordMode()
         ↓                                   ↓
         ↓                                   ↓
  RIGHT SWORD ACTIVATED ✅           LEFT SWORD ACTIVATED ✅
  (Auto-equip complete)               (Auto-equip complete)
```

---

## 🌍 World Pickup Flow

```
╔═══════════════════════════════════════════════════════════════════╗
║                    WORLD SWORD PICKUP                             ║
╚═══════════════════════════════════════════════════════════════════╝

Player walks near sword
        ↓
Distance < 250 units
        ↓
"Press E to pickup" appears
        ↓
Player presses E
        ↓
WorldSwordPickup.TryPickupSword()
        ↓
        ├─────────────────────┬─────────────────────┐
        ↓                     ↓                     ↓
  Check handType      RIGHT HAND?           LEFT HAND?
        ↓                     ↓                     ↓
        ├→ RightHand          ↓                     ↓
        │                     ↓                     ↓
        │         rightHandWeaponSlot    leftHandWeaponSlot
        │              empty?                 empty?
        │                     ↓                     ↓
        │                   YES ✅               YES ✅
        │                     ↓                     ↓
        │            Auto-equip to         Auto-equip to
        │         rightHandWeaponSlot   leftHandWeaponSlot
        │                     ↓                     ↓
        │                SetItem()              SetItem()
        │                     ↓                     ↓
        │              OnSlotChanged          OnSlotChanged
        │                     ↓                     ↓
        │            ToggleSwordMode()    ToggleLeftSwordMode()
        │                     ↓                     ↓
        │              RIGHT ACTIVATED     LEFT ACTIVATED
        │                     ↓                     ↓
        └─────────────────────┴─────────────────────┘
                              ↓
                    Play pickup sound
                    Show notification
                    Destroy world item ✅
```

---

## ⚔️ Dual-Wielding Combat Flow

```
╔═══════════════════════════════════════════════════════════════════╗
║                    DUAL-WIELDING IN ACTION                        ║
╚═══════════════════════════════════════════════════════════════════╝

SCENARIO: Player has BOTH swords equipped and active

┌────────────────────────────────────────────────────────────────┐
│  IsSwordModeActive = TRUE    |   IsLeftSwordModeActive = TRUE  │
│  (RIGHT HAND READY)          |   (LEFT HAND READY)            │
└────────────────────────────────────────────────────────────────┘
              ↓                              ↓
              ↓                              ↓
        Player input:                  Player input:
        Press RMB                      Press LMB
              ↓                              ↓
    HandleSecondaryTap()            HandlePrimaryTap()
              ↓                              ↓
    Checks IsSwordModeActive       Checks IsLeftSwordModeActive
              ↓                              ↓
          TRUE ✅                         TRUE ✅
              ↓                              ↓
    TriggerSwordAttack()          TriggerLeftSwordAttack()
              ↓                              ↓
    RIGHT hand attacks            LEFT hand attacks
              ↓                              ↓
              └──────────────┬───────────────┘
                             ↓
              BOTH ATTACKS EXECUTE INDEPENDENTLY!
                             ↓
                    Can overlap animations
                    Can charge simultaneously
                    Can toggle independently
```

---

## 🎯 State Machine Diagram

```
╔═══════════════════════════════════════════════════════════════════╗
║                  HAND MODE STATE MACHINE                          ║
╚═══════════════════════════════════════════════════════════════════╝

RIGHT HAND STATES:                    LEFT HAND STATES:
┌─────────────┐                       ┌─────────────┐
│  SHOOTING   │                       │  SHOOTING   │
│   MODE      │◄──Mouse4──┐           │   MODE      │◄──Mouse5──┐
│ (DEFAULT)   │           │           │ (DEFAULT)   │           │
└─────────────┘           │           └─────────────┘           │
      │                   │                 │                   │
      │ Mouse4            │                 │ Mouse5            │
      ↓                   │                 ↓                   │
┌─────────────┐           │           ┌─────────────┐           │
│   SWORD     │───────────┘           │   SWORD     │───────────┘
│    MODE     │                       │    MODE     │
│  (ACTIVE)   │                       │  (ACTIVE)   │
└─────────────┘                       └─────────────┘
      │                                     │
      │ RMB                                 │ LMB
      ↓                                     ↓
┌─────────────┐                       ┌─────────────┐
│  ATTACKING  │                       │  ATTACKING  │
│   (0.3s)    │                       │   (0.3s)    │
└─────────────┘                       └─────────────┘
      │                                     │
      │ Hold RMB                            │ Hold LMB
      ↓                                     ↓
┌─────────────┐                       ┌─────────────┐
│  CHARGING   │                       │  CHARGING   │
│  (1s min)   │                       │  (1s min)   │
└─────────────┘                       └─────────────┘
      │                                     │
      │ Release                             │ Release
      ↓                                     ↓
┌─────────────┐                       ┌─────────────┐
│   POWER     │                       │   POWER     │
│  ATTACK     │                       │  ATTACK     │
│  (0.5s)     │                       │  (0.5s)     │
└─────────────┘                       └─────────────┘

INDEPENDENT STATE MACHINES!
→ Both can be in different states simultaneously
→ Both can attack while other charges
→ Both can toggle independently
```

---

## 🔧 Component Architecture

```
╔═══════════════════════════════════════════════════════════════════╗
║                  SYSTEM COMPONENT DIAGRAM                         ║
╚═══════════════════════════════════════════════════════════════════╝

                        Player GameObject
                               │
                ┌──────────────┼──────────────┐
                ↓              ↓              ↓
    ┌─────────────────┐ ┌────────────┐ ┌────────────┐
    │WeaponEquipment  │ │PlayerShooter│ │Layered Hand│
    │   Manager       │ │ Orchestrator│ │ Animation  │
    │   (Singleton)   │ │             │ │ Controller │
    └─────────────────┘ └────────────┘ └────────────┘
            │                  │              │
            │                  │              ├──leftHandController
            │                  │              └──rightHandController
            │                  │
    ┌───────┴────────┐        │
    ↓                ↓        │
┌─────────┐    ┌─────────┐   │
│rightHand│    │leftHand │   │
│WeaponSlot│   │WeaponSlot│  │
│(UnifiedSlot)│ │(UnifiedSlot)│
└─────────┘    └─────────┘   │
    │                │        │
    │ OnSlotChanged  │ OnSlotChanged
    ↓                ↓        │
    └────────┬───────┘        │
             ↓                │
       CheckEquipment()       │
             ↓                │
             └────────────────┘
                     ↓
              SetSwordAvailable()
              ToggleSwordMode()
                     ↓
              SWORD ACTIVATED ✅


                     Sword Visual
                          │
                ┌─────────┴─────────┐
                ↓                   ↓
        ┌──────────────┐    ┌──────────────┐
        │ RIGHT Sword  │    │ LEFT Sword   │
        │  GameObject  │    │  GameObject  │
        │              │    │              │
        │ swordDamage  │    │leftSwordDamage│
        │              │    │              │
        └──────────────┘    └──────────────┘
               ↓                    ↓
          DealDamage()         DealDamage()
          DealChargedDamage()  DealChargedDamage()
```

---

## 📊 Data Flow Summary

```
╔═══════════════════════════════════════════════════════════════════╗
║               COMPLETE DUAL-HAND DATA FLOW                        ║
╚═══════════════════════════════════════════════════════════════════╝

1. INPUT DETECTION
   Mouse4/Mouse5 → PlayerShooterOrchestrator.Update()
   LMB/RMB → PlayerInputHandler → Event callbacks

2. MODE TOGGLE
   ToggleSwordMode() / ToggleLeftSwordMode()
   ├─ Inventory sync (move items between slots)
   ├─ Availability check
   ├─ Activate/Deactivate visual GameObject
   └─ Trigger reveal/hide animation

3. ATTACK EXECUTION
   TriggerSwordAttack() / TriggerLeftSwordAttack()
   ├─ Check cooldown (SwordDamage.IsReady())
   ├─ Play attack sound
   ├─ Trigger animation (SwordAttack1T or SwordAttack2T)
   └─ Deal damage (SwordDamage.DealDamage())

4. CHARGE SYSTEM
   StartChargingSwordAttack() / StartChargingLeftSwordAttack()
   ├─ Set charging flag = true
   ├─ Record start time
   ├─ Play charge animation (SwordChargeT)
   └─ Start charge sound loop
   
   ReleaseChargedSwordAttack() / ReleaseChargedLeftSwordAttack()
   ├─ Stop charging flag = false
   ├─ Stop charge sound loop
   ├─ Calculate charge duration
   ├─ If duration >= 1s: POWER ATTACK (2x damage)
   └─ If duration < 1s: Normal attack

5. INVENTORY INTEGRATION
   Drag sword to slot → UnifiedSlot.SetItem()
   ├─ Fire OnSlotChanged event
   ├─ WeaponEquipmentManager.CheckXXXHandEquipment()
   ├─ PlayerShooterOrchestrator.SetXXXSwordAvailable(true)
   └─ Auto-activate sword mode
   
   Drag sword from slot → UnifiedSlot.ClearSlot()
   ├─ Fire OnSlotChanged event
   ├─ WeaponEquipmentManager.CheckXXXHandEquipment()
   ├─ PlayerShooterOrchestrator.SetXXXSwordAvailable(false)
   └─ Force deactivate sword mode

6. WORLD PICKUP
   Press E → WorldSwordPickup.TryPickupSword()
   ├─ Detect handType (RIGHT or LEFT)
   ├─ Check appropriate weapon slot empty
   ├─ Auto-equip to slot (SetItem)
   ├─ Wait 1 frame for event processing
   └─ Auto-activate sword mode (ToggleXXXSwordMode)
```

---

## ✅ System Validation Points

```
╔═══════════════════════════════════════════════════════════════════╗
║                  VALIDATION CHECKPOINTS                           ║
╚═══════════════════════════════════════════════════════════════════╝

CHECKPOINT 1: Properties Assigned
✓ PlayerShooterOrchestrator.leftSwordDamage
✓ PlayerShooterOrchestrator.leftSwordVisualGameObject
✓ WeaponEquipmentManager.leftHandWeaponSlot
→ If any missing: Sword system won't work for that hand

CHECKPOINT 2: Item Data Configuration
✓ EquippableWeaponItemData.weaponTypeID = "sword"
✓ EquippableWeaponItemData.handType = LEFT HAND or RIGHT HAND
✓ EquippableWeaponItemData.itemIcon assigned
→ If any missing: Pickup/equip will fail

CHECKPOINT 3: Slot Validation
✓ rightHandWeaponSlot.itemValidationMode = BypassAll
✓ leftHandWeaponSlot.itemValidationMode = BypassAll
→ Allows weapons to be equipped without restrictions

CHECKPOINT 4: Animation Setup
✓ LEFT hand animator has SwordRevealT trigger
✓ LEFT hand animator has SwordAttack1T trigger
✓ LEFT hand animator has SwordAttack2T trigger
✓ LEFT hand animator has SwordChargeT trigger
→ If missing: Attacks won't animate (but will still deal damage)

CHECKPOINT 5: Runtime Validation
✓ IsSwordModeActive changes when Mouse4 pressed
✓ IsLeftSwordModeActive changes when Mouse5 pressed
✓ swordVisualGameObject visible when RIGHT sword active
✓ leftSwordVisualGameObject visible when LEFT sword active
→ Debug.Log messages confirm state changes
```

---

## 🎯 Feature Completeness Matrix

```
╔═══════════════════════════════════════════════════════════════════╗
║              FEATURE PARITY - RIGHT vs LEFT                       ║
╚═══════════════════════════════════════════════════════════════════╝

Feature                    | RIGHT Hand | LEFT Hand | Status
---------------------------|------------|-----------|--------
Toggle Input               |   Mouse4   |  Mouse5   |   ✅
Attack Input               |    RMB     |   LMB     |   ✅
Charge Input               | Hold RMB   | Hold LMB  |   ✅
Mode Active Flag           | IsSwordModeActive | IsLeftSwordModeActive | ✅
Availability Flag          | _isSwordAvailable | _isLeftSwordAvailable | ✅
Damage Component           | swordDamage | leftSwordDamage | ✅
Visual GameObject          | swordVisualGameObject | leftSwordVisualGameObject | ✅
Attack Index Tracking      | _currentSwordAttackIndex | _currentLeftSwordAttackIndex | ✅
Charging Flag              | _isChargingSwordAttack | _isChargingLeftSwordAttack | ✅
Charge Start Time          | _swordChargeStartTime | _leftSwordChargeStartTime | ✅
Charge Sound Handle        | _swordChargeLoopHandle | _leftSwordChargeLoopHandle | ✅
Set Availability Method    | SetSwordAvailable() | SetLeftSwordAvailable() | ✅
Toggle Method              | ToggleSwordMode() | ToggleLeftSwordMode() | ✅
Can Use Check              | CanUseSwordMode() | CanUseLeftSwordMode() | ✅
Trigger Attack Method      | TriggerSwordAttack() | TriggerLeftSwordAttack() | ✅
Start Charging Method      | StartChargingSwordAttack() | StartChargingLeftSwordAttack() | ✅
Release Charging Method    | ReleaseChargedSwordAttack() | ReleaseChargedLeftSwordAttack() | ✅
Weapon Slot                | rightHandWeaponSlot | leftHandWeaponSlot | ✅
Equipment Check            | CheckRightHandEquipment() | CheckLeftHandEquipment() | ✅
Has Weapon Check           | HasRightHandWeapon() | HasLeftHandWeapon() | ✅
Get Weapon Method          | GetRightHandWeapon() | GetLeftHandWeapon() | ✅
World Pickup Detection     | handType == RightHand | handType == LeftHand | ✅
Auto-Equip Target          | rightHandWeaponSlot | leftHandWeaponSlot | ✅
Auto-Activate Coroutine    | ActivateSwordModeNextFrame(isRightHand=true) | ActivateSwordModeNextFrame(isRightHand=false) | ✅

TOTAL FEATURES: 25
IMPLEMENTED:    25
PARITY:         100% ✅
```

---

**Bottom Line**: Complete architectural symmetry achieved. LEFT hand is perfect mirror of RIGHT hand. ⚔️⚔️
