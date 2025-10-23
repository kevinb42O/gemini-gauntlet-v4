# Chest Interaction Flow - Quick Reference

## Manual Chests (Pre-placed in Scene)

```
┌─────────────────────────────────────────────────────────────┐
│                    MANUAL CHEST FLOW                        │
└─────────────────────────────────────────────────────────────┘

1. SCENE START
   ├─ Chest is VISIBLE
   ├─ Chest is CLOSED (lid down)
   └─ Waiting for player

2. PLAYER APPROACHES & PRESSES E (First Time)
   ├─ Chest lid begins OPENING animation
   ├─ Play opening sound
   ├─ Chest state: Closed → Opening → Open
   └─ NO gems spawn
   └─ NO inventory opens yet

3. CHEST FULLY OPEN
   ├─ Lid is fully raised
   ├─ Chest state: Open
   └─ Ready for inventory interaction

4. PLAYER PRESSES E (Second Time)
   ├─ Inventory UI opens
   ├─ Chest inventory shows items (NO GEMS)
   ├─ Grant XP
   ├─ Track mission progress
   └─ Player can take items

5. PLAYER CLOSES INVENTORY
   ├─ Chest remains OPEN
   └─ Can be reopened anytime with E
```

---

## Spawned Chests (Platform Conquest)

```
┌─────────────────────────────────────────────────────────────┐
│                   SPAWNED CHEST FLOW                        │
└─────────────────────────────────────────────────────────────┘

1. PLATFORM CONQUEST
   ├─ Player destroys all towers
   ├─ ChestManager spawns chest
   └─ Chest is HIDDEN (underground)

2. AUTOMATIC EMERGENCE
   ├─ Chest rises from ground
   ├─ Play emergence sound/effects
   ├─ Chest state: Hidden → Emerging → Closed
   └─ No player input needed

3. AUTOMATIC OPENING
   ├─ Chest lid opens automatically
   ├─ Play opening sound
   ├─ Chest state: Closed → Opening → Open
   └─ No player input needed

4. GEM SPAWNING
   ├─ Gems eject from chest
   ├─ Scatter around chest
   ├─ Play gem spawn sounds
   └─ Player can collect gems

5. PLAYER PRESSES E
   ├─ Inventory UI opens
   ├─ Chest inventory shows items + gems
   ├─ Grant XP
   ├─ Track mission progress
   └─ Player can take items

6. PLAYER CLOSES INVENTORY
   ├─ Chest remains OPEN
   └─ Can be reopened anytime with E
```

---

## Key Differences

| Feature | Manual Chest | Spawned Chest |
|---------|-------------|---------------|
| **Placement** | Pre-placed in scene | Spawned by ChestManager |
| **Initial State** | Closed (visible) | Hidden (underground) |
| **Emergence** | None | Automatic animation |
| **Opening Trigger** | Player presses E | Automatic after emergence |
| **Gem Spawning** | ❌ NO | ✅ YES |
| **First Interaction** | Opens lid | Opens inventory |
| **Use Case** | Exploration/secrets | Platform rewards |

---

## Player Experience

### Manual Chest Discovery
```
Player: "Oh, a chest!"
        [Presses E]
        "It's opening..."
        [Waits for lid animation]
        [Presses E again]
        "Got the loot!"
```

### Spawned Chest Discovery
```
Player: "Last tower down!"
        [Chest emerges automatically]
        "A chest appeared!"
        [Chest opens automatically]
        [Gems scatter]
        "Gems! Let me collect them..."
        [Presses E]
        "Got the loot too!"
```

---

## Configuration Checklist

### For Manual Chests
- ✅ Set `Chest Type` to **Manual**
- ✅ Place in scene at desired location
- ✅ Configure chest body and lid transforms
- ✅ Set opening animation settings
- ❌ Ignore gem settings (not used)
- ✅ Chest will stay closed until player interacts

### For Spawned Chests
- ✅ ChestManager handles everything
- ✅ Assign chest prefabs to ChestManager
- ✅ Configure gem prefab and count
- ✅ Set emergence/opening animations
- ✅ Chest will auto-emerge and auto-open
