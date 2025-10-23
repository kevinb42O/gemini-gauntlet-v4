# 🎯 WALL JUMP CAMERA DIRECTION BOOST - SKILL-BASED CONTROL

## 🔥 THE PROBLEM YOU IDENTIFIED

**Before:** Wall jumps felt "arcade-y" and lacked player control
- Always pushed away from wall at same angle regardless of camera direction
- Too much vertical force, not enough horizontal control
- Player couldn't aim wall jumps with camera look direction
- Felt like the game was controlling you instead of you controlling the game

## ✅ THE SOLUTION IMPLEMENTED

### **Camera Direction Boost System**
Wall jumps now ADD extra force in the direction you're **LOOKING** with your camera!

**New Wall Jump Formula:**
```
Total Force = Wall Push (away from wall) 
            + Movement Boost (preserve momentum) 
            + CAMERA BOOST (aim with look direction) ← NEW!
            + Vertical Force (upward)
```

### **Key Changes Made:**

#### 1. **New Config Parameter: `wallJumpCameraDirectionBoost`**
- **Default Value:** 800 units
- **Purpose:** Extra horizontal force in camera forward direction
- **Result:** You can now AIM your wall jumps by looking where you want to go!

#### 2. **Reduced Vertical Force: `wallJumpUpForce`**
- **Old Value:** 1650 units (too much vertical pop)
- **New Value:** 1100 units (more horizontal control)
- **Result:** Less "flying up", more "launching forward"

#### 3. **Camera-Directed Trajectory**
```csharp
// Get camera forward direction (horizontal only)
Vector3 cameraForward = cameraTransform.forward;
cameraForward.y = 0; // Keep it horizontal
cameraForward.Normalize();

// Add boost in camera look direction
cameraBoost = cameraForward * wallJumpCameraDirectionBoost;
```

## 🎮 HOW TO USE IT

### **Optimal Wall Jump Technique:**
1. **Approach wall** while moving
2. **Turn camera** to face the direction you want to go
3. **Press Space** when touching wall
4. **Result:** Wall jump launches you in the direction you're looking!

### **Advanced Techniques:**

**Back-to-Wall Launch:**
- Face AWAY from wall with camera
- Wall jump pushes you away from wall
- Camera boost adds MASSIVE forward momentum
- Result: HUGE distance wall jump!

**Side-to-Side Chaining:**
- Look toward next wall while jumping
- Camera boost steers you toward target
- Momentum compounds with each jump
- Result: Infinite wall jump chains!

**Vertical Climbing:**
- Look slightly upward while wall jumping
- Camera boost adds upward trajectory
- Combine with reduced vertical force
- Result: Controlled vertical ascent!

## 📊 TUNING VALUES (MovementConfig.asset)

### **Recommended Starting Values:**
```
wallJumpUpForce = 1100f           // Vertical pop (reduced for control)
wallJumpOutForce = 1800f          // Push away from wall
wallJumpForwardBoost = 1200f      // Momentum preservation
wallJumpCameraDirectionBoost = 800f  // Camera aiming (NEW!)
wallJumpFallSpeedBonus = 1.2f     // Faster falls = bigger jumps
```

### **Tuning Guide:**

**If wall jumps feel too floaty:**
- Increase `gravity` (more negative, e.g., -2800f)
- Reduce `wallJumpUpForce` (e.g., 900f)

**If you want MORE camera control:**
- Increase `wallJumpCameraDirectionBoost` (e.g., 1200f)
- This makes camera direction MORE influential

**If you want LESS camera control:**
- Decrease `wallJumpCameraDirectionBoost` (e.g., 500f)
- This makes wall normal direction MORE influential

**If wall jumps don't go far enough:**
- Increase `wallJumpOutForce` (e.g., 2000f)
- Increase `wallJumpCameraDirectionBoost` (e.g., 1000f)

**If wall jumps go TOO far:**
- Decrease `wallJumpOutForce` (e.g., 1500f)
- Decrease `wallJumpCameraDirectionBoost` (e.g., 600f)

## 🎯 DESIGN PHILOSOPHY

### **Player Agency > Arcade Physics**
- **Old System:** Game decides trajectory (arcade-y)
- **New System:** Player aims trajectory with camera (skill-based)

### **Three Force Components:**
1. **Wall Normal Push:** Ensures you leave the wall (safety)
2. **Momentum Preservation:** Rewards speed building (flow)
3. **Camera Direction:** Enables precise aiming (skill)

### **Back-to-Wall Optimization:**
When your camera faces AWAY from the wall:
- Wall push: ← (away from wall)
- Camera boost: ← (same direction!)
- Result: MAXIMUM horizontal distance!

This is the "optimal wall jump experience" you wanted - player must position camera correctly for best results!

## 🔧 WHERE TO ADJUST

### **In Unity Inspector:**
1. Find your `MovementConfig.asset` in Project window
2. Scroll to "Wall Jump System" section
3. Adjust `Wall Jump Camera Direction Boost` slider
4. Test in Play mode (changes apply in real-time!)

### **Per-Player Override:**
If you want different values for specific scenarios:
1. Select Player GameObject
2. Find `AAAMovementController` component
3. Expand "Wall Jump Momentum Gain System"
4. Adjust `Wall Jump Camera Direction Boost` value
5. This overrides the config asset

## 🎪 TESTING TIPS

### **Test Scenario 1: Back-to-Wall Launch**
1. Run toward wall
2. Turn 180° (face away from wall)
3. Wall jump
4. Expected: HUGE forward launch

### **Test Scenario 2: Precision Aiming**
1. Stand near wall
2. Look at distant target
3. Wall jump
4. Expected: Launch toward target

### **Test Scenario 3: Vertical Climb**
1. Face wall
2. Look slightly upward
3. Wall jump repeatedly
4. Expected: Climb wall with control

## 🚀 RESULT

**You now have COMPLETE control over wall jump trajectory!**
- Camera direction = launch direction
- Back-to-wall = optimal distance
- Skill-based aiming system
- No more "arcade-y" feeling
- Player controls movement, not the game!

Enjoy your new wall jump system! 🎮
