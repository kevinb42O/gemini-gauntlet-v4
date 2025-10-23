# Keycard System - Quick Reference

## 5 Keycards Created

| Keycard Name | Color | Rarity | Description | Usage |
|--------------|-------|--------|-------------|-------|
| **Building21 Keycard** | Orange | Rare (3) | Access to Building 21 | **Infinite Use** ♾️ |
| **Green Keycard** | Green | Uncommon (2) | Opens green access panels | One-Time Use |
| **Blue Keycard** | Blue | Rare (3) | Opens blue access panels | One-Time Use |
| **Black Keycard** | Black | Legendary (5) | High-security access | One-Time Use |
| **Red Keycard** | Red | Epic (4) | Emergency access | One-Time Use |

---

## Quick Setup Steps

### 1. Create Keycard ScriptableObjects
```
Right-click in Assets > Create > Inventory > Item
Name: [Keycard]_Keycard (e.g., Building21_Keycard)
Configure: Name, Description, Type="Keycard", Rarity, Color
```

### 2. Create Keycard Pickup in Scene
```
1. Create Empty GameObject
2. Add KeycardItem.cs script
3. Assign keycard ScriptableObject to "Keycard Data"
4. Add visual (Cube/Model as child)
5. Set color to match keycard
```

### 3. Create Door in Scene
```
1. Create Cube (or door model)
2. Add KeycardDoor.cs script
3. Assign keycard ScriptableObject to "Required Keycard"
4. Set "Open Type" (SlideUp, RotateLeft, etc.)
5. Set "Open Distance" (how far it moves)
```

---

## Player Interaction

### Collecting Keycards (World Pickups)
- Walk near keycard
- See prompt: "Press E to collect [Keycard Name]"
- Press **E** to collect
- Keycard added to inventory
- Check inventory with **Tab**

### Collecting Keycards (From Chests)
- Find and open a chest
- Keycards may appear in chest loot (3-6 items per chest)
- Drag keycard from chest to inventory
- Close chest UI
- Keycard now in inventory

### Opening Doors
- Walk near door **without** keycard
  - See: "Requires [Keycard Name]"
  - Press E: Shows message "Requires keycard"
  
- Walk near door **with** keycard
  - See: "Press E to open with [Keycard Name]"
  - Press **E**: Door opens
  - **Building21 Keycard**: Stays in inventory (infinite use) ♾️
  - **Other Keycards**: Consumed (one-time use)

---

## Script Features

### KeycardItem.cs
- Auto-collection when player is near
- Bobbing and rotation animations
- Interaction UI prompts
- Inventory integration
- Collection sound effects

### KeycardDoor.cs
- Multiple open animations (slide, rotate, scale)
- Visual feedback (locked/unlocked colors)
- Audio feedback (locked, open, close sounds)
- Auto-close option
- Keycard consumption on use

---

## Door Animation Types

| Type | Description |
|------|-------------|
| **SlideUp** | Door slides upward |
| **SlideDown** | Door slides downward |
| **SlideLeft** | Door slides left |
| **SlideRight** | Door slides right |
| **SlideForward** | Door slides forward |
| **SlideBackward** | Door slides backward |
| **RotateLeft** | Door rotates 90° left (hinged) |
| **RotateRight** | Door rotates 90° right (hinged) |
| **Scale** | Door shrinks to nothing (force field) |

---

## Testing Commands

### KeycardItem (Right-click component in Inspector)
- **Test Collection**: Force collect keycard
- **Force Enable Collection**: Bypass cooldown

### KeycardDoor (Right-click component in Inspector)
- **Test Open Door**: Open without keycard
- **Test Close Door**: Close the door

---

## Common Issues & Fixes

| Issue | Solution |
|-------|----------|
| Keycard not collecting | Check player has "Player" tag, InventoryManager exists |
| Door not opening | Verify keycard is in inventory, check Required Keycard assigned |
| No visual effects | Enable bobbing/rotation, check Renderer exists |
| No UI showing | Assign Interaction UI and Text, check Canvas is World Space |

---

## File Locations

- **Scripts**: `Assets/scripts/KeycardItem.cs`, `Assets/scripts/KeycardDoor.cs`
- **Setup Guide**: `KEYCARD_SYSTEM_SETUP_GUIDE.md` (detailed instructions)
- **Quick Reference**: `KEYCARD_QUICK_REFERENCE.md` (this file)

---

## Integration

- **Inventory**: Uses existing InventoryManager system
- **Chests**: Integrates with ChestInteractionSystem (add to Possible Items list)
- **Audio**: Uses GameSounds.PlayGemCollection() for pickups
- **UI**: Uses CognitiveFeedManager for messages
- **Items**: Uses ChestItemData ScriptableObjects

---

## Example Color Values (RGB)

```csharp
Building21 (Orange): (1.0, 0.5, 0.0)
Green: (0.0, 1.0, 0.0)
Blue: (0.0, 0.5, 1.0)
Black: (0.1, 0.1, 0.1)
Red: (1.0, 0.0, 0.0)
```

---

## Chest Integration Steps

1. Find **ChestInteractionSystem** in scene hierarchy
2. Select it and find **Items and Generation** section
3. Expand **Possible Items** list
4. Increase **Size** by 5
5. Drag all 5 keycard ScriptableObjects into new slots
6. Test by opening chests in Play Mode

---

## Next Steps

1. Create 5 keycard ScriptableObjects in Unity
2. Place keycard pickups in your scene (optional - can rely on chests)
3. Add keycards to ChestInteractionSystem Possible Items
4. Create doors and assign required keycards
5. Test in Play Mode
6. Adjust distances, speeds, and animations as needed

**See KEYCARD_SYSTEM_SETUP_GUIDE.md for detailed step-by-step instructions!**
