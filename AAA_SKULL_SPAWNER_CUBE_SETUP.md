# Skull Spawner Cube - Setup Guide
## Dynamic Spinning Cube That Spawns Enemy Skulls

---

## ✨ Features

### Visual Effects (Zero Work!)
- ✅ **Smooth rotation** - Spins slowly on dynamic axes
- ✅ **Fast spin on spawn** - Spins rapidly when spawning skulls
- ✅ **Glowing emission** - Cyan glow (idle) → Red glow (spawning)
- ✅ **Pulsing effect** - Subtle glow pulse for extra polish
- ✅ **Wobble animation** - Slight wobble for organic feel
- ✅ **Smooth transitions** - All effects blend smoothly

### Spawning Mechanics
- ✅ **Timed waves** - Spawns skulls every 30 seconds (configurable)
- ✅ **Circle pattern** - Skulls spawn in 360° around cube
- ✅ **Dramatic launch** - Skulls burst outward with velocity
- ✅ **Auto-start** - Begins spawning automatically

---

## 🚀 Setup (30 Seconds)

### Step 1: Create the Cube
1. **Create a Cube** in your scene (GameObject → 3D Object → Cube)
2. **Name it**: `SkullSpawnerCube`
3. **Position it** where you want skulls to spawn from

### Step 2: Add the Script
1. **Add component**: `SkullSpawnerCube.cs`
2. **In Inspector:**
   - **Skull Prefab**: Drag your SkullEnemy prefab here
   - **Done!** Everything else has smart defaults

### Step 3: Make It Glow (Automatic!)
The script automatically:
- Creates a unique material instance
- Enables emission
- Sets up glow colors
- Handles all transitions

**No manual material setup needed!** 🎉

---

## 🎮 How It Works

### Normal State:
```
Slow spin (30°/sec)
Cyan glow with pulse
Smooth wobble
Dynamic axis changes every 5 seconds
```

### Spawning State (Every 30s):
```
Fast spin (360°/sec) 
Red glow
Spawns 3 skulls in circle pattern
Skulls burst outward
Returns to normal after 2 seconds
```

---

## ⚙️ Inspector Settings

### Skull Spawning
| Setting | Default | Description |
|---------|---------|-------------|
| Skull Prefab | None | Your SkullEnemy prefab |
| Skulls Per Wave | 3 | Number of skulls per spawn |
| Spawn Interval | 30s | Time between spawns |
| Spawn Radius | 500 | Circle radius for spawns |
| Spawn Height | 300 | Height above cube |

### Visual Settings
| Setting | Default | Description |
|---------|---------|-------------|
| Normal Rotation Speed | 30°/s | Slow spin speed |
| Fast Rotation Speed | 360°/s | Fast spin when spawning |
| Fast Spin Duration | 2s | How long fast spin lasts |
| Idle Glow Color | Cyan | Normal glow color |
| Spawn Glow Color | Red | Spawning glow color |
| Glow Intensity | 2.0 | Emission brightness |

### Auto-Start
| Setting | Default | Description |
|---------|---------|-------------|
| Auto Start | ✅ True | Start spawning on scene load |

---

## 🎨 Visual Features Breakdown

### 1. Dynamic Rotation
- Rotates on changing axes (mostly Y, sometimes diagonal)
- Axis changes every 5 seconds for variety
- Smooth transitions between axes

### 2. Fast Spin Effect
- Spins 12x faster when spawning (360°/s)
- Lasts for 2 seconds
- Smooth acceleration/deceleration

### 3. Glow System
- **Idle**: Cyan glow with subtle pulse
- **Spawning**: Red glow (no pulse)
- **Transitions**: Smooth color lerp over 0.5s
- **Emission**: Automatically enabled

### 4. Wobble Animation
- Subtle sine wave wobble
- Adds organic feel
- Doesn't interfere with main rotation

### 5. Pulsing Effect
- Glow intensity oscillates ±30%
- Only active when idle
- Frequency: 0.5 Hz (slow pulse)

---

## 🔍 Debug Logs

You'll see:
```
[SkullSpawnerCube] Initialized - Spawning 3 skulls every 30s
[SkullSpawnerCube] 💀 Spawning wave of 3 skulls!
[SkullSpawnerCube] ✅ Spawn wave complete!
```

---

## 🎯 Scene View Gizmos

When selected, you'll see:
- **Red wireframe sphere**: Spawn radius
- **Yellow wireframe sphere**: Spawn height visualization

---

## 🎮 Public Methods (Optional)

### Manual Control:
```csharp
// Trigger a spawn wave immediately
skullSpawnerCube.TriggerSpawnWave();

// Start the spawn cycle
skullSpawnerCube.StartSpawning();

// Stop spawning
skullSpawnerCube.StopSpawning();
```

---

## 💡 Customization Ideas

### More Skulls:
- Set `Skulls Per Wave` to 5-10 for intense waves

### Faster Spawning:
- Set `Spawn Interval` to 15s for constant pressure

### Dramatic Effect:
- Increase `Spawn Radius` to 1000 for wide spread
- Increase `Fast Rotation Speed` to 720°/s for crazy spin

### Different Colors:
- Change `Idle Glow Color` to green/purple/etc.
- Change `Spawn Glow Color` to orange/yellow/etc.

---

## 🎨 Material Requirements

**None!** The script works with any material:
- Standard shader
- URP/Lit shader
- Custom shaders with emission

The script automatically:
- Creates material instance
- Enables emission keyword
- Sets emission color

---

## 🚀 Performance

- **Efficient**: No continuous raycasts or physics checks
- **Optimized**: Coroutines for timed events
- **Clean**: Proper cleanup, no memory leaks
- **Lightweight**: Minimal Update() logic

---

## 🐛 Troubleshooting

**Cube doesn't glow:**
- Make sure material supports emission
- Check if material has `_EmissionColor` property
- Try using Standard or URP/Lit shader

**Skulls don't spawn:**
- Assign Skull Prefab in Inspector
- Check Console for warnings
- Verify skull prefab has necessary components

**Rotation looks weird:**
- Adjust `Normal Rotation Speed` (try 20-50)
- Check cube's initial rotation in Inspector
- Ensure cube scale is uniform (1,1,1)

**Spawns too fast/slow:**
- Adjust `Spawn Interval` (default: 30s)
- Reduce for more action, increase for breathing room

---

## 📋 Quick Checklist

- [ ] Created Cube GameObject
- [ ] Added SkullSpawnerCube.cs component
- [ ] Assigned Skull Prefab
- [ ] Positioned cube in scene
- [ ] Tested in Play mode
- [ ] Adjusted spawn interval if needed
- [ ] Customized colors (optional)

---

## 🎉 Summary

**What you get:**
- ✅ Spinning cube with dynamic rotation
- ✅ Glowing emission (cyan → red)
- ✅ Pulsing and wobble effects
- ✅ Fast spin on spawn
- ✅ Spawns skulls in circle pattern
- ✅ Fully automatic
- ✅ Zero material setup
- ✅ Zero animation setup
- ✅ Zero particle setup

**What you do:**
1. Create cube
2. Add script
3. Assign skull prefab
4. Done!

**Literally zero work!** 🎊
