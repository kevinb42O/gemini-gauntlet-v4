# Capture and Destroy Platform - Unity Setup Guide

## Overview
This guide explains how to set up the Capture and Destroy Platform system with orbiting Guardian enemies, capture mechanics, and skull spawning defense.

## Required Scripts
- `CaptureAndDestroyPlatform.cs` - Main platform controller
- `GuardianEnemy.cs` - Orbiting guard enemies
- `CaptureInteractable.cs` - Player interaction handler
- `CaptureProgressUI.cs` - Capture progress display
- `CelestialPlatform.cs` - (Existing) Platform integration

## Step 1: Create the Platform GameObject

1. **Create Platform Base:**
   - Create an empty GameObject: `CaptureAndDestroyPlatform_01`
   - Add Components:
     - `CaptureAndDestroyPlatform` script
     - `CelestialPlatform` script (for universe generator compatibility)
     - `PlatformTrigger` script (if you have one)
   - Add a Collider (BoxCollider or MeshCollider) for the platform surface
   - Tag the platform appropriately

2. **Add Visual Platform Model:**
   - Add your platform 3D model as a child object
   - Ensure it has proper colliders for player landing

## Step 2: Create the Capture Object

1. **Create Capture Object Prefab:**
   - Create GameObject: `CaptureObject_Satellite`
   - Add Components:
     - `CaptureInteractable` script
     - Mesh Renderer with your satellite/tower model
     - Sphere Collider (set as Trigger, radius ~5)
   - Add emission material for glow effect
   - Save as Prefab in `Assets/Prefabs/MissionObjects/`

## Step 3: Create Guardian Enemy Prefab

1. **Guardian Enemy Setup:**
   - Take your existing enemy model
   - Add Components:
     - `GuardianEnemy` script
     - Rigidbody (isKinematic = false, useGravity = false)
     - Collider (for hit detection)
     - AudioSource (for alert sounds)
   - Configure Guardian Settings:
     ```
     Movement Speed: 8
     Rotation Speed: 5
     Orbit Radius: 30
     Orbit Speed: 20
     Orbit Height: 10
     Intruder Detection Range: 50
     Attack Range: 3
     Return Distance: 5
     ```
   - Save as Prefab: `GuardianEnemy.prefab`

## Step 4: Configure Skull Enemy Spawning

1. **Ensure SkullEnemy Prefab Exists:**
   - Your existing `SkullEnemy.prefab` should work
   - Verify it has proper AI and attack behavior

## Step 5: Create Particle Effects

1. **Beam Effect Prefab:**
   - Create Particle System: `CaptureBeamEffect`
   - Configure:
     - Shape: Cone, angle 5°, radius 0.5
     - Start Lifetime: 2
     - Start Speed: 20
     - Start Size: 0.5-1
     - Start Color: Cyan to White gradient
     - Emission: Rate over Time = 100
     - Renderer: Material = Additive/Glow shader
   - Add Light component (optional)
   - Save as Prefab

2. **Capture Start Effect:**
   - Create smaller particle burst effect
   - Use for visual feedback when capture begins

## Step 6: Configure the Platform

1. **In the Inspector for CaptureAndDestroyPlatform:**

   **Basic Settings:**
   ```
   Platform Name: "Alpha Outpost"
   Mission XP Reward: 500
   ```

   **Guardian Configuration:**
   ```
   Guardian Prefab: [Drag GuardianEnemy.prefab]
   Number Of Guardians: 3
   Guardian Orbit Radius: 30
   Guardian Orbit Speed: 20
   Guardian Orbit Height: 10
   ```

   **Capture Configuration:**
   ```
   Capture Object Prefab: [Drag CaptureObject_Satellite.prefab]
   Capture Object Height Offset: 5
   Capture Time: 30
   Interaction Range: 5
   ```

   **Skull Defense:**
   ```
   Skull Enemy Prefab: [Drag SkullEnemy.prefab]
   Skull Spawn Interval: 2
   Max Skulls At Once: 5
   Skull Spawn Radius: 15
   Skull Spawn Height: 10
   ```

   **Effects:**
   ```
   Beam Effect Prefab: [Drag CaptureBeamEffect.prefab]
   Capture Start Effect: [Optional particle effect]
   Alert Sound: [Drag alert audio clip]
   Capture Complete Sound: [Drag success audio clip]
   ```

   **UI:**
   ```
   Progress UI Prefab: [Can leave empty, will auto-create]
   ```

## Step 7: Test the Setup

1. **Place Platform in Scene:**
   - Position the platform in your level
   - Ensure it's accessible to the player

2. **Test Checklist:**
   - [ ] Guardians spawn and orbit the platform
   - [ ] Guardians attack when player approaches
   - [ ] Capture object appears and glows
   - [ ] Player can interact with capture object (Press E)
   - [ ] Capture timer starts and UI appears
   - [ ] Skulls spawn during capture
   - [ ] Capture can be interrupted by leaving platform
   - [ ] Beam effect plays on completion
   - [ ] XP is awarded on success
   - [ ] Multiple platforms work independently

## Step 8: Multiple Platforms

To add more capture platforms:

1. **Duplicate the configured platform GameObject**
2. **Rename uniquely:** `CaptureAndDestroyPlatform_02`, etc.
3. **Modify per-platform settings:**
   - Platform Name
   - Number of Guardians
   - Capture Time
   - XP Reward

## Advanced Configuration

### Custom Guardian Patterns
In GuardianEnemy inspector, adjust:
- `orbitRadius` - Distance from platform center
- `orbitSpeed` - How fast they circle
- `orbitHeight` - Height above platform
- `intruderDetectionRange` - Alert distance

### Difficulty Scaling
For harder platforms:
- Increase `numberOfGuardians` (4-6)
- Decrease `captureTime` (20 seconds)
- Increase `skullSpawnInterval` (faster spawning)
- Increase `maxSkullsAtOnce` (more enemies)

### Visual Customization
- Change capture object model (tower, beacon, etc.)
- Modify beam effect colors/intensity
- Add platform-specific lighting
- Use different guardian enemy models

## Troubleshooting

**Guardians not orbiting:**
- Check `platformToGuard` is assigned
- Verify platform has proper Transform component
- Check orbit parameters are non-zero

**Capture not starting:**
- Verify player has "Player" tag
- Check interaction range is sufficient
- Ensure CaptureInteractable is on capture object

**Skulls not spawning:**
- Verify SkullEnemy prefab is assigned
- Check spawn height isn't underground
- Ensure platform bounds are correct

**UI not showing:**
- CaptureProgressUI will auto-create if not assigned
- Check Canvas exists in scene
- Verify UI layer settings

## Integration with UniverseGenerator

The platform works with your existing universe generation:

1. **CelestialPlatform Component:**
   - Already attached to platform
   - Ensures compatibility with generation system

2. **Spawn Configuration:**
   - Add to your platform spawn list
   - Set rarity/frequency as desired
   - Can mix with regular platforms

## Performance Optimization

For better performance with multiple platforms:

1. **LOD System:**
   - Disable guardian AI when far from player
   - Reduce particle effects at distance

2. **Object Pooling:**
   - Pool skull enemies instead of instantiating
   - Reuse particle effects

3. **Optimization Settings:**
   ```csharp
   // In CaptureAndDestroyPlatform Start()
   if (Vector3.Distance(transform.position, player.position) > 200f)
   {
       // Disable expensive updates
       enabled = false;
   }
   ```

## Final Notes

- Start with 1-2 platforms for testing
- Gradually increase difficulty/complexity
- Monitor performance with multiple platforms
- Adjust parameters based on playtesting
- Consider save/load system integration

This completes the Capture and Destroy Platform system setup!
