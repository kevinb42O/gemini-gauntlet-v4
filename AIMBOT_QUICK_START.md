# ⚡ AIMBOT SNAP MODE - QUICK START GUIDE

## 🎯 YOUR AIMBOT NOW HAS CAMERA SNAP!

Just like **EngineOwning's Call of Duty aimbot**, your camera now **snaps instantly** to enemies!

---

## 🚀 QUICK SETUP (30 seconds)

### Step 1: Find Your Aimbot Component
1. Open Unity
2. Select your **Player** GameObject
3. Find the `AAASmartAimbot` component in Inspector

### Step 2: Enable Snap Mode
1. Change **Aim Mode** dropdown to `Snap`
2. Set **Snap Speed** to `25` (default - very fast)
3. Set **Snap Threshold** to `0.5` (tight lock)

### Step 3: Test It
1. Press **Play**
2. Press **F11** to enable aimbot
3. Look near an enemy
4. Watch your camera **SNAP** to them instantly! ⚡

---

## 🎮 CONTROLS

| Key | Action |
|-----|--------|
| **F11** | Toggle aimbot ON/OFF |
| **F1** | Open cheat menu (if using cheat manager) |

---

## ⚙️ RECOMMENDED SETTINGS

### For EngineOwning CoD Style:
```
Aim Mode:           Snap
Snap Speed:         25-50
Snap Threshold:     0.5°
Aimbot FOV:         90°
Target Bone:        Head (for headshots)
Auto Fire:          Enabled (optional)
```

### Inspector Screenshot:
```
┌─────────────────────────────────────┐
│ AAASmartAimbot                      │
├─────────────────────────────────────┤
│ ✅ Aimbot Enabled                   │
│                                     │
│ === AIM SETTINGS ===                │
│ Aim Mode:        [Snap ▼]          │
│ Aim Smoothness:  15                 │
│ Snap Speed:      25 ████████        │
│ Snap Threshold:  0.5 ██             │
│ Aimbot FOV:      90 █████████       │
│ Max Aim Distance: 15000             │
│ Target Bone:     [Head ▼]          │
│                                     │
│ === SMART TARGETING ===             │
│ ✅ Prioritize Crosshair Proximity   │
│ ✅ Prioritize Low Health            │
│ ✅ Prioritize Distance              │
│ ✅ Require Line Of Sight            │
│                                     │
│ === VISUAL FEEDBACK ===             │
│ ✅ Draw Debug Line                  │
│ ✅ Draw FOV Cone                    │
└─────────────────────────────────────┘
```

---

## 🔥 WHAT YOU'LL SEE

### When Aimbot Activates:
1. **Red line** appears from camera to target
2. Camera **snaps instantly** to enemy
3. Console shows: `⚡ SNAP LOCKED on Enemy!`
4. Camera **tracks perfectly** as enemy moves

### Visual Indicators:
- **Yellow cone**: Your aimbot FOV
- **Red line**: Aim line to current target
- **Console logs**: Lock confirmations

---

## 📊 SNAP MODE vs SMOOTH MODE

| Feature | Snap Mode | Smooth Mode |
|---------|-----------|-------------|
| **Speed** | ⚡ Instant | 🎯 Gradual |
| **Feel** | EngineOwning CoD | Legit player |
| **Lock** | Perfect (0°) | Close (±2°) |
| **Time** | 0.1 seconds | 1.0 seconds |

---

## 🎯 HOW IT WORKS

```
1. Enemy enters FOV → 🎯
2. Aimbot selects target → 🔍
3. Camera SNAPS to target → ⚡
4. Perfect lock achieved → 🔒
5. Tracks movement → 📍
```

### Code Flow:
```csharp
if (angleToTarget > 0.5°)
{
    // Fast snap towards target
    RotateTowards(target, 2500°/second);
}
else
{
    // PERFECT LOCK - instant snap
    camera.rotation = targetRotation;
}
```

---

## 🔧 TROUBLESHOOTING

### Camera not snapping?
- ✅ Check `aimbotEnabled = true`
- ✅ Check `Aim Mode = Snap`
- ✅ Ensure enemies are within `Aimbot FOV`
- ✅ Check `Max Aim Distance` is high enough

### Snap too slow?
- ⬆️ Increase `Snap Speed` (try 40-50)

### Snap too fast/obvious?
- ⬇️ Decrease `Snap Speed` (try 15-20)

### Not locking perfectly?
- ⬇️ Decrease `Snap Threshold` (try 0.1°)

---

## 💡 PRO TIPS

### Tip 1: Adjust FOV for playstyle
- **Tight FOV (45°)**: Only snaps to enemies you're looking at (more legit)
- **Wide FOV (180°)**: Snaps to enemies anywhere (rage mode)

### Tip 2: Target bone matters
- **Head**: Headshots (harder to hit but more damage)
- **Chest**: Center mass (easier, consistent)

### Tip 3: Combine with auto-fire
- Enable `Auto Fire` for full rage mode
- Disable for semi-legit (you control when to shoot)

### Tip 4: Use prediction
- Enable `Use Prediction` for moving targets
- Aimbot will lead shots automatically

---

## 🎮 INTEGRATION WITH YOUR GAME

### Via Code:
```csharp
// Enable snap mode
AAASmartAimbot.Instance.SetAimMode(AAASmartAimbot.AimMode.Snap);

// Enable aimbot
AAASmartAimbot.Instance.SetAimbotEnabled(true);

// Toggle between modes
AAASmartAimbot.Instance.ToggleAimMode();

// Check if locked on target
if (AAASmartAimbot.Instance.IsOnTarget())
{
    Debug.Log("LOCKED ON!");
}
```

### Via Cheat Manager:
```csharp
// Unlock aimbot (costs 1200 points)
AAACheatManager.Instance.UnlockCheat("aimbot");

// Toggle aimbot
AAACheatManager.Instance.ToggleCheat("aimbot");
```

---

## ✅ COMPLETE FEATURE LIST

Your aimbot now has:
- ✅ **Snap Mode** - Instant camera snap (EngineOwning style)
- ✅ **Smooth Mode** - Human-like aim
- ✅ **Smart Targeting** - Prioritizes best target
- ✅ **Velocity Prediction** - Leads moving targets
- ✅ **Bullet Drop Compensation** - Adjusts for gravity
- ✅ **Auto-Fire** - Shoots when locked
- ✅ **Human Error** - Adds realism (smooth mode only)
- ✅ **FOV Limiting** - Only targets in cone
- ✅ **Line of Sight** - Ignores enemies behind walls
- ✅ **Debug Visualization** - Shows aim line and FOV

---

## 🎯 RESULT

**Your aimbot now works EXACTLY like EngineOwning's Call of Duty aimbot!**

- Camera snaps instantly to enemies ⚡
- Perfect lock when on target 🔒
- Configurable snap speed ⚙️
- Toggle between snap and smooth modes 🔄

**Press F11 and watch the magic happen!** 🎮

---

## 📚 MORE INFO

- **Full Guide**: See `AIMBOT_SNAP_MODE_GUIDE.md`
- **Visual Comparison**: See `AIMBOT_MODE_COMPARISON.md`
- **Code**: `Assets/scripts/AAASmartAimbot.cs`
