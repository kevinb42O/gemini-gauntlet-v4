# Advanced Grappling System - Physics Polish Summary

## Core Improvements

### 1. ENHANCED PENDULUM PHYSICS
- Added centripetal force (F = mv²/r) for realistic rope tension
- Configurable rope constraint stiffness (prevents jitter)
- Velocity damping on constraint violations
- Speed-adaptive air control (less control at high speeds)
- Fixed ground shot bug (rope no longer pushes you up)

### 2. SEAMLESS TRANSITIONS
- Rope to Wall Jump: Press jump while grappling
- Wall Jump to Rope: Shoot rope during wall jump (preserves momentum)
- Rope to Tricks: Already works via existing momentum system

### 3. UNIVERSAL SURFACE ATTACHMENT
- Minimum range = 0 (can shoot ground/close surfaces)
- Smart retraction (no retraction for close shots)
- Works on ground, walls, ceilings - anywhere
- Smart mode selection (ground shots auto-start in Pull mode)

### 4. MOVING PLATFORM SUPPORT
- Grapple to kinematic rigidbodies (moving platforms)
- Anchor follows platform movement in real-time
- Platform velocity inheritance on release (slingshot effect!)
- Configurable velocity multiplier (0-2x)

### 4. SKILLFUL RELEASE MECHANICS
- Optimal release angle bonus (45 degrees = max boost)
- Rewards timing and physics knowledge
- Up to 1.25x bonus for perfect releases

### 5. ENHANCED VISUAL FEEDBACK
- Rope color shows speed AND tension
- Blue (relaxed) to Cyan (moving) to Orange (fast) to Red (max tension)
- Rope width pulses with tension
- Clear visual feedback for physics state

## New Inspector Settings

### Swing Physics
- centripetalForceMultiplier: 1.2 (rope tension strength)
- ropeConstraintStiffness: 0.85 (snap hardness)
- constraintViolationDamping: 0.15 (jitter prevention)

### Release Mechanics
- optimalReleaseAngle: 45 degrees
- releaseAngleBonus: 1.25x multiplier

### Wall Jump Integration
- enableWallJumpWhileGrappling: true
- enableRopeShootDuringWallJump: true
- wallJumpToRopeMomentumBlend: 0.7 (70% wall jump momentum kept)

### Moving Platform Support
- enableMovingPlatformGrapple: true
- inheritPlatformVelocityOnRelease: true
- platformVelocityMultiplier: 1.0 (100% platform velocity added)

## How to Use

### Ground Shots
1. Look at ground near you
2. Click rope button
3. Instant attachment - use for quick jumps

### Wall Jump Combos
1. While swinging, press Jump near wall
2. OR: Wall jump, then shoot rope mid-air
3. Momentum chains seamlessly

### Perfect Releases
1. Swing to build speed
2. Release at upward arc (45 degree angle)
3. Get bonus momentum for skillful timing

### Moving Platform Combos
1. Grapple to moving platform (kinematic rigidbody)
2. Rope follows platform movement
3. Release to inherit platform velocity (slingshot!)
4. Combine with swing momentum for massive speed

## Technical Details

The system now uses proper physics:
- Centripetal acceleration pulls toward anchor
- Tangential forces add swing energy
- Constraint system prevents rope stretching
- Momentum preservation on all transitions
