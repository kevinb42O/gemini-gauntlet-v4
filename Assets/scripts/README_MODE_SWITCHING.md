# Mode Switching System Documentation

## Overview
This project uses `AAAMovementIntegrator` to switch between Celestial Flight (legacy) and AAA Ground movement with the F key. The integrator owns the toggle, handles auto-landing, preserves/clears lock-on appropriately, bridges landing momentum, and applies platform-following without parenting. `AAAMovementController`’s internal F handling is gated by `allowInternalModeToggle` and auto-disabled when an integrator is present (`preferIntegratorForModeToggle`).

## Key Components

### AAAMovementIntegrator.cs
- Owns F-key toggling between Flight and AAA (disabled during freefall via `isInFreefall`).
- Enables/disables `AAAMovementController`, associated AAA camera and audio controllers, and coordinates with `PlayerMovementManager`.
- Flight → AAA: aligns upright to the surface normal, snaps the capsule bottom to contact, sets the AAA mode to Walking and grounded via reflection, and ensures AAA physics.
- Preserves horizontal landing momentum using a brief, decaying external ground velocity.
- Tracks moving platforms by applying world-space deltas each frame (no parenting to platforms to avoid jitter).
- Lock-on aware: when locked to a platform, falls and lands on that platform; when leaving AAA back to flight, clears lock-on.
- Auto-landing while in flight for generic colliders (tunable distance, radius, slope limit, and layer mask).
- Upright alignment: on Flight → AAA, preserve current yaw and reset visual roll by zeroing `rollRigidbody.localRotation` and `cameraTransform.localRotation` (camera ends perfectly horizontal). In flight auto-upright, only unwind roll (no yaw change).

### AAAMovementController.cs
- CharacterController-based ground controller (fast movement, jump, crouch/slide, slope handling, ground prediction).
- Inspector flags to avoid F-key conflicts:
  - `allowInternalModeToggle` gates internal F handling.
  - `preferIntegratorForModeToggle` auto-disables internal toggle when an `AAAMovementIntegrator` exists.
- Crouch/slide can be internally driven or externally driven (e.g., `CleanAAACrouch`) via an external ground-velocity bridge. When an external driver is detected, internal crouch/slide is auto-disabled to prevent conflicts.
- Integrates with `CelestialDriftController` only when using its internal Flying mode; in normal play, the integrator owns mode switching.

### PlayerMovementManager.cs
- Manages Celestial Flight state and the Lock-On system (current platform lock state).
- During flight, performs conservative downcasts to detect landings and invokes the integrator to switch modes; preserves/restores lock-on to the landing platform.
- On AAA → Flight, lock-on is explicitly cleared by the integrator.
- Provides landing callbacks and a physics-safe platform-follow routine for flight.

### CelestialDriftController.cs
- Original flight controller. The integrator enables/disables it on mode switches and resets it to a clean state when entering flight.

### CelestialPlatform.cs
- Moving platforms with optional Rigidbody, freeze/unfreeze support, and per-frame predicted position/rotation for smooth following. Platforms are kept level (upright) to remain walkable.

## Setup

- Add to the player GameObject:
  - `CharacterController` + `Rigidbody`
  - `CelestialDriftController`
  - `PlayerMovementManager`
  - `AAAMovementIntegrator`
  - `AAAMovementController`, `AAACameraController`, `AAAMovementAudioManager`
  - Optional: `CleanAAACrouch`
- Inspector tips:
  - In `AAAMovementController`: keep `preferIntegratorForModeToggle` true; leave `allowInternalModeToggle` on (it will auto-disable when the integrator is present); disable `allowInternalCrouchSlide` if using `CleanAAACrouch`.
  - In `AAAMovementIntegrator`: enable auto-landing as needed and tune its checks; the player typically starts in flight.
  - In `PlayerMovementManager`: ensure `lockableLayers` includes your `CelestialPlatform` objects.

## How to Test

1. **Basic Mode Switching**
   - Press F to toggle. Flight → AAA: upright aligns to platform up, character snaps to contact; AAA → Flight: lock-on is cleared.
   - Verify free flight control in flight and grounded control in AAA.

2. **Lock-On Landing**
   - In flight, aim at a platform and press the Lock-On key (R by default).
   - Press F to land; you should fall/land on the locked platform and remain locked after landing.

3. **Auto-Landing**
   - With `enableAutoLanding` on, fly low over colliders without pressing F; verify automatic landing triggers when feet overlap a valid surface.

4. **Momentum Preservation**
   - While moving laterally in flight, press F to land. Confirm initial horizontal motion is preserved briefly and decays smoothly.

5. **Platform Following**
   - Stand on a moving `CelestialPlatform`. Confirm position deltas are applied smoothly (no parenting). Step off or switch to flight to stop following.

6. **Crouch/Slide Integration**
   - If `CleanAAACrouch` is present, verify AAA skips its own crouch/slide physics and respects the external driver.

7. **Edge Cases**
   - Toggle at high speed, while falling, near walls, and during a jump.

## Known Issues and Limitations

- Auto-landing will consider any collider unless filtered by `autoLandLayerMask`.
- Extremely steep or vertical surfaces are not supported for grounded walking.
- Very fast-moving platforms may still cause minor micro-corrections due to CharacterController snapping.
- Ensure script execution order allows platforms to update before consumers that read predicted positions (e.g., `PlayerMovementManager`).
- Visual rolling using CharacterController alone is not supported; use a separate visual Rigidbody if roll visuals are desired.

## Future Improvements

- Add a small transition effect when switching modes
- Implement directional gravity towards platform centers
- Add a small grace period after landing before allowing mode switching again
- Improve platform detection for complex platform shapes

