# Health & Energy UI Sliders Setup Guide

## Overview
This guide shows you how to set up the health and energy UI sliders in your main canvas.

## What Was Created
1. **PlayerEnergySystem.cs** - Manages sprint energy (depletes fast when sprinting, regenerates when not)
2. **HealthEnergyUI.cs** - Manages the UI sliders for health and energy
3. **AAAMovementController.cs** - Updated to use energy system for sprinting

## Setup Instructions

### Step 1: Add Components to Player
1. Select your **Player** GameObject in the hierarchy
2. Add the **PlayerEnergySystem** component:
   - Click "Add Component"
   - Search for "PlayerEnergySystem"
   - Add it
3. Configure PlayerEnergySystem settings (optional):
   - **Max Energy**: 100 (default)
   - **Energy Depletion Rate**: 40 (depletes pretty fast as requested)
   - **Energy Regen Rate**: 25 (regeneration speed)
   - **Regen Delay**: 0.5s (delay before regen starts)
   - **Min Energy To Sprint**: 5 (minimum energy needed to sprint)

### Step 2: Create UI Sliders in Canvas

#### Find Your Main Canvas
1. Locate your main game Canvas in the hierarchy (usually named "Canvas" or "GameCanvas")

#### Create Health Slider
1. Right-click on the Canvas → **UI** → **Slider**
2. Rename it to "**HealthSlider**"
3. Position it in the top-left corner of the screen:
   - Set Anchor to **Top-Left**
   - Position X: 150, Y: -30
   - Width: 200, Height: 20
4. Configure the slider:
   - **Min Value**: 0
   - **Max Value**: 1
   - **Value**: 1
   - **Interactable**: OFF (uncheck)
5. Customize appearance:
   - Select "HealthSlider/Fill Area/Fill"
   - Change color to **Red** (RGB: 200, 25, 25)
   - Select "HealthSlider/Background"
   - Change color to **Dark Red** (RGB: 50, 10, 10)

#### Create Energy Slider
1. Right-click on the Canvas → **UI** → **Slider**
2. Rename it to "**EnergySlider**"
3. Position it below the health slider:
   - Set Anchor to **Top-Left**
   - Position X: 150, Y: -60
   - Width: 200, Height: 20
4. Configure the slider:
   - **Min Value**: 0
   - **Max Value**: 1
   - **Value**: 1
   - **Interactable**: OFF (uncheck)
5. Customize appearance:
   - Select "EnergySlider/Fill Area/Fill"
   - Change color to **Cyan** (RGB: 50, 200, 255)
   - Select "EnergySlider/Background"
   - Change color to **Dark Blue** (RGB: 10, 30, 50)

### Step 3: Add HealthEnergyUI Component
1. Create an empty GameObject in your Canvas:
   - Right-click on Canvas → **Create Empty**
   - Rename it to "**HealthEnergyUIManager**"
2. Add the **HealthEnergyUI** component:
   - Select "HealthEnergyUIManager"
   - Click "Add Component"
   - Search for "HealthEnergyUI"
   - Add it
3. Assign the sliders:
   - **Health Slider**: Drag the HealthSlider from hierarchy
   - **Energy Slider**: Drag the EnergySlider from hierarchy
4. (Optional) Assign fill images for color changes:
   - **Health Fill Image**: Drag "HealthSlider/Fill Area/Fill" Image component
   - **Energy Fill Image**: Drag "EnergySlider/Fill Area/Fill" Image component

### Step 4: Add Optional Labels (Recommended)
1. Add text labels above each slider for clarity:
   - Right-click on Canvas → **UI** → **Text - TextMeshPro** (or Text)
   - Position above HealthSlider
   - Set text to "**HEALTH**"
   - Color: White
   - Font Size: 14
2. Repeat for Energy:
   - Position above EnergySlider
   - Set text to "**ENERGY**"
   - Color: Cyan
   - Font Size: 14

## How It Works

### Energy System
- **Sprint Key**: Hold Shift (or your configured boost key) to sprint
- **Energy Depletion**: Energy drains at 40/second while sprinting (depletes fast!)
- **Cannot Sprint**: When energy is below 5, sprinting is disabled
- **Regeneration**: Energy regenerates at 25/second after 0.5s delay
- **Visual Feedback**: Energy bar turns orange when below 30%

### Automatic Animation & FOV Changes
When energy runs out during sprinting:
- **Animation**: Automatically switches from SPRINT to WALK animation
- **FOV (Field of View)**: Returns to normal FOV (no longer sprint FOV)
- **Seamless Transition**: Smooth blend between animations and FOV changes
- **Re-enable Sprint**: Once energy regenerates above 5, you can sprint again

### Health System
- Uses existing PlayerHealth system
- Updates automatically when player takes damage
- Shows current health as a percentage

## Testing
1. Enter Play Mode
2. You should see both sliders at the top-left
3. Hold **Shift** to sprint - watch the energy bar deplete quickly
4. **Watch the animation change from RUN to WALK when energy depletes**
5. **Notice the FOV returns to normal when energy runs out**
6. Release Shift - energy regenerates after a brief delay
7. When energy is empty, you cannot sprint until it regenerates

## Customization Options

### Adjust Energy Depletion Speed
In PlayerEnergySystem component on Player:
- **Faster depletion**: Increase "Energy Depletion Rate" (e.g., 60)
- **Slower depletion**: Decrease "Energy Depletion Rate" (e.g., 20)

### Adjust Regeneration Speed
In PlayerEnergySystem component on Player:
- **Faster regen**: Increase "Energy Regen Rate" (e.g., 40)
- **Slower regen**: Decrease "Energy Regen Rate" (e.g., 15)

### Change UI Position
- Select HealthSlider and EnergySlider
- Adjust their RectTransform positions
- Common positions:
  - **Top-Left**: X: 150, Y: -30
  - **Top-Right**: X: -150, Y: -30 (change anchor to Top-Right)
  - **Bottom-Left**: X: 150, Y: 30 (change anchor to Bottom-Left)

### Change Colors
In HealthEnergyUI component:
- **Health Color**: Change the red color
- **Energy Color**: Change the cyan color
- **Low Energy Color**: Change the orange warning color
- **Low Energy Threshold**: Adjust when color changes (default 30%)

## Troubleshooting

### Sliders Not Updating
- Make sure HealthEnergyUI component is on an active GameObject in the Canvas
- Verify Health Slider and Energy Slider are assigned in the inspector
- Check that PlayerHealth and PlayerEnergySystem are on the Player GameObject

### Sprint Not Using Energy
- Ensure PlayerEnergySystem component is added to the Player
- Check that AAAMovementController is on the same GameObject
- Verify the sprint key is configured correctly in Controls.cs

### Energy Not Regenerating
- Check "Regen Delay" setting (default 0.5s)
- Ensure you're not holding the sprint key
- Verify Energy Regen Rate is > 0

## Complete!
Your health and energy UI sliders are now set up and functional. The energy system will prevent sprinting when depleted, adding a strategic element to movement.
