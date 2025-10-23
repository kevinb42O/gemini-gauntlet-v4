# 🩸 BLEEDING OUT CRAWLING SYSTEM

## ✅ What Changed

### **BEFORE:**
- Movement was disabled immediately when bleeding out started
- Camera stayed in first-person view
- Player was stuck, couldn't move

### **AFTER:**
- ✅ **Player CAN MOVE** while bleeding out (crawl, crouch)
- ✅ **Camera goes to overhead view** (10 units above player)
- ✅ **Top-down view** so you can see where you're going
- ✅ **Movement only disabled** when timer expires (actual death)

---

## 🎮 How It Works Now

### **When You Take Fatal Damage:**

1. **Bleeding Out Starts**
   - Health drops to 0
   - Camera smoothly transitions to **overhead view** (10 units up)
   - You can see yourself from above
   - **YOU CAN STILL MOVE!** (WASD, crouch, etc.)

2. **While Bleeding Out (30 seconds):**
   - Circular progress UI shows
   - Blood overlay pulsates
   - Timer counts down
   - **You can crawl toward teammates** (future revive mechanic)
   - Camera stays overhead, following you

3. **If Timer Expires:**
   - Movement gets disabled
   - "CONNECTION LOST" appears
   - Scene reloads after 2 seconds

4. **If You Press E (with self-revive):**
   - You stop bleeding out
   - Camera returns to normal
   - You're revived with full health

---

## 📐 Camera Settings

### **DeathCameraController:**
```
Camera Height: 10 units (adjustable in Inspector)
Zoom Duration: 2 seconds (smooth transition)
Pitch Angle: 60° (looking down)
View Type: Top-down overhead
```

### **Want Different Camera Height?**
```csharp
// In DeathCameraController Inspector:
Camera Height = 10f  // Default (good for most games)
Camera Height = 8f   // Closer view
Camera Height = 15f  // Further out
Camera Height = 20f  // Very far overhead
```

---

## 🎯 Movement During Bleeding Out

### **What You CAN Do:**
✅ Walk/crawl with WASD
✅ Crouch (stay in crouch position)
✅ Look around with mouse
✅ Rotate your view
✅ Move toward teammates for revive
✅ See the environment from overhead

### **What You CANNOT Do:**
❌ Sprint (too injured)
❌ Jump (too weak)
❌ Shoot weapons (incapacitated)
❌ Use abilities (no energy)

*Note: Sprint/jump/abilities are already blocked by other systems, no additional changes needed*

---

## 🔮 Future Multiplayer Expansion

This system is **100% ready** for teammate revives:

### **How It Will Work:**
1. **Player goes down** → Overhead camera activates
2. **Player crawls toward teammate** → Overhead view helps navigate
3. **Teammate approaches** → Can see both players from above
4. **Teammate holds E** → Revive progress bar fills
5. **Revive complete** → Camera returns to normal, player stands up

### **What You'll Need To Add Later:**
- Teammate proximity detection
- Revive interaction prompt for teammates
- Revive progress bar for teammate
- Interrupt system if teammate stops
- Notification when teammate is reviving you

**Current system handles all the camera and movement logic - just add the teammate interaction!**

---

## 🎬 Visual Flow

```
[FATAL DAMAGE]
     ↓
[Camera Smoothly Moves Up] (2 seconds)
     ↓
[Overhead View Established] (10 units above)
     ↓
[Player Can Crawl Around] ←→ [Bleeding Out UI Active]
     ↓
[Timer Expires OR Press E]
     ↓
[Movement Disabled]
     ↓
[Scene Reloads OR Revive Animation]
```

---

## 🔧 Technical Details

### **Key Changes:**

**PlayerHealth.cs:**
- Removed `DisableAllMovementForDeath()` from Die() method
- Added camera transition when bleeding out starts
- Movement only disabled in `OnBleedOutComplete()` (when timer expires)

**DeathCameraController.cs:**
- Changed from "behind player" to "directly overhead"
- Renamed `zoomOutDistance` to `cameraHeight`
- Added `StartBleedOutCameraMode()` method
- Camera position: `playerPosition + Vector3.up * cameraHeight`

---

## 🎮 Testing Checklist

- [ ] Take fatal damage → Camera goes overhead
- [ ] While bleeding out → Try to move with WASD (should work!)
- [ ] While bleeding out → Try to crouch (should work!)
- [ ] While bleeding out → Look around with mouse (should work!)
- [ ] Let timer expire → Movement stops
- [ ] Use self-revive → Camera returns to normal
- [ ] Camera height feels good (adjust if needed)

---

## 💡 Customization Tips

### **Camera Height Recommendations:**

**Tight Spaces / Indoor:**
```
cameraHeight = 8f  // Lower camera for ceiling clearance
```

**Open Areas / Outdoor:**
```
cameraHeight = 12f  // Higher for better overview
```

**Battle Royale Style:**
```
cameraHeight = 15f  // Very high, see teammates coming
```

**Tactical / Realistic:**
```
cameraHeight = 10f  // Default - good balance
```

### **Zoom Duration:**
```
zoomOutDuration = 1.5f  // Faster transition
zoomOutDuration = 2.5f  // Slower, more cinematic
```

---

## 🎯 Result

You now have a **complete bleeding out system** where:
- ✅ Camera smoothly transitions to overhead view
- ✅ Player can crawl around while bleeding out
- ✅ Perfect for future teammate revive mechanics
- ✅ Movement only disabled when actually dead
- ✅ Clean, cinematic camera transition

**Crawl toward your teammates and hope they revive you!** 🩸
