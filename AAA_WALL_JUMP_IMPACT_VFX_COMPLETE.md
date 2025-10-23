# 🌊 Wall Jump Impact VFX System - Complete Setup Guide

## Overview
This system creates cinematic ripple effects when the player performs wall jumps, with intensity that scales based on player speed. The effects include:
- **Ripple animations** that emanate from the impact point
- **Fresnel glow** that only appears during the ripple animation
- **Speed-based intensity scaling** (faster impacts = more dramatic effects)
- **Performance-optimized pooling** system
- **Full integration** with your existing MovementConfig system

---

## 🔧 Setup Instructions

### Step 1: Create Ripple Texture
1. Create or import a ripple texture (black and white pattern)
   - **Recommended**: Circular ripple pattern with multiple concentric rings
   - **Format**: Any texture format, preferably 512x512 or 1024x1024
   - **Usage**: The red channel will control ripple intensity

### Step 2: Setup Material and Prefab
1. **Create Material:**
   - Right-click in Project → Create → Material
   - Name it `WallJumpRippleMaterial`
   - Set Shader to `Custom/WallJumpRippleEffect`
   - Assign your ripple texture to the `Ripple Texture` slot
   - Adjust `Glow Color` (default: cyan `(0, 0.8, 1, 1)`)

2. **Create Prefab:**
   - Create an empty GameObject
   - Add a `Quad` as child (Create → 3D Object → Quad)
   - Scale the quad to desired size (recommended: `(100, 100, 1)`)
   - Assign the `WallJumpRippleMaterial` to the quad's Mesh Renderer
   - Set Cast Shadows to `Off` and Receive Shadows to `Off`
   - Save as prefab: `WallJumpRippleEffect`

### Step 3: Setup VFX Manager
1. **Create VFX Manager GameObject:**
   - Create empty GameObject in your scene
   - Name it `WallJumpVFXManager`
   - Add the `WallJumpImpactVFX` script component
   - Assign the `WallJumpRippleEffect` prefab to the `Ripple Effect Prefab` field

2. **Configure Settings:**
   ```
   Max Simultaneous Effects: 8
   Base Duration: 2.0
   Effect Scale: 1.0
   Min Speed Threshold: 300
   Max Intensity Speed: 1500
   Min Intensity: 0.3
   Max Intensity: 1.5
   Surface Offset: 0.01
   ```

### Step 4: Update MovementConfig Asset
Your `MovementConfig.asset` now includes these new settings:
```
=== WALL JUMP IMPACT VFX ===
Enable Wall Jump VFX: ✓ True
VFX Base Duration: 2.0
VFX Effect Scale: 1.0
VFX Min Speed Threshold: 300
VFX Max Intensity Speed: 1500
VFX Min Intensity: 0.3
VFX Max Intensity: 1.5
```

---

## 🎨 Customization Guide

### Shader Properties (Material Inspector)
- **Ripple Scale**: Controls overall ripple size
- **Ripple Speed**: How fast ripples propagate outward
- **Ripple Frequency**: Number of ripple rings
- **Glow Color**: Color of the fresnel glow effect
- **Fresnel Power**: Controls how sharp the edge glow is
- **Glow Intensity**: Overall brightness of the effect
- **Fade In/Out Time**: How quickly effects appear/disappear

### MovementConfig Settings
- **Enable Wall Jump VFX**: Master on/off switch
- **VFX Base Duration**: How long effects last (0.5-5 seconds)
- **VFX Effect Scale**: Size multiplier for ripples (0.1-3x)
- **VFX Min Speed Threshold**: Minimum speed to trigger effect (100-1000)
- **VFX Max Intensity Speed**: Speed for maximum intensity (500-3000)
- **VFX Min/Max Intensity**: Effect intensity range (0-3x)

---

## 🔧 Technical Features

### Performance Optimizations
- **Object Pooling**: Reuses effect objects to prevent garbage collection
- **Automatic Cleanup**: Effects return to pool when finished
- **Configurable Limits**: Maximum simultaneous effects prevents performance drops
- **Shader Efficiency**: Single-pass shader with minimal texture samples

### Surface Adaptation
- **Dynamic Positioning**: Effects position precisely on wall surface
- **Normal-Based Orientation**: Ripples face outward from wall
- **Z-Fighting Prevention**: Automatic surface offset
- **Multi-Surface Support**: Works on any wall angle or size

### Integration Benefits
- **Config-Driven**: All settings in MovementConfig for easy tuning
- **Debug-Friendly**: Respects existing debug settings
- **Performance-Aware**: Automatic pooling and cleanup
- **Scalable**: Easy to add more effect types

---

## 🐛 Troubleshooting

### No Effects Appearing
1. Check `WallJumpImpactVFX` GameObject exists in scene
2. Verify `Enable Wall Jump VFX` is checked in MovementConfig
3. Ensure player speed exceeds `VFX Min Speed Threshold`
4. Check prefab assignment in VFX Manager component

### Effects Too Weak/Strong
- Adjust `VFX Min/Max Intensity` in MovementConfig
- Modify `Glow Intensity` in material
- Check `VFX Effect Scale` for size issues

### Performance Issues
- Reduce `Max Simultaneous Effects` (try 4-6)
- Lower `VFX Base Duration` to clear effects faster
- Check for memory leaks (should auto-pool)

### Positioning Problems
- Increase `Surface Search Distance` in VFX Manager
- Adjust `Surface Offset` to prevent z-fighting
- Verify wall colliders have proper normals

---

## 🎯 Usage Tips

### For Best Visual Results
1. **Use high-contrast ripple textures** with clear concentric rings
2. **Match glow color to your game's aesthetic** (neon blue, orange energy, etc.)
3. **Tune speed thresholds** to your game's movement speeds
4. **Test on different wall sizes** - effects scale automatically

### For Performance
1. **Keep max effects around 6-8** for complex scenes
2. **Use power-of-2 texture sizes** (512x512, 1024x1024)
3. **Profile in builds**, not just editor

### For Gameplay Feel
1. **Lower min speed threshold** for more frequent effects
2. **Higher max intensity** for more dramatic fast impacts
3. **Longer duration** for more cinematic feel
4. **Larger scale** for bigger, more visible ripples

---

## 🚀 Advanced Customization

### Adding Sound Integration
Add this to the VFX trigger in `TriggerImpactEffect()`:
```csharp
// Play impact sound with intensity-based volume
float volume = Mathf.Lerp(0.3f, 1f, normalizedSpeed);
GameSounds.PlayWallJumpImpact(impactPoint, volume);
```

### Multiple Effect Types
- Create different prefabs for different wall materials
- Use `wallCollider.tag` to select appropriate effect
- Scale effects based on wall size using `wallCollider.bounds.size`

### Particle Integration
- Add particle systems to the ripple prefab
- Use same intensity scaling for particle emission
- Combine with existing particle effects for layered visuals

---

## ✅ System Complete!

Your wall jump impact VFX system is now fully integrated and ready to use! The effects will automatically trigger when players wall jump, with intensity based on their speed. All settings are easily tunable through the MovementConfig asset, maintaining consistency with your existing system architecture.

**Next Steps:**
1. Test in various scenarios and wall sizes
2. Fine-tune visual parameters to match your aesthetic
3. Consider adding sound effects for complete impact feedback
4. Profile performance in your target build configuration