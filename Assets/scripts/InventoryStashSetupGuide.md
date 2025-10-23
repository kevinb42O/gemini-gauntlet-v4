# Inventory & Stash Manager Setup Guide

## GameObject Placement by Scene

### **MENU SCENE**
Place these GameObjects in your menu scene:

1. **PersistentItemInventoryManager** 
   - Empty GameObject with `PersistentItemInventoryManager.cs` script
   - **CRITICAL**: This persists across ALL scenes automatically (DontDestroyOnLoad)
   - Only create this ONCE in the menu scene

2. **InventoryManager (Menu Context)**
   - GameObject with `InventoryManager.cs` script
   - Set `inventoryContext = Menu` in inspector
   - Assign all menu UI references (inventory panel, slots, etc.)

3. **StashManager**
   - GameObject with `StashManager.cs` script
   - Assign all stash UI references (stash panel, stash slots, gem slot)
   - **Auto-destroys if loaded in game scene**

### **GAME SCENE**
Place these GameObjects in your game scene:

1. **InventoryManager (Game Context)**
   - GameObject with `InventoryManager.cs` script  
   - Set `inventoryContext = Game` in inspector
   - Assign all game UI references (inventory panel, slots, etc.)
   - **Note**: PersistentItemInventoryManager will auto-assign missing references

## How Data Flows

### **Menu → Game Transition:**
1. PersistentItemInventoryManager (already exists, persisted from menu)
2. Game InventoryManager loads data from PersistentItemInventoryManager
3. Items appear in game inventory

### **Game → Menu Transition (via ExitZone):**
1. ExitZone saves game inventory to PersistentItemInventoryManager
2. Menu scene loads
3. Menu InventoryManager loads data from PersistentItemInventoryManager
4. Items appear in menu inventory
5. StashManager operates independently with its own save file

## Key Points

- **PersistentItemInventoryManager**: Only create ONCE in menu scene, persists everywhere
- **InventoryManager**: Create in BOTH scenes with different contexts
- **StashManager**: Only in menu scene, auto-destroys in game scenes
- **No singleton conflicts**: Each manager has its own role and scope

## Troubleshooting

If inventory is empty after scene transition:
1. Check that PersistentItemInventoryManager exists and persisted
2. Verify InventoryManager context settings are correct
3. Look for debug logs showing data transfer between managers
