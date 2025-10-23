using UnityEngine;
using GeminiGauntlet.Audio;

/// <summary>
/// 🦸 SUPERHERO LANDING SYSTEM 🦸
/// 
/// ⚠️ DEPRECATED - This system is being phased out in favor of the Unified Impact System
/// 
/// ════════════════════════════════════════════════════════════════════════════
/// ⚠️ DEPRECATION NOTICE - READ BEFORE USING ⚠️
/// ════════════════════════════════════════════════════════════════════════════
/// 
/// This system is DEPRECATED and will be removed in a future update.
/// It has been replaced by the UNIFIED IMPACT SYSTEM which provides:
/// 
/// ✅ Single source of truth (FallingDamageSystem)
/// ✅ Consistent thresholds across all systems
/// ✅ Impact-based superhero landing (not just tricks)
/// ✅ Event-driven architecture (ImpactEventBroadcaster)
/// ✅ Better performance (no duplicate tracking)
/// 
/// MIGRATION PATH:
/// 1. Remove this component from your player GameObject
/// 2. Ensure FallingDamageSystem is present
/// 3. Ensure AAACameraController is present
/// 4. The unified system handles superhero landing automatically via impact events
/// 
/// NEW ARCHITECTURE:
/// FallingDamageSystem → Calculates ImpactData → Broadcasts Event
///                                              ↓
///                                    AAACameraController listens
///                                    → Triggers superhero landing based on impact
/// 
/// WHY DEPRECATED:
/// - Duplicate fall tracking logic (same as FallingDamageSystem)
/// - No unique functionality (just visual effects)
/// - Inconsistent thresholds with other systems
/// - All functionality now in unified impact system
/// 
/// This component will log a deprecation warning on startup.
/// ════════════════════════════════════════════════════════════════════════════
/// 
/// ORIGINAL DESCRIPTION (for reference):
/// Your player has SUPERPOWERS - no fall damage!
/// Instead, they make EPIC LANDINGS with AOE effects!
/// 
/// The harder you fall (vertical distance only), the COOLER the landing effect!
/// Like Iron Man, Hulk, or any badass superhero landing!
/// 
/// Features:
/// - Tracks VERTICAL DISTANCE ONLY (world Y-axis)
/// - No damage - pure visual/audio feedback
/// - AOE effect spawned at feet position
/// - Scales effect intensity by fall height
/// - Smart detection (ignores tiny bumps)
/// </summary>
[System.Obsolete("This system is deprecated. Use the Unified Impact System (FallingDamageSystem + ImpactEventBroadcaster) instead.")]
[RequireComponent(typeof(CharacterController))]
public class SuperheroLandingSystem : MonoBehaviour
{
    [Header("=== LANDING TIERS (World Height) ===")]
    [Tooltip("Small jump - minimal effect")]
    [SerializeField] private float smallLandingHeight = 200f; // Small hop
    
    [Tooltip("Medium landing - noticeable effect")]
    [SerializeField] private float mediumLandingHeight = 500f; // Decent drop
    
    [Tooltip("Epic landing - dramatic effect")]
    [SerializeField] private float epicLandingHeight = 1000f; // Big fall
    
    [Tooltip("SUPERHERO landing - MAXIMUM EFFECT!")]
    [SerializeField] private float superheroLandingHeight = 2000f; // HUGE fall
    
    [Header("=== AOE LANDING EFFECTS (Child Objects) ===")]
    [Tooltip("Parent container - has all 4 child effects inside")]
    [SerializeField] private GameObject effectParent;
    
    [Tooltip("Small landing effect (child of parent)")]
    [SerializeField] private GameObject smallLandingEffect;
    
    [Tooltip("Medium landing effect (child of parent)")]
    [SerializeField] private GameObject mediumLandingEffect;
    
    [Tooltip("Epic landing effect (child of parent)")]
    [SerializeField] private GameObject epicLandingEffect;
    
    [Tooltip("SUPERHERO landing effect (child of parent)")]
    [SerializeField] private GameObject superheroLandingEffect;
    
    [Tooltip("Scale multiplier for effect intensity (0-1 = normal, >1 = exaggerated)")]
    [SerializeField] private float effectIntensityMultiplier = 1.0f;
    
    [Header("=== CAMERA EFFECTS ===")]
    [Tooltip("Enable camera shake on landing")]
    [SerializeField] private bool enableCameraShake = true;
    
    [Tooltip("Maximum camera trauma for superhero landings")]
    [SerializeField] private float maxCameraTrauma = 0.8f;
    
    [Header("=== ANTI-SPAM PROTECTION ===")]
    [Tooltip("Minimum air time to count as a real fall (prevents tiny bump spam)")]
    [SerializeField] private float minAirTimeForLanding = 0.3f;
    
    [Tooltip("Cooldown between landing detections")]
    [SerializeField] private float landingCooldown = 0.3f;
    
    // Component references
    private CharacterController controller;
    private AAAMovementController movementController;
    private AAACameraController cameraController;
    
    // Fall tracking
    private bool isFalling = false;
    private float highestWorldY = 0f; // WORLD Y POSITION - the key!
    private float fallStartTime = 0f;
    private bool wasGroundedLastFrame = true;
    private float lastLandingProcessedTime = -999f;
    
    // Platform detection (don't trigger on elevators)
    private ElevatorController currentElevator = null;
    private bool isOnPlatform = false;
    private ElevatorController[] cachedElevators = null; // CACHE THIS!
    
    void Awake()
    {
        // ⚠️ DEPRECATION WARNING
        Debug.LogWarning("⚠️ [DEPRECATED] SuperheroLandingSystem is deprecated! " +
                        "Migrate to the Unified Impact System (FallingDamageSystem + ImpactEventBroadcaster). " +
                        "This component will be removed in a future update. " +
                        "See class documentation for migration instructions.");
        
        controller = GetComponent<CharacterController>();
        movementController = GetComponent<AAAMovementController>();
        
        // Find camera controller for trauma effects
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            cameraController = mainCam.GetComponent<AAACameraController>();
        }
        
        if (controller == null)
        {
            Debug.LogError("[SuperheroLandingSystem] CharacterController not found!");
        }
        
        // Initialize
        fallStartTime = Time.time;
        lastLandingProcessedTime = Time.time - landingCooldown;
        
        // CACHE ELEVATORS ONCE - NOT EVERY FRAME!
        cachedElevators = FindObjectsOfType<ElevatorController>();
        
        // Make sure all child effects are disabled initially
        if (smallLandingEffect != null) smallLandingEffect.SetActive(false);
        if (mediumLandingEffect != null) mediumLandingEffect.SetActive(false);
        if (epicLandingEffect != null) epicLandingEffect.SetActive(false);
        if (superheroLandingEffect != null) superheroLandingEffect.SetActive(false);
    }
    
    void Update()
    {
        if (controller == null) return;
        
        // Get grounded state
        bool isGrounded = movementController != null ? movementController.IsGrounded : controller.isGrounded;
        
        // ONLY check platform if we're actually falling or just landed!
        if (isFalling || (!wasGroundedLastFrame && isGrounded))
        {
            DetectPlatform();
        }
        
        // Skip fall detection if on platform
        if (!isOnPlatform)
        {
            // Started falling
            if (wasGroundedLastFrame && !isGrounded)
            {
                StartFall();
            }
            
            // Falling - track highest point
            if (isFalling && !isGrounded)
            {
                UpdateFall();
            }
            
            // Landed!
            if (!wasGroundedLastFrame && isGrounded)
            {
                EndFall();
            }
        }
        else
        {
            // On platform - cancel any active fall
            if (isFalling)
            {
                ResetFallTracking();
                // Debug removed for performance
            }
        }
        
        wasGroundedLastFrame = isGrounded;
    }
    
    private void StartFall()
    {
        isFalling = true;
        highestWorldY = transform.position.y; // Start tracking from current WORLD Y
        fallStartTime = Time.time;
        
        // Debug removed for performance
    }
    
    private void UpdateFall()
    {
        // Track the HIGHEST WORLD Y POSITION during the fall
        // This is the key - we only care about vertical distance!
        float currentWorldY = transform.position.y;
        if (currentWorldY > highestWorldY)
        {
            highestWorldY = currentWorldY;
        }
    }
    
    private void EndFall()
    {
        if (!isFalling) return;
        
        // Calculate air time
        float airTime = Time.time - fallStartTime;
        
        // Check cooldown (prevent spam)
        float timeSinceLastLanding = Time.time - lastLandingProcessedTime;
        if (timeSinceLastLanding < landingCooldown)
        {
            ResetFallTracking();
            return;
        }
        
        // Check minimum air time (ignore tiny bumps)
        if (airTime < minAirTimeForLanding)
        {
            ResetFallTracking();
            return;
        }
        
        // Mark landing processed
        lastLandingProcessedTime = Time.time;
        
        // Calculate VERTICAL DISTANCE FALLEN (world space Y)
        float currentWorldY = transform.position.y;
        float verticalDistanceFallen = highestWorldY - currentWorldY;
        
        // Debug removed for performance
        
        // Process landing effect based on height
        ProcessSuperheroLanding(verticalDistanceFallen);
        
        // Reset tracking
        ResetFallTracking();
    }
    
    private void ProcessSuperheroLanding(float verticalDistance)
    {
        // Determine landing tier
        string landingTier = "None";
        float intensity = 0f;
        float traumaIntensity = 0f;
        GameObject effectToTrigger = null;
        
        if (verticalDistance >= superheroLandingHeight)
        {
            // 🦸 SUPERHERO LANDING! 🦸
            landingTier = "SUPERHERO";
            intensity = 1.0f;
            traumaIntensity = maxCameraTrauma;
            effectToTrigger = superheroLandingEffect;
        }
        else if (verticalDistance >= epicLandingHeight)
        {
            // Epic landing
            landingTier = "EPIC";
            float t = Mathf.InverseLerp(epicLandingHeight, superheroLandingHeight, verticalDistance);
            intensity = Mathf.Lerp(0.6f, 1.0f, t);
            traumaIntensity = Mathf.Lerp(0.5f, maxCameraTrauma, t);
            effectToTrigger = epicLandingEffect;
        }
        else if (verticalDistance >= mediumLandingHeight)
        {
            // Medium landing
            landingTier = "MEDIUM";
            float t = Mathf.InverseLerp(mediumLandingHeight, epicLandingHeight, verticalDistance);
            intensity = Mathf.Lerp(0.3f, 0.6f, t);
            traumaIntensity = Mathf.Lerp(0.2f, 0.5f, t);
            effectToTrigger = mediumLandingEffect;
        }
        else if (verticalDistance >= smallLandingHeight)
        {
            // Small landing
            landingTier = "SMALL";
            float t = Mathf.InverseLerp(smallLandingHeight, mediumLandingHeight, verticalDistance);
            intensity = Mathf.Lerp(0.1f, 0.3f, t);
            traumaIntensity = Mathf.Lerp(0.05f, 0.2f, t);
            effectToTrigger = smallLandingEffect;
        }
        else
        {
            // Too small - no effect (debug removed for performance)
            return;
        }
        
        // Apply intensity multiplier
        intensity *= effectIntensityMultiplier;
        
        // Debug removed for performance
        
        // Trigger the appropriate effect
        TriggerLandingEffect(effectToTrigger, intensity);
        
        // Camera shake
        if (enableCameraShake && cameraController != null)
        {
            cameraController.AddTrauma(traumaIntensity);
        }
        
        // Play landing sound (using existing fall damage sound scaled by intensity)
        GameSounds.PlayFallDamage(transform.position, intensity);
    }
    
    private void TriggerLandingEffect(GameObject effectToTrigger, float intensity)
    {
        if (effectToTrigger != null)
        {
            // Simple: just toggle the GameObject to restart particles
            effectToTrigger.SetActive(false);
            effectToTrigger.SetActive(true);
            // Debug removed for performance
        }
        // Removed error log for performance
    }
    
    private void ResetFallTracking()
    {
        isFalling = false;
        highestWorldY = 0f;
        fallStartTime = 0f;
    }
    
    private void DetectPlatform()
    {
        // Fast path: check if still on current elevator
        if (currentElevator != null)
        {
            if (!currentElevator.IsPlayerInElevator(controller))
            {
                currentElevator = null;
                isOnPlatform = false;
            }
            return;
        }
        
        // Use CACHED elevators - NO FindObjectsOfType every frame!
        if (cachedElevators != null && cachedElevators.Length > 0)
        {
            foreach (var elevator in cachedElevators)
            {
                if (elevator != null && elevator.IsPlayerInElevator(controller))
                {
                    currentElevator = elevator;
                    isOnPlatform = true;
                    return;
                }
            }
        }
        
        isOnPlatform = false;
    }
    
    /// <summary>
    /// Get current vertical fall distance (useful for UI)
    /// </summary>
    public float GetCurrentFallDistance()
    {
        if (!isFalling) return 0f;
        return highestWorldY - transform.position.y;
    }
    
    /// <summary>
    /// Check if currently falling
    /// </summary>
    public bool IsFalling()
    {
        return isFalling;
    }
    
    // Debug visualization (DISABLED - use Profiler instead for performance)
    /*
    void OnGUI()
    {
        if (!showDebugInfo || !isFalling) return;
        
        float currentFallDistance = GetCurrentFallDistance();
        string debugText = $"Superhero Landing System\n" +
                          $"Falling: YES\n" +
                          $"Highest Y: {highestWorldY:F1}\n" +
                          $"Current Y: {transform.position.y:F1}\n" +
                          $"Vertical Distance: {currentFallDistance:F1} units\n" +
                          $"Air Time: {(Time.time - fallStartTime):F2}s";
        
        GUI.Box(new Rect(10, 10, 300, 120), debugText);
    }
    */
}
