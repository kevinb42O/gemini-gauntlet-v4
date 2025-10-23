# 🔥 FORGE IN-GAME IMPLEMENTATION - AAA+ SENIOR DEV PLAN

## 🎯 CRITICAL SCALE INFORMATION
**⚠️ WORLD SCALE SPECIFICATIONS:**
- **Player Height:** 320 units
- **Player Width:** 50 units radius
- **Scale Factor:** 3.2x standard Unity scale
- **ALL distances and sizes in this document MUST be scaled accordingly!**

## 📋 Executive Summary

**Objective:** Make the FORGE system accessible during gameplay via an interactable cube, maintaining the exact same functionality as the menu version while routing crafted items to the persistent inventory system.

**Complexity Assessment:** ⭐⭐ EASY (2/5 stars)
- Leverage existing, proven patterns (ShopCube, ChestInteractionSystem)
- ForgeManager already well-architected and context-independent
- PersistentInventoryManager integration already established
- Zero bloat, pure architectural extension

**Development Time:** 2-3 hours (with testing)

**Target Environment:** Unity Editor with Claude Sonnet 4.5 + MCP Unity Extension

---

## 🎯 Core Design Principles

### 1. **Zero Duplication** 
- ONE ForgeManager instance handles both menu and game contexts
- Same recipes, same UI, same logic - just different routing

### 2. **Context-Aware Routing**
- **Menu Context:** Crafted items → Stash/Inventory (existing behavior)
- **Game Context:** Crafted items → PersistentInventoryManager → Game Inventory

### 3. **Proven Pattern Reuse**
- Copy ShopCube interaction pattern 100%
- Copy ChestInteractionSystem cursor management
- Copy ForgeManager's smart item routing logic

### 4. **Player Experience**
- Press **E** to open FORGE UI in-game
- Full mouse control, movement enabled, shooting disabled
- Press **E** again (or walk away) to close
- Crafted items automatically go to inventory

---

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    FORGE SYSTEM FLOW                         │
└─────────────────────────────────────────────────────────────┘

MENU SCENE:
  Menu → Open Forge UI → Craft Item → Stash/Inventory
                             ↓
                      ForgeManager
                             ↓
                    TryReturnItemToInventoryOrStash()

GAME SCENE:
  Walk to Cube → Press E → Forge UI Opens → Craft Item → PersistentInventory
       ↓                         ↓                               ↓
  ForgeCube              ForgeManager                    InventoryManager
       ↓                         ↓                               ↓
  Interaction           Context Detection              SaveInventoryData()
```

---

## 📂 File Structure (Zero Bloat)

### New Files (2 files only):
```
Assets/scripts/
├── ForgeCube.cs                    // 250 lines - Interaction handler
└── ForgeUIManager.cs               // 150 lines - UI visibility controller

Assets/Prefabs/
└── ForgeCube.prefab                // Cube with glow, interaction range
```

### Modified Files (2 files only):
```
Assets/scripts/
├── ForgeManager.cs                 // Add 50 lines - Context detection
└── InventoryManager.cs             // Optional - May not need changes
```

**Total New Code:** ~400 lines (including comments and logging)

---

## 🔧 Implementation Details

### Component 1: ForgeCube.cs
**Purpose:** Handle player interaction and trigger FORGE UI

**Pattern:** 100% based on ShopCube.cs

**⚠️ SCALE ADJUSTMENTS:**
- Base interaction range: 5f × 3.2 = **16f units** (scaled for 320-unit player)
- Cube size: 2x2x2 × 3.2 = **6.4 × 6.4 × 6.4 units** (visible to giant player)
- UI offset: 2f × 3.2 = **6.4 units** above cube
- Auto-close distance: 3f × 3.2 = **9.6 units** (walk-away threshold)

```csharp
public class ForgeCube : MonoBehaviour
{
    [Header("Interaction - SCALED FOR 320-UNIT PLAYER")]
    [SerializeField] private float interactionRange = 16f; // 5f × 3.2 scale
    [SerializeField] private LayerMask playerLayer;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color glowColor = new Color(1f, 0.5f, 0f); // Orange
    [SerializeField] private bool glowWhenNearby = true;
    
    private Transform playerTransform;
    private bool isPlayerNearby = false;
    private bool isForgeUIActive = false;
    private Renderer cubeRenderer;
    private Material originalMaterial;
    private Material glowMaterial;
    private PlayerShooterOrchestrator playerShooter;
    
    // Core Methods:
    // - OnPlayerEnterRange() - Show "Press E to use FORGE"
    // - OnPlayerExitRange() - Hide prompt, close UI if open
    // - StartForgeInteraction() - Open UI, unlock cursor, disable shooting
    // - ExitForgeInteraction() - Close UI, lock cursor, enable shooting
    // - Update() - Distance check + E key input
}
```

**Key Features:**
- ✅ E-key toggle (open/close)
- ✅ Auto-close on walk away (3+ unit distance)
- ✅ Glow effect when nearby
- ✅ Cursor management (unlock when open)
- ✅ Disable shooting, keep movement
- ✅ DynamicPlayerFeedManager integration for "Press E" prompt

---

### Component 2: ForgeUIManager.cs
**Purpose:** Manage FORGE UI visibility in game context

```csharp
public class ForgeUIManager : MonoBehaviour
{
    public static ForgeUIManager Instance { get; private set; }
    
    [Header("UI References")]
    [SerializeField] private GameObject forgeUIPanel; // Assign your existing Forge Canvas
    
    private bool isForgeUIVisible = false;
    
    public void ShowForgeUI()
    {
        // Enable the existing Forge UI Canvas
        // No need to recreate anything!
        forgeUIPanel.SetActive(true);
        isForgeUIVisible = true;
        
        // Notify ForgeManager it's in game context
        if (ForgeManager.Instance != null)
        {
            ForgeManager.Instance.SetContext(ForgeContext.Game);
        }
    }
    
    public void HideForgeUI()
    {
        forgeUIPanel.SetActive(false);
        isForgeUIVisible = false;
    }
    
    public bool IsUIVisible() => isForgeUIVisible;
}
```

**Key Features:**
- ✅ Singleton pattern (DontDestroyOnLoad not needed - game-scene only)
- ✅ Simple show/hide of existing UI
- ✅ Context notification to ForgeManager

---

### Component 3: ForgeManager Enhancement
**Purpose:** Add context awareness and smart routing

**Changes Required:**
```csharp
public enum ForgeContext
{
    Menu,
    Game
}

public class ForgeManager : MonoBehaviour
{
    // ADD THIS:
    private ForgeContext currentContext = ForgeContext.Menu;
    
    // ADD THIS METHOD:
    public void SetContext(ForgeContext context)
    {
        currentContext = context;
        Debug.Log($"[ForgeManager] Context set to: {context}");
    }
    
    // MODIFY THIS METHOD:
    void HandleOutputSlotDoubleClick(UnifiedSlot slot)
    {
        if (slot.IsEmpty) return;
        
        Debug.Log($"FORGE: Double-click on output - Context: {currentContext}");
        
        // Route based on context
        if (currentContext == ForgeContext.Game)
        {
            // IN-GAME: Route to persistent inventory
            bool success = TryAddToGameInventory(slot.CurrentItem, slot.ItemCount);
            if (success)
            {
                slot.ClearSlot();
                UpdateForgeState();
            }
        }
        else
        {
            // MENU: Use existing logic (stash/inventory)
            if (slot.CurrentItem is BackpackItem backpackItem)
            {
                bool success = TryEquipOrStoreBackpack(backpackItem, slot.ItemCount);
                if (success) slot.ClearSlot();
            }
            else
            {
                bool success = TryReturnItemToInventoryOrStash(slot.CurrentItem, slot.ItemCount);
                if (success) slot.ClearSlot();
            }
            UpdateForgeState();
        }
    }
    
    // ADD THIS METHOD:
    private bool TryAddToGameInventory(ChestItemData item, int count)
    {
        // Get the game InventoryManager instance
        InventoryManager gameInventory = InventoryManager.Instance;
        if (gameInventory == null)
        {
            Debug.LogError("[ForgeManager] Cannot find InventoryManager in game!");
            return false;
        }
        
        // Try to add item to inventory
        bool success = gameInventory.TryAddItem(item, count, autoSave: false);
        
        if (success)
        {
            Debug.Log($"[ForgeManager] Added {item.itemName} x{count} to game inventory");
            
            // Save inventory data
            gameInventory.SaveInventoryData();
            
            // CRITICAL: Update PersistentItemInventoryManager
            if (PersistentItemInventoryManager.Instance != null)
            {
                PersistentItemInventoryManager.Instance.UpdateFromInventoryManager(gameInventory);
                PersistentItemInventoryManager.Instance.SaveInventoryData();
                Debug.Log("[ForgeManager] ✅ Updated PersistentInventoryManager");
            }
            else
            {
                Debug.LogWarning("[ForgeManager] ⚠️ PersistentInventoryManager not found!");
            }
            
            // User feedback
            if (DynamicPlayerFeedManager.Instance != null)
            {
                DynamicPlayerFeedManager.Instance.ShowCustomMessage(
                    $"Crafted: {item.itemName} x{count}",
                    Color.green,
                    item.itemIcon,
                    false,
                    2f
                );
            }
        }
        else
        {
            Debug.LogWarning($"[ForgeManager] Inventory full! Cannot add {item.itemName}");
            
            // User feedback
            if (DynamicPlayerFeedManager.Instance != null)
            {
                DynamicPlayerFeedManager.Instance.ShowCustomMessage(
                    "Inventory Full!",
                    Color.red,
                    null,
                    false,
                    2f
                );
            }
        }
        
        return success;
    }
}
```

**Changes Summary:**
- ✅ +30 lines for context enum and setter
- ✅ +50 lines for TryAddToGameInventory() method
- ✅ ~20 lines modification to HandleOutputSlotDoubleClick()
- ✅ Total: ~100 lines (with comments and logging)

---

## 🎮 Player Experience Flow

### Opening the FORGE In-Game:
```
1. Player walks near ForgeCube (16 units - scaled for 320u player)
   └─> Cube glows orange
   └─> Message: "Press E to use FORGE"

2. Player presses E
   └─> Cursor unlocked (visible)
   └─> Shooting disabled
   └─> Movement still enabled (can walk away to close)
   └─> FORGE UI opens (same as menu version)
   └─> ForgeManager.SetContext(ForgeContext.Game)

3. Player drags items into FORGE slots
   └─> Same as menu - works exactly the same

4. Valid recipe detected
   └─> Craft button appears
   └─> Player clicks craft

5. Processing animation (5 seconds)
   └─> Progress bar fills
   └─> Countdown timer

6. Item crafted!
   └─> Output slot has item
   └─> Player double-clicks output slot
   └─> TryAddToGameInventory() called
   └─> Item goes to inventory
   └─> PersistentInventoryManager updated
   └─> Green feedback: "Crafted: Backpack T2 x1"

7. Player presses E to close (or walks away)
   └─> UI closes
   └─> Cursor locked
   └─> Shooting enabled
   └─> ForgeManager.SetContext(ForgeContext.Menu) // Reset default
```

---

## 🔄 Data Flow Diagrams

### Menu Context (Existing Behavior):
```
ForgeManager → Craft Item → Output Slot
                    ↓
           Double-Click Output
                    ↓
        TryReturnItemToInventoryOrStash()
                    ↓
           ┌────────┴────────┐
           ↓                 ↓
    Stash System    Inventory System
```

### Game Context (New Behavior):
```
ForgeManager → Craft Item → Output Slot
                    ↓
           Double-Click Output
                    ↓
          TryAddToGameInventory()
                    ↓
           InventoryManager.TryAddItem()
                    ↓
           SaveInventoryData()
                    ↓
      PersistentInventoryManager.UpdateFromInventoryManager()
                    ↓
      PersistentInventoryManager.SaveInventoryData()
                    ↓
           ✅ Item Persisted!
```

---

## 🛡️ Bulletproofing Strategy

### 1. Null Safety
```csharp
// Every external call protected
if (ForgeManager.Instance != null)
if (InventoryManager.Instance != null)
if (PersistentItemInventoryManager.Instance != null)
if (DynamicPlayerFeedManager.Instance != null)
```

### 2. State Management
```csharp
// Always clean up state
void OnDestroy()
{
    // Reset context when destroyed
    if (ForgeManager.Instance != null)
    {
        ForgeManager.Instance.SetContext(ForgeContext.Menu);
    }
    
    // Re-enable shooting
    if (playerShooter != null)
    {
        playerShooter.enabled = true;
    }
}
```

### 3. Edge Cases Handled
- ✅ Player walks away during crafting → Auto-close, keep crafting
- ✅ Inventory full → Show "Inventory Full!" message, keep item in output slot
- ✅ Player dies while FORGE open → UI auto-closes on scene change
- ✅ ForgeCube destroyed → Cleanup properly, re-enable shooting
- ✅ Multiple ForgeCubes → Each has own interaction range, no conflicts

---

## 📊 Persistence Integration

### Crafted Items in Game:
```
ForgeCube Interaction
        ↓
ForgeManager (Game Context)
        ↓
TryAddToGameInventory()
        ↓
InventoryManager.TryAddItem()
        ↓
inventoryManager.SaveInventoryData()
        ↓
        ├─> Saves: inventory_data.json
        └─> Triggers: PersistentItemInventoryManager.UpdateFromInventoryManager()
                ↓
                Saves: persistent_inventory.json
```

**Result:** Crafted items survive:
- ✅ Scene transitions (game → game)
- ✅ Menu transitions (game → menu → game)
- ✅ Death/respawn (cleared if player dies, as per existing system)

---

## 🎨 Visual Design Recommendations

### ForgeCube Appearance:
```
Material: Metallic/Glowing
Base Color: Dark Orange (#FF6600)
Emission: Bright Orange (#FF9933) at 0.5 intensity

⚠️ SCALED FOR 320-UNIT PLAYER (3.2x scale factor):
Size: 6.4 × 6.4 × 6.4 units (2 × 3.2)
Glow Radius: 16 units interaction range (5 × 3.2)
UI Offset Height: 6.4 units above cube (2 × 3.2)
Collider: Box 6.4 × 6.4 × 6.4 (matches visual size)
```

### UI Canvas Strategy:

**Option A: World-Space Canvas** (Recommended for immersion)
```
Pros:
- Fits in game world naturally
- Looks like a "workbench" in 3D space
- Player can see FORGE while looking at cube
Cons:
- Requires camera distance adjustment
- May be harder to read at distance
```

**Option B: Screen-Space Overlay** (Recommended for usability)
```
Pros:
- Clear, readable UI
- Same as menu experience
- No camera issues
Cons:
- Less immersive
- Breaks 3D illusion slightly

👉 RECOMMENDATION: Use Screen-Space Overlay
   Reason: ForgeManager UI is complex with lots of slots
           Screen-space ensures perfect readability
```

---

## 🧪 Testing Checklist

### Basic Interaction:
- [ ] Walk near ForgeCube → Cube glows, message appears
- [ ] Press E → UI opens, cursor visible, shooting disabled
- [ ] Press E again → UI closes, cursor locked, shooting enabled
- [ ] Walk away from cube → UI auto-closes

### Crafting Flow:
- [ ] Drag items into FORGE input slots (1 item each)
- [ ] Valid recipe → Craft button appears
- [ ] Click craft → Progress bar animates for 5 seconds
- [ ] Item appears in output slot
- [ ] Double-click output → Item goes to inventory
- [ ] Check inventory → Item is there
- [ ] Exit to menu → Item still in inventory (persistence check)

### Edge Cases:
- [ ] Inventory full → Show "Inventory Full!" message
- [ ] Walk away during crafting → Crafting continues, UI closes
- [ ] Multiple ForgeCubes in scene → Each works independently
- [ ] Die while FORGE open → UI closes, shooting re-enabled
- [ ] Craft backpack → Goes to inventory or auto-equips if upgrade

### Context Switching:
- [ ] Craft item in menu → Goes to stash/inventory (old behavior)
- [ ] Craft item in game → Goes to game inventory (new behavior)
- [ ] Menu → Game → Menu → Items persist correctly

---

## 🚀 Implementation Order

### Phase 1: Core Components (1 hour)
1. Create `ForgeCube.cs` - Copy ShopCube pattern
2. Create `ForgeUIManager.cs` - Simple UI toggle
3. Create ForgeCube prefab with glow material
4. Test: E-key opens/closes UI, cursor management works

### Phase 2: ForgeManager Enhancement (1 hour)
1. Add `ForgeContext` enum
2. Add `SetContext()` method
3. Add `TryAddToGameInventory()` method
4. Modify `HandleOutputSlotDoubleClick()` for context routing
5. Test: Crafted items go to inventory in game context

### Phase 3: Polish & Testing (30-60 minutes)
1. Add DynamicPlayerFeedManager messages
2. Add null safety checks everywhere
3. Test all edge cases
4. Add glow effect and visual feedback
5. Final testing with inventory persistence

---

## 💡 Advanced Features (Optional)

### Future Enhancements:
```
1. Multiple FORGE stations with different recipes
   └─> ForgeStationType enum: Basic, Advanced, Legendary
   └─> Each cube unlocks different recipe sets

2. FORGE upgrade system
   └─> Spend gems to unlock faster crafting
   └─> Reduce craft time from 5s to 3s to 1s

3. Auto-craft mode
   └─> Hold E to auto-craft multiple items
   └─> Consumes ingredients from inventory directly

4. Recipe discovery system
   └─> First time crafting shows "New Recipe Discovered!"
   └─> Save discovered recipes to PlayerPrefs
```

---

## 🎯 Success Criteria

### Must Have:
✅ ForgeCube interactable in game world
✅ E-key opens/closes FORGE UI
✅ Crafted items route to game inventory
✅ Full persistence (game → menu → game)
✅ Cursor management (unlock when open)
✅ Shooting disabled, movement enabled
✅ Auto-close on walk away
✅ Zero bloat code

### Nice to Have:
✅ Visual glow effect
✅ "Press E" prompt
✅ "Crafted: Item x1" feedback
✅ "Inventory Full!" warning
✅ Progress bar animation

---

## 📝 Code Quality Standards

### Logging Strategy:
```csharp
// Always log context switches
Debug.Log($"[ForgeCube] Player entered range - showing prompt");
Debug.Log($"[ForgeManager] Context set to: {context}");
Debug.Log($"[ForgeManager] Crafted item routed to game inventory");
```

### Comment Strategy:
```csharp
// CRITICAL: Update PersistentInventoryManager for cross-scene persistence
// GAME CONTEXT: Route to game inventory (not stash)
// AUTO-CLOSE: Player walked too far from cube
```

### Naming Conventions:
```csharp
// Clear, descriptive names
TryAddToGameInventory() // Not AddItem()
HandleOutputSlotDoubleClick() // Not OnClick()
SetContext() // Not ChangeMode()
```

---

## 🔍 Architecture Benefits

### Why This Design is AAA+:

1. **Single Responsibility**
   - ForgeCube: Handles interaction only
   - ForgeUIManager: Handles visibility only
   - ForgeManager: Handles crafting only

2. **Open/Closed Principle**
   - ForgeManager extended, not modified
   - New context enum, old code untouched

3. **Dependency Inversion**
   - Components depend on interfaces (Instance patterns)
   - No hard coupling between systems

4. **Don't Repeat Yourself (DRY)**
   - ONE ForgeManager for all contexts
   - Reuse existing UI, recipes, logic

5. **Keep It Simple, Stupid (KISS)**
   - 400 lines total new code
   - Proven patterns from ShopCube
   - No complex state machines

---

## 📚 Related Systems

### Systems This Integrates With:
- ✅ InventoryManager - Add items, save data
- ✅ PersistentInventoryManager - Cross-scene persistence
- ✅ UnifiedSlot - Drag/drop item handling
- ✅ DynamicPlayerFeedManager - Player feedback messages
- ✅ PlayerShooterOrchestrator - Disable shooting during interaction
- ✅ Cursor management - Unlock/lock cursor states

### Systems This Does NOT Touch:
- ❌ Movement system - No changes needed
- ❌ Combat system - No changes needed (just disabled during interaction)
- ❌ Save/load system - Uses existing persistence
- ❌ Recipe system - No changes needed
- ❌ Audio system - Can add later if desired

---

## 🎓 Learning Points

### Key Patterns Used:
1. **Singleton Pattern** - ForgeManager, InventoryManager, PersistentInventoryManager
2. **Strategy Pattern** - Context-based routing (Menu vs Game)
3. **Observer Pattern** - E-key input monitoring
4. **Facade Pattern** - ForgeUIManager simplifies UI complexity

### Unity Best Practices:
- ✅ SerializeField for Inspector exposure
- ✅ Null checks before every external call
- ✅ OnDestroy cleanup for state reset
- ✅ Gizmos for debugging interaction ranges
- ✅ Layer masks for player detection

---

## 🏁 Conclusion

This implementation is:
- **Easy** - Proven patterns, minimal code
- **Clean** - Zero bloat, single responsibility
- **Robust** - Null-safe, edge-case handled
- **Scalable** - Easy to add more FORGE stations
- **Maintainable** - Clear naming, good logging

**Total Implementation Time:** 2-3 hours
**Total Lines of Code:** ~400 lines
**Files Modified:** 2 files
**Files Created:** 2 files

**Result:** Production-ready FORGE system accessible in-game with full persistence and zero compromises.

---

## 🚦 Ready to Execute?

When you give the green light, I'll:
1. Create ForgeCube.cs
2. Create ForgeUIManager.cs
3. Enhance ForgeManager.cs with context awareness
4. Provide prefab setup instructions
5. Provide testing script

**This is a bulletproof, senior-dev quality implementation. Zero bloat, maximum foundation.**

🔥 **Let's make this happen!**

---

---

# 🤖 AI AGENT IMPLEMENTATION INSTRUCTIONS

## 📐 CRITICAL SCALE SPECIFICATIONS

**⚠️ YOU ARE IMPLEMENTING IN A LARGE-SCALE WORLD:**

### World Scale Parameters:
```csharp
// MANDATORY SCALE CONSTANTS
const float WORLD_SCALE = 3.2f;
const float PLAYER_HEIGHT = 320f;  // Units
const float PLAYER_RADIUS = 50f;   // Units

// DERIVED INTERACTION DISTANCES (All pre-scaled)
const float FORGE_INTERACTION_RANGE = 16f;      // 5f × 3.2
const float FORGE_CUBE_SIZE = 6.4f;              // 2f × 3.2
const float FORGE_UI_OFFSET_HEIGHT = 6.4f;       // 2f × 3.2
const float FORGE_AUTOWALK_AWAY_DISTANCE = 9.6f; // 3f × 3.2
const float FORGE_GLOW_INTENSITY = 0.8f;         // Higher for visibility at scale
```

### Why Scale Matters:
- Standard Unity tutorials assume 2-unit tall player
- This world has **320-unit tall player** (160x larger!)
- **50-unit radius** player (25x wider!)
- ALL interaction distances MUST be scaled by **3.2x**
- UI elements must be readable at distance

---

## 🎯 YOUR MISSION (AI Agent)

You are an AI agent with access to Unity Editor through MCP extensions. Your task is to implement the FORGE in-game system following this plan with **ZERO human intervention**.

### Available Tools:
- Unity MCP Server (file creation, prefab manipulation, scene editing)
- Claude Sonnet 4.5 (code generation, problem solving)
- Full Unity Editor API access
- Git integration (commit after each phase)

### Success Criteria:
✅ All files created and properly referenced
✅ Prefab configured with correct scale
✅ Scene integration complete (ForgeCube placed in test area)
✅ Testing script generated and executed
✅ All edge cases handled
✅ Documentation updated with implementation notes

---

## 📝 IMPLEMENTATION CHECKLIST (For AI Agent)

### Phase 1: Core Files Creation

#### Step 1.1: Create ForgeCube.cs
**Location:** `Assets/scripts/ForgeCube.cs`
**Requirements:**
- Copy ShopCube pattern exactly
- Use **16f interaction range** (not 5f!)
- Use **6.4f cube size** for prefab reference
- Implement distance check: `Vector3.Distance(transform.position, playerTransform.position) <= 16f`
- E-key input: `Input.GetKeyDown(KeyCode.E)`
- Cursor management: Unlock on open, lock on close
- Auto-close: Distance > 16f
- Glow effect: Orange emission material
- Find player: `GameObject.FindGameObjectWithTag("Player")` OR `FindObjectOfType<AAAMovementController>()`
- Get components: `playerShooter = FindObjectOfType<PlayerShooterOrchestrator>()`

**Code Template:**
```csharp
using UnityEngine;

/// <summary>
/// FORGE Cube - In-game interactable station for crafting
/// SCALED FOR 320-UNIT PLAYER (3.2x scale factor applied)
/// Pattern: Based on ShopCube.cs interaction model
/// </summary>
public class ForgeCube : MonoBehaviour
{
    [Header("Interaction - SCALED FOR 320-UNIT PLAYER")]
    [Tooltip("Distance at which player can interact (scaled 5f × 3.2 = 16f)")]
    [SerializeField] private float interactionRange = 16f;
    
    [Header("Visual Feedback")]
    [Tooltip("Color for glow effect when player is nearby")]
    [SerializeField] private Color glowColor = new Color(1f, 0.5f, 0f); // Orange
    [SerializeField] private bool glowWhenNearby = true;
    
    private Transform playerTransform;
    private bool isPlayerNearby = false;
    private bool isForgeUIActive = false;
    private Renderer cubeRenderer;
    private Material originalMaterial;
    private Material glowMaterial;
    private PlayerShooterOrchestrator playerShooter;
    
    void Start()
    {
        // Find player using multiple methods
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = FindObjectOfType<AAAMovementController>()?.gameObject;
        }
        
        if (player != null)
        {
            playerTransform = player.transform;
            playerShooter = FindObjectOfType<PlayerShooterOrchestrator>();
            Debug.Log($"[ForgeCube] Found player: {player.name}");
        }
        else
        {
            Debug.LogError("[ForgeCube] ❌ CRITICAL: Could not find player! Interactions will not work.");
        }
        
        SetupVisuals();
    }
    
    void SetupVisuals()
    {
        cubeRenderer = GetComponent<Renderer>();
        if (cubeRenderer != null)
        {
            originalMaterial = cubeRenderer.material;
            
            if (glowWhenNearby)
            {
                glowMaterial = new Material(originalMaterial);
                glowMaterial.color = glowColor;
                glowMaterial.EnableKeyword("_EMISSION");
                glowMaterial.SetColor("_EmissionColor", glowColor * 0.8f); // Higher intensity for scale
            }
        }
    }
    
    void Update()
    {
        if (playerTransform == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        bool wasNearby = isPlayerNearby;
        isPlayerNearby = distanceToPlayer <= interactionRange;
        
        // Handle entering/leaving interaction range
        if (isPlayerNearby && !wasNearby)
        {
            OnPlayerEnterRange();
        }
        else if (!isPlayerNearby && wasNearby)
        {
            OnPlayerExitRange();
        }
        
        // Handle E-key input
        if (isPlayerNearby && !isForgeUIActive && Input.GetKeyDown(KeyCode.E))
        {
            OpenForgeUI();
        }
        else if (isForgeUIActive && Input.GetKeyDown(KeyCode.E))
        {
            CloseForgeUI();
        }
    }
    
    void OnPlayerEnterRange()
    {
        Debug.Log("[ForgeCube] Player entered interaction range");
        
        // Apply glow effect
        if (glowWhenNearby && cubeRenderer != null && glowMaterial != null)
        {
            cubeRenderer.material = glowMaterial;
        }
        
        // Show interaction prompt
        if (DynamicPlayerFeedManager.Instance != null)
        {
            DynamicPlayerFeedManager.Instance.ShowCustomMessage(
                "Press E to use FORGE",
                Color.white,
                null,
                false,
                2.0f
            );
        }
    }
    
    void OnPlayerExitRange()
    {
        Debug.Log("[ForgeCube] Player left interaction range");
        
        // Remove glow effect
        if (cubeRenderer != null && originalMaterial != null)
        {
            cubeRenderer.material = originalMaterial;
        }
        
        // Auto-close UI if open
        if (isForgeUIActive)
        {
            CloseForgeUI();
        }
    }
    
    void OpenForgeUI()
    {
        Debug.Log("[ForgeCube] Opening FORGE UI");
        isForgeUIActive = true;
        
        // Show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Disable shooting (keep movement)
        if (playerShooter != null)
        {
            playerShooter.enabled = false;
        }
        
        // Show FORGE UI
        if (ForgeUIManager.Instance != null)
        {
            ForgeUIManager.Instance.ShowForgeUI();
        }
        else
        {
            Debug.LogError("[ForgeCube] ❌ ForgeUIManager.Instance is null!");
        }
    }
    
    void CloseForgeUI()
    {
        Debug.Log("[ForgeCube] Closing FORGE UI");
        isForgeUIActive = false;
        
        // Hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Re-enable shooting
        if (playerShooter != null)
        {
            playerShooter.enabled = true;
        }
        
        // Hide FORGE UI
        if (ForgeUIManager.Instance != null)
        {
            ForgeUIManager.Instance.HideForgeUI();
        }
    }
    
    void OnDestroy()
    {
        // Cleanup: Re-enable shooting if destroyed while UI is open
        if (isForgeUIActive && playerShooter != null)
        {
            playerShooter.enabled = true;
        }
        
        // Reset cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw interaction range (scaled)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // Draw cube size reference
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 6.4f); // Scaled cube size
    }
}
```

**Validation:**
- [ ] File created at correct path
- [ ] All using statements present
- [ ] Scale constants correct (16f range, 6.4f size)
- [ ] Player finding logic matches ShopCube
- [ ] E-key input works
- [ ] Gizmos show correct scale in editor

---

#### Step 1.2: Create ForgeUIManager.cs
**Location:** `Assets/scripts/ForgeUIManager.cs`
**Requirements:**
- Singleton pattern
- Reference to FORGE Canvas (find by name or assign in inspector)
- ShowForgeUI() - Enable canvas, set context
- HideForgeUI() - Disable canvas
- No DontDestroyOnLoad (game-scene only)

**Code Template:**
```csharp
using UnityEngine;

/// <summary>
/// ForgeUIManager - Manages FORGE UI visibility in game context
/// Singleton pattern for easy access from ForgeCube
/// </summary>
public class ForgeUIManager : MonoBehaviour
{
    public static ForgeUIManager Instance { get; private set; }
    
    [Header("UI References")]
    [Tooltip("Assign the FORGE Canvas GameObject (find in scene hierarchy)")]
    [SerializeField] private GameObject forgeUIPanel;
    
    private bool isForgeUIVisible = false;
    
    void Awake()
    {
        // Singleton pattern (no DontDestroyOnLoad - game scene only)
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[ForgeUIManager] Instance created");
        }
        else
        {
            Debug.LogWarning("[ForgeUIManager] Duplicate instance found - destroying");
            Destroy(gameObject);
            return;
        }
        
        // Auto-find FORGE UI if not assigned
        if (forgeUIPanel == null)
        {
            // Try to find by name (adjust name to match your canvas)
            forgeUIPanel = GameObject.Find("ForgeCanvas");
            
            if (forgeUIPanel == null)
            {
                Debug.LogError("[ForgeUIManager] ❌ CRITICAL: forgeUIPanel not assigned and could not auto-find 'ForgeCanvas'!");
                Debug.LogError("[ForgeUIManager] Please assign the FORGE Canvas in the inspector or rename your canvas to 'ForgeCanvas'");
            }
            else
            {
                Debug.Log("[ForgeUIManager] Auto-found ForgeCanvas");
            }
        }
        
        // Ensure UI starts hidden
        if (forgeUIPanel != null)
        {
            forgeUIPanel.SetActive(false);
        }
    }
    
    public void ShowForgeUI()
    {
        if (forgeUIPanel == null)
        {
            Debug.LogError("[ForgeUIManager] ❌ Cannot show UI - forgeUIPanel is null!");
            return;
        }
        
        Debug.Log("[ForgeUIManager] Showing FORGE UI");
        isForgeUIVisible = true;
        forgeUIPanel.SetActive(true);
        
        // Notify ForgeManager of game context
        if (ForgeManager.Instance != null)
        {
            ForgeManager.Instance.SetContext(ForgeContext.Game);
            Debug.Log("[ForgeUIManager] Set ForgeManager context to Game");
        }
        else
        {
            Debug.LogWarning("[ForgeUIManager] ⚠️ ForgeManager.Instance is null - context not set");
        }
    }
    
    public void HideForgeUI()
    {
        if (forgeUIPanel == null)
        {
            Debug.LogWarning("[ForgeUIManager] Cannot hide UI - forgeUIPanel is null");
            return;
        }
        
        Debug.Log("[ForgeUIManager] Hiding FORGE UI");
        isForgeUIVisible = false;
        forgeUIPanel.SetActive(false);
        
        // Reset ForgeManager context to Menu (default)
        if (ForgeManager.Instance != null)
        {
            ForgeManager.Instance.SetContext(ForgeContext.Menu);
            Debug.Log("[ForgeUIManager] Reset ForgeManager context to Menu");
        }
    }
    
    public bool IsUIVisible()
    {
        return isForgeUIVisible;
    }
    
    void OnDestroy()
    {
        // Cleanup singleton reference
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
```

**Validation:**
- [ ] File created at correct path
- [ ] Singleton pattern implemented
- [ ] Auto-find ForgeCanvas logic present
- [ ] Context switching calls ForgeManager
- [ ] No DontDestroyOnLoad (intentional)

---

#### Step 1.3: Enhance ForgeManager.cs
**Location:** `Assets/scripts/ForgeManager.cs`
**Requirements:**
- Add ForgeContext enum BEFORE class definition
- Add SetContext() method
- Add TryAddToGameInventory() method
- Modify HandleOutputSlotDoubleClick()

**Code Changes:**

**CHANGE 1: Add enum before class** (Line ~15, before `public class ForgeManager`)
```csharp
/// <summary>
/// Context enum for ForgeManager to determine routing behavior
/// </summary>
public enum ForgeContext
{
    Menu,  // Route to stash/inventory (existing behavior)
    Game   // Route to game inventory + PersistentInventoryManager (new behavior)
}
```

**CHANGE 2: Add context field** (Inside class, around line ~100, after public fields)
```csharp
// Context tracking for routing behavior
private ForgeContext currentContext = ForgeContext.Menu;
```

**CHANGE 3: Add SetContext method** (Around line ~170, near Awake/Start)
```csharp
/// <summary>
/// Set the FORGE context for routing behavior
/// Called by ForgeUIManager when UI is shown/hidden in game
/// </summary>
public void SetContext(ForgeContext context)
{
    currentContext = context;
    Debug.Log($"[ForgeManager] Context set to: {context}");
}
```

**CHANGE 4: Add TryAddToGameInventory method** (Around line ~500, near TryReturnItemToInventoryOrStash)
```csharp
/// <summary>
/// Add crafted item to game inventory with full persistence
/// Used when FORGE is accessed in-game (not menu)
/// </summary>
private bool TryAddToGameInventory(ChestItemData item, int count)
{
    if (item == null)
    {
        Debug.LogError("[ForgeManager] TryAddToGameInventory called with null item!");
        return false;
    }
    
    // Get the game InventoryManager instance
    InventoryManager gameInventory = InventoryManager.Instance;
    if (gameInventory == null)
    {
        Debug.LogError("[ForgeManager] ❌ Cannot find InventoryManager in game scene!");
        return false;
    }
    
    // Try to add item to inventory (don't auto-save yet)
    bool success = gameInventory.TryAddItem(item, count, autoSave: false);
    
    if (success)
    {
        Debug.Log($"[ForgeManager] ✅ Added {item.itemName} x{count} to game inventory");
        
        // Save inventory data (this updates inventory_data.json)
        gameInventory.SaveInventoryData();
        Debug.Log("[ForgeManager] Saved inventory data");
        
        // CRITICAL: Update PersistentItemInventoryManager for cross-scene persistence
        if (PersistentItemInventoryManager.Instance != null)
        {
            PersistentItemInventoryManager.Instance.UpdateFromInventoryManager(gameInventory);
            PersistentItemInventoryManager.Instance.SaveInventoryData();
            Debug.Log("[ForgeManager] ✅ Updated PersistentInventoryManager - item will persist across scenes");
        }
        else
        {
            Debug.LogWarning("[ForgeManager] ⚠️ PersistentInventoryManager not found! Item may not persist to menu.");
        }
        
        // User feedback
        if (DynamicPlayerFeedManager.Instance != null)
        {
            DynamicPlayerFeedManager.Instance.ShowCustomMessage(
                $"Crafted: {item.itemName} x{count}",
                Color.green,
                item.itemIcon,
                false,
                2f
            );
        }
    }
    else
    {
        Debug.LogWarning($"[ForgeManager] ⚠️ Inventory full! Cannot add {item.itemName}");
        
        // User feedback
        if (DynamicPlayerFeedManager.Instance != null)
        {
            DynamicPlayerFeedManager.Instance.ShowCustomMessage(
                "Inventory Full!",
                Color.red,
                null,
                false,
                2f
            );
        }
    }
    
    return success;
}
```

**CHANGE 5: Modify HandleOutputSlotDoubleClick** (Find existing method around line ~350)
```csharp
/// <summary>
/// Handle double-click on output slot - smart routing for backpacks, context-aware routing
/// ENHANCED: Routes to game inventory when in Game context
/// </summary>
void HandleOutputSlotDoubleClick(UnifiedSlot slot)
{
    if (slot.IsEmpty) return;
    
    Debug.Log($"FORGE: Double-click on output slot - Item: {slot.CurrentItem.itemName}, Context: {currentContext}");
    
    bool success = false;
    
    // Route based on context
    if (currentContext == ForgeContext.Game)
    {
        // IN-GAME CONTEXT: Route to game inventory + persistence
        Debug.Log("[ForgeManager] Game context - routing to game inventory");
        success = TryAddToGameInventory(slot.CurrentItem, slot.ItemCount);
    }
    else
    {
        // MENU CONTEXT: Use existing logic (stash/inventory or backpack smart routing)
        Debug.Log("[ForgeManager] Menu context - using existing routing");
        
        // Check if this is a backpack item (smart routing)
        if (slot.CurrentItem is BackpackItem backpackItem)
        {
            success = TryEquipOrStoreBackpack(backpackItem, slot.ItemCount);
        }
        else
        {
            // Regular item - try to return to inventory first, then stash
            success = TryReturnItemToInventoryOrStash(slot.CurrentItem, slot.ItemCount);
        }
    }
    
    // Clear slot on success
    if (success)
    {
        slot.ClearSlot();
        UpdateForgeState();
        Debug.Log("[ForgeManager] ✅ Output item successfully routed and slot cleared");
    }
    else
    {
        Debug.LogWarning("[ForgeManager] ⚠️ Failed to route output item - keeping in output slot");
    }
}
```

**Validation:**
- [ ] ForgeContext enum added before class
- [ ] currentContext field added
- [ ] SetContext() method added
- [ ] TryAddToGameInventory() method added
- [ ] HandleOutputSlotDoubleClick() modified with context check
- [ ] All debug logs present
- [ ] No compilation errors

---

### Phase 2: Prefab Creation

#### Step 2.1: Create ForgeCube Prefab
**Instructions for AI Agent:**

1. **Create Cube GameObject:**
   ```csharp
   // In Unity scene
   GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
   cube.name = "ForgeCube";
   cube.transform.localScale = new Vector3(6.4f, 6.4f, 6.4f); // SCALED
   ```

2. **Add ForgeCube Component:**
   ```csharp
   ForgeCube forgeCubeScript = cube.AddComponent<ForgeCube>();
   ```

3. **Create Glow Material:**
   ```csharp
   Material glowMat = new Material(Shader.Find("Standard"));
   glowMat.name = "ForgeCube_Glow";
   glowMat.color = new Color(1f, 0.5f, 0f); // Orange
   glowMat.EnableKeyword("_EMISSION");
   glowMat.SetColor("_EmissionColor", new Color(1f, 0.5f, 0f) * 0.8f);
   glowMat.SetFloat("_Metallic", 0.7f);
   glowMat.SetFloat("_Glossiness", 0.8f);
   
   // Save to Assets/Materials/
   AssetDatabase.CreateAsset(glowMat, "Assets/Materials/ForgeCube_Glow.mat");
   ```

4. **Assign Material:**
   ```csharp
   cube.GetComponent<Renderer>().material = glowMat;
   ```

5. **Setup Collider:**
   ```csharp
   BoxCollider collider = cube.GetComponent<BoxCollider>();
   collider.isTrigger = false; // Solid collider
   collider.size = Vector3.one; // Already 6.4 due to scale
   ```

6. **Save as Prefab:**
   ```csharp
   PrefabUtility.SaveAsPrefabAsset(cube, "Assets/Prefabs/ForgeCube.prefab");
   ```

**Validation:**
- [ ] Prefab created at Assets/Prefabs/ForgeCube.prefab
- [ ] Scale set to (6.4, 6.4, 6.4)
- [ ] ForgeCube component attached
- [ ] Material has orange glow
- [ ] Collider configured

---

#### Step 2.2: Setup ForgeUIManager in Scene
**Instructions for AI Agent:**

1. **Create Manager GameObject:**
   ```csharp
   GameObject managerObj = new GameObject("ForgeUIManager");
   ForgeUIManager manager = managerObj.AddComponent<ForgeUIManager>();
   ```

2. **Find FORGE Canvas:**
   ```csharp
   // Try multiple naming patterns
   GameObject forgeCanvas = GameObject.Find("ForgeCanvas");
   if (forgeCanvas == null) forgeCanvas = GameObject.Find("Forge Canvas");
   if (forgeCanvas == null) forgeCanvas = GameObject.Find("ForgeUI");
   
   if (forgeCanvas == null)
   {
       Debug.LogError("⚠️ Could not auto-find FORGE Canvas! Manual assignment required.");
       Debug.LogError("Please find your FORGE Canvas in the hierarchy and assign it to ForgeUIManager.forgeUIPanel");
   }
   else
   {
       // Use SerializedObject to assign the field
       SerializedObject so = new SerializedObject(manager);
       so.FindProperty("forgeUIPanel").objectReferenceValue = forgeCanvas;
       so.ApplyModifiedProperties();
       Debug.Log($"✅ Assigned FORGE Canvas: {forgeCanvas.name}");
   }
   ```

3. **Ensure Canvas Starts Inactive:**
   ```csharp
   if (forgeCanvas != null)
   {
       forgeCanvas.SetActive(false);
   }
   ```

**Validation:**
- [ ] ForgeUIManager GameObject created
- [ ] forgeUIPanel assigned (or warning logged)
- [ ] Canvas starts inactive

---

### Phase 3: Scene Integration & Testing

#### Step 3.1: Place ForgeCube in Scene
**Instructions for AI Agent:**

1. **Instantiate Prefab:**
   ```csharp
   GameObject forgeCubeInstance = PrefabUtility.InstantiatePrefab(
       AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ForgeCube.prefab")
   ) as GameObject;
   ```

2. **Position Near Player Spawn:**
   ```csharp
   // Find player spawn or use origin
   GameObject player = GameObject.FindGameObjectWithTag("Player");
   Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;
   
   // Place 25 units in front of player (scaled: ~8 standard units)
   forgeCubeInstance.transform.position = playerPos + new Vector3(0, 0, 25f);
   
   Debug.Log($"✅ Placed ForgeCube at {forgeCubeInstance.transform.position}");
   ```

3. **Verify Gizmo Visibility:**
   ```csharp
   Selection.activeGameObject = forgeCubeInstance;
   SceneView.lastActiveSceneView.FrameSelected();
   ```

**Validation:**
- [ ] ForgeCube placed in scene
- [ ] Position is near player spawn
- [ ] Gizmo visible in editor (16-unit range sphere)

---

#### Step 3.2: Create Testing Script
**Location:** `Assets/Editor/ForgeCubeTests.cs`

```csharp
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor testing utilities for FORGE in-game system
/// </summary>
public static class ForgeCubeTests
{
    [MenuItem("Tools/FORGE/Test ForgeCube Interaction")]
    public static void TestForgeCubeInteraction()
    {
        // Find ForgeCube in scene
        ForgeCube forgeCube = Object.FindObjectOfType<ForgeCube>();
        if (forgeCube == null)
        {
            Debug.LogError("❌ No ForgeCube found in scene!");
            return;
        }
        
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("❌ No player found in scene!");
            return;
        }
        
        // Test 1: Distance check
        float distance = Vector3.Distance(forgeCube.transform.position, player.transform.position);
        Debug.Log($"✅ TEST 1: Distance to ForgeCube: {distance:F2} units (interaction range: 16 units)");
        
        if (distance <= 16f)
        {
            Debug.Log("✅ Player is within interaction range");
        }
        else
        {
            Debug.LogWarning($"⚠️ Player is too far! Move player within 16 units or teleport to ForgeCube.");
        }
        
        // Test 2: Component references
        Debug.Log("✅ TEST 2: Component references:");
        Debug.Log($"  - ForgeCube script: {forgeCube != null}");
        Debug.Log($"  - ForgeUIManager: {ForgeUIManager.Instance != null}");
        Debug.Log($"  - ForgeManager: {ForgeManager.Instance != null}");
        Debug.Log($"  - InventoryManager: {InventoryManager.Instance != null}");
        Debug.Log($"  - PersistentInventoryManager: {PersistentItemInventoryManager.Instance != null}");
        
        // Test 3: Material check
        Renderer renderer = forgeCube.GetComponent<Renderer>();
        if (renderer != null)
        {
            Debug.Log($"✅ TEST 3: Material assigned: {renderer.sharedMaterial != null}");
            if (renderer.sharedMaterial != null)
            {
                Debug.Log($"  - Material name: {renderer.sharedMaterial.name}");
                Debug.Log($"  - Has emission: {renderer.sharedMaterial.IsKeywordEnabled("_EMISSION")}");
            }
        }
        
        Debug.Log("✅ TESTING COMPLETE - Check logs above for any warnings");
    }
    
    [MenuItem("Tools/FORGE/Teleport Player to ForgeCube")]
    public static void TeleportPlayerToForgeCube()
    {
        ForgeCube forgeCube = Object.FindObjectOfType<ForgeCube>();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        if (forgeCube == null || player == null)
        {
            Debug.LogError("❌ Could not find ForgeCube or Player!");
            return;
        }
        
        // Teleport player 10 units in front of cube (within interaction range)
        player.transform.position = forgeCube.transform.position + new Vector3(0, 0, -10f);
        
        Debug.Log($"✅ Teleported player to {player.transform.position}");
        Debug.Log($"   Distance to ForgeCube: {Vector3.Distance(player.transform.position, forgeCube.transform.position):F2} units");
    }
    
    [MenuItem("Tools/FORGE/Verify All Components")]
    public static void VerifyAllComponents()
    {
        Debug.Log("=== FORGE SYSTEM VERIFICATION ===");
        
        // Check scripts exist
        bool forgeCubeExists = System.IO.File.Exists("Assets/scripts/ForgeCube.cs");
        bool forgeUIManagerExists = System.IO.File.Exists("Assets/scripts/ForgeUIManager.cs");
        
        Debug.Log($"✅ ForgeCube.cs exists: {forgeCubeExists}");
        Debug.Log($"✅ ForgeUIManager.cs exists: {forgeUIManagerExists}");
        
        // Check ForgeManager has new methods (check compilation)
        bool forgeManagerCompiled = typeof(ForgeManager).GetMethod("SetContext") != null;
        Debug.Log($"✅ ForgeManager.SetContext() exists: {forgeManagerCompiled}");
        
        // Check prefab exists
        bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ForgeCube.prefab") != null;
        Debug.Log($"✅ ForgeCube prefab exists: {prefabExists}");
        
        // Check scene instances
        ForgeCube[] forgeCubes = Object.FindObjectsOfType<ForgeCube>();
        ForgeUIManager[] forgeUIManagers = Object.FindObjectsOfType<ForgeUIManager>();
        
        Debug.Log($"✅ ForgeCube instances in scene: {forgeCubes.Length}");
        Debug.Log($"✅ ForgeUIManager instances in scene: {forgeUIManagers.Length}");
        
        if (forgeCubes.Length == 0)
        {
            Debug.LogWarning("⚠️ No ForgeCube in scene! Place the prefab in your game scene.");
        }
        
        if (forgeUIManagers.Length == 0)
        {
            Debug.LogWarning("⚠️ No ForgeUIManager in scene! Create GameObject with ForgeUIManager component.");
        }
        
        Debug.Log("=== VERIFICATION COMPLETE ===");
    }
}
```

**Validation:**
- [ ] File created at Assets/Editor/ForgeCubeTests.cs
- [ ] Menu items appear under Tools/FORGE/
- [ ] All tests run without errors

---

## ⚡ EXECUTION PROTOCOL (For AI Agent)

### Pre-Flight Checklist:
```
✅ Confirm Unity Editor is open
✅ Confirm project path: c:\Users\kevin\Desktop\Game Project\BACK UP\Gemini Gauntlet - V4.0
✅ Confirm scene loaded (check for Player, FORGE Canvas, etc.)
✅ Confirm write permissions to Assets folder
✅ Confirm Git is available for committing
```

### Execution Sequence:
```
1. CREATE ForgeCube.cs
   └─> Save to Assets/scripts/
   └─> Verify compilation (no errors)

2. CREATE ForgeUIManager.cs
   └─> Save to Assets/scripts/
   └─> Verify compilation (no errors)

3. MODIFY ForgeManager.cs
   └─> Add enum, field, methods
   └─> Verify compilation (no errors)

4. CREATE ForgeCube Prefab
   └─> Create cube primitive
   └─> Scale to 6.4 × 6.4 × 6.4
   └─> Create glow material
   └─> Save prefab

5. SETUP ForgeUIManager
   └─> Create GameObject in scene
   └─> Auto-find FORGE Canvas
   └─> Link references

6. PLACE ForgeCube
   └─> Instantiate prefab in scene
   └─> Position near player spawn

7. CREATE Testing Script
   └─> Save to Assets/Editor/
   └─> Run verification tests

8. GIT COMMIT
   └─> Commit all changes
   └─> Message: "feat: Add FORGE in-game system with ForgeCube interactable"

9. GENERATE REPORT
   └─> Create AAA_FORGE_IMPLEMENTATION_REPORT.md
   └─> Document what was done
   └─> List any warnings/issues
```

### Error Handling:
```csharp
// If any step fails:
try {
    // Execute step
} catch (Exception e) {
    Debug.LogError($"❌ STEP FAILED: {e.Message}");
    // Log to AAA_FORGE_IMPLEMENTATION_ERRORS.md
    // Continue with remaining steps
}
```

---

## 🔍 POST-IMPLEMENTATION VERIFICATION

### Automated Checks (Run via Testing Script):
- [ ] All scripts compiled successfully
- [ ] ForgeContext enum exists
- [ ] ForgeManager.SetContext() method exists
- [ ] ForgeCube prefab exists at correct path
- [ ] ForgeCube placed in scene
- [ ] ForgeUIManager in scene with canvas assigned
- [ ] Gizmo visible in scene view (16-unit range)

### Manual Testing Checklist (For Human):
- [ ] Enter Play Mode
- [ ] Walk near ForgeCube (within 16 units)
- [ ] Cube glows orange
- [ ] Message appears: "Press E to use FORGE"
- [ ] Press E → UI opens, cursor visible
- [ ] Drag items to FORGE input slots
- [ ] Valid recipe → Craft button appears
- [ ] Click craft → Progress bar fills
- [ ] Double-click output → Item goes to inventory
- [ ] Press E → UI closes, cursor locks
- [ ] Walk away → UI auto-closes
- [ ] Exit to menu → Item persists in menu inventory
- [ ] Return to game → Item still in game inventory

---

## 📊 EXPECTED OUTPUT FILES

After AI agent execution, you should have:

### New Files Created:
```
Assets/scripts/ForgeCube.cs                    (✓ 250 lines)
Assets/scripts/ForgeUIManager.cs               (✓ 150 lines)
Assets/Editor/ForgeCubeTests.cs                (✓ 100 lines)
Assets/Materials/ForgeCube_Glow.mat            (✓ Material asset)
Assets/Prefabs/ForgeCube.prefab                (✓ Prefab asset)
AAA_FORGE_IMPLEMENTATION_REPORT.md             (✓ Implementation log)
```

### Modified Files:
```
Assets/scripts/ForgeManager.cs                 (+ 100 lines, ~1100 total)
[Current Scene].unity                          (+ ForgeCube + ForgeUIManager)
```

### Git Commits:
```
feat: Add FORGE in-game system with ForgeCube interactable
- Created ForgeCube.cs for E-key interaction
- Created ForgeUIManager.cs for UI visibility
- Enhanced ForgeManager.cs with context awareness
- Added ForgeCube prefab with correct scale (6.4u)
- Placed ForgeCube in game scene
- Added editor testing utilities
```

---

## 🎓 CRITICAL REMINDERS FOR AI AGENT

### Scale Awareness:
```csharp
// ❌ WRONG (Standard Unity scale)
float interactionRange = 5f;
Vector3 cubeSize = Vector3.one * 2f;

// ✅ CORRECT (Scaled for 320-unit player)
float interactionRange = 16f;    // 5f × 3.2
Vector3 cubeSize = Vector3.one * 6.4f;  // 2f × 3.2
```

### Context Routing:
```csharp
// ❌ WRONG (Always uses menu behavior)
TryReturnItemToInventoryOrStash(item, count);

// ✅ CORRECT (Context-aware)
if (currentContext == ForgeContext.Game)
    TryAddToGameInventory(item, count);
else
    TryReturnItemToInventoryOrStash(item, count);
```

### Persistence Chain:
```csharp
// ❌ INCOMPLETE (Doesn't persist to menu)
inventoryManager.TryAddItem(item, count);
inventoryManager.SaveInventoryData();

// ✅ COMPLETE (Full persistence)
inventoryManager.TryAddItem(item, count, autoSave: false);
inventoryManager.SaveInventoryData();
PersistentItemInventoryManager.Instance.UpdateFromInventoryManager(inventoryManager);
PersistentItemInventoryManager.Instance.SaveInventoryData();
```

---

## 🚀 FINAL WORD TO AI AGENT

You have:
- ✅ Complete implementation plan
- ✅ All code templates
- ✅ Correct scale constants
- ✅ Testing utilities
- ✅ Error handling protocols

**Your mission:** Implement this system with ZERO human intervention.

**Success means:**
- All files created and compiled
- All components placed in scene
- Testing script runs without errors
- Git commit made with proper message
- Implementation report generated

**If you encounter issues:**
- Log them clearly
- Continue with remaining steps
- Document in error log
- Provide remediation suggestions

**You are equipped with everything you need. Execute with confidence! 🔥**

