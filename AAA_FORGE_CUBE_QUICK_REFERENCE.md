# 🔥 FORGE IN-GAME QUICK REFERENCE

## 📏 Scale Constants (CRITICAL!)
```csharp
Player Height: 320 units
Player Radius: 50 units
Scale Factor: 3.2x

FORGE Cube Size: 6.4 × 6.4 × 6.4 units
Interaction Range: 16 units
UI Offset Height: 6.4 units above cube
Auto-Close Distance: 9.6 units
```

## 🎯 What Gets Built
```
New Files:
├─ Assets/scripts/ForgeCube.cs           (250 lines)
├─ Assets/scripts/ForgeUIManager.cs      (150 lines)
├─ Assets/Editor/ForgeCubeTests.cs       (100 lines)
├─ Assets/Materials/ForgeCube_Glow.mat   (Orange glow)
└─ Assets/Prefabs/ForgeCube.prefab       (6.4u cube)

Modified:
└─ Assets/scripts/ForgeManager.cs        (+100 lines)
```

## 🔧 Key Components

### ForgeCube.cs
- E-key interaction (toggle)
- 16-unit interaction range (scaled)
- Cursor unlock/lock
- Disable shooting, keep movement
- Auto-close on walk away

### ForgeUIManager.cs
- Shows/hides FORGE Canvas
- Sets ForgeManager context
- Singleton pattern

### ForgeManager Enhancement
- ForgeContext enum (Menu/Game)
- SetContext() method
- TryAddToGameInventory() method
- Modified HandleOutputSlotDoubleClick()

## 🎮 Player Flow
```
1. Walk near cube (16u) → Glows + "Press E"
2. Press E → UI opens, cursor visible
3. Craft item → Same as menu
4. Double-click output → Item to inventory
5. Press E / Walk away → UI closes
6. Item persists to menu ✓
```

## 🧪 Testing Commands
```
Tools/FORGE/Test ForgeCube Interaction
Tools/FORGE/Teleport Player to ForgeCube
Tools/FORGE/Verify All Components
```

## ✅ Success Checklist
- [ ] ForgeCube glows when nearby
- [ ] E-key opens/closes UI
- [ ] Cursor unlocks when open
- [ ] Shooting disabled when open
- [ ] Movement works when open
- [ ] Auto-closes on walk away
- [ ] Items go to inventory
- [ ] Items persist to menu

## 🎨 Visual Specs
```
Color: Orange (#FF6600)
Emission: #FF9933 at 0.8 intensity
Metallic: 0.7
Glossiness: 0.8
Size: 6.4 × 6.4 × 6.4 units
```

## 🔗 Integration Points
```
ForgeCube
    ↓ (E-key)
ForgeUIManager
    ↓ (SetContext)
ForgeManager
    ↓ (TryAddToGameInventory)
InventoryManager
    ↓ (SaveInventoryData)
PersistentInventoryManager
    ↓
✅ Full Persistence
```

## ⚠️ Common Issues

**Issue:** Cube doesn't glow
**Fix:** Check ForgeCube_Glow material has emission enabled

**Issue:** E-key doesn't work
**Fix:** Check player is within 16 units (use gizmo)

**Issue:** Items don't persist
**Fix:** Verify PersistentInventoryManager exists in scene

**Issue:** Cursor doesn't unlock
**Fix:** Check PlayerShooterOrchestrator reference found

## 📊 Implementation Time
- Phase 1 (Scripts): 1 hour
- Phase 2 (Prefab): 30 mins
- Phase 3 (Testing): 30 mins
- **Total: 2 hours**

## 🚀 For AI Agent
**Full implementation plan:** AAA_FORGE_IN_GAME_IMPLEMENTATION_PLAN.md
**Target:** Claude Sonnet 4.5 with Unity MCP
**Mode:** Zero human intervention
**Output:** Complete working system + tests + documentation
