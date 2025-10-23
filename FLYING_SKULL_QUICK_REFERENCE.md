# 💀 Flying Skull Enemy - Quick Reference

## 🎯 One-Page Setup Guide

### Prefab Setup (2 minutes)

1. Create GameObject `FlyingSkull`
2. Add child mesh (your skull model)
3. Add components:
   - `Rigidbody` (Use Gravity: OFF)
   - `Sphere Collider` (Is Trigger: ON)
   - `FlyingSkullEnemy` script
4. Set layer to `Enemy`
5. Save as prefab

### Spawn Manager Setup (3 minutes)

1. Create empty GameObject `FlyingSkullSpawnManager`
2. Add `FlyingSkullSpawnManager` script
3. Create 3 empty GameObjects as spawn centers
4. Position centers at each floor/level
5. Configure manager:
   - Assign skull prefab
   - Assign 3 center points
   - Set skull counts (5-10 per level)
   - Set spawn radii (30-50)
6. Enable gizmos to visualize

### Quick Settings

**Prefab - Core:**
- Max Health: `30`
- Fly Speed: `10`
- Detection Radius: `50`
- Attack Range: `3`
- Max Chase Range: `150`

**Prefab - Avoidance:**
- Ground/Wall/Ceiling Clearance: `2-3`
- Obstacle Detection Range: `5`
- Avoidance Force: `15`

**Manager - Spawning:**
- Skull Count: `5-10` per level
- Spawn Radius: `30-50`
- Min Spacing: `10`
- Vertical Spread: `5`

---

## 🎮 Controls & Behavior

**Detection:** Simple 360° radius around skull  
**Chasing:** Flies directly toward player with floating motion  
**Attacking:** Lethal on contact (instant kill)  
**Avoidance:** Automatically steers around walls/ground/ceiling  

---

## 🐛 Quick Fixes

**Not spawning?**
→ Check console, ensure center points assigned

**Spawning in walls?**
→ Enable "Validate Spawn Positions"

**Clipping through walls?**
→ Increase "Avoidance Force" and "Obstacle Detection Range"

**Too many/too few?**
→ Adjust "Skull Count" in each spawn level

**Performance issues?**
→ Reduce skull count, increase "Detection Interval"

---

## 📊 Performance Budgets

| PC Type | Max Skulls | Detection Interval |
|---------|------------|-------------------|
| Low | 10-15 | 1.0s |
| Mid | 20-30 | 0.5s |
| High | 40-50 | 0.3s |

---

## 🔧 Common Tweaks

**Make skulls faster/aggressive:**
```
Fly Speed: 15
Erratic Movement Intensity: 0.9
Detection Radius: 80
```

**Make skulls slower/creepy:**
```
Fly Speed: 6
Erratic Movement Intensity: 0.4
Detection Radius: 30
```

**Dense swarm (more skulls, tight space):**
```
Skull Count: 15
Min Spacing: 5
Spawn Radius: 30
```

**Scattered patrol (fewer skulls, wide area):**
```
Skull Count: 3
Min Spacing: 20
Spawn Radius: 80
```

---

## ✅ Quick Checklist

- [ ] Skull prefab with FlyingSkullEnemy script
- [ ] Spawn manager in scene
- [ ] 3 center points created & assigned
- [ ] Gizmos show spawn areas correctly
- [ ] Test in Play Mode - skulls spawn
- [ ] Skulls chase player when detected
- [ ] Skulls avoid walls/ground/ceiling
- [ ] Audio plays (chatter/attack/death)

---

**Full documentation:** See `FLYING_SKULL_ENEMY_SETUP.md`
