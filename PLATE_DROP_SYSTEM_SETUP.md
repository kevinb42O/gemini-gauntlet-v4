# Armor Plate Drop System - Setup Guide

## Overview
Enemy companions now drop armor plates when killed. Players can pick them up by pressing **E** when in range (walking mode only, not flying).

## System Components

### 1. PlateItem.cs Script
- **Location**: `Assets/scripts/PlateItem.cs`
- **Purpose**: Handles world pickup of armor plates (similar to ScrapItem)
- **Features**:
  - Press E to collect when in range
  - Only works in walking mode (AAA movement)
  - Visual effects (bobbing, rotation)
  - Stacks with existing plates in inventory
  - Collection cooldown to prevent instant pickup

### 2. CompanionCore.cs Updates
- **Location**: `Assets/scripts/CompanionAI/CompanionCore.cs`
- **New Fields**:
  - `dropPlatesOnDeath` - Enable/disable plate drops
  - `platesToDrop` - Number of plates to drop (1-3)
  - `plateItemData` - Reference to ArmorPlate.asset
  - `plateItemPrefab` - Prefab with PlateItem script and model
  - `dropHeight` - Height offset for drop position

### 3. Existing Assets
- **Plate Model**: `Assets/prefabs_made/PLATES/armor-plate/source/plastine.fbx`
- **Plate Data**: `Assets/prefabs_made/PLATES/ArmorPlate.asset`
- **Plate Icon**: `Assets/prefabs_made/PLATES/PLATEITEM.png`

## Setup Instructions

### Step 1: Create PlateItem Prefab

1. **Create Empty GameObject**:
   - Right-click in Hierarchy → Create Empty
   - Name it: `PlateItem_Prefab`

2. **Add Visual Model**:
   - Drag `plastine.fbx` from `Assets/prefabs_made/PLATES/armor-plate/source/` into the prefab as a child
   - Scale the model appropriately (suggested: 0.5 - 1.0)
   - Position it at center (0, 0, 0)

3. **Add PlateItem Script**:
   - Select the root `PlateItem_Prefab` object
   - Add Component → PlateItem
   - Configure settings:
     - **Plate Item Data**: Drag `ArmorPlate.asset` from `Assets/prefabs_made/PLATES/`
     - **Plate Count**: 1 (default, will be overridden by CompanionCore)
     - **Collection Distance**: 3
     - **Collection Cooldown**: 0.5
     - **Enable Bobbing**: ✓
     - **Enable Rotation**: ✓

4. **Add Collider** (if not auto-added):
   - The script will auto-add a SphereCollider
   - Or manually add: Add Component → Sphere Collider
   - Set as Trigger: ✓
   - Radius: 3

5. **Optional: Add Interaction UI**:
   - Create a World Space Canvas as child
   - Add Text: "Press E to collect Armor Plate"
   - Assign to PlateItem script's `interactionUI` field

6. **Save as Prefab**:
   - Drag the configured GameObject to `Assets/prefabs_made/` folder
   - Name it: `PlateItem_Prefab`
   - Delete from scene

### Step 2: Configure Enemy Companions

1. **Select Enemy Companion Prefab** (or instance in scene):
   - Find your enemy companion GameObject
   - Select it in Hierarchy

2. **Configure CompanionCore Component**:
   - Find the "💎 LOOT DROP SYSTEM" section
   - **Drop Plates On Death**: ✓ (checked)
   - **Plates To Drop**: 1-3 (your choice)
   - **Plate Item Data**: Drag `ArmorPlate.asset` from `Assets/prefabs_made/PLATES/`
   - **Plate Item Prefab**: Drag your `PlateItem_Prefab` from prefabs folder
   - **Drop Height**: 1 (default, adjust if needed)

3. **Apply Changes**:
   - If editing a prefab instance, click "Apply" to save changes
   - If editing prefab directly, save the prefab

### Step 3: Test the System

1. **Enter Play Mode**
2. **Kill an Enemy Companion**:
   - Shoot/damage the enemy companion until it dies
   - Watch for the plate drop at its death position
3. **Collect the Plate**:
   - Walk (not fly) to the dropped plate
   - Press **E** when in range
   - Check inventory for the armor plate

## How It Works

### Drop Mechanics
1. When `CompanionCore.Die()` is called:
   - `DropLoot()` method is invoked
   - Checks if drop is enabled and prefab/data are assigned
   - Spawns `plateItemPrefab` at companion's position + `dropHeight`
   - Configures `PlateItem` component with data and count

### Pickup Mechanics
1. Player enters trigger range (3 units default)
2. UI prompt appears: "Press E to collect Armor Plate"
3. Player presses E (only works in walking mode)
4. Plate is added to inventory (stacks with existing plates)
5. Collection sound plays (armor plate apply sound)
6. Plate item is destroyed

### Movement Mode Restriction
- **Walking Mode (AAA)**: Can collect ✓
- **Flying Mode (Celestial)**: Cannot collect ✗
- Message shown: "You must be walking to collect armor plates!"

## Customization Options

### Visual Effects
- **Bobbing**: Enable/disable vertical bobbing animation
- **Rotation**: Enable/disable spinning animation
- **Bobbing Speed**: How fast the plate bobs (default: 2)
- **Bobbing Height**: How high the plate bobs (default: 0.3)
- **Rotation Speed**: How fast the plate spins (default: 90°/sec)

### Collection Settings
- **Collection Distance**: How close player must be (default: 3)
- **Collection Cooldown**: Delay before pickup is enabled (default: 0.5s)

### Drop Settings
- **Plates To Drop**: 1-3 plates per enemy
- **Drop Height**: Vertical offset from enemy position (default: 1)

## Troubleshooting

### Plates Not Dropping
- ✓ Check `dropPlatesOnDeath` is enabled on CompanionCore
- ✓ Verify `plateItemPrefab` is assigned
- ✓ Verify `plateItemData` is assigned
- ✓ Check console for error messages

### Cannot Pick Up Plates
- ✓ Ensure you're in walking mode (not flying)
- ✓ Check if inventory is full
- ✓ Verify PlateItem script is attached to prefab
- ✓ Verify collider is set as trigger

### Plates Are Invisible
- ✓ Check that model is added as child to prefab
- ✓ Verify model has MeshRenderer component
- ✓ Check model scale (may be too small)

### Plates Fall Through Ground
- ✓ Add Rigidbody to prefab (set kinematic if needed)
- ✓ Ensure ground has collider
- ✓ Adjust `dropHeight` value

## Integration with Existing Systems

### Inventory System
- Plates use `ArmorPlateItemData` (extends `ChestItemData`)
- Automatically stacks with existing plates
- Uses unified inventory slots (not gem slots)

### Armor System
- Collected plates go to inventory
- Press **C** to apply plates from inventory
- Each plate provides 1500 shield points
- Max 3 plates can be equipped (based on vest tier)

### Audio System
- Uses `GameSounds.PlayArmorPlateApply()` for pickup sound
- Same sound as applying plates manually

## Code Reference

### Key Methods

**CompanionCore.cs**:
```csharp
private void DropLoot()
{
    // Spawns plate item at death position
    // Configures PlateItem component
}
```

**PlateItem.cs**:
```csharp
public bool TryCollectPlate()
{
    // Handles E key press
    // Checks movement mode
    // Adds to inventory
    // Plays sound and destroys item
}
```

## Future Enhancements

Potential improvements:
- Random plate count drops (e.g., 1-3 plates)
- Rare chance for higher tier plates
- Different plate types (light/heavy)
- Magnetic collection (auto-pickup in range)
- Drop animation/particle effects
- Loot table system for varied drops

## Notes

- Plates drop at companion's center position + height offset
- Collection requires walking mode (prevents flying cheese)
- Plates stack in inventory (no limit on stack size)
- Drop system is modular - can be disabled per enemy
- Uses same pickup system as ScrapItem for consistency
