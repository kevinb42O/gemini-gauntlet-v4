# Keycard System Setup Guide

## Overview
This guide explains how to set up the keycard system with 5 different keycards (Building21, Green, Blue, Black, and Red) and doors that require them.

## System Components

### Scripts Created
1. **KeycardItem.cs** - Handles world pickups for keycards
2. **KeycardDoor.cs** - Handles doors that require specific keycards

### Features
- **5 Unique Keycards**: Building21, Green, Blue, Black, and Red
- **Interactive Doors**: Require specific keycards to open
- **Keycard Consumption**: Keycards are removed from inventory when used
- **Visual Feedback**: Doors show locked/unlocked states
- **Audio Feedback**: Sounds for locked, open, and close states
- **UI Prompts**: Shows interaction prompts when player is near

---

## Part 1: Creating Keycard Items (ScriptableObjects)

### Step 1: Create the 5 Keycard ScriptableObjects

1. In Unity, navigate to your **Assets** folder
2. Right-click in the Project window
3. Select **Create > Inventory > Item**
4. Create 5 items with the following names:
   - `Building21_Keycard`
   - `Green_Keycard`
   - `Blue_Keycard`
   - `Black_Keycard`
   - `Red_Keycard`

### Step 2: Configure Each Keycard

For each keycard, set the following properties in the Inspector:

#### Building21 Keycard
```
Item Name: Building21 Keycard
Description: A master security keycard that grants access to Building 21. This keycard has infinite uses and will not be consumed when used.
Item Type: Keycard
Item Rarity: 3 (Rare)
Crafting Category: Key Items
Rarity Color: Orange (1.0, 0.5, 0.0, 1.0)

SPECIAL: This keycard is NOT consumed when used - infinite uses!
```

#### Green Keycard
```
Item Name: Green Keycard
Description: A green security keycard. Opens doors marked with green access panels.
Item Type: Keycard
Item Rarity: 2 (Uncommon)
Crafting Category: Key Items
Rarity Color: Green (0.0, 1.0, 0.0, 1.0)
```

#### Blue Keycard
```
Item Name: Blue Keycard
Description: A blue security keycard. Opens doors marked with blue access panels.
Item Type: Keycard
Item Rarity: 3 (Rare)
Crafting Category: Key Items
Rarity Color: Blue (0.0, 0.5, 1.0, 1.0)
```

#### Black Keycard
```
Item Name: Black Keycard
Description: A high-security black keycard. Opens the most restricted areas.
Item Type: Keycard
Item Rarity: 5 (Legendary)
Crafting Category: Key Items
Rarity Color: Black (0.1, 0.1, 0.1, 1.0)
```

#### Red Keycard
```
Item Name: Red Keycard
Description: A red security keycard. Opens emergency access doors.
Item Type: Keycard
Item Rarity: 4 (Epic)
Crafting Category: Key Items
Rarity Color: Red (1.0, 0.0, 0.0, 1.0)
```

### Step 3: Add Icons (Optional)
- For each keycard, assign an appropriate icon sprite to the **Item Icon** field
- You can create simple colored rectangles in an image editor or use existing sprites

---

## Part 2: Creating Keycard World Pickups

### Step 1: Create a Keycard Pickup GameObject

1. In your scene, create an **Empty GameObject**
2. Rename it to match the keycard (e.g., "Building21_Keycard_Pickup")
3. Add the **KeycardItem.cs** script to it

### Step 2: Configure the KeycardItem Component

In the Inspector, set the following:

```
Keycard Data: [Drag the appropriate keycard ScriptableObject here]
Collection Distance: 2.5
Auto Collect: true
Collection Cooldown: 0.5

Visual Effects:
  Enable Bobbing: true
  Bobbing Speed: 2
  Bobbing Height: 0.3
  Enable Rotation: true
  Rotation Speed: 60

Audio:
  Play Collection Sound: true
```

### Step 3: Add Visual Representation

1. Add a **3D model** or **primitive shape** as a child of the keycard pickup
   - For quick setup, use a **Cube** or **Capsule**
2. Scale it appropriately (e.g., 0.2 x 0.5 x 0.1 for a card shape)
3. Add a **Material** with the appropriate color:
   - Building21: Orange
   - Green: Green
   - Blue: Blue
   - Black: Black/Dark Gray
   - Red: Red

### Step 4: Add Interaction UI (Optional)

1. Create a **Canvas** as a child of the keycard pickup
2. Set Canvas **Render Mode** to **World Space**
3. Add a **Text** or **TextMeshPro** component
4. Position it above the keycard
5. In the KeycardItem component:
   - Assign the Canvas to **Interaction UI**
   - Assign the Text to **Interaction Text**

### Step 5: Duplicate for All Keycards

1. Duplicate the keycard pickup GameObject 4 times
2. Rename each one appropriately
3. Update the **Keycard Data** field for each one
4. Update the visual representation colors
5. Place them in your scene where you want players to find them

---

## Part 3: Creating Keycard Doors

### Step 1: Create a Door GameObject

1. In your scene, create a **3D Object > Cube** (or use your own door model)
2. Rename it to match the keycard requirement (e.g., "Building21_Door")
3. Scale it to door size (e.g., 2 x 3 x 0.2)
4. Position it where you want the door

### Step 2: Add the KeycardDoor Component

1. Add the **KeycardDoor.cs** script to the door
2. The script will automatically add a **Collider** if one doesn't exist

### Step 3: Configure the KeycardDoor Component

In the Inspector, set the following:

```
Keycard Requirement:
  Required Keycard: [Drag the appropriate keycard ScriptableObject here]

Door Settings:
  Open Type: SlideUp (or choose another animation type)
  Open Distance: 3 (how far the door moves)
  Open Speed: 2
  Auto Close: false (set to true if you want it to close automatically)
  Auto Close Delay: 5 (if auto close is enabled)

Interaction Settings:
  Interaction Distance: 3

Visual Feedback:
  Locked Color: Red (1.0, 0.0, 0.0, 1.0)
  Unlocked Color: Green (0.0, 1.0, 0.0, 1.0)
```

### Step 4: Configure Door Animation Type

Choose from the following **Open Type** options:
- **SlideUp**: Door slides upward (good for garage-style doors)
- **SlideDown**: Door slides downward
- **SlideLeft**: Door slides to the left
- **SlideRight**: Door slides to the right
- **SlideForward**: Door slides forward
- **SlideBackward**: Door slides backward
- **RotateLeft**: Door rotates 90° to the left (good for hinged doors)
- **RotateRight**: Door rotates 90° to the right
- **Scale**: Door shrinks to nothing (good for force fields)

### Step 5: Add Audio (Optional)

1. Add **AudioClip** assets to your project for:
   - Locked sound (e.g., buzzer or error sound)
   - Open sound (e.g., mechanical door opening)
   - Close sound (e.g., mechanical door closing)
2. Assign them to the KeycardDoor component:
   - **Locked Sound**
   - **Open Sound**
   - **Close Sound**

### Step 6: Add Interaction UI (Optional)

1. Create a **Canvas** as a child of the door
2. Set Canvas **Render Mode** to **World Space**
3. Add a **Text** or **TextMeshPro** component
4. Position it near the door
5. In the KeycardDoor component:
   - Assign the Canvas to **Interaction UI**
   - Assign the Text to **Interaction Text**

### Step 7: Duplicate for All Doors

1. Duplicate the door GameObject 4 times
2. Rename each one appropriately
3. Update the **Required Keycard** field for each one
4. Update the visual representation colors
5. Position them in your scene

---

## Part 4: Adding Keycards to Chest Loot

### Step 1: Find the ChestInteractionSystem

1. In your scene hierarchy, find the GameObject with the **ChestInteractionSystem** component
   - This is usually on a GameObject named "ChestInteractionSystem" or "ChestManager"
   - If you can't find it, search for it in the hierarchy (Ctrl+F / Cmd+F)

### Step 2: Add Keycards to Possible Items

1. Select the GameObject with **ChestInteractionSystem**
2. In the Inspector, find the **Items and Generation** section
3. Expand the **Possible Items** list
4. Increase the **Size** by 5 (to add 5 new slots)
5. Drag each keycard ScriptableObject into the new slots:
   - Building21_Keycard
   - Green_Keycard
   - Blue_Keycard
   - Black_Keycard
   - Red_Keycard

### Step 3: Configure Keycard Spawn Rates (Optional)

By default, keycards will have the same spawn chance as other items. If you want to make them rarer:

**Option 1: Adjust Rarity Values**
- Open each keycard ScriptableObject
- Increase the **Item Rarity** value (1-5)
- Higher rarity = less common in chests

**Option 2: Add Fewer Keycard Entries**
- Only add 1-2 keycards to the Possible Items list
- This makes them spawn less frequently

**Option 3: Create a Separate Keycard Spawn System**
- Similar to the Self-Revive system in ChestInteractionSystem
- Add a spawn chance percentage for keycards
- This requires modifying the `GenerateChestItems()` method

### Step 4: Test Chest Spawning

1. Enter Play Mode
2. Find and open a chest
3. Check if keycards appear in the chest loot
4. Collect a keycard from the chest
5. Verify it appears in your inventory (press Tab)
6. Test using it on a door

### How Chest Loot Works

- Each chest generates **3-6 random items** from the Possible Items list
- Items are selected randomly each time a chest is opened **for the first time**
- Once a chest's loot is generated, it **persists** (won't change if you close and reopen)
- Keycards will appear alongside other loot items
- Self-revive items have a separate spawn chance (15% by default)

---

## Part 5: Testing the System

### Test Keycard Pickup

1. Enter Play Mode
2. Walk up to a keycard pickup
3. You should see the interaction UI: "Press E to collect [Keycard Name]"
4. Press **E** to collect the keycard
5. Open your inventory (press **Tab**) to verify the keycard was added

### Test Door Interaction

1. Walk up to a door **without** the required keycard
2. You should see: "Requires [Keycard Name]"
3. Press **E** - you should see a message saying the keycard is required
4. Collect the matching keycard
5. Walk back to the door
6. You should now see: "Press E to open with [Keycard Name]"
7. Press **E** - the door should open and the keycard should be removed from inventory

### Debug Testing

Both scripts have **Context Menu** options for testing:

#### KeycardItem
- Right-click the component in Inspector
- Select **Test Collection** to force collection
- Select **Force Enable Collection** to bypass cooldown

#### KeycardDoor
- Right-click the component in Inspector
- Select **Test Open Door** to open without keycard
- Select **Test Close Door** to close the door

---

## Part 6: Advanced Customization

### Custom Door Models

1. Replace the Cube with your own door model
2. Ensure the model has a **Renderer** component for visual feedback
3. Adjust the **Open Distance** and **Open Type** to match your model

### Custom Keycard Models

1. Replace the primitive shape with your own keycard model
2. Ensure the model is a child of the KeycardItem GameObject
3. The bobbing and rotation will apply to the parent GameObject

### Multiple Keycards for One Door

If you want a door to require multiple keycards:
1. Modify the **KeycardDoor.cs** script
2. Change `requiredKeycard` to `requiredKeycards` (array)
3. Update the `HasRequiredKeycard()` method to check for all keycards
4. Update the `TryOpenDoor()` method to remove all keycards

### Reusable Keycards

**Building21 Keycard is already infinite use!** It's never consumed when opening doors.

If you want to make OTHER keycards reusable:
1. Open **KeycardDoor.cs**
2. Find the `TryOpenDoor()` method
3. Add your keycard name to the check:
   ```csharp
   bool isBuilding21Keycard = requiredKeycard.itemName.Contains("Building21") || 
                              requiredKeycard.itemName.Contains("YourKeycardName");
   ```
4. That keycard will now also be infinite use

---

## Part 7: Integration with Existing Systems

### Inventory System
- Keycards use the existing **InventoryManager** system
- They appear in the player's inventory like any other item
- They can be stacked if you collect multiple of the same type

### Audio System
- Keycards use the existing **GameSounds.PlayGemCollection()** for pickup sounds
- Doors use **AudioSource** components for door sounds

### UI System
- Uses the existing **CognitiveFeedManager** for on-screen messages
- Shows collection confirmations and door interaction messages

---

## Part 8: Troubleshooting

### Keycard Not Collecting
- Ensure the player has the **"Player"** tag
- Check that **InventoryManager.Instance** exists in the scene
- Verify the keycard has a **Collider** with **Is Trigger** enabled
- Check the **Collection Distance** is large enough

### Door Not Opening
- Ensure the **Required Keycard** is assigned in the Inspector
- Verify the player has the keycard in their inventory
- Check that the door has a **Collider** with **Is Trigger** enabled
- Ensure **InventoryManager.Instance** exists in the scene

### Visual Effects Not Working
- For keycards: Ensure **Enable Bobbing** and **Enable Rotation** are checked
- For doors: Ensure the door has a **Renderer** component
- Check that materials/colors are assigned

### UI Not Showing
- Ensure the **Interaction UI** and **Interaction Text** are assigned
- Check that the Canvas is set to **World Space** render mode
- Verify the UI is positioned correctly (not inside the object)

---

## Part 9: Quick Setup Checklist

### For Each Keycard:
- [ ] Create ChestItemData ScriptableObject
- [ ] Configure name, description, rarity, and color
- [ ] Create GameObject with KeycardItem script
- [ ] Assign keycard data to script
- [ ] Add visual representation (model or primitive)
- [ ] Add collider (auto-added by script)
- [ ] Test collection in Play Mode

### For Each Door:
- [ ] Create door GameObject (model or primitive)
- [ ] Add KeycardDoor script
- [ ] Assign required keycard
- [ ] Configure open type and distance
- [ ] Add collider (auto-added by script)
- [ ] Test interaction in Play Mode

### For Chest Integration:
- [ ] Find ChestInteractionSystem in scene
- [ ] Add all 5 keycards to Possible Items list
- [ ] Test opening chests to verify keycards spawn
- [ ] Verify keycards can be collected from chests
- [ ] Test using chest-collected keycards on doors

---

## Summary

You now have a complete keycard system with:
- **5 unique keycards** (Building21, Green, Blue, Black, Red)
- **World pickups** that can be collected by the player
- **Chest loot integration** - keycards spawn in chests
- **Interactive doors** that require specific keycards
- **Keycard consumption** when doors are opened
- **Visual and audio feedback**
- **UI prompts** for player guidance

The system integrates seamlessly with your existing inventory, chest, audio, and UI systems!

---

## Example Scene Setup

Here's a suggested layout for testing:

```
Scene Hierarchy:
├── Player
├── InventoryManager
├── Keycards
│   ├── Building21_Keycard_Pickup (position: 0, 1, 5)
│   ├── Green_Keycard_Pickup (position: 5, 1, 5)
│   ├── Blue_Keycard_Pickup (position: 10, 1, 5)
│   ├── Black_Keycard_Pickup (position: 15, 1, 5)
│   └── Red_Keycard_Pickup (position: 20, 1, 5)
└── Doors
    ├── Building21_Door (position: 0, 1.5, 10)
    ├── Green_Door (position: 5, 1.5, 10)
    ├── Blue_Door (position: 10, 1.5, 10)
    ├── Black_Door (position: 15, 1.5, 10)
    └── Red_Door (position: 20, 1.5, 10)
```

This creates a test area where each keycard is in front of its corresponding door!
