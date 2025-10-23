# Powerup Inventory System Setup Guide

## Overview
This guide will help you set up the new 6-slot powerup inventory system with scroll wheel navigation and middle-click activation.

## Required Components

### 1. PowerupInventoryManager Setup
1. Create an empty GameObject in your scene and name it "PowerupInventoryManager"
2. Add the `PowerupInventoryManager` script to it
3. Configure the 6 inventory slots in the inspector:

#### For each slot (0-5), you need:
- **Slot Object**: The main GameObject containing the slot UI
- **Background Image**: The background image component
- **Icon Image**: The image component for the powerup icon
- **Info Text**: TextMeshPro component for displaying charges/duration
- **Selection Border**: Optional image component for selection highlight

### 2. PowerupIconManager Setup
1. Right-click in Project window → Create → Game → Powerup Icon Manager
2. Name it "PowerupIcons" 
3. Assign all powerup icon sprites:
   - Max Hand Upgrade Icon
   - Homing Daggers Icon
   - AOE Attack Icon
   - Double Gems Icon
   - Slow Time Icon
   - God Mode Icon
4. Drag this asset to the PowerupInventoryManager's "Icon Manager" field

### 3. UI Layout Recommendations

#### Slot Structure (repeat 6 times):
```
PowerupSlot_0
├── Background (Image)
├── Icon (Image) 
├── InfoText (TextMeshPro)
└── SelectionBorder (Image, optional)
```

#### Visual Settings:
- **Selected Slot Color**: Yellow (#FFFF00)
- **Normal Slot Color**: White (#FFFFFF)
- **Selection Scale Multiplier**: 1.1
- **Scroll Sensitivity**: 1.0

### 4. Input Controls
- **Mouse Scroll Wheel**: Navigate between powerup slots
- **Middle Mouse Button**: Activate selected powerup
- Only occupied slots can be selected
- Selection wraps around (first ↔ last)

### 5. Integration with Existing Systems
The system automatically integrates with:
- PlayerAOEAbility
- PlayerProgression (DoubleGems, SlowTime)
- PlayerHealth (GodMode)
- DynamicPlayerFeedManager

### 6. Features
- **Sequential Filling**: Powerups fill slots 0→5 in order
- **Stacking**: Same powerup types stack (charges/duration)
- **Visual Selection**: Selected slot highlighted with color/scale
- **Auto-Cleanup**: Empty slots are hidden automatically
- **Event Integration**: Listens to existing powerup events

### 7. Display Logic
- **Charge-based powerups** (AOE, Homing): Show number only
- **Duration-based powerups** (DoubleGems, SlowTime, GodMode): Show time with "s"
- **Active countdown**: Updates in real-time during powerup use

## Testing
1. Collect multiple different powerups
2. Use scroll wheel to navigate between them
3. Middle-click to activate selected powerup
4. Verify visual feedback and slot management

## Troubleshooting
- Ensure all slot references are properly assigned
- Check that PowerupIconManager is assigned and has all icons
- Verify the GameObject has the PowerupInventoryManager script
- Make sure existing powerup systems are not conflicting
