# 🎨 TOWER PROTECTOR CUBE - UI SETUP GUIDE

## Overview
The capture UI now displays **TWO sliders**:
1. **Capture Progress** - Shows platform capture progress (cyan → green)
2. **Cube Health** - Shows Tower Protector Cube health (red → green, or cyan when friendly)

---

## 🚀 Quick UI Setup in Unity

### Step 1: Create the UI Canvas (if not exists)
1. Right-click in Hierarchy → **UI → Canvas**
2. Set Canvas to **Screen Space - Overlay**
3. Add **Canvas Scaler** component
4. Set UI Scale Mode to **Scale With Screen Size**
5. Reference Resolution: **1920 x 1080**

### Step 2: Create Capture Progress Slider
1. Right-click Canvas → **UI → Slider**
2. Rename to **"CaptureProgressSlider"**
3. Position at bottom-center of screen
4. Recommended settings:
   - Width: **600**
   - Height: **40**
   - Anchor: Bottom-Center
   - Position Y: **100** (from bottom)

### Step 3: Create Cube Health Slider
1. Right-click Canvas → **UI → Slider**
2. Rename to **"CubeHealthSlider"**
3. Position **above** the capture slider
4. Recommended settings:
   - Width: **600**
   - Height: **30**
   - Anchor: Bottom-Center
   - Position Y: **150** (from bottom)

### Step 4: Style the Sliders

#### Capture Progress Slider
1. Select **Fill** child object
2. Set color to **Cyan** (will animate to green)
3. Optional: Add **Background** with dark color

#### Cube Health Slider
1. Select **Fill** child object
2. Set color to **Green** (will animate based on health)
3. Optional: Add **Background** with dark color
4. Optional: Make slightly smaller/thinner than capture slider

### Step 5: Connect to PlatformCaptureUI Script
1. Select the GameObject with **PlatformCaptureUI** component
2. In Inspector:
   - Drag **CaptureProgressSlider** to **"Capture Slider"** field
   - Drag **CubeHealthSlider** to **"Cube Health Slider"** field
   - Optional: Assign **UI Container** if you want to group them

### Step 6: Customize Colors (Optional)
In PlatformCaptureUI Inspector:
- **Capturing Color**: Cyan (default)
- **Complete Color**: Green (default)
- **Cube Health High Color**: Green (default)
- **Cube Health Low Color**: Red (default)
- **Cube Health Friendly Color**: Cyan-Green (default)

---

## 🎨 Visual Behavior

### Capture Progress Slider
- **Hidden** when player not on platform
- **Visible** when player on platform
- **Color**: Cyan → Green (as progress increases)
- **Value**: 0% → 100% over capture duration

### Cube Health Slider
- **Hidden** when:
  - Player not on platform
  - Cube is dead
  - No cube assigned
- **Visible** when:
  - Player on platform
  - Cube exists and is hostile
- **Color Changes**:
  - **Red → Green**: Based on health (0% = red, 100% = green)
  - **Cyan-Green**: When cube becomes friendly
- **Value**: Current health percentage

---

## 📐 Recommended Layout

```
┌─────────────────────────────────────┐
│                                     │
│         GAMEPLAY AREA               │
│                                     │
│                                     │
│                                     │
│                                     │
│                                     │
│                                     │
│                                     │
│                                     │
│                                     │
│                                     │
│  ┌───────────────────────────────┐  │
│  │ CUBE HEALTH ████████░░░░░░░░░ │  │ ← Cube Health (smaller)
│  └───────────────────────────────┘  │
│  ┌───────────────────────────────┐  │
│  │ CAPTURE     ██████░░░░░░░░░░░ │  │ ← Capture Progress (larger)
│  └───────────────────────────────┘  │
└─────────────────────────────────────┘
```

---

## 🎯 Advanced Customization

### Add Text Labels
1. Add **Text (TextMeshPro)** as child of each slider
2. Position above or inside slider
3. Update text in code:

```csharp
// In PlatformCaptureUI.cs, add:
public TextMeshProUGUI captureText;
public TextMeshProUGUI cubeHealthText;

// In UpdateProgress():
if (captureText != null)
    captureText.text = $"Capturing: {progress * 100:F0}%";

// In UpdateCubeHealth():
if (cubeHealthText != null)
    cubeHealthText.text = isFriendly ? "FRIENDLY" : $"Cube: {healthPercent * 100:F0}%";
```

### Add Icons
1. Add **Image** component next to sliders
2. Assign icon sprites:
   - Capture: Flag/Tower icon
   - Cube Health: Cube/Shield icon

### Add Animations
1. Create **Animator** component on sliders
2. Add animations for:
   - Fade in/out when showing/hiding
   - Pulse effect when cube is low health
   - Flash effect when cube becomes friendly

### Add Sound Effects
1. Play sound when cube health changes significantly
2. Play sound when cube becomes friendly
3. Play sound when cube dies

---

## 🔧 Troubleshooting

### Sliders Not Showing
- Check **Canvas** is active
- Verify sliders assigned in **PlatformCaptureUI** Inspector
- Ensure player is on platform
- Check console for debug messages

### Cube Health Not Updating
- Verify **Tower Protector Cube** assigned in **PlatformCaptureSystem**
- Check cube has **GetHealthPercent()** method
- Ensure cube is not dead
- Look for "Cube Health Slider" assignment

### Colors Not Changing
- Check **Fill** image is assigned on sliders
- Verify colors set in **PlatformCaptureUI** Inspector
- Ensure fill image has **Image** component

### UI Overlapping
- Adjust **Position Y** values to separate sliders
- Use **Layout Groups** for automatic spacing
- Check **Canvas Scaler** settings

---

## 📱 Responsive Design Tips

### For Different Screen Sizes
1. Use **Anchors** properly:
   - Bottom-Center for both sliders
   - Stretch horizontally if needed

2. Use **Canvas Scaler**:
   - Match Width or Height
   - Reference Resolution: 1920x1080

3. Test on different aspect ratios:
   - 16:9 (standard)
   - 21:9 (ultrawide)
   - 4:3 (old monitors)

### For VR/AR (Future)
- Position sliders in **world space**
- Attach to platform or floating above it
- Use **3D UI** instead of screen overlay

---

## 🎨 Style Presets

### Minimalist
```
Capture Slider:
- Width: 400, Height: 30
- No background, just fill
- Simple solid colors

Cube Health:
- Width: 400, Height: 20
- Positioned 10px above capture
- Subtle transparency
```

### Cyberpunk
```
Both Sliders:
- Glowing neon colors
- Animated scan lines
- Holographic effect
- Pulsing borders
```

### Sci-Fi
```
Both Sliders:
- Metallic borders
- Blue/cyan theme
- Hexagonal shapes
- Energy pulse effects
```

---

## 📊 Performance Notes

- **Minimal overhead**: Only updates when player on platform
- **Efficient**: Uses cached Image references
- **Optimized**: No per-frame allocations
- **Clean**: Proper show/hide logic

---

## ✅ Final Checklist

- [ ] Canvas created with proper settings
- [ ] Capture Progress Slider created and styled
- [ ] Cube Health Slider created and styled
- [ ] Both sliders assigned in PlatformCaptureUI
- [ ] Colors customized to your liking
- [ ] Tested in Play Mode
- [ ] UI shows when player enters platform
- [ ] UI hides when player leaves platform
- [ ] Cube health updates in real-time
- [ ] Cube health turns cyan when friendly
- [ ] Cube health hides when cube dies

---

## 🎮 Player Experience

### What Players See:
1. **Enter platform** → Both sliders appear
2. **Cube attacks** → Health bar visible, shows damage
3. **Player shoots cube** → Health bar decreases (red when low)
4. **Cube dies** → Health bar disappears
5. **Platform captured with cube alive** → Health bar turns cyan-green
6. **Leave platform** → Both sliders disappear

### Visual Feedback:
- **Capture Progress**: "How close am I to winning?"
- **Cube Health**: "Should I kill it or keep it alive?"
- **Color Changes**: Clear visual state communication
- **Smooth Animations**: Professional polish

---

**Your UI is now ready to display both capture progress AND cube health! 🎨✨**
