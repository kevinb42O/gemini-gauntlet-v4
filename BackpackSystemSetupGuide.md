# Backpack System Setup Guide - Gemini Gauntlet V2.0.0

## Overview
The Backpack System provides tier-based inventory expansion for the Gemini Gauntlet project. Players start with a Tier 1 backpack (5 slots) and can craft and equip higher tier backpacks for more inventory space.

## System Features
- **Tier-based slot expansion**: Tier 1 (5 slots), Tier 2 (7 slots), Tier 3 (10 slots)
- **Craftable backpacks**: Only Tier 2 and Tier 3 can be crafted (Tier 1 is default)
- **Bound equipment**: Once equipped, backpacks cannot be unequipped (only upgraded)
- **Death penalty**: Death resets backpack to Tier 1 and items in extra slots are lost
- **Persistent storage**: Backpack tier persists across scene transitions
- **Forge integration**: Backpacks can be crafted using existing ForgeManager system

## Files Created/Modified

### New Files Created:
1. `Assets/scripts/BackpackItem.cs` - ScriptableObject for backpack items
2. `Assets/scripts/BackpackSlotController.cs` - Manages backpack equipment slot
3. `Assets/Resources/Items/Tier1Backpack.asset` - Default Tier 1 backpack
4. `Assets/Resources/Items/Tier2Backpack.asset` - Craftable Tier 2 backpack
5. `Assets/Resources/Items/Tier3Backpack.asset` - Craftable Tier 3 backpack

### Modified Files:
1. `Assets/scripts/InventoryManager.cs` - Added backpack system integration
2. `Assets/scripts/PersistentItemInventoryManager.cs` - Added backpack persistence
3. `Assets/scripts/UnifiedSlot.cs` - Added backpack item detection

## Setup Instructions

### Step 1: Unity Inspector Setup

#### InventoryManager Setup:
1. Open your InventoryManager GameObject in the scene
2. In the Inspector, find the "Essential References" section
3. Assign the BackpackSlotController component to the "Backpack Slot" field
4. Ensure "Current Active Slots" is set to 5 (default Tier 1)

#### BackpackSlotController Setup:
1. Create a new GameObject for the backpack slot UI
2. Add the `BackpackSlotController` component
3. Create a UnifiedSlot as a child (for the visual slot)
4. Assign the UnifiedSlot to the "Backpack Slot" field
5. Assign the Tier1Backpack asset to "Default Tier1 Backpack" field
6. Optionally add a TextMeshProUGUI for backpack info display

### Step 2: UI Layout Setup

#### Positioning the Backpack Slot:
The backpack slot should be positioned **to the left** of the gem slot in your inventory UI:

```
[BackpackSlot] [GemSlot] [InventorySlot1] [InventorySlot2] ... [InventorySlot10]
```

#### Recommended UI Hierarchy:
```
InventoryPanel
├── BackpackSlot (BackpackSlotController)
│   ├── BackpackUnifiedSlot (UnifiedSlot)
│   └── BackpackInfoText (TextMeshProUGUI) [Optional]
├── GemSlot (UnifiedSlot with isGemSlot = true)
└── InventorySlotsParent
    ├── InventorySlot1 (UnifiedSlot)
    ├── InventorySlot2 (UnifiedSlot)
    └── ... (up to 10 slots)
```

### Step 3: Forge Recipe Setup

To make backpacks craftable, add recipes to your ForgeManager:

#### Example Tier 2 Backpack Recipe:
- **Required Ingredients**: ScrapMetal x3, Leather x2 (set these in inspector)
- **Output Item**: Tier2Backpack
- **Output Count**: 1

#### Example Tier 3 Backpack Recipe:
- **Required Ingredients**: Tier2Backpack x1, ScrapMetal x5, RareComponent x1
- **Output Item**: Tier3Backpack  
- **Output Count**: 1

### Step 4: Asset Configuration

#### BackpackItem Assets:
The provided backpack assets need their Script references updated:
1. Select each backpack asset (Tier1Backpack, Tier2Backpack, Tier3Backpack)
2. In Inspector, ensure the Script field points to `BackpackItem`
3. Set appropriate icons in the "Backpack Icon" field
4. Verify tier and slot count values are correct

## System Behavior

### Backpack Equipping:
1. **Drag & Drop**: Drag a backpack from inventory/stash to the backpack slot
2. **Upgrade Only**: Can only equip higher tier backpacks (no downgrades)
3. **Binding**: Once equipped, the backpack is bound and cannot be unequipped
4. **Item Loss**: Previous backpack is lost when upgrading (no refund)

### Slot Management:
1. **Dynamic Slots**: Inventory slots are shown/hidden based on equipped backpack
2. **Item Protection**: Items in active slots are safe
3. **Item Loss**: Items in slots beyond the current backpack tier are lost when downgrading

### Death Penalty:
1. **Backpack Reset**: Death resets backpack to Tier 1 (5 slots)
2. **Item Loss**: Items in slots 6-10 are lost if player had higher tier backpack
3. **Gem Protection**: Gem slot is always preserved (separate from backpack system)

### Persistence:
1. **Scene Transitions**: Backpack tier persists across scenes via PersistentItemInventoryManager
2. **Game Sessions**: Backpack data is saved to persistent storage
3. **Menu Integration**: Works seamlessly with existing menu/game scene transitions

## Debugging & Testing

### Debug Methods:
- `BackpackSlotController`: Right-click to destroy current backpack (Tier 2/3 only)
- `PersistentItemInventoryManager`: Use "Debug Current Inventory" context menu
- `InventoryManager`: Check "Current Active Slots" field in inspector

### Testing Workflow:
1. Start game with Tier 1 backpack (5 slots visible)
2. Craft Tier 2 backpack in forge
3. Drag Tier 2 backpack to backpack slot (7 slots should become visible)
4. Fill inventory with items
5. Die to test death penalty (should reset to 5 slots, items 6-7 lost)
6. Test scene transitions to verify persistence

### Common Issues:
- **Slots not updating**: Check InventoryManager.backpackSlot reference
- **Items not persisting**: Verify PersistentItemInventoryManager integration
- **UI positioning**: Ensure backpack slot is positioned left of gem slot
- **Crafting not working**: Check ForgeManager recipe configuration

## Integration Notes

### Compatibility:
- **Fully compatible** with existing inventory system
- **Preserves** gem slot functionality (always separate)
- **Maintains** self-revive slot behavior
- **Works with** existing stash system
- **Integrates** with forge crafting system

### Performance:
- **Minimal overhead**: Only updates UI when backpack changes
- **Efficient persistence**: Backpack data is lightweight (tier + slot count)
- **Smart slot management**: Hidden slots are deactivated, not destroyed

## Future Enhancements

### Potential Additions:
1. **Backpack Durability**: Backpacks could degrade over time
2. **Special Backpacks**: Backpacks with unique properties (auto-sort, etc.)
3. **Backpack Repair**: System to repair damaged backpacks
4. **Visual Feedback**: Animations when slots are added/removed
5. **Backpack Collections**: Multiple backpack types with different benefits

### Extension Points:
- `BackpackItem.cs`: Add new properties for special effects
- `BackpackSlotController.cs`: Add durability/repair methods
- `InventoryManager.cs`: Add slot animation methods
- `ForgeManager.cs`: Add backpack-specific crafting logic

## Conclusion

The Backpack System provides a robust, tier-based inventory expansion that integrates seamlessly with your existing systems. It follows the established patterns in your codebase while adding meaningful progression and risk/reward mechanics through the death penalty system.

The system is designed to be:
- **Modular**: Easy to extend with new features
- **Persistent**: Survives scene transitions and game sessions  
- **Balanced**: Provides progression while maintaining challenge
- **Compatible**: Works with all existing inventory features

For any issues or questions, check the debug methods and ensure all inspector references are properly assigned.
