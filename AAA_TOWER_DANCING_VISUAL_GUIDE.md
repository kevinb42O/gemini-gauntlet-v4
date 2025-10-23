# Tower Dancing Behavior - Visual Guide

## Platform Boundary Enforcement

```
┌─────────────────────────────────────────────────┐
│         PLATFORM TRIGGER BOUNDS                 │
│  ┌───────────────────────────────────────────┐  │
│  │  SAFE ZONE (2-unit margin from edges)    │  │
│  │                                           │  │
│  │    T₁ ──────→ ●                          │  │
│  │   (spawn)   (center)                     │  │
│  │                                           │  │
│  │              FIRST MOVE:                  │  │
│  │         Moves 60-80% towards center      │  │
│  │                                           │  │
│  │         T₂                                │  │
│  │          ↓                                │  │
│  │          ↓  SUBSEQUENT MOVES:            │  │
│  │          ↓  Random within safe zone      │  │
│  │          ●                                │  │
│  │                                           │  │
│  └───────────────────────────────────────────┘  │
│  ← 2 units →                    ← 2 units →     │
└─────────────────────────────────────────────────┘
```

## Movement Rules

### Rule 1: First Move Towards Center
```
SPAWN POSITION (near edge)
     │
     │ 60-80% of distance
     ↓
CENTER TARGET
```

**Why?** Towers spawning near edges immediately move inward to safety.

### Rule 2: Stay Within Bounds
```
❌ INVALID: Outside platform bounds
❌ INVALID: Within 2 units of edge
✅ VALID: Inside safe zone
✅ VALID: At least 5 units from other towers
```

### Rule 3: Collision Avoidance
```
    T₁        T₂
    ●         ●
     ←─ 5+ ─→
    
Minimum 5-unit distance between towers
```

## Decision Flow

```
START: Need new dance target
    │
    ├─→ Is this first move?
    │   YES → Move towards platform center (60-80%)
    │   NO  → Continue to random selection
    │
    └─→ Generate random position
        │
        ├─→ Check platform bounds
        │   │
        │   ├─→ Outside bounds? → Try again (up to 10 attempts)
        │   └─→ Within bounds? → Continue
        │
        ├─→ Check tower collisions
        │   │
        │   ├─→ Too close to another tower? → Try again
        │   └─→ Safe distance? → Continue
        │
        └─→ Position is VALID → Move to target
        
If 10 attempts fail → Stay at current position
```

## Example Scenarios

### Scenario 1: Edge Spawn
```
Before:
┌──────────────┐
│ T            │  ← Tower spawns near left edge
│              │
└──────────────┘

After First Move:
┌──────────────┐
│     T        │  ← Moves towards center
│              │
└──────────────┘
```

### Scenario 2: Multiple Towers
```
┌──────────────────┐
│  T₁    T₂    T₃  │  ← All towers maintain 5+ unit spacing
│                  │
│  All stay within │
│  platform bounds │
└──────────────────┘
```

### Scenario 3: Small Platform
```
┌─────┐
│  T  │  ← Tower on small platform
└─────┘     stays centered, minimal movement
```

## Safety Features

1. **2-Unit Safety Margin**: Towers never get within 2 units of platform edge
2. **10 Attempt Limit**: Prevents infinite loops when searching for valid positions
3. **Fallback Behavior**: If no valid position found, tower stays put
4. **World Space Validation**: Accurate boundary checking regardless of platform rotation/scale
5. **First Move Inward**: Towers near edges move towards safety first

## Debug Logging

Watch for these logs in Unity Console:

```
[TowerController] TowerName FIRST DANCE MOVE towards center: (x, y, z)
[TowerController] TowerName picked valid dancing target within bounds (attempt N)
[TowerController] TowerName couldn't find valid dancing position - staying put
```

## Performance Notes

- Boundary checks use simple AABB (axis-aligned bounding box) comparisons - very fast
- Maximum 10 attempts per target selection - bounded computation time
- No physics raycasts needed - uses collider bounds directly
- Collision checks only against active towers - scales well
