# 🗡️ SWORD MODE - ALTERNATING ATTACKS SYSTEM

**Feature**: Two sword animations that automatically alternate on each attack
**Status**: ✅ Fully Implemented and Ready to Use

---

## 🎯 WHAT IT DOES

The sword system now alternates between **TWO different attacks** every time you click RMB:

```
Click 1 → Attack 1 (e.g., horizontal slash)
Click 2 → Attack 2 (e.g., overhead swing)
Click 3 → Attack 1 (horizontal slash again)
Click 4 → Attack 2 (overhead swing again)
... continues alternating forever
```

This makes combat feel more **dynamic and varied** instead of repeating the same animation!

---

## 🔧 HOW IT WORKS

### Technical Flow:
```
1. Player clicks RMB in sword mode
   └─> PlayerShooterOrchestrator.TriggerSwordAttack()
       ├─> Checks _currentSwordAttackIndex (starts at 1)
       ├─> Calls rightHand.TriggerSwordAttack(_currentSwordAttackIndex)
       ├─> Alternates index: 1→2 or 2→1
       └─> Next click will use the OTHER attack

2. IndividualLayeredHandController receives attack index
   └─> TriggerSwordAttack(int attackIndex)
       ├─> If attackIndex == 1 → Sets trigger "SwordAttack1T"
       ├─> If attackIndex == 2 → Sets trigger "SwordAttack2T"
       └─> Animator plays corresponding animation
```

### Code Implementation:
**PlayerShooterOrchestrator.cs**:
- `_currentSwordAttackIndex` tracks which attack is next (1 or 2)
- Alternates automatically: `_currentSwordAttackIndex = (_currentSwordAttackIndex == 1) ? 2 : 1;`

**IndividualLayeredHandController.cs**:
- `TriggerSwordAttack(int attackIndex)` now accepts a parameter
- Triggers "SwordAttack1T" or "SwordAttack2T" based on index

---

## 🎮 ANIMATOR SETUP

### Step 1: Add Trigger Parameters
In your **Right Hand Animator Controller**:

1. Open the Animator window
2. Go to **Parameters** tab
3. Add **TWO** trigger parameters:
   - Name: `SwordAttack1T` (Type: Trigger)
   - Name: `SwordAttack2T` (Type: Trigger)

### Step 2: Create Animation States
In the **Shooting Layer** (Layer 1):

1. Create state: **"SwordAttack1"**
   - Assign your first sword animation (e.g., horizontal slash)
   
2. Create state: **"SwordAttack2"**
   - Assign your second sword animation (e.g., overhead swing)

### Step 3: Add Transitions
Create transitions from **Any State**:

1. **Any State → SwordAttack1**
   - Condition: `SwordAttack1T` trigger
   - Transition Duration: ~0.1s
   - Exit Time: Unchecked
   
2. **Any State → SwordAttack2**
   - Condition: `SwordAttack2T` trigger
   - Transition Duration: ~0.1s
   - Exit Time: Unchecked

### Step 4: Add Animation Events (CRITICAL!)
For **BOTH** animations, add the damage event:

**SwordAttack1 Animation**:
1. Select the animation clip
2. Open Animation window
3. Find the impact frame (where sword hits target)
4. Add Animation Event:
   - Function: `DealDamage`
   - Object: SwordDamage component (auto-found)

**SwordAttack2 Animation**:
1. Same process as above
2. Add event at the impact frame
3. Function: `DealDamage`

---

## 🎨 ANIMATION IDEAS

### Attack 1 (Horizontal Slash):
- **Motion**: Side-to-side swing
- **Timing**: Fast (0.5s)
- **Feel**: Quick, light attack
- **Best for**: Multiple weak enemies

### Attack 2 (Overhead Swing):
- **Motion**: Top-to-bottom heavy strike
- **Timing**: Slower (0.7s)
- **Feel**: Powerful, impactful
- **Best for**: Single tough enemy

### Other Ideas:
- **Attack 1**: Upward slash (launching enemies)
- **Attack 2**: Downward stab (pinning enemies)
- **Attack 1**: Spinning slash (wide area)
- **Attack 2**: Thrust (long reach)

---

## 📊 CUSTOMIZATION

### Different Damage Per Attack
Modify `SwordDamage.cs` to accept attack index:

```csharp
public void DealDamage(int attackIndex = 1)
{
    float damageAmount = damage;
    
    // Attack 2 deals more damage
    if (attackIndex == 2)
    {
        damageAmount *= 1.5f; // 50% more damage
        Debug.Log($"[SwordDamage] HEAVY ATTACK! Damage increased to {damageAmount}");
    }
    
    // ... rest of damage logic
}
```

Then call it from animation event with a parameter!

### Different Cooldowns Per Attack
```csharp
public float attack1Cooldown = 0.5f;
public float attack2Cooldown = 0.7f;

public void DealDamage(int attackIndex = 1)
{
    float cooldown = (attackIndex == 2) ? attack2Cooldown : attack1Cooldown;
    _nextAttackTime = Time.time + cooldown;
    // ... damage logic
}
```

### Different Ranges Per Attack
```csharp
public float attack1Radius = 2f;
public float attack2Radius = 3f;

public void DealDamage(int attackIndex = 1)
{
    float radius = (attackIndex == 2) ? attack2Radius : attack1Radius;
    Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius, damageLayerMask);
    // ... damage logic
}
```

---

## 🎯 ADVANCED PATTERNS

### Three Attack Combo
Modify `PlayerShooterOrchestrator.cs`:

```csharp
private int _currentSwordAttackIndex = 1;

// In TriggerSwordAttack():
_currentSwordAttackIndex++;
if (_currentSwordAttackIndex > 3) 
    _currentSwordAttackIndex = 1;
```

Add "SwordAttack3T" trigger and animation to Animator!

### Finisher Attack (Every 3rd Hit)
```csharp
private int _swordAttackCount = 0;

// In TriggerSwordAttack():
_swordAttackCount++;
int attackIndex = (_swordAttackCount % 3 == 0) ? 3 : (_swordAttackCount % 2 == 0 ? 2 : 1);
rightHand.TriggerSwordAttack(attackIndex);
```

This gives pattern: 1 → 2 → **FINISHER** → 1 → 2 → **FINISHER**

### Random Attack Selection
```csharp
// In TriggerSwordAttack():
int attackIndex = Random.Range(1, 3); // Random between 1 and 2
rightHand.TriggerSwordAttack(attackIndex);
```

More chaotic but less predictable!

---

## 🐛 TROUBLESHOOTING

### Problem: Always plays same attack
**Solution**: 
1. Check Console - should show "SWORD ATTACK 1" then "SWORD ATTACK 2" alternating
2. Verify both `SwordAttack1T` and `SwordAttack2T` triggers exist in Animator
3. Check transitions from Any State to both animation states

### Problem: Attack index stuck at 1
**Solution**:
1. Verify the alternating code in `PlayerShooterOrchestrator.TriggerSwordAttack()`:
   ```csharp
   _currentSwordAttackIndex = (_currentSwordAttackIndex == 1) ? 2 : 1;
   ```
2. Check that this line comes AFTER calling `TriggerSwordAttack()`

### Problem: Wrong animation plays
**Solution**:
1. Check Animator transitions - triggers must match exactly:
   - "SwordAttack1T" → SwordAttack1 state
   - "SwordAttack2T" → SwordAttack2 state
2. Check for typos in trigger names (case-sensitive!)

### Problem: Damage only works on one attack
**Solution**:
1. Both animations need the `DealDamage()` animation event
2. Check Animation window for both SwordAttack1 and SwordAttack2
3. Verify event is at the correct frame (impact moment)

---

## 📋 QUICK REFERENCE

### Animator Parameters Needed:
- ✅ `SwordAttack1T` (Trigger)
- ✅ `SwordAttack2T` (Trigger)

### Animation States Needed (Shooting Layer):
- ✅ `SwordAttack1` state with animation
- ✅ `SwordAttack2` state with animation

### Transitions Needed:
- ✅ Any State → SwordAttack1 (condition: SwordAttack1T)
- ✅ Any State → SwordAttack2 (condition: SwordAttack2T)

### Animation Events Needed:
- ✅ SwordAttack1: `DealDamage()` at impact frame
- ✅ SwordAttack2: `DealDamage()` at impact frame

---

## 🎉 BENEFITS

### Player Experience:
- ✅ **More Variety** - Combat feels less repetitive
- ✅ **Visual Polish** - Two different animations add flair
- ✅ **Skill Expression** - Can time different attacks for different situations

### Technical Benefits:
- ✅ **Automatic** - No player input needed to choose attacks
- ✅ **Deterministic** - Predictable pattern (1→2→1→2)
- ✅ **Expandable** - Easy to add Attack 3, 4, etc.

### Design Benefits:
- ✅ **Different Timing** - Can vary attack speeds
- ✅ **Different Ranges** - Some attacks reach further
- ✅ **Different Damage** - Balance light vs heavy attacks

---

## 📝 TESTING CHECKLIST

1. ✅ Enter sword mode (Backspace)
2. ✅ Click RMB once → Console shows "SWORD ATTACK 1"
3. ✅ Wait for cooldown (~0.5s)
4. ✅ Click RMB again → Console shows "SWORD ATTACK 2"
5. ✅ Click RMB again → Console shows "SWORD ATTACK 1"
6. ✅ Pattern continues: 1 → 2 → 1 → 2 → 1...
7. ✅ Both animations play correctly
8. ✅ Both attacks deal damage

---

## 🚀 NEXT STEPS

### Easy Enhancements:
- Add unique VFX to each attack (different particle effects)
- Add unique sounds to each attack (light vs heavy whoosh)
- Add camera shake variation (light shake for Attack 1, heavy for Attack 2)

### Medium Complexity:
- Add Attack 3 as a finisher every 3rd hit
- Make Attack 2 deal more damage but have longer cooldown
- Add directional attacks (forward, left, right based on movement)

### Advanced Features:
- Combo counter that resets if you wait too long
- Combo finishers that deal massive damage
- Special attacks after X consecutive hits
- Animation canceling for faster combos

---

## ✨ SUMMARY

You now have a **professional-grade alternating attack system** that:
- ✅ Automatically switches between two sword animations
- ✅ Requires minimal setup (two triggers, two states, two animations)
- ✅ Feels dynamic and engaging for players
- ✅ Is easy to expand to 3+ attacks

**Status**: Production ready! Just add your animations to the Animator Controller.

---

**Created**: October 21, 2025  
**Version**: 2.0 - Alternating Attacks Feature  
**Compatibility**: Works with existing sword mode system

🗡️⚔️ **Enjoy your enhanced sword combat!** ⚔️🗡️
