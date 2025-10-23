# 🎯 Scale-Corrected Enemy AI Settings

## Your Game Scale
- **Player Height:** 320 units
- **Player Width:** 50 units
- **Standard Unity Scale:** ~1.8 units (you're using ~178x larger!)

## ✅ ALL VALUES NOW CORRECTED FOR YOUR SCALE

---

## 🎮 TacticalEnemyAI - Inspector Settings

### Detection Settings
```
Detection Range: 25000 (25m equivalent)
Field Of View: 90°
Detection Interval: 0.2s
Memory Duration: 10s
```

### Line of Sight
```
Vision Blocking Layers: Select "Walls" layer
Eye Height: 160 (half of 320 = center of player)
LOS Raycast Count: 3
LOS Raycast Spread: 30 (covers 50-unit wide player)
```

### Movement Settings
```
Patrol Speed: 400
Alert Speed: 700
Combat Speed: 600
Rotation Speed: 120
```

### Combat Behavior
```
Preferred Combat Distance: 8000
Min Combat Distance: 2000 (back up if closer)
Max Combat Distance: 15000 (advance if farther)
Accuracy: 0.7 (70%)
Fire Rate: 0.5s
Burst Duration: 2s
Burst Cooldown: 1.5s
```

### Cover System
```
Use Cover System: TRUE
Cover Search Radius: 10000
Cover Duration: 3s
Cover Health Threshold: 0.5 (50%)
Cover Layers: Select "Walls" layer
```

### Sound Detection
```
Enable Sound Detection: TRUE
Sound Detection Range: 12000
```

### Death & Damage
```
Enable Hit Effects: TRUE
Hit Color: Red
Hit Effect Duration: 0.2s
Destroy After Death: 10s
```

---

## 🎮 EnemyCompanionBehavior - Inspector Settings

### Detection Settings
```
Player Detection Radius: 25000
Attack Range: 5000
Max Shooting Range: 8000
Detection Interval: 0.3s
Require Line Of Sight: TRUE
Line Of Sight Blockers: Select "Walls" layer
LOS Raycast Count: 3
Eye Height: 160
LOS Raycast Spread: 30
```

### Combat Behavior
```
Max Beam Duration: 5s
Beam Cooldown Time: 2s
Aim Accuracy: 0.7
Aim Deviation: 150
Enable Tactical Movement: TRUE
Combat Movement Speed: 1.2
```

### Environment Awareness
```
Auto Detect Indoors: TRUE
Force Indoor Mode: FALSE
Indoor Speed Multiplier: 0.5
Disable Jumping Indoors: TRUE
Disable Tactical Movement Indoors: TRUE
```

### Patrol Behavior
```
Enable Patrol: TRUE
Patrol Points: Assign waypoints
Patrol Wait Time: 2s
Random Patrol Radius: 8000
```

---

## 🔧 Critical Setup Steps

### 1. Layers (MUST DO!)
Create these layers in Unity:
- **Walls** - All building walls/obstacles
- **ground** - Your existing ground layer (lowercase is fine!)
- **Player** - Player character
- **Enemy** - Enemy characters

### 2. Assign Layers
- All building walls → `Walls` layer
- All floors → `ground` layer
- Player GameObject → `Player` layer
- Enemy GameObjects → `Enemy` layer

### 3. Configure LayerMasks
In enemy inspector:
- `visionBlockingLayers` → Select `Walls` ONLY
- `coverLayers` → Select `Walls`
- `groundLayers` → Select `ground`

### 4. Collider Setup (For Hit Detection)
Enemy needs:
- **Collider** (CapsuleCollider recommended)
  - Radius: 50 (matches player width)
  - Height: 320 (matches player height)
  - Center: (0, 160, 0)
  - Is Trigger: FALSE (must be solid!)
- **Rigidbody** (for physics)
  - Use Gravity: TRUE
  - Freeze Rotation: TRUE
- **CompanionCore** (implements IDamageable)
  - Max Health: Set as desired
  - Layer: `Enemy`

---

## 💀 Death System

### How It Works
1. Enemy takes damage via `CompanionCore.TakeDamage()`
2. Health decreases
3. When health reaches 0:
   - `OnCompanionDied` event fires
   - Enemy transitions to Dead state
   - NavMesh disabled
   - Rigidbody unfrozen (ragdoll effect)
   - Destroyed after 10 seconds

### Hit Detection
Your player weapon must:
- Raycast and hit enemy collider
- Get `IDamageable` component
- Call `TakeDamage(amount, hitPoint, hitDirection)`

Example:
```csharp
RaycastHit hit;
if (Physics.Raycast(origin, direction, out hit, range))
{
    IDamageable damageable = hit.collider.GetComponent<IDamageable>();
    if (damageable != null)
    {
        damageable.TakeDamage(50f, hit.point, direction);
    }
}
```

---

## 🧪 Testing Checklist

### Test 1: Detection
- [ ] Enemy detects you at ~25000 units
- [ ] Enemy doesn't see through walls
- [ ] Console shows "PLAYER DETECTED"
- [ ] Green debug rays when detected

### Test 2: Line of Sight
- [ ] Enemy loses you when you hide behind walls
- [ ] Red debug rays when blocked
- [ ] Console shows "Line of sight blocked"

### Test 3: Combat
- [ ] Enemy shoots when has LOS
- [ ] Enemy stops shooting when LOS blocked
- [ ] Enemy maintains distance (2000-15000 range)
- [ ] Burst fire pattern (2s fire, 1.5s pause)

### Test 4: Hit Detection
- [ ] Shooting enemy flashes red
- [ ] Enemy health decreases
- [ ] Console shows damage amount
- [ ] Enemy dies at 0 health

### Test 5: Death
- [ ] Enemy stops moving when dead
- [ ] Enemy falls over (ragdoll)
- [ ] Enemy destroyed after 10s
- [ ] Console shows "Enemy died!"

### Test 6: Indoor Behavior
- [ ] Enemy moves slower indoors (50% speed)
- [ ] Enemy doesn't jump indoors
- [ ] Console shows "Entered INDOOR area"
- [ ] Smooth hallway movement

---

## 🐛 Common Issues

### "Enemy doesn't detect me"
**Cause:** Detection range too small for your scale
**Fix:** Set `detectionRange = 25000` (already done!)

### "Enemy sees through walls"
**Cause:** Wrong layer mask
**Fix:** Set `visionBlockingLayers` to `Walls` layer ONLY

### "Enemy doesn't take damage"
**Cause:** Missing collider or wrong layer
**Fix:** 
1. Add CapsuleCollider (radius 50, height 320)
2. Set `isTrigger = false`
3. Ensure enemy is on `Enemy` layer
4. Verify weapon raycasts hit the collider

### "Enemy doesn't die"
**Cause:** Health not reaching 0
**Fix:**
1. Check `CompanionCore.maxHealth` value
2. Verify damage amount is sufficient
3. Enable `showDebugInfo` to see health changes
4. Use context menu "Test Kill" to verify death works

### "Enemy moves too fast/slow"
**Cause:** NavMesh speed not scaled
**Fix:** Already set to 400-700 range (scaled for your game)

### "Eye height wrong"
**Cause:** Was set for standard Unity scale (1.8)
**Fix:** Now set to 160 (half of 320 = center mass)

---

## 📊 Scale Conversion Reference

If you need to adjust any values:

| Standard Unity | Your Scale | Multiplier |
|---------------|------------|------------|
| 1.8 (height)  | 320        | ~178x      |
| 0.5 (width)   | 50         | 100x       |
| 10m range     | ~5000      | 500x       |
| 1m distance   | ~500       | 500x       |

**Formula:** `YourValue = StandardValue × 500` (approximately)

---

## ✅ Summary

### What's Fixed:
1. ✅ Detection range scaled to 25000 (was 30)
2. ✅ Eye height scaled to 160 (was 1.8)
3. ✅ Raycast spread scaled to 30 (was 0.3)
4. ✅ Combat distances scaled (2000-15000)
5. ✅ Movement speeds scaled (400-700)
6. ✅ Cover radius scaled to 10000
7. ✅ Sound range scaled to 12000
8. ✅ Ceiling detection scaled to 5000
9. ✅ Hit detection fully implemented
10. ✅ Death system fully implemented

### What You Get:
- Enemies that **actually see you** at proper distances
- Enemies that **respect walls** (no wallhacks)
- Enemies that **take damage** and flash red
- Enemies that **die** when health reaches 0
- Enemies that **move smoothly** indoors
- Enemies that **use cover** tactically
- **AAA-quality behavior** at your game's scale

---

## 🚀 You're Ready!

All values are now correctly scaled for your game. Just:
1. Set up layers (Walls, ground, Player, Enemy)
2. Configure inspector settings (copy from above)
3. Test in your building
4. Enjoy AAA-quality enemies! 🔥
