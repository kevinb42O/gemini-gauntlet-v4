# 🚀 AAA Laptop Optimization Checklist - Complete Guide

## Overview

This checklist covers **EVERYTHING** you need to make your game run smoothly on normal laptops while maintaining AAA visual quality. Follow this step-by-step!

**Expected Results:**
- 60+ FPS on normal laptops
- 30-40 FPS on potato laptops
- AAA visual quality maintained
- Professional, polished look

---

## 🎯 Phase 1: Quality Settings (5 Minutes)

### Edit > Project Settings > Quality

#### Graphics Settings:
```
✅ Render Pipeline: Universal Render Pipeline (URP)
✅ VSync: Don't Sync (let GPU breathe)
✅ Anti-Aliasing: FXAA or Off (MSAA is too expensive)
✅ Anisotropic Textures: Forced On (good balance)
✅ Texture Quality: Half Resolution (saves VRAM)
```

#### Shadows:
```
✅ Shadows: Hard and Soft Shadows
✅ Shadow Resolution: Low or Medium (not High!)
✅ Shadow Distance: 30-50 (not 150!)
✅ Shadow Cascades: 2 (not 4!)
✅ Shadow Near Plane Offset: 3
✅ Shadow Projection: Stable Fit
```

#### Lighting:
```
✅ Realtime GI CPU Usage: Low or Off
✅ Realtime Reflection Probes: Off
✅ Billboards Face Camera Position: Enabled
```

#### Rendering:
```
✅ Pixel Light Count: 2-4 (not 8+!)
✅ LOD Bias: 2
✅ Maximum LOD Level: 0
✅ Particle Raycast Budget: 64-128 (not 256!)
```

#### Performance:
```
✅ Async Asset Upload Time Slice: 4ms
✅ Async Asset Upload Buffer Size: 8
✅ Skin Weights: 4 Bones (good balance)
```

---

## 🌟 Phase 2: Lighting Setup (30 Minutes)

### Step 1: Setup OptimizedLightingController

1. **Add to scene:**
   - Create empty GameObject: "LightingController"
   - Add component: `OptimizedLightingController.cs`

2. **Configure for Baked mode (best performance):**
   ```
   Lighting Mode: Baked
   Main Directional Light: Assign your sun light
   Enable Fog: True
   Fog Density: 0.01
   Update Every N Frames: 2
   ```

3. **Or configure for Dynamic day/night:**
   ```
   Lighting Mode: Dynamic
   Enable Day Night Cycle: True
   Day Length In Seconds: 300 (5 minutes)
   Time Of Day: 0.5 (noon)
   ```

### Step 2: Bake Lighting (See AAA_BAKED_LIGHTING_SETUP_GUIDE.md)

1. **Mark static objects:**
   - Select all non-moving geometry
   - Check "Static" in Inspector

2. **Configure Lighting window:**
   ```
   Window > Rendering > Lighting
   
   Lightmapper: Progressive GPU
   Direct Samples: 32
   Indirect Samples: 128
   Bounces: 2-3
   Lightmap Resolution: 20
   Lightmap Size: 1024
   Compress Lightmaps: Enabled ✅
   Ambient Occlusion: Enabled ✅
   ```

3. **Bake lighting:**
   - Click "Generate Lighting"
   - Wait for completion

4. **Add Light Probes:**
   - GameObject > Light > Light Probe Group
   - Place probes where player/enemies move
   - Re-bake lighting

---

## 🎨 Phase 3: Post-Processing (15 Minutes)

### Step 1: Setup URP Post-Processing

1. **Create Volume:**
   - GameObject > Volume > Global Volume
   - Name it "PostProcessing"

2. **Add OptimizedPostProcessing component:**
   - Add component: `OptimizedPostProcessing.cs`

3. **Configure for Laptop:**
   ```
   Performance Profile: Laptop
   
   Enable Bloom: True
   Bloom Intensity: 0.3
   Bloom Threshold: 1.0
   
   Enable Color Grading: True
   Saturation: 10
   Contrast: 5
   
   Enable Vignette: True
   Vignette Intensity: 0.3
   
   Enable Film Grain: True
   Film Grain Intensity: 0.15
   
   Enable Chromatic Aberration: False (too expensive)
   Enable Depth Of Field: False (very expensive)
   Enable Motion Blur: False (expensive)
   Enable Ambient Occlusion: False (very expensive)
   
   Enable Dynamic Quality: True
   Target FPS: 60
   ```

### Step 2: Verify URP Asset

1. **Edit > Project Settings > Graphics**
2. **Verify URP Asset settings:**
   ```
   Rendering:
   - Render Scale: 1.0 (or 0.9 for extra performance)
   - Depth Texture: Off (unless needed)
   - Opaque Texture: Off (unless needed)
   
   Quality:
   - HDR: Off (expensive on laptops)
   - Anti-Aliasing: None or FXAA
   - Render Scale: 1.0
   
   Lighting:
   - Main Light: Per Pixel
   - Additional Lights: Per Pixel
   - Additional Lights Count: 4
   - Per Object Limit: 4
   
   Shadows:
   - Max Distance: 50
   - Cascade Count: 2
   - Soft Shadows: Enabled
   ```

---

## 🎯 Phase 4: Occlusion Culling (20 Minutes)

### See AAA_OCCLUSION_CULLING_GUIDE.md for full details

1. **Mark static objects:**
   - Select all walls, buildings, large props
   - Enable "Occluder Static" and "Occludee Static"

2. **Bake occlusion data:**
   ```
   Window > Rendering > Occlusion Culling
   
   Smallest Occluder: 5.0
   Smallest Hole: 0.25
   View Cell Size: 1.0
   
   Click "Bake"
   ```

3. **Verify in Play Mode:**
   - Enable Stats window
   - Watch tri count decrease when looking at walls

---

## 🔧 Phase 5: Mesh & Material Optimization (30 Minutes)

### Mesh Optimization:

1. **Setup LOD Groups:**
   ```
   Select large objects
   Add Component > LOD Group
   
   LOD 0 (0-60%): Full detail
   LOD 1 (60-80%): 50% triangles
   LOD 2 (80-95%): 25% triangles
   Culled (95-100%): Hidden
   ```

2. **Combine Static Meshes:**
   - Select multiple static objects
   - Use Mesh Baker or manual combining
   - Reduces draw calls significantly

3. **Optimize Mesh Settings:**
   ```
   Select mesh in Project
   Model tab:
   - Read/Write Enabled: Off
   - Optimize Mesh: On
   - Generate Colliders: Off (unless needed)
   ```

### Material Optimization:

1. **Use URP/Lit shader:**
   - Avoid custom shaders if possible
   - URP shaders are highly optimized

2. **Texture Settings:**
   ```
   Select texture in Project
   
   Max Size: 1024 or 2048 (not 4096!)
   Compression: High Quality (or Normal Quality)
   Generate Mip Maps: Enabled ✅
   Filter Mode: Trilinear
   Aniso Level: 2-4
   ```

3. **Atlas Textures:**
   - Combine multiple small textures into one atlas
   - Reduces texture switches (faster rendering)

---

## 🎮 Phase 6: Camera & Rendering (10 Minutes)

### Camera Settings:

```
Select Main Camera

Projection: Perspective
Field of View: 60-75 (not 90+)
Clipping Planes:
  - Near: 0.3
  - Far: 100-200 (not 1000!)

Rendering:
  - Renderer: URP Renderer
  - Post Processing: Enabled ✅
  - Anti-aliasing: FXAA or None
  - Stop NaN: Enabled
  - Dithering: Disabled

Culling:
  - Culling Mask: Everything (or exclude unnecessary layers)
  - Occlusion Culling: Enabled ✅
```

### URP Camera Settings:

```
Rendering:
  - Renderer: UniversalRenderer
  - Render Shadows: Enabled
  - Depth Texture: Off (unless needed)
  - Opaque Texture: Off (unless needed)

Stack:
  - Empty (unless using camera stacking)
```

---

## 🚀 Phase 7: Physics Optimization (10 Minutes)

### Edit > Project Settings > Physics

```
✅ Default Solver Iterations: 6 (not 12)
✅ Default Solver Velocity Iterations: 1 (not 8)
✅ Bounce Threshold: 2
✅ Sleep Threshold: 0.005
✅ Default Contact Offset: 0.01
✅ Auto Simulation: Enabled
✅ Auto Sync Transforms: Disabled (use manual sync)
✅ Reuse Collision Callbacks: Enabled
```

### Rigidbody Optimization:

```
For all Rigidbodies:
- Collision Detection: Discrete (not Continuous)
- Interpolate: None (or Interpolate if needed)
- Use Gravity: Only if needed
- Is Kinematic: True for non-physics objects
```

---

## 📦 Phase 8: Asset Optimization (Ongoing)

### Audio:

```
Select audio clips in Project

Load Type: Compressed In Memory (not Decompress On Load)
Compression Format: Vorbis (good balance)
Quality: 70% (not 100%)
Sample Rate: 22050 Hz (not 48000 Hz)
```

### Particles:

```
For each Particle System:

Max Particles: 50-100 (not 1000+)
Simulation Space: World (usually)
Prewarm: Off (expensive)
Culling Mode: Automatic
Scaling Mode: Local
Play On Awake: Only if needed

Rendering:
- Render Mode: Billboard (cheapest)
- Material: Simple Additive/Alpha Blended
- Sorting Fudge: 0
```

### Animations:

```
Select animation clips in Project

Animation Compression: Optimal
Rotation Error: 0.5
Position Error: 0.5
Scale Error: 0.5
Anim. Compression: Optimal
```

---

## 🔍 Phase 9: Profiling & Testing (Ongoing)

### Unity Profiler:

1. **Window > Analysis > Profiler**
2. **Enter Play Mode**
3. **Check these metrics:**
   ```
   CPU Usage:
   - Scripts: <5ms per frame
   - Rendering: <10ms per frame
   - Physics: <2ms per frame
   
   GPU Usage:
   - Opaque: <10ms per frame
   - Transparent: <2ms per frame
   - Post-processing: <3ms per frame
   
   Memory:
   - Total: <2GB
   - GC Alloc: <1MB per frame
   ```

### Stats Window:

```
Game view > Stats button

Target values for 60 FPS:
- FPS: 60+
- Batches: <500
- Tris: <500k
- Verts: <1M
- SetPass calls: <100
```

### Frame Debugger:

```
Window > Analysis > Frame Debugger

Use to identify:
- Expensive draw calls
- Redundant rendering
- Overdraw issues
- Shader complexity
```

---

## ✅ Final Optimization Checklist

### Graphics:
- [ ] Quality Settings configured for laptop
- [ ] URP Asset optimized
- [ ] Shadow distance: 30-50
- [ ] Texture quality: Half Resolution
- [ ] Anti-aliasing: FXAA or Off
- [ ] VSync: Don't Sync

### Lighting:
- [ ] OptimizedLightingController added
- [ ] Baked lighting setup complete
- [ ] Light Probes placed
- [ ] Reflection Probes added
- [ ] Real-time lights: 2-4 max
- [ ] Directional light mode: Mixed or Baked

### Post-Processing:
- [ ] OptimizedPostProcessing added
- [ ] Performance Profile: Laptop
- [ ] Expensive effects disabled (DOF, Motion Blur, AO)
- [ ] Dynamic quality enabled
- [ ] Target FPS: 60

### Culling:
- [ ] Occlusion culling baked
- [ ] Camera far clip: 100-200
- [ ] LOD groups on large objects
- [ ] Static objects marked correctly

### Meshes & Materials:
- [ ] LOD groups setup
- [ ] Textures compressed (1024-2048 max)
- [ ] Materials use URP shaders
- [ ] Mesh optimization enabled
- [ ] Static meshes combined where possible

### Physics:
- [ ] Solver iterations: 6
- [ ] Collision detection: Discrete
- [ ] Unnecessary Rigidbodies removed
- [ ] Triggers used instead of collisions where possible

### Assets:
- [ ] Audio compressed (Vorbis, 70%)
- [ ] Particles limited (50-100 max)
- [ ] Animations compressed (Optimal)
- [ ] Unused assets removed

### Testing:
- [ ] Profiler shows <16ms per frame (60 FPS)
- [ ] Stats window shows acceptable values
- [ ] Tested on target laptop hardware
- [ ] No frame drops during gameplay
- [ ] Memory usage <2GB

---

## 🎯 Performance Targets

### Normal Laptop (Integrated GPU):
```
Target: 60 FPS
Resolution: 1920x1080
Settings: Medium-Low
Triangles: <300k visible
Draw Calls: <300
Memory: <1.5GB
```

### Potato Laptop (Old Integrated GPU):
```
Target: 30-40 FPS
Resolution: 1280x720
Settings: Low
Triangles: <150k visible
Draw Calls: <200
Memory: <1GB
```

### Gaming Laptop (Dedicated GPU):
```
Target: 120+ FPS
Resolution: 1920x1080
Settings: High
Triangles: <1M visible
Draw Calls: <500
Memory: <3GB
```

---

## 🐛 Common Performance Issues

### Issue: Low FPS everywhere
**Check:**
- Shadow distance too high (>50)
- Too many real-time lights (>4)
- No occlusion culling
- No baked lighting
- Expensive post-processing effects

### Issue: FPS drops in specific areas
**Check:**
- Too many objects in view
- Missing LOD groups
- Particle systems spawning too many particles
- Complex shaders/materials
- No occlusion culling in that area

### Issue: Stuttering/hitching
**Check:**
- GC allocations (use Profiler)
- Asset loading (use async loading)
- Physics calculations (reduce iterations)
- Shader compilation (precompile shaders)

### Issue: High memory usage
**Check:**
- Texture sizes (reduce to 1024-2048)
- Audio clips (compress more)
- Unused assets in scene
- Memory leaks (use Profiler)

---

## 🚀 Quick Wins (Do These First!)

1. **Reduce shadow distance to 30-50** → Instant 20-30% FPS boost
2. **Enable baked lighting** → 30-50% FPS boost
3. **Reduce texture quality to Half Resolution** → 20% FPS boost
4. **Disable expensive post-processing** → 10-15% FPS boost
5. **Enable occlusion culling** → 30-60% FPS boost in indoor scenes
6. **Reduce camera far clip to 100-200** → 10-20% FPS boost
7. **Limit real-time lights to 2-4** → 20-30% FPS boost

**Total potential improvement: 2-3x FPS increase!**

---

## 📚 Additional Resources

- **AAA_BAKED_LIGHTING_SETUP_GUIDE.md** - Complete lighting setup
- **AAA_OCCLUSION_CULLING_GUIDE.md** - Occlusion culling details
- **OptimizedLightingController.cs** - Smart lighting system
- **OptimizedPostProcessing.cs** - Performance-friendly effects

---

## 🎉 Final Notes

**Remember:**
- Optimization is iterative - test and adjust
- Profile before and after changes
- Test on actual target hardware
- Balance quality vs performance
- Baked lighting is your best friend!

**With these optimizations, you should see:**
- 2-3x FPS improvement
- AAA visual quality maintained
- Smooth 60 FPS on normal laptops
- Professional, polished look

**Good luck making your game SPECIAL and OPTIMAL! 🚀**
