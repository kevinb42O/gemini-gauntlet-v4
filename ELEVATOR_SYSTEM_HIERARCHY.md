# Elevator System - Unity Hierarchy Setup

## Scene Hierarchy Structure

```
Scene
├─ ChestSystem (or GameManager)
│  └─ ChestInteractionSystem (Component)
│     └─ Possible Items
│        ├─ [Other items...]
│        └─ ElevatorKeycard ← ADD THIS
│
├─ ElevatorDoor (GameObject)
│  ├─ ElevatorDoor (Component) ← SCRIPT
│  │  ├─ Required Keycard: ElevatorKeycard
│  │  ├─ Left Door: → LeftDoor
│  │  ├─ Right Door: → RightDoor
│  │  ├─ Elevator Arrival Delay: 20
│  │  ├─ Door Slide Distance: 2
│  │  └─ Interaction Distance: 3
│  │
│  ├─ BoxCollider (Is Trigger: ✓)
│  │
│  ├─ LeftDoor (Child GameObject)
│  │  ├─ Transform: Position at left side
│  │  ├─ MeshFilter: Cube or custom mesh
│  │  └─ MeshRenderer: Door material
│  │
│  └─ RightDoor (Child GameObject)
│     ├─ Transform: Position at right side
│     ├─ MeshFilter: Cube or custom mesh
│     └─ MeshRenderer: Door material
│
└─ ExitZone (GameObject)
   ├─ Transform: Behind elevator doors
   ├─ ExitZone (Component)
   └─ Collider (Is Trigger: ✓)
```

---

## Project Assets Structure

```
Assets/
├─ scripts/
│  ├─ ElevatorKeycardData.cs ← NEW SCRIPT
│  └─ ElevatorDoor.cs ← NEW SCRIPT
│
├─ [Your Items Folder]/
│  └─ ElevatorKeycard.asset ← CREATE THIS
│     └─ (ScriptableObject: ElevatorKeycardData)
│
└─ [Your Materials Folder]/
   ├─ DoorLockedMaterial.mat (Optional - Red)
   └─ DoorUnlockedMaterial.mat (Optional - Green)
```

---

## Visual Layout (Top View)

```
                PLAYER
                  ↓
                  
    ┌─────────────────────────┐
    │   Interaction Zone      │
    │   (BoxCollider Trigger) │
    │                         │
    │    ┌──────┬──────┐     │
    │    │      │      │     │ ← CLOSED DOORS
    │    │ Left │Right │     │   (Initial State)
    │    │ Door │ Door │     │
    │    └──────┴──────┘     │
    └─────────────────────────┘
    
    After 20 seconds:
    
    ┌─────────────────────────┐
    │                         │
    ┌──────┐         ┌──────┐
    │      │         │      │ ← OPEN DOORS
    │ Left │         │Right │   (Slid outward)
    │ Door │         │ Door │
    └──────┘         └──────┘
    │                         │
    │      [EXIT ZONE]        │ ← Now accessible
    │                         │
    └─────────────────────────┘
```

---

## Component Inspector Setup

### ElevatorDoor Component

```
┌─────────────────────────────────────────┐
│ Elevator Door (Script)                  │
├─────────────────────────────────────────┤
│ Elevator Keycard Requirement            │
│ ├─ Required Keycard: [ElevatorKeycard] │
│                                         │
│ Door References                         │
│ ├─ Left Door: [LeftDoor]               │
│ └─ Right Door: [RightDoor]             │
│                                         │
│ Door Settings                           │
│ ├─ Door Slide Distance: 2              │
│ ├─ Door Open Speed: 2                  │
│ └─ Elevator Arrival Delay: 20          │
│                                         │
│ Interaction Settings                    │
│ └─ Interaction Distance: 3             │
│                                         │
│ Visual Feedback                         │
│ ├─ Locked Material: [Optional]         │
│ ├─ Unlocked Material: [Optional]       │
│ ├─ Locked Color: (255, 0, 0)          │
│ ├─ Unlocked Color: (0, 255, 0)        │
│ └─ Arriving Color: (255, 255, 0)      │
│                                         │
│ Interaction UI                          │
│ ├─ Interaction UI: [Your UI Panel]    │
│ └─ Interaction Text: [Text Component]  │
│                                         │
│ Audio                                   │
│ ├─ Elevator Called Sound: [AudioClip] │
│ ├─ Elevator Arrival Sound: [AudioClip]│
│ ├─ Door Open Sound: [AudioClip]       │
│ └─ Locked Sound: [AudioClip]          │
└─────────────────────────────────────────┘
```

---

## Door Transform Setup

### Initial Positions (CLOSED)

```
ElevatorDoor (Parent)
├─ Position: (0, 0, 0)
│
├─ LeftDoor
│  └─ Local Position: (-1, 0, 0) ← Touching center
│
└─ RightDoor
   └─ Local Position: (1, 0, 0) ← Touching center
```

### After Opening (doorSlideDistance = 2)

```
ElevatorDoor (Parent)
├─ Position: (0, 0, 0) ← Unchanged
│
├─ LeftDoor
│  └─ Local Position: (-3, 0, 0) ← Slid left by 2 units
│
└─ RightDoor
   └─ Local Position: (3, 0, 0) ← Slid right by 2 units
```

---

## State Flow Diagram

```
┌─────────────┐
│   LOCKED    │ ← Initial State
│   (Red)     │
└──────┬──────┘
       │ Player uses keycard
       ↓
┌─────────────┐
│  ARRIVING   │ ← 20-second countdown
│  (Yellow)   │
└──────┬──────┘
       │ Timer reaches 0
       ↓
┌─────────────┐
│  ARRIVED    │ ← Doors opening
│  (Green)    │
└──────┬──────┘
       │ Doors fully open
       ↓
┌─────────────┐
│    OPEN     │ ← ExitZone accessible
│  (Green)    │
└─────────────┘
```

---

## Interaction Messages

```
State: LOCKED (No Keycard)
Message: "Requires Elevator Keycard"

State: LOCKED (Has Keycard)
Message: "Press E to call elevator (uses Elevator Keycard)"

State: ARRIVING
Message: "Elevator arriving in 15s..." (countdown)

State: ARRIVED
Message: "Elevator has arrived! Doors opening..."

State: OPEN
Message: (No message - doors are open)
```

---

## Gizmo Visualization (Scene View)

```
When ElevatorDoor is selected in Hierarchy:

┌─────────────────────────┐
│   Interaction Range     │ ← Wire cube (red/yellow/green)
│   (interactionDistance) │
│                         │
│    ┌──────┬──────┐     │
│    │      │      │     │
│    └──────┴──────┘     │
│         ↓         ↓    │
│        ●─────────●     │ ← Slide direction indicators
│     (cyan spheres)     │
└─────────────────────────┘
```

---

## Chest Loot Configuration

```
ChestInteractionSystem
└─ Possible Items (Array)
   ├─ [0] ArmorPlate
   ├─ [1] SelfRevive
   ├─ [2] Backpack
   ├─ [3] Vest
   ├─ [4] GoodsItem
   └─ [5] ElevatorKeycard ← ADD HERE
```

**Spawn Behavior:**
- Rarity: 5 (Legendary)
- Spawn Chance: Very rare
- Stack Size: 1 (no stacking)
- Persistence: NO (scene-specific)

---

## Testing Workflow

### 1. Editor Testing (No Keycard Needed)
```
1. Select ElevatorDoor in Hierarchy
2. Right-click component header
3. Choose "Test Call Elevator"
4. Watch 20-second countdown
5. Verify doors open automatically
```

### 2. Play Mode Testing (Full Flow)
```
1. Start game
2. Open chest → Find ElevatorKeycard
3. Approach elevator → See prompt
4. Press E → Keycard consumed
5. Wait 20 seconds → Countdown visible
6. Doors open → Enter ExitZone
```

---

## Summary

**3 Main Components:**
1. **ElevatorKeycardData** - The rare item
2. **ElevatorDoor** - The door script
3. **ExitZone** - The exit (already exists)

**Setup Time:** ~5 minutes
**Complexity:** Low (uses existing systems)
**Result:** Professional elevator system with tension!

See `ELEVATOR_KEYCARD_SYSTEM_SETUP.md` for detailed instructions.
