using UnityEngine;
using System.Collections;

/// <summary>
/// Handles the death camera sequence - zooms out from player and shows CONNECTION LOST
/// </summary>
public class DeathCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Camera mainCamera; // FPS camera (will be disabled)
    [SerializeField] private Transform playerTransform;
    private Camera bleedOutCamera; // DEDICATED third-person camera for bleeding out
    private GameObject bleedOutCameraObject;
    
    [Header("Movement Controller References")]
    [Tooltip("AAAMovementController to disable during bleed out (auto-found if null)")]
    [SerializeField] private AAAMovementController aaaMovementController;
    [Tooltip("CleanAAACrouch to disable during bleed out (auto-found if null)")]
    [SerializeField] private CleanAAACrouch cleanAAACrouch;
    [Tooltip("BleedOutMovementController to activate during bleed out (auto-found if null)")]
    [SerializeField] private BleedOutMovementController bleedOutMovementController;
    [Tooltip("AAACameraController to disable during bleed out (auto-found if null)")]
    [SerializeField] private AAACameraController aaaCameraController;
    
    // Movement controller state tracking
    private bool aaaMovementWasEnabled = false;
    private bool crouchWasEnabled = false;
    private bool aaaCameraWasEnabled = false;
    
    [Header("Death Sequence Settings")]
    [SerializeField] private float cameraHeight = 500f; // Height above player - VERY HIGH overhead view for full visibility!
    [SerializeField] private float zoomOutDuration = 1.5f; // How long the zoom takes
    [SerializeField] private AnimationCurve zoomOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float pitchAngle = 60f; // Look down angle at player (top-down view)
    
    [Header("Third-Person Follow Settings")]
    [SerializeField] private bool enableCameraFollow = true; // Follow player while bleeding out
    [SerializeField] private float followSmoothness = 8f; // INCREASED for buttery smooth follow
    [SerializeField] private float lookAroundSensitivity = 1.5f; // Mouse look sensitivity
    [SerializeField] private float maxLookAroundAngle = 45f; // Max angle from looking down
    
    [Header("AAA Visual Effects")]
    [SerializeField] private bool enableBreathingEffect = false; // DISABLED - causes twitching
    [SerializeField] private float breathingIntensity = 0.5f; // Reduced intensity
    [SerializeField] private float breathingSpeed = 0.8f; // Breathing rate (labored, slow)
    [SerializeField] private bool enableStrugglingShake = false; // DISABLED - causes twitching
    [SerializeField] private float struggleShakeIntensity = 0.3f; // Reduced shake
    [SerializeField] private float cameraHeightVariation = 1f; // Reduced height variation
    
    // Visual effect state
    private float breathingTimer = 0f;
    private Vector3 currentCameraOffset = Vector3.zero;
    
    [Header("Wall Avoidance")]
    [SerializeField] private bool enableWallAvoidance = true;
    [SerializeField] private float minCameraDistance = 20f; // Minimum distance if wall detected
    [SerializeField] private float wallCheckRadius = 5f; // Sphere cast radius
    [SerializeField] private LayerMask wallLayers = -1; // What counts as walls
    [SerializeField] private float wallPushbackDistance = 3f; // How far to push camera away from walls
    [SerializeField] private bool lockRotationNearWalls = true; // Prevent spinning in tight spaces
    
    // Wall avoidance state
    private bool isCameraBlockedByWall = false;
    private Vector3 lastSafeCameraPosition = Vector3.zero;
    
    [Header("Auto-Setup")]
    [SerializeField] private bool autoFindReferences = true;
    
    // Original camera state (to restore later if needed)
    private Vector3 originalCameraLocalPosition;
    private Quaternion originalCameraLocalRotation;
    private Transform originalCameraParent;
    private bool isDeathSequenceActive = false;
    private bool isBleedingOutMode = false; // True during bleeding out, false during actual death
    
    // Camera look control
    private float currentYaw = 0f;
    private float currentPitch = 60f; // Start looking down
    private Vector3 targetCameraPosition;
    private Quaternion targetCameraRotation;
    
    void Awake()
    {
        if (autoFindReferences)
        {
            // Find main camera if not assigned
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    mainCamera = FindObjectOfType<Camera>();
                }
            }
            
            // Find player transform if not assigned
            if (playerTransform == null)
            {
                PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerTransform = playerHealth.transform;
                }
            }
            
            // Auto-find movement controllers on player
            if (playerTransform != null)
            {
                if (aaaMovementController == null)
                {
                    aaaMovementController = playerTransform.GetComponent<AAAMovementController>();
                }
                if (cleanAAACrouch == null)
                {
                    cleanAAACrouch = playerTransform.GetComponent<CleanAAACrouch>();
                }
                if (bleedOutMovementController == null)
                {
                    bleedOutMovementController = playerTransform.GetComponent<BleedOutMovementController>();
                    
                    // If not found, add it automatically
                    if (bleedOutMovementController == null)
                    {
                        bleedOutMovementController = playerTransform.gameObject.AddComponent<BleedOutMovementController>();
                        Debug.Log("[DeathCameraController] Auto-created BleedOutMovementController component");
                    }
                }
            }
            
            // Auto-find AAACameraController on main camera
            if (mainCamera != null && aaaCameraController == null)
            {
                aaaCameraController = mainCamera.GetComponent<AAACameraController>();
            }
        }
        
        if (mainCamera != null)
        {
            // Save original camera state
            originalCameraParent = mainCamera.transform.parent;
            originalCameraLocalPosition = mainCamera.transform.localPosition;
            originalCameraLocalRotation = mainCamera.transform.localRotation;
            
            Debug.Log("[DeathCameraController] Initialized - Original camera state saved");
        }
        else
        {
            Debug.LogError("[DeathCameraController] Main camera not found! Death sequence will not work.");
        }
        
        // Create dedicated bleed out camera (disabled by default)
        CreateBleedOutCamera();
    }
    
    /// <summary>
    /// Create a separate, dedicated camera for bleeding out mode
    /// This camera is ONLY active during bleeding out - clean and simple!
    /// </summary>
    private void CreateBleedOutCamera()
    {
        // Create new camera GameObject
        bleedOutCameraObject = new GameObject("BleedOutCamera");
        bleedOutCameraObject.transform.SetParent(null); // World space
        
        // Add Camera component
        bleedOutCamera = bleedOutCameraObject.AddComponent<Camera>();
        
        // Copy settings from main camera
        if (mainCamera != null)
        {
            bleedOutCamera.fieldOfView = mainCamera.fieldOfView;
            bleedOutCamera.nearClipPlane = mainCamera.nearClipPlane;
            bleedOutCamera.farClipPlane = mainCamera.farClipPlane;
            bleedOutCamera.clearFlags = mainCamera.clearFlags;
            bleedOutCamera.backgroundColor = mainCamera.backgroundColor;
            
            // Copy culling mask from main camera - render everything including hands
            bleedOutCamera.cullingMask = mainCamera.cullingMask;
            
            bleedOutCamera.depth = mainCamera.depth + 1; // Render on top
        }
        else
        {
            // Default settings if no main camera
            bleedOutCamera.fieldOfView = 100f;
            bleedOutCamera.nearClipPlane = 0.1f;
            bleedOutCamera.farClipPlane = 10000f;
            bleedOutCamera.cullingMask = ~0; // Render everything by default
        }
        
        // DISABLE camera by default (only enable during bleeding out)
        bleedOutCamera.enabled = false;
        bleedOutCameraObject.SetActive(false);
        
        Debug.Log("[DeathCameraController] Created dedicated BleedOutCamera - disabled by default");
    }
    
    /// <summary>
    /// Start the death camera sequence (actual death - no movement)
    /// </summary>
    public void StartDeathSequence()
    {
        if (isDeathSequenceActive)
        {
            Debug.LogWarning("[DeathCameraController] Death sequence already active");
            return;
        }
        
        if (mainCamera == null)
        {
            Debug.LogError("[DeathCameraController] Cannot start death sequence - camera is null");
            return;
        }
        
        if (playerTransform == null)
        {
            Debug.LogError("[DeathCameraController] Cannot start death sequence - player transform is null");
            return;
        }
        
        Debug.Log("[DeathCameraController] Starting death camera sequence");
        isDeathSequenceActive = true;
        isBleedingOutMode = false; // Not bleeding out, actually dead
        
        StartCoroutine(DeathSequenceCoroutine());
    }
    
    /// <summary>
    /// Stop the death sequence and restore camera
    /// Disables bleed out camera and re-enables main camera
    /// ALSO: Re-enables AAAMovementController and CleanAAACrouch, disables BleedOutMovementController
    /// </summary>
    public void StopDeathSequence()
    {
        if (!isDeathSequenceActive) return;
        
        Debug.Log("[DeathCameraController] Stopping death sequence - restoring camera and controllers");
        isDeathSequenceActive = false;
        isBleedingOutMode = false;
        
        // ========== DISABLE BLEED OUT SYSTEMS ==========
        // DISABLE bleed out movement controller
        if (bleedOutMovementController != null && bleedOutMovementController.IsActive())
        {
            bleedOutMovementController.DeactivateBleedOutMovement();
            Debug.Log("[DeathCameraController] 🔴 BleedOutMovementController DEACTIVATED");
        }
        
        // DISABLE bleed out camera
        if (bleedOutCamera != null)
        {
            bleedOutCamera.enabled = false;
            bleedOutCameraObject.SetActive(false);
            Debug.Log("[DeathCameraController] BleedOutCamera DISABLED");
        }
        
        // ========== RE-ENABLE MAIN CONTROLLERS ==========
        // RE-ENABLE AAAMovementController (restore to previous state)
        if (aaaMovementController != null)
        {
            aaaMovementController.enabled = aaaMovementWasEnabled;
            Debug.Log($"[DeathCameraController] ✅ RE-ENABLED AAAMovementController (restored to {aaaMovementWasEnabled})");
        }
        
        // RE-ENABLE CleanAAACrouch (restore to previous state)
        if (cleanAAACrouch != null)
        {
            cleanAAACrouch.enabled = crouchWasEnabled;
            Debug.Log($"[DeathCameraController] ✅ RE-ENABLED CleanAAACrouch (restored to {crouchWasEnabled})");
        }
        
        // RE-ENABLE AAACameraController (restore to previous state)
        if (aaaCameraController != null)
        {
            aaaCameraController.enabled = aaaCameraWasEnabled;
            Debug.Log($"[DeathCameraController] ✅ RE-ENABLED AAACameraController (restored to {aaaCameraWasEnabled})");
        }
        
        // RE-ENABLE main camera (FPS camera) - hands were never disabled
        if (mainCamera != null)
        {
            mainCamera.enabled = true;
            Debug.Log("[DeathCameraController] Main camera RE-ENABLED - back to FPS view");
        }
        
        // Restore camera to original parent and position (if needed)
        if (mainCamera != null && originalCameraParent != null)
        {
            mainCamera.transform.SetParent(originalCameraParent);
            mainCamera.transform.localPosition = originalCameraLocalPosition;
            mainCamera.transform.localRotation = originalCameraLocalRotation;
        }
        
        // Restore cursor lock (back to FPS mode)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    /// <summary>
    /// Death sequence coroutine - static camera (player can't move)
    /// </summary>
    private IEnumerator DeathSequenceCoroutine()
    {
        // Calculate target position (directly above player)
        Vector3 startPosition = mainCamera.transform.position;
        Vector3 targetPosition = playerTransform.position + Vector3.up * cameraHeight;
        
        // Calculate target rotation (look down at player)
        Vector3 directionToPlayer = playerTransform.position - targetPosition;
        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
        
        Quaternion startRotation = mainCamera.transform.rotation;
        
        // Unparent camera from player
        mainCamera.transform.SetParent(null);
        
        // Animate camera movement
        float elapsed = 0f;
        
        while (elapsed < zoomOutDuration && isDeathSequenceActive)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / zoomOutDuration);
            float curveT = zoomOutCurve.Evaluate(t);
            
            // Smoothly move camera
            mainCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, curveT);
            mainCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, curveT);
            
            yield return null;
        }
        
        // Ensure final position is set
        if (isDeathSequenceActive)
        {
            mainCamera.transform.position = targetPosition;
            mainCamera.transform.rotation = targetRotation;
            
            Debug.Log("[DeathCameraController] Death camera sequence complete - static view");
        }
    }
    
    /// <summary>
    /// Bleed out camera coroutine - FOLLOWS player and allows looking around!
    /// Uses DEDICATED bleed out camera - clean and simple!
    /// </summary>
    private IEnumerator BleedOutCameraCoroutine()
    {
        // Get starting position from main camera (before we disable it)
        Vector3 startPosition = mainCamera != null ? mainCamera.transform.position : playerTransform.position;
        Quaternion startRotation = mainCamera != null ? mainCamera.transform.rotation : Quaternion.identity;
        
        // Initial zoom out animation - SMOOTH transition to angled view
        float elapsed = 0f;
        while (elapsed < zoomOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / zoomOutDuration);
            float curveT = zoomOutCurve.Evaluate(t);
            
            // Calculate ANGLED position (behind and above) - same as UpdateBleedOutCamera
            Vector3 offset = -playerTransform.forward * cameraHeight * 0.5f; // Behind player
            offset += Vector3.up * cameraHeight; // Above player
            Vector3 targetPos = playerTransform.position + offset;
            
            // Calculate ANGLED rotation with pitch
            Vector3 dirToPlayer = playerTransform.position - targetPos;
            Quaternion targetRot = Quaternion.LookRotation(dirToPlayer);
            Vector3 eulerAngles = targetRot.eulerAngles;
            eulerAngles.x = pitchAngle; // Apply pitch angle from Inspector
            targetRot = Quaternion.Euler(eulerAngles);
            
            // Smoothly zoom out to ANGLED position (not straight up!)
            bleedOutCamera.transform.position = Vector3.Lerp(startPosition, targetPos, curveT);
            bleedOutCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRot, curveT);
            
            yield return null;
        }
        
        Debug.Log("[DeathCameraController] BleedOut camera - third person follow active!");
        
        // NOW: Continuous follow mode with look-around capability
        while (isDeathSequenceActive && isBleedingOutMode)
        {
            UpdateBleedOutCamera();
            yield return null;
        }
    }
    
    /// <summary>
    /// ANGLED third-person camera - follows player from behind and above
    /// NO MOUSE INPUT - camera follows player movement direction only
    /// FIXED ANGLE - uses pitchAngle setting from Inspector
    /// </summary>
    private void UpdateBleedOutCamera()
    {
        if (playerTransform == null || bleedOutCamera == null) return;
        
        // Calculate camera position: Behind and above player at fixed angle
        // Use player's forward direction to position camera behind them
        Vector3 offset = -playerTransform.forward * cameraHeight * 0.5f; // Behind player
        offset += Vector3.up * cameraHeight; // Above player
        
        Vector3 desiredPosition = playerTransform.position + offset;
        
        // SMOOTH FOLLOW: Camera follows player smoothly
        bleedOutCamera.transform.position = Vector3.Lerp(
            bleedOutCamera.transform.position, 
            desiredPosition, 
            followSmoothness * Time.unscaledDeltaTime
        );
        
        // ANGLED ROTATION: Look at player with fixed pitch angle (from Inspector)
        Vector3 lookDirection = playerTransform.position - bleedOutCamera.transform.position;
        
        // Apply pitch angle override (Inspector setting)
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        
        // Override pitch to use Inspector setting
        Vector3 eulerAngles = targetRotation.eulerAngles;
        eulerAngles.x = pitchAngle; // Use Inspector pitch angle (e.g., 20 degrees)
        targetRotation = Quaternion.Euler(eulerAngles);
        
        bleedOutCamera.transform.rotation = targetRotation;
    }
    
    /// <summary>
    /// Reset the camera to its original state
    /// Call this when respawning or reloading scene
    /// </summary>
    public void ResetCamera()
    {
        StopDeathSequence();
    }
    
    /// <summary>
    /// Check if death sequence is currently active
    /// </summary>
    public bool IsDeathSequenceActive()
    {
        return isDeathSequenceActive;
    }
    
    /// <summary>
    /// Start bleeding out camera mode (overhead view for crawling)
    /// Activates DEDICATED bleed out camera and disables main camera
    /// ALSO: Disables AAAMovementController and CleanAAACrouch, enables BleedOutMovementController
    /// </summary>
    public void StartBleedOutCameraMode()
    {
        if (isDeathSequenceActive)
        {
            Debug.LogWarning("[DeathCameraController] Camera sequence already active");
            return;
        }
        
        if (bleedOutCamera == null)
        {
            Debug.LogError("[DeathCameraController] BleedOutCamera not created!");
            return;
        }
        
        if (playerTransform == null)
        {
            Debug.LogError("[DeathCameraController] Cannot start bleed out mode - player transform is null");
            return;
        }
        
        Debug.Log("[DeathCameraController] Starting bleed out camera mode - ACTIVATING DEDICATED CAMERA");
        
        // ========== DISABLE MAIN CONTROLLERS ==========
        // Save current enabled states
        if (aaaMovementController != null)
        {
            aaaMovementWasEnabled = aaaMovementController.enabled;
            aaaMovementController.enabled = false;
            Debug.Log($"[DeathCameraController] 🔴 DISABLED AAAMovementController (was {aaaMovementWasEnabled})");
        }
        
        if (cleanAAACrouch != null)
        {
            crouchWasEnabled = cleanAAACrouch.enabled;
            cleanAAACrouch.enabled = false;
            Debug.Log($"[DeathCameraController] 🔴 DISABLED CleanAAACrouch (was {crouchWasEnabled})");
        }
        
        // CRITICAL: Disable AAACameraController to stop mouse look and camera effects
        if (aaaCameraController != null)
        {
            aaaCameraWasEnabled = aaaCameraController.enabled;
            aaaCameraController.enabled = false;
            Debug.Log($"[DeathCameraController] 🔴 DISABLED AAACameraController (was {aaaCameraWasEnabled}) - NO MORE SPINNING!");
        }
        
        // CRITICAL: DISABLE main camera (FPS camera) ONLY - hands stay on body!
        if (mainCamera != null)
        {
            mainCamera.enabled = false;
            Debug.Log("[DeathCameraController] Main camera DISABLED - hands stay attached to body");
        }
        
        // ========== ENABLE BLEED OUT SYSTEMS ==========
        // ENABLE dedicated bleed out camera
        bleedOutCameraObject.SetActive(true);
        bleedOutCamera.enabled = true;
        Debug.Log("[DeathCameraController] BleedOutCamera ENABLED");
        
        // ENABLE bleed out movement controller (keyboard-only simple controller)
        if (bleedOutMovementController != null)
        {
            bleedOutMovementController.ActivateBleedOutMovement(bleedOutCamera);
            Debug.Log("[DeathCameraController] ✅ BleedOutMovementController ACTIVATED (keyboard-only)");
        }
        else
        {
            Debug.LogError("[DeathCameraController] BleedOutMovementController is NULL! Cannot activate bleeding out movement!");
        }
        
        isDeathSequenceActive = true;
        isBleedingOutMode = true;
        
        // Initialize camera angles to fixed overhead position
        currentYaw = 0f;
        currentPitch = pitchAngle;
        
        // Hide cursor during bleed out (player crawls with keyboard only)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        StartCoroutine(BleedOutCameraCoroutine());
    }
}
