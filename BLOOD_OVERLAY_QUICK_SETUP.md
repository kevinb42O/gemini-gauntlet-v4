# 🩸 BLOOD OVERLAY - QUICK SETUP GUIDE

## The Problem You Had
Pause menu and blood splat overlay were **breaking each other** - you could only have one working at a time.

## The Solution (5 Minutes)

### Step 1: Create Independent Canvas
1. In Unity Hierarchy, right-click at the ROOT (not inside Player!)
2. UI → Canvas
3. Name it: `BloodOverlayCanvas`

### Step 2: Configure Canvas
Select `BloodOverlayCanvas`:
- **Render Mode**: Screen Space - Overlay
- **Sort Order**: 100

### Step 3: Create Blood Overlay Image
1. Right-click `BloodOverlayCanvas` → UI → Image
2. Name it: `BloodSplatOverlay`
3. Set to full screen:
   - Anchor Preset: Click the square in bottom-right (stretch/stretch)
   - Left: 0, Top: 0, Right: 0, Bottom: 0
4. Assign your blood splat texture to `Source Image`
5. **UNCHECK** "Raycast Target"

### Step 4: Add CanvasGroup
Select `BloodSplatOverlay`:
1. Add Component → Canvas Group
2. Set **Alpha**: 0
3. **UNCHECK** "Interactable"
4. **UNCHECK** "Block Raycasts"

### Step 5: Link to PlayerHealth
1. Select your **Player** GameObject
2. Find **PlayerHealth** component
3. Drag `BloodSplatOverlay` into the **Blood Overlay Image** field

## ✅ Done!

Your hierarchy should look like:
```
Scene
├── BloodOverlayCanvas ← Independent, at root!
│   └── BloodSplatOverlay (Image + CanvasGroup)
├── Player
│   └── PlayerHealth (references BloodSplatOverlay)
└── UICanvas
    └── PauseMenuPanel
```

## Test It
1. Take damage → Blood splat fades in/out ✅
2. Press ESC → Pause menu works ✅
3. Take damage then pause → Both work together ✅

**That's it! No more conflicts!** 🎉
