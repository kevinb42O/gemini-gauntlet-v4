using UnityEngine;
using System.Collections;

/// <summary>
/// Clean aerial trick system - Separates horizontal (body) from vertical (camera) rotation
/// ARCHITECTURE: Horizontal → Player Body, Vertical/Roll → Camera
/// NO reconciliation needed - rotations are always on the correct transform
/// </summary>
public class AerialTrickSystem : MonoBehaviour
{
    [Header("=== TRICK ACTIVATION ===")]
    [SerializeField] private bool enableTrickSystem = true;
    [SerializeField] private KeyCode trickActivationKey = KeyCode.Mouse2; // Middle mouse
    [SerializeField] private float minAirTimeForTricks = 0.15f;
    
    [Header("=== ROTATION SETTINGS ===")]
    [SerializeField] private float horizontalSensitivity = 3.5f; // Body rotation speed
    [SerializeField] private float verticalSensitivity = 3.5f;   // Camera pitch speed
    [SerializeField] private float rollSensitivity = 2.0f;       // Camera roll speed
    [SerializeField] private bool invertVertical = false;
    
    [Header("=== MOMENTUM PHYSICS ===")]
    [SerializeField] private bool enableMomentum = true;
    [SerializeField] private float angularAcceleration = 12f;
    [SerializeField] private float angularDrag = 4f;
    [SerializeField] private float maxAngularVelocity = 720f;
    
    [Header("=== KEYBOARD ROLL ===")]
    [SerializeField] private bool enableKeyboardRoll = true;
    [SerializeField] private KeyCode rollLeftKey = KeyCode.Q;
    [SerializeField] private KeyCode rollRightKey = KeyCode.E;
    [SerializeField] private float keyboardRollSpeed = 180f;
    
    [Header("=== REFERENCES ===")]
    [SerializeField] private Transform playerBody;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private AAAMovementController movementController;
    
    // State
    private bool isTrickActive = false;
    private float airborneStartTime = 0f;
    private bool wasGroundedLastFrame = true;
    
    // Camera rotation (body-relative)
    private float cameraPitch = 0f;
    private float cameraRoll = 0f;
    
    // Momentum system
    private Vector2 pitchYawVelocity = Vector2.zero; // x=yaw, y=pitch
    private float rollVelocity = 0f;
    
    // Tracking for stats/XP
    private float totalPitchRotation = 0f;
    private float totalYawRotation = 0f;
    private float totalRollRotation = 0f;
    
    // Jump mechanics
    private bool isHoldingTrickButton = false;
    private float trickButtonPressTime = 0f;
    
    void Start()
    {
        // Auto-find references if not set
        if (playerBody == null) playerBody = transform;
        if (cameraTransform == null) cameraTransform = Camera.main?.transform;
        if (movementController == null) movementController = GetComponent<AAAMovementController>();
        
        // Initialize camera pitch from current rotation
        if (cameraTransform != null)
        {
            cameraPitch = cameraTransform.localEulerAngles.x;
            if (cameraPitch > 180f) cameraPitch -= 360f;
        }
    }
    
    void Update()
    {
        if (!enableTrickSystem) return;
        
        bool isGrounded = movementController != null && movementController.IsGrounded;
        bool isAirborne = !isGrounded;
        
        // Track airborne time
        if (isAirborne && wasGroundedLastFrame)
        {
            airborneStartTime = Time.time;
        }
        
        float airTime = isAirborne ? Time.time - airborneStartTime : 0f;
        
        // === TRICK ACTIVATION ===
        if (Input.GetKeyDown(trickActivationKey))
        {
            if (!isAirborne)
            {
                // Trigger jump and activate tricks
                TriggerTrickJump();
                ActivateTricks();
            }
            else if (airTime >= minAirTimeForTricks && !isTrickActive)
            {
                // Already airborne - just activate tricks
                ActivateTricks();
            }
            
            isHoldingTrickButton = true;
            trickButtonPressTime = Time.time;
        }
        
        if (Input.GetKeyUp(trickActivationKey))
        {
            isHoldingTrickButton = false;
            
            // Variable jump height
            if (isAirborne && movementController != null && movementController.Velocity.y > 0f)
            {
                StartCoroutine(ApplyJumpCut());
            }
        }
        
        // === TRICK INPUT ===
        if (isTrickActive && isAirborne)
        {
            HandleTrickInput();
        }
        else if (isTrickActive && !isAirborne)
        {
            Debug.LogWarning("🎪 [TRICK SYSTEM] Trick active but NOT AIRBORNE! Can't process input.");
        }
        
        // === LANDING ===
        if (isGrounded && !wasGroundedLastFrame && isTrickActive)
        {
            HandleLanding();
        }
        
        wasGroundedLastFrame = isGrounded;
    }
    
    void LateUpdate()
    {
        // Clamp pitch when NOT in trick mode (normal gameplay)
        if (!isTrickActive)
        {
            cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f);
        }
        
        // Apply camera rotation (body-relative)
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, cameraRoll);
        }
    }
    
    /// <summary>
    /// Handle trick input - CLEAN SEPARATION: Horizontal → Body, Vertical → Camera
    /// </summary>
    private void HandleTrickInput()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        
        // Debug: Log when we get significant input
        if (Mathf.Abs(mouseX) > 0.01f || Mathf.Abs(mouseY) > 0.01f)
        {
            Debug.Log($"🎪 [TRICK INPUT] Mouse: X={mouseX:F3}, Y={mouseY:F3}");
        }
        
        if (invertVertical) mouseY = -mouseY;
        
        // Time compensation for slow-mo
        float timeScale = Mathf.Max(0.1f, Time.timeScale);
        
        if (enableMomentum)
        {
            // === MOMENTUM-BASED ROTATION ===
            
            // Apply input as force
            Vector2 inputForce = new Vector2(mouseX * horizontalSensitivity, mouseY * verticalSensitivity);
            pitchYawVelocity += inputForce * angularAcceleration * timeScale * Time.unscaledDeltaTime;
            
            // Apply drag
            pitchYawVelocity -= pitchYawVelocity * angularDrag * Time.unscaledDeltaTime;
            
            // Clamp velocity
            pitchYawVelocity = Vector2.ClampMagnitude(pitchYawVelocity, maxAngularVelocity);
            
            // Apply rotation from velocity
            float yawDelta = pitchYawVelocity.x * Time.unscaledDeltaTime;
            float pitchDelta = -pitchYawVelocity.y * Time.unscaledDeltaTime;
            
            // HORIZONTAL → PLAYER BODY (world space)
            if (playerBody != null && Mathf.Abs(yawDelta) > 0.01f)
            {
                playerBody.Rotate(Vector3.up, yawDelta, Space.World);
                totalYawRotation += yawDelta;
            }
            
            // VERTICAL → CAMERA (local space)
            if (Mathf.Abs(pitchDelta) > 0.01f)
            {
                cameraPitch += pitchDelta;
                // NO CLAMP during tricks - allow full 360° rotations!
                totalPitchRotation += pitchDelta;
            }
        }
        else
        {
            // === DIRECT ROTATION (no momentum) ===
            
            float yawDelta = mouseX * horizontalSensitivity * timeScale * Time.unscaledDeltaTime;
            float pitchDelta = -mouseY * verticalSensitivity * timeScale * Time.unscaledDeltaTime;
            
            // HORIZONTAL → PLAYER BODY
            if (playerBody != null && Mathf.Abs(yawDelta) > 0.01f)
            {
                playerBody.Rotate(Vector3.up, yawDelta, Space.World);
                totalYawRotation += yawDelta;
            }
            
            // VERTICAL → CAMERA
            if (Mathf.Abs(pitchDelta) > 0.01f)
            {
                cameraPitch += pitchDelta;
                // NO CLAMP during tricks - allow full 360° rotations!
                totalPitchRotation += pitchDelta;
            }
        }
        
        // === KEYBOARD ROLL ===
        if (enableKeyboardRoll)
        {
            float rollInput = 0f;
            if (Input.GetKey(rollLeftKey)) rollInput = -1f;
            if (Input.GetKey(rollRightKey)) rollInput = 1f;
            
            if (Mathf.Abs(rollInput) > 0.01f)
            {
                float rollDelta = rollInput * keyboardRollSpeed * Time.unscaledDeltaTime;
                cameraRoll += rollDelta;
                totalRollRotation += rollDelta;
            }
            else
            {
                // Decay roll when no input
                cameraRoll = Mathf.Lerp(cameraRoll, 0f, 5f * Time.unscaledDeltaTime);
            }
        }
    }
    
    /// <summary>
    /// Activate trick mode
    /// </summary>
    private void ActivateTricks()
    {
        isTrickActive = true;
        
        // Reset tracking
        totalPitchRotation = 0f;
        totalYawRotation = 0f;
        totalRollRotation = 0f;
        
        // Reset momentum
        pitchYawVelocity = Vector2.zero;
        rollVelocity = 0f;
        
        // Get current camera pitch
        if (cameraTransform != null)
        {
            cameraPitch = cameraTransform.localEulerAngles.x;
            if (cameraPitch > 180f) cameraPitch -= 360f;
        }
        
        Debug.Log("🎪 [TRICK SYSTEM] Activated!");
    }
    
    /// <summary>
    /// Handle landing - NO reconciliation needed, rotations are already correct
    /// </summary>
    private void HandleLanding()
    {
        isTrickActive = false;
        
        // Stop momentum
        pitchYawVelocity = Vector2.zero;
        rollVelocity = 0f;
        
        // Smoothly return camera roll to zero
        StartCoroutine(SmoothReturnRoll());
        
        // Calculate landing quality
        float pitchDeviation = Mathf.Abs(cameraPitch);
        float rollDeviation = Mathf.Abs(cameraRoll);
        float totalDeviation = pitchDeviation + rollDeviation;
        
        bool isCleanLanding = totalDeviation < 25f;
        
        Debug.Log($"🎪 [TRICK SYSTEM] Landed! Rotations: Pitch={totalPitchRotation/360f:F1} Yaw={totalYawRotation/360f:F1} Roll={totalRollRotation/360f:F1} | Clean: {isCleanLanding}");
        
        // Award XP
        if (AerialTrickXPSystem.Instance != null)
        {
            float airtime = Time.time - airborneStartTime;
            Vector3 rotations = new Vector3(totalPitchRotation, totalYawRotation, totalRollRotation);
            AerialTrickXPSystem.Instance.OnTrickLanded(airtime, rotations, transform.position, isCleanLanding);
        }
    }
    
    /// <summary>
    /// Smoothly return camera roll to zero after landing
    /// </summary>
    private IEnumerator SmoothReturnRoll()
    {
        float startRoll = cameraRoll;
        float elapsed = 0f;
        float duration = 0.5f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            cameraRoll = Mathf.Lerp(startRoll, 0f, t);
            yield return null;
        }
        
        cameraRoll = 0f;
    }
    
    /// <summary>
    /// Trigger trick jump
    /// </summary>
    private void TriggerTrickJump()
    {
        if (movementController != null)
        {
            movementController.TriggerTrickJumpFromExternalSystem();
            Debug.Log("🎮 [TRICK SYSTEM] Jump triggered!");
        }
    }
    
    /// <summary>
    /// Apply jump cut for variable height
    /// </summary>
    private IEnumerator ApplyJumpCut()
    {
        yield return null;
        
        if (movementController != null)
        {
            var velocityField = typeof(AAAMovementController).GetField("velocity", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (velocityField != null)
            {
                Vector3 currentVelocity = (Vector3)velocityField.GetValue(movementController);
                currentVelocity.y *= 0.5f;
                velocityField.SetValue(movementController, currentVelocity);
                Debug.Log($"🎮 [TRICK SYSTEM] Jump cut applied! New Y velocity: {currentVelocity.y:F1}");
            }
        }
    }
    
    /// <summary>
    /// Public API: Is trick system active?
    /// </summary>
    public bool IsTrickActive => isTrickActive;
    
    /// <summary>
    /// Public API: Get current camera pitch (for other systems)
    /// </summary>
    public float CameraPitch => cameraPitch;
    
    /// <summary>
    /// Public API: Get current camera roll (for other systems)
    /// </summary>
    public float CameraRoll => cameraRoll;
    
    /// <summary>
    /// Public API: Get trick rotations (for UI/debug systems)
    /// </summary>
    public Vector3 GetTrickRotations()
    {
        return new Vector3(totalPitchRotation, totalYawRotation, totalRollRotation);
    }
    
    /// <summary>
    /// Public API: Get current rotation speed (for UI/debug systems)
    /// </summary>
    public float GetTrickRotationSpeed()
    {
        if (!enableMomentum) return 0f;
        return pitchYawVelocity.magnitude;
    }
}
