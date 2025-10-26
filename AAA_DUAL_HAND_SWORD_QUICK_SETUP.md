# ⚔️⚔️ DUAL-HAND SWORD - 5 MINUTE SETUP GUIDE

**Objective**: Get dual-wielding swords working in Unity Editor  
**Time**: 5-10 minutes  
**Difficulty**: Easy (just Inspector assignments)

---

## ✅ Step 1: Assign LEFT Sword Properties (2 minutes)

### PlayerShooterOrchestrator Component
1. Find Player GameObject in Hierarchy
2. Select PlayerShooterOrchestrator component in Inspector
3. Scroll to **LEFT HAND Sword Settings** section

**Assign these:**
```
leftSwordDamage:
  → Find your LEFT sword GameObject (under LEFT hand bone)
  → Drag the SwordDamage component reference here
  
leftSwordVisualGameObject:
  → Find your LEFT sword mesh GameObject
  → Drag the GameObject reference here (NOT the component, the GameObject itself)
```

**Visual Check**: 
- leftSwordDamage should show: "SwordDamage (SwordDamage)"
- leftSwordVisualGameObject should show: "LeftSwordMesh (GameObject)"

---

## ✅ Step 2: Assign LEFT Weapon Slot (1 minute)

### WeaponEquipmentManager Component
1. Find WeaponEquipmentManager in scene (usually on GameManager or UI)
2. Select WeaponEquipmentManager component in Inspector

**Assign:**
```
leftHandWeaponSlot:
  → Find your LEFT hand weapon slot in UI (Canvas > Inventory > WeaponSlots > LeftHandSlot)
  → Should be a GameObject with UnifiedSlot component
  → Drag the UnifiedSlot component reference here
```

**Visual Check**:
- leftHandWeaponSlot should show: "LeftHandSlot (UnifiedSlot)"

---

## ✅ Step 3: Create LEFT Hand Sword Item Data (2 minutes)

### Create Sword Asset
1. In Project window, navigate to: `Assets/ScriptableObjects/Items/Weapons/` (or similar)
2. Right-click → Create → Inventory → Equippable Weapon
3. Name it: `LeftHandSword_Basic`

**Configure Asset:**
```
Item Name: "Left Hand Sword"
Item Description: "A deadly blade for your left hand"
weaponTypeID: "sword"          ← CRITICAL!
handType: LEFT HAND            ← SELECT LEFT HAND!
uniqueItem: ✓ (checked)       ← Only one allowed
itemIcon: [Assign a sprite]    ← REQUIRED for UI display
```

**Visual Check**:
- Inspector should show: "Hand Type: Left Hand"
- Icon should show in Inspector preview

---

## ✅ Step 4: Setup World Pickup (Optional, 2 minutes)

### Create LEFT Sword Pickup in World
1. Create empty GameObject in scene: "LeftSwordPickup"
2. Add Component → WorldSwordPickup
3. Assign child mesh for visual (your sword model)

**Configure WorldSwordPickup:**
```
swordItemData: [Drag your LeftHandSword_Basic asset here]
pickupRange: 250
enableBobbing: ✓
enableRotation: ✓
```

**Position**: Place near player for easy testing

---

## ✅ Step 5: Test in Play Mode (2 minutes)

### Test LEFT Hand Sword
1. Press Play
2. Walk to LEFT sword pickup
3. Press **E** to pickup
4. **Expected**: Sword auto-equips to LEFT hand slot, activates immediately
5. Press **LMB (tap)** → Should attack
6. Hold **LMB** → Should charge, release for power attack
7. Press **Mouse5** → Should toggle sword off
8. Press **Mouse5** again → Should toggle sword back on

### Test Dual-Wielding
1. Pickup RIGHT hand sword (existing system)
2. Press **Mouse4** → RIGHT sword active
3. Pickup LEFT hand sword
4. Press **Mouse5** → LEFT sword active
5. **Both swords visible!**
6. Press **LMB** → LEFT attacks
7. Press **RMB** → RIGHT attacks
8. Hold **LMB** + **RMB** → Both charge simultaneously!

---

## 🎯 Input Quick Reference

| **Action**          | **RIGHT Hand** | **LEFT Hand** |
|---------------------|----------------|---------------|
| Toggle Sword        | Mouse4         | Mouse5        |
| Attack              | RMB            | LMB           |
| Charge Attack       | Hold RMB       | Hold LMB      |
| Inventory Equip     | Drag to RIGHT slot | Drag to LEFT slot |

---

## 🚨 Troubleshooting

### LEFT Sword Not Appearing
**Problem**: Press Mouse5, nothing happens  
**Fix**: Check leftSwordVisualGameObject is assigned in PlayerShooterOrchestrator

### LEFT Sword No Damage
**Problem**: Attacks play animation but no damage  
**Fix**: Check leftSwordDamage is assigned in PlayerShooterOrchestrator

### Can't Pickup LEFT Sword
**Problem**: Press E, nothing happens  
**Fix**: Check swordItemData.handType is set to LEFT HAND

### LEFT Sword Goes to Inventory Instead of Auto-Equip
**Problem**: Pickup adds to inventory, doesn't equip  
**Fix**: 
1. Check leftHandWeaponSlot is assigned in WeaponEquipmentManager
2. Make sure LEFT weapon slot is empty before pickup

### Both Swords Use Same Hand
**Problem**: Both RIGHT and LEFT swords appear on same hand  
**Fix**: 
1. Check leftSwordVisualGameObject points to LEFT hand mesh (not RIGHT)
2. Check leftSwordDamage points to LEFT sword component (not RIGHT)

---

## ✅ Validation Checklist

Before testing, verify:
- [ ] PlayerShooterOrchestrator.leftSwordDamage assigned
- [ ] PlayerShooterOrchestrator.leftSwordVisualGameObject assigned
- [ ] WeaponEquipmentManager.leftHandWeaponSlot assigned
- [ ] LEFT hand sword ItemData created with handType = LEFT HAND
- [ ] LEFT hand sword ItemData has icon assigned
- [ ] LEFT hand sword ItemData has weaponTypeID = "sword"

---

## 🎉 Success Indicators

When setup correctly:
1. ✅ Press E on LEFT sword pickup → Auto-equips to LEFT slot
2. ✅ LEFT sword appears in LEFT hand (visible mesh)
3. ✅ Press LMB → LEFT hand attacks with sword
4. ✅ Hold LMB → LEFT sword charges (animation + sound)
5. ✅ Press Mouse5 → LEFT sword toggles off/on
6. ✅ Can have both RIGHT + LEFT swords active simultaneously
7. ✅ Drag LEFT sword from slot to inventory → Sword deactivates
8. ✅ Drag LEFT sword back to slot → Sword reactivates

---

## 📝 Next Steps After Setup

### 1. Create LEFT Hand Animations
You mentioned: *"i want it to be the choice of the player wether to equip a sword on the right hand (perfect as it is right now!!!! ) and i want the exact same for the left hand"*

**Animation Tasks**:
1. Duplicate your RIGHT hand sword animations
2. Mirror them for LEFT hand
3. Assign to LEFT hand animator controller
4. Use same trigger names:
   - SwordRevealT (equip)
   - SwordAttack1T (slash 1)
   - SwordAttack2T (slash 2)
   - SwordChargeT (charge)

### 2. Create More LEFT Hand Swords
- Duplicate `LeftHandSword_Basic` asset
- Create variants: Fire, Ice, Lightning, etc.
- Add to chest loot tables
- Test pickup and equip flow

### 3. Balance Dual-Wielding
- Test damage output with both swords
- Adjust cooldowns if needed
- Test combat feel

---

## 🎮 Ready to Play!

Once all steps complete, you have:
- ⚔️ **Full dual-wielding system**
- ⚔️ **Independent control (Mouse4/Mouse5)**
- ⚔️ **Independent attacks (LMB/RMB)**
- ⚔️ **Independent charging**
- ⚔️ **Full inventory integration**
- ⚔️ **Auto-equip from world pickups**

**Time to test dual-wielding in action!** ⚔️⚔️

---

*Full implementation details: See `AAA_DUAL_HAND_SWORD_IMPLEMENTATION_COMPLETE.md`*
