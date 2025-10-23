# Scrap Collection System Setup Guide

## Overview
The scrap collection system allows players to collect scrap items from platforms by pressing "E" when walking (not flying). The system includes configurable spawn rates, amounts, and movement mode restrictions.

## Components Created

### 1. ScrapSpawn.cs
- Attach to platforms where you want scrap to spawn
- Configurable spawn chance percentage (0-100%)
- Configurable min/max scrap amounts
- Automatic spawning when player lands on platform
- Respawn control options

### 2. ScrapItem.cs
- Individual scrap items that can be collected
- E key interaction for collection
- Movement mode restriction (only works in AAA/walking mode)
- Visual effects (bobbing, rotation)
- UI interaction prompts

### 3. ScrapMetal.asset
- Example scrap item data using existing ChestItemData system
- Rarity 2 (Uncommon - Green)
- Material type for crafting integration

## Setup Instructions

### Step 1: Create Scrap Item Prefab
1. Create an empty GameObject named "ScrapItemPrefab"
2. Add a 3D model/mesh for the scrap (or use a simple cube/sphere)
3. Add the `ScrapItem.cs` component
4. Configure the ScrapItem settings:
   - Assign the ScrapMetal.asset to "Scrap Item Data"
   - Set collection distance (default: 3)
   - Configure visual effects (bobbing, rotation)
   - Set up interaction UI (optional)

### Step 2: Setup Interaction UI (Optional)
1. Create a Canvas as child of ScrapItemPrefab (World Space)
2. Add a Panel with background
3. Add Text component with "Press E to collect"
4. Assign the Canvas to "Interaction UI" field in ScrapItem
5. Assign the Text to "Interaction Text" field

### Step 3: Configure Platforms
1. Select platforms where you want scrap to spawn
2. Add the `ScrapSpawn.cs` component
3. Configure spawn settings:
   - **Spawn Chance Percentage**: 0-100% (30% recommended)
   - **Min/Max Scrap Amount**: 1-5 items recommended
   - **Scrap Item Data**: Assign ScrapMetal.asset
   - **Scrap Item Prefab**: Assign your ScrapItemPrefab
   - **Spawn Radius**: Area around platform center (5 units recommended)
   - **Spawn Height Offset**: Height above platform (1 unit recommended)

### Step 4: Layer Setup
1. Ensure platforms are on appropriate layers for ground detection
2. Configure "Ground Layer Mask" in ScrapSpawn to include platform layers
3. Test spawning positions with gizmos enabled

## Configuration Examples

### Conservative Spawning (Rare Scrap)
```
Spawn Chance: 15%
Min Amount: 1
Max Amount: 2
```

### Moderate Spawning (Balanced)
```
Spawn Chance: 30%
Min Amount: 2
Max Amount: 4
```

### Generous Spawning (Abundant Scrap)
```
Spawn Chance: 60%
Min Amount: 3
Max Amount: 6
```

## Testing Features

### ScrapSpawn Context Menu Options
- **Force Spawn Scrap**: Immediately spawn scrap regardless of chance
- **Clear All Scrap**: Remove all spawned scrap items

### ScrapItem Context Menu Options
- **Test Collection**: Test collection in play mode
- **Force Enable Collection**: Skip cooldown timer

## Movement Mode Restrictions

The system automatically detects player movement mode:
- ✅ **AAA Movement (Walking)**: Collection allowed
- ❌ **Celestial Flight**: Collection blocked with message

Players must land on platforms and be in walking mode to collect scrap.

## Integration Points

### Inventory System
- Uses existing `InventoryManager.TryAddItem()` method
- Compatible with current item stacking system
- Proper inventory full handling

### Audio System
- Plays collection sounds through AudioManager
- Configurable sound names per scrap type

### UI Feedback
- Uses CognitiveFeedManager for user messages
- Movement mode restriction notifications
- Inventory full notifications

## Debug Features

### Visual Gizmos
- Yellow wireframe: Spawn radius
- Green sphere: Spawn height
- Red circles: Minimum distance between items
- Cyan wireframe: Collection range (ScrapItem)
- Green/Red sphere: Movement mode status

### Console Logging
- Spawn chance rolls and results
- Collection attempts and restrictions
- Movement mode detection
- Item spawning and cleanup

## Customization Options

### Creating New Scrap Types
1. Create new ChestItemData asset in Resources/Items/
2. Configure name, description, rarity, crafting properties
3. Assign to ScrapSpawn components
4. Create corresponding prefabs with different visuals

### Platform-Specific Configuration
- Different platforms can have different spawn rates
- Rare platforms can have higher-value scrap
- Boss platforms can have guaranteed spawns

### Visual Customization
- Adjust bobbing speed/height for different scrap types
- Different rotation speeds for variety
- Custom materials and particle effects

## Troubleshooting

### Scrap Not Spawning
1. Check spawn chance percentage
2. Verify ScrapItemData and prefab assignments
3. Check ground layer mask configuration
4. Use "Force Spawn Scrap" context menu for testing

### Collection Not Working
1. Ensure player is in AAA movement mode (walking)
2. Check collection distance
3. Verify ScrapItem has proper collider setup
4. Check inventory space availability

### Movement Mode Detection Issues
1. Verify AAAMovementIntegrator is on player
2. Check PlayerMovementManager component
3. Enable movement debug logging in ScrapItem

## Performance Considerations

- Scrap items clean up automatically when collected
- Parent spawners track active items to prevent over-spawning
- Gizmos only show in editor/selected objects
- Efficient trigger-based player detection

## Future Enhancements

Potential additions to the system:
- Magnetic collection (scrap flies to player)
- Rarity-based visual effects
- Collection streaks and bonuses
- Platform-specific scrap types
- Scrap degradation over time
- Collection statistics tracking
