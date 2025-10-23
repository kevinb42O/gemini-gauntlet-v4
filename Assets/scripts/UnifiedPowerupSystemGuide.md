# Unified Powerup System Setup Guide

This guide explains how to set up the unified powerup icon and display system that eliminates duplication between PowerupDisplay and PowerupInventoryManager.

## System Overview

The unified system uses a single `PowerupIconManager` ScriptableObject to manage all powerup icons, ensuring consistency across both the individual PowerupDisplay slots and the PowerupInventoryManager's 6-slot inventory system.

## Components

### 1. PowerupIconManager (ScriptableObject)
- **Location**: `Assets/scripts/PowerupIconManager.cs`
- **Purpose**: Centralized icon management for all powerup types
- **Features**: 
  - Stores all powerup icons in one place
  - Provides static and instance methods for icon retrieval
  - Eliminates icon duplication across systems

### 2. PowerupDisplay (Updated)
- **Location**: `Assets/scripts/PowerupDisplay.cs`
- **Changes**: 
  - Removed individual icon fields (maxHandUpgradeIcon, homingDaggersIcon, etc.)
  - Added PowerupIconManager reference field
  - Updated GetIconForPowerupType() to use centralized system

### 3. PowerupInventoryManager (Updated)
- **Location**: `Assets/scripts/PowerupInventoryManager.cs`
- **Features**:
  - Uses PowerupIconManager for all icon assignments
  - Handles 6-slot inventory display
  - Manages powerup activation and selection

## Setup Instructions

### Step 1: Create PowerupIconManager Asset
1. In Unity, right-click in your Assets folder
2. Go to `Create > Game > Powerup Icon Manager`
3. Name it "PowerupIconManager"
4. Assign all powerup icons to their respective fields:
   - Max Hand Upgrade Icon
   - Homing Daggers Icon
   - AOE Attack Icon
   - Double Gems Icon
   - Slow Time Icon
   - God Mode Icon

### Step 2: Configure PowerupInventoryManager
1. Select your PowerupInventoryManager GameObject in the scene
2. In the Inspector, find the "Icon Manager" field
3. Drag your PowerupIconManager asset into this field
4. Configure your 6 inventory slots:
   - Slot Object (the main GameObject for each slot)
   - Background Image (for selection highlighting)
   - Icon Image (where powerup icons will appear)
   - Info Text (for displaying powerup names/durations)
   - Selection Border (optional visual indicator)
   - Bonus Gems Text (for Double Gems powerup bonus display)

### Step 3: Configure PowerupDisplay Slots
1. For each PowerupDisplay component in your scene:
2. In the Inspector, find the "Icon Manager Reference" field
3. Drag your PowerupIconManager asset into this field
4. Remove any old individual icon assignments (they're no longer needed)

### Step 4: Verify Integration
1. Test powerup collection - icons should appear correctly in both systems
2. Test inventory navigation - scroll wheel should work properly
3. Test powerup activation - middle-click should activate selected powerups
4. Verify visual feedback - selection highlighting and scaling should work

## Key Benefits

### Eliminated Duplication
- **Before**: Icons assigned in both PowerupDisplay AND PowerupInventoryManager
- **After**: Icons assigned once in PowerupIconManager, used by both systems

### Centralized Management
- All icon changes happen in one place
- Consistent icons across all UI systems
- Easy to swap out icon sets

### Maintainability
- Single source of truth for powerup icons
- Reduced setup complexity
- Less chance for inconsistencies

## System Flow

1. **Powerup Collection**: Powerup scripts call `PowerupInventoryManager.AddPowerup()`
2. **Icon Assignment**: PowerupInventoryManager gets icon from PowerupIconManager
3. **Display Update**: Both inventory slots and individual displays use same icon source
4. **Activation**: Middle-click activates selected powerup from inventory
5. **Visual Feedback**: Selection highlighting and scaling provide clear user feedback

## Troubleshooting

### Icons Not Appearing
- Ensure PowerupIconManager asset is assigned to both systems
- Check that all icon sprites are assigned in the PowerupIconManager
- Verify that Icon Image components are properly referenced in slot configurations

### Selection Not Visible
- Check selectedSlotColor and normalSlotColor settings
- Ensure Background Image components are assigned and active
- Verify selection border components are properly configured

### Powerups Not Activating
- Confirm middle mouse button input is working
- Check that powerup activation methods are properly implemented
- Verify PowerupInventoryManager is receiving input events

## Migration Notes

If upgrading from the old dual-system approach:
1. Remove old icon assignments from PowerupDisplay components
2. Create and configure PowerupIconManager asset
3. Assign PowerupIconManager to both PowerupDisplay and PowerupInventoryManager
4. Test all powerup collection and activation workflows

This unified system provides a clean, maintainable approach to powerup display that eliminates redundancy while maintaining all existing functionality.
