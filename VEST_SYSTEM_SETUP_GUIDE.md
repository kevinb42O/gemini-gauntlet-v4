# Vest System Setup Guide - Gemini Gauntlet V3.0

## Overview
The Vest System provides tier-based armor plate capacity expansion for the Gemini Gauntlet project. Players start with a T1 vest (1 plate) and can find/craft higher tier vests for more plate capacity. The shield UI dynamically adjusts to show the correct number of segments based on the equipped vest.

## System Features
- **Tier-based plate capacity**: T1 (1 plate), T2 (2 plates), T3 (3 plates)
- **Dynamic shield UI**: Shield slider segments automatically adjust based on vest tier
- **Percentage-based filling**: Each plate fills the appropriate percentage of the bar (100% for T1, 50% for T2, 33.33% for T3)
- **Bound equipment**: Once equipped, vests cannot be unequipped (only upgraded)
- **Death penalty**: Death resets vest to T1 and removes all equipped plates
- **Persistent storage**: Vest tier persists across scene transitions and game sessions
- **Stash protection**: Vests in menu stash are NOT lost on death (only equipped/inventory vests)

## Files Created/Modified

### New Files Created:
1. `Assets/scripts/VestItem.cs` - ScriptableObject for vest items
2. `Assets/scripts/VestSlotController.cs` - Manages vest equipment slot
3. `VEST_SYSTEM_SETUP_GUIDE.md` - This setup guide

### Modified Files:
1. `Assets/scripts/ArmorPlateSystem.cs` - Added dynamic max plates based on vest
2. `Assets/scripts/ShieldSliderSegments.cs` - Added dynamic segment count updates
3. `Assets/scripts/HealthEnergyUI.cs` - Already supports dynamic shield updates
4. `Assets/scripts/UnifiedSlot.cs` - Added vest item detection
5. `Assets/scripts/InventoryManager.cs` - Added vest slot integration
6. `Assets/scripts/PersistentItemInventoryManager.cs` - Added vest persistence

## Setup Instructions

### Step 1: Create Vest Item Assets

#### Create T1 Vest (Default):
1. In Unity, right-click in `Assets/Resources/Items/` folder
2. Select `Create > Inventory > Vest Item`
3. Name it `T1_Vest`
4. Configure in Inspector:
   - **Item Name**: "T1 Vest"
   - **Item Type**: "Vest" (auto-set)
   - **Vest Tier**: 1
   - **Max Plates**: 1
   - **Vest Display Name**: "T1 Vest"
   - **Vest Description**: "Basic tactical vest. Holds 1 armor plate."
   - **Item Icon**: Assign your T1 vest icon sprite
   - **Is Stackable**: false
   - **Max Stack Size**: 1

#### Create T2 Vest:
1. Duplicate T1_Vest or create new Vest Item
2. Name it `T2_Vest`
3. Configure in Inspector:
   - **Item Name**: "T2 Vest"
   - **Vest Tier**: 2
   - **Max Plates**: 2
   - **Vest Display Name**: "T2 Vest"
   - **Vest Description**: "Advanced tactical vest. Holds 2 armor plates."
   - **Item Icon**: Assign your T2 vest icon sprite

#### Create T3 Vest:
1. Duplicate T2_Vest or create new Vest Item
2. Name it `T3_Vest`
3. Configure in Inspector:
   - **Item Name**: "T3 Vest"
   - **Vest Tier**: 3
   - **Max Plates**: 3
   - **Vest Display Name**: "T3 Vest"
   - **Vest Description**: "Tactical assault vest. Holds 3 armor plates."
   - **Item Icon**: Assign your T3 vest icon sprite

### Step 2: Unity Inspector Setup

#### Create Vest Slot UI:
1. Open your inventory UI scene/prefab
2. Create a new GameObject for the vest slot (sibling to backpack slot)
3. Name it `VestSlot`
4. Add `VestSlotController` component
5. Create a child `UnifiedSlot` GameObject:
   - Add `UnifiedSlot` component
   - Add `Image` component for slot background
   - Add child `Image` for item icon
   - Add child `TextMeshProUGUI` for count text (optional)
6. Configure VestSlotController:
   - **Vest Slot**: Assign the child UnifiedSlot
   - **Default T1 Vest**: Assign the T1_Vest asset you created
   - **Vest Info Text**: (Optional) Assign a TextMeshProUGUI for vest info display

#### InventoryManager Setup:
1. Open your InventoryManager GameObject in the scene
2. In the Inspector, find the "Essential References" section
3. Assign the VestSlotController component to the "Vest Slot" field

#### ArmorPlateSystem Setup:
No changes needed - the system will automatically detect and use the vest system.

#### ShieldSliderSegments Setup:
1. Find your Shield Slider GameObject in the scene
2. The `ShieldSliderSegments` component should already be attached
3. It will automatically start with 1 segment (T1 vest default)
4. Segments will update dynamically when vest changes

### Step 3: UI Layout Setup

#### Recommended Inventory UI Hierarchy:
```
InventoryPanel
├── VestSlot (VestSlotController)
│   ├── VestUnifiedSlot (UnifiedSlot)
│   │   ├── Background (Image)
│   │   ├── ItemIcon (Image)
│   │   └── CountText (TextMeshProUGUI) [Optional]
│   └── VestInfoText (TextMeshProUGUI) [Optional]
├── BackpackSlot (BackpackSlotController)
│   └── ... (existing backpack setup)
├── GemSlot (UnifiedSlot with isGemSlot = true)
└── InventorySlotsParent
    ├── InventorySlot1 (UnifiedSlot)
    └── ... (up to 10 slots)
```

#### Positioning Recommendation:
```
[VestSlot] [BackpackSlot] [GemSlot] [InventorySlot1] [InventorySlot2] ... [InventorySlot10]
```

### Step 4: Shield UI Setup

#### Shield Slider Configuration:
Your existing shield slider should work automatically, but verify:
1. Find your Shield Slider GameObject
2. Ensure `ShieldSliderSegments` component is attached
3. The component will:
   - Start with 1 segment (T1 vest)
   - Automatically update to 2 segments when T2 vest equipped
   - Automatically update to 3 segments when T3 vest equipped

#### Visual Dividers:
The system automatically creates divider lines between segments:
- **T1 Vest**: No dividers (single segment)
- **T2 Vest**: 1 divider at 50% mark
- **T3 Vest**: 2 dividers at 33.33% and 66.66% marks

### Step 5: Loot/Spawn Integration

#### Add Vests to Loot Tables:
1. Open your chest/loot table configurations
2. Add T2_Vest and T3_Vest as possible loot drops
3. Set appropriate rarity/spawn rates

#### Add Vests to Forge Recipes (Optional):
If you want vests to be craftable:
1. Open ForgeManager
2. Add recipes for T2_Vest and T3_Vest
3. Example T2 recipe: ScrapMetal x5 + Fabric x3 → T2_Vest
4. Example T3 recipe: T2_Vest x1 + ScrapMetal x10 + RareComponent x2 → T3_Vest

## System Behavior

### Vest Equipping:
1. **Drag & Drop**: Drag a vest from inventory/stash to the vest slot
2. **Upgrade Only**: Can only equip higher tier vests (no downgrades)
3. **Binding**: Once equipped, the vest is bound and cannot be unequipped
4. **Item Loss**: Previous vest is lost when upgrading (no refund)
5. **Plate Adjustment**: If downgrading (death), excess plates are removed

### Plate Capacity:
1. **Dynamic Capacity**: Max plates = vest tier (T1=1, T2=2, T3=3)
2. **Automatic Adjustment**: Plate capacity updates immediately when vest changes
3. **Excess Removal**: If vest downgrades, excess plates are removed
4. **UI Updates**: Shield slider segments update automatically

### Shield UI Behavior:
1. **T1 Vest (1 plate)**: 
   - Shield bar fills 100% when 1 plate equipped
   - No divider lines
2. **T2 Vest (2 plates)**:
   - Each plate fills 50% of the bar
   - 1 divider line at 50% mark
3. **T3 Vest (3 plates)**:
   - Each plate fills 33.33% of the bar
   - 2 divider lines at 33.33% and 66.66% marks

### Death Penalty:
1. **Vest Reset**: Death resets vest to T1 (1 plate capacity)
2. **Plate Loss**: All equipped plates are removed
3. **Inventory Loss**: Vests in player inventory are lost
4. **Stash Protection**: Vests in menu stash are NOT lost

### Persistence:
1. **Scene Transitions**: Vest tier persists across scenes via PersistentItemInventoryManager
2. **Game Sessions**: Vest data is saved to persistent storage
3. **Focus Loss**: Vest data is saved on application pause/focus loss
4. **Menu Integration**: Works seamlessly with existing menu/game scene transitions

## Testing Workflow

### Basic Testing:
1. Start game - should have T1 vest equipped (1 plate capacity)
2. Press C to apply plates - should only allow 1 plate
3. Find/spawn T2 vest
4. Drag T2 vest to vest slot - should upgrade to 2 plate capacity
5. Press C to apply plates - should now allow 2 plates
6. Verify shield UI shows 2 segments with divider at 50%
7. Die - vest should reset to T1, plates removed
8. Test scene transitions - vest should persist

### Advanced Testing:
1. Test vest persistence across game sessions (quit and reload)
2. Test vest in stash survives death (only equipped vest is lost)
3. Test plate application with different vest tiers
4. Test shield UI updates correctly with partial plate damage
5. Test vest upgrade/downgrade with plates already equipped

## Debugging

### Debug Methods:
- **VestSlotController**: Right-click to destroy current vest (T2/T3 only)
- **PersistentItemInventoryManager**: Use "Debug Current Inventory" context menu to see vest data
- **ArmorPlateSystem**: Check console logs for plate capacity updates

### Common Issues:

#### Vest Not Equipping:
- Check that vest asset is in `Resources/Items/` folder
- Verify VestSlotController has Default T1 Vest assigned
- Check console for error messages

#### Shield UI Not Updating:
- Verify ShieldSliderSegments component is on Shield Slider GameObject
- Check that ArmorPlateSystem is calling UpdateSegmentCount()
- Look for console logs showing segment count updates

#### Plates Not Applying:
- Check that ArmorPlateSystem.dynamicMaxPlates is set correctly
- Verify vest is actually equipped (check VestSlotController)
- Check console logs for plate application attempts

#### Vest Not Persisting:
- Verify PersistentItemInventoryManager exists in scene
- Check that InventoryManager.vestSlot reference is assigned
- Use "Debug Current Inventory" to verify vest data is saved

### Console Log Indicators:
- `[VestSlotController] Equipped T2 Vest - Armor capacity now 2 plates` - Vest equipped successfully
- `[ArmorPlateSystem] Max plates updated: 1 -> 2` - Plate capacity updated
- `[ShieldSliderSegments] Updating segment count: 1 -> 2` - UI segments updated
- `[PersistentItemInventoryManager] Saved vest data: Tier 2, 2 plates` - Vest persisted

## Integration Notes

### Compatibility:
- **Fully compatible** with existing armor plate system
- **Preserves** all existing plate functionality
- **Works with** existing inventory and stash systems
- **Integrates** with persistence system
- **Compatible** with backpack system (both use similar patterns)

### Performance:
- **Minimal overhead**: Only updates UI when vest changes
- **Efficient persistence**: Vest data is lightweight (tier + max plates)
- **Smart UI updates**: Segments recreated only when necessary

## Code Architecture

### Key Classes:
1. **VestItem**: ScriptableObject defining vest properties
2. **VestSlotController**: Manages vest equipment and UI
3. **ArmorPlateSystem**: Updated to use dynamic max plates
4. **ShieldSliderSegments**: Updated to support dynamic segment count
5. **PersistentItemInventoryManager**: Handles vest persistence

### Event Flow:
```
Player equips vest
    ↓
VestSlotController.EquipVest()
    ↓
ArmorPlateSystem.UpdateMaxPlates()
    ↓
ShieldSliderSegments.UpdateSegmentCount()
    ↓
UI updates with new segment count
```

### Save/Load Flow:
```
Save:
InventoryManager → PersistentItemInventoryManager → JSON file

Load:
JSON file → PersistentItemInventoryManager → VestSlotController → ArmorPlateSystem
```

## Future Enhancements

### Potential Additions:
1. **Vest Durability**: Vests could degrade over time or when taking damage
2. **Special Vests**: Vests with unique properties (faster plate application, damage reduction, etc.)
3. **Vest Repair**: System to repair damaged vests
4. **Visual Feedback**: Animations when vest is equipped/upgraded
5. **Vest Collections**: Multiple vest types per tier with different benefits

### Extension Points:
- `VestItem.cs`: Add new properties for special effects
- `VestSlotController.cs`: Add durability/repair methods
- `ArmorPlateSystem.cs`: Add vest-specific plate bonuses
- `ShieldSliderSegments.cs`: Add custom segment visuals per vest tier

## Summary

The Vest System provides a robust, tier-based armor plate capacity system that integrates seamlessly with your existing armor plate and inventory systems. It follows the established patterns in your codebase (similar to BackpackSystem) while adding meaningful progression and risk/reward mechanics through the death penalty system.

The system is designed to be:
- **Modular**: Easy to extend with new features
- **Persistent**: Survives scene transitions and game sessions
- **Balanced**: Provides progression while maintaining challenge
- **Compatible**: Works with all existing inventory and armor features
- **User-Friendly**: Automatic UI updates with no manual configuration needed

## Quick Reference

### Default Values:
- **T1 Vest**: 1 plate capacity (default, always equipped)
- **T2 Vest**: 2 plate capacity (upgrade)
- **T3 Vest**: 3 plate capacity (max upgrade)

### Key Bindings:
- **C**: Apply plates from inventory (respects vest capacity)
- **TAB**: Toggle inventory (to access vest slot)
- **Drag & Drop**: Equip vest to vest slot

### Important Notes:
1. Players ALWAYS have a vest equipped (minimum T1)
2. Vests CANNOT be unequipped, only upgraded
3. Death resets vest to T1 and removes all plates
4. Vests in stash are protected from death penalty
5. Shield UI automatically adjusts to vest tier

For any issues or questions, check the debug methods and ensure all inspector references are properly assigned.
