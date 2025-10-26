# 🌊 Grapple Mode Comparison

## SWING MODE (Default) - Worms Style
```
     ANCHOR (Fixed Point)
        🎯
         |
         | ← ROPE (FIXED LENGTH - Never changes!)
         |
         O ← YOU (Pendulum bob)
        /|\ 
         |
        / \

PHYSICS:
┌─────────────────────────────────────────┐
│ Rope Length: CONSTANT                   │
│ Gravity: FULL (creates arc)            │
│ Damping: NONE (max speed preservation) │
│ Input: Tangential force (adds energy)  │
└─────────────────────────────────────────┘

EXAMPLE - Bridge Swing:
    Bridge ═══════╗
                  ║ ← Anchor
                  ║
                  ○ ← You falling
                 ↙ ↘
              ↙       ↘
           ↙             ↘
        ○                 ○
    (bottom of arc)    (swing back up)
        ↑
    Press W here = Add energy!
```

## PULL MODE (Hold LeftAlt) - Winch Style
```
     ANCHOR (Fixed Point)
        🎯
         ↑↑↑ ← PULLING FORCE
         |
         | ← ROPE (SHORTENING)
         ↑
         O ← YOU (Being winched)
        /|\ 
         |
        / \

PHYSICS:
┌─────────────────────────────────────────┐
│ Rope Length: SHRINKING                  │
│ Gravity: FULL (still applies)          │
│ Damping: 0.85 (prevents bounce)        │
│ Pull Force: Distance-based (far=strong)│
└─────────────────────────────────────────┘

EXAMPLE - Tower Climb:
    Tower Top ═══╗
                 ║ ← Anchor
                 ║↑
            ○ ───┘ ← Hold LeftAlt
          You      Rope shortens
           ↑       Pull up!
```

---

## 🎮 CONTROL FLOW

### Swing Mode (Natural Pendulum)
```
1. Jump off bridge
   ↓
2. Shoot rope (Mouse5)
   ↓
3. Rope attaches → FIXED LENGTH set
   ↓
4. Gravity pulls you down
   ↓
5. Swing in arc (rope constraint keeps length)
   ↓
6. Press WASD to add energy (tangential force)
   ↓
7. Release Mouse5 at peak → FLY with momentum!
```

### Pull Mode (Active Winch)
```
1. Jump off cliff
   ↓
2. Shoot rope (Mouse5)
   ↓
3. Rope attaches → Initial length set
   ↓
4. HOLD LeftAlt → Switch to Pull Mode
   ↓
5. Pull force activates (distance-based)
   ↓
6. Rope shortens as you approach anchor
   ↓
7. Stop at target distance (800 units)
   ↓
8. Release LeftAlt → Return to Swing Mode
```

---

## 🔬 PHYSICS COMPARISON

| Aspect | SWING Mode | PULL Mode |
|--------|------------|-----------|
| **Rope Length** | FIXED (never changes) | SHORTENING (active winch) |
| **Gravity** | Full effect (creates arc) | Full effect (adds weight) |
| **Damping** | 0% (no energy loss) | 15% (prevents bounce) |
| **Input Effect** | Adds energy to swing | No effect (auto-pull) |
| **Speed Cap** | None (build infinite speed!) | 5000 units/s (smooth cap) |
| **Use Case** | Momentum, skill, arcs | Climbing, positioning |

---

## 🎯 SKILL EXPRESSION

### Swing Mode Mastery
```
BEGINNER: Shoot rope → Swing → Release randomly
          ↓
          Inconsistent landings, low speed

INTERMEDIATE: Shoot rope → Swing → Release at peak
              ↓
              Consistent arcs, medium speed

EXPERT: Shoot while falling → Add energy at bottom → Chain swings
        ↓
        Massive speed, precision landings, style points!
```

### Pull Mode Mastery
```
BEGINNER: Hold LeftAlt entire time
          ↓
          Smooth climb but slow

INTERMEDIATE: Tap LeftAlt near top
              ↓
              Quick bursts, faster climb

EXPERT: Swing → Switch to Pull → Switch back to Swing
        ↓
        Hybrid movement, maximum versatility!
```

---

## 🧪 TESTING SCENARIOS

### Scenario 1: Bridge Swing (Worms Classic)
```
START:  Bridge ═══════╗
                      ║
        YOU →    ○    ║
                     ↙ 
PHASE 1:          ○ ← Falling
                 ↙
PHASE 2:      ○ ← Bottom of swing
             ↙ (Press W to add energy!)
            ↙
PHASE 3:   ○ ← Swinging back up
          ↙
         ↙
END:   ○ → Release at peak → FLY!
```

### Scenario 2: Tower Climb (Pure Pull)
```
         Tower ═══╗
                  ║ ← Anchor
                  ║
START: ○ ─────────┘ ← Hold LeftAlt
       ↑
       │ (Rope shortens)
       ↑
       ○ ← Pulling up
       ↑
       │ (Gravity resists)
       ↑
END:   ○ ─╗ ← At target distance
       │  ║
       ╚══╝ Land on platform!
```

### Scenario 3: Hybrid Movement (Expert)
```
Phase 1: SWING
    ○ ← Shoot rope, build speed
   ↙ ↘
  ↙     ↘
 ○       ○ ← High speed!

Phase 2: Switch to PULL (LeftAlt)
         Rope shortens
    ○ → ○ → ○ ← Controlled rise
    
Phase 3: Release PULL, back to SWING
           Maintain height, swing forward
    ○ ─→ ○ ─→ ○ ← Speed + altitude!
```

---

## 🎨 VISUAL INDICATORS

### Rope Color (Runtime)
- **CYAN**: Swing Mode (fixed, pendulum)
- **YELLOW**: Pull Mode (shortening, winch)
- **Brightness**: Speed (brighter = faster)

### Scene View Gizmos
- **Cyan Sphere**: Fixed rope constraint (Swing)
- **Red Sphere**: Current length (Pull)
- **Green Sphere**: Target pull distance
- **Green Ray**: Velocity vector

---

## 💡 PRO TIPS

### Swing Mode
✅ **Add energy at bottom of arc** (W key = speed boost)  
✅ **Release at peak** (maximum forward momentum)  
✅ **Chain swings** (speed compounds infinitely!)  
✅ **Use slopes** (start swing from high point)  

### Pull Mode  
✅ **Tap LeftAlt** (burst pulls for efficiency)  
✅ **Switch near target** (avoid overshoot)  
✅ **Combine with jump** (launch off at top)  
✅ **Use for vertical** (climb towers/cliffs)  

### Hybrid
✅ **Swing → Pull → Swing** (speed + control)  
✅ **Pull to peak → Release** (launch upward)  
✅ **Swing low → Pull high** (U-turn maneuver)  
✅ **Build speed, then pull** (momentum climb)  

---

**🌊 TWO MODES, INFINITE POSSIBILITIES! 🪝**
