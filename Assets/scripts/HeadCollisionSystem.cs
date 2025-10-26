using UnityEngine;
using GeminiGauntlet.Audio;

/// <summary>
/// AAA HEAD COLLISION SYSTEM
/// 
/// Professional implementation of upward collision damage system.
/// Handles impacts when player hits ceiling/overhang during upward movement.
/// 
/// FEATURES:
/// - Velocity-based damage calculation
/// - Realistic bounce-back physics with energy loss
/// - Camera trauma integration
/// - Audio feedback system
/// - Blood splat visual feedback
/// - Anti-spam protection
/// 
/// INTEGRATION:
/// - Works with CharacterController.OnControllerColliderHit
/// - Uses existing PlayerHealth damage system
/// - Integrates with AAACameraController trauma
/// - Uses modern GameSounds audio system
/// - Respects grappling/aerial trick states
/// 
/// Author: Professional AAA Implementation
/// Date: 2025-10-25
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AAAMovementController))]
public class HeadCollisionSystem : MonoBehaviour
{
    [Header("=== 🎮 CONFIGURATION ===")]
    [Tooltip("ScriptableObject configuration (recommended). If null, system is disabled.")]
    [SerializeField] private HeadCollisionConfig config;
    
    
    [Header("=== 🔗 REFERENCES (Auto-Found if null) ===")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private AAAMovementController movementController;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private AAACameraController cameraController;
    [SerializeField] private AdvancedGrapplingSystem advancedGrapplingSystem;
    
    
    // Runtime state
    private float lastCollisionTime = -999f;
    private Vector3 lastVelocity = Vector3.zero;
    
    // Constants
    private const float CEILING_ANGLE_THRESHOLD = 60f; // Surfaces less than 60° from straight down = ceiling
    
    
    void Awake()
    {
        // Auto-find references if not assigned
        if (controller == null)
            controller = GetComponent<CharacterController>();
        
        if (movementController == null)
            movementController = GetComponent<AAAMovementController>();
        
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();
        
        if (cameraController == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
                cameraController = mainCam.GetComponent<AAACameraController>();
        }
        
        // Find rope/grappling systems (optional)
        if (advancedGrapplingSystem == null)
            advancedGrapplingSystem = GetComponent<AdvancedGrapplingSystem>();
        
        // Validate critical components
        if (controller == null)
        {
            Debug.LogError("[HEAD COLLISION] CharacterController not found! Disabling system.", this);
            enabled = false;
            return;
        }
        
        if (movementController == null)
        {
            Debug.LogError("[HEAD COLLISION] AAAMovementController not found! Disabling system.", this);
            enabled = false;
            return;
        }
        
        // Warn about missing optional components
        if (playerHealth == null)
            Debug.LogWarning("[HEAD COLLISION] PlayerHealth not found - damage disabled.", this);
        
        if (cameraController == null)
            Debug.LogWarning("[HEAD COLLISION] AAACameraController not found - camera trauma disabled.", this);
        
        // Check config
        if (config == null)
        {
            Debug.LogWarning("[HEAD COLLISION] No configuration assigned - system disabled. Create one via: Assets > Create > Game > Head Collision Configuration", this);
            enabled = false;
            return;
        }
        
        if (!config.enableHeadCollisionDamage)
        {
            Debug.Log("[HEAD COLLISION] System disabled in configuration.", this);
            enabled = false;
            return;
        }
        
        Debug.Log("[HEAD COLLISION] ✅ System initialized successfully");
    }
    
    void Update()
    {
        // Track velocity every frame for collision detection
        if (movementController != null)
        {
            lastVelocity = movementController.Velocity;
        }
    }
    
    /// <summary>
    /// CharacterController collision callback - detects head collisions
    /// </summary>
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (config == null || !config.enableHeadCollisionDamage || hit == null)
            return;
        
        // Check cooldown (anti-spam)
        if (Time.time - lastCollisionTime < config.collisionCooldown)
            return;
        
        // Only process if moving upward
        if (lastVelocity.y <= 0f)
            return;
        
        // Check if we hit a ceiling (surface pointing mostly downward)
        float angleFromDown = Vector3.Angle(hit.normal, Vector3.down);
        bool isCeiling = angleFromDown < CEILING_ANGLE_THRESHOLD;
        
        if (!isCeiling)
            return; // Not a ceiling collision
        
        // Calculate collision velocity (upward component)
        float upwardVelocity = Mathf.Abs(lastVelocity.y);
        
        // Check minimum velocity threshold
        if (upwardVelocity < config.minVelocityThreshold)
            return; // Impact too weak to register
        
        // Check collision angle (must be moving mostly upward)
        Vector3 velocityDirection = lastVelocity.normalized;
        float velocityAngleFromUp = Vector3.Angle(velocityDirection, Vector3.up);
        
        if (velocityAngleFromUp > config.headCollisionAngleThreshold)
            return; // Not moving upward enough (probably sliding along ceiling)
        
        // VALID HEAD COLLISION DETECTED!
        ProcessHeadCollision(hit, upwardVelocity);
        
        lastCollisionTime = Time.time;
    }
    
    /// <summary>
    /// Check if player is currently attached to rope/grapple
    /// </summary>
    private bool IsAttachedToRope()
    {
        // Check AdvancedGrapplingSystem (uses direct property access)
        if (advancedGrapplingSystem != null && advancedGrapplingSystem.IsGrappling)
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Get rope anchor position (for directional bounce calculation)
    /// </summary>
    private Vector3 GetRopeAnchorPosition()
    {
        // Use AdvancedGrapplingSystem (direct property access)
        if (advancedGrapplingSystem != null && advancedGrapplingSystem.IsGrappling)
        {
            return advancedGrapplingSystem.GrappleAnchor;
        }
        
        return transform.position; // Fallback
    }
    
    /// <summary>
    /// Process a valid head collision with all effects
    /// </summary>
    private void ProcessHeadCollision(ControllerColliderHit hit, float collisionVelocity)
    {
        // Calculate severity
        float damage = config.CalculateDamage(collisionVelocity);
        float trauma = config.CalculateTrauma(collisionVelocity);
        float audioVolume = config.CalculateAudioVolume(collisionVelocity);
        float severityNormalized = config.GetSeverityNormalized(collisionVelocity);
        string severityName = config.GetSeverityName(collisionVelocity);
        
        // Debug logging
        if (config.showDebugLogs)
        {
            Debug.Log($"<color=yellow>💥 [HEAD COLLISION] {severityName} impact at {collisionVelocity:F0} units/s | Damage: {damage:F0} | Trauma: {trauma:F2}</color>");
        }
        
        // Apply damage
        if (damage > 0f && playerHealth != null)
        {
            playerHealth.TakeDamageBypassArmor(damage);
            
            // Blood splat effect for severe hits
            if (config.enableBloodSplat && severityNormalized >= config.bloodSplatThreshold)
            {
                playerHealth.TriggerDramaticBloodSplat(trauma);
            }
        }
        
        // Apply camera trauma
        if (config.enableCameraTrauma && trauma > 0f && cameraController != null)
        {
            cameraController.AddTrauma(trauma);
        }
        
        // Play audio
        if (config.enableAudio && audioVolume > 0f)
        {
            // Use fall damage sound (it's a good impact sound)
            GameSounds.PlayFallDamage(hit.point, audioVolume);
        }
        
        // Apply bounce-back physics
        ApplyBouncePhysics(hit.normal, collisionVelocity);
    }
    
    /// <summary>
    /// Apply realistic bounce-back physics when hitting ceiling
    /// ROPE MODE: Special handling for predictable, punishing response
    /// </summary>
    private void ApplyBouncePhysics(Vector3 surfaceNormal, float collisionVelocity)
    {
        if (movementController == null)
            return;
        
        // Check if attached to rope
        bool isRoped = IsAttachedToRope();
        
        if (isRoped && config.enableRopeCollisionHandling)
        {
            // === ROPE COLLISION MODE - PREDICTABLE BOUNCE ===
            ApplyRopeBouncePhysics(surfaceNormal, collisionVelocity);
        }
        else
        {
            // === NORMAL COLLISION MODE ===
            ApplyNormalBouncePhysics(surfaceNormal, collisionVelocity);
        }
    }
    
    /// <summary>
    /// Normal bounce physics (free movement)
    /// </summary>
    private void ApplyNormalBouncePhysics(Vector3 surfaceNormal, float collisionVelocity)
    {
        // Get current velocity
        Vector3 currentVelocity = movementController.Velocity;
        
        // Calculate bounce-back velocity (reverse upward component with energy loss)
        float bounceSpeed = collisionVelocity * config.bounceCoefficient;
        
        // Clamp bounce speed to valid range
        bounceSpeed = Mathf.Clamp(bounceSpeed, config.minBounceVelocity, config.maxBounceVelocity);
        
        // Reverse the upward velocity component (bounce downward)
        currentVelocity.y = -bounceSpeed;
        
        // Dampen horizontal velocity (ceiling scrape effect)
        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        horizontalVelocity *= config.horizontalDampening;
        
        currentVelocity.x = horizontalVelocity.x;
        currentVelocity.z = horizontalVelocity.z;
        
        // Add small push away from surface (prevents sticking/jitter)
        Vector3 pushDirection = surfaceNormal.normalized;
        Vector3 pushVelocity = pushDirection * config.surfacePushForce;
        
        // Only apply push if it's mostly horizontal (don't interfere with bounce)
        if (Mathf.Abs(pushDirection.y) < 0.5f)
        {
            currentVelocity += pushVelocity;
        }
        
        // Apply modified velocity to movement controller
        movementController.SetVelocity(currentVelocity);
        
        if (config.showDebugLogs)
        {
            Debug.Log($"[HEAD COLLISION] Normal bounce applied - New velocity: {currentVelocity.magnitude:F1} units/s (Y: {currentVelocity.y:F1})");
        }
    }
    
    /// <summary>
    /// Rope-specific bounce physics - PREDICTABLE and PUNISHING
    /// Bounces player back toward rope anchor with reduced force
    /// </summary>
    private void ApplyRopeBouncePhysics(Vector3 surfaceNormal, float collisionVelocity)
    {
        // Get current velocity and rope anchor
        Vector3 currentVelocity = movementController.Velocity;
        Vector3 ropeAnchor = GetRopeAnchorPosition();
        
        // Calculate direction toward rope anchor
        Vector3 toAnchor = (ropeAnchor - transform.position).normalized;
        
        // Calculate pure reflection (bounce away from surface)
        Vector3 reflectionDirection = Vector3.Reflect(-toAnchor, surfaceNormal).normalized;
        
        // BLEND reflection with anchor direction for predictability
        // Higher ropeAnchorBias = more predictable (always bounces toward anchor)
        Vector3 bounceDirection = Vector3.Lerp(reflectionDirection, toAnchor, config.ropeAnchorBias).normalized;
        
        // Calculate bounce speed with REDUCED coefficient (softer bounce while roped)
        float bounceSpeed = collisionVelocity * config.ropeBounceCoefficient;
        
        // Clamp to rope-specific limits (lower max for control)
        bounceSpeed = Mathf.Clamp(bounceSpeed, config.minBounceVelocity, config.ropeMaxBounceVelocity);
        
        // Apply PENALTY for collision (lose speed as punishment)
        bounceSpeed *= (1f - config.ropeCollisionPenalty);
        
        // Create bounce velocity vector
        Vector3 bounceVelocity = bounceDirection * bounceSpeed;
        
        // Preserve some horizontal momentum (perpendicular to anchor direction)
        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        Vector3 horizontalAnchorDir = new Vector3(toAnchor.x, 0f, toAnchor.z).normalized;
        Vector3 tangentialVelocity = horizontalVelocity - horizontalAnchorDir * Vector3.Dot(horizontalVelocity, horizontalAnchorDir);
        
        // Apply EXTRA horizontal dampening while roped (more drag)
        tangentialVelocity *= config.horizontalDampening * config.ropeHorizontalDampeningMultiplier;
        
        // Combine bounce and tangential velocity
        Vector3 finalVelocity = bounceVelocity + tangentialVelocity;
        
        // Apply to movement controller
        movementController.SetVelocity(finalVelocity);
        
        if (config.showDebugLogs)
        {
            Debug.Log($"[HEAD COLLISION] 🪝 ROPE bounce applied - Direction bias: {config.ropeAnchorBias:F2} | Speed: {finalVelocity.magnitude:F1} units/s (Y: {finalVelocity.y:F1})");
            Debug.Log($"[HEAD COLLISION] Collision penalty: -{config.ropeCollisionPenalty * 100f:F0}% speed | Anchor at: {ropeAnchor}");
        }
        
        // Debug visualization
        if (config.showDebugRays)
        {
            Debug.DrawRay(transform.position, surfaceNormal * 100f, Color.red, 1f); // Surface normal
            Debug.DrawRay(transform.position, toAnchor * 100f, Color.yellow, 1f); // To anchor
            Debug.DrawRay(transform.position, bounceDirection * 100f, Color.cyan, 1f); // Blended bounce
            Debug.DrawRay(transform.position, finalVelocity.normalized * 100f, Color.green, 1f); // Final velocity
        }
    }
    
    /// <summary>
    /// Editor helper: Create default config asset
    /// </summary>
    [ContextMenu("Create Default Config")]
    private void CreateDefaultConfig()
    {
        #if UNITY_EDITOR
        HeadCollisionConfig newConfig = HeadCollisionConfig.CreateDefault();
        
        // Save as asset
        string path = $"Assets/HeadCollisionConfig_Default.asset";
        UnityEditor.AssetDatabase.CreateAsset(newConfig, path);
        UnityEditor.AssetDatabase.SaveAssets();
        
        // Assign to this component
        config = newConfig;
        
        Debug.Log($"[HEAD COLLISION] Created default config at: {path}");
        UnityEditor.Selection.activeObject = newConfig;
        #endif
    }
    
    /// <summary>
    /// Editor helper: Test head collision with simulated velocity
    /// </summary>
    [ContextMenu("Test Head Collision (Moderate)")]
    private void TestModerateCollision()
    {
        if (config == null)
        {
            Debug.LogWarning("[HEAD COLLISION] No config assigned - cannot test");
            return;
        }
        
        float testVelocity = config.moderateCollisionVelocity;
        float damage = config.CalculateDamage(testVelocity);
        float trauma = config.CalculateTrauma(testVelocity);
        string severity = config.GetSeverityName(testVelocity);
        
        Debug.Log($"<color=cyan>🧪 [HEAD COLLISION TEST] {severity} collision at {testVelocity:F0} units/s → Damage: {damage:F0} HP, Trauma: {trauma:F2}</color>");
    }
}
