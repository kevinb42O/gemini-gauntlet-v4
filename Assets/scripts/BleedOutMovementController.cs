using UnityEngine;

/// <summary>
/// ULTRA-SIMPLE third-person keyboard-only movement controller for bleeding out mode.
/// ONLY controls character movement - camera is handled separately by DeathCameraController.
/// This is designed to be DEAD SIMPLE to prevent any conflicts with the main movement systems.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class BleedOutMovementController : MonoBehaviour
{
    [Header("=== BLEED OUT MOVEMENT ===")]
    [Tooltip("Slow crawl speed when bleeding out")]
    [SerializeField] private float crawlSpeed = 2.5f; // Very slow crawling
    
    [Tooltip("Turn speed when using A/D keys (degrees per second)")]
    [SerializeField] private float turnSpeed = 10f; // How fast player turns left/right
    
    [Tooltip("Input smoothing for smooth, heavy crawl feel")]
    [SerializeField] private float inputSmoothing = 8f;
    
    [Tooltip("Gravity while crawling (keeps player grounded)")]
    [SerializeField] private float gravity = -20f;
    
    [Header("=== REFERENCES ===")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Camera bleedOutCamera; // Third-person camera to get forward direction
    
    // State
    private Vector2 currentInput = Vector2.zero;
    private Vector2 inputVelocity = Vector2.zero;
    private float verticalVelocity = 0f;
    private bool isActive = false;
    
    // CRITICAL: CharacterController ownership tracking
    private bool _hasCharacterControllerOwnership = false;
    private bool _characterControllerWasEnabled = false;
    
    void Awake()
    {
        // Auto-find references
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
            if (controller != null)
            {
                Debug.Log($"[BleedOutMovement] Found CharacterController: {controller.name}");
            }
            else
            {
                Debug.LogError("[BleedOutMovement] CharacterController NOT FOUND! Make sure Player has CharacterController component!");
            }
        }
        
        // This script starts disabled - it's only activated during bleed out
        enabled = false;
    }
    
    void OnEnable()
    {
        // Double-check controller reference when enabled
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
            Debug.LogWarning("[BleedOutMovement] Had to re-find CharacterController in OnEnable!");
        }
    }
    
    void Update()
    {
        if (!isActive) return;
        
        // CRITICAL: Safety check - ensure controller is still valid and enabled
        if (controller == null || !controller.enabled)
        {
            Debug.LogError("[BleedOutMovement] CharacterController is null or disabled! Deactivating.");
            DeactivateBleedOutMovement();
            return;
        }
        
        // Get keyboard input using YOUR Controls system
        float moveForward = 0f;
        float turnInput = 0f;
        
        // W/S = Move forward/backward
        if (Input.GetKey(Controls.MoveForward)) moveForward += 1f;
        if (Input.GetKey(Controls.MoveBackward)) moveForward -= 1f;
        
        // A/D = Turn left/right
        if (Input.GetKey(Controls.MoveLeft)) turnInput -= 1f;
        if (Input.GetKey(Controls.MoveRight)) turnInput += 1f;
        
        // TURN PLAYER with A/D
        if (Mathf.Abs(turnInput) > 0.01f)
        {
            float turnAmount = turnInput * turnSpeed * Time.unscaledDeltaTime;
            transform.Rotate(0f, turnAmount, 0f, Space.Self);
        }
        
        // Smooth forward/backward input
        Vector2 targetInput = new Vector2(0f, moveForward);
        currentInput = Vector2.SmoothDamp(currentInput, targetInput, ref inputVelocity, 1f / inputSmoothing, Mathf.Infinity, Time.unscaledDeltaTime);
        
        // Apply gravity (keep character grounded)
        if (controller.isGrounded)
        {
            verticalVelocity = -2f; // Small downward force to stay grounded
        }
        else
        {
            verticalVelocity += gravity * Time.unscaledDeltaTime;
        }
        
        // Calculate movement direction (forward/backward only, no strafing)
        Vector3 moveDirection = transform.forward * currentInput.y * crawlSpeed;
        
        // Add vertical velocity
        moveDirection.y = verticalVelocity;
        
        // Move character
        controller.Move(moveDirection * Time.unscaledDeltaTime);
    }
    
    /// <summary>
    /// Calculate movement direction RELATIVE TO CAMERA (third-person)
    /// W = Forward relative to camera view, S = Back, A = Left, D = Right
    /// </summary>
    private Vector3 CalculateCameraRelativeMovement()
    {
        Vector3 forward, right;
        
        if (bleedOutCamera != null)
        {
            // Get camera's forward and right directions (flatten to ground plane)
            forward = bleedOutCamera.transform.forward;
            forward.y = 0f;
            forward.Normalize();
            
            right = bleedOutCamera.transform.right;
            right.y = 0f;
            right.Normalize();
        }
        else
        {
            // Fallback to world-space if no camera
            forward = Vector3.forward;
            right = Vector3.right;
        }
        
        // Calculate movement direction relative to camera
        Vector3 moveDirection = (forward * currentInput.y + right * currentInput.x).normalized * crawlSpeed;
        
        return moveDirection;
    }
    
    /// <summary>
    /// Activate bleed out movement (called externally by DeathCameraController)
    /// CRITICAL: Takes EXCLUSIVE ownership of CharacterController
    /// </summary>
    public void ActivateBleedOutMovement(Camera thirdPersonCamera)
    {
        if (isActive)
        {
            Debug.LogWarning("[BleedOutMovement] Already active!");
            return;
        }
        
        // CRITICAL: Take exclusive ownership of CharacterController
        if (controller != null)
        {
            _characterControllerWasEnabled = controller.enabled;
            controller.enabled = true; // Force enable for bleeding out
            _hasCharacterControllerOwnership = true;
            Debug.Log($"[BleedOutMovement] 🔒 TOOK CharacterController ownership (was {_characterControllerWasEnabled})");
        }
        else
        {
            Debug.LogError("[BleedOutMovement] CharacterController is NULL! Cannot activate!");
            return;
        }
        
        bleedOutCamera = thirdPersonCamera;
        isActive = true;
        enabled = true;
        
        // Reset state
        currentInput = Vector2.zero;
        inputVelocity = Vector2.zero;
        verticalVelocity = 0f;
        
        Debug.Log("[BleedOutMovement] ✅ ACTIVATED - Keyboard-only crawl movement enabled");
    }
    
    /// <summary>
    /// Deactivate bleed out movement (called externally on revival)
    /// CRITICAL: Releases CharacterController ownership
    /// </summary>
    public void DeactivateBleedOutMovement()
    {
        if (!isActive)
        {
            return;
        }
        
        // CRITICAL: Release CharacterController ownership and restore previous state
        if (_hasCharacterControllerOwnership && controller != null)
        {
            controller.enabled = _characterControllerWasEnabled;
            _hasCharacterControllerOwnership = false;
            Debug.Log($"[BleedOutMovement] 🔓 RELEASED CharacterController ownership (restored to {_characterControllerWasEnabled})");
        }
        
        isActive = false;
        enabled = false;
        
        // Reset state
        currentInput = Vector2.zero;
        inputVelocity = Vector2.zero;
        verticalVelocity = 0f;
        
        Debug.Log("[BleedOutMovement] ✅ DEACTIVATED - Keyboard crawl movement disabled");
    }
    
    /// <summary>
    /// Check if currently active
    /// </summary>
    public bool IsActive()
    {
        return isActive;
    }
}
