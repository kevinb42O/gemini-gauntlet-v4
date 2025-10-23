# 🧗 SMART WALL JUMP SYSTEM - COMPLETE GUIDE

## 🎯 Overview

The Smart Wall Jump System allows players to jump off walls while airborne, propelling them away from the wall with both upward and outward force. The system is intelligent, responsive, and integrates seamlessly with all existing movement mechanics.

---

## ✨ Key Features

### 🎮 Core Mechanics
- **Multi-Directional Wall Detection**: Detects walls in 8 directions around the player (N, NE, E, SE, S, SW, W, NW)
- **Smart Wall Validation**: Only detects actual walls (60-120° from vertical), not floors or ceilings
- **Realistic Physics**: Dynamic forces based on fall speed, momentum preservation, and player input
- **Player Input Influence**: Steer your wall jump direction with WASD (40% influence by default)
- **Momentum Preservation**: Keeps 25% of your horizontal speed for natural arcs
- **Dynamic Force Scaling**: Faster falls = bigger upward boost (30% bonus)
- **Consecutive Limit**: Configurable number of wall jumps allowed before touching ground
- **Anti-Spam Protection**: Cooldowns and grace periods prevent wall jump spam

### 🔧 Smart Conditions
The system only allows wall jumps when ALL conditions are met:
1. **Not Grounded**: Player must be airborne
2. **Falling Fast Enough**: Player must have minimum downward velocity
3. **Cooldown Expired**: Minimum time between wall jumps
4. **Grace Period Expired**: Brief delay after wall jump before detecting walls again
5. **Within Limit**: Haven't exceeded max consecutive wall jumps
6. **Wall Detected**: Valid wall surface within detection range

---

## 🎛️ Configuration (Inspector)

### Wall Jump System Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| **Enable Wall Jump** | `true` | Master toggle for wall jump system |
| **Wall Jump Up Force** | `160f` | Base upward force (strong vertical boost) |
| **Wall Jump Out Force** | `140f` | Base horizontal force pushing away from wall |
| **Wall Jump Fall Speed Bonus** | `0.3f` | Extra upward boost from fall speed (30% of fall speed added) |
| **Wall Jump Input Influence** | `0.4f` | How much WASD input steers direction (0-1, 40% = responsive) |
| **Wall Jump Momentum Preservation** | `0.25f` | How much horizontal speed is kept (0-1, 25% = natural arcs) |
| **Wall Detection Distance** | `60f` | How far to check for walls (scaled for 300-unit character) |
| **Wall Jump Cooldown** | `0.25f` | Minimum time between wall jumps (seconds) |
| **Wall Jump Grace Period** | `0.15f` | Delay after wall jump before detecting walls again |
| **Max Consecutive Wall Jumps** | `2` | Number of wall jumps allowed before landing |
| **Min Fall Speed For Wall Jump** | `10f` | Minimum falling speed required (-Y velocity) |
| **Show Wall Jump Debug** | `true` | Show debug rays in Scene view |

---

## 🎨 Visual Debug System

When **Show Wall Jump Debug** is enabled, you'll see:

- **🔴 Red Rays**: No wall detected in that direction
- **⚫ Gray Rays**: Surface detected but not a valid wall (floor/ceiling)
- **🔵 Cyan Rays**: Valid wall detected!
- **🟡 Yellow Ray**: Wall normal (direction of push)
- **🟢 Green Line**: Line from player to wall hit point

---

## 🎮 How to Use

### Basic Wall Jump
1. **Jump or Fall** off a ledge
2. **Move toward a wall** (or just fall past one)
3. **Press Jump (Space)** while falling near the wall
4. **You'll propel away** from the wall with upward arc!

### Advanced Techniques

#### Wall Climb
Chain wall jumps between parallel walls:
- Jump off Wall A → propel to Wall B
- Jump off Wall B → propel back to Wall A
- Repeat up to `maxConsecutiveWallJumps` times
- Touch ground to reset counter

#### Wall Jump + Air Control
- After wall jump, use WASD for air steering
- Your existing air control system still works
- Can redirect trajectory mid-flight

#### Wall Jump into Slide
- Wall jump near ground
- Hold Crouch on landing
- Momentum carries into slide!

---

## 🔧 Technical Details

### Wall Detection Algorithm
```csharp
1. Cast 8 rays around player at mid-height
2. For each hit:
   - Check angle from vertical (60-120° = valid wall)
   - Verify player is moving toward wall (or at least not away)
   - Track closest valid wall
3. Return closest wall normal and hit point
```

### Realistic Physics System
```csharp
// Step 1: Calculate base direction from wall normal
awayFromWall = wallNormal.normalized

// Step 2: Blend with player input (40% influence)
finalDirection = Lerp(awayFromWall, playerInputDirection, 0.4f)

// Step 3: Dynamic upward force based on fall speed
dynamicUpForce = wallJumpUpForce + (fallSpeed * 0.3f)

// Step 4: Preserve 25% of horizontal momentum
preservedMomentum = currentVelocity * 0.25f

// Step 5: Combine all forces
horizontalPush = finalDirection * wallJumpOutForce + preservedMomentum
velocity = (horizontalPush.x, dynamicUpForce, horizontalPush.z)
```

### Integration Points
- ✅ Works with regular jumps
- ✅ Works with double jumps (if enabled)
- ✅ Respects coyote time and jump buffering
- ✅ Triggers jump animation
- ✅ Plays jump sound
- ✅ Integrates with animation state manager
- ✅ Resets on landing (grounded state)

---

## 🎯 Tuning Guide

### For More Power
```
wallJumpUpForce = 180f (increase base power)
wallJumpOutForce = 160f (increase horizontal push)
wallJumpFallSpeedBonus = 0.4f (bigger dynamic boost)
```

### For More Player Control
```
wallJumpInputInfluence = 0.6f (60% player control - very responsive)
wallJumpMomentumPreservation = 0.15f (less momentum = easier steering)
```

### For More Momentum/Realism
```
wallJumpMomentumPreservation = 0.35f (keep 35% speed = natural arcs)
wallJumpInputInfluence = 0.25f (less control = more realistic physics)
```

### For Wall Climb Chains
```
maxConsecutiveWallJumps = 3 or 4 (more jumps before landing)
wallJumpCooldown = 0.2f (faster chaining)
wallJumpFallSpeedBonus = 0.4f (bigger boost when falling fast)
```

### For Tighter/Harder Wall Jumps
```
wallDetectionDistance = 50f (must be closer)
minFallSpeedForWallJump = 15f (must be falling faster)
wallJumpInputInfluence = 0.2f (less control)
```

### For Easier/Forgiving Wall Jumps
```
minFallSpeedForWallJump = 5f (jump at any fall speed)
wallJumpGracePeriod = 0.1f (detect walls sooner)
wallJumpInputInfluence = 0.5f (more control)
```

---

## 🐛 Troubleshooting

### Wall Jumps Not Working?
- ✅ Check `enableWallJump` is true
- ✅ Ensure you're falling fast enough (`velocity.y < -minFallSpeedForWallJump`)
- ✅ Verify wall is within `wallDetectionDistance`
- ✅ Check if you've used all `maxConsecutiveWallJumps`
- ✅ Enable `showWallJumpDebug` to see detection rays

### Wall Jump Feels Weak?
- Increase `wallJumpUpForce` and `wallJumpOutForce`
- Check that `gravity` isn't too strong (overpowers jump)

### Can't Chain Wall Jumps?
- Increase `maxConsecutiveWallJumps`
- Decrease `wallJumpCooldown`
- Check walls are within detection range

### Jumping Off Floors/Ceilings?
- System validates wall angle (60-120° from vertical)
- If this happens, verify `groundMask` layer settings

---

## 🎬 Animation Integration

Wall jumps automatically trigger the **Jump** animation state through `PlayerAnimationStateManager`. This is the same animation used for:
- Regular jumps
- Double jumps
- Wall jumps

No additional animation setup required!

---

## 🔊 Audio Integration

Wall jumps use the **exact same jump sound** as regular jumps from `SoundEvents.jumpSounds` via:
- `GameSounds.PlayPlayerJump(position, 1.1f)` - Plays at 110% volume for extra impact

The sound system automatically:
- ✅ Picks random jump sound from array (variety)
- ✅ Applies proper 3D positioning
- ✅ Adds pitch variation for natural feel
- ✅ Respects cooldowns to prevent spam

**Same sound, consistent feel!** Players won't be confused by different sounds for different jump types.

---

## 🎮 Why These Wall Jumps Feel AMAZING

### Realistic Physics System

**1. Dynamic Force Scaling**
- Faster falls = bigger upward boost
- Fall at 50 speed → get 160 + (50 × 0.3) = **175 upward force**
- Creates satisfying "impact-based" jumps

**2. Player Input Influence**
- Press W/A/S/D while wall jumping to steer direction
- 40% influence means responsive but not overpowered
- Can't jump back into wall (safety check)

**3. Momentum Preservation**
- Keeps 25% of your horizontal speed
- Sprint toward wall → wall jump keeps some speed
- Creates beautiful arcing trajectories

**4. Camera-Relative Control**
- Input uses camera direction (intuitive)
- Jump where you're looking (natural feel)
- No awkward world-space only controls

**Result:** Wall jumps feel like AAA parkour games (Mirror's Edge, Titanfall) instead of cheap mobile game mechanics!

---

## 💡 Design Philosophy

The wall jump system follows these principles:

1. **Realistic Physics**: Natural momentum and dynamic forces
2. **Player Agency**: Input matters but doesn't override physics
3. **Smart Detection**: Only valid walls, no false positives
4. **Responsive**: Instant feedback, no input lag
5. **Balanced**: Cooldowns prevent abuse while feeling fluid
6. **Integrated**: Works with all existing systems
7. **Debuggable**: Visual feedback for tuning

---

## 🚀 Performance Notes

- **HIGHLY OPTIMIZED**: Only 8 raycasts **when you press jump button** (not every frame!)
- **Zero cost while grounded** - system completely inactive
- **On-demand detection** - only runs when player attempts wall jump
- **Potato-friendly** - designed for low-end hardware
- **Typical cost**: 8 raycasts once every 0.25+ seconds (when cooldown allows)
- **Early-out conditions** prevent unnecessary checks

---

## 📝 Code Location

All wall jump code is in `AAAMovementController.cs`:
- **Lines 50-76**: Configuration fields and state variables
- **Lines 1076**: Reset counter on landing
- **Lines 1156-1164**: Wall jump input detection and execution (on-demand only!)
- **Lines 1505-1650**: Wall jump system implementation
  - `CanWallJump()`: Validation logic (cheap early-out checks)
  - `DetectWall()`: Multi-directional wall detection (only runs on button press)
  - `PerformWallJump()`: Force application and state updates

---

## 🎊 Enjoy Your New Wall Jump System!

You now have a production-ready wall jump system that feels smooth, responsive, and fun. Experiment with the parameters to find the perfect feel for your game!

**Pro Tip**: Start with default values, then adjust `wallJumpUpForce` and `wallJumpOutForce` to match your game's pacing.
