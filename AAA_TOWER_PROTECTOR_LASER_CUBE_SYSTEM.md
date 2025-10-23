# 🎯 TOWER PROTECTOR LASER CUBE SYSTEM

## Overview
The **Tower Protector Cube** (formerly SkullSpawnerCube) is a deadly laser-shooting guardian that protects capture points. It tracks the player, fires devastating laser beams, and becomes a friendly ally if the platform is captured while it's still alive.

---

## 🔥 Core Features

### 1. **Laser Attack System**
- **Attack Interval**: Every 15 seconds
- **Laser Duration**: 5 seconds of continuous fire
- **Damage**: 50 damage per second (250 total per attack)
- **Range**: 2000 units

### 2. **Attack Sequence**
1. **Tracking Phase** (2 seconds)
   - Cube glows **ORANGE** (charging)
   - Smoothly rotates to face player at 45°/second
   - Visual warning for player to prepare
   
2. **Firing Phase** (5 seconds)
   - Cube glows **YELLOW** (firing)
   - LineRenderer displays laser beam
   - Continuous damage to player
   - Laser sound plays (looping)
   - Beam tracks player with prediction

### 3. **Player Prediction**
- **Aim Prediction Factor**: 0.3 (30% prediction)
- Accounts for player velocity
- Predicts position based on distance and travel time
- Makes dodging possible but challenging at high speeds

### 4. **Health System**
- **Max Health**: 1000 HP
- Implements `IDamageable` interface
- Takes damage from all player weapons
- White flash effect on hit
- Death sequence with rapid flashing

### 5. **Friendly State**
- If platform is captured while cube is alive → becomes **FRIENDLY**
- Glows **GREEN** when friendly
- Stops all attacks immediately
- Cannot be damaged when friendly
- Permanent ally for that platform

---

## 🎨 Visual States

| State | Color | Behavior |
|-------|-------|----------|
| **Hostile Idle** | Red | Slow rotation, pulsing glow |
| **Tracking** | Orange | Fast rotation, tracking player |
| **Firing** | Yellow | Laser beam active, intense glow |
| **Friendly** | Green | Slow rotation, peaceful glow |
| **Hit** | White Flash | Brief flash on damage |
| **Death** | Rapid Flash | White/Black flashing before destruction |

---

## 🎨 UI Integration

### Health Slider
- **Displays in same UI** as capture progress
- **Shows cube health** in real-time (0-100%)
- **Color-coded**:
  - Green → Red gradient (based on health)
  - Cyan-green when friendly
- **Auto-hides** when cube dies or player leaves platform
- **Updates every frame** while visible

### Setup
1. Create second slider in capture UI
2. Assign to **PlatformCaptureUI** component
3. System handles show/hide automatically
4. See **AAA_TOWER_PROTECTOR_UI_SETUP.md** for detailed guide

---

## 🔊 Audio Integration

### Sound Event
- **`soundEvents.towerLaserShoot`** - Looping laser sound
- Plays attached to cube transform
- 5-second duration (matches laser duration)
- Automatically stops when laser ends

### Setup in Unity
1. Open **SoundEvents** ScriptableObject
2. Find **"► ENVIRONMENT: Towers"** section
3. Assign audio clip to **"Tower Laser Shoot"** field
4. Recommended: Sci-fi laser beam sound with intensity

---

## ⚙️ Inspector Settings

### Health System
- **Max Health**: 1000 (default)
  - Adjust based on difficulty
  - 1000 HP = ~20 seconds of sustained fire

### Laser Attack
- **Laser Interval**: 15 seconds
  - Time between attacks
  - Gives player breathing room
  
- **Laser Duration**: 5 seconds
  - How long each attack lasts
  - Balance between threat and fairness
  
- **Damage Per Second**: 50
  - Total damage per attack = 250
  - Adjust for difficulty scaling
  
- **Tracking Speed**: 45°/second
  - How fast cube rotates to face player
  - Lower = easier to dodge
  
- **Aim Prediction**: 0.3
  - 0 = no prediction (easy)
  - 1 = full prediction (very hard)
  - 0.3 = balanced for 900 units/s movement

### Visual Settings
- **Laser Width**: 2 units
- **Laser Max Range**: 2000 units
- **Laser Color**: Red (customizable)
- **Glow Intensity**: 2.0

---

## 🎮 Gameplay Design

### Player Speed Considerations
- **Normal Movement**: ~900 units/second
- **Sprint**: ~1800 units/second
- **Prediction Factor**: 0.3 allows skilled dodging
- **Warning Phase**: 2 seconds to react

### Strategic Depth
1. **Kill Before Capture**: Destroy cube for safety
2. **Capture While Alive**: Keep cube as friendly ally
3. **Risk vs Reward**: Friendly cube provides visual landmark

### Balancing
- **15-second interval**: Enough time to recover/reposition
- **2-second warning**: Fair reaction time
- **5-second duration**: Long enough to be threatening, short enough to survive
- **Prediction**: Makes it challenging but not impossible to dodge

---

## 🔗 Integration with Platform Capture System

### Setup
1. Drag **SkullSpawnerCube** into scene
2. Position on top of Central Tower
3. In **PlatformCaptureSystem** Inspector:
   - Assign cube to **"Tower Protector Cube"** field

### Behavior
- **During Capture**: Cube attacks normally
- **On Capture Complete**: 
  - If cube is alive → `MakeFriendly()` called
  - If cube is dead → nothing happens
  - Friendly cube glows green permanently

### Debug Visualization
- **Red line**: Hostile cube connection
- **Green line**: Friendly cube connection
- Visible in Scene view when PlatformCaptureSystem selected

---

## 🛠️ Technical Implementation

### Key Components
```csharp
public class SkullSpawnerCube : MonoBehaviour, IDamageable
```

### Core Methods
- **`LaserAttackCycle()`**: Main coroutine loop
- **`LaserAttackSequence()`**: Track → Fire sequence
- **`FireLaser()`**: Laser beam logic with damage
- **`TrackPlayer()`**: Smooth rotation toward player
- **`GetPredictedPlayerPosition()`**: Velocity-based prediction
- **`TakeDamage()`**: IDamageable implementation
- **`MakeFriendly()`**: Called by PlatformCaptureSystem
- **`Die()`**: Death sequence and cleanup

### Beam Visual System
- Uses **MagicArsenal Arcane Beam** prefab (simple LineRenderer)
- Clean, efficient beam rendering
- Real-time tracking of player
- Raycasts for collision detection
- Auto-configured on spawn
- Proper cleanup on deactivation

---

## 📋 Setup Checklist

### In Unity Editor
- [ ] Place SkullSpawnerCube on platform
- [ ] Assign **Beam Prefab**:
  - Navigate to `Assets/MagicArsenal/Effects/Prefabs/Beams/Setup/Beam/`
  - Drag **Arcane Beam.prefab** to "Beam Prefab" field
  - (Or use Fire Beam, Lightning Beam, Frost Beam, etc. for different looks!)
- [ ] Assign **SoundEvents** ScriptableObject
- [ ] Assign laser sound clip in SoundEvents
- [ ] Link cube to **PlatformCaptureSystem**
- [ ] **Setup UI Sliders**:
  - Create Capture Progress Slider
  - Create Cube Health Slider
  - Assign both to PlatformCaptureUI component
  - See AAA_TOWER_PROTECTOR_UI_SETUP.md for details
- [ ] Adjust health/damage for difficulty
- [ ] Test laser tracking and damage
- [ ] Verify friendly state transition
- [ ] Check audio plays correctly
- [ ] Verify beam visual effects work
- [ ] Verify health slider updates correctly

### Testing
- [ ] Cube attacks every 15 seconds
- [ ] 2-second warning before laser
- [ ] Magic beam spawns with particle effects
- [ ] Laser deals damage to player
- [ ] Cube takes damage from weapons
- [ ] Health slider updates when cube takes damage
- [ ] Health slider color changes (green → red)
- [ ] Death sequence plays correctly
- [ ] Health slider hides when cube dies
- [ ] Friendly state works on capture
- [ ] Health slider turns cyan when friendly
- [ ] Sound plays and stops properly
- [ ] Beam visual effects look awesome!

---

## 🎨 Beam Visual Variants

The system supports multiple beam types from MagicArsenal! Choose the one that fits your aesthetic:

### Available Beams (Setup/Beam folder)
- **Arcane Beam** - Purple/blue mystical energy (recommended)
- **Fire Beam** - Orange/red flames
- **Lightning Beam** - Electric blue bolts
- **Frost Beam** - Icy blue beam
- **Life Beam** - Green healing energy
- **Earth Beam** - Brown/green nature

### Customization
Each beam has:
- Clean LineRenderer visual
- Unique color and style
- Efficient rendering
- Easy to swap in Inspector

Simply drag a different beam prefab to change the look!

---

## 🎯 Design Philosophy

### Fair but Deadly
- **Telegraph**: 2-second warning with color change
- **Dodgeable**: Prediction allows skilled evasion
- **Threatening**: 250 damage per attack is significant
- **Rewarding**: Keeping it alive grants friendly ally

### Player Agency
- **Choice 1**: Kill it for safety
- **Choice 2**: Capture platform while dodging
- **Risk/Reward**: Friendly cube = visual landmark + bragging rights

### Performance
- Single LineRenderer per cube
- Efficient raycasting
- No particle systems (optional to add)
- Coroutine-based (no Update spam)

---

## 🚀 Future Enhancements (Optional)

### Visual Polish
- Particle effects on laser impact
- Beam glow shader with bloom
- Charge-up particle ring
- Death explosion effect

### Audio Enhancements
- Charge-up sound (2-second warning)
- Laser impact sound on hit
- Friendly conversion sound
- Death explosion sound

### Gameplay Additions
- Multiple cubes per platform
- Cube health bar UI
- Damage numbers on hit
- XP reward for killing cube

---

## 🎮 Player Experience

### First Encounter
1. Player lands on platform
2. Cube starts glowing orange
3. Cube rotates to face player
4. **"OH SHIT!"** moment
5. Laser fires - player dodges or tanks
6. Player learns the pattern

### Mastery
- Skilled players dodge with sprint
- Strategic positioning behind cover
- Quick kills with high DPS weapons
- Risk-taking for friendly cube reward

---

## 💡 Tips for Level Design

### Placement
- **Top of Central Tower**: Classic, visible
- **Platform Edge**: More challenging
- **Multiple Cubes**: Extreme difficulty

### Cover Design
- Add pillars/structures for cover
- Balance open space for movement
- Consider laser range (2000 units)

### Difficulty Scaling
- **Easy**: 1 cube, low damage, slow tracking
- **Medium**: 1 cube, default settings
- **Hard**: 2 cubes, high damage, fast tracking
- **Insane**: 3+ cubes, overlapping attacks

---

## 🐛 Troubleshooting

### Laser Beam Not Visible
- Verify **Magic Beam Prefab** is assigned in Inspector
- Check that ArcaneBeamStatic.prefab exists in MagicArsenal folder
- Look for "LaserEmitPoint" child object on cube
- Check console for beam spawn messages

### Beam Looks Wrong
- Try different beam variants (Fire, Lightning, etc.)
- Verify beam prefab has MagicBeamStatic component
- Check beam length setting (default 2000)

### No Damage to Player
- Player must have `PlayerHealth` component
- Player must have "Player" tag
- Check raycast is hitting player collider

### Cube Not Friendly on Capture
- Verify cube assigned in PlatformCaptureSystem
- Check `isFriendly` flag in Inspector
- Ensure cube is alive when capture completes

### Sound Not Playing
- Assign SoundEvents ScriptableObject
- Assign audio clip to `towerLaserShoot`
- Check audio source settings in SoundSystemCore

---

## 📊 Stats Summary

| Property | Value | Notes |
|----------|-------|-------|
| Health | 1000 HP | ~20s sustained fire to kill |
| Attack Interval | 15s | Time between attacks |
| Warning Duration | 2s | Tracking phase |
| Laser Duration | 5s | Firing phase |
| Damage/Second | 50 | 250 total per attack |
| Tracking Speed | 45°/s | Rotation speed |
| Aim Prediction | 0.3 | 30% velocity prediction |
| Laser Range | 2000 units | Maximum beam distance |

---

## ✅ SYSTEM COMPLETE

**Status**: ✅ Fully Implemented  
**Files Modified**: 
- `SkullSpawnerCube.cs` - Complete rewrite
- `SoundEvents.cs` - Added `towerLaserShoot`
- `PlatformCaptureSystem.cs` - Added friendly cube logic

**Ready for**: 
- Unity Inspector setup
- Audio clip assignment
- Gameplay testing
- Visual polish (optional)

---

**The world will love this feature! 🎮🔥**

*Created with precision and care for an AAA experience.*
