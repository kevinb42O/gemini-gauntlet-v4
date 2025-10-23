# DoubleGemsTracker Setup Guide

## Overview
The DoubleGemsTracker system integrates with existing PowerupDisplay slots to show bonus gems collected during double gems powerup with flickering animations as time runs out.

## Setup Instructions

### 1. Create DoubleGemsTracker GameObject
1. In your main game scene, create an empty GameObject
2. Name it "DoubleGemsTracker"
3. Add the `DoubleGemsTracker` script component

### 2. Update PowerupDisplay for DoubleGems
Add a bonus gems text field to your existing DoubleGems PowerupDisplay slot:

**In the PowerupDisplay prefab/GameObject:**
1. Add a new TextMeshPro - UI component as a child
2. Name it "BonusGemsText"
3. Position it below or next to the existing charges text
4. Set initial color to Yellow (#FFFF00)
5. Set font size to 14-18 (smaller than main charges text)

**Recommended Layout:**
```
PowerupDisplay (DoubleGems)
├── Background Image
├── Icon Image  
├── ChargesText (shows countdown: "15s")
└── BonusGemsText (shows bonus: "+25") ← NEW
```

### 3. Configure DoubleGemsTracker Component
In the Inspector, assign the following:

**PowerupDisplay Integration:**
- `Double Gems Powerup Display`: Drag the PowerupDisplay component that handles DoubleGems powerup

**Flickering Settings:**
- `Slow Flicker Threshold`: 5 (starts slow flicker at 5 seconds)
- `Fast Flicker Threshold`: 2 (starts fast flicker at 2 seconds)

**Debug:**
- `Verbose Debugging`: Check this for detailed console logs

### 4. Configure PowerupDisplay Component
In the PowerupDisplay Inspector, assign:
- `Bonus Gems Text`: Drag the new BonusGemsText component you created

### 5. Integration Notes
The system automatically integrates with:
- **InventoryManager**: Listens for gem collection events
- **PlayerProgression**: Starts/stops tracking when double gems activates/deactivates
- **DynamicPlayerFeedManager**: Shows bonus gems awarded message

### 6. How It Works
1. When double gems powerup activates, the UI appears
2. As gems are collected, the bonus count increases (showing the extra gems that will be awarded)
3. Time remaining counts down
4. Text starts flickering slowly at 5 seconds, then faster at 2 seconds
5. When time expires, bonus gems are added to inventory and UI disappears

### 7. Visual Behavior
- **Normal State**: Yellow text showing bonus gems and time
- **Slow Flicker** (≤5s): Text alternates between yellow and white every 0.5s
- **Fast Flicker** (≤2s): Text alternates between yellow and white every 0.15s
- **End State**: UI disappears, bonus gems message shows

### 8. Testing
To test the system:
1. Activate double gems powerup from inventory
2. Collect gems during the active period
3. Watch the bonus counter increase
4. Observe flickering behavior as time runs out
5. Verify bonus gems are awarded when period ends

### 9. Troubleshooting
- **UI not showing**: Check that DoubleGemsUI is assigned and initially inactive
- **No bonus gems**: Verify InventoryManager.OnGemCollected event is firing
- **No flickering**: Check flicker threshold values and text component assignment
- **Gems not awarded**: Check console for DoubleGemsTracker debug messages

### 10. Customization
You can customize:
- UI position and styling
- Flicker timing and colors
- Text formatting and fonts
- Panel background and effects
- Debug verbosity level
