# In-World Shop System Setup Guide

## 🎯 Overview
This creates an in-world shop with two-step interaction where players can purchase:
- **Flight Unlock** - 500 gems (enables CelestialDrift flight)
- **Secondary Hand Unlock** - 500 gems (enables dual-wielding)

## 🎮 Two-Step Interaction System
1. **Step 1**: Player approaches within 50 meters → Static shop UI appears
2. **Step 2**: Player presses E → Mouse cursor appears, movement/shooting disabled, buttons become interactive

## 📦 Scripts Created
- `ShopCube.cs` - Main shop interaction controller with two-step system
- `WorldShopUI.cs` - Static world-space UI management
- This setup guide

## 🔧 Unity Setup Instructions

### Step 1: Create Shop Cube GameObject
1. Create new **Cube GameObject** in scene
2. Name it `ShopCube`
3. Add `ShopCube.cs` script to it
4. Position where you want the shop (recommend near spawn area)
5. **Optional**: Change cube material to make it look like a shop (cyan/gold color works well)

### Step 2: Create World Shop UI Prefab
1. Create new **Canvas** GameObject
2. Set Canvas **Render Mode** = World Space
3. Set Canvas **World Camera** = Main Camera
4. Scale Canvas to reasonable size (try 0.01, 0.01, 0.01)
5. Add child **Panel** to Canvas

#### Panel Structure:
```
Canvas (WorldShopUI)
├── Panel (Main Shop Panel)
    ├── HeaderText ("GEMINI SHOP")
    ├── PlayerGemsText ("Your Gems: 0")
    ├── FlightSection
    │   ├── FlightIcon (Image)
    │   ├── FlightTitleText ("Flight Unlock")
    │   ├── FlightPriceText ("500 Gems")
    │   ├── FlightStatusText ("Available")
    │   └── FlightUnlockButton ("BUY FLIGHT")
    ├── SecondHandSection
    │   ├── SecondHandIcon (Image)
    │   ├── SecondHandTitleText ("Dual Wielding")
    │   ├── SecondHandPriceText ("500 Gems")
    │   ├── SecondHandStatusText ("Available")
    │   └── SecondHandUnlockButton ("BUY SECOND HAND")
    └── CloseButton ("X" or "CLOSE")
```

### Step 3: Configure WorldShopUI Script
1. Add `WorldShopUI.cs` to the Canvas
2. Assign all UI references in inspector:
   - **Flight Unlock Button**: FlightUnlockButton
   - **Flight Price Text**: FlightPriceText  
   - **Flight Status Text**: FlightStatusText
   - **Flight Icon**: FlightIcon
   - **Second Hand Unlock Button**: SecondHandUnlockButton
   - **Second Hand Price Text**: SecondHandPriceText
   - **Second Hand Status Text**: SecondHandStatusText  
   - **Second Hand Icon**: SecondHandIcon
   - **Player Gems Text**: PlayerGemsText
   - **Close Button**: CloseButton

### Step 4: Create Prefab
1. Drag the complete Canvas to Project window to create prefab
2. Name it `WorldShopUI`
3. Delete Canvas from scene (we only want the prefab)

### Step 5: Configure ShopCube
In the `ShopCube` inspector:
- **Interaction Range**: 50 (distance to show shop UI)
- **World Shop UI Prefab**: Drag the WorldShopUI prefab here
- **World UI Position**: Will auto-create if empty
- **Interaction Prompt**: Optional UI element (not required for two-step system)
- **Shop Cube Color**: Cyan or any highlight color
- **Glow When Nearby**: true for visual feedback

## 🎮 How It Works

### Two-Step Player Interaction:
1. Player approaches cube within 50 meters
2. Cube glows and static shop UI appears above cube
3. Message shows "Press E to Interact with Menu"
4. E key enables mouse cursor and disables movement/shooting
5. Player can click buttons to make purchases
6. ESC key exits interaction mode (re-enables movement) and hides cursor

### Purchase Logic:
- **Flight Unlock**: Sets `CelestialDriftController.isFlightUnlocked = true`
- **Second Hand Unlock**: Calls `PlayerSecondHandAbility.ForceUnlockForRun()`
- **Gem Spending**: Uses `PlayerProgression.TrySpendGems()`
- **Real-time Updates**: Shows gem count and availability status

### Visual Feedback:
- Buttons only work when E key is pressed (interaction active)
- Status text shows "UNLOCKED" for purchased items  
- Dynamic gem counter updates in real-time
- Shop cube glows when player approaches
- Static UI screen (doesn't follow camera)

## 🚀 Features Included

### ✅ Two-Step Interaction System:
- **Step 1**: 50m proximity → Static UI appears
- **Step 2**: E key → Mouse enabled, movement disabled
- ESC key → Exit interaction, re-enable movement

### ✅ Complete Integration:
- Works with existing gem system
- Integrates with CelestialDriftController
- Connects to PlayerSecondHandAbility
- Uses PlayerProgression for gem transactions
- Disables AAAMovementController and PlayerShooterOrchestrator during interaction

### ✅ User Experience:
- Static world-space UI (player must face it)
- Player can walk around while UI is visible
- Large interaction range (50 meters)
- Mouse cursor control
- Real-time gem count updates
- Clear purchase status

### ✅ Error Handling:
- Insufficient gems messaging
- Already purchased detection
- Missing component warnings
- Proper cleanup on destroy

## 🎯 Testing Checklist
- [ ] Cube appears in scene and glows on approach (50m range)
- [ ] Static shop UI appears above cube when approaching
- [ ] Message shows "Press E to Interact with Menu"
- [ ] E key enables mouse cursor and disables movement/shooting
- [ ] Buttons work only when interaction is active
- [ ] Purchase buttons work when player has enough gems
- [ ] Flight unlock enables CelestialDrift
- [ ] Second hand unlock enables dual-wielding
- [ ] ESC exits interaction and re-enables movement
- [ ] Already purchased items show as "UNLOCKED"

## 🔧 Customization Options
- Change gem costs by modifying `flightUnlockCost` and `secondHandUnlockCost`
- Adjust interaction range in `interactionRange`
- Modify shop cube appearance with materials and colors
- Add more items by extending the UI and purchase logic

The shop system is now complete and ready to use! 🎉
