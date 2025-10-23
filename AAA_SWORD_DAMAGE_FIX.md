# ⚔️ SWORD DAMAGE FIX - QUICK TROUBLESHOOTING

## 🎯 Problem: Sword Can't Damage Enemies/Gems

Your sword system is working, but it's not damaging `SkullEnemy.cs` or `Gem.cs`. This is almost always a **Layer Mask** issue!

---

## ✅ SOLUTION (3 Steps)

### Step 1: Check Enemy Layers
1. Select a **SkullEnemy** in your scene (Hierarchy)
2. Look at the **Inspector** panel
3. At the **very top**, find the **Layer** dropdown
4. Note what layer it's on (probably "Enemy" or "Default")
5. Do the same for a **Gem** (probably "gems" layer)

Example:
```
SkullEnemy → Layer: Enemy
Gem → Layer: gems
```

### Step 2: Configure Damage Layer Mask
1. Select your **Sword GameObject** (the one with SwordDamage component)
2. Find the **SwordDamage** component in Inspector
3. Find **Damage Layer Mask** field
4. Click the dropdown (shows layer names)
5. **CHECK the layers** you found in Step 1:
   - ✅ Enemy
   - ✅ gems
   - ✅ Default (if some enemies are on Default)

**CRITICAL**: Make sure those layers are CHECKED (blue checkmark)!

### Step 3: Test with Debug Logs
1. Press Play
2. Toggle sword mode (Backspace)
3. Attack near an enemy (RMB)
4. **Watch the Console** - you should see:

```
[SwordDamage] ⚔️ SWORD ATTACK! Position: (x,y,z), Radius: 2, Found 3 colliders
[SwordDamage] Hit collider: SkullEnemy (Layer: Enemy)
[SwordDamage] ✅ DAMAGED SKULL: SkullEnemy for 50 damage!
[SwordDamage] 🎯 Successfully damaged 1 targets!
```

---

## 🔍 What the Logs Mean

### ✅ Good Logs (Working):
```
Found 3 colliders           ← Detecting enemies
Hit collider: SkullEnemy    ← Found the enemy
✅ DAMAGED SKULL            ← Applied damage
Successfully damaged 1      ← Success!
```

### ❌ Bad Logs (Not Working):

**Case 1: Found 0 colliders**
```
Found 0 colliders
⚠️ No targets damaged
```
**Problem**: Layer Mask is WRONG or Radius too small  
**Fix**: Check Layer Mask includes enemy layers, increase Radius to 5 for testing

**Case 2: Found colliders but no damage**
```
Found 2 colliders
Hit collider: SkullEnemy (Layer: Enemy)
❌ No damageable component on SkullEnemy
⚠️ No targets damaged
```
**Problem**: Enemy missing SkullEnemy component (unlikely)  
**Fix**: Make sure enemy GameObject has SkullEnemy script attached

---

## 🎨 Visual Setup

### Correct Layer Mask Configuration:
```
SwordDamage Component
├─ Damage: 50
├─ Damage Radius: 2 (or 5 for testing)
├─ Damage Layer Mask:
│   ✅ Enemy      ← CHECK THIS for SkullEnemy
│   ✅ gems       ← CHECK THIS for Gems
│   ✅ Default    ← CHECK if enemies are on Default
│   ☐ UI
│   ☐ Water
│   ☐ etc...
├─ Attack Cooldown: 0.5
└─ Show Debug Sphere: ✓
```

---

## 🧪 Testing Steps

### 1. Enable Debug Sphere
- SwordDamage → Show Debug Sphere: ✓
- You'll see a red wireframe sphere in Scene view
- This shows the damage range

### 2. Increase Radius (Temporary)
- SwordDamage → Damage Radius: 5
- Makes it easier to test
- Reduce back to 2 once it works

### 3. Stand Next to Enemy
- Get VERY close to a SkullEnemy
- Press Backspace (sword mode)
- Press RMB (attack)
- Enemy should take damage immediately

### 4. Watch Console
- Clear Console (top-left icon)
- Attack once
- You should see ~5-10 log messages
- Check for "✅ DAMAGED SKULL" message

---

## 💡 Common Issues

| Issue | Solution |
|-------|----------|
| Found 0 colliders | Layer Mask doesn't include enemy layers |
| Found colliders, no damage | Missing SkullEnemy component |
| No console logs at all | SwordDamage.DealDamage() not being called |
| Sword swings but no logs | Animation event missing or wrong function name |

---

## 🔧 Emergency Fix

If nothing works, try this **"Everything" Layer Mask**:

1. Select Sword GameObject
2. SwordDamage component
3. Damage Layer Mask dropdown
4. Click **"Everything"** (at top)
5. Test again

**This will damage EVERYTHING in range (including player, platforms, etc.)**  
But it will prove the system works!

Once it works with "Everything", narrow it down:
1. Click "Nothing"
2. Then CHECK ONLY: Enemy, gems
3. Test again

---

## 📊 What Updated

The new `SwordDamage.cs` script now:
- ✅ Specifically checks for `SkullEnemy` component
- ✅ Specifically checks for `Gem` component
- ✅ Falls back to `IDamageable` interface
- ✅ Shows detailed debug logs
- ✅ Reports what it's hitting
- ✅ Reports successful damage

**This makes debugging 100x easier!**

---

## 🎯 Expected Behavior

### Before Fix:
```
[SwordDamage] Dealing 50 damage in radius 2 - found 0 potential targets
```
Not helpful! Can't see what's wrong.

### After Fix:
```
[SwordDamage] ⚔️ SWORD ATTACK! Position: (1.2, 0.5, 3.4), Radius: 2, Found 2 colliders
[SwordDamage] Hit collider: SkullEnemy_01 (Layer: Enemy)
[SwordDamage] ✅ DAMAGED SKULL: SkullEnemy_01 for 50 damage!
[SwordDamage] Hit collider: Platform (Layer: Default)
[SwordDamage] ❌ No damageable component on Platform
[SwordDamage] 🎯 Successfully damaged 1 targets!
```
Perfect! You can see exactly what's happening.

---

## ✅ Checklist

Before asking for help, verify:
- ☐ Sword GameObject has SwordDamage component
- ☐ Damage Layer Mask includes "Enemy" and "gems" layers
- ☐ Damage Radius is at least 2 (try 5 for testing)
- ☐ SkullEnemy has SkullEnemy component attached
- ☐ Gem has Gem component attached
- ☐ Console shows "[SwordDamage] ⚔️ SWORD ATTACK!" when you attack
- ☐ Console shows "Found X colliders" where X > 0
- ☐ You're attacking close enough to enemy

---

## 🚀 Once It Works

After you confirm damage is working:
1. Reduce Damage Radius back to 2 (or desired value)
2. Adjust Damage to balance (50 is good for testing)
3. Set Attack Cooldown to 0.5 (or faster/slower as desired)
4. Disable "Show Debug Sphere" (optional - it's only visible in editor)

---

**Status**: ✅ Enhanced with detailed debugging  
**Fix Applied**: Specific SkullEnemy & Gem detection  
**Debug Logs**: Fully detailed  

**Your sword WILL work now - just check the Layer Mask! ⚔️**
