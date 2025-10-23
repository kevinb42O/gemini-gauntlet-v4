# Procedural Arm Animation System — Setup & Migration

This guide explains how to wire the new per-arm procedural animation system and safely migrate from the legacy `ProceduralHandAnimator`.

Components referenced:
- `Assets/scripts/HandAnimationProfile.cs`
- `Assets/scripts/ProceduralArmAnimator.cs`
- `Assets/scripts/PlayerAnimationController.cs`

---

## 1) Create a HandAnimationProfile asset
- In Unity: Create → GeminiGauntlet → Hand Animation Profile
- Assign sensible defaults (script includes working defaults). You can fine-tune later:
  - Idle: `idleBreathingSpeed`, `idleFloatAmplitude`
  - Recoil: `recoilStrength`, `recoilDuration`, weights per bone
  - Beam: `beamWristUpDegrees`, subtle vibration
  - Movement: `runningSwingAmplitude/speed`, `flyingFloatAmplitude/speed`
  - Inertia: `inertiaAmount`, `inertiaBlendSpeed`, `inertiaResponse`

## 2) Add ProceduralArmAnimator to each arm
Add `ProceduralArmAnimator` to your left/right arm GameObjects (typically under the arms rig).

Required references on each component:
- `profile`: assign the profile asset you created above (same asset can be used for both arms).
- `meshRenderer`: the SkinnedMeshRenderer for the arm/hand mesh (used for bone auto-assign).
- `bonePrefix`: e.g. `L_` for Left, `R_` for Right.
  - Auto-assign expects bone names `Arm`, `Forearm`, `Wrist`, `Hand` with your prefix (e.g., `L_Wrist`).
  - If your naming differs, assign `arm/forearm/wrist/hand` transforms manually.
- Conflict handling:
  - Keep `disableAnimatorConflicts` ON to prevent any Animator/Legacy Animation from driving the same bone chain.
  - Keep `disableOtherAnimationScripts` ON to disable potentially conflicting IK/Constraint/Rig scripts in the controlled chain.

Notes:
- Wrist rotation is constrained to pitch (X axis) only across idle/run/fly/recoil/beam/inertia to prevent inward/outward yaw/roll.
- If the elbow/forearm appears static, verify the `forearm` bone reference and that no other component is overriding it.

## 3) Add PlayerAnimationController to the player root
- Add `PlayerAnimationController` to a suitable root object (e.g., the player root).
- Assign references:
  - `leftArm` / `rightArm`: drag your two `ProceduralArmAnimator` components.
  - `primaryIsRightHand`: set true if primary weapon hand is the right arm.
  - Optional: assign `characterController` (preferred) or `rigidbodySource` for movement sampling.
  - Optional: enable `logDebug` for useful warnings during setup/testing.

The controller will continuously sample motion/grounded state and forward it to both arms.

## 4) Integration with firing code (already rewired)
- `HandFiringMechanics.cs` calls `PlayerAnimationController.Instance.TriggerShotgunRecoil(isPrimary)`
- Beam state uses `PlayerAnimationController.Instance.SetBeamFiring(isPrimary, isFiring)`
- Legacy calls to `ProceduralHandAnimator` were removed.

## 5) Playmode verification checklist
- Idle/Run/Fly:
  - Wrist only pitches up/down. No inward/outward twist.
  - Arm/forearm move subtly; feels natural.
- Recoil:
  - Fire and observe a clean wrist/forearm/shoulder pitch back, then recover.
- Beam:
  - Wrist tilts up by `beamWristUpDegrees`; micro vibration is visible but not excessive.
- Inertia:
  - Quick strafes add slight pitch-only response on the wrist.
- Conflicts:
  - If arm seems frozen or distorted, check Console for `[ProceduralArmAnimator] Disabled ...` messages and confirm no other arm IK/rig components remain enabled in the same chain.

## 6) Migration off legacy ProceduralHandAnimator
Safe removal steps after verification:
1. In all scenes/prefabs, remove any `ProceduralHandAnimator` components still attached to arm objects.
2. Confirm there are no runtime references remaining (search: `ProceduralHandAnimator`).
   - As of now, only comment notes remain in `Assets/scripts/HandFiringMechanics.cs`.
3. Delete obsolete scripts:
   - `Assets/scripts/ProceduralHandAnimator.cs`
   - `Assets/scripts/Editor/ProceduralHandAnimatorEditor.cs`
4. Rebuild and playtest to ensure no missing script warnings in Console.

## 7) Troubleshooting
- Wrong arm receives recoil/beam: adjust `primaryIsRightHand` in `PlayerAnimationController`.
- Wrist still twists: ensure no other animation scripts/constraints are enabled on the wrist chain; leave conflict-disabling toggles ON.
- Bones not auto-assigned: verify `meshRenderer` points to the SkinnedMeshRenderer that actually contains the bone list, and `bonePrefix` matches your skeleton naming. If needed, assign transforms manually.
- No motion while moving: ensure `characterController` or `rigidbodySource` is assigned (or let the controller auto-find in parent), and verify the player is updating velocity.

---

You can tune visuals entirely from the `HandAnimationProfile` without touching code. For deeper changes, see:
- `ProceduralArmAnimator.ApplyIdle/ApplyRunning/ApplyFlying/ApplyRecoil/ApplyBeamPose/ApplyInertia`
- Constraints are implemented by applying wrist rotations as X-axis-only in all states.
