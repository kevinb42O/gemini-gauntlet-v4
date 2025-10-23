# Chest System - Inspector Setup Guide

## CRITICAL: You MUST assign these in Unity Inspector!

### Step 1: Find ChestInteractionSystem GameObject
Look for the GameObject that has the `ChestInteractionSystem` component attached.
This is usually on:
- GameManager
- Player
- A dedicated "ChestSystem" GameObject

### Step 2: Assign Camera Controller
```
ChestInteractionSystem Component
├── Player Input Control (Header)
│   ├── Camera Controller: [DRAG AAACameraController HERE]
│   └── Shooter Script: [DRAG PlayerShooterOrchestrator HERE]
```

**How to assign:**
1. Find your Player GameObject in the hierarchy
2. Look for the `AAACameraController` component (usually on Player or Camera child)
3. Drag the **component** (not the GameObject) into the `Camera Controller` field

### Step 3: Assign Shooter Script
1. Find your Player GameObject in the hierarchy
2. Look for the `PlayerShooterOrchestrator` component
3. Drag the **component** (not the GameObject) into the `Shooter Script` field

### Step 4: Verify Movement Threshold
- Should show: `Movement Threshold: 2.5`
- This is the distance (in units) the player can walk before chest auto-closes
- Adjust if needed:
  - **Increase** (3.0-4.0) = Player can walk further before auto-close
  - **Decrease** (1.5-2.0) = Chest closes sooner when walking away

---

## What Happens If You Don't Assign Them?

### If Camera Controller is NOT assigned:
- ⚠️ Warning in console: `No camera controller assigned!`
- ❌ Player can still look around with mouse while chest is open
- ❌ Breaks immersion (player shouldn't be able to look around while browsing inventory)

### If Shooter Script is NOT assigned:
- ⚠️ Warning in console: `No shooter script assigned!`
- ❌ Player can still shoot while chest is open
- ❌ Can cause bugs (shooting while UI is open)

---

## Quick Visual Reference

```
Inspector View:
┌─────────────────────────────────────────────┐
│ ChestInteractionSystem (Script)             │
├─────────────────────────────────────────────┤
│ [Other fields...]                           │
│                                             │
│ ▼ Player Input Control                     │
│   Camera Controller:                        │
│   ┌─────────────────────────────────────┐  │
│   │ AAACameraController (MonoBehaviour) │  │ ← MUST ASSIGN
│   └─────────────────────────────────────┘  │
│                                             │
│   Shooter Script:                           │
│   ┌─────────────────────────────────────┐  │
│   │ PlayerShooterOrchestrator (...)     │  │ ← MUST ASSIGN
│   └─────────────────────────────────────┘  │
│                                             │
│ ▼ Player Movement Tracking                 │
│   Movement Threshold: 2.5                   │ ← Can adjust
└─────────────────────────────────────────────┘
```

---

## Testing After Setup

1. **Start the game**
2. **Open a chest**
3. **Check console for these messages:**
   ```
   🚫 Disabled camera look: AAACameraController
   🚫 Disabled shooting: PlayerShooterOrchestrator
   ✅ Player can still MOVE but cannot LOOK or SHOOT while chest is open
   ```

4. **If you see warnings instead:**
   ```
   ⚠️ No camera controller assigned! Assign AAACameraController in Inspector.
   ⚠️ No shooter script assigned! Assign PlayerShooterOrchestrator in Inspector.
   ```
   → Go back and assign them properly!

---

## Common Mistakes

### ❌ WRONG: Dragging the GameObject
```
Camera Controller: [Player GameObject] ← WRONG!
```

### ✅ CORRECT: Dragging the Component
```
Camera Controller: [AAACameraController (MonoBehaviour)] ← CORRECT!
```

**How to tell the difference:**
- GameObject drag shows: `Player` or `Main Camera`
- Component drag shows: `AAACameraController (MonoBehaviour)`

---

## Troubleshooting

### "I can't find AAACameraController!"
- Check if it's on the Player GameObject
- Check if it's on a child Camera GameObject
- Search in hierarchy: Type "camera" in search box

### "I can't find PlayerShooterOrchestrator!"
- It should be on the Player GameObject
- Search in hierarchy: Type "shooter" in search box
- Check if the script exists in your project

### "Movement threshold doesn't appear!"
- It's a private field with default value 2.5
- You won't see it in Inspector (it's in the code)
- If you want to adjust it, you can make it public in the code

---

## Done!
Once both fields are assigned, the chest system will work perfectly:
- ✅ Single press to open
- ✅ Can move while chest is open
- ✅ Auto-closes when walking away
- ✅ Camera and shooting disabled while browsing
