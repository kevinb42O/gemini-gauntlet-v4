# 💀 FLYING SKULL ENEMY SYSTEM - Complete Setup Guide

## 🎯 Overview

You now have a **brand new persistent flying skull enemy system** that's completely separate from your existing `SkullEnemy.cs`. This system features:

✅ **Persistent World Presence** - Skulls spawn at game start and stay in your world  
✅ **Multi-Level Spawning** - Spawn across 3+ levels/floors with configurable radii  
✅ **Intelligent Flying AI** - Erratic floating movement with up/down oscillation  
✅ **360° Detection** - Simple radius-based player detection  
✅ **Wall-Aware Pathfinding** - Never penetrates walls, ground, or ceilings  
✅ **Indoor-Optimized** - Perfect for enclosed environments with obstacles  
✅ **Performance Optimized** - LOD-friendly with efficient raycasting  

---

## 📦 New Files Created

1. **`FlyingSkullEnemy.cs`** - The flying skull enemy script (attach to prefab)
2. **`FlyingSkullSpawnManager.cs`** - Spawn manager for multi-level spawning (scene object)
3. **`FLYING_SKULL_ENEMY_SETUP.md`** - This documentation file

---

## 🚀 Quick Start Setup (5 Minutes)

### Step 1: Create the Flying Skull Prefab

1. **Create a new GameObject** in your scene:
   - Right-click in Hierarchy → `Create Empty`
   - Name it: `FlyingSkull`

2. **Add the skull mesh/model**:
   - Drag your skull model as a child of `FlyingSkull`
   - Position it so the skull is centered at (0, 0, 0)

3. **Add required components** to `FlyingSkull`:
   - Add `Rigidbody`
   - Add `Sphere Collider`
   - Add `FlyingSkullEnemy` script

4. **Configure the components**:
   
   **Rigidbody:**
   - ✅ Use Gravity: `OFF`
   - Drag: `3`
   - Angular Drag: `5`
   - Constraints: Freeze Rotation X, Z
   
   **Sphere Collider:**
   - ✅ Is Trigger: `ON`
   - Radius: `1-2` (depends on your skull size)
   
   **FlyingSkullEnemy:**
   - Max Health: `30`
   - Fly Speed: `10`
   - Detection Radius: `50`
   - Attack Range: `3`
   - Max Chase Range: `150`
   - Detection Interval: `0.5s`
   
   **Obstacle Avoidance:**
   - Wall Clearance: `2`
   - Ground Clearance: `3`
   - Ceiling Clearance: `2`
   - Obstacle Detection Range: `5`
   - Avoidance Force: `15`
   - Obstacle Layers: Set to `Default` + any wall/ground layers

5. **Set the layer**:
   - Set `FlyingSkull` GameObject layer to: **`Enemy`**

6. **Optional - Add visual effects**:
   - Assign `Death Effect Prefab` (particle effect on death)
   - Assign `Power Up Prefabs` (list of power-ups to drop)

7. **Save as prefab**:
   - Drag `FlyingSkull` from Hierarchy to your Prefabs folder
   - Delete the instance from the scene

---

### Step 2: Setup the Spawn Manager

1. **Create spawn manager GameObject**:
   - Right-click in Hierarchy → `Create Empty`
   - Name it: `FlyingSkullSpawnManager`

2. **Add the script**:
   - Add `FlyingSkullSpawnManager` component

3. **Create spawn center points** (one for each level):
   - Create 3 empty GameObjects in your scene
   - Name them:
     - `SkullSpawn_Level1_Center`
     - `SkullSpawn_Level2_Center`
     - `SkullSpawn_Level3_Center`
   - Position them at the center of each floor/level in your indoor area

4. **Configure the spawn manager**:

   **Flying Skull Prefab:**
   - Drag your `FlyingSkull` prefab into this slot

   **Spawn Levels (Array):**
   
   **Level 1 (Ground Floor):**
   - Level Name: `Ground Floor`
   - Center Point: Drag `SkullSpawn_Level1_Center`
   - Skull Count: `5`
   - Spawn Radius: `50`
   - Height Offset: `0`
   - Vertical Spread: `5`
   
   **Level 2 (Middle Floor):**
   - Level Name: `Middle Floor`
   - Center Point: Drag `SkullSpawn_Level2_Center`
   - Skull Count: `5`
   - Spawn Radius: `50`
   - Height Offset: `0`
   - Vertical Spread: `5`
   
   **Level 3 (Top Floor):**
   - Level Name: `Top Floor`
   - Center Point: Drag `SkullSpawn_Level3_Center`
   - Skull Count: `5`
   - Spawn Radius: `50`
   - Height Offset: `0`
   - Vertical Spread: `5`

   **Spacing & Validation:**
   - Min Spacing: `10`
   - ✅ Validate Spawn Positions: `ON`
   - Obstacle Layers: Set to layers that should block spawning (walls, etc.)
   - Spawn Check Radius: `2`

5. **Test in editor**:
   - ✅ Enable `Show Gizmos`
   - You should see colored circles (Red, Green, Blue) showing spawn areas
   - Adjust center points and radii until they cover your desired areas

---

## 🎮 Testing & Debugging

### Visual Gizmos

The spawn manager shows color-coded spawn areas:
- 🔴 **Red Circle** = Level 1 spawn area
- 🟢 **Green Circle** = Level 2 spawn area
- 🔵 **Blue Circle** = Level 3 spawn area
- 🟡 **Yellow Spheres** = Actual spawn positions (play mode only)

### Debug Console

When you enter Play Mode, watch the console for:
```
[FlyingSkullSpawnManager] 🚀 FlyingSkullSpawnManager starting...
[FlyingSkullSpawnManager] ✅ Prefab validated: FlyingSkull
[FlyingSkullSpawnManager] 🏢 Level 0 (Ground Floor): Spawning 5 skulls
[FlyingSkullSpawnManager] ✅ Ground Floor: Spawned skull 1/5 at (...)
[FlyingSkullSpawnManager] 🎯 SPAWN COMPLETE: 15 flying skulls spawned across 3 levels
```

### Common Issues

**❌ Skulls not spawning?**
- Check console for error messages
- Ensure center points are assigned
- Increase spawn radius if area is too small
- Check obstacle layers aren't blocking all positions

**❌ Skulls spawning inside walls?**
- Enable `Validate Spawn Positions`
- Set `Obstacle Layers` to include walls
- Increase `Spawn Check Radius`

**❌ Skulls clipping through walls during movement?**
- Increase `Obstacle Detection Range` on skull prefab
- Increase `Avoidance Force`
- Ensure `Obstacle Layers` includes walls/ground/ceiling

**❌ Skulls sinking into ground?**
- Increase `Ground Clearance`
- Ensure ground layer is in `Obstacle Layers`

---

## ⚙️ Advanced Configuration

### Detection Ranges

**For Large Indoor Spaces (Warehouses, Hangars):**
```
Detection Radius: 80-100
Max Chase Range: 200-250
Detection Interval: 0.3-0.5s
```

**For Tight Corridors:**
```
Detection Radius: 30-40
Max Chase Range: 100-120
Detection Interval: 0.7-1.0s
```

**For Performance (Many Skulls):**
```
Detection Interval: 1.0-1.5s
Enable Hit Glow: OFF
```

### Flying Behavior

**Aggressive/Fast Skulls:**
```
Fly Speed: 12-15
Erratic Movement Intensity: 0.8-1.0
Erratic Change Interval: 0.5-0.8s
Float Speed: 2.0-2.5
```

**Slow/Creepy Skulls:**
```
Fly Speed: 6-8
Erratic Movement Intensity: 0.3-0.5
Erratic Change Interval: 2.0-3.0s
Float Speed: 1.0-1.5
```

### Spawn Patterns

**Dense Swarm:**
```
Skull Count: 10-15 per level
Min Spacing: 5-8
Spawn Radius: 30-40
```

**Scattered Patrols:**
```
Skull Count: 3-5 per level
Min Spacing: 15-20
Spawn Radius: 60-80
```

---

## 🔧 Integration with Existing Systems

### Audio

The flying skulls use your existing skull audio system:
- `SkullSoundEvents.StartSkullChatter()` - Chatter sounds
- `SkullSoundEvents.PlaySkullAttackSound()` - Attack sounds
- `SkullDeathManager.QueueDeathAudio()` - Death sounds

### Stats & Progression

Kills are tracked as:
- Enemy Type: `"flyingskull"`
- Stats: `GameStats.AddSkullKillToCurrentRun()`
- XP: `XPHooks.OnEnemyKilled("flyingskull", position)`
- Missions: `MissionProgressHooks.OnEnemyKilled("flyingskull")`

### Power-Ups

Configure on the skull prefab:
- Enable `Can Drop Power Ups`
- Set `Power Up Drop Chance` (0.08 = 8%)
- Assign `Power Up Prefabs` array

---

## 📊 Performance Considerations

### Skull Count Recommendations

**Potato PC (30-60 FPS):**
- Max 10-15 skulls total
- Detection Interval: 1.0s+
- Disable Hit Glow

**Mid-Range PC (60-120 FPS):**
- Max 20-30 skulls total
- Detection Interval: 0.5s
- Enable Hit Glow

**High-End PC (120+ FPS):**
- Max 40-50 skulls total
- Detection Interval: 0.3s
- All effects enabled

### Optimization Tips

1. **Use shorter detection intervals for fewer skulls**
2. **Disable hit glow if you have 20+ skulls**
3. **Increase spacing to reduce collision checks**
4. **Use larger spawn radii to spread skulls out**
5. **Disable debug logs in production builds**

---

## 🎨 Visual Customization

### Materials

1. Create a skull material
2. Enable emission for glow effects
3. Assign to skull mesh
4. The hit glow system will automatically animate emission

### Particles

1. Add particle systems as children of the skull prefab
2. They'll auto-play on spawn and stop on death
3. Assign death effect particles to `Death Effect Prefab`

---

## 🔥 Pro Tips

1. **Test one level first** - Set levels 2 and 3 to 0 skulls while testing
2. **Use vertical spread** - Creates natural-looking vertical variation
3. **Place center points carefully** - Put them in open areas, not inside walls
4. **Adjust detection based on difficulty** - Smaller = harder to spot, larger = more aggressive
5. **Balance skull count with space** - Too many in tight areas = clustered chaos
6. **Use scene view gizmos** - Visualize spawn areas before playing

---

## 🐛 Troubleshooting

### Skulls Not Moving

**Check:**
- Rigidbody `Use Gravity` is OFF
- Rigidbody is NOT Kinematic
- Detection Radius is large enough to reach player
- Player is in scene and active

### Skulls Moving Erratically

**Fix:**
- Reduce `Erratic Movement Intensity`
- Increase `Erratic Change Interval`
- Reduce `Fly Speed`

### Skulls Getting Stuck

**Fix:**
- Increase `Avoidance Force`
- Increase `Obstacle Detection Range`
- Ensure `Obstacle Layers` is correctly set
- Check for complex geometry blocking paths

### Performance Issues

**Fix:**
- Reduce skull count
- Increase `Detection Interval`
- Disable `Enable Hit Glow`
- Disable `Enable Debug Logs`
- Increase `Min Spacing` (fewer skulls in same area)

---

## 📝 Comparison: Flying Skull vs Original Skull

| Feature | SkullEnemy (Original) | FlyingSkullEnemy (New) |
|---------|----------------------|------------------------|
| **Spawning** | Tower-based, triggered | Persistent world spawns |
| **Movement** | Attack patterns (swooping, circling) | Free-floating with erratic motion |
| **Detection** | Complex targeting system | Simple 360° radius |
| **Purpose** | Boss minions, tower defense | Ambient world threat |
| **Pathfinding** | Basic avoidance | Full wall/ceiling/ground awareness |
| **State Machine** | Spawning → Hunting → Attacking → Decaying | Idle → Hunting → Attacking |
| **LOD System** | Yes (SkullEnemyManager) | No (standalone) |
| **Tower Integration** | Yes | No |

Both systems can coexist in your game!

---

## ✅ Setup Checklist

- [ ] `FlyingSkullEnemy.cs` added to project
- [ ] `FlyingSkullSpawnManager.cs` added to project
- [ ] Flying skull prefab created with all components
- [ ] Prefab saved to Prefabs folder
- [ ] Spawn manager added to scene
- [ ] 3 spawn center points created and positioned
- [ ] Center points assigned to spawn manager
- [ ] Flying skull prefab assigned to spawn manager
- [ ] Spawn levels configured (counts, radii)
- [ ] Obstacle layers set correctly
- [ ] Tested in Play Mode
- [ ] Verified skulls spawn correctly
- [ ] Verified skull detection and chasing
- [ ] Verified wall/ground collision avoidance
- [ ] Audio working (chatter, attack, death)
- [ ] Death effects spawning
- [ ] Stats tracking working

---

## 🎊 You're Done!

Your flying skull enemies are now ready to terrorize your players! Adjust the settings to match your game's difficulty and performance requirements.

**Enjoy your new persistent flying skull threat! 💀✨**
