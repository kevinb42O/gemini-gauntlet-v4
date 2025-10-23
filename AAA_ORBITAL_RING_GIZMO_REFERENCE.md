# 🎨 Orbital Ring Gizmo Visual Reference

## What You'll See in the Scene View

### Gizmo Components

```
                    [Label: "50.0u, 8 platforms"]
                              ↓
                              ●
                             /|\
                            / | \
                           /  |  \
                          /   |   \
                         /    |    \
        ●───────────────●─────●─────●───────────────●
       /                      ↑                      \
      /                   [Center]                    \
     ●                                                 ●
     |                                                 |
     |              [Colored Ring Circle]              |
     |                                                 |
     ●                                                 ●
      \                                               /
       \                                             /
        ●───────────────●─────────────●───────────●

Legend:
● = Platform spawn position (sphere gizmo)
─ = Ring circle line
/ = Line from center to platform (when selected)
```

---

## Gizmo Behavior

### When NOT Selected (Passive View)
- **Ring circles**: Faint colored lines (30% opacity)
- **Platform markers**: Small spheres (1 unit radius)
- **Center lines**: Hidden
- **Labels**: Hidden

### When Selected (Active View)
- **Ring circles**: Bright colored lines (80% opacity)
- **Platform markers**: Larger spheres (2 unit radius)
- **Center lines**: Visible (from center to each platform)
- **Labels**: Visible (shows radius and platform count)

---

## Example: 3-Ring System

```
Scene View (Top-Down):

                        Outer Ring (Magenta)
                    ●       ●       ●       ●
                 ●                             ●
              ●                                   ●
           ●          Middle Ring (Yellow)          ●
          ●        ●       ●       ●       ●        ●
         ●      ●                             ●      ●
        ●    ●        Inner Ring (Cyan)        ●    ●
        ●   ●      ●       ●       ●       ●      ●   ●
        ●  ●    ●                             ●    ●  ●
        ● ●   ●                                 ●   ● ●
        ●●  ●                                     ●  ●●
        ●● ●                   ✦                   ● ●●
        ●●  ●              [Center]              ●  ●●
        ● ●   ●                                 ●   ● ●
        ●  ●    ●                             ●    ●  ●
        ●   ●      ●       ●       ●       ●      ●   ●
        ●    ●        Inner Ring (Cyan)        ●    ●
         ●      ●                             ●      ●
          ●        ●       ●       ●       ●        ●
           ●          Middle Ring (Yellow)          ●
              ●                                   ●
                 ●                             ●
                    ●       ●       ●       ●
                        Outer Ring (Magenta)

Ring 1 (Cyan):    Radius 30u,  8 platforms
Ring 2 (Yellow):  Radius 60u,  12 platforms
Ring 3 (Magenta): Radius 90u,  18 platforms
```

---

## Gizmo Colors in Action

### Color Coding Strategy

**By Distance:**
```
Closest → Farthest
Cyan → Green → Yellow → Orange → Magenta → Red
```

**By Tier Type:**
```
Combat Platforms:  Red
Safe Platforms:    Green
Objective Points:  Yellow
Spawn Platforms:   Cyan
Boss Arenas:       Magenta
```

**By Difficulty:**
```
Easy:    Green
Medium:  Yellow
Hard:    Orange
Extreme: Red
```

---

## Scene View Tips

### Best Camera Angles

**Top-Down View (Y-axis):**
- Best for seeing ring spacing
- Shows platform distribution clearly
- Easy to spot overlaps

**Side View (X or Z-axis):**
- Shows orbital plane variance
- Reveals vertical spacing issues
- Good for checking tilt angles

**Isometric View:**
- Overall system overview
- Good for screenshots
- Natural gameplay perspective

---

## Reading the Labels

When you select a GameObject with OrbitalSystem, you'll see labels like:

```
50.0u
8 platforms
```

This means:
- **50.0u**: Ring radius is 50 units from center
- **8 platforms**: This ring will spawn 8 platforms

Labels appear at the **right edge** of each ring (positive X-axis direction).

---

## Gizmo Troubleshooting

### "I don't see any gizmos"

**Check 1: Gizmos Enabled?**
- Look for the "Gizmos" button in Scene view (top-right)
- Make sure it's enabled (not grayed out)

**Check 2: GameObject Selected?**
- Gizmos are brighter when selected
- Try clicking the GameObject in Hierarchy

**Check 3: Rings Configured?**
- Open Inspector
- Check that Rings list is not empty
- Verify platformCount > 0

**Check 4: Camera Position?**
- Zoom out if rings are very large
- Check you're looking at the right position

### "Gizmos are too small/large"

The gizmo size is fixed:
- **Ring circles**: Scale with radius (automatic)
- **Platform spheres**: 1-2 units (fixed)
- **Lines**: 1 pixel wide (fixed)

If they look wrong:
- Adjust your Scene view zoom
- Check that radius values are reasonable (10-200 units typical)

### "Colors are hard to see"

**Solution 1: Change Gizmo Colors**
- Select brighter colors in Inspector
- Avoid dark colors (hard to see on dark backgrounds)
- Use high-contrast colors

**Solution 2: Change Scene Background**
- Unity menu: Edit → Preferences → Colors
- Adjust "Scene Background" color
- Light gray works well for most gizmo colors

**Solution 3: Select the GameObject**
- Selected gizmos are 80% opacity (much brighter)
- Unselected gizmos are only 30% opacity

---

## Gizmo Performance

### How Many Rings Can I Have?

Gizmos are very lightweight:
- **Each ring**: ~64 line segments + platform spheres
- **Typical system**: 3-5 rings = ~300 gizmo elements
- **Large system**: 10 rings = ~1000 gizmo elements

**Performance impact**: Negligible (editor only, not in builds)

### Gizmo Draw Order

Gizmos draw in this order:
1. Ring circles (back to front by radius)
2. Platform position spheres
3. Center-to-platform lines (if selected)
4. Labels (if selected)

---

## Advanced Gizmo Usage

### Measuring Distances

Use gizmos to measure:
1. Select the GameObject
2. Look at the labels (show exact radius)
3. Count platform spheres (verify platform count)
4. Check spacing between rings visually

### Aligning Multiple Systems

If you have multiple orbital systems:
1. Use the same gizmo colors for similar rings
2. Offset ring radii to avoid overlaps
3. Use Scene view to position systems relative to each other

### Debugging Platform Spawns

If platforms spawn in wrong places:
1. Check gizmo sphere positions (where they SHOULD spawn)
2. Enter Play mode and compare
3. If mismatch: check CelestialPlatform.Initialize() logic
4. If match: problem is elsewhere (not in OrbitalSystem)

---

## Gizmo Keyboard Shortcuts

**Unity Scene View Shortcuts:**
- `F`: Frame selected object (centers camera on gizmos)
- `Shift+F`: Frame selected object and zoom to fit
- `Q`: Pan view (hand tool)
- `W/E/R/T`: Transform tools (won't affect gizmos)
- `Alt+Left Mouse`: Orbit camera around selection

**Recommended Workflow:**
1. Select GameObject with OrbitalSystem
2. Press `Shift+F` to frame it
3. Use `Alt+Left Mouse` to orbit and inspect from all angles
4. Adjust ring values in Inspector
5. Gizmos update in real-time!

---

## Gizmo Best Practices

### ✅ DO:
- Use distinct colors for each ring
- Keep ring radii well-spaced (at least 10-20 units apart)
- Select GameObject to see detailed view
- Use top-down view for initial layout

### ❌ DON'T:
- Use very similar colors (hard to distinguish)
- Place rings too close together (visual clutter)
- Forget to enable Gizmos in Scene view
- Ignore the labels (they show exact values!)

---

## Summary

| Gizmo Element | Purpose | When Visible |
|--------------|---------|--------------|
| **Ring Circle** | Shows exact radius | Always (if rings configured) |
| **Platform Sphere** | Shows spawn position | Always (if platformCount > 0) |
| **Center Line** | Shows radial alignment | When selected |
| **Label** | Shows radius & count | When selected |

**The gizmos give you perfect visual feedback before you even press Play!** 🎯
