# 🎯 WALL JUMP XP CHAIN - QUICK SETUP

## What You Get (NO ASSETS NEEDED!)

✅ **XP rewards** that scale exponentially (5 → 192 XP)  
✅ **Chain system** with 2-second window  
✅ **Visual feedback** with colors and animations  
✅ **Auto-created UI** (no prefabs!)  
✅ **XPManager integration** (uses existing system)  

## Setup (30 Seconds!)

### 1. Add to Scene
- Create empty GameObject: `WallJumpXPSystem`
- Add component: `WallJumpXPChainSystem`
- Done!

### 2. Test
- Wall jump → See "WALL JUMP! +5 XP"
- Wall jump again (within 2s) → "DOUBLE! x2 CHAIN +8 XP"
- Keep chaining → Watch it scale!

## Chain Rewards

| Chain | Title | XP | Color |
|-------|-------|-----|-------|
| x1 | WALL JUMP! | 5 | White |
| x2 | DOUBLE! | 8 | Green |
| x3 | TRIPLE! | 11 | Green |
| x4 | QUAD! | 17 | Cyan |
| x5 | MEGA! | 25 | Blue |
| x6 | ULTRA! | 38 | Orange |
| x7 | MONSTER! | 57 | Red |
| x8 | LEGENDARY! | 85 | Magenta |
| x9 | GODLIKE! | 128 | Yellow |
| x10+ | UNSTOPPABLE!!! | 192 | Gold |

## How It Works

**Chain continues:** Next wall jump within 2 seconds  
**Chain resets:** Landing on ground or timeout  
**XP formula:** `5 × (1.5 ^ (chain - 1))`  

## Visual Feedback

```
     LEGENDARY!        ← Title (colored, scaled)
     x8 CHAIN          ← Chain level
     +85 XP            ← XP earned (gold)
```

**Animation:** Fade in (0.2s) → Hold (0.5-1s) → Fade out (0.3s)  
**Scale:** Grows with chain (1.0 → 2.0)  
**Colors:** White → Green → Cyan → Orange → Red → Gold  

## Already Integrated!

✅ Triggers on every wall jump  
✅ Resets when landing  
✅ Tracks session stats  
✅ Grants XP through XPManager  

**Just add the script and play!**
