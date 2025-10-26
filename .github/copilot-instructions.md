# Gemini Gauntlet - AI Coding Instructions

This is **Gemini Gauntlet V4.0**, a Unity 3D action-adventure game featuring dual-hand combat, aerial tricks, companion AI, and procedural platforms. Built with Unity 2023.3 and URP.

## 📚 Essential Documentation (Current Systems Only)

### 🚀 Quick Start Guides (Always Use These)
- **`AAA_MASTER_SETUP_INDEX.md`** - Central hub for all setup guides and current systems
- **`Assets/scripts/Audio/AudioSystemSetupGuide.md`** - Modern audio system (5-minute setup)
- **`AAA_AERIAL_TRICK_MASTER_INDEX.md`** - Complete aerial trick system documentation suite

### 🎮 Core Systems Documentation (Latest)
- **`AAA_AUDIO_SYSTEM_COMPLETE_FIX.md`** - Fixed audio architecture (Oct 2025)
- **`AAA_CRITICAL_FIXES_COMPLETE.md`** - All critical movement/physics fixes applied
- **`AAA_BLEEDING_OUT_DELUXE_COMPLETE.md`** - Advanced dying state system
- **`AAA_FALLING_DAMAGE_SYSTEM_COMPLETE.md`** - Physics-based damage system
- **`AAA_WALLHACK_COMPLETE_PACKAGE.md`** - ESP/cheat system implementation (deprecated but included)

### 🎯 Latest Feature Systems (Oct 2025)
- **`KEYCARD_SYSTEM_COMPLETE.md`** - Door access system with 5 keycard types
- **`AAA_SWORD_MODE_IMPLEMENTATION_COMPLETE.md`** - Melee combat system
- **`AAA_EQUIPPABLE_SWORD_MASTER_INDEX.md`** - Equipment-gated weapon system with inventory integration
- **`IMPLEMENTATION_COMPLETE_SUMMARY.md`** - Latest equippable sword implementation details
- **`AAA_COMPLETE_AUDIO_COMBO_SYSTEM.md`** - XP combo multiplier with audio
- **`ELEVATOR_SYSTEM_SUMMARY.md`** - Elevator keycard mechanics
- **`AAA_ORBITAL_RING_SYSTEM_COMPLETE.md`** - Procedural platform generation upgrade

### ⚠️ AVOID DEPRECATED FILES
Many `.md` files exist but are outdated. Always prefer files with:
- `AAA_` prefix (indicates refactored systems)
- `COMPLETE` or `FINAL` in name
- Recent dates (October 2025)
- `✅` status indicators

## 🎯 Architecture Overview

### Core Systems & Singletons
- **GameManager** (`GameManager.cs`) - Central singleton providing cached references to all major systems, eliminating 180+ `FindObjectOfType` calls
- **AAAMovementController** - Primary player movement with wall-jumping, double-jumping, and aerial tricks
- **PlayerShooterOrchestrator** - Dual-hand shooting system (left/right emit points) with shotgun and stream modes
- **CompanionAI** - Legacy monolithic AI system being migrated to modular `CompanionAI.CompanionCore` namespace

### Configuration-Driven Design
Most systems use **ScriptableObject configs** instead of inspector values:
- `MovementConfig.cs` - Replaces 60+ movement settings with data assets
- `HandAnimationProfile.cs` - Animation timing and blend configurations
- `CompanionData.cs` - AI behavior presets (Aggressive, Tank, Medic, etc.)
- Create via `Assets > Create > Game > [System] Configuration`

### Namespace Organization
```csharp
GeminiGauntlet.Audio         // Modern sound system (FIXSOUNDSCRIPTS/)
GeminiGauntlet.Animation     // Hand and procedural animation
GeminiGauntlet.Progression   // XP, leveling, stats
GeminiGauntlet.UI           // FloatingTextManager, UI systems
GeminiGauntlet.Missions     // Quest system with FORGE integration
```

## 🎮 Key Game Systems

### Movement & Physics
- **Scale Factor**: Game uses 300+ unit characters with proportionally scaled physics
- **Integration Points**: `AAAMovementController` connects to `AAACameraController`, `AerialTrickSystem`, `WallJumpXPSystem`
- **Bleeding Out Mode**: Special slow movement state when player health is critical

### Audio Architecture
- **Modern System**: `Assets/scripts/Audio/FIXSOUNDSCRIPTS/` contains current implementation
- **Legacy Support**: Old `GameSounds`, `SoundEvents` calls still work via compatibility layer
- **3D Audio Standard**: All spatial audio uses `spatialBlend = 1f`, `minDistance = 5f`, `maxDistance = 500f`
- **Setup**: Use `SoundSystemBootstrap` component for auto-initialization

### Hand System
- **Dual Hands**: Independent left/right systems with separate emit points, animations, and overheating
- **Integration**: `HolographicHandController` provides visual effects, `LayeredHandAnimationController` manages animations
- **Emit Points**: Critical for shooting - usually on hand GameObjects, configured in `PlayerShooterOrchestrator`

## 🔧 Development Patterns

### Setup & Integration Helpers
Most systems include setup automation:
- `[ContextMenu("Setup System")]` methods for one-click configuration
- `*SetupGuide.cs` components with validation and auto-setup
- Example: `MissionSetupGuide.cs` validates all dependencies and creates missing components

### Error Handling & Diagnostics
- **NaN Protection**: Physics systems include comprehensive NaN/infinity checks
- **Graceful Fallbacks**: Missing components degrade functionality instead of crashing
- **Debug Tools**: Most systems have `[ContextMenu("Test/Debug")]` options

### Performance Optimization
- **Object Pooling**: Used for projectiles, VFX, audio sources
- **Cached References**: GameManager pattern eliminates repeated component lookups  
- **Coroutine-Based Updates**: Heavy systems use coroutines instead of Update()

## 🚀 Common Workflows

### Adding New Systems
1. Create ScriptableObject config if needed
2. Add reference to `GameManager.cs` for easy access
3. Include setup helper with validation
4. Add namespace following `GeminiGauntlet.[Domain]` pattern

### Adding Equippable Weapons
1. Create `EquippableWeaponItemData` asset via `Assets > Create > Inventory > Equippable Weapon`
2. Configure weapon properties (hand type, weapon ID, unique flag)
3. Create world pickup prefab with `WorldSwordPickup` component
4. Add to chest loot tables via `ChestController.swordItemData`
5. `WeaponEquipmentManager` auto-syncs with `PlayerShooterOrchestrator`
6. Use `UnifiedSlot.OnSlotChanged` event for equipment tracking

### Testing & Debugging
- Use GameManager's cached references instead of `FindObjectOfType`
- Check Console for system initialization messages (✅/❌ patterns)
- Most components have built-in test methods via context menus

### Audio Integration
```csharp
// Modern approach
GameSounds.PlayShotgunBlast(position, level, volume);

// Legacy support (still works)
SoundEvents.PlayRandomSound3D(clips, position, volume);
```

### Movement Integration
```csharp
// Get movement controller via GameManager
var movement = GameManager.Instance.AAAMovementController;
if (movement != null && movement.IsGrounded()) { /* ... */ }
```

## 📦 Key Dependencies

- **Unity Input System** (1.14.0) - New input system used throughout
- **URP** (16.0.6) - Universal Render Pipeline with post-processing
- **NavMesh** (2.0.7) - For companion AI pathfinding  
- **ProBuilder** (6.0.5) - Level geometry tools
- **Newtonsoft JSON** (3.2.1) - Save/load systems

## ⚠️ Important Notes

- **File Naming**: "AAA" prefix indicates high-quality/refactored systems
- **Legacy Code**: Some systems marked as "DEPRECATED" or "LEGACY" - check for modern replacements
- **Documentation**: Extensive `.md` files in root directory contain implementation details and setup guides
- **Scale Aware**: All physics values are tuned for 300+ unit scale characters

When modifying existing systems, always check for related setup guides and integration helpers to understand the full dependency chain.

## 🗺️ System-Specific Navigation Guide

### 🎮 Movement & Physics
**Current Implementation**: `AAAMovementController.cs` + `MovementConfig.cs`
- **Main Doc**: `AAA_CRITICAL_FIXES_COMPLETE.md` - All physics bugs fixed
- **Setup**: Physics values tuned for 300+ unit scale characters
- **Integration**: Wall jump → `AAA_WALL_JUMP_SYSTEM.md`
- **Bleeding Out**: `AAA_BLEEDING_OUT_DELUXE_COMPLETE.md`
- **Falling Damage**: `AAA_FALLING_DAMAGE_SYSTEM_COMPLETE.md`

### 🔊 Audio Systems
**Current Implementation**: `Assets/scripts/Audio/FIXSOUNDSCRIPTS/`
- **Main Doc**: `AAA_AUDIO_SYSTEM_COMPLETE_FIX.md` - Root cause analysis & fixes
- **Setup Guide**: `Assets/scripts/Audio/AudioSystemSetupGuide.md`
- **Legacy Compatibility**: All old `GameSounds`/`SoundEvents` calls still work
- **3D Audio Standard**: `spatialBlend = 1f`, `minDistance = 5f`, `maxDistance = 500f`

### ✋ Hand & Combat Systems
**Current Implementation**: Dual-hand shooting + holographic effects + equippable sword system
- **Shooting**: `PlayerShooterOrchestrator.cs` with emit points
- **Holographic**: `Assets/AAA_HOLOGRAPHIC_INTEGRATION_GUIDE.md`
- **Sword Mode**: `AAA_SWORD_MODE_IMPLEMENTATION_COMPLETE.md`
- **Equippable Weapons**: `WeaponEquipmentManager.cs` singleton gates sword mode behind equipment slot
- **Item Data**: `EquippableWeaponItemData.cs` extends `ChestItemData` for weapon properties
- **World Pickup**: `WorldSwordPickup.cs` handles E key interaction (250 unit range)
- **Equipment Slots**: Right hand weapon slot required for sword mode (left hand future-ready)
- **Hand Animation**: `LayeredHandAnimationController.cs`

### 🤖 Companion AI
**Migration in Progress**: From monolithic to modular system
- **Legacy**: `CompanionAI.cs` (still functional, marked LEGACY)
- **Modern**: `CompanionAI.CompanionCore` namespace (preferred)
- **Setup**: Use `CompanionData.cs` ScriptableObjects for behavior presets

### 🏗️ Platform & Procedural Systems
**Current Implementation**: Orbital ring system with tier-based generation
- **Main Doc**: `AAA_ORBITAL_RING_SYSTEM_COMPLETE.md`
- **Generation**: `ProceduralPlatformGenerator.cs`
- **Migration**: Old configs need conversion (see migration guide)

### 🎯 XP & Progression
**Current Implementation**: Integrated XP system with audio feedback
- **Main Doc**: `AAA_COMPLETE_AUDIO_COMBO_SYSTEM.md`
- **Wall Jump XP**: `WallJumpXPSimple.cs`
- **Aerial Tricks**: `AerialTrickXPSystem.cs`
- **Combo Multipliers**: `ComboMultiplierSystem.cs`

### 🔐 Access & Security Systems
**Latest Additions** (Oct 2025):
- **Keycards**: `KEYCARD_SYSTEM_COMPLETE.md` - 5 keycard types for doors
- **Elevators**: `ELEVATOR_SYSTEM_SUMMARY.md` - Keycard-activated elevators
- **Cheats/ESP**: `AAA_WALLHACK_COMPLETE_PACKAGE.md` - Professional wallhack system

### 🎥 Camera & Visual Effects
**Current Implementation**: AAA-grade camera with aerial trick support
- **Main Index**: `AAA_AERIAL_TRICK_MASTER_INDEX.md` - Complete documentation suite
- **Camera Controller**: `AAACameraController.cs`
- **Visual Flow**: `AAA_AERIAL_TRICK_VISUAL_FLOW_DIAGRAM.md`

## 🔍 Finding the Right Documentation

### Quick System Status Check
1. Look for `AAA_` prefixed files (current systems)
2. Check for `COMPLETE` or `FINAL` in filename
3. Verify date is October 2025 or later
4. Look for `✅` completion markers in content

### Documentation Hierarchy
1. **Master Index** → `AAA_MASTER_SETUP_INDEX.md`
2. **System Complete** → `AAA_[SYSTEM]_COMPLETE.md`
3. **Quick Setup** → `AAA_[SYSTEM]_QUICK_SETUP.md`
4. **Technical Reference** → `AAA_[SYSTEM]_TECHNICAL_REFERENCE.md`

### Setup Pattern Recognition
Most systems follow this pattern:
- `[System]SetupGuide.cs` - Validation component with context menus
- `[System]Config.cs` - ScriptableObject configuration
- `[System]Manager.cs` - Runtime controller
- Setup guides always include `[ContextMenu("Setup System")]` methods