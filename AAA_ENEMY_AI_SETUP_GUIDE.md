# 🎯 AAA Enemy AI - Complete Setup Guide

## Overview
This guide covers the setup and configuration of the enhanced enemy AI system inspired by DMZ Building 21. The system provides:

- **Environment-aware behavior** (indoor/outdoor detection)
- **Proper line of sight** (no seeing through walls)
- **Tactical movement** (controlled, non-glitchy)
- **Cover system** (strategic positioning)
- **State-based AI** (patrol, alert, combat, retreat)

---

## 🚀 Quick Start

### Option 1: Enhanced EnemyCompanionBehavior (Recommended)
Use the **improved existing system** with all the fixes applied.

1. **Add to enemy GameObject:**
   - `CompanionCore`
   - `CompanionMovement`
   - `CompanionCombat`
   - `CompanionTargeting`
   - `EnemyCompanionBehavior` ← The enhanced script

2. **Configure Inspector Settings:**
   ```
   ✅ Is Enemy: TRUE
   
   DETECTION:
   - Detection Radius: 25000 (25m)
   - Require Line Of Sight: TRUE
   - Line Of Sight Blockers: Set to "Walls" layer
   - LOS Raycast Count: 3
   - Eye Height: 1.8
   
   COMBAT:
   - Combat Movement Speed: 1.2 (REDUCED from 3.0)
   - Enable Tactical Movement: TRUE
   
   ENVIRONMENT AWARENESS:
   - Auto Detect Indoors: TRUE
   - Indoor Speed Multiplier: 0.5
   - Disable Jumping Indoors: TRUE
   - Disable Tactical Movement Indoors: TRUE
   
   PATROL:
   - Enable Patrol: TRUE
   - Patrol Points: Assign waypoints for hallway routes
   ```

### Option 2: TacticalEnemyAI (New System)
Use the **brand new AAA-grade system** with full state machine.

1. **Add to enemy GameObject:**
   - `CompanionCore`
   - `CompanionMovement`
   - `CompanionCombat`
   - `CompanionTargeting`
   - `TacticalEnemyAI` ← The new script

2. **Configure Inspector Settings:**
   ```
   ✅ Is Enemy: TRUE
   
   DETECTION:
   - Detection Range: 30
   - Field Of View: 90
   - Vision Blocking Layers: "Walls"
   - Eye Height: 1.8
   - LOS Raycast Count: 3
   
   MOVEMENT:
   - Patrol Speed: 2
   - Alert Speed: 4
   - Combat Speed: 3
   
   INDOOR BEHAVIOR:
   - Auto Detect Indoors: TRUE
   - Indoor Speed Multiplier: 0.6
   - Disable Jumping Indoors: TRUE
   
   COMBAT:
   - Preferred Combat Distance: 15
   - Accuracy: 0.7
   - Fire Rate: 0.5
   - Burst Duration: 2
   - Burst Cooldown: 1.5
   
   COVER SYSTEM:
   - Use Cover System: TRUE
   - Cover Search Radius: 15
   - Cover Health Threshold: 0.5
   ```

---

## 🏗️ Layer Setup (CRITICAL)

### Required Layers
You **must** set up these layers for proper detection:

1. **Create Layers:**
   - `Walls` - All walls, obstacles, buildings
   - `Ground` - Floor, terrain
   - `Player` - Player character
   - `Enemy` - Enemy characters

2. **Assign Layers:**
   - All building walls → `Walls` layer
   - All floors/terrain → `Ground` layer
   - Player GameObject → `Player` layer
   - Enemy GameObjects → `Enemy` layer

3. **Configure LayerMasks:**
   - `lineOfSightBlockers` → Select `Walls` layer ONLY
   - `coverLayers` → Select `Walls` layer
   - `groundLayers` → Select `Ground` layer

### Why This Matters
- **Without proper layers:** Enemies will see through walls
- **With proper layers:** Enemies respect line of sight and use cover

---

## 🎮 Behavior Comparison

### Before (Old System)
❌ Sees through walls  
❌ Crazy speed indoors (300x multiplier)  
❌ Constant jumping in hallways  
❌ Glitchy repositioning  
❌ No environment awareness  

### After (Enhanced System)
✅ Proper line of sight with wall detection  
✅ Controlled speed (1.2x, 0.5x indoors)  
✅ No jumping indoors  
✅ Smooth tactical movement  
✅ Auto-detects indoor/outdoor  

---

## 🏢 Indoor vs Outdoor Behavior

### Indoor Mode (Auto-Detected)
When enemy detects ceiling above:
- **Speed:** 50% slower (prevents glitching)
- **Jumping:** Disabled
- **Tactical Movement:** Disabled or reduced
- **Behavior:** Guard-like patrol, controlled engagement

### Outdoor Mode
When no ceiling detected:
- **Speed:** Normal combat speed
- **Jumping:** Enabled (30% chance)
- **Tactical Movement:** Full tactical repositioning
- **Behavior:** Aggressive pursuit, dynamic combat

### Force Indoor Mode
Set `forceIndoorMode = true` to always use indoor behavior regardless of environment.

---

## 🎯 Combat States (TacticalEnemyAI)

### State Machine Flow
```
Idle → Patrolling → Alert → Searching → Combat → Taking Cover
                                    ↓
                                Retreating
```

### State Descriptions

**Idle**
- Standing still
- Minimal processing

**Patrolling**
- Walking patrol route
- Checking for player periodically
- Smooth hallway movement

**Alert**
- Heard/saw something suspicious
- Moving to investigate
- Increased awareness

**Searching**
- Lost sight of player
- Searching last known position
- Will return to patrol if not found

**Combat**
- Actively engaging player
- Burst fire system
- Tactical positioning (advance/retreat/strafe)

**Taking Cover**
- Health below threshold
- Finding nearest cover
- Peeking out to shoot

**Retreating**
- Low health, falling back
- Seeking cover while moving away

---

## 🔫 Combat Configuration

### Burst Fire System
```
Burst Duration: 2s    ← How long to fire
Burst Cooldown: 1.5s  ← Pause between bursts
Fire Rate: 0.5s       ← Time between individual shots
```

This creates realistic combat rhythm:
- Fire for 2 seconds
- Pause for 1.5 seconds
- Repeat

### Accuracy System
```
Accuracy: 0.7 (70%)
```
- 70% chance to aim directly at player
- 30% chance to miss with random offset
- Creates realistic gunfight feel

### Combat Distance
```
Min Distance: 5m      ← Back up if closer
Preferred: 15m        ← Ideal combat range
Max Distance: 25m     ← Advance if farther
```

---

## 🛡️ Cover System

### How It Works
1. Enemy takes damage
2. Health drops below threshold (default 50%)
3. Searches for nearest cover within radius
4. Moves to cover position
5. Stays in cover for duration
6. Peeks out to shoot
7. Returns to combat

### Cover Detection
- Finds nearest walls/obstacles
- Positions behind them relative to player
- Scores based on distance and safety

### Configuration
```
Cover Search Radius: 15m
Cover Duration: 3s
Cover Health Threshold: 0.5 (50% health)
```

---

## 🚶 Patrol Setup

### Option A: Waypoint Patrol
1. Create empty GameObjects in your scene
2. Position them along hallway routes
3. Assign to `patrolPoints` array
4. Enemy will walk between them in order

**Best for:** Indoor hallways, guard routes

### Option B: Random Patrol
1. Leave `patrolPoints` empty
2. Set `randomPatrolRadius`
3. Enemy patrols randomly around spawn point

**Best for:** Open areas, outdoor zones

### Patrol Settings
```
Patrol Wait Time: 2s     ← Time at each waypoint
Random Patrol Radius: 20m ← Area to patrol if no waypoints
```

---

## 🔍 Detection System

### Line of Sight
The system uses **multiple raycasts** for accurate detection:

1. **Center ray** - Main detection
2. **Left/Right rays** - Peripheral vision
3. **Up/Down rays** - Full body detection

**Result:** Can't exploit edge cases, realistic detection

### Detection Radius
```
Detection Range: 30m  ← Maximum sight distance
Field of View: 90°    ← Cone of vision
```

### Sound Detection (TacticalEnemyAI only)
```
Sound Detection Range: 15m
```
- Hears player even outside FOV
- Triggers alert state
- Investigates sound source

---

## 🐛 Troubleshooting

### Enemy sees through walls
**Fix:** Set `lineOfSightBlockers` to `Walls` layer only

### Enemy moves too fast indoors
**Fix:** 
- Set `combatMovementSpeed = 1.2`
- Set `indoorSpeedMultiplier = 0.5`
- Enable `autoDetectIndoors`

### Enemy jumps constantly indoors
**Fix:** Enable `disableJumpingIndoors`

### Enemy doesn't detect player
**Fix:**
- Check `detectionRange` is large enough
- Verify `requireLineOfSight` settings
- Ensure player is in `fieldOfView`

### Enemy glitches/teleports
**Fix:**
- Reduce `combatSpeedMultiplier` to 1.5 or lower
- Increase `repositionInterval` to 1.0+
- Enable indoor detection

### NavMesh issues
**Fix:**
- Bake NavMesh for your scene
- Ensure enemy is on NavMesh
- Check NavMeshAgent settings

---

## 🎨 Visual Debug

### Gizmos (Scene View)
Enable `showDebugGizmos` to see:
- **Yellow sphere** - Detection range
- **Red sphere** - Min combat distance
- **Green sphere** - Preferred combat distance
- **Blue sphere** - Max combat distance
- **Yellow cone** - Field of view
- **Cyan spheres** - Patrol points
- **Red line** - Line to detected player

### Debug Logs
Enable `showDebugInfo` for console output:
- State transitions
- Player detection
- Line of sight checks
- Indoor/outdoor transitions
- Combat events

---

## 🎯 Recommended Settings by Scenario

### Indoor Hallway Guard (DMZ B21 Style)
```
Detection Range: 25m
Combat Speed: 3m/s
Indoor Speed Multiplier: 0.5
Disable Jumping Indoors: TRUE
Disable Tactical Movement Indoors: TRUE
Patrol Points: Set waypoints along hallways
Accuracy: 0.7
```

### Outdoor Aggressive Enemy
```
Detection Range: 40m
Combat Speed: 5m/s
Indoor Speed Multiplier: 1.0
Enable Tactical Movement: TRUE
Jump Chance: 0.3
Accuracy: 0.6
```

### Boss/Elite Enemy
```
Detection Range: 50m
Combat Speed: 4m/s
Accuracy: 0.9
Use Cover System: TRUE
Cover Health Threshold: 0.7
Burst Duration: 3s
```

### Patrol Guard (Low Threat)
```
Detection Range: 20m
Field of View: 60°
Patrol Speed: 2m/s
Combat Speed: 3m/s
Accuracy: 0.5
```

---

## 📊 Performance Notes

### Optimizations Built-In
- Periodic detection checks (not every frame)
- Cached raycast results
- State-based processing
- Efficient collision detection

### Recommended Settings
- `detectionInterval`: 0.2-0.3s
- `losRaycastCount`: 3 (balance of accuracy/performance)
- Max enemies per scene: 20-30

---

## 🔧 Advanced Customization

### Modify AI States
Edit `TacticalEnemyAI.cs`:
- `UpdateCombatState()` - Combat behavior
- `UpdatePatrolState()` - Patrol logic
- `UpdateAlertState()` - Investigation behavior

### Add Custom Behaviors
Override state transitions in `OnStateEnter()`:
```csharp
case AIState.Combat:
    // Your custom combat logic
    break;
```

### Integrate with Other Systems
The AI respects:
- NavMesh obstacles
- Physics collisions
- Damage system (IDamageable)
- Companion combat system

---

## 🎓 Summary

### Key Improvements
1. ✅ **No more seeing through walls** - Multi-raycast LOS
2. ✅ **Smooth indoor movement** - Speed reduction + jump disable
3. ✅ **Environment awareness** - Auto-detects indoor/outdoor
4. ✅ **Tactical combat** - Cover system + burst fire
5. ✅ **Professional behavior** - State machine + smart positioning

### What Makes It AAA
- **Responsive** - Immediate threat detection
- **Believable** - Realistic combat patterns
- **Adaptive** - Changes behavior based on environment
- **Robust** - Handles edge cases gracefully
- **Polished** - Smooth movement, no glitches

---

## 🚀 Next Steps

1. **Set up layers** (most important!)
2. **Configure inspector settings**
3. **Create patrol waypoints**
4. **Test in your building**
5. **Adjust difficulty** (accuracy, speed, etc.)
6. **Add multiple enemies**
7. **Fine-tune for your game**

---

**You now have AAA-quality enemy AI. Go blow some minds! 🔥**
