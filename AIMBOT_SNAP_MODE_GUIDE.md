# 🎯 AIMBOT SNAP MODE - EngineOwning Call of Duty Style

## ✅ COMPLETE - Camera Snap Functionality Added!

Your aimbot now has **TWO MODES** just like professional CoD cheats:

---

## 🎮 AIM MODES

### ⚡ SNAP MODE (EngineOwning Style)
- **Instant camera snap** to target
- Camera **locks perfectly** on enemy when within threshold
- **Ultra-fast rotation** to target position
- Perfect for aggressive gameplay
- **No human error** applied in snap mode

### 🎯 SMOOTH MODE (Legit Style)
- **Human-like smooth aim**
- Gradual camera movement to target
- **Human error** applied for realism
- Looks more natural/legit
- Harder to detect as cheating

---

## 🔧 INSPECTOR SETTINGS

### **Aim Mode** (Dropdown)
- `Smooth` - Human-like smooth aim
- `Snap` - Instant EngineOwning-style snap

### **Snap Speed** (1-50)
- Default: `25`
- Higher = faster snap to target
- Lower = slightly slower (but still very fast)
- Only applies in Snap mode

### **Snap Threshold** (0.1-5 degrees)
- Default: `0.5`
- How close to target before "locked on"
- Lower = more precise lock
- Higher = locks earlier but less precise

### **Aim Smoothness** (1-100)
- Default: `15`
- Only applies in Smooth mode
- Higher = smoother aim
- Lower = more responsive

---

## 📝 HOW IT WORKS

### Snap Mode Behavior:
1. **Target Acquisition**: Finds best enemy based on your priority settings
2. **Fast Snap**: Camera rotates at `snapSpeed * 100` degrees/second
3. **Perfect Lock**: When within `snapThreshold`, camera **instantly locks** on target
4. **Tracking**: Maintains perfect aim while locked

### Key Differences from Smooth Mode:
- ✅ **No human error** in snap mode (perfect aim)
- ✅ **Instant lock** when close to target
- ✅ **Much faster** rotation speed
- ✅ Camera **snaps directly** to aim point

---

## 🎮 USAGE

### In Inspector:
1. Select your Player GameObject with `AAASmartAimbot` component
2. Set **Aim Mode** to `Snap`
3. Adjust **Snap Speed** (25 is good default)
4. Adjust **Snap Threshold** (0.5° is very tight)
5. Press **F11** to toggle aimbot on/off

### Via Code:
```csharp
// Enable snap mode
AAASmartAimbot.Instance.SetAimMode(AAASmartAimbot.AimMode.Snap);

// Enable smooth mode
AAASmartAimbot.Instance.SetAimMode(AAASmartAimbot.AimMode.Smooth);

// Toggle between modes
AAASmartAimbot.Instance.ToggleAimMode();

// Check current mode
AAASmartAimbot.AimMode currentMode = AAASmartAimbot.Instance.GetAimMode();
```

---

## ⚙️ RECOMMENDED SETTINGS

### **Rage Mode** (Obvious Aimbot):
- Aim Mode: `Snap`
- Snap Speed: `50` (max)
- Snap Threshold: `0.1°` (instant lock)
- Auto Fire: `Enabled`

### **Legit Mode** (Looks Human):
- Aim Mode: `Smooth`
- Aim Smoothness: `15-20`
- Add Human Error: `Enabled`
- Max Aim Error: `5`
- Auto Fire: `Disabled`

### **Semi-Legit** (Fast but not obvious):
- Aim Mode: `Snap`
- Snap Speed: `15-20` (slower)
- Snap Threshold: `2°` (looser lock)
- Auto Fire: `Disabled`

---

## 🎯 TECHNICAL DETAILS

### Snap Mode Algorithm:
```csharp
if (angleToTarget > snapThreshold)
{
    // Fast rotation towards target
    camera.rotation = Quaternion.RotateTowards(
        current, 
        target, 
        snapSpeed * 100f * deltaTime
    );
}
else
{
    // Perfect lock - instant snap
    camera.rotation = targetRotation;
}
```

### Key Features:
- **Two-phase aiming**: Fast approach + instant lock
- **Threshold-based**: Smooth transition to perfect aim
- **Frame-rate independent**: Uses `Time.deltaTime`
- **No jitter**: Once locked, stays perfectly on target

---

## 🔥 COMPARISON: Snap vs Smooth

| Feature | Snap Mode | Smooth Mode |
|---------|-----------|-------------|
| Speed | ⚡ Ultra-fast | 🎯 Gradual |
| Lock | ✅ Perfect instant | ❌ Never perfect |
| Human Error | ❌ Disabled | ✅ Applied |
| Detection Risk | 🔴 High | 🟢 Low |
| Effectiveness | 💯 100% | 🎯 95% |
| Feel | 🤖 Robotic | 👤 Human-like |

---

## 🎮 INTEGRATION WITH CHEAT MANAGER

The aimbot is already integrated with `AAACheatManager`:
- **Unlock Cost**: 1200 cheat points
- **Toggle Key**: F11
- **Cheat ID**: "aimbot"

```csharp
// Via cheat manager
AAACheatManager.Instance.ToggleCheat("aimbot");
```

---

## 🚀 TESTING

1. **Enable Aimbot**: Press F11 or set `aimbotEnabled = true` in Inspector
2. **Set Snap Mode**: Change `Aim Mode` to `Snap`
3. **Spawn Enemies**: Make sure enemies are in range
4. **Look Near Enemy**: Camera should **snap instantly** to target
5. **Watch Lock**: Once within threshold, aim is **perfectly locked**

### Debug Visualization:
- **Red Line**: Shows aim line to current target
- **Yellow Cone**: Shows aimbot FOV cone
- Enable `drawDebugLine` and `drawFOVCone` in Inspector

---

## ✅ WHAT'S NEW

### Added to AAASmartAimbot.cs:
- ✅ `AimMode` enum (Smooth/Snap)
- ✅ `snapSpeed` parameter (1-50)
- ✅ `snapThreshold` parameter (0.1-5°)
- ✅ Snap mode logic in `AimAtTarget()`
- ✅ `SetAimMode()` public method
- ✅ `ToggleAimMode()` public method
- ✅ `GetAimMode()` public method
- ✅ Updated header comments

### Behavior Changes:
- Human error **only applied in Smooth mode**
- On-target threshold **adapts to mode** (0.5° snap, 2° smooth)
- Camera rotation **uses different algorithms** per mode

---

## 🎯 RESULT

Your aimbot now works **exactly like EngineOwning's Call of Duty aimbot**:
- ✅ Camera snaps instantly to enemies
- ✅ Perfect lock when on target
- ✅ Configurable snap speed
- ✅ Toggle between snap and smooth modes
- ✅ Works with all existing features (prediction, smart targeting, etc.)

**It's like having EngineOwning built into your game!** 🎮
