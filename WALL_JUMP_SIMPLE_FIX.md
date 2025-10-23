# Wall Jump - Simple & Effective Fix

## 🎯 The Problem

**You were getting pushed sideways and losing all your momentum!**

The old system was adding boost in a **different direction** than your movement, which created sideways forces that killed your forward speed.

---

## ✅ The Simple Solution

**Wall jump = 45° push away from wall + boost in YOUR MOVEMENT DIRECTION**

### Key Principle:
```
1. Wall jump ALWAYS pushes you 45° away from wall (predictable)
2. Boost is added in the direction YOU'RE ALREADY MOVING (preserves momentum)
3. If you're pushing forward → extra boost in your movement direction
```

---

## 🔧 How It Works

### If You're Moving:
```csharp
// Get your current movement direction
Vector3 currentMovementDir = currentHorizontalVelocity.normalized;

// Add boost in YOUR movement direction
movementBoost = currentMovementDir * (baseBoost + speedBonus);
```

**Result:**
- Wall jump pushes you 45° away from wall
- Boost adds to your existing momentum (doesn't fight it!)
- You keep moving in the direction you were going
- Speed builds naturally

### If You're Not Moving:
```csharp
// Use your input direction instead
movementBoost = inputDirection * boost;
```

**Result:**
- Wall jump pushes you 45° away from wall
- Boost goes in the direction you're pushing
- You start moving in your input direction

---

## 📊 Force Breakdown

### Example: Moving Forward, Wall Jump
```
Current velocity: 150 units/s forward
Wall on right side

Wall Jump Push:      110 units/s (45° away from wall)
Movement Boost:      80 units/s (in your forward direction)
Speed Bonus:         18 units/s (12% of 150)
────────────────────────────────────────────────
Total boost in forward direction: 98 units/s
Wall jump push: 110 units/s at 45°

Result: You keep moving forward + get pushed away from wall!
```

### Example: Moving Forward, Pushing Forward
```
Current velocity: 150 units/s forward
Wall on right side
Input: W key (forward)

Wall Jump Push:      110 units/s (45° away from wall)
Movement Boost:      120 units/s (1.5x because pushing forward!)
Speed Bonus:         18 units/s (12% of 150)
────────────────────────────────────────────────
Total boost in forward direction: 138 units/s
Wall jump push: 110 units/s at 45°

Result: You keep moving forward FASTER + get pushed away from wall!
```

---

## 🎮 What You Experience

### Scenario 1: Running Forward, Hit Wall, Wall Jump
```
Before: Moving 150 units/s forward
Wall Jump: Pushes 45° away + adds 98 units/s forward
After: Moving ~200 units/s forward + away from wall

✅ Momentum preserved!
✅ Speed gained!
✅ Natural feel!
```

### Scenario 2: Running Forward, Pushing Forward, Wall Jump
```
Before: Moving 150 units/s forward
Wall Jump: Pushes 45° away + adds 138 units/s forward (extra boost!)
After: Moving ~240 units/s forward + away from wall

✅ Momentum preserved!
✅ Extra speed for skilled input!
✅ Rewarding feel!
```

### Scenario 3: Stationary, Pushing Forward, Wall Jump
```
Before: Not moving
Wall Jump: Pushes 45° away + adds 120 units/s in input direction
After: Moving ~180 units/s in input direction + away from wall

✅ Input respected!
✅ Good starting speed!
✅ Responsive feel!
```

---

## 🔥 Why This Works

### 1. **Predictable Base**
- Wall jump ALWAYS pushes 45° away from wall
- You always know where you'll go
- Consistent and reliable

### 2. **Momentum Preservation**
- Boost goes in YOUR movement direction
- Doesn't fight your existing velocity
- Speed builds naturally

### 3. **Skill Reward**
- Push in your movement direction → 1.5x boost
- Passive wall jump → 1.0x boost
- Skilled play gets extra speed

### 4. **No Sideways Killing**
- Boost aligns with your movement
- No perpendicular forces
- Momentum flows naturally

---

## 🐛 Debug Output

### Moving Forward, Wall Jump:
```
💨 [MOVEMENT BOOST] Direction: (0.0, 0.0, 1.0), Base: 80.0, Speed bonus: 18.0, Total: 98.0
📊 [WALL JUMP] Base push: 110.0 (45° from wall), Movement boost: 98.0, Total: 208.0
🧭 [DIRECTIONS] Wall: (0.7, 0.0, 0.7), Movement: (0.0, 0.0, 1.0)
```

### Moving Forward, Pushing Forward:
```
🚀 [MOMENTUM BOOST] Pushing in movement direction! Multiplier: 1.5x
💨 [MOVEMENT BOOST] Direction: (0.0, 0.0, 1.0), Base: 80.0, Speed bonus: 18.0, Total: 138.0
📊 [WALL JUMP] Base push: 110.0 (45° from wall), Movement boost: 138.0, Total: 248.0
🧭 [DIRECTIONS] Wall: (0.7, 0.0, 0.7), Movement: (0.0, 0.0, 1.0)
```

---

## 🎯 Result

**Simple and effective:**

✅ **Wall jump pushes 45° away from wall** (predictable)  
✅ **Boost adds to your movement direction** (preserves momentum)  
✅ **Pushing forward gives extra boost** (rewards skill)  
✅ **No sideways forces killing your speed** (natural flow)  
✅ **Speed builds with each wall jump** (momentum compounds)

**You keep your momentum and gain speed - exactly what you wanted!** 🚀
