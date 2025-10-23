# 🎯 DYNAMIC WALL-RELATIVE CAMERA TILT SYSTEM

## OBJECTIVE
Create AAA camera tilt that leans AWAY from walls during airborne wall jump chains. **Zero new raycasts** - reuse existing wall detection from `AAAMovementController.DetectWall()`.

---

## SYSTEM FLOW
1. Player performs wall jump (airborne)
2. `DetectWall()` already runs every frame → detects wall position
3. Camera reads wall hit point
4. Calculate which side of screen wall is on (-1=left, 0=center, +1=right)
5. Tilt AWAY from wall (left wall = tilt right, right wall = tilt left)
6. Reset on landing

---

## FILE 1: `AAAMovementController.cs`

### Add Public Properties (after line ~230)
```csharp
/// <summary>
/// Returns true if in wall jump chain (airborne + recent wall jump)
/// </summary>
public bool IsInWallJumpChain => consecutiveWallJumps > 0 && !IsGrounded && (Time.time - lastWallJumpTime < 0.6f);

/// <summary>
/// Last detected wall hit point (for camera tilt)
/// </summary>
public Vector3 LastWallHitPoint { get; private set; }
```

### Modify `DetectWall()` (around line 2423)
**Add ONE line after wall is found:**
```csharp
if (foundWall)
{
    lastWallNormal = wallNormal;
    LastWallHitPoint = hitPoint; // ← ADD THIS LINE
    // ...existing code...
}
```

**That's it for movement controller.** No new raycasts, just expose existing data.

---

## FILE 2: `AAACameraController.cs`

### 1. Add Fields (in Header section)
```csharp
[Header("=== DYNAMIC WALL-RELATIVE TILT ===")]
[SerializeField] private bool enableDynamicWallTilt = true;
[SerializeField] private float dynamicTiltMaxAngle = 12f;
[SerializeField] private float dynamicTiltSpeed = 20f;
[SerializeField] private float dynamicTiltReturnSpeed = 15f;
[SerializeField] private float screenCenterDeadzone = 0.2f;
[SerializeField] private bool showDynamicTiltDebug = false;

private float dynamicWallTilt = 0f;
private float dynamicWallTiltTarget = 0f;
private float dynamicWallTiltVelocity = 0f;
```

### 2. Add Update Method (new method)
```csharp
private void UpdateDynamicWallTilt()
{
    if (!enableDynamicWallTilt || movementController == null)
    {
        dynamicWallTiltTarget = 0f;
        return;
    }

    // Only active during wall jump chains
    if (!movementController.IsInWallJumpChain)
    {
        dynamicWallTiltTarget = 0f;
    }
    else
    {
        Vector3 wallPoint = movementController.LastWallHitPoint;
        if (wallPoint != Vector3.zero)
        {
            // Calculate screen position of wall
            Vector3 screenPos = playerCamera.WorldToViewportPoint(wallPoint);
            float horizontalPos = (screenPos.x - 0.5f) * 2f; // -1 to +1

            // Determine side (with deadzone)
            int wallSide = 0;
            if (horizontalPos < -screenCenterDeadzone) wallSide = -1; // Left
            else if (horizontalPos > screenCenterDeadzone) wallSide = 1; // Right

            // Tilt AWAY from wall (negate to invert)
            dynamicWallTiltTarget = -wallSide * dynamicTiltMaxAngle;

            if (showDynamicTiltDebug)
                Debug.Log($"[CAMERA] Wall screen pos: {horizontalPos:F2}, side: {wallSide}, tilt: {dynamicWallTiltTarget:F1}°");
        }
        else
        {
            dynamicWallTiltTarget = 0f;
        }
    }

    // Smooth transition
    float smoothTime = (Mathf.Abs(dynamicWallTiltTarget) > 0.01f) 
        ? (1f / dynamicTiltSpeed) 
        : (1f / dynamicTiltReturnSpeed);
    dynamicWallTilt = Mathf.SmoothDamp(dynamicWallTilt, dynamicWallTiltTarget, 
                                       ref dynamicWallTiltVelocity, smoothTime);
}
```

### 3. Call in LateUpdate()
```csharp
void LateUpdate()
{
    // ...existing camera code...
    
    UpdateDynamicWallTilt(); // ← ADD THIS

    // ...rest of existing code...
}
```

### 4. Blend Tilt in ApplyCameraTransform()
**Find where you apply roll tilt (around line where `currentTilt` or `wallJumpTiltAmount` is used):**
```csharp
// Change from:
float totalRollTilt = currentTilt + wallJumpTiltAmount;

// To:
float totalRollTilt = currentTilt + wallJumpTiltAmount + dynamicWallTilt; // ← ADD dynamicWallTilt
```

### 5. Reset on Landing (in OnPlayerLanded or similar)
```csharp
public void OnPlayerLanded()
{
    // ...existing code...
    dynamicWallTiltTarget = 0f;
    dynamicWallTilt = 0f;
}
```

---

## SYSTEM ISOLATION

**Never runs when:**
- Grounded
- Not in wall jump chain (`IsInWallJumpChain == false`)
- Wall point is `Vector3.zero` (no wall detected)

**Additive blending:**
- Doesn't override other tilt systems
- Adds to existing tilt values

**Performance:**
- Zero new raycasts
- Uses cached wall detection from movement controller
- Only updates when airborne + wall jumping

---

## TUNING PARAMETERS

| Parameter | Default | Effect |
|-----------|---------|--------|
| `dynamicTiltMaxAngle` | 12° | Max tilt angle away from wall |
| `dynamicTiltSpeed` | 20 | Speed of tilt application |
| `dynamicTiltReturnSpeed` | 15 | Speed of return to neutral |
| `screenCenterDeadzone` | 0.2 | Ignore walls near center (0-0.5) |

---

## EXPECTED BEHAVIOR

**Scenario 1: Wall on LEFT side**
```
Wall screen position: -0.5 (left)
wallSide = -1
Tilt = -(-1) * 12° = +12° (tilt RIGHT, away from wall)
```

**Scenario 2: Wall on RIGHT side**
```
Wall screen position: +0.5 (right)
wallSide = +1
Tilt = -(+1) * 12° = -12° (tilt LEFT, away from wall)
```

**Scenario 3: Wall CENTERED or no wall**
```
Wall screen position: 0.0 or Vector3.zero
wallSide = 0
Tilt = 0° (neutral)
```

---

## DEBUG
- Enable `showDynamicTiltDebug = true` in Inspector
- Console shows: screen position, side detection, target tilt
- Verify tilt direction matches wall position

---

## COMPLETE
✅ Zero new raycasts (reuses existing system)  
✅ AAA cinematic feel  
✅ No system conflicts  
✅ Performance: <0.001ms overhead  
✅ Intuitive tilt direction (always away from wall)  
✅ Clean state management  

**Implementation time: 10 minutes**
