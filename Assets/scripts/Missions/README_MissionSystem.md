# 🎯 Mission System Implementation Guide

## Overview
This comprehensive mission system allows players to select and complete missions for XP and gem rewards. The system supports 5 mission types, tier progression, and seamless integration with existing game systems.

## 🚀 Quick Setup

### 1. Add MissionManager to Scene
1. Create an empty GameObject named "MissionManager"
2. Add the `MissionManager` component
3. Assign your mission assets to the `allMissions` array
4. Configure audio clips and settings

### 2. Create Mission Assets
1. Right-click in Project → Create → Gemini Gauntlet → Mission
2. Configure mission properties in Inspector
3. Use the `CreateDemoMissions` script to generate starter missions

### 3. Setup UI Components
1. Add `EquippedMissionsUI` to your main menu
2. Add `MissionSelectionUI` to your mission selection canvas
3. Configure UI prefabs and references

### 4. Add Progress Tracking
Add the appropriate tracking components to your game objects:
- `MissionEnemyTracker` on enemy prefabs
- `MissionChestTracker` on chest objects
- `MissionPlatformTracker` on platform objects
- `MissionCollectibleTracker` on collectible items
- `MissionForgeIntegration` for crafting missions

## 📋 Mission Types

### Kill Missions
- **Type**: `MissionType.Kill`
- **Target Specifier**: Enemy type (e.g., "skull", "goblin")
- **Tracking**: `MissionProgressHooks.OnEnemyKilled(enemyType)`

### Conquer Missions
- **Type**: `MissionType.Conquer`
- **Target Specifier**: Not used
- **Tracking**: `MissionProgressHooks.OnPlatformConquered()`

### Loot Missions
- **Type**: `MissionType.Loot`
- **Target Specifier**: Not used
- **Tracking**: `MissionProgressHooks.OnChestLooted()`

### Collect Missions
- **Type**: `MissionType.Collect`
- **Target Specifier**: Item name (e.g., "gem", "crystal")
- **Tracking**: `MissionProgressHooks.OnItemCollected(itemName)`

### Craft Missions
- **Type**: `MissionType.Craft`
- **Target Specifier**: Item name to craft
- **Tracking**: `MissionProgressHooks.OnItemCrafted(itemName)`

## 🔧 Integration Points

### XP System Integration
The mission system automatically integrates with your existing XP system:
- Mission completion XP appears as "Missions Completed" category in XP summary
- Session mission XP is added to total session XP
- Mission session data is cleared after XP summary is shown

### Gem Reward Integration
Gem rewards are automatically added to the player's inventory via StashManager:
- Gems are added to inventory gem slot first
- If full, gems go to stash gem slot
- Animated gem counting is handled in XP summary UI

### FORGE System Integration
For craft missions, add `MissionForgeIntegration` component:
- Monitors FORGE output slot for new items
- Automatically triggers mission progress when items are crafted
- Can be manually triggered for custom crafting systems

## 🎮 Player Flow

### Mission Selection
1. Player clicks "MISSIONS" button in main menu
2. Camera navigates to mission selection canvas
3. Player sees missions organized by tier
4. Player can accept missions (max 3 equipped)

### Mission Progress
1. Equipped missions appear in bottom-right of main menu
2. Real-time progress updates as player plays
3. Visual feedback when missions complete
4. Mission completion audio cues

### Reward Claiming
1. Completed missions show "Claim Rewards" button
2. Player clicks to claim XP and gem rewards
3. Mission XP appears in exit zone XP summary
4. Gem rewards are animated and added to inventory

## ⚙️ Configuration

### Mission ScriptableObject Properties
```csharp
// Basic Info
string missionID;           // Unique identifier
string missionName;         // Display name
string missionDescription;  // Description text
Sprite missionIcon;         // Mission icon

// Mission Type
MissionType missionType;    // Kill, Conquer, Loot, Collect, Craft
MissionTier tier;           // Tier1, Tier2, Tier3
int targetCount;            // How many to complete
string targetSpecifier;     // Specific target (enemy type, item name)

// Progress
bool persistProgressOnDeath; // Keep progress if player dies

// Rewards
int xpReward;              // XP given on completion
int gemReward;             // Gems given on completion
ChestItemData[] itemRewards; // Optional item rewards

// UI Text
string progressTemplate;    // "{current}/{target}" format
string completionText;      // Shown when complete
```

### MissionManager Settings
```csharp
Mission[] allMissions;          // All available missions
int maxEquippedMissions = 3;    // Max equipped at once
AudioClip missionProgressSound; // Progress update audio
AudioClip missionCompleteSound; // Completion audio
bool enableDebugLogs;           // Debug logging
```

## 🔄 Tier System

### Tier Progression
- **Tier 1**: Available from start (3 starter missions)
- **Tier 2**: Unlocked when ALL Tier 1 missions are completed
- **Tier 3**: Unlocked when ALL Tier 2 missions are completed

### Tier Configuration
Missions are assigned to tiers via the `tier` property in the ScriptableObject.

## 📱 UI Components

### EquippedMissionsUI
Shows currently equipped missions in main menu:
- Mission name and description
- Progress bar and text
- "Unequip" or "Claim Rewards" buttons
- Real-time progress updates

### MissionSelectionUI
Full mission browser with tier organization:
- Tier sections with lock/unlock status
- Mission cards with details and rewards
- Accept/Unequip mission buttons
- Visual indicators for equipped/completed missions

## 🐛 Debugging

### Debug Methods
```csharp
// MissionManager debug methods
DEBUG_ResetAllMissionData()  // Reset all progress
DEBUG_GrantXP()              // Test XP system

// UI debug methods
DEBUG_RefreshEquippedMissions()    // Refresh equipped UI
DEBUG_RefreshMissionSelection()    // Refresh selection UI
```

### Debug Logs
Enable `enableDebugLogs` in MissionManager for detailed logging of:
- Mission progress updates
- Mission completions
- Tier unlocking
- Reward claiming

## 🔌 Code Integration Examples

### Adding Enemy Kill Tracking
```csharp
// In your enemy death script:
using GeminiGauntlet.Missions.Integration;

public class Enemy : MonoBehaviour
{
    public string enemyType = "skull";
    
    void OnDeath()
    {
        // Your existing death logic...
        
        // Add mission tracking
        MissionProgressHooks.OnEnemyKilled(enemyType);
    }
}
```

### Adding Chest Loot Tracking
```csharp
// In your chest interaction script:
using GeminiGauntlet.Missions.Integration;

public class Chest : MonoBehaviour
{
    void OnLooted()
    {
        // Your existing loot logic...
        
        // Add mission tracking
        MissionProgressHooks.OnChestLooted();
    }
}
```

### Adding Platform Conquest Tracking
```csharp
// In your platform control script:
using GeminiGauntlet.Missions.Integration;

public class Platform : MonoBehaviour
{
    void OnConquered()
    {
        // Your existing conquest logic...
        
        // Add mission tracking
        MissionProgressHooks.OnPlatformConquered();
    }
}
```

## 🎨 UI Customization

### Mission Slot Prefab Requirements
Your mission slot prefab should have:
- `Image missionIcon`
- `TextMeshProUGUI missionNameText`
- `TextMeshProUGUI missionDescriptionText`
- `Slider progressBar`
- `TextMeshProUGUI progressText`
- `GameObject completedIndicator`
- `Button unequipButton`

### Mission Card Prefab Requirements
Your mission card prefab should have:
- `Image missionIcon`
- `TextMeshProUGUI missionNameText`
- `TextMeshProUGUI missionDescriptionText`
- `TextMeshProUGUI rewardsText`
- `Button acceptButton`
- `GameObject equippedIndicator`
- `GameObject completedIndicator`

## 🚨 Important Notes

1. **Singleton Pattern**: MissionManager uses DontDestroyOnLoad for cross-scene persistence
2. **Save System**: Mission progress is automatically saved to JSON file
3. **XP Integration**: Mission XP is added to existing XP categories, not replaced
4. **Gem Integration**: Requires existing StashManager and gem system
5. **Audio Requirements**: Uses existing AudioManager for sound playback
6. **Performance**: Mission progress tracking is lightweight and efficient

## 🔄 Extending the System

### Adding New Mission Types
1. Add new type to `MissionType` enum
2. Add tracking method to `MissionProgressHooks`
3. Add progress method to `MissionManager`
4. Update display name logic in UI components

### Custom Reward Types
1. Extend Mission ScriptableObject with new reward fields
2. Modify `ClaimMissionRewards()` in MissionManager
3. Add integration with your custom systems

### Advanced Mission Logic
1. Create custom Mission subclass
2. Override `IsComplete()` and `GetProgressText()` methods
3. Add custom tracking logic as needed

This mission system is designed to be expandable and maintainable while integrating seamlessly with your existing game systems. Happy mission creating! 🎯