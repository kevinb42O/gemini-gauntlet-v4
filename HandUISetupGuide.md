# Hand UI Manager Setup Guide

## Overview
The **HandUIManager** is an amazing dynamic UI system that displays both hands' levels, gem collection progress, heat management, and provides stunning visual effects. It features real-time updates, smooth animations, beautiful progress indicators, and comprehensive heat monitoring with overheat warnings.

## Features
- ✨ **Dynamic Text Fields** - Real-time level and gem count updates
- 📊 **Progress Bars** - Visual progress tracking with gradient colors
- 🔥 **Heat Management Display** - Real-time heat percentage and overheat warnings
- 🎆 **Level Up Effects** - Spectacular animations when hands level up
- 💎 **Gem Collection Effects** - Satisfying visual feedback for gem collection
- ⚠️ **Heat Warning System** - Visual alerts when hands approach overheating
- 🚨 **Overheat Animations** - Dramatic effects when hands overheat
- 🌟 **Player Particle Effects** - Immersive particle effects attached to player/camera
- 🎨 **Customizable Animations** - Scale, color, and timing controls
- 🔄 **Auto-Sync** - Automatically syncs with PlayerProgression and PlayerOverheatManager systems

## Setup Instructions

### 1. Create UI Canvas
1. Create a new Canvas in your scene (UI → Canvas)
2. Set Canvas Scaler to "Scale With Screen Size"
3. Set Reference Resolution to 1920x1080

### 2. Create Player Particle Effects
Before setting up the UI, create particle effects as children of your player/camera:

```
Player (or Main Camera)
├── LeftHandHeatWarningParticles (Particle System)
├── LeftHandOverheatParticles (Particle System)
├── RightHandHeatWarningParticles (Particle System)
└── RightHandOverheatParticles (Particle System)
```

**Particle System Setup:**
- **Heat Warning Particles**: Subtle orange/yellow particles for 70%+ heat
- **Overheat Particles**: Intense red/fire particles for overheated state
- **Play On Awake**: Set to FALSE (HandUIManager will control them)
- **Looping**: Set to TRUE for continuous effects
- **Position**: Adjust to appear near hand positions in player's view

### 3. Create Hand UI Structure
Create the following UI hierarchy:

```
Canvas
├── HandUIManager (Empty GameObject with HandUIManager script)
├── LeftHandPanel (Panel)
│   ├── LeftHandContainer (Empty GameObject - RectTransform)
│   │   ├── LeftHandLevelText (TextMeshPro - UGUI)
│   │   ├── LeftHandGemsText (TextMeshPro - UGUI)
│   │   ├── LeftHandProgressText (TextMeshPro - UGUI)
│   │   ├── LeftHandProgressBar (Slider)
│   │   ├── LeftHandHeatText (TextMeshPro - UGUI)
│   │   └── LeftHandHeatBar (Slider)
│   └── LeftHandBackground (Image - Optional)
└── RightHandPanel (Panel)
    ├── RightHandContainer (Empty GameObject - RectTransform)
    │   ├── RightHandLevelText (TextMeshPro - UGUI)
    │   ├── RightHandGemsText (TextMeshPro - UGUI)
    │   ├── RightHandProgressText (TextMeshPro - UGUI)
    │   ├── RightHandProgressBar (Slider)
    │   ├── RightHandHeatText (TextMeshPro - UGUI)
    │   └── RightHandHeatBar (Slider)
    └── RightHandBackground (Image - Optional)
```

### 4. Assign Particle Effect References

In the HandUIManager component, assign the particle system references:

#### Player Heat Particle Effects:
- `leftHandHeatWarningParticles` → LeftHandHeatWarningParticles component
- `leftHandOverheatParticles` → LeftHandOverheatParticles component
- `rightHandHeatWarningParticles` → RightHandHeatWarningParticles component
- `rightHandOverheatParticles` → RightHandOverheatParticles component

### 5. Configure UI Elements

#### Text Components (TextMeshPro - UGUI)
- **Font Size**: 24-36 for level text, 18-24 for other text
- **Color**: White or bright colors for visibility (heat text can start white, will change to red when overheated)
- **Alignment**: Center alignment recommended
- **Auto Size**: Enable for responsive text

#### Progress Bars (Slider) - For Gem Progress
- **Direction**: Left to Right
- **Min Value**: 0
- **Max Value**: 1
- **Whole Numbers**: Disabled
- **Fill Rect**: Assign a child Image component
- **Handle**: Disable handle (set Handle Slide Area to inactive)

#### Heat Bars (Slider) - For Heat Display
- **Direction**: Left to Right
- **Min Value**: 0
- **Max Value**: 1
- **Whole Numbers**: Disabled
- **Fill Rect**: Assign a child Image component (will use heat gradient)
- **Handle**: Disable handle (set Handle Slide Area to inactive)
- **Colors**: Will automatically use heat gradient (cool blue → hot red)

#### Positioning Recommendations
- **Left Hand Panel**: Anchor to bottom-left (0, 0) with offset
- **Right Hand Panel**: Anchor to bottom-right (1, 0) with offset
- **Spacing**: Leave enough space between panels

### 6. Assign UI References in HandUIManager

In the HandUIManager component, assign all the UI references:

#### Left Hand (Secondary) References:
- `leftHandLevelText` → LeftHandLevelText component
- `leftHandGemsText` → LeftHandGemsText component  
- `leftHandProgressText` → LeftHandProgressText component
- `leftHandProgressBar` → LeftHandProgressBar component
- `leftHandHeatText` → LeftHandHeatText component
- `leftHandHeatBar` → LeftHandHeatBar component
- `leftHandContainer` → LeftHandContainer RectTransform

#### Right Hand (Primary) References:
- `rightHandLevelText` → RightHandLevelText component
- `rightHandGemsText` → RightHandGemsText component
- `rightHandProgressText` → RightHandProgressText component
- `rightHandProgressBar` → RightHandProgressBar component
- `rightHandHeatText` → RightHandHeatText component
- `rightHandHeatBar` → RightHandHeatBar component
- `rightHandContainer` → RightHandContainer RectTransform

### 7. Configure Visual Effects

#### Progress Bar Gradient (For Gem Progress)
1. Create a new Gradient asset or configure in inspector
2. Set colors: Red (0%) → Yellow (50%) → Green (100%)
3. Assign to `progressBarGradient` field

#### Heat Bar Gradient (For Heat Display)
1. Create a new Gradient asset or configure in inspector
2. Set colors: Cool Blue (0%) → Yellow (50%) → Hot Red (100%)
3. Assign to `heatBarGradient` field

#### Effect Prefabs (Optional)
- Create particle effect prefabs for level up and gem collection
- Create warning effect prefabs for heat warnings and overheat
- Assign to `levelUpEffectPrefab`, `gemCollectionEffectPrefab`, and `overheatWarningEffectPrefab`

#### Animation Settings
- `scaleAnimationIntensity`: 1.2 (20% scale increase)
- `animationDuration`: 0.3 seconds
- `levelUpGlowDuration`: 2.0 seconds
- `gemPulseDuration`: 0.5 seconds
- `heatWarningPulseDuration`: 1.0 seconds

### 8. Color Configuration

#### Flash Colors
- `levelUpFlashColor`: Bright Yellow (#FFFF00)
- `gemCollectionFlashColor`: Bright Cyan (#00FFFF)
- `heatWarningFlashColor`: Bright Red (#FF0000)
- `overheatedTextColor`: Red (#FF0000) - for "OVERHEATED!" text

## Usage

### Automatic Operation
The HandUIManager automatically:
- Subscribes to PlayerProgression events
- Subscribes to PlayerOverheatManager events
- Updates UI when hands level up
- Shows progress toward next level
- Displays current gem counts
- Shows real-time heat percentages
- Displays "OVERHEATED!" when hands overheat
- Triggers visual effects for all events
- Changes heat bar colors based on temperature
- Warns when heat approaches dangerous levels (70%+)
- **Activates heat warning particles at 70%+ heat**
- **Activates overheat particles when hands overheat**
- **Deactivates particles when cooling down**

### Manual Control
You can manually control the UI:

```csharp
// Force update specific hand
HandUIManager.Instance.ForceUpdateHandUI(true, 3, 12, 15); // Right hand, level 3, 12/15 gems

// Force update heat display
HandUIManager.Instance.ForceUpdateHeatUI(true, 75f, 100f, false); // Right hand, 75% heat, not overheated

// Refresh all UI
HandUIManager.Instance.RefreshAllHandUI();

// Test effects (available in context menu)
HandUIManager.Instance.TestLeftHandLevelUp();
HandUIManager.Instance.TestRightHandGemCollection();
HandUIManager.Instance.TestLeftHandHeatWarning();
HandUIManager.Instance.TestRightHandOverheat();
HandUIManager.Instance.TestLeftHandRecovery();

// Test particle effects (available in context menu)
HandUIManager.Instance.TestLeftHandHeatWarningParticles();
HandUIManager.Instance.TestRightHandOverheatParticles();
HandUIManager.Instance.StopAllHeatParticles();
```

## Integration with Existing Systems

### PlayerProgression Integration
The HandUIManager automatically integrates with:
- `PlayerProgression.OnPrimaryHandLevelChangedForHUD`
- `PlayerProgression.OnPrimaryHandGemsChangedForHUD`
- `PlayerProgression.OnSecondaryHandLevelChangedForHUD`
- `PlayerProgression.OnSecondaryHandGemsChangedForHUD`

### PlayerOverheatManager Integration
The HandUIManager automatically integrates with:
- `PlayerOverheatManager.OnHeatChangedForHUD`
- `PlayerOverheatManager.OnHandFullyOverheated`
- `PlayerOverheatManager.OnHandRecoveredFromForcedCooldown`

### HandLevelPersistenceManager Integration
The system works seamlessly with the existing hand level persistence system.

## Customization Options

### Animation Tweaks
- Modify `scaleAnimationIntensity` for more/less dramatic scaling
- Adjust `animationDuration` for faster/slower animations
- Change flash colors for different visual themes

### Progress Bar Styling
- Customize the gradient for different color schemes
- Modify progress bar background and fill images
- Add border effects or shadows

### Text Formatting
- Change font styles and sizes
- Add text shadows or outlines
- Modify text content format in the update methods

## Troubleshooting

### UI Not Updating
1. Check that PlayerProgression.Instance exists
2. Verify all UI references are assigned
3. Ensure the HandUIManager is active in the scene

### Visual Effects Not Working
1. Check that effect prefabs are assigned
2. Verify containers have proper RectTransform components
3. Ensure gradient is properly configured

### Performance Issues
1. Reduce animation frequency if needed
2. Optimize effect prefabs
3. Consider object pooling for frequent effects

## Advanced Features

### Custom Events
You can extend the system by adding custom events:

```csharp
public static event System.Action<bool> OnHandMaxLevel; // bool = isRightHand
```

### Additional Visual Effects
- Add screen shake on level up
- Implement sound effects integration
- Create combo multiplier displays
- Add achievement notifications

## Testing

Use the context menu options in the inspector:

**Level & Gem Effects:**
- "Test Left Hand Level Up"
- "Test Right Hand Level Up" 
- "Test Left Hand Gem Collection"
- "Test Right Hand Gem Collection"

**Heat Effects:**
- "Test Left Hand Heat Warning"
- "Test Right Hand Heat Warning"
- "Test Left Hand Overheat"
- "Test Right Hand Overheat"
- "Test Left Hand Recovery"
- "Test Right Hand Recovery"

**Particle Effects:**
- "Test Left Hand Heat Warning Particles"
- "Test Right Hand Heat Warning Particles"
- "Test Left Hand Overheat Particles"
- "Test Right Hand Overheat Particles"
- "Stop All Heat Particles"

These will trigger the visual effects without affecting game data.

---

**Enjoy your amazing new Hand UI system! 🚀✨**
