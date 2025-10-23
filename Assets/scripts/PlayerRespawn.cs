using UnityEngine;
using System.Reflection;

/// <summary>
/// Comprehensive player respawn system that resets ALL player state for testing.
/// Resets position, rotation, velocity, camera, energy, health, and all other systems.
/// Attach to Player GameObject.
/// </summary>
public class PlayerRespawn : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("The spawn position to reset to. If not set, uses the player's starting position.")]
    public Transform spawnPoint;

    [Header("Debug")]
    [Tooltip("Show detailed reset logs")]
    [SerializeField] private bool showDebugLogs = true;

    // Initial state storage
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    
    // Physics components
    private Rigidbody rb;
    private CharacterController characterController;
    
    // Camera system
    private AAACameraController cameraController;
    private Transform cameraTransform;
    private Vector2 initialCameraRotation; // X = pitch, Y = yaw
    
    // Movement system
    private AAAMovementController movementController;
    
    // Energy and health systems
    private PlayerEnergySystem energySystem;
    private PlayerHealth playerHealth;
    
    // Crouch/slide system
    private CleanAAACrouch crouchController;
    
    // Animation systems
    private LayeredHandAnimationController layeredHandAnimationController;
    private PlayerAnimationStateManager animationStateManager;

    private void Awake()
    {
        // Store initial position/rotation as fallback
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        // Get physics components
        rb = GetComponent<Rigidbody>();
        characterController = GetComponent<CharacterController>();
        
        // Get camera components
        cameraController = GetComponentInChildren<AAACameraController>();
        if (cameraController != null)
        {
            cameraTransform = cameraController.transform;
            // Store initial camera rotation (should be 0,0 typically)
            initialCameraRotation = new Vector2(cameraTransform.localEulerAngles.x, transform.eulerAngles.y);
        }
        
        // Get movement and state systems
        movementController = GetComponent<AAAMovementController>();
        energySystem = GetComponent<PlayerEnergySystem>();
        playerHealth = GetComponent<PlayerHealth>();
        crouchController = GetComponent<CleanAAACrouch>();
        
        // Get animation systems
        layeredHandAnimationController = GetComponent<LayeredHandAnimationController>();
        animationStateManager = GetComponent<PlayerAnimationStateManager>();
    }

    private void Update()
    {
        // Check for ENTER key press
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            RespawnPlayer();
        }
    }

    private void RespawnPlayer()
    {
        Vector3 targetPosition = spawnPoint != null ? spawnPoint.position : initialPosition;
        Quaternion targetRotation = spawnPoint != null ? spawnPoint.rotation : initialRotation;

        if (showDebugLogs)
            Debug.Log($"[PlayerRespawn] === FULL PLAYER RESET INITIATED ===");

        // ========== STEP 1: RESET PHYSICS ==========
        ResetPhysics(targetPosition, targetRotation);
        
        // ========== STEP 2: RESET CAMERA ==========
        ResetCamera();
        
        // ========== STEP 3: RESET MOVEMENT CONTROLLER ==========
        ResetMovementController();
        
        // ========== STEP 4: RESET ENERGY SYSTEM ==========
        ResetEnergySystem();
        
        // ========== STEP 5: RESET HEALTH SYSTEM ==========
        ResetHealthSystem();
        
        // ========== STEP 6: RESET CROUCH/SLIDE SYSTEM ==========
        ResetCrouchSystem();
        
        // ========== STEP 7: RESET ANIMATION SYSTEMS ==========
        ResetAnimationSystems();

        if (showDebugLogs)
            Debug.Log($"[PlayerRespawn] === FULL RESET COMPLETE - Fresh start at {targetPosition} ===");
    }

    private void ResetPhysics(Vector3 targetPosition, Quaternion targetRotation)
    {
        // Disable CharacterController temporarily if it exists
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        // Reset position and rotation
        transform.position = targetPosition;
        transform.rotation = targetRotation;

        // Reset velocity if Rigidbody exists
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep(); // Force physics to settle
            rb.WakeUp(); // Wake it back up fresh
        }

        // Re-enable CharacterController
        if (characterController != null)
        {
            characterController.enabled = true;
        }

        if (showDebugLogs)
            Debug.Log($"[PlayerRespawn] Physics reset: Position={targetPosition}, Rotation={targetRotation}");
    }

    private void ResetCamera()
    {
        if (cameraController == null) return;

        // Reset camera rotation to initial state using reflection to access private fields
        System.Type cameraType = cameraController.GetType();
        
        // Reset look rotation (pitch/yaw)
        FieldInfo currentLookField = cameraType.GetField("currentLook", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo targetLookField = cameraType.GetField("targetLook", BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (currentLookField != null && targetLookField != null)
        {
            currentLookField.SetValue(cameraController, initialCameraRotation);
            targetLookField.SetValue(cameraController, initialCameraRotation);
        }
        
        // Reset camera local position and rotation
        if (cameraTransform != null)
        {
            cameraTransform.localPosition = Vector3.zero;
            cameraTransform.localRotation = Quaternion.Euler(initialCameraRotation.x, 0f, 0f);
        }
        
        // Reset player body rotation
        transform.rotation = Quaternion.Euler(0f, initialCameraRotation.y, 0f);
        
        // Reset all camera effects using reflection
        ResetCameraField(cameraType, "shakeOffset", Vector3.zero);
        ResetCameraField(cameraType, "currentBeamShake", Vector3.zero);
        ResetCameraField(cameraType, "currentShotgunShake", Vector3.zero);
        ResetCameraField(cameraType, "traumaShakeOffset", Vector3.zero);
        ResetCameraField(cameraType, "currentTrauma", 0f);
        ResetCameraField(cameraType, "currentTilt", 0f);
        ResetCameraField(cameraType, "targetTilt", 0f);
        ResetCameraField(cameraType, "wallJumpTiltAmount", 0f);
        ResetCameraField(cameraType, "wallJumpTiltTarget", 0f);
        ResetCameraField(cameraType, "dynamicWallTilt", 0f);
        ResetCameraField(cameraType, "dynamicWallTiltTarget", 0f);
        ResetCameraField(cameraType, "headBobOffset", Vector3.zero);
        ResetCameraField(cameraType, "landingCompressionOffset", 0f);
        ResetCameraField(cameraType, "landingTiltOffset", 0f);
        ResetCameraField(cameraType, "isFreestyleModeActive", false);
        ResetCameraField(cameraType, "freestyleRotation", Quaternion.identity);
        
        // Reset FOV
        ResetCameraField(cameraType, "currentFOV", 100f);
        ResetCameraField(cameraType, "targetFOV", 100f);

        if (showDebugLogs)
            Debug.Log($"[PlayerRespawn] Camera reset: Rotation={initialCameraRotation}, all effects cleared");
    }

    private void ResetCameraField(System.Type cameraType, string fieldName, object value)
    {
        FieldInfo field = cameraType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(cameraController, value);
        }
    }

    private void ResetMovementController()
    {
        if (movementController == null) return;

        // Use reflection to reset internal movement state
        System.Type movementType = movementController.GetType();
        
        // Reset velocity and movement vectors
        FieldInfo velocityField = movementType.GetField("velocity", BindingFlags.NonPublic | BindingFlags.Instance);
        if (velocityField != null)
            velocityField.SetValue(movementController, Vector3.zero);
        
        // Reset falling state
        FieldInfo isFallingField = movementType.GetField("isFalling", BindingFlags.NonPublic | BindingFlags.Instance);
        if (isFallingField != null)
            isFallingField.SetValue(movementController, false);
        
        // Reset jump states
        FieldInfo hasDoubleJumpedField = movementType.GetField("hasDoubleJumped", BindingFlags.NonPublic | BindingFlags.Instance);
        if (hasDoubleJumpedField != null)
            hasDoubleJumpedField.SetValue(movementController, false);
        
        // Reset dash state
        FieldInfo isDashingField = movementType.GetField("isDashing", BindingFlags.NonPublic | BindingFlags.Instance);
        if (isDashingField != null)
            isDashingField.SetValue(movementController, false);

        if (showDebugLogs)
            Debug.Log($"[PlayerRespawn] Movement controller reset: All velocities and states cleared");
    }

    private void ResetEnergySystem()
    {
        if (energySystem == null) return;

        // Call public method to restore full energy if it exists
        MethodInfo restoreMethod = energySystem.GetType().GetMethod("RestoreEnergy", BindingFlags.Public | BindingFlags.Instance);
        if (restoreMethod != null)
        {
            // Restore full energy (pass large value)
            restoreMethod.Invoke(energySystem, new object[] { 9999f });
        }

        if (showDebugLogs)
            Debug.Log($"[PlayerRespawn] Energy system reset: Full energy restored");
    }

    private void ResetHealthSystem()
    {
        if (playerHealth == null) return;

        // Use reflection to reset health to max
        System.Type healthType = playerHealth.GetType();
        
        FieldInfo currentHealthField = healthType.GetField("currentHealth", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo maxHealthField = healthType.GetField("maxHealth", BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (currentHealthField != null && maxHealthField != null)
        {
            float maxHealth = (float)maxHealthField.GetValue(playerHealth);
            currentHealthField.SetValue(playerHealth, maxHealth);
        }
        
        // Reset bleeding out state if exists
        FieldInfo isBleedingOutField = healthType.GetField("isBleedingOut", BindingFlags.NonPublic | BindingFlags.Instance);
        if (isBleedingOutField != null)
            isBleedingOutField.SetValue(playerHealth, false);

        if (showDebugLogs)
            Debug.Log($"[PlayerRespawn] Health system reset: Full health restored");
    }

    private void ResetCrouchSystem()
    {
        if (crouchController == null) return;

        // Use reflection to reset crouch/slide state
        System.Type crouchType = crouchController.GetType();
        
        FieldInfo isCrouchingField = crouchType.GetField("isCrouching", BindingFlags.NonPublic | BindingFlags.Instance);
        if (isCrouchingField != null)
            isCrouchingField.SetValue(crouchController, false);
        
        FieldInfo isSlidingField = crouchType.GetField("isSliding", BindingFlags.NonPublic | BindingFlags.Instance);
        if (isSlidingField != null)
            isSlidingField.SetValue(crouchController, false);

        if (showDebugLogs)
            Debug.Log($"[PlayerRespawn] Crouch system reset: Standing position restored");
    }

    private void ResetAnimationSystems()
    {
        // Reset layered hand animation controller if it exists
        if (layeredHandAnimationController != null)
        {
            // Set all hands to idle movement state
            MethodInfo setMovementMethod = layeredHandAnimationController.GetType().GetMethod("SetAllHandsMovementState", BindingFlags.Public | BindingFlags.Instance);
            if (setMovementMethod != null)
            {
                setMovementMethod.Invoke(layeredHandAnimationController, new object[] { 0 }); // 0 = Idle
            }
        }
        
        // Reset animation state manager if it exists
        if (animationStateManager != null)
        {
            // Force unlock all hands
            MethodInfo unlockMethod = animationStateManager.GetType().GetMethod("ForceUnlockAllHands", BindingFlags.Public | BindingFlags.Instance);
            if (unlockMethod != null)
            {
                unlockMethod.Invoke(animationStateManager, null);
            }
        }

        if (showDebugLogs)
            Debug.Log($"[PlayerRespawn] Animation systems reset: All hands idle and unlocked");
    }

    /// <summary>
    /// Public method to set a new spawn point at runtime
    /// </summary>
    public void SetSpawnPoint(Transform newSpawnPoint)
    {
        spawnPoint = newSpawnPoint;
        Debug.Log($"[PlayerRespawn] Spawn point updated to: {newSpawnPoint.position}");
    }

    /// <summary>
    /// Public method to manually trigger respawn
    /// </summary>
    public void TriggerRespawn()
    {
        RespawnPlayer();
    }
}
