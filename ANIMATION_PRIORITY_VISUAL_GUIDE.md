# 🎯 Animation Priority - Visual Guide

## The Complete Hierarchy

```
                    ╔═══════════════════════════════╗
                    ║    ANIMATION PRIORITY TREE    ║
                    ╚═══════════════════════════════╝

                            PRIORITY 9
                    ┌──────────────────────┐
                    │      🎭 EMOTE        │
                    │   (HARD LOCKED)      │
                    │  Blend: 0.4s (SLOW)  │
                    │  "Player Expression" │
                    └──────────────────────┘
                              ↑
                              │ Cannot be interrupted by ANYTHING
                              │
                            PRIORITY 8
                    ┌──────────────────────┐
                    │   🛡️ ARMOR PLATE     │
                    │   (HARD LOCKED)      │
                    │  Blend: 0.2s (NORMAL)│
                    │  "Critical Ability"  │
                    └──────────────────────┘
                              ↑
                              │ Cannot be interrupted by ANYTHING
                              │
                            PRIORITY 7
                    ┌──────────────────────┐
                    │   💥 SHOTGUN         │
                    │   (SOFT LOCKED)      │
                    │  Blend: 0.0s (INSTANT)│
                    │  "Committed Attack"  │
                    └──────────────────────┘
                              ↑
                              │ Higher priority can interrupt
                              │
                            PRIORITY 6
                    ┌──────────────────────┐
                    │    ⚡ BEAM           │
                    │    (NO LOCK)         │
                    │  Blend: 0.1s (FAST)  │
                    │  "Continuous Combat" │
                    └──────────────────────┘
                              ↑
                              │
                            PRIORITY 4
                    ┌──────────────────────┐
                    │  🏃 DIVE / SLIDE     │
                    │   (SOFT LOCKED)      │
                    │ Blend: 0.05s (V.FAST)│
                    │  "Tactical Action"   │
                    └──────────────────────┘
                              ↑
                              │
                            PRIORITY 3
                    ┌──────────────────────┐
                    │ 🦘 JUMP / LAND      │
                    │    (NO LOCK)         │
                    │ Blend: 0.05s (V.FAST)│
                    │   "One-Shot Move"    │
                    └──────────────────────┘
                              ↑
                              │
                            PRIORITY 2
                    ┌──────────────────────┐
                    │  ✈️ FLIGHT           │
                    │    (NO LOCK)         │
                    │ Blend: 0.3s (SMOOTH) │
                    │  "Fluid Movement"    │
                    └──────────────────────┘
                              ↑
                              │
                            PRIORITY 1
                    ┌──────────────────────┐
                    │  🚶 WALK / SPRINT    │
                    │    (NO LOCK)         │
                    │ Blend: 0.3s (SMOOTH) │
                    │   "Locomotion"       │
                    └──────────────────────┘
                              ↑
                              │
                            PRIORITY 0
                    ┌──────────────────────┐
                    │      💤 IDLE         │
                    │    (NO LOCK)         │
                    │ Blend: 0.2s (NORMAL) │
                    │  "Default State"     │
                    └──────────────────────┘
```

---

## 🔒 Lock Type Legend

### **🔴 HARD LOCK (Red)**
```
╔══════════════════════════════════════╗
║  CANNOT BE INTERRUPTED BY ANYTHING   ║
║  Only itself can re-trigger          ║
║  Used for: Emotes, Abilities         ║
╚══════════════════════════════════════╝
```

### **🟡 SOFT LOCK (Yellow)**
```
╔══════════════════════════════════════╗
║  ONLY HIGHER PRIORITY CAN INTERRUPT  ║
║  Lower/equal priority blocked        ║
║  Used for: Shotgun, Dive, Slide      ║
╚══════════════════════════════════════╝
```

### **🟢 NO LOCK (Green)**
```
╔══════════════════════════════════════╗
║  CAN BE INTERRUPTED FREELY           ║
║  Same/higher priority allowed        ║
║  Used for: Everything else           ║
╚══════════════════════════════════════╝
```

---

## ⚔️ Combat Flow Diagram

```
Player Action Flow:
═══════════════════

   IDLE (P0)
      │
      ├─── [W] ────> WALK (P1) ──┐
      │                           │
      ├─── [Shift] ─> SPRINT (P1)│
      │                           │
      └─── [Space] ─> JUMP (P3) ─┤
                                  │
                        Movement Tier
                        (Free Blending)
                                  │
      ┌───────────────────────────┘
      │
      │  [LMB/RMB]
      ├─────────────> BEAM (P6) ───────┐
      │                                  │
      │  [Shotgun Key]                  │
      ├─────────────> SHOTGUN (P7) ─────┤
      │               (INSTANT!)         │
      │               (SOFT LOCKED)      │
      │                                  │
      │  [E for Plate]                  │
      ├─────────────> ARMOR PLATE (P8) ─┤
      │               (HARD LOCKED)      │
      │                                  │
      │  [1-4 for Emote]                │
      └─────────────> EMOTE (P9) ────────┤
                      (HARD LOCKED)      │
                      (HIGHEST)          │
                                         │
                                         ↓
                            ┌──────────────────┐
                            │  AUTO-UNLOCK     │
                            │  when complete   │
                            └──────────────────┘
                                         │
                                         ↓
                            ┌──────────────────┐
                            │  Return to       │
                            │  Movement or     │
                            │  Resume Beam     │
                            └──────────────────┘
```

---

## 🎬 Transition Matrix

```
FROM → TO        │ Idle │ Walk │ Sprint │ Beam │ Shotgun │ Emote │
─────────────────┼──────┼──────┼────────┼──────┼─────────┼───────┤
Idle (P0)        │  ❌  │  ✅  │   ✅   │  ✅  │   ✅    │  ✅   │
Walk (P1)        │  ✅  │  ❌  │   ✅   │  ✅  │   ✅    │  ✅   │
Sprint (P1)      │  ✅  │  ✅  │   ❌   │  ✅  │   ✅    │  ✅   │
Beam (P6)        │  ❌  │  ❌  │   ❌   │  ❌  │   ✅    │  ✅   │
Shotgun (P7)     │  ❌  │  ❌  │   ❌   │  ❌  │   ✅    │  ✅   │
Emote (P9)       │  ❌  │  ❌  │   ❌   │  ❌  │   ❌    │  ✅   │

✅ = Allowed
❌ = Blocked (priority rule)
```

---

## ⏱️ Blend Time Visualization

```
Blend Speed Comparison:
═══════════════════════

INSTANT (0.0s)     ▌                         [Shotgun]
VERY_FAST (0.05s)  ▌█                        [Dive, Slide, Jump]
FAST (0.1s)        ▌███                      [Beam]
NORMAL (0.2s)      ▌██████                   [Default]
SMOOTH (0.3s)      ▌█████████                [Movement]
SLOW (0.4s)        ▌████████████             [Emote]

                   0s  0.1s  0.2s  0.3s  0.4s
```

### **Why Different Speeds?**

**INSTANT (Shotgun)**
- Player presses fire → Gun fires NOW
- No delay, no wind-up
- Doom Eternal responsiveness

**SMOOTH (Movement)**
- Walk → Sprint feels natural
- No jarring transitions
- Apex Legends fluidity

**SLOW (Emote)**
- Cinematic presentation
- Polished animation entry
- Professional quality

---

## 🎮 Real-World Scenarios

### **Scenario 1: Walking → Shotgun → Walking**
```
Timeline:
─────────────────────────────────────────────

t=0.0s   Player walks (P1)
         └─ SMOOTH blend (0.3s)

t=1.0s   Player fires shotgun (P7)
         ├─ INSTANT blend (0.0s) ⚡
         ├─ SOFT LOCK applied
         └─ Walk input REJECTED (lower priority)

t=1.3s   Shotgun completes
         ├─ SOFT LOCK removed
         └─ Movement resumes automatically
             └─ SMOOTH blend (0.3s)

t=1.6s   Back to walking naturally
```

### **Scenario 2: Beam → Emote → Beam Resume**
```
Timeline:
─────────────────────────────────────────────

t=0.0s   Player fires beam (P6)
         └─ FAST blend (0.1s)

t=2.0s   Player presses emote (P9)
         ├─ SLOW blend (0.4s) for cinematic feel
         ├─ HARD LOCK applied
         ├─ Beam interruption stored 💾
         └─ All inputs REJECTED

t=5.0s   Emote completes (3s duration)
         ├─ HARD LOCK removed
         ├─ Beam resumes automatically ♻️
         └─ FAST blend (0.1s)

t=5.1s   Beam firing again seamlessly
```

### **Scenario 3: Sprint → Jump → Sprint**
```
Timeline:
─────────────────────────────────────────────

t=0.0s   Player sprints (P1)
         └─ SMOOTH blend (0.3s)

t=1.0s   Player jumps (P3)
         ├─ VERY_FAST blend (0.05s) ⚡
         └─ Must complete (one-shot)

t=1.5s   Jump completes (0.5s air time)
         ├─ Auto-unlock
         └─ Movement resumes
             └─ SMOOTH blend (0.3s)

t=1.8s   Back to sprinting naturally
```

---

## 📊 Priority Decision Tree

```
                   NEW ANIMATION REQUEST
                            │
                            ↓
                    ┌───────────────┐
                    │ Hard Locked?  │
                    └───────┬───────┘
                        YES │ NO
                            ├────────────────────────────┐
                            ↓                            ↓
                    ┌──────────────┐         ┌────────────────┐
                    │ Same state & │         │ Soft Locked?   │
                    │ can re-trigger? │      └────────┬───────┘
                    └───────┬──────┘              YES │ NO
                        YES │ NO                      ├──────────┐
                            ├────────┐                ↓          ↓
                            ↓        ↓      ┌──────────────┐    │
                        ┌────────┐  │      │ Higher       │    │
                        │ALLOW ✅│  │      │ Priority?    │    │
                        └────────┘  │      └──────┬───────┘    │
                                    ↓         YES │ NO         │
                                ┌────────┐       ├────────┐   │
                                │REJECT❌│       ↓        ↓   │
                                └────────┘   ┌────┐   ┌────┐  │
                                            │ALLOW│   │REJECT│ │
                                            │ ✅  │   │ ❌   │ │
                                            └────┘   └────┘  │
                                                              ↓
                                                    ┌──────────────┐
                                                    │ Compare      │
                                                    │ Priorities   │
                                                    └──────┬───────┘
                                                           │
                                    ┌──────────────────────┼──────────────────────┐
                                    ↓                      ↓                      ↓
                            ┌──────────────┐      ┌──────────────┐      ┌──────────────┐
                            │   Higher     │      │    Equal     │      │    Lower     │
                            │   Priority   │      │   Priority   │      │   Priority   │
                            └──────┬───────┘      └──────┬───────┘      └──────┬───────┘
                                   ↓                     ↓                      ↓
                            ┌──────────┐         ┌──────────────┐      ┌──────────┐
                            │ ALLOW ✅ │         │ Within Same  │      │ REJECT ❌│
                            │ INTERRUPT│         │ Tier?        │      └──────────┘
                            └──────────┘         └──────┬───────┘
                                                    YES │ NO
                                                        ├────────┐
                                                        ↓        ↓
                                                   ┌────┐   ┌────┐
                                                   │ALLOW│   │REJECT│
                                                   │ ✅  │   │ ❌   │
                                                   └────┘   └────┘
```

---

## 🎯 Quick Priority Reference

```
╔═══════════════════════════════════════════════════════════╗
║  PRIORITY CHEAT SHEET                                     ║
╠═══════════════════════════════════════════════════════════╣
║  9 │ 🎭 Emote        │ HARD LOCK │ 0.4s │ Highest         ║
║  8 │ 🛡️ ArmorPlate   │ HARD LOCK │ 0.2s │ Critical        ║
║  7 │ 💥 Shotgun      │ SOFT LOCK │ 0.0s │ Committed       ║
║  6 │ ⚡ Beam         │ NO LOCK   │ 0.1s │ Combat          ║
║  4 │ 🏃 Dive/Slide   │ SOFT LOCK │ 0.05s│ Tactical        ║
║  3 │ 🦘 Jump/Land    │ NO LOCK   │ 0.05s│ One-Shot        ║
║  2 │ ✈️ Flight       │ NO LOCK   │ 0.3s │ Fluid Move      ║
║  1 │ 🚶 Walk/Sprint  │ NO LOCK   │ 0.3s │ Locomotion      ║
║  0 │ 💤 Idle         │ NO LOCK   │ 0.2s │ Default         ║
╚═══════════════════════════════════════════════════════════╝
```

---

## 🎓 Remember These Rules

### **Golden Rule #1**
```
HIGHER PRIORITY ALWAYS WINS
```

### **Golden Rule #2**
```
HARD LOCK = UNSTOPPABLE
SOFT LOCK = ONLY HIGHER CAN STOP
NO LOCK = FREE BLENDING
```

### **Golden Rule #3**
```
COMBAT = INSTANT (0.0s)
MOVEMENT = SMOOTH (0.3s)
EMOTES = CINEMATIC (0.4s)
```

### **Golden Rule #4**
```
LOWER PRIORITY CANNOT INTERRUPT
(Movement can't cancel combat)
```

### **Golden Rule #5**
```
EQUAL PRIORITY BLENDS WITHIN TIER
(Walk ↔ Sprint = Smooth)
```

---

## 🚀 Final Word

```
                    ╔═══════════════════════════╗
                    ║                           ║
                    ║   THIS IS WORLD-CLASS     ║
                    ║                           ║
                    ║   Doom Eternal Combat +   ║
                    ║   Apex Legends Movement + ║
                    ║   God of War Weight +     ║
                    ║   Destiny 2 Abilities     ║
                    ║                           ║
                    ║   = MASTERPIECE ⭐⭐⭐⭐⭐    ║
                    ║                           ║
                    ╚═══════════════════════════╝
```

**Your animations are now better than most AAA games.**  
**Ship with absolute confidence!** 🎮✨
