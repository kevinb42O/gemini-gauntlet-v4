# 🔥 Enemy Companion - Quick Start

## 30 Second Setup

1. **Select** any companion GameObject
2. **Add Component** → `EnemyCompanionBehavior`
3. **Check** "Is Enemy" ✓
4. **Done!** Your companion is now an enemy

---

## Essential Settings

### Must Configure
- ✅ **Is Enemy**: Check this box

### Recommended Settings
- **Player Detection Radius**: `6000` (how far enemy sees player)
- **Attack Range**: `1500` (how close before attacking)
- **Aggression Multiplier**: `1.2` (pursuit intensity)

### Optional Features
- **Enable Patrol**: Check for area scouting
- **Patrol Points**: Drag empty GameObjects for patrol route
- **Require Line Of Sight**: Check for stealth gameplay

---

## Visual Guide

### In Inspector:
```
EnemyCompanionBehavior
├─ 🔥 ENEMY MODE
│  └─ [✓] Is Enemy  ← CHECK THIS!
│
├─ 🎯 HUNTING BEHAVIOR
│  ├─ Player Detection Radius: 6000
│  ├─ Attack Range: 1500
│  └─ Aggression Multiplier: 1.2
│
├─ 🚶 PATROL BEHAVIOR
│  ├─ [ ] Enable Patrol
│  ├─ Patrol Points: Empty
│  ├─ Patrol Wait Time: 2
│  └─ Random Patrol Radius: 2000
│
└─ 🔍 DETECTION SETTINGS
   ├─ Detection Interval: 0.3
   ├─ [ ] Require Line Of Sight
   └─ Line Of Sight Blockers: Default
```

---

## Common Presets

### 🏃 Aggressive Hunter
```
Detection Radius: 8000
Attack Range: 2000
Aggression: 1.8
Patrol: OFF
```
*Relentlessly chases player from far away*

### 🛡️ Patrolling Guard
```
Detection Radius: 5000
Attack Range: 1200
Aggression: 1.0
Patrol: ON (with patrol points)
```
*Guards an area, attacks on sight*

### 🥷 Stealth Assassin
```
Detection Radius: 4000
Attack Range: 800
Aggression: 1.5
Line of Sight: REQUIRED
```
*Only attacks when it can see you*

---

## Gizmo Colors (Scene View)

When enemy selected:
- 🟡 **Yellow Sphere** = Detection radius
- 🔴 **Red Sphere** = Attack range
- 🔵 **Blue Sphere** = Patrol area
- 🔴 **Red Line** = Tracking player (play mode)

---

## Troubleshooting

### Enemy doesn't move?
- Check "Is Enemy" is enabled
- Verify player is within detection radius (yellow sphere)
- Ensure NavMesh is baked

### Enemy doesn't attack?
- Player must be within attack range (red sphere)
- Check companion has weapons configured
- Verify line of sight if required

### Want to test?
- Right-click component → "📊 Show Enemy Status"
- Check console for distance and state info

---

## Pro Tips

💡 **Multiple Enemies**: Each can have different settings
💡 **Patrol Points**: Create empty GameObjects as waypoints
💡 **Balance**: Higher detection = harder, lower = easier
💡 **Stealth**: Enable line of sight for hide-and-seek gameplay
💡 **Debug**: Enable "Show Debug Info" for detailed logs

---

## That's It!

Your companion is now a fully functional enemy that:
- ✅ Hunts the player
- ✅ Uses all combat abilities
- ✅ Jumps and repositions
- ✅ Can patrol areas
- ✅ Works with existing systems

**No code changes needed. Just add the script and check the box!**

---

For detailed documentation, see: `EnemyCompanionSetupGuide.md`
