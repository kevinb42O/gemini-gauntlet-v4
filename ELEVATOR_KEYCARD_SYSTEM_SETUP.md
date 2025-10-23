# Elevator Keycard System - Complete Setup Guide

## Overview
The Elevator Keycard System provides a rare, one-time use item that opens special elevator doors with a 20-second arrival delay. This creates tension and strategic gameplay as players must wait for the elevator to arrive before accessing the exit zone.

## System Components

### 1. ElevatorKeycardData (ChestItemData)
**File:** `Assets/scripts/ElevatorKeycardData.cs`

**Features:**
- Extends `ChestItemData` for full inventory integration
- **One-time use** - consumed when used
- **NOT persistent across scenes** - will be lost on scene transition
- **Legendary rarity (5)** - very rare spawn chance
- **Gold color** - visually distinct from other items
- **Does NOT stack** - each keycard is unique

### 2. ElevatorDoor Script
**File:** `Assets/scripts/ElevatorDoor.cs`

**Features:**
- Dual sliding doors (left and right)
- 20-second elevator "arrival" delay after keycard use
- Visual feedback (red → yellow → green)
- Audio feedback for all states
- Countdown timer display
- Automatic door opening when elevator arrives
- Works with existing ExitZone system

---

## Unity Setup Instructions

### Step 1: Create the Elevator Keycard Item

1. **Create the ScriptableObject:**
   - In Unity Project window, navigate to `Assets/` folder
   - Right-click → `Create` → `Inventory` → `Elevator Keycard`
   - Name it: `ElevatorKeycard`

2. **Configure the Keycard:**
   - **Item Name:** "Elevator Keycard"
   - **Description:** "A rare keycard that calls the elevator. One-time use only."
   - **Item Icon:** Assign a gold/yellow keycard sprite
   - **Item Type:** "ElevatorKeycard" (auto-set)
   - **Item Rarity:** 5 (Legendary - auto-set)
   - **Rarity Color:** Gold (1, 0.84, 0) - auto-set
   - **Is One Time Use:** ✓ Checked

3. **DO NOT place in Resources folder** - This item should NOT be persistent across scenes

### Step 2: Add Elevator Keycard to Chest Loot Table

1. **Find ChestInteractionSystem in your scene:**
   - Usually attached to a GameObject like "ChestSystem" or "GameManager"

2. **Add to Possible Items list:**
   - Expand `Possible Items` array
   - Increase size by 1
   - Drag the `ElevatorKeycard` asset into the new slot

3. **Configure Spawn Chance:**
   - The keycard will spawn randomly like other chest items
   - **Recommended:** Keep it rare (it's already legendary rarity 5)
   - Consider adding special spawn logic if you want even more control

### Step 3: Create Elevator Door GameObject

1. **Create Parent GameObject:**
   - Create empty GameObject: `ElevatorDoor`
   - Position it where you want the elevator entrance

2. **Create Door Meshes:**
   - Create two door objects as children:
     - `LeftDoor` (3D cube or custom mesh)
     - `RightDoor` (3D cube or custom mesh)
   - Position them side-by-side to form a closed door
   - **Important:** Doors should be in their CLOSED position by default

3. **Add ElevatorDoor Script:**
   - Select the parent `ElevatorDoor` GameObject
   - Add Component → `ElevatorDoor` script

4. **Configure ElevatorDoor Component:**

   **Elevator Keycard Requirement:**
   - **Required Keycard:** Drag your `ElevatorKeycard` asset here

   **Door References:**
   - **Left Door:** Drag `LeftDoor` child object
   - **Right Door:** Drag `RightDoor` child object

   **Door Settings:**
   - **Door Slide Distance:** 2.0 (adjust based on door size)
   - **Door Open Speed:** 2.0 (how fast doors slide)
   - **Elevator Arrival Delay:** 20.0 seconds (the waiting period)

   **Interaction Settings:**
   - **Interaction Distance:** 3.0 (how close player must be)

   **Visual Feedback:**
   - **Locked Material:** (Optional) Red material for locked state
   - **Unlocked Material:** (Optional) Green material for unlocked state
   - **Locked Color:** Red (255, 0, 0)
   - **Unlocked Color:** Green (0, 255, 0)
   - **Arriving Color:** Yellow (255, 255, 0)

   **Interaction UI:**
   - **Interaction UI:** Drag your interaction prompt UI GameObject
   - **Interaction Text:** Drag the Text component from the UI

   **Audio:**
   - **Elevator Called Sound:** Sound when keycard is used
   - **Elevator Arrival Sound:** Sound when elevator arrives
   - **Door Open Sound:** Sound when doors slide open
   - **Locked Sound:** Sound when player tries without keycard

5. **Setup Collider:**
   - The script will auto-add a BoxCollider if none exists
   - Make sure it's set to **Is Trigger: ✓**
   - Adjust size to cover interaction area

### Step 4: Position ExitZone Behind Doors

1. **Find or Create ExitZone:**
   - Your existing `ExitZone` GameObject (the portal/exit)

2. **Position Behind Elevator Doors:**
   - Place the ExitZone directly behind the closed elevator doors
   - Players should only be able to reach it when doors are open

3. **Test Positioning:**
   - Ensure ExitZone is not accessible when doors are closed
   - Ensure ExitZone is easily accessible when doors are open

---

## How It Works (Player Experience)

1. **Player finds Elevator Keycard in chest** (rare drop)
2. **Player approaches elevator doors** (closed, red color)
3. **Interaction prompt appears:** "Press E to call elevator (uses Elevator Keycard)"
4. **Player presses E:**
   - Keycard is consumed (removed from inventory)
   - Message: "Elevator called! Arriving in 20 seconds..."
   - Doors turn yellow
   - Countdown begins
5. **20-second wait period:**
   - UI shows: "Elevator arriving in X seconds..."
   - Doors remain yellow
   - Player must wait (creates tension!)
6. **Elevator arrives:**
   - Message: "Elevator has arrived! Doors opening..."
   - Doors turn green
   - Doors slide open automatically
   - ExitZone is now accessible
7. **Player enters ExitZone** to complete the level

---

## Technical Details

### Keycard Consumption
- **One-time use:** Keycard is removed from inventory when used
- **Stack handling:** If player has multiple keycards (unlikely), only 1 is consumed
- **Inventory save:** Changes are saved immediately

### Door Animation
- **Dual sliding:** Left door slides left, right door slides right
- **Smooth interpolation:** Uses smooth step curve for professional feel
- **Local space:** Doors slide in local space (works with any rotation)

### State Management
- **Locked:** Initial state, red color, requires keycard
- **Arriving:** Yellow color, countdown active, cannot be interrupted
- **Arrived:** Green color, doors opening automatically
- **Open:** Doors fully open, ExitZone accessible

### Scene Persistence
- **NOT persistent:** Keycard does NOT carry over to next scene
- **Scene-specific:** Each scene needs its own elevator setup
- **Fresh start:** Players must find new keycard in each scene

---

## Customization Options

### Adjust Arrival Time
```csharp
// In ElevatorDoor component
public float elevatorArrivalDelay = 20f; // Change to any value
```

### Adjust Door Slide Distance
```csharp
// In ElevatorDoor component
public float doorSlideDistance = 2f; // Increase for wider doors
```

### Make Keycard More/Less Rare
- Adjust `itemRarity` in ElevatorKeycardData (1-5)
- Add custom spawn logic in ChestInteractionSystem
- Create special "boss chests" that always contain keycard

### Add Multiple Elevators
- Create multiple ElevatorDoor GameObjects
- Each can require the same keycard type
- Player must choose which elevator to use (strategic choice!)

---

## Testing Checklist

### In-Editor Testing
1. **Test Call Elevator:**
   - Select ElevatorDoor in Hierarchy
   - Right-click component → `Test Call Elevator`
   - Verify 20-second countdown works
   - Verify doors open automatically

2. **Test Open Doors Immediately:**
   - Select ElevatorDoor in Hierarchy
   - Right-click component → `Test Open Doors Immediately`
   - Verify doors slide correctly

### In-Game Testing
1. ✓ Keycard spawns in chests (rare)
2. ✓ Keycard appears in inventory with gold color
3. ✓ Elevator doors show interaction prompt
4. ✓ Without keycard: Shows "Requires Elevator Keycard"
5. ✓ With keycard: Shows "Press E to call elevator"
6. ✓ Using keycard: Consumes 1 keycard from inventory
7. ✓ Countdown: Shows "Elevator arriving in X seconds..."
8. ✓ Arrival: Doors turn green and open automatically
9. ✓ ExitZone: Accessible after doors open
10. ✓ Scene transition: Keycard does NOT persist

---

## Troubleshooting

### "Keycard not spawning in chests"
- Check that ElevatorKeycard is in ChestInteractionSystem's `possibleItems` list
- Verify keycard rarity is set (5 = very rare, may take multiple chests)
- Try temporarily increasing spawn chance for testing

### "Doors not opening"
- Check that leftDoor and rightDoor are assigned in Inspector
- Verify doors are positioned correctly (closed state)
- Check doorSlideDistance value (may be too small)

### "Countdown not showing"
- Verify interactionUI and interactionText are assigned
- Check that UI is on correct Canvas layer
- Ensure player is in range (interactionDistance)

### "ExitZone accessible before doors open"
- Move ExitZone further behind doors
- Increase door size to fully block access
- Add invisible walls around ExitZone

### "Keycard persisting across scenes"
- This is intentional design - keycard should NOT persist
- If it does persist, check PersistentItemInventoryManager settings
- Keycard is NOT stored in Resources folder (correct behavior)

---

## Integration with Existing Systems

### Works With:
- ✓ ChestInteractionSystem (loot generation)
- ✓ InventoryManager (inventory management)
- ✓ UnifiedSlot system (drag & drop)
- ✓ ExitZone (level completion)
- ✓ CognitiveFeedManager (UI messages)
- ✓ GameSounds (audio feedback)

### Does NOT Interfere With:
- ✓ Other keycard types (Building21, etc.)
- ✓ Regular doors (KeycardDoor)
- ✓ Chest spawning system
- ✓ Persistent inventory

---

## Future Enhancements (Optional)

### Multiple Elevator Types
- Create different keycard colors (Gold, Silver, Bronze)
- Each opens different elevator tiers
- Higher tiers = better loot areas

### Elevator Shaft Visuals
- Add animated elevator shaft behind doors
- Show elevator "arriving" with moving platform
- Add cable/pulley animations

### Emergency Override
- Add backup power system
- Player can manually open doors (takes longer)
- Requires special tool or multiple players

### Elevator Interior
- Create actual elevator room
- Player enters elevator
- Elevator "moves" to different floor
- Doors open to new area

---

## Code Reference

### Key Methods

**ElevatorDoor.cs:**
- `TryCallElevator()` - Handles keycard consumption and starts countdown
- `StartElevatorArrival()` - Begins 20-second timer
- `ElevatorArrived()` - Called when timer reaches 0
- `OpenDoors()` - Animates door sliding

**ElevatorKeycardData.cs:**
- `IsSameItem()` - Prevents stacking (each keycard unique)
- `OnEnable()` - Sets default properties (legendary rarity, gold color)

---

## Summary

The Elevator Keycard System adds strategic depth and tension to your game:
- **Rare resource** - Players must decide when to use it
- **Time pressure** - 20-second wait creates vulnerability
- **One-time use** - No save-scumming or hoarding
- **Scene-specific** - Fresh challenge each level
- **Professional feel** - Smooth animations and feedback

This system integrates seamlessly with your existing chest, inventory, and exit zone systems while maintaining the non-persistent design you requested.

**Enjoy your new elevator system!** 🎮🚪
