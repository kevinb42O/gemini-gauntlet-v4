# Momentum-Based Air Control System

## Overview
The movement controller now features **AAA-quality momentum-based air control** that feels incredible and realistic.

## How It Works

### **Camera Rotation = NO Effect on Movement**
- When you jump and look around with the mouse, your trajectory stays locked
- You maintain your momentum direction perfectly
- This prevents the "floaty" feeling of arcade games

### **WASD Strafing = 30% Air Control**
- Pressing WASD keys gives you **limited steering** while airborne
- You can adjust your direction but can't do sharp turns
- Feels natural and skill-based

### **No Input = Pure Momentum**
- Let go of WASD in mid-air → you glide in a perfect arc
- Camera rotation has ZERO effect on your trajectory
- Exactly like real physics!

## Tunable Parameters (Inspector)

### `airControlStrength` (default: 0.3)
- How much control you have in the air
- 0.0 = no control (pure momentum)
- 1.0 = full control (arcade style)
- **Recommended: 0.2-0.4** for AAA feel

### `airAcceleration` (default: 15)
- How quickly you can change direction
- Lower = more momentum preservation
- Higher = more responsive steering
- **Recommended: 10-20** for smooth control

### `maxAirSpeed` (default: 80)
- Maximum speed from air control alone
- Prevents infinite acceleration exploits
- Should be lower than ground moveSpeed
- **Recommended: 70-90** for balance

## Performance
- **Zero additional cost** - only runs when airborne
- Simple vector math, no raycasts or physics queries
- No impact on frame rate

## Examples

### Sprint Jump
1. Sprint forward (Shift + W)
2. Jump (Space)
3. Let go of W
4. **Result:** You fly forward with momentum, camera look doesn't change direction

### Mid-Air Strafe
1. Jump forward
2. Press A or D while airborne
3. **Result:** Gradual steering adjustment (30% control)
4. Can't do 90° turns, feels realistic

### Precision Landing
1. Sprint jump toward target
2. Use WASD to make small adjustments
3. Look around to scout landing zone
4. **Result:** Smooth, controlled landing without affecting trajectory

## Why This Feels Amazing
- **Momentum preservation** makes jumps feel weighty and satisfying
- **Limited air control** rewards skill and planning
- **Camera independence** lets you look around freely
- **No "ice skating"** or floating sensation
- Exactly how modern AAA games handle movement!
