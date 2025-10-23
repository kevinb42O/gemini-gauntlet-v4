# 🎪 AERIAL TRICK XP SYSTEM - EXTRAORDINARY EDITION

## What You Get

**SICK TRICKS = MASSIVE XP!**

The longer you're in the air, the more you spin, the MORE XP YOU GET!

## Setup (10 Seconds!)

1. **Add to scene:**
   - Create GameObject: `AerialTrickXPSystem`
   - Add component: `AerialTrickXPSystem`
   - Done!

2. **Test:**
   - Jump and do tricks (middle mouse button)
   - Spin like crazy!
   - Land → SEE MASSIVE TEXT WITH YOUR STATS!

## How XP is Calculated

### Base Formula
```
XP = (Base + Airtime XP + Rotation XP) × Combo Multiplier + Perfect Bonus

Base XP: 10
Airtime XP: 5 per second
Rotation XP: 15 per 360° rotation (any axis)
Combo Multiplier: 1.5x per additional axis (2 axes = 1.5x, 3 axes = 2.25x!)
Perfect Landing Bonus: +50 XP
```

### Examples

**Small Trick:**
- 1 second airtime
- 1× rotation (360°)
- 1 axis
- XP: 10 + 5 + 15 = **30 XP**

**Medium Trick:**
- 2 seconds airtime
- 2× rotations (720°)
- 2 axes (combo!)
- XP: (10 + 10 + 30) × 1.5 = **75 XP**

**BIG Trick:**
- 3 seconds airtime
- 3× rotations (1080°)
- 2 axes
- Perfect landing!
- XP: (10 + 15 + 45) × 1.5 + 50 = **155 XP**

**GODLIKE Trick:**
- 4 seconds airtime
- 5× rotations (1800°)
- 3 axes (CHAOS!)
- Perfect landing!
- XP: (10 + 20 + 75) × 2.25 + 50 = **286 XP!!!**

## Visual Feedback (EXTRAORDINARY!)

### Text Shows:
```
🔥 GODLIKE TRICK! 🔥
4.2s AIRTIME
5× ROTATIONS
3-AXIS COMBO!
⭐ PERFECT LANDING! ⭐

+286 XP
```

### Colors by Awesomeness:
- **Small tricks:** Light Green (nice!)
- **Medium tricks:** Cyan (getting good!)
- **Big tricks:** Neon Pink (impressive!)
- **Insane tricks:** Hot Orange (SICK!)
- **Godlike tricks:** Hot Pink (LEGENDARY!)
- **Perfect landing:** Gold (FLAWLESS!)

### Text Size:
- **Scales with trick size!**
- Small trick: Normal size
- Big trick: 2x larger
- Godlike trick: 3x larger!
- **YOU CAN'T MISS IT!**

## Trick Categories

### By Airtime:
- **< 1s:** Small
- **1-1.5s:** Medium
- **1.5-2s:** Big
- **2-3s:** Insane
- **3s+:** GODLIKE

### By Rotations:
- **< 1×:** Small
- **1-2×:** Medium
- **2-3×:** Big
- **3-4×:** Insane
- **4×+:** GODLIKE

### By Axes:
- **1 axis:** Regular trick
- **2 axes:** COMBO! (1.5x multiplier)
- **3 axes:** CHAOS SPIN! (2.25x multiplier)

## Perfect Landing Bonus

**+50 XP for landing clean!**

Landing is "perfect" if camera deviation < 25° from upright.

**Tips for perfect landings:**
- Level out before landing
- Don't land upside down!
- Smooth = bonus XP!

## Inspector Controls

### XP Calculation:
- `Base Air XP`: 10 (base reward)
- `XP Per Second Airtime`: 5 (time in air)
- `XP Per 360 Rotation`: 15 (spinning)
- `Combo Multiplier`: 1.5 (multi-axis bonus)
- `Perfect Landing Bonus`: 50 (clean landing)

### Thresholds:
- `Min Airtime For XP`: 0.5s (must be in air this long)
- `Min Rotation For Trick`: 180° (half rotation minimum)
- `Clean Landing Threshold`: 25° (max deviation for perfect)

### Visual:
- `Spawn Distance In Front`: 3000 (where text appears)
- `Text Size Multiplier`: 6.0 (BIGGER than wall jumps!)

### Colors:
- All 6 colors customizable in Inspector!

## Integration

✅ **Already integrated into AAACameraController!**
- Triggers on landing
- Calculates airtime automatically
- Tracks all rotations
- Detects perfect landings
- Awards XP through XPManager
- Shows floating text automatically

**Just add the script and DO SICK TRICKS!** 🎪

## Session Stats

Track your performance:
- Total tricks landed
- Total XP from tricks
- Biggest trick XP

Access via:
```csharp
var (tricks, xp, biggest) = AerialTrickXPSystem.Instance.GetSessionStats();
```

## Performance

✅ **Optimized for potato PCs!**
- No debug logs (unless enabled)
- Locked rotation (no continuous updates)
- Uses existing FloatingTextManager
- Zero GC allocations

## Pro Tips

### Max XP Strategy:
1. **Stay in air LONG** (time = XP!)
2. **Spin on MULTIPLE axes** (combo multiplier!)
3. **Do FULL rotations** (360° = 15 XP each!)
4. **Land CLEAN** (+50 XP bonus!)

### Example Combo:
1. Jump high
2. Backflip (X axis)
3. Spin (Y axis)
4. Barrel roll (Z axis)
5. Level out before landing
6. **GODLIKE TRICK + PERFECT LANDING = MASSIVE XP!**

**GO DO SOME SICK TRICKS AND GET THAT XP!** 🚀🎪
