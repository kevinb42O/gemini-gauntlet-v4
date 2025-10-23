# 💰 XP & Damage Text Integration - Complete!

## What I Added

Your enemies now have **full XP and damage text integration**:

### 1. 💥 Floating Damage Text
- Shows **"-XXXhp"** when you hit enemy
- Red color
- Floats upward and fades
- Uses your existing FloatingTextManager

### 2. 💰 XP Granting on Death
- Automatically grants XP when enemy dies
- Uses your existing XPGranter component
- Integrates with XPManager
- No duplicate XP (checks HasGrantedXP)

---

## 🎮 Setup (2 Minutes)

### Step 1: Add XPGranter Component

On your enemy GameObject:
```
1. Add Component → XPGranter
2. Configure:
   ├─ XP Amount: 15 (or whatever you want)
   ├─ XP Category: Enemy
   ├─ Grant On Destroy: TRUE
   └─ Enable Debug Logs: TRUE (optional)
```

### Step 2: Ensure FloatingTextManager Exists

Make sure you have FloatingTextManager in your scene:
```
- GameObject with FloatingTextManager component
- It will auto-create canvas and prefab if needed
- No additional setup required!
```

### That's It!
The integration is **automatic** - no additional code needed!

---

## 🔍 How It Works

### When You Hit Enemy:
```csharp
CompanionCore.TakeDamage(amount, hitPoint, hitDirection)
    ↓
ShowDamageText(amount, hitPoint)
    ↓
FloatingTextManager.ShowFloatingText("-285hp", hitPoint, Color.red)
    ↓
Red text floats up and fades
```

### When Enemy Dies:
```csharp
CompanionCore.Die()
    ↓
GrantXPOnDeath()
    ↓
XPGranter.GrantXPManually("Enemy Death")
    ↓
XPManager.GrantXP(amount, "Enemies", enemyName)
    ↓
Player gets XP!
```

---

## 📊 What You'll See

### Damage Text:
```
-285hp  ← Red text, floats up
-150hp
-420hp
```

### Console Output (when enemy dies):
```
[CompanionCore] 💰 Granted 15 XP for killing EnemyCompanion
[XPGranter] Granting 15 XP from 'EnemyCompanion' (Category: Enemy, Reason: Enemy Death)
[XPManager] Granted 15 XP to category 'Enemies' from 'EnemyCompanion'
```

---

## 🔧 Customization

### Change XP Amount:
```
XPGranter component:
└─ XP Amount: 15 → Change to whatever you want
```

### Change Damage Text Color:
In `CompanionCore.cs` line 352:
```csharp
FloatingTextManager.Instance.ShowFloatingText(damageText, hitPoint, Color.red);
                                                                    ↑ Change color
```

### Change Damage Text Format:
In `CompanionCore.cs` line 351:
```csharp
string damageText = $"-{Mathf.RoundToInt(damage)}hp";
                     ↑ Change format (e.g., "-{damage} DMG")
```

---

## ✅ Features

### Damage Text:
- ✅ Shows on every hit
- ✅ Red color (customizable)
- ✅ Rounds to nearest integer
- ✅ Format: "-XXXhp"
- ✅ Floats upward
- ✅ Fades out
- ✅ Faces camera
- ✅ Works in world space

### XP System:
- ✅ Grants XP on death
- ✅ Only grants once (no duplicates)
- ✅ Integrates with XPManager
- ✅ Categorized as "Enemies"
- ✅ Configurable amount
- ✅ Debug logging
- ✅ Works with existing XP system

---

## 🐛 Troubleshooting

### "No damage text appears"
**Check:**
1. FloatingTextManager exists in scene?
2. Console shows any errors?
3. Enable `debugMode` in FloatingTextManager
4. Check console for floating text logs

### "No XP granted"
**Check:**
1. XPGranter component added to enemy?
2. XPManager exists in scene?
3. Enable `enableDebugLogs` in XPGranter
4. Check console for XP logs

### "Damage text too small/big"
**Adjust:**
```
FloatingTextManager:
├─ Text Size: 48 (default)
└─ World Scale Multiplier: 200 (default)
```

### "XP granted multiple times"
**This shouldn't happen** - XPGranter checks `HasGrantedXP`
If it does, check console for duplicate death calls

---

## 📝 Code Changes Summary

### CompanionCore.cs:
```csharp
// Added namespaces
using GeminiGauntlet.Progression;
using GeminiGauntlet.UI;

// In TakeDamage():
ShowDamageText(amount, hitPoint); // ✅ Shows damage text

// New method:
private void ShowDamageText(float damage, Vector3 hitPoint)
{
    if (FloatingTextManager.Instance != null)
    {
        string damageText = $"-{Mathf.RoundToInt(damage)}hp";
        FloatingTextManager.Instance.ShowFloatingText(damageText, hitPoint, Color.red);
    }
}

// In Die():
GrantXPOnDeath(); // ✅ Grants XP

// New method:
private void GrantXPOnDeath()
{
    XPGranter xpGranter = GetComponent<XPGranter>();
    if (xpGranter != null && !xpGranter.HasGrantedXP)
    {
        xpGranter.GrantXPManually("Enemy Death");
    }
}
```

---

## 🎯 Integration Points

### Works With:
- ✅ XPManager (your existing XP system)
- ✅ FloatingTextManager (your existing text system)
- ✅ XPGranter (your existing granter component)
- ✅ CompanionCore (enemy health system)
- ✅ IDamageable interface (damage system)

### Doesn't Break:
- ✅ Existing damage system
- ✅ Existing XP system
- ✅ Existing floating text
- ✅ Enemy AI behavior
- ✅ Death system
- ✅ Hit effects

---

## 🎮 Example Setup

### Enemy GameObject:
```
EnemyCompanion
├─ CompanionCore (health: 100)
├─ XPGranter (amount: 15, category: Enemy) ← ADD THIS
├─ EnemyCompanionBehavior
├─ CompanionMovement
├─ CompanionCombat
├─ Rigidbody
├─ Collider
└─ NavMeshAgent
```

### Scene:
```
Scene
├─ Player
├─ Enemies (with XPGranter)
├─ XPManager ← Must exist
└─ FloatingTextManager ← Must exist
```

---

## ✅ Summary

**What you get:**
1. ✅ **Damage text** shows on every hit ("-285hp")
2. ✅ **XP granted** when enemy dies (15 XP default)
3. ✅ **Full integration** with existing systems
4. ✅ **No breaking changes** to current functionality
5. ✅ **Easy to configure** (just add XPGranter component)

**What you need to do:**
1. Add `XPGranter` component to enemies
2. Set XP amount (default 15)
3. Ensure `XPManager` and `FloatingTextManager` exist in scene
4. Test and enjoy!

**Your enemies now give XP and show damage! 💰💥**
