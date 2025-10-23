# 🔧 TOWER PROTECTOR CUBE - LAYER & DAMAGE FIX

## 🎯 The Issue

The cube is on the **Enemy layer** but weapons might not be hitting it because:
1. The weapon uses a **LayerMask** from HandLevelSO configuration
2. The Enemy layer might not be included in that mask

## ✅ Solution Options

### Option 1: Update HandLevelSO Configuration (RECOMMENDED)
This ensures ALL weapons can hit enemies on the Enemy layer.

#### Steps:
1. **Find HandLevelSO assets**
   - Navigate to your ScriptableObjects folder
   - Look for HandLevelSO files (e.g., "Hand Level 1", "Hand Level 2", etc.)

2. **Select each HandLevelSO**
   - Click on the asset in Project window

3. **Update Damage Layer Mask**
   - In Inspector, find **"Damage Layer Mask"**
   - Click the dropdown
   - **Check "Enemy"** layer
   - Also check "gems" if you want to shoot gems
   - Click outside to confirm

4. **Repeat for ALL HandLevelSO files**
   - Each hand level needs the Enemy layer enabled

### Option 2: Move Cube to Different Layer
If you can't modify HandLevelSO, put the cube on a layer that weapons already hit.

#### Steps:
1. **Select cube in Hierarchy**
2. **Top of Inspector → Layer dropdown**
3. **Try these layers** (in order of likelihood):
   - **"gems"** - Most likely to work (weapons shoot gems)
   - **"Default"** - Standard layer
   - **"Enemy"** - If HandLevelSO includes it

---

## 🔍 How to Check Current LayerMask

### In Play Mode:
1. Start the game
2. Shoot your weapon
3. Check Console for this message:
```
[HandFiringMechanics] SHOTGUN RAYCAST DEBUG:
LayerMask: 1234567
```

### Decode the LayerMask:
The number tells you which layers are included. To check if Enemy layer is included:

```csharp
int enemyLayer = LayerMask.NameToLayer("Enemy");
bool hasEnemy = (layerMaskValue & (1 << enemyLayer)) != 0;
```

If `hasEnemy` is false, the Enemy layer is NOT in the mask!

---

## 🎮 Quick Test

### Test 1: Check Cube Layer
```
1. Select cube in Hierarchy
2. Inspector → Top shows "Layer: Enemy"
3. If not "Enemy", change it
```

### Test 2: Check Collider
```
1. Select cube in Hierarchy
2. Inspector → Should have BoxCollider component
3. If missing, it will auto-add on Start
```

### Test 3: Check IDamageable
```
1. Start Play Mode
2. Check Console for:
   "[TowerProtector] ✅ Initialized - ... Implements IDamageable: True"
3. If False, script not attached correctly
```

### Test 4: Shoot Cube
```
1. Start Play Mode
2. Shoot cube with weapon
3. Check Console for:
   "[TowerProtector] 🎯 TakeDamage called!"
4. If you see this, damage is working!
5. If not, check LayerMask
```

---

## 🔧 Manual LayerMask Fix (Advanced)

If you can't modify HandLevelSO in Inspector, you can hardcode it:

### In HandFiringMechanics.cs:
Find where `damageLayerMask` is used and add Enemy layer:

```csharp
// Add Enemy layer to mask
LayerMask fixedMask = _currentConfig.damageLayerMask;
int enemyLayer = LayerMask.NameToLayer("Enemy");
fixedMask |= (1 << enemyLayer); // Add Enemy layer

// Use fixedMask instead of _currentConfig.damageLayerMask
PerformHitDetection(raycastOrigin, fireDirection, damage, radius, maxDistance, fixedMask, "SHOTGUN");
```

---

## 📊 Layer Compatibility Chart

| Cube Layer | Weapon Hits? | Notes |
|------------|--------------|-------|
| **gems** | ✅ Very Likely | Weapons shoot gems |
| **Enemy** | ⚠️ Maybe | Depends on HandLevelSO |
| **Default** | ⚠️ Maybe | Depends on HandLevelSO |
| **Ignore Raycast** | ❌ Never | Skipped by raycasts |

---

## 🎯 Recommended Setup

### For Cube:
```
Layer: Enemy
Collider: BoxCollider (auto-added)
Script: SkullSpawnerCube (implements IDamageable)
```

### For HandLevelSO:
```
Damage Layer Mask:
☑ Enemy
☑ gems
☐ Default (optional)
☐ Ignore Raycast (never)
```

---

## 🐛 Troubleshooting Checklist

- [ ] Cube has **Collider** component
- [ ] Cube is on **Enemy layer**
- [ ] Cube has **SkullSpawnerCube script** attached
- [ ] **HandLevelSO** includes Enemy layer in mask
- [ ] Weapon is **actually firing** (check console)
- [ ] Raycast is **hitting cube** (check Scene view)
- [ ] Console shows **"TakeDamage called"** message

---

## 💡 Quick Fix Summary

**If cube won't take damage:**

1. **Check HandLevelSO** → Add Enemy layer to Damage Layer Mask
2. **Check cube layer** → Set to Enemy
3. **Check collider** → Should have BoxCollider
4. **Test in Play Mode** → Shoot cube, check console

**Most common issue:** HandLevelSO doesn't include Enemy layer in mask!

---

## 🎮 Expected Console Output

### On Start:
```
[TowerProtector] ✅ Initialized - Health: 1000/1000, Layer: Enemy, Has Collider: True, Implements IDamageable: True
```

### On Weapon Fire:
```
[HandFiringMechanics] SHOTGUN RAYCAST DEBUG:
LayerMask: 12345
```

### On Hit:
```
[TowerProtector] 🎯 TakeDamage called! Amount: 25, IsDead: False, IsFriendly: False
[TowerProtector] 💥 Took 25 damage! Health: 975/1000 (98%)
```

---

**If you still can't damage the cube after checking all this, the issue is 100% the LayerMask in HandLevelSO! 🎯**
