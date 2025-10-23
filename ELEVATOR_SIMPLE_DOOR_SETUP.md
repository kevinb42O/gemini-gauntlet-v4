# 🚪 SIMPLE ELEVATOR DOOR SYSTEM - Quick Setup

## 🎯 How It Works:

**Front Doors (Bottom Floor Entry/Exit):**
- Open when elevator arrives at BOTTOM
- Close when leaving bottom (going UP)

**Back Doors (Top Floor Exit):**
- Open when elevator arrives at TOP
- Close when leaving top (going DOWN)

**Flow:**
1. Player enters through FRONT doors at bottom
2. Press button → FRONT doors close
3. Elevator goes UP
4. Arrive at top → BACK doors open
5. Player exits through BACK
6. Press button → BACK doors close
7. Elevator goes DOWN
8. Arrive at bottom → FRONT doors open (ready for next player)

---

## 🔧 SETUP (5 Minutes):

### **STEP 1: Create Door GameObjects (Children of Elevator Car)**

**Front Doors:**
```
ElevatorCar (your elevator)
  └── FrontDoors (empty GameObject)
       ├── FrontLeftDoor (Cube, scale: 1, 3, 0.1)
       └── FrontRightDoor (Cube, scale: 1, 3, 0.1)
```

**Back Doors:**
```
ElevatorCar
  └── BackDoors (empty GameObject)
       ├── BackLeftDoor (Cube, scale: 1, 3, 0.1)
       └── BackRightDoor (Cube, scale: 1, 3, 0.1)
```

**Position them:**
- **Front Doors:** Position at front of elevator (Z = 1.5 or wherever front is)
- **Back Doors:** Position at back of elevator (Z = -1.5 or wherever back is)
- **Door spacing:** Left and right doors should touch in middle when closed

---

### **STEP 2: Add ElevatorDoorSimple Script**

**Front Doors:**
1. Select `FrontDoors` GameObject
2. Add Component → `ElevatorDoorSimple`
3. Configure:
   - **Left Door:** Drag `FrontLeftDoor` here
   - **Right Door:** Drag `FrontRightDoor` here
   - **Slide Distance:** `1.5` (how far doors slide open)
   - **Door Speed:** `2` (how fast they move)

**Back Doors:**
1. Select `BackDoors` GameObject
2. Add Component → `ElevatorDoorSimple`
3. Configure:
   - **Left Door:** Drag `BackLeftDoor` here
   - **Right Door:** Drag `BackRightDoor` here
   - **Slide Distance:** `1.5`
   - **Door Speed:** `2`

---

### **STEP 3: Connect Doors to Button Panel**

1. Select your `ButtonPanel` (the cube inside elevator)
2. In `ElevatorButtonPanel` component:
   - **Use Doors:** ✅ **Check this!**
   - **Front Doors:** Drag `FrontDoors` GameObject here
   - **Back Doors:** Drag `BackDoors` GameObject here
   - **Door Open Delay:** `0.5` (delay before doors open on arrival)

---

### **STEP 4: Test It!**

**At Bottom Floor:**
1. FRONT doors should be open (or opening on start)
2. Enter elevator through front
3. Press button cube (E key)
4. FRONT doors close
5. Elevator goes UP

**At Top Floor:**
6. BACK doors open automatically
7. Exit through back
8. Press button again
9. BACK doors close
10. Elevator goes DOWN
11. FRONT doors open at bottom

---

## 🎨 VISUAL POLISH (Optional):

### **Add Materials:**
```
Create → Material → Name: "DoorMetal"
Apply to all 4 door cubes
```

### **Add Audio:**
1. Select `FrontDoors` GameObject
2. Add Component → Audio Source
3. Assign door open/close sounds
4. Same for `BackDoors`

### **Make Button Look Better:**
Your button is just a cube for now - that's fine!
Later you can:
- Add a material/texture
- Change color on press
- Add a button mesh model
- Whatever you want!

---

## ⚙️ TWEAKING:

### **Door Speed:**
```
Door Speed = 2  → Normal (default)
Door Speed = 4  → Fast
Door Speed = 1  → Slow/dramatic
Door Speed = 10 → Instant
```

### **Slide Distance:**
```
Slide Distance = 1.5  → Normal (default)
Slide Distance = 2.0  → Wide opening
Slide Distance = 1.0  → Narrow opening
```

Must be enough for player to walk through!

### **Door Open Delay:**
```
Door Open Delay = 0.5s  → Default (opens 0.5s after arrival)
Door Open Delay = 0.0s  → Instant (opens immediately)
Door Open Delay = 1.0s  → Delayed (1 second wait)
```

---

## 🐛 TROUBLESHOOTING:

**Doors don't move:**
- ✅ Check Left Door and Right Door are assigned
- ✅ Check doors are children of door parent GameObject
- ✅ Check slide distance is > 0

**Doors slide wrong direction:**
- ✅ Adjust slide distance value
- ✅ Or swap left/right door assignments

**Wrong doors open:**
- ✅ Check Front Doors and Back Doors are assigned correctly
- ✅ Front = bottom floor entry
- ✅ Back = top floor exit

**Doors don't close when button pressed:**
- ✅ Check "Use Doors" is enabled
- ✅ Check door references are assigned
- ✅ Check ElevatorController is assigned

---

## 🎯 HIERARCHY EXAMPLE:

```
TowerElevatorSystem
├── TopFloorPosition (empty, Y=100)
├── BottomFloorPosition (empty, Y=0)
└── ElevatorCar (Y=100, has ElevatorController)
    ├── PlayerDetectionZone (empty)
    ├── ButtonPanel (cube, has ElevatorButtonPanel)
    │   └── InteractionUI (optional canvas)
    ├── FrontDoors (has ElevatorDoorSimple)
    │   ├── FrontLeftDoor (cube)
    │   └── FrontRightDoor (cube)
    └── BackDoors (has ElevatorDoorSimple)
        ├── BackLeftDoor (cube)
        └── BackRightDoor (cube)
```

**KEY POINT:** Front and Back doors are CHILDREN of ElevatorCar!
This means they travel WITH the elevator automatically! 🚀

---

## ✅ CHECKLIST:

- [ ] Created FrontDoors with left/right door cubes
- [ ] Created BackDoors with left/right door cubes
- [ ] Both door systems are children of ElevatorCar
- [ ] Added ElevatorDoorSimple to FrontDoors
- [ ] Added ElevatorDoorSimple to BackDoors
- [ ] Assigned left/right doors in both systems
- [ ] Set slide distance and speed
- [ ] Enabled "Use Doors" on ButtonPanel
- [ ] Assigned FrontDoors and BackDoors to ButtonPanel
- [ ] Tested: Front doors open at bottom
- [ ] Tested: Back doors open at top
- [ ] Tested: Doors close when leaving

---

## 🎉 YOU'RE DONE!

**Simple. No keycard. Just works.**

- Doors travel WITH elevator (they're children!)
- Front doors for entering (bottom floor)
- Back doors for exiting (top floor)
- Automatic opening/closing
- Button is just a cube (perfect!)

**Press E on the cube and enjoy your elevator!** 🚀
