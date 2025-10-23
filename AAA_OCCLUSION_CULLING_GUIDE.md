# 🎯 Occlusion Culling Setup Guide - Don't Render What You Can't See!

## What is Occlusion Culling?

Occlusion culling automatically **stops rendering objects that are hidden behind other objects**. This is HUGE for laptop performance!

### Example:
- Player is in Room A
- Room B is behind a wall
- Without occlusion culling: GPU renders both rooms (wasted performance)
- With occlusion culling: GPU only renders Room A (50%+ FPS boost!)

---

## 🚀 Quick Setup (10 Minutes)

### Step 1: Prepare Your Scene

1. **Mark all static objects as Static** (if not already done):
   - Select all walls, floors, buildings, large props
   - Check **"Static"** in Inspector (top-right)
   - Select **"Occluder Static"** and **"Occludee Static"**

2. **What should be marked:**
   - ✅ **Occluders** (objects that block view): Walls, buildings, large props
   - ✅ **Occludees** (objects that can be hidden): All static geometry
   - ❌ **NOT static**: Player, enemies, small props, particles

---

### Step 2: Open Occlusion Culling Window

1. **Window > Rendering > Occlusion Culling**
2. Window opens with 4 tabs: Object, Bake, Visualization, Areas

---

### Step 3: Configure Bake Settings

Go to **"Bake"** tab:

#### Laptop-Optimized Settings:
```
Smallest Occluder: 5.0
  - Objects smaller than this won't be occluders
  - Lower = more accurate, slower bake
  - Higher = faster bake, less accurate
  - Laptop: 5.0-10.0

Smallest Hole: 0.25
  - Smallest gap that camera can see through
  - Lower = more accurate, slower bake
  - Laptop: 0.25-0.5

Backface Threshold: 100
  - How much backface culling to use
  - Default: 100 (recommended)
```

#### Advanced Settings (Usually leave default):
```
View Cell Size: 1.0
  - Size of cells for occlusion data
  - Smaller = more accurate, larger data
  - Laptop: 1.0-2.0

Near Clip Plane: 0.3
Far Clip Plane: 1000
  - Match your camera settings
```

---

### Step 4: Bake Occlusion Data

1. In **"Bake"** tab, click **"Bake"** button
2. Wait for baking to complete (1-10 minutes depending on scene size)
3. Unity creates occlusion data files in your scene folder

**Progress:**
- Console shows "Occlusion culling data baked successfully"
- Scene view shows occlusion visualization (colored cells)

---

### Step 5: Verify Occlusion Culling

1. Go to **"Visualization"** tab
2. Enable **"Occlusion Culling"** toggle (top of Scene view)
3. Move camera around in Scene view
4. Objects should appear/disappear based on visibility

**Visualization Colors:**
- **Blue cells**: Camera can see through these
- **Red areas**: Occluded (hidden) geometry
- **Green**: Visible geometry

---

### Step 6: Test in Play Mode

1. Enter Play Mode
2. Enable **Stats** window (Game view > Stats button)
3. Watch **"Batches"** and **"Tris"** counts
4. Move around - counts should decrease when looking at walls/away from geometry

**Expected Results:**
- Looking at open area: High tri count
- Looking at wall: Low tri count (50-80% reduction!)
- Moving through rooms: Tri count changes dynamically

---

## 🎨 Advanced Techniques

### Occlusion Areas (For Complex Scenes)

Occlusion Areas let you manually define culling regions:

1. **GameObject > 3D Object > Occlusion Area**
2. Position and scale to cover a room/area
3. In Inspector:
   ```
   Is View Volume: Enabled ✅
   Size: Cover entire room
   ```
4. Repeat for each distinct room/area
5. Re-bake occlusion data

**When to use:**
- Large open-world scenes
- Multi-room buildings
- Complex indoor environments
- Scenes with distinct areas

---

### Occlusion Portals (For Doorways)

Portals control visibility between areas:

1. **GameObject > 3D Object > Occlusion Portal**
2. Position in doorway/window
3. Scale to match opening size
4. In Inspector:
   ```
   Open: True (door open) or False (door closed)
   ```
5. Re-bake occlusion data

**Dynamic Portals:**
```csharp
// Script to open/close portal when door opens
OcclusionPortal portal = GetComponent<OcclusionPortal>();
portal.open = isDoorOpen;
```

---

### LOD Groups + Occlusion Culling

Combine LOD (Level of Detail) with occlusion culling for maximum performance:

1. Select object with LOD Group
2. In Inspector:
   ```
   LOD 0 (Close): High detail mesh
   LOD 1 (Medium): Medium detail mesh
   LOD 2 (Far): Low detail mesh
   Culled: Object disappears
   ```

3. Adjust LOD distances:
   ```
   LOD 0: 0% - 60% (0-30m)
   LOD 1: 60% - 80% (30-50m)
   LOD 2: 80% - 95% (50-100m)
   Culled: 95% - 100% (100m+)
   ```

**Result:** Objects use low detail when far, disappear when very far, and are culled when behind walls!

---

## 🔧 Laptop-Specific Optimizations

### Aggressive Culling Settings:
```
Smallest Occluder: 3.0-5.0
  - More objects act as occluders
  - Better culling, slightly slower bake

Smallest Hole: 0.5-1.0
  - Less precise, but faster runtime

View Cell Size: 1.5-2.0
  - Larger cells = less data, faster culling checks
```

### Camera Settings:
```
Far Clip Plane: 100-200 (not 1000!)
  - Don't render distant objects
  - Huge performance boost

Culling Mask:
  - Disable layers you don't need to render
  - Example: Disable "Editor Only" layer
```

### Additional Optimizations:
```
Use Frustum Culling: Always enabled (automatic)
Use Occlusion Culling: Enabled ✅
Use LOD Groups: For all large objects
Shadow Distance: 30-50 (match Quality Settings)
```

---

## 🎯 Performance Impact

### Before Occlusion Culling:
```
Scene: 100,000 triangles
Looking at wall: Still rendering 100,000 tris
FPS: 30-40 on laptop
GPU Usage: 80-90%
```

### After Occlusion Culling:
```
Scene: 100,000 triangles
Looking at wall: Only rendering 20,000 tris (80% culled!)
FPS: 60-80 on laptop
GPU Usage: 40-50%
```

**Expected FPS Improvement:** 30-60% in indoor/complex scenes!

---

## 🐛 Common Issues & Solutions

### Issue: Occlusion culling not working
**Solution:** 
- Verify objects are marked "Occluder Static" and "Occludee Static"
- Re-bake occlusion data
- Check that occlusion culling is enabled in Camera component

### Issue: Objects popping in/out of view
**Solution:**
- Increase "Smallest Hole" to 0.5-1.0
- Reduce "Smallest Occluder" to 3.0-5.0
- Add Occlusion Areas for better control

### Issue: Baking takes too long
**Solution:**
- Increase "Smallest Occluder" to 10.0+
- Increase "View Cell Size" to 2.0-3.0
- Reduce scene complexity (combine meshes)

### Issue: Culling too aggressive (objects disappear when visible)
**Solution:**
- Decrease "Smallest Occluder" to 3.0
- Decrease "Smallest Hole" to 0.25
- Add manual Occlusion Areas

### Issue: Not enough performance improvement
**Solution:**
- Verify scene has actual occlusion (walls, buildings)
- Combine with LOD groups
- Check Stats window - are tris actually being culled?
- Scene might be too open (occlusion works best indoors)

---

## 📊 Best Practices

### DO:
- ✅ Mark all static geometry as Static
- ✅ Use larger "Smallest Occluder" for laptops (5.0-10.0)
- ✅ Combine with LOD groups for maximum performance
- ✅ Test in Play Mode with Stats window
- ✅ Re-bake after major scene changes
- ✅ Use Occlusion Areas for complex scenes

### DON'T:
- ❌ Mark dynamic objects as Static (player, enemies)
- ❌ Use tiny "Smallest Occluder" values (<1.0) on laptops
- ❌ Forget to re-bake after scene changes
- ❌ Use in completely open scenes (no benefit)
- ❌ Expect miracles in outdoor-only scenes

---

## 🎮 Integration with Other Systems

### With Baked Lighting:
1. Bake lighting first
2. Then bake occlusion culling
3. Both systems work together perfectly

### With LOD Groups:
1. Setup LOD groups on objects
2. Mark all LOD levels as Static
3. Bake occlusion culling
4. Objects will use LOD + occlusion culling together

### With Post-Processing:
- Occlusion culling reduces rendered objects
- Post-processing runs on fewer pixels
- Combined performance boost!

---

## 🔍 Debugging Occlusion Culling

### Enable Visualization:
1. Scene view > Occlusion Culling toggle (top toolbar)
2. Move camera around
3. Watch objects appear/disappear

### Stats Window:
1. Game view > Stats button
2. Watch these values:
   - **Tris**: Should decrease when looking at walls
   - **Batches**: Should decrease when objects are culled
   - **SetPass calls**: Should decrease with culling

### Console Debugging:
```csharp
// Add to camera script
void Update()
{
    if (Input.GetKeyDown(KeyCode.F6))
    {
        Camera cam = GetComponent<Camera>();
        Debug.Log($"Occlusion Culling: {cam.useOcclusionCulling}");
        
        // Get visible renderers
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();
        int visibleCount = 0;
        foreach (Renderer r in allRenderers)
        {
            if (r.isVisible) visibleCount++;
        }
        Debug.Log($"Visible Renderers: {visibleCount} / {allRenderers.Length}");
    }
}
```

---

## ✅ Final Checklist

- [ ] All static objects marked as "Occluder Static" and "Occludee Static"
- [ ] Occlusion culling baked successfully
- [ ] Visualization shows proper culling in Scene view
- [ ] Stats window shows tri count reduction in Play Mode
- [ ] Camera has "Occlusion Culling" enabled
- [ ] Far Clip Plane set to reasonable distance (100-200)
- [ ] LOD groups setup on large objects
- [ ] Tested on target laptop hardware
- [ ] FPS improvement verified (30-60% expected)

---

## 🚀 Next Steps

1. **Combine with baked lighting** - See AAA_BAKED_LIGHTING_SETUP_GUIDE.md
2. **Add LOD groups** - For distant objects
3. **Optimize Quality Settings** - See AAA_LAPTOP_OPTIMIZATION_CHECKLIST.md
4. **Profile performance** - Use Unity Profiler to verify improvements
5. **Test on laptop** - Verify real-world performance gains

---

**Remember:** Occlusion culling is FREE performance - it only helps, never hurts! Always use it for indoor/complex scenes.
