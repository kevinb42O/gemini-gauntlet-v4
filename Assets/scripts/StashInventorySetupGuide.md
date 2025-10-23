# Stash & Inventory System Setup Guide

## Critical Issues Fixed

### 1. Data Structure Mismatch ✅ FIXED
- **Problem**: InventoryManager used `SlotSaveData` while StashManager used `SlotData` - incompatible formats
- **Solution**: Unified both to use the same `SlotSaveData` structure with `itemName`, `itemPath`, and `itemCount`

### 2. Singleton Persistence ✅ FIXED  
- **Problem**: Managers were destroyed when switching between game and menu scenes
- **Solution**: Added `DontDestroyOnLoad()` to both InventoryManager and StashManager singletons

### 3. Missing Initialization ✅ FIXED
- **Problem**: StashManager's `InitializeStash()` wasn't being called automatically
- **Solution**: Added auto-initialization in menu scenes via `Start()` method

## Required GameObject Setup

### InventoryManager GameObject
```
GameObject: "InventoryManager" 
├── InventoryManager.cs script
├── Essential References:
│   ├── reviveSlot: ReviveSlotController
│   ├── gemSlot: UnifiedSlot (mark isGemSlot = true)
│   └── inventorySlotsParent: Transform (parent of all inventory slots)
├── Inventory UI:
│   └── inventoryUIPanel: GameObject (main inventory panel)
└── Settings:
    ├── useUniformGemSystem: true
    └── inventoryCapacity: 24
```

### StashManager GameObject  
```
GameObject: "StashManager"
├── StashManager.cs script
├── Stash Slots:
│   ├── stashGemSlot: UnifiedSlot (mark isGemSlot = true)
│   ├── stashSlot1-5: UnifiedSlot
├── Inventory Slots:
│   ├── inventoryGemSlot: UnifiedSlot (mark isGemSlot = true) 
│   └── inventorySlot1-11: UnifiedSlot
├── Audio:
│   ├── audioSource: AudioSource
│   ├── gemTransferSound: AudioClip
│   ├── transferSound: AudioClip
│   └── errorSound: AudioClip
└── Flying Gem Effect:
    ├── enableFlyingGemEffect: true
    └── gemFlightDuration: 0.3
```

## Inspector Configuration Checklist

### ✅ Both GameObjects Must:
1. **Exist in BOTH game and menu scenes** (or be persistent)
2. **Have all slot references properly assigned**
3. **Have UnifiedSlot components with correct isGemSlot flags**
4. **Be placed on separate GameObjects** (not combined)

### ✅ Critical Settings:
- **Gem Slots**: Set `isGemSlot = true` in inspector for gem slots only
- **Slot Indices**: Ensure slots have proper `slotIndex` values (0, 1, 2, etc.)
- **Parent References**: InventoryManager needs `inventorySlotsParent` assigned

## How It Works Now

### Menu Scene Flow:
1. **Scene Load**: Both managers persist from previous scene OR get created fresh
2. **Auto-Initialize**: StashManager detects menu scene and calls `InitializeStash()`
3. **Data Loading**: 
   - StashManager loads `stash_data.json`
   - InventoryManager loads `inventory_data.json`
4. **Gem Transfer**: Automatic transfer from inventory to stash with visual effects

### Game Scene Flow:
1. **Persistent Data**: Both managers retain data from menu scene
2. **Inventory Access**: Press TAB to open/close inventory
3. **Auto-Save**: All changes save immediately to JSON files

## Debugging Commands

### Check Manager Status:
```csharp
// In Unity Console
Debug.Log($"InventoryManager Instance: {InventoryManager.Instance != null}");
Debug.Log($"StashManager Instance: {StashManager.Instance != null}");
```

### Force Stash Initialization:
```csharp
// Call from Inspector or Console
StashManager.Instance?.InitializeStash();
```

### Test Gem Display:
```csharp
// Use StashManager's context menu: "Test Gem Display"
// Or call: StashManager.Instance?.TestGemDisplay();
```

## File Locations
- **Stash Data**: `%USERPROFILE%/AppData/LocalLow/[CompanyName]/[GameName]/stash_data.json`
- **Inventory Data**: `%USERPROFILE%/AppData/LocalLow/[CompanyName]/[GameName]/inventory_data.json`

## Common Issues & Solutions

### "Stash is empty on menu start"
- **Check**: StashManager GameObject exists and has script attached
- **Check**: `InitializeStash()` is being called (look for debug logs)
- **Check**: `stash_data.json` file exists and has valid data

### "Inventory doesn't transfer to menu"
- **Check**: InventoryManager has `DontDestroyOnLoad()` (should persist)
- **Check**: Both managers use unified `SlotSaveData` structure
- **Check**: `inventory_data.json` is being saved properly in game scene

### "Inspector references are null"
- **Check**: All slot references are assigned in both managers
- **Check**: UnifiedSlot components exist on referenced GameObjects
- **Check**: Gem slots have `isGemSlot = true` flag set

The system should now properly:
✅ Load stash data from JSON on menu start
✅ Transfer inventory items between game and menu scenes  
✅ Auto-save all changes immediately
✅ Handle gem transfers with visual effects
