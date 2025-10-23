# 🗡️ MAGNIFICENT SWORD MODE SYSTEM - COMPLETE SETUP GUIDE

## Overview
This guide will help you set up the new **Sword Mode** system for the right hand. When activated with Backspace, the right hand switches from shooting mode to sword mode, while the left hand continues shooting normally.

---

## ✅ What Was Implemented

### 1. **SwordDamage Script** (`Assets/scripts/SwordDamage.cs`)
- Simple, clean damage component with NO bloat code
- Deals damage in a configurable sphere radius
- Cooldown system to prevent spam
- Animation event integration
- Debug sphere visualization in editor

### 2. **Input System Integration**
- Added `SwordModeToggle` key (Backspace) to `Controls.cs`
- Added `swordModeToggle` setting to `InputSettings.cs`
- Fully configurable in Unity Inspector

### 3. **PlayerShooterOrchestrator Enhancements**
- `IsSwordModeActive` flag tracks sword mode state
- `ToggleSwordMode()` switches between shooting/sword mode
- `TriggerSwordAttack()` handles sword attack logic
- Right hand shooting disabled when in sword mode
- Left hand continues shooting normally (dual-wielding!)

### 4. **IndividualLayeredHandController Animation Support**
- New `TriggerSwordAttack()` method for sword animations
- Uses existing Shooting layer (clean integration)
- Proper priority handling (interrupts emotes, beam)
- Auto-resets after animation completes

---

## 🎮 How It Works

### User Experience:
1. **Press Backspace** → Toggles sword mode ON (right hand only)
2. **Right Mouse Button (RMB)** → Triggers sword attack animation
3. **Animation Event** → Calls `SwordDamage.DealDamage()` at the perfect frame
4. **Press Backspace again** → Back to shooting mode

### Technical Flow:
```
User Input (Backspace) 
  ↓
PlayerShooterOrchestrator.ToggleSwordMode()
  ↓
IsSwordModeActive = true/false
  ↓
RMB Click → TriggerSwordAttack()
  ↓
rightHandController.TriggerSwordAttack()
  ↓
Animator triggers "SwordAttackT"
  ↓
Animation Event → SwordDamage.DealDamage()
  ↓
Sphere damage detection → Hits enemies
```

---

## 🛠️ Setup Instructions

### Step 1: Create Your Sword GameObject
1. Create a new GameObject for your sword (e.g., "PlayerSword")
2. Position it as a child of your **Right Hand** bone/transform
3. Add a mesh/visual representation of the sword
4. Add the `SwordDamage` component to this GameObject

### Step 2: Configure SwordDamage Component
In the Inspector for your sword GameObject:
- **Damage**: Set to desired damage value (default: 50)
- **Damage Radius**: Sphere radius for hit detection (default: 2)
- **Damage Layer Mask**: **CRITICAL!** Select the layers containing enemies and gems
  - Include: "Enemy" layer (for skulls)
  - Include: "gems" layer (for gems)
  - Include: "Default" layer (if enemies are on default)
  - **TIP**: Click the dropdown and check ALL relevant layers
- **Attack Cooldown**: Time between attacks in seconds (default: 0.5)
- **Show Debug Sphere**: Enable to visualize damage radius in Scene view

**IMPORTANT**: If sword doesn't damage anything, check your **Damage Layer Mask**!
The layer mask MUST include the layers your enemies and gems are on.

### Step 3: Connect to PlayerShooterOrchestrator
1. Select your player GameObject with `PlayerShooterOrchestrator` component
2. In Inspector, find the **Sword Mode System** section
3. Drag your sword GameObject into the **Sword Damage** field

### Step 4: Set Up Animator
You need to add a sword attack animation to your **Right Hand Animator**:

#### Option A: Quick Setup (Use Shotgun Animation as Placeholder)
The system will work immediately using existing animations.

#### Option B: Full Setup (Custom Sword Animation)
1. Open your Right Hand Animator Controller
2. Go to the **Shooting Layer** (Layer 1)
3. Add **TWO** new trigger parameters: 
   - `SwordAttack1T` (first attack)
   - `SwordAttack2T` (second attack)
4. Create **TWO** new animation states:
   - "SwordAttack1" (e.g., horizontal slash)
   - "SwordAttack2" (e.g., overhead swing)
5. Add transitions:
   - `Any State → SwordAttack1` with condition `SwordAttack1T`
   - `Any State → SwordAttack2` with condition `SwordAttack2T`
6. Import/create your two sword swing animations
7. The system will **automatically alternate** between Attack 1 and Attack 2
8. **CRITICAL**: Add Animation Events to BOTH animations to call `SwordDamage.DealDamage()` at the impact frame
   - Select the SwordAttack animation
   - Open Animation window
   - Add Event at the frame where sword hits
   - Function name: `DealDamage`
   - Object: Your SwordDamage component

### Step 5: Configure Input Settings (Optional)
If you have an InputSettings ScriptableObject:
1. Find it in your Resources folder or Project
2. Set **Sword Mode Toggle** to your preferred key (default: Backspace)

---

## 📋 Animation Event Setup (CRITICAL!)

For the sword to deal damage at the right moment, you MUST add an animation event:

### How to Add Animation Event:
1. Select your sword attack animation clip
2. Open the Animation window (Window → Animation → Animation)
3. Find the frame where the sword IMPACTS the target
4. Click the "Add Event" button (or right-click timeline → Add Animation Event)
5. In the Event inspector:
   - **Function**: `DealDamage`
   - **Object**: (Unity will auto-find SwordDamage component)
6. Save the animation

### Without Animation Event:
If you don't set this up, damage will be dealt IMMEDIATELY when you click, not when the sword visually hits. This works but looks less polished.

---

## 🎯 Testing Your Setup

### Test Checklist:
1. ✅ **Toggle Test**: Press Backspace → Console shows "SWORD MODE ACTIVATED"
2. ✅ **Attack Test**: Press RMB in sword mode → Sword animation plays
3. ✅ **Damage Test**: Attack near enemy → Enemy takes damage (50 by default)
4. ✅ **Cooldown Test**: Spam RMB → Respects 0.5s cooldown
5. ✅ **Left Hand Test**: Left hand continues shooting normally
6. ✅ **Mode Switch**: Press Backspace again → Back to shooting mode

### Debug Tools:
- Enable **Show Debug Sphere** in SwordDamage to visualize range
- Enable **Enable Debug Logs** in IndividualLayeredHandController
- Watch Console for "[PlayerShooterOrchestrator] SWORD ATTACK TRIGGERED!"

---

## 🎨 Customization Guide

### Change Sword Damage:
```
SwordDamage component → Damage = 100 (or any value)
```

### Change Attack Range:
```
SwordDamage component → Damage Radius = 3 (larger area)
```

### Change Attack Speed:
```
SwordDamage component → Attack Cooldown = 0.3 (faster attacks)
```

### Change Toggle Key:
```
InputSettings ScriptableObject → Sword Mode Toggle = KeyCode.F
```

### Add Special Effects:
Add to `SwordDamage.DealDamage()`:
```csharp
// Add particle effect at sword position
if (swordVFX != null)
    Instantiate(swordVFX, transform.position, transform.rotation);

// Add screen shake
CameraShake.Instance?.Shake(0.3f, 0.2f);

// Add sound effect
AudioSource.PlayClipAtPoint(swordSwooshSound, transform.position);
```

---

## 🔧 Advanced Integration

### Combo System:
Track consecutive hits in `SwordDamage.cs`:
```csharp
private int comboCount = 0;
private float lastHitTime = 0f;

public void DealDamage()
{
    // Combo resets after 2 seconds
    if (Time.time - lastHitTime > 2f)
        comboCount = 0;
    
    comboCount++;
    float comboDamage = damage * (1 + comboCount * 0.2f); // 20% per combo
    
    // ... apply comboDamage instead of damage
    lastHitTime = Time.time;
}
```

### Different Damage for Different Enemies:
```csharp
foreach (Collider hit in hitColliders)
{
    if (hit.CompareTag("Boss"))
        damageable.TakeDamage(damage * 2f, hit.transform.position, damageDirection);
    else if (hit.CompareTag("Skull"))
        damageable.TakeDamage(damage, hit.transform.position, damageDirection);
}
```

### Critical Hits:
```csharp
float critChance = 0.2f; // 20% crit chance
float finalDamage = damage;

if (Random.value < critChance)
{
    finalDamage *= 2f; // Critical hit!
    Debug.Log("CRITICAL HIT!");
    // Show crit VFX
}
```

---

## 🐛 Troubleshooting

### Problem: Sword doesn't deal damage
**Solution**: Check that:
1. SwordDamage component is assigned in PlayerShooterOrchestrator
2. **Damage Layer Mask includes the correct layers!**
   - Check what layer your enemies are on (select enemy → Inspector → Layer dropdown at top)
   - Common layers: "Enemy", "gems", "Default"
   - In SwordDamage component, click Layer Mask dropdown and CHECK those layers
3. Enemies have SkullEnemy or Gem component (they do!)
4. Damage Radius is large enough (try 5 for testing)
5. **Watch Console for detailed logs** - new version shows what it's hitting!

**Debug Steps**:
1. Attack near an enemy
2. Check Console - you should see:
   - `[SwordDamage] ⚔️ SWORD ATTACK! ... Found X colliders`
   - `[SwordDamage] Hit collider: SkullEnemy (Layer: Enemy)`
   - `[SwordDamage] ✅ DAMAGED SKULL: SkullEnemy for 50 damage!`
3. If you see "Found 0 colliders" → **Layer Mask is wrong!**
4. If you see colliders but "No damageable component" → Check enemy has SkullEnemy script

### Problem: Damage happens instantly, not with animation
**Solution**: 
1. Add Animation Event to your sword animation
2. Event should call `DealDamage()` at impact frame
3. Without event, damage is called immediately (by design)

### Problem: Can't toggle sword mode
**Solution**:
1. Check Console for "SWORD MODE ACTIVATED" message
2. Verify Backspace key in InputSettings
3. Ensure PlayerShooterOrchestrator.Update() is running

### Problem: Left hand also stops shooting
**Solution**: This is a bug! Check that:
1. Only `HandleSecondaryTap()` checks sword mode
2. `HandlePrimaryTap()` should NOT check sword mode
3. IsSwordModeActive should only affect secondary (RMB) hand

### Problem: Animation doesn't play
**Solution**:
1. Check Animator has "SwordAttackT" trigger parameter
2. Verify transition from Any State to SwordAttack exists
3. Enable Debug Logs in IndividualLayeredHandController
4. Watch for "SWORD ATTACK ANIMATION TRIGGERED!" in Console

---

## 📊 Performance Notes

### Optimizations Built-In:
- ✅ Cooldown system prevents spam
- ✅ Single sphere overlap check (efficient)
- ✅ No continuous Update() checks
- ✅ Event-driven damage (no polling)
- ✅ Reuses existing animation layer

### Not Recommended:
- ❌ Damage radius > 10 (too many collider checks)
- ❌ Attack cooldown < 0.1s (animation spam)
- ❌ Continuous damage per frame (use discrete hits)

---

## 🎓 System Architecture

### Clean Code Principles:
- **Single Responsibility**: SwordDamage only handles damage
- **No Bloat**: Minimal code, maximum functionality
- **Easy Extension**: Add features without breaking existing code
- **Unity-Friendly**: Uses standard components and events

### Integration Points:
1. **Input**: Controls.cs → PlayerShooterOrchestrator
2. **Animation**: IndividualLayeredHandController → Animator
3. **Damage**: Animation Event → SwordDamage → IDamageable
4. **State**: PlayerShooterOrchestrator tracks mode

---

## 🚀 Future Enhancements

### Easy Additions:
- Add sword trail VFX (particle system)
- Add impact VFX at hit point
- Add sword whoosh sound effect
- Add camera shake on hit
- Add UI indicator for sword mode

### Medium Complexity:
- Multiple sword types (fast/slow/heavy)
- Elemental damage (fire/ice/lightning)
- Blocking/parry system
- Charge attack (hold RMB)

### Advanced Features:
- Combo system with different attacks
- Directional attacks (overhead, horizontal, thrust)
- Executions on low-health enemies
- Sword energy/mana system

---

## 📝 Summary

You now have a fully functional sword mode system that:
- ✅ Toggles with Backspace
- ✅ Works ONLY on right hand (left hand keeps shooting)
- ✅ Uses animation events for perfect timing
- ✅ Has configurable damage, radius, and cooldown
- ✅ Integrates cleanly with existing shooting system
- ✅ Zero bloat code - clean and maintainable

**Next Steps**:
1. Create your sword GameObject
2. Add SwordDamage component
3. Connect to PlayerShooterOrchestrator
4. Set up sword animation in Animator
5. Add animation event for damage timing
6. Test and customize!

---

## 💡 Pro Tips

1. **Visual Feedback**: Enable "Show Debug Sphere" during development
2. **Sound Design**: Add satisfying sword whoosh and impact sounds
3. **Animation Polish**: Make sword animation snappy (0.5-0.7s total)
4. **Balance**: Start with high damage (50+) since it's melee range
5. **Camera**: Consider slight camera shake on successful hit

---

**Created by**: Senior Dev AI Assistant  
**Date**: October 20, 2025  
**Version**: 1.0 - Initial Implementation  
**Status**: ✅ COMPLETE AND READY TO USE!

---

## Need Help?
- Check troubleshooting section above
- Enable all debug logs
- Verify each setup step
- Test incrementally (toggle → animation → damage)

**Enjoy your new sword mode system! 🗡️⚔️**
