# 🔥 COMBO SYSTEM - QUICK SETUP (5 Minutes!)

## ⚡ What You're Getting

**INSANE combo system** that multiplies XP when you chain wall jumps + tricks together!

- Wall Jump → Trick → Wall Jump = **2.75x XP!** 🔥
- Different sounds for different trick sizes
- Combo notification sounds that get higher pitched with bigger multipliers
- Visual feedback showing your combo multiplier

## 🎯 Setup Steps

### Step 1: Add ComboMultiplierSystem (30 seconds)
1. In Unity Hierarchy, **Right-click** → **Create Empty**
2. Name it: `ComboMultiplierSystem`
3. **Add Component** → Search for `ComboMultiplierSystem`
4. Done! (Default settings are perfect)

### Step 2: Assign Trick Sounds (2 minutes)
1. Open `Assets/audio/AudioMixer/SoundEvents.asset`
2. Scroll to **"► PLAYER: Aerial Tricks"**
3. Assign sounds to these fields:

**Required (for trick landings):**
- **Trick Landing Small**: Light impact sound
- **Trick Landing Medium**: Medium impact
- **Trick Landing Big**: Heavy impact
- **Trick Landing Insane**: Epic crash
- **Trick Landing Godlike**: LEGENDARY impact

**Optional (bonus sounds):**
- **Perfect Landing Bonus**: Chime/ding for perfect landings
- **Combo Multiplier Sound**: Notification/achievement sound

**Don't have sounds?** Use existing sounds temporarily:
- Copy from `landSounds` array
- Copy from `jumpSounds` array
- Any impact/notification sound works!

### Step 3: Enable Audio (10 seconds)
1. Find **AerialTrickXPSystem** in your scene
2. Check **"Enable Audio"** ✅

(WallJumpXPSimple should already have audio enabled)

### Step 4: TEST! (2 minutes)
1. Enter Play Mode
2. Do a **wall jump** → See XP
3. Do an **aerial trick** → Hear trick landing sound!
4. Do **wall jump → trick → wall jump** → See **COMBO x2.75!** 🔥

## 🎮 How It Works

### Combo Points
- **Wall Jump**: +1.0 points
- **Aerial Trick**: +2.0 points (bigger tricks = more!)

### Multiplier
- **Formula**: 1.0 + (points × 0.25)
- **Examples**:
  - 2 points = 1.5x XP
  - 4 points = 2.0x XP
  - 8 points = 3.0x XP
  - 12 points = 4.0x XP

### Combo Window
- **3 seconds** to continue the combo
- Chain moves quickly to keep it going!

## 🎵 Sound Tiers

The system automatically picks the right sound:

| Tier | Airtime | Rotations | Sound |
|------|---------|-----------|-------|
| Small | < 1s | < 1 | Light |
| Medium | 1-1.5s | 1 | Medium |
| Big | 1.5-2s | 2 | Heavy |
| Insane | 2-3s | 3 | Epic |
| Godlike | > 3s | 4+ | LEGENDARY |

## 🔥 Example Combos

### Beginner Combo
```
Wall Jump → Wall Jump → Wall Jump
3 points = 1.75x multiplier
```

### Intermediate Combo
```
Wall Jump → Trick → Wall Jump
4 points = 2.0x multiplier
```

### Advanced Combo
```
Trick → Wall Jump → Trick → Wall Jump
7 points = 2.75x multiplier
```

### GODLIKE COMBO
```
Big Trick → Wall Jump x3 → Insane Trick
13 points = 4.25x multiplier! 💥
```

## 🎯 Visual Feedback

**Wall Jump with Combo:**
```
CHAIN x2!
+10 XP
🔥 COMBO x1.5! 🔥
```

**Trick with Combo:**
```
💫 BIG TRICK! 💫
2.0s AIRTIME
2× ROTATIONS

🔥 COMBO x2.5! 🔥

+125 XP
```

## 🔧 Troubleshooting

### No Combo Showing?
- Check `ComboMultiplierSystem` is in scene
- Combo must be > 1.0x to display
- Try: Wall jump → Trick (should show combo!)

### No Trick Sounds?
- Assign sounds in `SoundEvents.asset`
- Check "Enable Audio" in `AerialTrickXPSystem`
- Look for debug logs in Console

### Combo Resets Too Fast?
- In `ComboMultiplierSystem`, increase `Combo Time Window` to 4-5 seconds

## ✅ That'ssss It!

You now have an **INSANE combo system** that makes movement feel INCREDIBLE!

**Try this:**
1. Wall jump up a wall
2. Do a flip at the top
3. Wall jump again on the way down
4. Watch your XP EXPLODE! 🚀

---

For full documentation, see: `AAA_COMBO_MULTIPLIER_SYSTEM.md`
