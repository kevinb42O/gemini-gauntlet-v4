# 🌟 AAA Baked Lighting Setup Guide - Zero Runtime Cost!

## What is Baked Lighting?

Baked lighting pre-calculates all lighting and shadows at **build time**, storing them in **lightmaps**. This gives you:
- ✅ **AAA visual quality** (realistic shadows, bounce light, ambient occlusion)
- ✅ **Zero runtime performance cost** (perfect for laptops!)
- ✅ **Gorgeous indirect lighting** (light bouncing off surfaces)
- ✅ **Professional look** without expensive real-time calculations

---

## 🚀 Quick Setup (5 Minutes)

### Step 1: Mark Static Objects

1. Select all **non-moving objects** in your scene (walls, floors, buildings, props)
2. In the Inspector, check **"Static"** checkbox (top-right)
3. Unity will ask which static flags - select **"Everything"** for now
4. Repeat for all static geometry

**What to mark static:**
- ✅ Terrain
- ✅ Buildings/structures
- ✅ Large props (crates, barrels, furniture)
- ✅ Walls, floors, ceilings

**What NOT to mark static:**
- ❌ Player character
- ❌ Enemies
- ❌ Moving platforms
- ❌ Doors (if they open)
- ❌ Pickups/collectibles

---

### Step 2: Configure Lighting Settings

1. Open **Window > Rendering > Lighting**
2. Go to **"Scene"** tab

#### Environment Settings:
```
Skybox Material: Your skybox (or default)
Sun Source: Your Directional Light
Environment Lighting:
  - Source: Skybox
  - Intensity Multiplier: 1.0
Environment Reflections:
  - Source: Skybox
  - Resolution: 128 (laptop) or 256 (desktop)
  - Compression: Auto
```

#### Lightmapping Settings:
```
Lightmapper: Progressive GPU (fastest)
  - If GPU not supported, use Progressive CPU

Direct Samples: 32 (laptop) or 64 (desktop)
Indirect Samples: 128 (laptop) or 256 (desktop)
Environment Samples: 128 (laptop) or 256 (desktop)

Bounces:
  - Min Bounces: 2
  - Max Bounces: 3 (laptop) or 4 (desktop)

Filtering:
  - Direct: Auto
  - Indirect: Auto
  - Ambient Occlusion: Enabled ✅
  - AO Max Distance: 1.0

Lightmap Resolution: 20 (laptop) or 40 (desktop)
Lightmap Padding: 2
Lightmap Size: 1024 (laptop) or 2048 (desktop)
Compress Lightmaps: Enabled ✅
Ambient Occlusion: Enabled ✅
Final Gather: Enabled ✅
```

---

### Step 3: Setup Your Lights

#### Main Directional Light (Sun):
```
Type: Directional
Mode: Mixed (best balance) or Baked (best performance)
Color: Warm white (255, 244, 214)
Intensity: 1.0 - 1.5
Shadow Type: Soft Shadows
Indirect Multiplier: 1.0
```

#### Additional Lights (Point/Spot):
```
Mode: Baked (for static lights)
Range: As needed
Intensity: Adjust to taste
Shadow Type: No Shadows (baked into lightmap)
Render Mode: Important
```

**Laptop Optimization:**
- Use **maximum 2-4 real-time lights** (Mode: Realtime)
- All other lights should be **Baked**
- Disable shadows on real-time lights if possible

---

### Step 4: Configure Materials for Lightmapping

For each material that should receive baked lighting:

1. Select material in Project window
2. In Inspector:
   ```
   Shader: Universal Render Pipeline/Lit (or similar)
   Workflow Mode: Metallic
   Surface Type: Opaque
   
   Enable these:
   ✅ Receive Shadows
   ✅ Contribute GI (Global Illumination)
   
   Emission (optional):
   - If object should glow, enable Emission
   - Set Emission color and intensity
   - This will add light to the scene!
   ```

---

### Step 5: Generate Lightmaps

1. In Lighting window, go to **"Scene"** tab
2. Scroll to bottom
3. Click **"Generate Lighting"**
4. Wait for baking to complete (can take 1-30 minutes depending on scene size)

**Progress indicators:**
- Bottom-right of Unity shows baking progress
- Console shows "Bake completed" when done

**Troubleshooting:**
- If baking takes too long, reduce Lightmap Resolution to 10-20
- If quality is poor, increase Indirect Samples to 256-512
- If shadows are blocky, increase Lightmap Size to 2048-4096

---

### Step 6: Optimize Lightmap Settings

After first bake, optimize:

1. Select baked objects in Hierarchy
2. In Inspector, expand **"Mesh Renderer"** component
3. Find **"Lightmap Settings"** section:
   ```
   Scale in Lightmap: 1.0 (default)
   - Increase for important objects (2.0-4.0)
   - Decrease for unimportant objects (0.5-0.25)
   
   Prioritize Illumination: Off (usually)
   Stitch Seams: Enabled ✅
   ```

4. Re-bake lighting after adjustments

---

## 🎨 Advanced Techniques

### Emissive Materials (Glowing Objects)

Make objects emit light without using Light components:

1. Select material
2. Enable **"Emission"**
3. Set Emission color (bright colors work best)
4. Set Emission intensity (1-10 for subtle, 10-100 for strong)
5. In Lighting window, enable **"Baked Global Illumination"**
6. Re-bake lighting

**Great for:**
- Neon signs
- Computer screens
- Glowing crystals
- Lava/fire effects
- Sci-fi panels

---

### Light Probes (For Moving Objects)

Light probes capture baked lighting so **moving objects** can still be lit correctly:

1. **GameObject > Light > Light Probe Group**
2. Position probe group in your scene
3. Edit probes to cover areas where player/enemies move
4. Place probes:
   - At floor level where characters walk
   - Near walls and corners
   - In doorways and transitions
   - More probes = better lighting on dynamic objects

5. Re-bake lighting

**Probe Placement Tips:**
- 2-3 meters apart in open areas
- Closer together (1m) in detailed areas
- Always place at character height
- Cover entire playable area

---

### Reflection Probes (For Shiny Objects)

Reflection probes capture environment reflections:

1. **GameObject > Light > Reflection Probe**
2. Position in center of room/area
3. Configure:
   ```
   Type: Baked
   Resolution: 128 (laptop) or 256 (desktop)
   HDR: Enabled
   Box Projection: Enabled ✅
   Size: Cover entire room
   ```

4. Click **"Bake"** button in Inspector
5. Repeat for each distinct area

---

## 🔧 Laptop-Specific Optimizations

### Lightmap Settings for Laptops:
```
Lightmap Resolution: 10-20 (lower = faster bake, smaller files)
Lightmap Size: 512-1024 (lower = less VRAM usage)
Direct Samples: 16-32
Indirect Samples: 64-128
Compress Lightmaps: Always Enabled ✅
```

### Scene Optimization:
```
Max Real-time Lights: 2-4
Shadow Distance: 30-50
Shadow Cascades: 2
Light Probe Density: Medium (2-3m spacing)
Reflection Probe Resolution: 64-128
```

---

## 🎯 Quality vs Performance Balance

### Ultra Performance (Potato Laptops):
```
Lightmap Resolution: 10
Lightmap Size: 512
Samples: 16/64/64
Bounces: 2
Real-time Lights: 2
Shadow Distance: 30
```

### Balanced (Normal Laptops):
```
Lightmap Resolution: 20
Lightmap Size: 1024
Samples: 32/128/128
Bounces: 3
Real-time Lights: 4
Shadow Distance: 50
```

### High Quality (Gaming Laptops):
```
Lightmap Resolution: 40
Lightmap Size: 2048
Samples: 64/256/256
Bounces: 4
Real-time Lights: 6
Shadow Distance: 75
```

---

## 🐛 Common Issues & Solutions

### Issue: Lightmaps look blotchy/splotchy
**Solution:** Increase Indirect Samples to 256-512

### Issue: Seams visible between objects
**Solution:** Enable "Stitch Seams" on all objects, increase Lightmap Padding to 4

### Issue: Objects too dark
**Solution:** Increase Environment Lighting Intensity, add fill lights, increase light Indirect Multiplier

### Issue: Baking takes forever
**Solution:** Reduce Lightmap Resolution, reduce Samples, use Progressive GPU

### Issue: Lightmaps use too much memory
**Solution:** Reduce Lightmap Size, enable Compress Lightmaps, reduce Lightmap Resolution

### Issue: Moving objects don't match lighting
**Solution:** Add Light Probes covering movement areas

### Issue: Reflections look wrong
**Solution:** Add Reflection Probes, enable Box Projection

---

## 🎮 Integration with OptimizedLightingController

### For Static Scenes (Baked Only):
```csharp
// In OptimizedLightingController Inspector:
Lighting Mode: Baked
```
This disables the dynamic directional light, using only baked lighting (zero runtime cost).

### For Dynamic Day/Night with Baked Base:
```csharp
// In OptimizedLightingController Inspector:
Lighting Mode: Dynamic
Enable Day Night Cycle: True

// In Lighting Settings:
Directional Light Mode: Mixed
```
This combines baked indirect lighting with dynamic direct lighting (best of both worlds).

---

## 📊 Expected Results

### Before Baked Lighting:
- Real-time shadows: 10-20 FPS cost
- No indirect lighting: Flat, unrealistic look
- High GPU usage: 60-80%

### After Baked Lighting:
- Zero runtime cost: 0 FPS cost
- Beautiful indirect lighting: AAA look
- Low GPU usage: 20-40%
- **30-50% FPS improvement on laptops!**

---

## ✅ Final Checklist

- [ ] All static objects marked as Static
- [ ] Lighting Settings configured for laptop performance
- [ ] Main Directional Light set to Mixed or Baked
- [ ] Materials configured to receive GI
- [ ] Lightmaps generated successfully
- [ ] Light Probes placed in movement areas
- [ ] Reflection Probes added to key areas
- [ ] OptimizedLightingController set to Baked mode
- [ ] Scene tested on target laptop hardware

---

## 🚀 Next Steps

1. **Test on laptop** - Check FPS and visual quality
2. **Iterate** - Adjust lightmap settings based on results
3. **Add post-processing** - Use OptimizedPostProcessing.cs for extra polish
4. **Setup occlusion culling** - See AAA_OCCLUSION_CULLING_GUIDE.md
5. **Profile performance** - Use Unity Profiler to verify improvements

---

**Remember:** Baked lighting is the #1 way to get AAA visuals on laptop hardware. It's worth the setup time!
