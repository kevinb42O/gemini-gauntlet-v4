# 🎨 PARTICLE PAINTING - QUICK START
**Get persistent particles working in 5 minutes!**

---

## ✅ What You Have Now:

I created 3 scripts for you:
1. ✅ `PooledVFX.cs` - Returns VFX to pool after lifetime
2. ✅ `ParticleDecalManager.cs` - Manages persistent particle decals
3. ✅ `ParticleCollisionHandler.cs` - Detects collisions and creates decals

---

## 🚀 SETUP (5 Steps)

### Step 1: Create ParticleDecalManager (2 min)
1. Hierarchy → Create Empty → Name: `ParticleDecalManager`
2. Add Component → `ParticleDecalManager`
3. Set in Inspector:
   - Decal Lifetime: **5**
   - Max Active Decals: **500**
   - Pool Size: **100**

### Step 2: Create Decal Prefab (2 min)
1. Hierarchy → Effects → Particle System → Name: `ParticleDecal`
2. Configure ParticleSystem:
   - Duration: **5**
   - Looping: **OFF**
   - Start Lifetime: **5**
   - Start Speed: **0**
   - Start Size: **0.2**
   - Max Particles: **1**
   - Simulation Space: **World**
3. Drag to Project folder (make prefab)
4. Delete from Hierarchy
5. **Assign prefab** to ParticleDecalManager's "Decal Particle Prefab" field

### Step 3: Add to Your VFX Prefabs (1 min per VFX)

For **each** of your 16 VFX prefabs:

1. Open VFX prefab
2. Find main ParticleSystem component
3. **Add Component:** `ParticleCollisionHandler`
4. **Add Component:** `PooledVFX` (if not already added)
5. **Enable Collision Module:**
   - Check "Collision" checkbox
   - Type: **World**
   - Mode: **3D**
   - Send Collision Messages: **✓** (CRITICAL!)

### Step 4: Test It!
1. Play game
2. Fire at wall
3. **Particles should stick for 5 seconds!** 🎉

---

## 🎯 INSPECTOR SETTINGS (Copy These)

### ParticleDecalManager:
```
Decal Lifetime: 5
Max Active Decals: 500
Pool Size: 100
Enable Debug Logs: ☐
```

### ParticleCollisionHandler (on each VFX):
```
Create Decals On Collision: ✓
Decal Size Multiplier: 1.0
Decal Layers: Everything
Max Decals Per Frame: 10
Enable Debug Logs: ☐
```

### PooledVFX (on each VFX):
```
Lifetime: 2.0
Use Particle System Duration: ✓
Enable Debug Logs: ☐
```

### VFX ParticleSystem Collision Module:
```
Collision: ✓ (enabled)
Type: World
Mode: 3D
Dampen: 0
Bounce: 0
Lifetime Loss: 1.0
Send Collision Messages: ✓ (CRITICAL!)
Collides With: Everything
```

---

## ⚠️ CRITICAL SETTINGS

**These MUST be correct or it won't work:**

1. ✅ ParticleSystem Collision Module: **Enabled**
2. ✅ Send Collision Messages: **TRUE**
3. ✅ ParticleCollisionHandler: **Added to VFX**
4. ✅ ParticleDecalManager: **Exists in scene**
5. ✅ Decal Particle Prefab: **Assigned**

---

## 🎨 RESULT

**Shotgun:**
- Each pellet creates a decal on impact
- Decals stick for 5 seconds
- Creates "bullet hole" effect

**Beam/Stream:**
- Continuous painting as you sweep
- Creates paint trail on surfaces
- Stays after you stop firing!

**Performance:** Near zero impact (fully pooled!)

---

## 💡 QUICK TIPS

**Too many decals?**
- Lower `maxActiveDecals` to 300

**Decals too small?**
- Increase `decalSizeMultiplier` to 1.5

**Want longer persistence?**
- Increase `decalLifetime` to 10

**Want to paint enemies?**
- Include "Enemy" layer in `decalLayers`

---

**That's it! Your particle painting system is ready!** 🚀
