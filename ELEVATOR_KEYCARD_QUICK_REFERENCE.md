# Elevator Keycard System - Quick Reference

## 🎯 Quick Setup (5 Minutes)

### 1. Create Keycard Item
```
Right-click in Project → Create → Inventory → Elevator Keycard
Name: "ElevatorKeycard"
```

### 2. Add to Chest Loot
```
Find: ChestInteractionSystem in scene
Add: ElevatorKeycard to "Possible Items" array
```

### 3. Create Elevator Doors
```
Create Empty GameObject: "ElevatorDoor"
├─ LeftDoor (Cube mesh)
└─ RightDoor (Cube mesh)

Add Component: ElevatorDoor script
Assign:
  - Required Keycard: ElevatorKeycard asset
  - Left Door: LeftDoor child
  - Right Door: RightDoor child
```

### 4. Position Exit Zone
```
Place ExitZone behind closed elevator doors
```

**Done!** 🎉

---

## 🎮 Player Flow

1. **Find keycard** in chest (rare)
2. **Approach elevator** → "Press E to call elevator"
3. **Use keycard** → Consumed, countdown starts
4. **Wait 20 seconds** → "Elevator arriving in X seconds..."
5. **Doors open** → Access ExitZone

---

## ⚙️ Key Settings

| Setting | Default | Purpose |
|---------|---------|---------|
| Elevator Arrival Delay | 20s | Wait time after using keycard |
| Door Slide Distance | 2.0 | How far doors slide open |
| Door Open Speed | 2.0 | How fast doors slide |
| Interaction Distance | 3.0 | How close player must be |

---

## 🎨 Visual States

- **🔴 Red** = Locked (needs keycard)
- **🟡 Yellow** = Arriving (countdown active)
- **🟢 Green** = Arrived (doors opening)

---

## 🔧 Testing Commands

**In-Editor (Select ElevatorDoor):**
- Right-click → `Test Call Elevator` (starts 20s countdown)
- Right-click → `Test Open Doors Immediately` (skip countdown)

---

## ⚠️ Important Notes

- ✅ **One-time use** - Keycard consumed when used
- ✅ **NOT persistent** - Does NOT carry across scenes
- ✅ **Legendary rarity** - Very rare spawn
- ✅ **Gold color** - Visually distinct
- ✅ **No stacking** - Each keycard is unique

---

## 🐛 Common Issues

**Keycard not spawning?**
→ Check it's in ChestInteractionSystem.possibleItems

**Doors not opening?**
→ Verify leftDoor and rightDoor are assigned

**Countdown not showing?**
→ Assign interactionUI and interactionText

**ExitZone accessible too early?**
→ Move ExitZone further behind doors

---

## 📁 Files Created

- `Assets/scripts/ElevatorKeycardData.cs` - Keycard item definition
- `Assets/scripts/ElevatorDoor.cs` - Door script with 20s delay
- `ELEVATOR_KEYCARD_SYSTEM_SETUP.md` - Full documentation

---

## 🚀 Quick Customization

**Change wait time:**
```csharp
// In ElevatorDoor component
elevatorArrivalDelay = 30f; // Change to any value
```

**Make keycard more common:**
```csharp
// In ElevatorKeycardData
itemRarity = 3; // Lower = more common (1-5)
```

**Wider doors:**
```csharp
// In ElevatorDoor component
doorSlideDistance = 3f; // Increase for wider opening
```

---

That's it! See `ELEVATOR_KEYCARD_SYSTEM_SETUP.md` for detailed documentation.
