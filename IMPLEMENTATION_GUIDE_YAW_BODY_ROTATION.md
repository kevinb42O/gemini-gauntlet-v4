# IMPLEMENTATION GUIDE: Yaw-Based Body Rotation for Aerial Trick System
## Senior Unity GameDev Level - For Claude Sonnet 4.5 (Promo) Agent

---

## EXECUTIVE SUMMARY

**Objective**: Eliminate camera reconciliation lag for horizontal rotations by rotating the player body (yaw) during aerial tricks, while keeping vertical rotations (pitch/roll) camera-only.

**Current Problem**: 
- Player performs 180° horizontal spin during trick
- Lands facing backward
- Camera reconciles over 0.6 seconds, causing movement disconnect
- Player input feels "drunk" during reconciliation

**Solution**:
- **Horizontal rotation (yaw/Y-axis)**: Rotate player body in real-time during tricks
- **Vertical rotation (pitch/X-axis, roll/Z-axis)**: Camera-only, reconcile on landing
- **Result**: Zero reconciliation lag for horizontal movement, immediate control on landing

---

## SYSTEM ARCHITECTURE ANALYSIS

### Current Hierarchy Structure
```
Player GameObject (has AAAMovementController)
└── Camera GameObject (has AAACameraController)
```

**Key Finding**: `playerTransform = transform.parent` (line 431 of AAACameraController.cs)

### Movement Direction System Analysis

**CRITICAL DISCOVERY** (AAAMovementController.cs, lines 1679-1688):
```csharp
// NORMAL MODE: Camera-relative movement (standard FPS controls)
forward = activeCameraTransform.forward;
right = activeCameraTransform.right;
forward.y = 0;  // Flatten to horizontal plane
right.y = 0;
forward.Normalize();
right.Normalize();
```

**What This Means**:
- Movement system uses **camera transform's forward/right vectors**
- Y-component is zeroed (horizontal plane projection)
- Movement is **camera-relative**, NOT body-relative

**Implication**: 
✅ **PERFECT COMPATIBILITY** - If we rotate the player body with camera yaw, the movement system will automatically follow because it reads `cameraTransform.forward` which is a child of the body.

### Current Yaw Rotation System

**Normal Mode** (AAACameraController.cs, lines 659-665):
```csharp
float yawDelta = currentLook.x - yawStart;
Quaternion yawRotation = Quaternion.AngleAxis(yawDelta, referenceUp);
playerTransform.rotation = yawRotation * baseYawRotation;
```

**Trick Mode** (AAACameraController.cs, lines 2620-2626):
```csharp
// Apply rotations in LOCAL SPACE (3-axis control)
Quaternion pitchRotation = Quaternion.AngleAxis(pitchDelta, Vector3.right);
Quaternion yawRotation = Quaternion.AngleAxis(yawDelta, Vector3.up);
Quaternion rollRotation = Quaternion.AngleAxis(rollDelta, Vector3.forward);

// Combine all three axes
freestyleRotation = freestyleRotation * pitchRotation * yawRotation * rollRotation;
```

**Problem**: In trick mode, yaw is applied to `freestyleRotation` (camera-only), NOT to `playerTransform.rotation` (body).

---

## IMPLEMENTATION STRATEGY

### Phase 1: Separate Yaw from Pitch/Roll in Trick System

**Location**: `HandleFreestyleLookInput()` method (lines 2429-2672)

**Current Code** (lines 2620-2626):
```csharp
// === APPLY ROTATIONS IN LOCAL SPACE (3-AXIS CONTROL) ===
Quaternion pitchRotation = Quaternion.AngleAxis(pitchDelta, Vector3.right);
Quaternion yawRotation = Quaternion.AngleAxis(yawDelta, Vector3.up);
Quaternion rollRotation = Quaternion.AngleAxis(rollDelta, Vector3.forward);

// Combine all three axes
freestyleRotation = freestyleRotation * pitchRotation * yawRotation * rollRotation;
```

**New Code**:
```csharp
// === APPLY ROTATIONS: YAW TO BODY, PITCH/ROLL TO CAMERA ===

// 1. Apply YAW to PLAYER BODY (real-time body rotation)
if (playerTransform != null)
{
    Quaternion bodyYawRotation = Quaternion.AngleAxis(yawDelta, Vector3.up);
    playerTransform.rotation = playerTransform.rotation * bodyYawRotation;
}

// 2. Apply PITCH/ROLL to CAMERA ONLY (freestyle rotation)
Quaternion pitchRotation = Quaternion.AngleAxis(pitchDelta, Vector3.right);
Quaternion rollRotation = Quaternion.AngleAxis(rollDelta, Vector3.forward);

// Combine ONLY pitch and roll (yaw is handled by body)
freestyleRotation = freestyleRotation * pitchRotation * rollRotation;

// 3. CRITICAL: Sync freestyle rotation's yaw with body to prevent drift
// Extract body's yaw and apply to freestyle rotation
Vector3 bodyEuler = playerTransform.rotation.eulerAngles;
Vector3 freestyleEuler = freestyleRotation.eulerAngles;
freestyleRotation = Quaternion.Euler(freestyleEuler.x, bodyEuler.y, freestyleEuler.z);
```

**Rationale**:
- Body rotation handles horizontal orientation (player facing direction)
- Camera rotation handles trick orientation (pitch/roll only)
- Sync step prevents yaw drift between body and camera

---

### Phase 2: Modify Landing Reconciliation

**Location**: `UpdateLandingReconciliation()` method (lines 2310-2416)

**Current Target Calculation** (lines 2348-2358):
```csharp
// Calculate target rotation (normal camera orientation)
Vector3 freestyleEuler = freestyleRotation.eulerAngles;
float normalizedPitch = NormalizeAngle(freestyleEuler.x);
float normalizedYaw = NormalizeAngle(freestyleEuler.y);

reconciliationTargetRotation = Quaternion.Euler(normalizedPitch, normalizedYaw, 0f);
```

**New Target Calculation**:
```csharp
// Calculate target rotation (UPRIGHT camera orientation)
// Yaw is ALREADY CORRECT (body handles it), only reconcile pitch/roll to zero
Vector3 freestyleEuler = freestyleRotation.eulerAngles;

// Target: pitch=0 (upright), yaw=current body yaw (already synced), roll=0 (no tilt)
float currentBodyYaw = NormalizeAngle(playerTransform.rotation.eulerAngles.y);
reconciliationTargetRotation = Quaternion.Euler(0f, currentBodyYaw, 0f);

Debug.Log($"🎯 [RECONCILIATION] Yaw already correct ({currentBodyYaw:F1}°), reconciling pitch/roll only");
```

**Rationale**:
- Yaw reconciliation is eliminated (body already rotated during trick)
- Only pitch and roll need to snap back to zero (upright)
- Reconciliation time effectively reduced (fewer axes to blend)

---

### Phase 3: Update Landing Detection

**Location**: `LandDuringFreestyle()` method (lines 2230-2298)

**Current Clean Landing Check** (lines 2250-2263):
```csharp
// Calculate the ACTUAL angle between current freestyle rotation and UPRIGHT target
Vector3 freestyleEuler = freestyleRotation.eulerAngles;
float normalizedYaw = NormalizeAngle(freestyleEuler.y);

// Target is UPRIGHT - pitch=0 (head up), yaw=preserved (direction), roll=0 (no tilt)
Quaternion targetRotation = Quaternion.Euler(0f, normalizedYaw, 0f);

// This is the REAL deviation - the actual angle we'll reconcile through
float totalDeviation = Quaternion.Angle(freestyleRotation, targetRotation);

bool isCleanLanding = totalDeviation < cleanLandingThreshold;
```

**New Clean Landing Check**:
```csharp
// Calculate deviation from UPRIGHT (pitch/roll only, yaw is irrelevant)
Vector3 freestyleEuler = freestyleRotation.eulerAngles;
float normalizedPitch = NormalizeAngle(freestyleEuler.x);
float normalizedRoll = NormalizeAngle(freestyleEuler.z);

// Calculate pitch/roll deviation from zero (upright = 0° pitch, 0° roll)
float pitchDeviation = Mathf.Abs(normalizedPitch);
float rollDeviation = Mathf.Abs(normalizedRoll);

// Handle wraparound (e.g., 350° is actually -10°, which is 10° from upright)
if (pitchDeviation > 180f) pitchDeviation = 360f - pitchDeviation;
if (rollDeviation > 180f) rollDeviation = 360f - rollDeviation;

// Total deviation is the combined pitch/roll offset from upright
float totalDeviation = Mathf.Sqrt(pitchDeviation * pitchDeviation + rollDeviation * rollDeviation);

bool isCleanLanding = totalDeviation < cleanLandingThreshold;

Debug.Log($"🎯 [LANDING] Pitch: {normalizedPitch:F1}°, Roll: {normalizedRoll:F1}°, Deviation: {totalDeviation:F1}° (yaw ignored)");
```

**Rationale**:
- Clean landing now only considers pitch/roll (vertical orientation)
- Horizontal spin amount is irrelevant (you can land clean after 10 spins)
- More intuitive: "Did I land upright?" not "Did I land facing the right way?"

---

### Phase 4: Initialize Freestyle Rotation Correctly

**Location**: `EnterFreestyleMode()` method (lines 2158-2211)

**Current Initialization** (lines 2162-2166):
```csharp
isFreestyleModeActive = true;
freestyleRotation = transform.localRotation; // Capture current camera rotation
freestyleModeStartTime = Time.time;
isInInitialBurst = true;
```

**New Initialization**:
```csharp
isFreestyleModeActive = true;

// Initialize freestyle rotation with ONLY camera's local pitch
// Yaw comes from body, roll starts at zero
Vector3 currentLocalEuler = transform.localRotation.eulerAngles;
float currentPitch = NormalizeAngle(currentLocalEuler.x);
float currentBodyYaw = NormalizeAngle(playerTransform.rotation.eulerAngles.y);

// Freestyle rotation = current pitch, body's yaw, zero roll
freestyleRotation = Quaternion.Euler(currentPitch, currentBodyYaw, 0f);

freestyleModeStartTime = Time.time;
isInInitialBurst = true;

Debug.Log($"🎪 [FREESTYLE INIT] Pitch: {currentPitch:F1}°, Yaw: {currentBodyYaw:F1}° (from body), Roll: 0°");
```

**Rationale**:
- Ensures clean separation: pitch from camera, yaw from body
- Prevents initial rotation jump when entering freestyle
- Roll starts at zero for clean trick start

---

### Phase 5: Handle Wall Jump Integration

**Location**: `TriggerWallJumpTilt()` method (lines 1024-1091)

**Current BFFL Integration** (lines 1032-1060):
```csharp
if (wasInTrickMode)
{
    // FORCE IMMEDIATE RECONCILIATION TO UPRIGHT (like landing)
    Vector3 freestyleEuler = freestyleRotation.eulerAngles;
    float normalizedYaw = NormalizeAngle(freestyleEuler.y);
    
    // Snap camera to UPRIGHT orientation (pitch=0, roll=0, preserve yaw)
    Quaternion uprightTarget = Quaternion.Euler(0f, normalizedYaw, 0f);
    
    // INSTANT snap - no blending
    freestyleRotation = uprightTarget;
    
    // Clear ALL trick states
    isReconciling = false;
    isInLandingGrace = false;
    isFreestyleModeActive = false;
    
    // Reset momentum physics
    angularVelocity = Vector2.zero;
    rollVelocity = 0f;
    smoothedInput = Vector2.zero;
    inputVelocity = Vector2.zero;
    isFlickBurstActive = false;
    
    Debug.Log($"🤝 [TRICK→WALLJUMP] INSTANT RECONCILIATION! Camera snapped to upright (yaw: {normalizedYaw:F1}°)");
}
```

**New BFFL Integration**:
```csharp
if (wasInTrickMode)
{
    // FORCE IMMEDIATE RECONCILIATION TO UPRIGHT (pitch/roll only, yaw already correct)
    float currentBodyYaw = NormalizeAngle(playerTransform.rotation.eulerAngles.y);
    
    // Snap camera to UPRIGHT orientation (pitch=0, roll=0, yaw=body's current yaw)
    Quaternion uprightTarget = Quaternion.Euler(0f, currentBodyYaw, 0f);
    
    // INSTANT snap - no blending
    freestyleRotation = uprightTarget;
    
    // Clear ALL trick states
    isReconciling = false;
    isInLandingGrace = false;
    isFreestyleModeActive = false;
    
    // Reset momentum physics (but preserve body rotation)
    angularVelocity = Vector2.zero;
    rollVelocity = 0f;
    smoothedInput = Vector2.zero;
    inputVelocity = Vector2.zero;
    isFlickBurstActive = false;
    
    Debug.Log($"🤝 [TRICK→WALLJUMP] INSTANT RECONCILIATION! Pitch/roll zeroed, yaw preserved at {currentBodyYaw:F1}°");
}
```

**Rationale**:
- Wall jump from trick now only resets pitch/roll
- Yaw is already correct (body handled it during trick)
- Cleaner transition, no yaw snapping

---

### Phase 6: Update ApplyCameraTransform

**Location**: `ApplyCameraTransform()` method (lines 1241-1246)

**Current Code**:
```csharp
// 🎪 FREESTYLE MODE: Camera rotation is COMPLETELY INDEPENDENT
if (isFreestyleModeActive || isReconciling)
{
    // During tricks or landing reconciliation, use freestyle rotation
    transform.localRotation = freestyleRotation;
}
```

**New Code**:
```csharp
// 🎪 FREESTYLE MODE: Camera rotation is PITCH/ROLL ONLY (yaw handled by body)
if (isFreestyleModeActive || isReconciling)
{
    // During tricks or landing reconciliation, use freestyle rotation
    // This contains pitch/roll from tricks, yaw synced with body
    transform.localRotation = freestyleRotation;
    
    // CRITICAL: Convert to local rotation relative to body
    // Since body is rotating with yaw, camera's local rotation should only show pitch/roll
    Vector3 bodyEuler = playerTransform.rotation.eulerAngles;
    Vector3 freestyleEuler = freestyleRotation.eulerAngles;
    
    // Camera local rotation = pitch and roll only (yaw is zero in local space)
    float localPitch = NormalizeAngle(freestyleEuler.x - bodyEuler.x);
    float localRoll = NormalizeAngle(freestyleEuler.z);
    
    transform.localRotation = Quaternion.Euler(localPitch, 0f, localRoll);
}
```

**WAIT - CRITICAL REALIZATION**:

Actually, this is **SIMPLER** than I thought. Since `freestyleRotation` will now be synced with body yaw (from Phase 1, step 3), and we're applying it as `transform.localRotation`, it should work correctly.

**Revised Code** (Keep it simple):
```csharp
// 🎪 FREESTYLE MODE: Camera rotation (pitch/roll from tricks, yaw synced with body)
if (isFreestyleModeActive || isReconciling)
{
    // During tricks or landing reconciliation, use freestyle rotation
    // freestyleRotation contains: pitch/roll from tricks, yaw synced with body
    transform.localRotation = freestyleRotation;
}
```

**Rationale**:
- No changes needed here if Phase 1 sync is implemented correctly
- `freestyleRotation` already has correct yaw (synced with body)
- Local rotation application works as-is

---

## EDGE CASES & CONSIDERATIONS

### Edge Case 1: Momentum Mismatch After Horizontal Spin

**Scenario**:
```
Player moving forward (North) at 20 m/s
Performs 180° horizontal spin mid-air
Now facing South, but velocity still North
```

**Current Behavior**: Movement feels "backward" until reconciliation completes

**New Behavior**: Movement feels "backward" immediately (realistic physics)

**Solution**: **Accept it as realistic**
- Real-world physics: momentum is preserved
- Player adapts in 30 seconds
- Alternative (if needed): Gradually rotate velocity vector with body over 0.2s

**Implementation** (if velocity rotation desired):
```csharp
// In HandleFreestyleLookInput(), after applying yaw to body:
if (movementController != null && yawDelta != 0f)
{
    // Gradually rotate horizontal velocity with body
    Vector3 currentVelocity = movementController.Velocity;
    Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
    
    // Rotate velocity by yaw delta (scaled down for gradual feel)
    Quaternion velocityRotation = Quaternion.AngleAxis(yawDelta * 0.3f, Vector3.up);
    horizontalVelocity = velocityRotation * horizontalVelocity;
    
    // Apply rotated velocity back
    movementController.SetVelocity(new Vector3(horizontalVelocity.x, currentVelocity.y, horizontalVelocity.z));
}
```

**Recommendation**: Start without velocity rotation, add only if players complain.

---

### Edge Case 2: Trick Mode Special Movement

**Location**: AAAMovementController.cs, lines 1650-1677

**Current Code**:
```csharp
// TRICK MODE: Use velocity direction as reference frame
if (currentSpeed > 5f)
{
    forward = currentHorizontalVelocity.normalized;
    right = Vector3.Cross(Vector3.up, forward).normalized;
}
else
{
    // Low speed: use camera's horizontal rotation only
    Vector3 cameraForwardFlat = new Vector3(activeCameraTransform.forward.x, 0, activeCameraTransform.forward.z);
    // ...
}
```

**Analysis**: This is **PERFECT** for our new system
- High speed: Uses velocity direction (momentum-based, realistic)
- Low speed: Uses camera forward (which is child of body, so follows body rotation)

**Action**: **No changes needed** - system is already compatible

---

### Edge Case 3: Collider Rotation

**Concern**: Rotating player body rotates the CharacterController collider

**Analysis**:
- CharacterController is a capsule (rotationally symmetric)
- Rotating around Y-axis (yaw) doesn't change collision shape
- No gameplay impact

**Action**: **No changes needed**

---

### Edge Case 4: Animation System

**Potential Issue**: Character model might spin with body during tricks

**Investigation Needed**: Check if player has visible character model

**Solutions**:
1. **No visible model**: No issue
2. **Has model, want it to spin**: No changes needed
3. **Has model, don't want it to spin**: Decouple model from body transform

**Implementation** (if decoupling needed):
```csharp
// In player model controller:
void LateUpdate()
{
    if (cameraController.IsPerformingAerialTricks)
    {
        // Lock model rotation to pre-trick orientation
        transform.rotation = trickStartRotation;
    }
}
```

**Recommendation**: Test first, implement only if needed

---

### Edge Case 5: Multiple Rapid Spins

**Scenario**: Player does 5x 360° spins rapidly

**Current System**: `totalRotationY` accumulates (1800°)

**New System**: Body rotation accumulates the same way

**Concern**: Quaternion drift over many rotations?

**Solution**: Already handled by existing normalization (line 2668):
```csharp
// CRITICAL: Normalize every frame to prevent quaternion drift
freestyleRotation = Quaternion.Normalize(freestyleRotation);
```

**Additional Safety** (add to body rotation):
```csharp
// After applying yaw to body:
playerTransform.rotation = Quaternion.Normalize(playerTransform.rotation);
```

---

## TESTING PROTOCOL

### Test 1: Basic Horizontal Spin
1. Jump and enter trick mode
2. Perform 180° horizontal spin (yaw only)
3. Land
4. **Expected**: Immediate control, no reconciliation lag, movement matches look direction

### Test 2: Vertical Flip
1. Jump and enter trick mode
2. Perform 360° vertical flip (pitch only)
3. Land
4. **Expected**: Reconciliation occurs (pitch only), yaw unchanged, movement correct

### Test 3: Combined Rotation
1. Jump and enter trick mode
2. Perform diagonal input (pitch + yaw + roll)
3. Land
4. **Expected**: Yaw applied to body (no reconciliation), pitch/roll reconcile, movement correct

### Test 4: Wall Jump Combo
1. Jump and enter trick mode
2. Perform 180° spin
3. Wall jump before landing
4. **Expected**: Instant upright (pitch/roll), yaw preserved, seamless transition

### Test 5: Momentum Test
1. Sprint forward (North)
2. Jump and perform 180° spin (now facing South)
3. Land
4. **Expected**: Still moving North, can immediately turn around with input

### Test 6: Rapid Spins
1. Jump and enter trick mode
2. Perform 10x 360° spins rapidly
3. Land
4. **Expected**: No quaternion drift, clean landing, correct orientation

---

## PERFORMANCE CONSIDERATIONS

### Optimization 1: Reduce Reconciliation Complexity

**Before**: Reconcile 3 axes (pitch, yaw, roll)
**After**: Reconcile 2 axes (pitch, roll)

**Performance Gain**: ~33% fewer quaternion operations during reconciliation

### Optimization 2: Eliminate Yaw Drift Sync

**Current**: Every frame during reconciliation, sync yaw between freestyle and normal
**After**: No yaw sync needed (body handles it)

**Performance Gain**: Fewer vector extractions and angle normalizations

### Optimization 3: Simpler Clean Landing Check

**Before**: Full quaternion angle calculation (3D)
**After**: 2D pitch/roll deviation (simpler math)

**Performance Gain**: Faster landing detection

---

## ROLLBACK PLAN

If implementation causes issues, rollback is simple:

### Rollback Step 1: Revert Phase 1
```csharp
// Restore original code in HandleFreestyleLookInput():
Quaternion pitchRotation = Quaternion.AngleAxis(pitchDelta, Vector3.right);
Quaternion yawRotation = Quaternion.AngleAxis(yawDelta, Vector3.up);
Quaternion rollRotation = Quaternion.AngleAxis(rollDelta, Vector3.forward);
freestyleRotation = freestyleRotation * pitchRotation * yawRotation * rollRotation;
```

### Rollback Step 2: Revert Phase 2
```csharp
// Restore original reconciliation target:
float normalizedYaw = NormalizeAngle(freestyleEuler.y);
reconciliationTargetRotation = Quaternion.Euler(normalizedPitch, normalizedYaw, 0f);
```

All other phases are optional enhancements and can be left as-is.

---

## IMPLEMENTATION CHECKLIST

- [ ] **Phase 1**: Separate yaw from pitch/roll in `HandleFreestyleLookInput()`
  - [ ] Apply yaw to `playerTransform.rotation`
  - [ ] Apply pitch/roll to `freestyleRotation`
  - [ ] Sync freestyle yaw with body yaw
  - [ ] Add body rotation normalization

- [ ] **Phase 2**: Modify `UpdateLandingReconciliation()`
  - [ ] Change target to use body yaw
  - [ ] Update debug logs

- [ ] **Phase 3**: Update `LandDuringFreestyle()`
  - [ ] Change clean landing check to pitch/roll only
  - [ ] Update debug logs

- [ ] **Phase 4**: Fix `EnterFreestyleMode()`
  - [ ] Initialize freestyle rotation with body yaw
  - [ ] Update debug logs

- [ ] **Phase 5**: Update `TriggerWallJumpTilt()`
  - [ ] Use body yaw for upright target
  - [ ] Update debug logs

- [ ] **Phase 6**: Verify `ApplyCameraTransform()`
  - [ ] Confirm no changes needed
  - [ ] Test local rotation application

- [ ] **Testing**: Run all 6 test cases
  - [ ] Test 1: Basic horizontal spin
  - [ ] Test 2: Vertical flip
  - [ ] Test 3: Combined rotation
  - [ ] Test 4: Wall jump combo
  - [ ] Test 5: Momentum test
  - [ ] Test 6: Rapid spins

- [ ] **Edge Cases**: Verify handling
  - [ ] Momentum mismatch (decide if velocity rotation needed)
  - [ ] Trick mode movement (verify compatibility)
  - [ ] Collider rotation (verify no issues)
  - [ ] Animation system (check if model exists, decouple if needed)
  - [ ] Quaternion drift (verify normalization)

- [ ] **Polish**: Update serialized fields
  - [ ] Update tooltip for `landingReconciliationDuration` (now pitch/roll only)
  - [ ] Update tooltip for `cleanLandingThreshold` (now pitch/roll only)
  - [ ] Update header comments to reflect new system

---

## EXPECTED RESULTS

### Before Implementation
- Horizontal spin → 0.6s reconciliation lag → movement disconnect
- Player feels "drunk" after landing
- Reconciliation can be interrupted by mouse input (jarring)

### After Implementation
- Horizontal spin → zero reconciliation lag → immediate control
- Player movement matches look direction instantly
- Only vertical orientation reconciles (faster, less noticeable)
- Cleaner, more intuitive feel matching AAA standards

---

## FINAL NOTES FOR IMPLEMENTING AGENT

**Code Quality Standards**:
- Maintain existing code style and formatting
- Preserve all debug logs (update messages to reflect new behavior)
- Keep all safety systems (emergency recovery, quaternion normalization)
- Add comments explaining yaw/body separation

**Testing Requirements**:
- Test in-editor first (Unity Play mode)
- Verify no console errors or warnings
- Check performance (should be equal or better)
- Test all 6 scenarios before marking complete

**Documentation**:
- Update inline comments where behavior changes
- Keep this document in project root for reference
- Note any deviations from plan in implementation

**Risk Mitigation**:
- Implement phases sequentially (test after each)
- Keep rollback plan ready
- If any phase causes issues, revert and document why

---

## TECHNICAL REFERENCE

### Key Variables
- `playerTransform`: Parent transform (player body)
- `transform`: Camera transform (child of player body)
- `freestyleRotation`: Camera's independent rotation during tricks
- `totalRotationY`: Accumulated yaw rotation (for stats/XP)
- `yawDelta`: Frame-to-frame yaw change

### Key Methods
- `HandleFreestyleLookInput()`: Processes trick input (MAIN CHANGE HERE)
- `UpdateLandingReconciliation()`: Blends camera back to upright
- `LandDuringFreestyle()`: Detects landing and starts reconciliation
- `EnterFreestyleMode()`: Initializes trick mode
- `TriggerWallJumpTilt()`: Handles wall jump integration
- `ApplyCameraTransform()`: Applies final rotation to camera

### Coordinate Systems
- **World Space**: Y-up, right-handed
- **Player Local Space**: Y-up (body's up), Z-forward (body's forward)
- **Camera Local Space**: Y-up (camera's up), Z-forward (camera's forward)

### Rotation Axes
- **Pitch (X)**: Vertical flip (forward/backward rotation)
- **Yaw (Y)**: Horizontal spin (left/right rotation)
- **Roll (Z)**: Barrel roll (tilt rotation)

---

**Document Version**: 1.0  
**Created**: 2025-10-21  
**For**: Claude Sonnet 4.5 (Promo) Implementation Agent  
**Project**: Gemini Gauntlet V4.0 - Aerial Trick System Refactor
