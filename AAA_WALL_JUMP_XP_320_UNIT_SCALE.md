# 🎯 WALL JUMP XP - 320-UNIT CHARACTER SCALE FIX

## The Problem

Your character is **MASSIVE**:
- Height: 320 units
- Radius: 50 units
- **100x larger than standard Unity scale!**

The text was spawning at tiny scale (designed for 3-unit characters), so you couldn't see it!

## The Fix (Applied!)

### Updated Default Values

| Setting | Old (Standard) | New (320-Unit) | Multiplier |
|---------|---------------|----------------|------------|
| **Spawn Distance** | 3 | 300 | 100x |
| **Height Offset** | 2 | 200 | 100x |
| **Float Speed** | 2 | 100 | 50x |
| **Text Size** | 0.5 | 50 | 100x |
| **Font Size** | 4 | 400 | 100x |

### Why These Values?

```
Standard Character: 3 units tall
Your Character: 320 units tall
Scale Factor: 320 / 3 ≈ 100x

Therefore:
- Spawn distance: 3 × 100 = 300 units
- Height offset: 2 × 100 = 200 units
- Text size: 0.5 × 100 = 50 units
- Font size: 4 × 100 = 400
```

## Current Settings (In Inspector)

```
=== WORLD SPACE DISPLAY ===
Spawn Distance: 300        // 300 units from wall
Height Offset: 200         // 200 units above player
Float Speed: 100           // 100 units/sec upward
Text Lifetime: 2.0         // 2 seconds visible
Text Size: 50              // 50 world units

Font Size (in code): 400   // TextMeshPro font size
```

## Testing

### What You Should See:
1. **Wall jump** → Text spawns in front of you
2. **Text appears** 200 units above your position
3. **Text is HUGE** (readable from far away!)
4. **Text floats upward** at 100 units/sec
5. **Text fades out** after 2 seconds

### Debug Console Output:
```
[WallJumpXP] 🎯 CHAIN x1! Earned 5 XP
[WallJumpXP] Text spawned at position: (x, y, z)
```

## Fine-Tuning (If Needed)

### Text Too Close?
```
Spawn Distance: 300 → 500  // Further from wall
```

### Text Too High/Low?
```
Height Offset: 200 → 150   // Lower
Height Offset: 200 → 300   // Higher
```

### Text Too Small?
```
Text Size: 50 → 75         // Bigger
Font Size: 400 → 600       // In code
```

### Text Too Big?
```
Text Size: 50 → 30         // Smaller
Font Size: 400 → 250       // In code
```

### Text Moves Too Slow?
```
Float Speed: 100 → 150     // Faster upward
```

### Text Moves Too Fast?
```
Float Speed: 100 → 50      // Slower upward
```

## Recommended Settings for Your Scale

### Conservative (Easy to See)
```
Spawn Distance: 400
Height Offset: 250
Float Speed: 80
Text Size: 60
Text Lifetime: 2.5
```

### Balanced (Default)
```
Spawn Distance: 300
Height Offset: 200
Float Speed: 100
Text Size: 50
Text Lifetime: 2.0
```

### Aggressive (Fast-Paced)
```
Spawn Distance: 250
Height Offset: 150
Float Speed: 150
Text Size: 40
Text Lifetime: 1.5
```

## Troubleshooting

### Still Can't See Text?

1. **Check Console for Logs:**
   ```
   [WallJumpXP] 🎯 CHAIN x1! Earned 5 XP
   ```
   If you see this, text IS spawning!

2. **Check Scene View:**
   - Open Scene view while playing
   - Look for `WallJumpXP_Text` objects
   - They should be visible in scene

3. **Increase Text Size:**
   ```
   Text Size: 50 → 100
   ```

4. **Check Camera Distance:**
   - If camera is VERY far from player, increase spawn distance
   ```
   Spawn Distance: 300 → 600
   ```

5. **Check Text Position in Scene:**
   - Wall jump
   - Pause game
   - Select `WallJumpXP_Text` in hierarchy
   - Check position in inspector
   - Adjust spawn distance/height offset

### Text Behind You?
```
Spawn Distance: 300 → -300  // Negative = behind wall
```

### Text Spawning Inside Wall?
```
Spawn Distance: 300 → 500   // Further from wall
```

## Scale Reference

Your world is **100x larger** than standard Unity scale:

```
Standard Unity:
- Character: 2-3 units tall
- Door: 4 units tall
- Room: 10×10 units

Your Game:
- Character: 320 units tall
- Door: 400 units tall (estimated)
- Room: 1000×1000 units (estimated)

Text needs to match this scale!
```

## Performance Note

Even at 100x scale, the system is still optimized:
- Object pooling still works
- Zero GC allocations
- Same performance as small scale

**Scale doesn't affect performance!** 🚀

## Quick Test

1. Add `WallJumpXPWorldSpace` to scene
2. Wall jump
3. Look for **HUGE text** in front of you
4. Should see "WALL JUMP! x1 CHAIN +5 XP"
5. Text floats up and fades out

**If you see it, you're good to go!** ✅
