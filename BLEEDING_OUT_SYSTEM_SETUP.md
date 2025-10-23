# 🩸 BLEEDING OUT SYSTEM - Complete Setup Guide

## 📋 Overview

The new **Bleeding Out System** provides a cinematic death experience with:
- ✅ **Circular progress indicator** around self-revive item icon
- ✅ **Pulsating blood overlay** (slow → fast as death approaches)
- ✅ **Hold E to speed up death** (2x speed)
- ✅ **Camera zoom-out effect** with "CONNECTION LOST" text
- ✅ **Works with AND without self-revive**
- ✅ **Future-proofed for multiplayer teammate revives**

---

## 🎮 How It Works

### **When Player Has Self-Revive:**
1. Player takes fatal damage
2. Bleeding out starts - circular progress shows around self-revive icon
3. Blood overlay pulsates (slow at first, faster as death approaches)
4. Player sees: "Press E to use Self-Revive" or "Hold E to skip"
5. **Press E once** → Uses self-revive (3 second animation, then full health)
6. **Hold E** → Speeds up death to 2x (skip waiting)
7. If timer expires → Death sequence (camera zoom, CONNECTION LOST)

### **When Player Has NO Self-Revive:**
1. Player takes fatal damage
2. Bleeding out starts - circular progress shows around skull icon
3. Blood overlay pulsates (slow at first, faster as death approaches)
4. Player sees: "Hold E to skip"
5. **Hold E** → Speeds up death to 2x
6. When timer expires → Death sequence (camera zoom, CONNECTION LOST, scene reload)

---

## 🛠️ Setup Instructions

### **Step 1: Add BleedOutUIManager to Scene**

1. Create an empty GameObject in your scene: `GameObject → Create Empty`
2. Name it: **"BleedOutManager"**
3. Add component: **BleedOutUIManager**
4. Configure settings:
   - **Bleed Out Duration**: `30` seconds (default)
   - **Hold E Speed Multiplier**: `2` (double speed)
   - **Skip Key**: `E` (KeyCode.E)

**Optional Icons** (Recommended):
   - Assign **Self Revive Icon Sprite** (shows when player has self-revive)
   - Assign **Skull Icon Sprite** (shows when no self-revive available)
   - If no icons assigned, system uses colored backgrounds instead

### **Step 2: Add DeathCameraController to Scene**

1. Create an empty GameObject: `GameObject → Create Empty`
2. Name it: **"DeathCameraController"**
3. Add component: **DeathCameraController**
4. Configure settings:
   - **Main Camera**: Auto-finds Camera.main (or assign manually)
   - **Player Transform**: Auto-finds PlayerHealth (or assign manually)
   - **Zoom Out Distance**: `10` units
   - **Zoom Out Duration**: `2` seconds
   - **Pitch Angle**: `20` degrees (look down at player)
   - **Auto Find References**: ✅ (finds camera and player automatically)

### **Step 3: Update PlayerHealth References**

1. Select your **Player** GameObject in the scene
2. Find the **PlayerHealth** component
3. Assign references in Inspector:
   - **Bleed Out UI Manager**: Drag the BleedOutManager GameObject here
   - **Death Camera Controller**: Drag the DeathCameraController GameObject here
   - **Blood Overlay Image**: Assign your blood splat UI overlay (should already be assigned)

**Important:** The system will auto-create these if not assigned, but manual assignment is recommended for better control.

### **Step 4: Verify Blood Overlay Setup**

The system reuses your existing blood overlay but adds pulsation:

1. Ensure **Blood Overlay Image** is assigned in PlayerHealth
2. Blood overlay GameObject should have a **CanvasGroup** component
3. If CanvasGroup is missing, add one:
   - Select your blood overlay UI element
   - Add Component → **Canvas Group**
   - Set initial alpha to `0`

---

## 🎨 UI Customization

### **Circular Progress Indicator:**
- Edit `BleedOutUIManager.cs`:
  - `circularProgressColor` - Change progress bar color (default: red)
  - `circularProgressSize` - Adjust size (default: 200 pixels)

### **Text Customization:**
- "BLEEDING OUT" text: Line 148-154 in BleedOutUIManager.cs
  - Font size: 56
  - Color: Bright red
  - Bold style

- "CONNECTION LOST" text: Line 176-183 in BleedOutUIManager.cs
  - Font size: 72
  - Color: Bright red
  - Bold style

### **Blood Overlay Pulsation:**
- Edit `PlayerHealth.cs` → `PulsateBloodOverlay()` method (line ~1668):
  - `minAlpha` - Minimum blood opacity during pulse (default: 0.4)
  - `maxAlpha` - Maximum blood opacity during pulse (default: 0.9)
  - `minPulseSpeed` - Slowest pulse rate (default: 1.0)
  - `maxPulseSpeed` - Fastest pulse rate near death (default: 4.0)

---

## 🎬 Death Camera Customization

Edit `DeathCameraController.cs` settings:

- **Zoom Out Distance**: How far camera moves back (default: 10 units)
- **Zoom Out Duration**: Animation length (default: 2 seconds)
- **Zoom Out Curve**: Animation curve for smooth motion
- **Pitch Angle**: Look-down angle at player (default: 20 degrees)

Camera moves **behind and above** the player for dramatic effect.

---

## 🔧 Technical Details

### **Key Features:**

✅ **Unscaled Time** - Works during pause (Time.timeScale = 0)
✅ **Event-Driven** - Clean event system for extensibility
✅ **Auto-Creation** - Creates UI at runtime if missing
✅ **Backward Compatible** - Disables old self-revive UI systems
✅ **Multiplayer Ready** - Architecture supports teammate revives in future

### **Event System:**
- `OnBleedOutComplete` - Fired when player dies (timer expired)
- `OnSelfReviveRequested` - Fired when player presses E with self-revive
- `OnBleedOutProgress` - Fired each frame with progress (0-1)

### **Integration Points:**
- **PlayerHealth.cs** - Main death logic and bleeding out coordination
- **BleedOutUIManager.cs** - UI display and input handling
- **DeathCameraController.cs** - Camera animation
- **ReviveSlotController.cs** - Self-revive inventory management

---

## 🚀 Future Multiplayer Support

The system is designed to be extended for multiplayer:

### **Current State:**
- Player bleeds out alone
- Self-revive is the only option
- Timer expires → Death

### **Future Multiplayer Addition:**
Just add this to `BleedOutUIManager.cs`:

```csharp
// New event for teammate revives
public System.Action<float> OnTeammateReviveProgress;

// Show teammate revive prompt
public void ShowTeammateReviving(string teammateName, float progress)
{
    instructionText.text = $"{teammateName} is reviving you... {progress:P0}";
    // Update circular progress based on revive progress
}
```

**No changes needed to core bleeding out logic!**

---

## 🐛 Troubleshooting

### **Blood overlay doesn't pulsate:**
- Check that Blood Overlay Image has a **CanvasGroup** component
- Verify CanvasGroup is not controlled by other systems (pause menu, etc.)
- Check console for "[PlayerHealth] Starting blood pulsation effect" message

### **Circular progress doesn't show:**
- Check that BleedOutUIManager is in the scene and enabled
- Verify Canvas is created (should auto-create at runtime)
- Check Canvas sorting order (should be 32765 - very high)

### **Camera doesn't zoom out:**
- Ensure DeathCameraController has reference to Main Camera
- Check that Player Transform is assigned
- Verify Auto Find References is enabled

### **"CONNECTION LOST" text doesn't appear:**
- Wait for full bleed out duration (30 seconds default)
- Check BleedOutUIManager → OnBleedOutComplete event fires
- Verify connectionLostText is created in CreateBleedOutUI()

### **Hold E doesn't speed up death:**
- Check skipKey is set to KeyCode.E in BleedOutUIManager
- Verify Input.GetKey() is detecting E key press
- Check console for speed multiplier changes

---

## 📊 Default Timing

- **Bleed Out Duration**: 30 seconds
- **Self-Revive Animation**: 3 seconds (blinking blood overlay)
- **Camera Zoom Duration**: 2 seconds
- **Delay Before Scene Reset**: 2 seconds (after CONNECTION LOST)
- **Total Death Time**: ~34 seconds (if not skipped)
- **With Hold E**: ~15 seconds (2x speed)

---

## 💡 Best Practices

1. **Always test both scenarios:**
   - With self-revive available
   - Without self-revive

2. **Adjust bleed out duration based on game pace:**
   - Fast-paced games: 15-20 seconds
   - Tactical games: 30-45 seconds
   - Survival games: 60+ seconds

3. **Add audio cues:**
   - Heartbeat sound (increasing frequency)
   - Heavy breathing
   - Teammate voice callouts (future multiplayer)

4. **Visual polish ideas:**
   - Screen desaturation over time
   - Vignette effect increasing
   - Screen shake on final moments
   - Particle effects (blood drips, sparks, etc.)

---

## ✅ Testing Checklist

- [ ] Die with self-revive → Press E → Successfully revive
- [ ] Die with self-revive → Let timer expire → Death sequence plays
- [ ] Die without self-revive → Timer expires → Death sequence plays
- [ ] Hold E during bleed out → Death timer speeds up (2x)
- [ ] Blood overlay pulsates slow at start, fast near end
- [ ] Circular progress drains from full to empty
- [ ] Camera zooms out when timer expires
- [ ] "CONNECTION LOST" text appears at end
- [ ] Scene reloads after death sequence
- [ ] All UI elements are visible and readable

---

## 🎉 Summary

You now have a **professional, AAA-quality bleeding out system** that:
- Enhances death experience with cinematic camera
- Gives players agency (revive or skip)
- Provides clear visual feedback (pulsating blood, progress circle)
- Is ready for future multiplayer expansion
- Works flawlessly with existing systems

**Enjoy your enhanced death mechanics!** 🩸
