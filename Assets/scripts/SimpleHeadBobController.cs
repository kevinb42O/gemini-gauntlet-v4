using UnityEngine;

/// <summary>
/// Simple head bob controller that syncs perfectly with PlayerFootstepController
/// Clean, simple, and effective - just vertical bob based on movement speed
/// 
/// SETUP INSTRUCTIONS:
/// 1. Add this component to your CAMERA GameObject (the child camera object, not the player)
/// 2. References will auto-find, or drag in AAAMovementController manually
/// 3. Adjust bobIntensity to taste (0.05 = subtle, 0.15 = noticeable)
/// 4. Enable showDebugInfo to verify it's working
/// </summary>
[AddComponentMenu("Gemini Gauntlet/Camera/Simple Head Bob Controller")]
public class SimpleHeadBobController : MonoBehaviour
{
    [Header("Head Bob Settings")]
    [Tooltip("Enable/disable head bob effect")]
    [SerializeField] private bool enableHeadBob = true;
    
    [Tooltip("Vertical bob intensity - how much camera moves up/down")]
    [SerializeField] private float bobIntensity = 0.05f;
    
    [Tooltip("How smoothly the bob transitions (higher = smoother)")]
    [SerializeField] private float bobSmoothness = 10f;
    
    [Header("Speed-Based Timing")]
    [Tooltip("Bob frequency at minimum speed (bobs per second)")]
    [SerializeField] private float minBobFrequency = 1.0f;
    
    [Tooltip("Bob frequency at maximum speed (bobs per second)")]
    [SerializeField] private float maxBobFrequency = 2.5f;
    
    [Tooltip("Speed at which bob reaches maximum frequency")]
    [SerializeField] private float maxSpeedForBob = 1485f; // Sprint speed
    
    [Tooltip("Minimum speed to start bobbing")]
    [SerializeField] private float minSpeedForBob = 50f;
    
    [Header("References (Auto-found if null)")]
    [Tooltip("Movement controller reference")]
    [SerializeField] private AAAMovementController movementController;
    
    [Tooltip("Crouch system reference (to disable bob during slides)")]
    [SerializeField] private CleanAAACrouch crouchSystem;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // Private state
    private Vector3 baseLocalPosition;
    private float bobTimer = 0f;
    private float currentBobOffset = 0f;
    private float currentBobFrequency = 1.5f;
    
    void Start()
    {
        // Store the base position
        baseLocalPosition = transform.localPosition;
        
        // Auto-find references if not assigned
        if (movementController == null)
        {
            movementController = GetComponentInParent<AAAMovementController>();
            if (movementController == null)
            {
                // Try to find in player
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    movementController = player.GetComponent<AAAMovementController>();
                }
            }
        }
        
        if (crouchSystem == null)
        {
            crouchSystem = GetComponentInParent<CleanAAACrouch>();
            if (crouchSystem == null)
            {
                // Try to find in player
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    crouchSystem = player.GetComponent<CleanAAACrouch>();
                }
            }
        }
        
        if (movementController == null)
        {
            Debug.LogError("[SimpleHeadBobController] ❌ AAAMovementController not found! Head bob will NOT work. Please assign it manually in the inspector.");
        }
        else
        {
            Debug.Log($"[SimpleHeadBobController] ✅ Found AAAMovementController. Head bob is ready!");
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[SimpleHeadBobController] Debug enabled. Camera: {gameObject.name}, Base Position: {baseLocalPosition}");
        }
    }
    
    [ContextMenu("Test Head Bob (Enable Debug)")]
    private void TestHeadBob()
    {
        showDebugInfo = true;
        Debug.Log("[SimpleHeadBobController] ✅ Debug enabled. Move around to see head bob info in console.");
    }
    
    void LateUpdate()
    {
        if (!enableHeadBob || movementController == null)
        {
            // Smoothly return to base position
            currentBobOffset = Mathf.Lerp(currentBobOffset, 0f, bobSmoothness * Time.deltaTime);
            ApplyBobOffset();
            return;
        }
        
        // Check if sliding/diving - disable bob during these states
        if (crouchSystem != null && (crouchSystem.IsSliding || crouchSystem.IsDiving || crouchSystem.IsDiveProne))
        {
            currentBobOffset = Mathf.Lerp(currentBobOffset, 0f, bobSmoothness * 2f * Time.deltaTime);
            ApplyBobOffset();
            return;
        }
        
        // Get movement state
        bool isGrounded = movementController.IsGrounded;
        float currentSpeed = movementController.CurrentSpeed;
        bool isMoving = currentSpeed > minSpeedForBob;
        
        if (isGrounded && isMoving)
        {
            // Calculate bob frequency based on speed (matches PlayerFootstepController logic)
            float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeedForBob);
            float targetFrequency = Mathf.Lerp(minBobFrequency, maxBobFrequency, speedRatio);
            
            // Smoothly transition frequency
            currentBobFrequency = Mathf.Lerp(currentBobFrequency, targetFrequency, 8f * Time.deltaTime);
            
            // Increment bob timer based on current frequency
            bobTimer += Time.deltaTime * currentBobFrequency;
            
            // Calculate vertical bob using smooth sine wave
            // bobTimer is in "bobs" so we multiply by 2π to get full sine cycle per bob
            float bobPhase = bobTimer * Mathf.PI * 2f;
            float targetBobOffset = Mathf.Sin(bobPhase) * bobIntensity;
            
            // Smooth the bob for natural feel
            currentBobOffset = Mathf.Lerp(currentBobOffset, targetBobOffset, bobSmoothness * Time.deltaTime);
            
            if (showDebugInfo && Time.frameCount % 60 == 0) // Log every 60 frames
            {
                Debug.Log($"[HeadBob] Speed: {currentSpeed:F1} | Freq: {currentBobFrequency:F2} Hz | Offset: {currentBobOffset:F3}");
            }
        }
        else
        {
            // Not grounded or not moving - smoothly return to center
            currentBobOffset = Mathf.Lerp(currentBobOffset, 0f, bobSmoothness * Time.deltaTime);
            
            // Reset timer when not moving
            if (!isMoving)
            {
                bobTimer = 0f;
            }
        }
        
        ApplyBobOffset();
    }
    
    /// <summary>
    /// Apply the bob offset to camera position
    /// </summary>
    private void ApplyBobOffset()
    {
        // Apply vertical offset only (Y axis)
        Vector3 newPosition = baseLocalPosition;
        newPosition.y += currentBobOffset;
        transform.localPosition = newPosition;
    }
    
    /// <summary>
    /// Update the base position (useful if camera height changes, e.g., from crouch system)
    /// </summary>
    public void UpdateBasePosition(Vector3 newBasePosition)
    {
        baseLocalPosition = newBasePosition;
    }
    
    /// <summary>
    /// Enable or disable head bob at runtime
    /// </summary>
    public void SetHeadBobEnabled(bool enabled)
    {
        enableHeadBob = enabled;
    }
    
    /// <summary>
    /// Set bob intensity at runtime
    /// </summary>
    public void SetBobIntensity(float intensity)
    {
        bobIntensity = Mathf.Max(0f, intensity);
    }
    
    /// <summary>
    /// Get current bob frequency (for debugging)
    /// </summary>
    public float GetCurrentBobFrequency()
    {
        return currentBobFrequency;
    }
}
