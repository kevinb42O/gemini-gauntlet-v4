# Companion Selection System Setup Guide

## Overview
This system allows players to equip up to 4 companions from the companion stats panel and displays them in a selection panel that's always visible.

## Components Created

### 1. PersistentCompanionSelectionManager.cs
- Manages which companions are equipped (up to 4)
- Handles visual updates of the selection slots
- Provides methods for game to spawn selected companions
- Persists across scenes

### 2. Updated CompanionSelectionManager.cs
- Added "Equip" button to stats panel
- Button text changes based on companion status:
  - "Equip" - Available to equip
  - "Equipped" - Already equipped (disabled)
  - "Slots Full" - No more slots available (disabled)

## Setup Instructions

### Step 1: Setup PersistentCompanionSelectionManager (Menu Scene)
1. Create an empty GameObject in your **menu scene**
2. Name it "PersistentCompanionSelectionManager"
3. Add the `PersistentCompanionSelectionManager` component
4. In the inspector, assign:
   - **Companion Slot Backgrounds**: 4 Image components for slot backgrounds
   - **Companion Slot Images**: 4 Image components for companion sprites (these will be clickable)
   - **Companion Level Texts**: 4 TextMeshPro components for showing companion levels
   - **Companion Prefabs**: Assign your companion prefabs in the SAME ORDER as your CompanionSelectionManager:
     - Index 0: First companion in your CompanionSelectionManager
     - Index 1: Second companion in your CompanionSelectionManager  
     - Index 2: Third companion in your CompanionSelectionManager
     - Index 3: Fourth companion in your CompanionSelectionManager
   - **Auto Spawn Settings**:
     - Spawn Radius: 3 (how far from player to spawn companions)
     - Ground Layer Mask: Default (what counts as ground)

### Step 2: Update CompanionSelectionManager
1. Find your existing CompanionSelectionManager in the scene
2. In the inspector, assign:
   - **Equip Companion Button**: Button in the stats panel for equipping

### Step 3: Setup Game Scene
1. **Create Spawn Points** (Optional):
   - Create 4 empty GameObjects in your game scene
   - Name them exactly: `CompanionSpawnPoint1`, `CompanionSpawnPoint2`, `CompanionSpawnPoint3`, `CompanionSpawnPoint4`
   - Position them where you want companions to appear
   
2. **Add Game Spawner**:
   - Create an empty GameObject and add the `GameCompanionSpawner` component
   - Assign companion prefabs in the SAME ORDER as your menu
   - Optionally assign player transform (or ensure player has "Player" tag for fallback)

### Step 4: Connect Game Start
1. **Add Game Start Trigger**:
   - Add `GameStartTrigger` component to your "Start Game" button or similar
   - Call `TriggerGameStart()` when player starts the game
   - This saves selection data and clears the menu UI

### Spawn Priority:
1. **First Priority**: Named spawn points (`CompanionSpawnPoint1-4`)
2. **Fallback**: Circle around player (if spawn points not found)

### Step 4: UI Setup
1. **Selection Panel (Always Visible)**:
   - Create 4 slots, each containing:
     - Background Image (always visible)
     - Companion Image (shows companion sprite when equipped, clickable to unequip)
     - Level Text (TextMeshPro showing "Lvl X")

2. **Stats Panel**:
   - Add an "Equip" button to your existing stats panel

**Note**: The companion images will automatically get Button components added at runtime for click functionality.

## How It Works

### Equipping Companions
1. Player clicks on a companion in the selection area
2. Stats panel opens showing companion details
3. Player clicks "Equip" button
4. Companion appears in the first available slot in the selection panel

### Unequipping Companions
1. Player clicks directly on an equipped companion image in the selection panel
2. Companion is removed from that slot
3. Slot shows empty (just background)
4. Empty slots are not clickable

### Visual States
- **Empty Slot**: Shows background image only
- **Equipped Slot**: Shows background + companion sprite
- **Button States**:
  - "Equip": Available to equip (enabled)
  - "Equipped": Already equipped (disabled)
  - "Slots Full": No slots available (disabled)

### Game Integration
```csharp
// In your game scene, spawn selected companions:
List<GameObject> companions = PersistentCompanionSelectionManager.Instance.SpawnSelectedCompanions(spawnParent);
```

## Companion Prefab Mapping
The system uses **direct index mapping** - much simpler and more reliable:
- **Companion 0** (first in CompanionSelectionManager) → **Prefab 0** (first in array)
- **Companion 1** (second in CompanionSelectionManager) → **Prefab 1** (second in array)
- **Companion 2** (third in CompanionSelectionManager) → **Prefab 2** (third in array)
- **Companion 3** (fourth in CompanionSelectionManager) → **Prefab 3** (fourth in array)

**No name matching required!** Just make sure the prefab array order matches your companion order.

## Cooldown System
- **Medic**: 30 minutes cooldown
- **Loyal**: 1 hour cooldown  
- **Tank**: 2 hours cooldown
- **Aggressive**: 3 hours cooldown

### Cooldown Behavior:
- Cooldowns only start when companions are actually spawned in the game
- **Persistent Cooldowns**: When you close the game, cooldown progress is saved using real-world time
- **Smart Resume**: When you reopen the game, the system calculates how much real-world time passed and updates cooldowns accordingly
- **Automatic Completion**: If enough real-world time passed while the game was closed, cooldowns will be finished when you return

### Technical Details:
- Uses `DateTime` and `PlayerPrefs` to track real-world time
- Saves cooldown data when game closes, loses focus, or every 10 seconds during play
- Loads and calculates elapsed time when game starts or regains focus

## Scene Architecture (Improved!)
- **Menu Scene**: PersistentCompanionSelectionManager handles UI and selection
- **Game Scene**: GameCompanionSpawner reads selection from PlayerPrefs and spawns companions
- **Data Transfer**: Uses PlayerPrefs instead of DontDestroyOnLoad (much cleaner!)
- **Cooldowns**: Started in game scene, saved to PlayerPrefs, loaded back in menu

## Features
- ✅ Up to 4 companions can be equipped
- ✅ Visual feedback for slot states (including cooldown status)
- ✅ Level display in selection slots
- ✅ Click to unequip functionality
- ✅ Cooldown prevention (can't equip companions on cooldown)
- ✅ Button state management with cooldown awareness
- ✅ Persistent across scenes
- ✅ Automatic spawning around player (no manual spawn points needed)
- ✅ Proper cooldown timing (starts when entering game)
- ✅ **Real-world cooldown persistence** (continues even when game is closed)
- ✅ Smart cooldown resume based on elapsed real-world time
- ✅ Transfers current companion stats (as modified by player)

## Debug Tools
Add `CompanionCooldownDebugger` to any GameObject for testing:
- **F1**: Show current cooldown status
- **F2**: Clear all cooldowns
- **GUI Buttons**: Various debug functions
- **Context Menu**: Right-click component for debug options