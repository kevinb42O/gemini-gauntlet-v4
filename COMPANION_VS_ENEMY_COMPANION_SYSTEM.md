# COMPANION VS ENEMY COMPANION COMBAT SYSTEM

## 🎯 Overview
Your friendly companions can now **fight enemy companions**! This creates epic companion-vs-companion battles while maintaining their primary mission of killing skulls and gems.

## ✅ What Was Implemented

### **Modified File: `CompanionTargeting.cs`**

#### **1. New Inspector Settings**
Added two new toggles in the Unity Inspector under "Enemy Companion Targeting":
- **`targetEnemyCompanions`** (default: `true`) - Enable/disable enemy companion targeting
- **`prioritizeEnemyCompanions`** (default: `true`) - Prioritize enemy companions over skulls/gems

#### **2. Enemy Companion Detection**
- Companions now scan for enemy companions within their **detection radius** (same range as skulls: 5000 units by default)
- Enemy companions are identified by:
  - Having `EnemyCompanionBehavior` component
  - `isEnemy` flag set to `true`
  - Being alive (not dead)

#### **3. Target Priority System**
When `prioritizeEnemyCompanions = true`:
1. **Emergency threats** (very close enemies)
2. **Enemy companions** ⭐ NEW!
3. **Skulls** (via threat tracker)
4. **Gems**

When `prioritizeEnemyCompanions = false`:
1. **Emergency threats**
2. **Skulls** (via threat tracker)
3. **Enemy companions** ⭐ NEW!
4. **Gems**

#### **4. Performance Optimized**
- Uses `OverlapSphereNonAlloc` with buffer size of 10 for enemy companion detection
- Uses `sqrMagnitude` for distance calculations (no expensive square root)
- Cached detection results

## 🎮 How It Works

### **Scenario 1: Enemy Companion Nearby**
```
Player's Companion detects:
- Enemy Companion at 3000 units
- Skull at 2500 units
- Gem at 1500 units

Result: Shoots Enemy Companion (prioritized!)
```

### **Scenario 2: No Enemy Companions**
```
Player's Companion detects:
- Skull at 2500 units
- Gem at 1500 units

Result: Shoots Skull (normal behavior)
```

### **Scenario 3: Emergency Threat**
```
Player's Companion detects:
- Skull at 500 units (VERY CLOSE!)
- Enemy Companion at 3000 units

Result: Shoots Skull (emergency override)
```

## 🔧 Configuration

### **In Unity Inspector (CompanionTargeting component):**

1. **Enable Enemy Companion Targeting:**
   - `Target Enemy Companions` = ✅ (enabled by default)

2. **Prioritize Enemy Companions:**
   - `Prioritize Enemy Companions` = ✅ (enabled by default)
   - If disabled, companions will target skulls first, then enemy companions

3. **Detection Range:**
   - Uses existing `Detection Radius` setting (default: 5000 units)
   - Enemy companions must be within this range to be detected

## 🎯 Combat Behavior

### **What Your Companions Do:**
- ✅ **Detect enemy companions** within detection radius
- ✅ **Prioritize enemy companions** when close (if enabled)
- ✅ **Shoot enemy companions** with shotgun (close range) or beam (long range)
- ✅ **Continue killing skulls and gems** when no enemy companions nearby
- ✅ **Switch targets dynamically** based on threat priority

### **What Enemy Companions Do:**
- ❌ **Do NOT target other enemy companions** (they only hunt the player)
- ✅ **Can be damaged and killed** by friendly companions
- ✅ **Fight back against the player** (not against friendly companions)

## 🔊 Debug Logs

When an enemy companion is detected, you'll see:
```
[CompanionTargeting] 🎯 ENEMY COMPANION DETECTED: EnemyCompanion_01 at 3245.2 units - ENGAGING!
```

When targeting changes:
```
[CompanionTargeting] New target acquired: EnemyCompanion_01
```

## 🚀 Testing

### **How to Test:**
1. **Place friendly companions** in the scene (your helpers)
2. **Place enemy companions** in the scene (with `EnemyCompanionBehavior.isEnemy = true`)
3. **Start the game**
4. **Watch your companions automatically engage enemy companions!**

### **Expected Behavior:**
- Friendly companions will **immediately turn and shoot** at enemy companions when they get close
- Enemy companions will **take damage** and eventually die
- Friendly companions will **return to killing skulls/gems** after enemy companions are eliminated

## 🎨 Visual Feedback

- **Friendly companions** will face and track enemy companions
- **Weapon particles** (shotgun/beam) will fire at enemy companions
- **Enemy companions** will show hit effects (if enabled on their `EnemyCompanionBehavior`)
- **Death effects** will play when enemy companions are killed

## ⚙️ Technical Details

### **New Methods Added:**
1. **`FindClosestEnemyCompanion()`** - Scans for and returns closest enemy companion
2. **`IsEnemyCompanion(Collider)`** - Validates if a collider is an enemy companion

### **Modified Methods:**
1. **`SelectBestTarget()`** - Added enemy companion priority logic
2. **`CheckForImmediateThreats()`** - Added enemy companion detection to emergency checks

### **New Fields:**
- `_enemyCompanionBuffer` - Collider buffer for enemy companion detection (size: 10)

## 🎉 Result

**FUCKING AWESOME companion battles!** Your friendly companions will now:
- ✅ Protect you by eliminating enemy companions
- ✅ Maintain their primary mission (killing skulls and gems)
- ✅ Create dynamic, engaging combat scenarios
- ✅ Work seamlessly with existing AI systems

## 📝 Notes

- **Performance:** Minimal impact - uses existing detection systems with small buffer
- **Compatibility:** Works with all existing companion types (Aggressive, Tank, Medic, etc.)
- **Flexibility:** Can be toggled on/off per companion in Inspector
- **Priority:** Enemy companions are prioritized by default but can be deprioritized if needed

---

**Enjoy your epic companion battles! 🔥⚔️**
