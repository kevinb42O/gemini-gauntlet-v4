# 🏢 Multi-Level Spawn System - Setup Guide

## What's New

The spawn manager now supports **multiple floors/levels** with individual settings for each!

---

## 🚀 Quick Setup

### Step 1: Create Floor Center Points

For each floor, create an empty GameObject:

```
Hierarchy:
├─ FloorCenters (empty parent)
│  ├─ Floor_Basement (Transform at basement center)
│  ├─ Floor_Ground (Transform at ground floor center)
│  └─ Floor_Upper (Transform at upper floor center)
```

Position each at the **center** of that floor.

---

### Step 2: Configure Spawn Manager

```
EnemySpawnManager:
├─ Enemy Prefab: [Your enemy prefab]
├─ Floors (Size: 3)
│  ├─ Element 0 (Basement -1)
│  │  ├─ Floor Name: "Basement"
│  │  ├─ Floor Center: [Floor_Basement]
│  │  ├─ Enemy Count: 5
│  │  ├─ Spawn Radius: 20000
│  │  └─ Height Offset: 0
│  │
│  ├─ Element 1 (Ground Floor 0)
│  │  ├─ Floor Name: "Ground Floor"
│  │  ├─ Floor Center: [Floor_Ground]
│  │  ├─ Enemy Count: 10
│  │  ├─ Spawn Radius: 30000
│  │  └─ Height Offset: 0
│  │
│  └─ Element 2 (Upper Floor 1)
│     ├─ Floor Name: "Upper Floor"
│     ├─ Floor Center: [Floor_Upper]
│     ├─ Enemy Count: 5
│     ├─ Spawn Radius: 15000
│     └─ Height Offset: 0
│
├─ Min Spacing: 5000
└─ Enable Debug Logs: TRUE
```

---

## 🎨 Visual Gizmos

### In Scene View:
- 🔴 **Red circle** = Basement (Floor -1)
- 🟢 **Green circle** = Ground Floor (Floor 0)
- 🔵 **Blue circle** = Upper Floor (Floor 1)
- 🟡 **Yellow spheres** = Spawned enemies (play mode)

---

## 📊 Per-Floor Settings

### Floor Name:
- Just for debugging/logs
- Example: "Basement", "Gelijksvloers", "First Floor"

### Floor Center:
- **Required!** Transform at center of floor
- Enemies spawn in radius around this point

### Enemy Count:
- How many enemies on THIS floor
- Can be different per floor
- Set to 0 to skip a floor

### Spawn Radius:
- How far from center to spawn
- Can be different per floor
- Smaller for tight areas, larger for open areas

### Height Offset:
- Optional Y-axis adjustment
- Usually leave at 0
- Use if enemies spawn slightly above/below floor

---

## 📝 Example Setup

### Your 3 Floors:

**Basement (-1):**
```
Floor Name: "Kelder" (basement in Dutch)
Floor Center: [Transform at Y = -5000]
Enemy Count: 3
Spawn Radius: 15000
```

**Ground Floor (0):**
```
Floor Name: "Gelijksvloers"
Floor Center: [Transform at Y = 0]
Enemy Count: 10
Spawn Radius: 30000
```

**Upper Floor (1):**
```
Floor Name: "Eerste Verdieping"
Floor Center: [Transform at Y = 5000]
Enemy Count: 5
Spawn Radius: 20000
```

**Total: 18 enemies across 3 floors!**

---

## 📊 Console Output

```
[EnemySpawnManager] 🏢 Floor 0 (Kelder): Spawning 3 enemies
[EnemySpawnManager] ✅ Kelder: Spawned enemy 1/3 at (-2000, -5000, 3000)
[EnemySpawnManager] ✅ Kelder: Spawned enemy 2/3 at (5000, -5000, -1000)
[EnemySpawnManager] ✅ Kelder: Spawned enemy 3/3 at (-3000, -5000, 8000)
[EnemySpawnManager] 🏢 Kelder COMPLETE: 3/3 enemies spawned

[EnemySpawnManager] 🏢 Floor 1 (Gelijksvloers): Spawning 10 enemies
...
[EnemySpawnManager] 🏢 Gelijksvloers COMPLETE: 10/10 enemies spawned

[EnemySpawnManager] 🏢 Floor 2 (Eerste Verdieping): Spawning 5 enemies
...
[EnemySpawnManager] 🏢 Eerste Verdieping COMPLETE: 5/5 enemies spawned

[EnemySpawnManager] 🎯 SPAWN COMPLETE: 18 total enemies spawned across 3 floors
```

---

## 🔧 Tips

### Different Sizes Per Floor:
- Basement small? → Lower spawn radius (10000)
- Ground floor huge? → Higher spawn radius (40000)
- Each floor independent!

### Skip A Floor:
- Set Enemy Count = 0
- Floor will be skipped

### Add More Floors:
- Change Floors array size to 4, 5, etc.
- Add more floor center points
- Configure each one!

---

## ✅ Benefits

1. ✅ **Per-floor control** (count, radius, position)
2. ✅ **Independent settings** (each floor different)
3. ✅ **Visual gizmos** (see all floors in editor)
4. ✅ **Clear logging** (know which floor spawned what)
5. ✅ **Flexible** (skip floors, different sizes, etc.)

---

## 🎯 Summary

**What you get:**
- ✅ Spawn enemies on multiple floors
- ✅ Different count per floor
- ✅ Different radius per floor
- ✅ Visual gizmos (red/green/blue circles)
- ✅ Clear per-floor logging

**What you need:**
1. Create floor center transforms
2. Configure each floor in inspector
3. Run game!

**Your enemies now spawn across all 3 floors! 🏢✨**
