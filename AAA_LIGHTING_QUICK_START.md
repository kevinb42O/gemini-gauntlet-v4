# 🚀 AAA Lighting & Optimization - Quick Start Guide

## 30-Minute Setup for SPECIAL + OPTIMAL Results!

This guide gets you from "meh" to "WOW!" in 30 minutes. Follow these steps in order.

---

## ⚡ Step 1: Add Scripts to Scene (2 Minutes)

### 1. Create Lighting Controller:
```
1. Create empty GameObject: "LightingController"
2. Add Component: OptimizedLightingController
3. Assign your Directional Light to "Main Directional Light" field
4. Set Lighting Mode: Baked (for best performance)
5. Enable Fog: True
6. Fog Density: 0.01
```

### 2. Create Post-Processing Volume:
```
1. GameObject > Volume > Global Volume
2. Name it: "PostProcessing"
3. Add Component: OptimizedPostProcessing
4. Set Performance Profile: Laptop
5. Enable Bloom: True (Intensity: 0.3)
6. Enable Color Grading: True (Saturation: 10, Contrast: 5)
7. Enable Vignette: True (Intensity: 0.3)
8. Enable Film Grain: True (Intensity: 0.15)
9. Disable expensive effects: DOF, Motion Blur, AO, Chromatic Aberration
```

---

## 🌟 Step 2: Bake Lighting (10 Minutes)

### 1. Mark Static Objects:
```
1. Select all walls, floors, buildings, large props
2. Check "Static" in Inspector (top-right)
3. Click "Yes, change children" when prompted
```

### 2. Configure Lighting:
```
1. Window > Rendering > Lighting
2. Scene tab:

Environment:
- Skybox Material: Your skybox
- Sun Source: Your Directional Light
- Environment Lighting > Intensity: 1.0

Lightmapping Settings:
- Lightmapper: Progressive GPU
- Direct Samples: 32
- Indirect Samples: 128
- Environment Samples: 128
- Bounces: 2-3
- Lightmap Resolution: 20
- Lightmap Size: 1024
- Compress Lightmaps: ✅
- Ambient Occlusion: ✅
```

### 3. Bake:
```
1. Click "Generate Lighting" at bottom
2. Wait 5-10 minutes
3. Done!
```

---

## 🎯 Step 3: Setup Occlusion Culling (8 Minutes)

### 1. Verify Static Objects:
```
1. Select walls/buildings
2. In Inspector > Static dropdown
3. Enable: Occluder Static, Occludee Static
```

### 2. Bake Occlusion:
```
1. Window > Rendering > Occlusion Culling
2. Bake tab:
   - Smallest Occluder: 5.0
   - Smallest Hole: 0.25
3. Click "Bake"
4. Wait 2-5 minutes
```

---

## ⚙️ Step 4: Optimize Quality Settings (5 Minutes)

### Edit > Project Settings > Quality:
```
Rendering:
- Render Pipeline: Universal Render Pipeline
- VSync: Don't Sync
- Anti-Aliasing: FXAA or None
- Texture Quality: Half Resolution
- Anisotropic Textures: Forced On

Shadows:
- Shadow Distance: 30-50 (CRITICAL!)
- Shadow Resolution: Low or Medium
- Shadow Cascades: 2

Lighting:
- Realtime GI CPU Usage: Low or Off
- Pixel Light Count: 2-4

Performance:
- Particle Raycast Budget: 64-128
- Async Upload Time Slice: 4ms
- Async Upload Buffer Size: 8
```

---

## 📷 Step 5: Optimize Camera (2 Minutes)

### Select Main Camera:
```
Clipping Planes:
- Near: 0.3
- Far: 100-200 (not 1000!)

Rendering:
- Post Processing: ✅
- Anti-aliasing: FXAA or None
- Occlusion Culling: ✅
```

---

## 🎮 Step 6: Test & Verify (3 Minutes)

### 1. Enable Stats Window:
```
1. Enter Play Mode
2. Game view > Stats button
3. Watch these values:
   - FPS: Should be 60+
   - Tris: Should decrease when looking at walls
   - Batches: Should be <500
```

### 2. Test Movement:
```
1. Move around scene
2. Look at walls (tris should drop)
3. Look at open areas (tris should increase)
4. Verify smooth 60 FPS
```

---

## 🎨 Optional: Add Light Probes (5 Minutes)

For better lighting on moving characters:

```
1. GameObject > Light > Light Probe Group
2. Position in scene
3. Edit probes to cover player movement areas
4. Place probes 2-3 meters apart
5. Window > Rendering > Lighting > Generate Lighting
```

---

## ✅ Quick Checklist

- [ ] OptimizedLightingController added and configured
- [ ] OptimizedPostProcessing added and configured
- [ ] Static objects marked as Static
- [ ] Lighting baked successfully
- [ ] Occlusion culling baked
- [ ] Quality Settings optimized (Shadow Distance: 30-50!)
- [ ] Camera far clip: 100-200
- [ ] Stats window shows 60+ FPS
- [ ] Tris count decreases when looking at walls

---

## 🚀 Expected Results

### Before:
- FPS: 30-40 on laptop
- Flat, boring lighting
- No shadows or indirect light
- High GPU usage

### After:
- FPS: 60-80 on laptop
- Beautiful, realistic lighting
- Soft shadows and bounce light
- Low GPU usage
- SPECIAL and OPTIMAL! 🎉

---

## 📚 Need More Details?

- **AAA_BAKED_LIGHTING_SETUP_GUIDE.md** - Complete lighting guide
- **AAA_OCCLUSION_CULLING_GUIDE.md** - Occlusion culling details
- **AAA_LAPTOP_OPTIMIZATION_CHECKLIST.md** - Full optimization checklist

---

## 🐛 Troubleshooting

### FPS still low?
1. Check Shadow Distance (should be 30-50, not 150!)
2. Verify Texture Quality is Half Resolution
3. Disable expensive post-processing (DOF, Motion Blur, AO)
4. Check Stats window - are tris being culled?

### Lighting looks bad?
1. Increase Indirect Samples to 256
2. Increase Lightmap Resolution to 40
3. Add more lights to dark areas
4. Adjust post-processing settings

### Objects popping in/out?
1. Increase Occlusion Culling > Smallest Hole to 0.5
2. Add Light Probes for moving objects
3. Check LOD distances

---

## 🎉 You're Done!

Your game now has:
- ✅ AAA lighting quality
- ✅ Laptop-optimized performance
- ✅ Beautiful post-processing
- ✅ Smart occlusion culling
- ✅ SPECIAL + OPTIMAL results!

**Enjoy your 2-3x FPS improvement! 🚀**
