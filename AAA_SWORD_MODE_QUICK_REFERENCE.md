# ⚔️ SWORD MODE QUICK REFERENCE

## 🎮 Controls
- **Backspace**: Toggle sword mode ON/OFF (right hand only)
- **Right Mouse Button (RMB)**: Sword attack (when in sword mode)
- **Left Mouse Button (LMB)**: Still shoots normally (dual-wielding!)

---

## 📁 Files Created/Modified

### New Files:
1. `Assets/scripts/SwordDamage.cs` - Sword damage component

### Modified Files:
1. `Assets/scripts/Controls.cs` - Added SwordModeToggle key
2. `Assets/scripts/InputSettings.cs` - Added swordModeToggle setting
3. `Assets/scripts/PlayerShooterOrchestrator.cs` - Added sword mode system
4. `Assets/scripts/IndividualLayeredHandController.cs` - Added TriggerSwordAttack()

---

## ⚡ Quick Setup (5 Steps)

### 1. Create Sword GameObject
```
- Right-click in Hierarchy → Create Empty
- Name: "PlayerSword"
- Parent it to Right Hand bone
- Add mesh/visual
```

### 2. Add SwordDamage Component
```
- Select PlayerSword
- Add Component → SwordDamage
- Set Damage: 50
- Set Radius: 2
- Set Layer Mask: Enemy, gems, etc.
```

### 3. Connect to PlayerShooterOrchestrator
```
- Select Player GameObject
- Find PlayerShooterOrchestrator component
- Drag PlayerSword into "Sword Damage" field
```

### 4. Setup Animator (Choose One)

**Option A - Quick (Works Immediately)**
- Do nothing! Uses existing animations

**Option B - Full (Custom Animation)**
- Open Right Hand Animator
- Add trigger: "SwordAttackT"
- Create state: "SwordAttack"
- Add transition: Any State → SwordAttack (condition: SwordAttackT)
- Add Animation Event: DealDamage() at impact frame

### 5. Test It!
```
1. Press Play
2. Press Backspace (Console: "SWORD MODE ACTIVATED")
3. Press RMB (Sword attack animation plays)
4. Attack near enemy (Enemy takes damage)
5. Press Backspace again (Back to shooting mode)
```

---

## 🎯 Inspector Settings

### SwordDamage Component:
```
Damage: 50                    ← Damage per hit
Damage Radius: 2              ← Sphere detection range
Damage Layer Mask: Everything ← What can be damaged
Attack Cooldown: 0.5          ← Seconds between attacks
Show Debug Sphere: ✓          ← Visualize range (editor only)
```

### PlayerShooterOrchestrator:
```
Sword Mode System:
  Is Sword Mode Active: (readonly)
  Sword Damage: [PlayerSword] ← Your sword GameObject
```

---

## 🔧 Common Customizations

### Change Damage:
```csharp
SwordDamage → Damage = 100
```

### Change Range:
```csharp
SwordDamage → Damage Radius = 3
```

### Change Speed:
```csharp
SwordDamage → Attack Cooldown = 0.3
```

### Change Toggle Key:
```csharp
InputSettings → Sword Mode Toggle = KeyCode.F
```

---

## 🐛 Quick Troubleshooting

| Problem | Solution |
|---------|----------|
| No damage | Check SwordDamage assigned in PlayerShooterOrchestrator |
| Instant damage | Add Animation Event to call DealDamage() at impact frame |
| Can't toggle | Check Console for "SWORD MODE ACTIVATED" message |
| Left hand stops | BUG! Only secondary (RMB) should check sword mode |
| No animation | Add "SwordAttackT" trigger to Right Hand Animator |

---

## 📊 Key Methods

### SwordDamage.cs:
```csharp
DealDamage()    ← Called by animation event
TryAttack()     ← Check if ready to attack
IsReady()       ← Check cooldown status
```

### PlayerShooterOrchestrator.cs:
```csharp
ToggleSwordMode()     ← Backspace key toggles mode
TriggerSwordAttack()  ← RMB triggers attack
IsSwordModeActive     ← Check current mode (bool)
```

### IndividualLayeredHandController.cs:
```csharp
TriggerSwordAttack()  ← Plays sword animation
```

---

## 🎨 Animation Event Setup

### Critical Step (For Perfect Timing):
1. Select sword attack animation clip
2. Open Animation window
3. Add event at impact frame
4. Function: `DealDamage`
5. Save

**Without this**: Damage happens instantly when you click  
**With this**: Damage happens when sword visually hits

---

## 💡 Pro Tips

✅ **Enable Debug Sphere** - See attack range in Scene view  
✅ **Test incrementally** - Toggle → Animation → Damage  
✅ **Watch Console** - Look for "SWORD ATTACK TRIGGERED!"  
✅ **Start simple** - Get basic attack working first  
✅ **Add VFX later** - Sword trail, impact sparks, camera shake  

---

## 🚀 System Flow

```
[Backspace Pressed]
    ↓
Toggle Sword Mode
    ↓
[RMB Clicked]
    ↓
TriggerSwordAttack()
    ↓
Animator plays "SwordAttack"
    ↓
Animation Event → DealDamage()
    ↓
Sphere overlap check
    ↓
Apply damage to enemies
```

---

## ✨ Features

✅ **Ultra Simple** - No bloat, just 3 core methods  
✅ **Right Hand Only** - Left hand keeps shooting  
✅ **Animation Driven** - Damage synced to animation  
✅ **Configurable** - All settings in Inspector  
✅ **Debug Friendly** - Visual sphere, console logs  
✅ **Performance** - Event-based, no Update() spam  

---

## 📞 Debug Commands

### Check Mode:
```csharp
Debug.Log(PlayerShooterOrchestrator.Instance.IsSwordModeActive);
```

### Force Toggle:
```csharp
// Call in Inspector or Debug Console
PlayerShooterOrchestrator.Instance.ToggleSwordMode();
```

### Test Damage:
```csharp
// Call directly
swordDamage.DealDamage();
```

---

**Version**: 1.0  
**Status**: ✅ READY TO USE  
**No Bloat**: 100% clean code  

**Happy Slashing! 🗡️**
