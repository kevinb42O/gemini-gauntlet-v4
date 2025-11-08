using UnityEngine;
using GeminiGauntlet.Audio;

/// <summary>
/// AAA Falling Damage System - Realistic, scaled damage with camera effects
/// Features: Scaled damage by height, high-speed collision damage, camera trauma, blood overlay
/// 
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FallingDamageSystem : MonoBehaviour
{
    [Header("=== AOE LANDING EFFECTS ===")]
    [Tooltip("Small landing effect GameObject (child of player)")]
    [SerializeField] private GameObject smallLandingEffect;
    [Tooltip("Medium landing effect GameObject")]
    [SerializeField] private GameObject mediumLandingEffect;
    [Tooltip("Epic landing effect GameObject")]
    [SerializeField] private GameObject epicLandingEffect;
    [Tooltip("Superhero landing effect GameObject")]
    [SerializeField] private GameObject superheroLandingEffect;
    
    [Header("=== SCALED FALL DAMAGE ===")]
    [Tooltip("Fall height where damage starts (1x player height)")]
    [SerializeField] private float minDamageFallHeight = 320f; // 1x player height - light damage
    [Tooltip("Fall height that deals moderate damage (2x player height)")]
    [SerializeField] private float moderateDamageFallHeight = 640f; // 2x player height - hurts
    [Tooltip("Fall height that deals severe damage (3x player height)")]
    [SerializeField] private float severeDamageFallHeight = 960f; // 3x player height - very dangerous
    [Tooltip("Fall height that is LETHAL (4x+ player height)")]
    [SerializeField] private float lethalFallHeight = 1280f; // 4x player height - instant death
    
    [Header("=== DAMAGE SCALING ===")]
    [Tooltip("Damage at minimum threshold")]
    [SerializeField] private float minFallDamage = 250f; // Light damage - survivable
    [Tooltip("Damage at moderate height")]
    [SerializeField] private float moderateFallDamage = 750f; // Moderate damage - hurts
    [Tooltip("Damage at severe height")]
    [SerializeField] private float severeFallDamage = 1500f; // Severe damage - very dangerous
    [Tooltip("Damage at lethal height (ensures death)")]
    [SerializeField] private float lethalFallDamage = 10000f; // Instant death
    [Tooltip("Damage scaling curve (distance to damage multiplier)")]
    [SerializeField] private AnimationCurve damageScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("=== HIGH-SPEED COLLISION DAMAGE ===")]
    [Tooltip("Enable damage from high-speed collisions (flying into walls)")]
    [SerializeField] private bool enableCollisionDamage = true;
    [Tooltip("Minimum collision speed to cause damage")]
    [SerializeField] private float minCollisionSpeed = 100f; // Fast collision starts damage
    [Tooltip("Collision speed that causes severe damage")]
    [SerializeField] private float severeCollisionSpeed = 200f; // Terminal velocity impact
    [Tooltip("Damage at minimum collision speed")]
    [SerializeField] private float minCollisionDamage = 200f;
    [Tooltip("Damage at severe collision speed")]
    [SerializeField] private float maxCollisionDamage = 2000f;
    [Tooltip("Cooldown between collision damage (prevents spam)")]
    [SerializeField] private float collisionDamageCooldown = 0.5f;
    
    [Header("=== ANTI-SPAM PROTECTION ===")]
    [SerializeField] private float minAirTimeForFallDetection = 1.0f; // Minimum airtime to count as a fall (prevents spam on tiny bumps)
    [SerializeField] private float landingCooldown = 0.5f; // Cooldown between landing detections (prevents jitter spam)
    
    [Header("=== VERTICAL FALLING WIND SOUND ===")]
    [SerializeField] private float verticalWindThreshold = 1500f; // Minimum downward speed to trigger falling wind
    [SerializeField] private float verticalWindMinVolume = 0.3f; // Volume at threshold speed
    [SerializeField] private float verticalWindMaxVolume = 1.0f; // Volume at max speed
    [SerializeField] private float verticalWindMaxSpeed = 3000f; // Speed for maximum volume (terminal velocity)
    [SerializeField] private float verticalWindHysteresis = 200f; // Hysteresis for downward speed
    [Tooltip("Cooldown after jump/landing before vertical wind sound can start")]
    [SerializeField] private float verticalWindJumpCooldown = 0.5f; // Half second cooldown after jump
    
    [Header("=== HORIZONTAL RUSHING WIND SOUND ===")]
    [SerializeField] private float horizontalWindThreshold = 3500f; // Minimum horizontal speed to trigger rushing wind
    [SerializeField] private float horizontalWindMinVolume = 0.3f; // Volume at threshold speed
    [SerializeField] private float horizontalWindMaxVolume = 1.0f; // Volume at max speed
    [SerializeField] private float horizontalWindMaxSpeed = 18000f; // Max horizontal speed for volume scaling
    [SerializeField] private float horizontalWindHysteresis = 700f; // Hysteresis for horizontal speed
    
    [Header("=== 🎯 UNIFIED IMPACT SYSTEM ===")]
    [Tooltip("Base camera compression amount for landing impacts (used for calculation)")]
    [SerializeField] private float landingCompressionAmount = 80f; // Matches AAACameraController default
    
    [Header("References")]
    private CharacterController controller;
    private PlayerHealth playerHealth;
    private AAAMovementController movementController;
    private AAACameraController cameraController;
    
    // Fall tracking
    private bool isFalling = false;
    private float fallStartHeight = 0f;
    private float highestPointDuringFall = 0f;
    private bool wasGroundedLastFrame = true;
    private float fallStartTime = 0f;
    
    // Vertical wind sound tracking
    private bool isVerticalWindPlaying = false;
    private SoundHandle verticalWindHandle = SoundHandle.Invalid;
    private float verticalWindStartTime = 0f;
    private float verticalWindStopTime = 0f;
    
    // Horizontal wind sound tracking
    private bool isHorizontalWindPlaying = false;
    private SoundHandle horizontalWindHandle = SoundHandle.Invalid;
    private float horizontalWindStartTime = 0f;
    private float horizontalWindStopTime = 0f;
    
    private float lastLandingProcessedTime = -999f; // Anti-spam cooldown
    private float lastJumpOrLandingTime = -999f; // Track last jump/landing for wind sound cooldown
    private Vector3 lastFrameVelocity = Vector3.zero; // Track velocity for sudden stop detection
    
    // Collision damage tracking
    private float lastCollisionDamageTime = -999f;
    private Vector3 lastVelocity = Vector3.zero;
    
    // CRITICAL: Platform movement tracking
    private ElevatorController _currentElevator = null;
    private bool _isOnPlatform = false;
    
    // CRITICAL FIX: Slide tracking to prevent false fall damage
    private CleanAAACrouch _crouchController = null;
    
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerHealth = GetComponent<PlayerHealth>();
        movementController = GetComponent<AAAMovementController>();
        _crouchController = GetComponent<CleanAAACrouch>();
        
        // Find camera controller for trauma effects
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            cameraController = mainCam.GetComponent<AAACameraController>();
            if (cameraController == null)
            {
                Debug.LogWarning("[FallingDamageSystem] AAACameraController not found! Camera shake disabled.");
            }
        }
        
        if (controller == null)
        {
            Debug.LogError("[FallingDamageSystem] CharacterController not found!");
        }
        
        if (playerHealth == null)
        {
            Debug.LogError("[FallingDamageSystem] PlayerHealth not found!");
        }
        
        if (movementController == null)
        {
            Debug.LogWarning("[FallingDamageSystem] AAAMovementController not found - fall tracking may be less accurate");
        }
        
        // CRITICAL FIX: Initialize fallStartTime to prevent huge air time calculations
        // If player starts on ground, we need a valid timestamp
        fallStartTime = Time.time;
        lastLandingProcessedTime = Time.time - landingCooldown; // Allow first landing immediately
        lastCollisionDamageTime = Time.time - collisionDamageCooldown; // Allow first collision damage immediately
        
        // Disable landing effects initially
        if (smallLandingEffect != null) smallLandingEffect.SetActive(false);
        if (mediumLandingEffect != null) mediumLandingEffect.SetActive(false);
        if (epicLandingEffect != null) epicLandingEffect.SetActive(false);
        if (superheroLandingEffect != null) superheroLandingEffect.SetActive(false);
    }
    
    void Update()
    {
        if (controller == null || playerHealth == null) return;
        
        // CRITICAL: Check if on moving platform (elevator)
        DetectPlatform();
        
        // Track velocity for collision detection
        if (movementController != null)
        {
            lastVelocity = movementController.Velocity;
        }
        
        // Update wind sound based on current speed (works in ANY state)
        UpdateWindSound();
        
        // Check if we're grounded using the movement controller if available
        bool isGrounded = movementController != null ? movementController.IsGrounded : controller.isGrounded;
        
        // CRITICAL FIX: Skip fall detection if sliding!
        // Sliding applies high downward velocity for ground adhesion - this is NOT falling damage!
        bool isSliding = _crouchController != null && _crouchController.IsSliding;
        
        // CRITICAL: Skip fall detection if on moving platform!
        if (!_isOnPlatform && !isSliding)
        {
            // Detect when we leave the ground (start falling)
            if (wasGroundedLastFrame && !isGrounded)
            {
                StartFall();
            }
            
            // Track fall progress
            if (isFalling && !isGrounded)
            {
                TrackFallProgress();
            }
            
            // Detect when we land (end falling)
            if (!wasGroundedLastFrame && isGrounded)
            {
                EndFall();
            }
        }
        else
        {
            // On platform - cancel any active fall tracking
            if (isFalling)
            {
                // Reset fall state without damage
                isFalling = false;
                fallStartHeight = 0f;
                highestPointDuringFall = 0f;
                fallStartTime = 0f;
                
                Debug.Log("[FallingDamageSystem] ✅ On moving platform/sliding - fall tracking cancelled");
            }
        }
        
        wasGroundedLastFrame = isGrounded;
    }
    
    /// <summary>
    /// Detect high-speed collisions with CharacterController
    /// CRITICAL: Only for WALL collisions, not ground landings!
    /// </summary>
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!enableCollisionDamage || playerHealth == null || movementController == null) return;
        
        // CRITICAL FIX: Don't apply collision damage during slides!
        // Slides intentionally use high downward velocity for ground adhesion
        bool isSliding = _crouchController != null && _crouchController.IsSliding;
        if (isSliding) return;
        
        // CRITICAL FIX: Don't apply collision damage when grounded!
        // Ground landings are handled by fall damage system, not collision damage
        // This prevents double-damage and fixes high-velocity landing spam
        bool isGrounded = movementController != null ? movementController.IsGroundedWithCoyote : controller.isGrounded;
        if (isGrounded) return;
        
        // CRITICAL FIX: Only apply damage to VERTICAL surfaces (walls), not horizontal (ground/ceiling)
        // Calculate surface angle from vertical (0° = perfectly vertical wall, 90° = perfectly horizontal ground)
        float surfaceAngleFromVertical = Vector3.Angle(hit.normal, Vector3.up);
        
        // Only damage on walls (roughly vertical surfaces between 30-150 degrees from up vector)
        // This excludes: ground (0-30°), ceiling (150-180°), and shallow slopes
        if (surfaceAngleFromVertical < 30f || surfaceAngleFromVertical > 150f) return;
        
        // Check cooldown
        if (Time.time - lastCollisionDamageTime < collisionDamageCooldown) return;
        
        // Get collision speed (magnitude of velocity change)
        float collisionSpeed = lastVelocity.magnitude;
        
        // Only process significant collisions
        if (collisionSpeed < minCollisionSpeed) return;
        
        // Calculate normal angle - only damage on head-on collisions
        float hitAngle = Vector3.Angle(-lastVelocity.normalized, hit.normal);
        
        // Only damage if hitting something roughly head-on (within 60 degrees)
        if (hitAngle > 60f) return;
        
        // Calculate collision damage based on speed
        float damagePercent = Mathf.InverseLerp(minCollisionSpeed, severeCollisionSpeed, collisionSpeed);
        float collisionDamage = Mathf.Lerp(minCollisionDamage, maxCollisionDamage, damagePercent);
        
        // Apply collision damage
        ApplyCollisionDamage(collisionDamage, collisionSpeed);
        
        lastCollisionDamageTime = Time.time;
    }
    
    private void StartFall()
    {
        isFalling = true;
        fallStartHeight = transform.position.y;
        highestPointDuringFall = fallStartHeight;
        fallStartTime = Time.time;
        
        // Record jump time to prevent vertical wind sound from triggering immediately
        lastJumpOrLandingTime = Time.time;
        
        // Debug log removed - only log actual falls (ones that last > minAirTimeForFallDetection)
    }
    
    private void TrackFallProgress()
    {
        // Track the highest point during the fall (in case player jumps or gets pushed up)
        float currentHeight = transform.position.y;
        if (currentHeight > highestPointDuringFall)
        {
            highestPointDuringFall = currentHeight;
        }
    }
    
    private void EndFall()
    {
        if (!isFalling) return;
        
        // 🔇 CRITICAL FIX: Stop wind sounds IMMEDIATELY when landing detected
        // This must happen BEFORE any other checks to prevent wind playing after landing
        StopAllWindSounds();
        
        // Calculate how long player was in air
        float airTime = Time.time - fallStartTime;
        
        // CRITICAL ANTI-SPAM: Check landing cooldown to prevent rapid re-triggers from jittery ground detection
        float timeSinceLastLanding = Time.time - lastLandingProcessedTime;
        if (timeSinceLastLanding < landingCooldown)
        {
            // Reset fall tracking WITHOUT logging
            isFalling = false;
            fallStartHeight = 0f;
            highestPointDuringFall = 0f;
            fallStartTime = 0f;
            
            Debug.Log($"⏱️ [FALL COOLDOWN] Ignoring landing - too soon after last landing ({timeSinceLastLanding:F2}s < {landingCooldown}s)");
            return;
        }
        
        // CRITICAL FIX: Only process falls that lasted long enough (prevents spam on tiny bumps/steps)
        if (airTime < minAirTimeForFallDetection)
        {
            // Reset fall tracking WITHOUT logging or processing damage
            isFalling = false;
            fallStartHeight = 0f;
            highestPointDuringFall = 0f;
            fallStartTime = 0f;
            
            // Silent return - this was just a tiny bump, not a real fall
            return;
        }
        
        // Mark that we're processing this landing
        lastLandingProcessedTime = Time.time;
        lastJumpOrLandingTime = Time.time; // Also record for wind sound cooldown
        
        // Calculate total fall distance from highest point
        float currentHeight = transform.position.y;
        float fallDistance = highestPointDuringFall - currentHeight;
        
        Debug.Log($"[FallingDamageSystem] Landed! Air time: {airTime:F2}s, Fall distance: {fallDistance:F1} units");
        
        // 🎯 UNIFIED IMPACT SYSTEM - Calculate comprehensive impact data
        ImpactData impact = CalculateImpactData(fallDistance, airTime, currentHeight);
        
        // 📢 BROADCAST IMPACT EVENT FIRST (single source of truth!)
        // This notifies all listeners (camera, audio, effects) about the impact
        ImpactEventBroadcaster.BroadcastImpact(impact);
        
        // Then apply damage (this system's specific responsibility)
        if (impact.damageAmount > 0)
        {
            ApplyFallDamageFromImpact(impact);
        }
        
        // Trigger visual landing effects (this system's responsibility)
        TriggerLandingEffectFromImpact(impact);
        
        // Reset fall tracking
        isFalling = false;
        fallStartHeight = 0f;
        highestPointDuringFall = 0f;
        fallStartTime = 0f;
    }
    
    /// <summary>
    /// 🌬️ DUAL WIND SOUND SYSTEM - Separate sounds for vertical falling and horizontal rushing
    /// VERTICAL: Falling wind for downward movement (terminal velocity feel)
    /// HORIZONTAL: Rushing wind for high-speed horizontal movement (flying/grappling feel)
    /// Both can play simultaneously for realistic sound when moving fast in multiple directions!
    /// </summary>
    private void UpdateWindSound()
    {
        if (movementController == null) return;
        
        // 🔇 CRITICAL FIX: Check if we're ACTUALLY on ground (raw state, no debouncing delay)
        // This prevents wind from playing during the debounced landing detection period
        bool isRawGrounded = controller != null && controller.isGrounded;
        if (isRawGrounded)
        {
            // We're physically on the ground - stop ALL wind sounds immediately
            if (isVerticalWindPlaying || isHorizontalWindPlaying)
            {
                StopAllWindSounds();
            }
            return; // Early exit - no wind sounds while grounded
        }
        
        Vector3 velocity = movementController.Velocity;
        
        // Calculate downward speed (negative Y velocity)
        float downwardSpeed = Mathf.Max(0f, -velocity.y);
        
        // Calculate horizontal speed (XZ plane)
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
        float horizontalSpeed = horizontalVelocity.magnitude;
        
        // SUDDEN STOP DETECTION - Check for abrupt deceleration (wall crash)
        float currentTotalSpeed = velocity.magnitude;
        float lastTotalSpeed = lastFrameVelocity.magnitude;
        float speedChange = lastTotalSpeed - currentTotalSpeed;
        const float CRASH_DECELERATION_THRESHOLD = 3000f;
        bool suddenStop = speedChange > CRASH_DECELERATION_THRESHOLD;
        
        // Update last frame velocity for next frame
        lastFrameVelocity = velocity;
        
        // Update both wind sound systems
        UpdateVerticalWindSound(downwardSpeed, suddenStop);
        UpdateHorizontalWindSound(horizontalSpeed, suddenStop);
    }
    
    /// <summary>
    /// Update VERTICAL falling wind sound (downward movement)
    /// </summary>
    private void UpdateVerticalWindSound(float downwardSpeed, bool suddenStop)
    {
        // Check if we're in the cooldown period after a jump/landing
        float timeSinceJumpOrLanding = Time.time - lastJumpOrLandingTime;
        bool inCooldown = timeSinceJumpOrLanding < verticalWindJumpCooldown;
        
        // Anti-spam timing
        float timeSinceStart = Time.time - verticalWindStartTime;
        float timeSinceStopped = Time.time - verticalWindStopTime;
        const float MIN_PLAY_DURATION = 0.3f;
        const float MIN_STOP_DURATION = 0.2f;
        
        bool shouldPlay = false;
        
        if (isVerticalWindPlaying)
        {
            // IMMEDIATE STOP on sudden deceleration
            if (suddenStop)
            {
                StopVerticalWind();
                Debug.Log($"[FallingDamageSystem] 🌬️ VERTICAL wind CRASHED");
                return;
            }
            
            // Use hysteresis threshold to stop
            shouldPlay = downwardSpeed >= (verticalWindThreshold - verticalWindHysteresis);
            
            // Enforce minimum play duration
            if (!shouldPlay && timeSinceStart < MIN_PLAY_DURATION)
            {
                shouldPlay = true;
            }
        }
        else
        {
            // Use normal threshold to start, check cooldown
            shouldPlay = downwardSpeed >= verticalWindThreshold && !inCooldown;
            
            // Enforce minimum stop duration
            if (shouldPlay && timeSinceStopped < MIN_STOP_DURATION)
            {
                shouldPlay = false;
            }
        }
        
        // Start vertical wind
        if (shouldPlay && !isVerticalWindPlaying)
        {
            verticalWindHandle = GameSounds.StartFallingWindLoop(transform, verticalWindMinVolume);
            isVerticalWindPlaying = true;
            verticalWindStartTime = Time.time;
            Debug.Log($"[FallingDamageSystem] 🌬️ VERTICAL wind STARTED (D:{downwardSpeed:F0})");
        }
        // Stop vertical wind
        else if (!shouldPlay && isVerticalWindPlaying)
        {
            StopVerticalWind();
            Debug.Log($"[FallingDamageSystem] 🌬️ VERTICAL wind STOPPED (D:{downwardSpeed:F0})");
        }
        // Update volume
        else if (isVerticalWindPlaying && verticalWindHandle.IsValid)
        {
            float speedPercent = Mathf.InverseLerp(verticalWindThreshold, verticalWindMaxSpeed, downwardSpeed);
            float targetVolume = Mathf.Lerp(verticalWindMinVolume, verticalWindMaxVolume, speedPercent);
            verticalWindHandle.SetVolume(targetVolume);
        }
    }
    
    /// <summary>
    /// Update HORIZONTAL rushing wind sound (XZ plane movement)
    /// </summary>
    private void UpdateHorizontalWindSound(float horizontalSpeed, bool suddenStop)
    {
        // No cooldown for horizontal wind - can start immediately
        
        // Anti-spam timing
        float timeSinceStart = Time.time - horizontalWindStartTime;
        float timeSinceStopped = Time.time - horizontalWindStopTime;
        const float MIN_PLAY_DURATION = 0.3f;
        const float MIN_STOP_DURATION = 0.2f;
        
        bool shouldPlay = false;
        
        if (isHorizontalWindPlaying)
        {
            // IMMEDIATE STOP on sudden deceleration
            if (suddenStop)
            {
                StopHorizontalWind();
                Debug.Log($"[FallingDamageSystem] 💨 HORIZONTAL wind CRASHED");
                return;
            }
            
            // Use hysteresis threshold to stop
            shouldPlay = horizontalSpeed >= (horizontalWindThreshold - horizontalWindHysteresis);
            
            // Enforce minimum play duration
            if (!shouldPlay && timeSinceStart < MIN_PLAY_DURATION)
            {
                shouldPlay = true;
            }
        }
        else
        {
            // Use normal threshold to start
            shouldPlay = horizontalSpeed >= horizontalWindThreshold;
            
            // Enforce minimum stop duration
            if (shouldPlay && timeSinceStopped < MIN_STOP_DURATION)
            {
                shouldPlay = false;
            }
        }
        
        // Start horizontal wind
        if (shouldPlay && !isHorizontalWindPlaying)
        {
            // TODO: Replace with dedicated horizontal rushing wind sound when available
            horizontalWindHandle = GameSounds.StartFallingWindLoop(transform, horizontalWindMinVolume);
            isHorizontalWindPlaying = true;
            horizontalWindStartTime = Time.time;
            Debug.Log($"[FallingDamageSystem] 💨 HORIZONTAL wind STARTED (H:{horizontalSpeed:F0})");
        }
        // Stop horizontal wind
        else if (!shouldPlay && isHorizontalWindPlaying)
        {
            StopHorizontalWind();
            Debug.Log($"[FallingDamageSystem] 💨 HORIZONTAL wind STOPPED (H:{horizontalSpeed:F0})");
        }
        // Update volume
        else if (isHorizontalWindPlaying && horizontalWindHandle.IsValid)
        {
            float speedPercent = Mathf.InverseLerp(horizontalWindThreshold, horizontalWindMaxSpeed, horizontalSpeed);
            float targetVolume = Mathf.Lerp(horizontalWindMinVolume, horizontalWindMaxVolume, speedPercent);
            horizontalWindHandle.SetVolume(targetVolume);
        }
    }
    
    /// <summary>
    /// Helper: Stop vertical wind sound
    /// </summary>
    private void StopVerticalWind()
    {
        if (verticalWindHandle.IsValid)
        {
            verticalWindHandle.Stop();
        }
        GameSounds.StopFallingWindLoop();
        isVerticalWindPlaying = false;
        verticalWindHandle = SoundHandle.Invalid;
        verticalWindStopTime = Time.time;
    }
    
    /// <summary>
    /// Helper: Stop horizontal wind sound
    /// </summary>
    private void StopHorizontalWind()
    {
        if (horizontalWindHandle.IsValid)
        {
            horizontalWindHandle.Stop();
        }
        // TODO: Stop dedicated horizontal wind when available
        GameSounds.StopFallingWindLoop();
        isHorizontalWindPlaying = false;
        horizontalWindHandle = SoundHandle.Invalid;
        horizontalWindStopTime = Time.time;
    }
    
    /// <summary>
    /// Helper: Stop ALL wind sounds immediately (used on landing)
    /// </summary>
    private void StopAllWindSounds()
    {
        // Stop vertical wind
        if (isVerticalWindPlaying)
        {
            StopVerticalWind();
            Debug.Log("[FallingDamageSystem] 🔇 VERTICAL wind FORCE STOPPED (landing)");
        }
        
        // Stop horizontal wind
        if (isHorizontalWindPlaying)
        {
            StopHorizontalWind();
            Debug.Log("[FallingDamageSystem] 🔇 HORIZONTAL wind FORCE STOPPED (landing)");
        }
    }
    
    /// <summary>
    /// 🎯 UNIFIED IMPACT SYSTEM - Calculate comprehensive impact data
    /// This is the SINGLE SOURCE OF TRUTH for all impact calculations
    /// </summary>
    private ImpactData CalculateImpactData(float fallDistance, float airTime, float currentHeight)
    {
        ImpactData impact = new ImpactData
        {
            fallDistance = fallDistance,
            airTime = airTime,
            landingPosition = transform.position,
            timestamp = Time.time
        };
        
        // Get ground normal for slope detection
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 2f))
        {
            impact.landingNormal = hit.normal;
        }
        else
        {
            impact.landingNormal = Vector3.up;
        }
        
        // Calculate impact speed from movement controller
        if (movementController != null)
        {
            impact.impactSpeed = Mathf.Abs(movementController.Velocity.y);
        }
        
        // Determine severity tier and calculate all values
        if (fallDistance >= lethalFallHeight)
        {
            // LETHAL IMPACT (4x+ player height)
            impact.severity = ImpactSeverity.Lethal;
            impact.severityNormalized = 1.0f;
            impact.damageAmount = lethalFallDamage;
            impact.traumaIntensity = 1.0f;
            impact.compressionAmount = landingCompressionAmount * 1.5f;
        }
        else if (fallDistance >= severeDamageFallHeight)
        {
            // SEVERE IMPACT (3-4x player height)
            impact.severity = ImpactSeverity.Severe;
            float t = Mathf.InverseLerp(severeDamageFallHeight, lethalFallHeight, fallDistance);
            impact.severityNormalized = Mathf.Lerp(0.6f, 1.0f, t);
            impact.damageAmount = Mathf.Lerp(severeFallDamage, lethalFallDamage, damageScaleCurve.Evaluate(t));
            impact.traumaIntensity = Mathf.Lerp(0.6f, 1.0f, t);
            impact.compressionAmount = landingCompressionAmount * Mathf.Lerp(1.2f, 1.5f, t);
        }
        else if (fallDistance >= moderateDamageFallHeight)
        {
            // MODERATE IMPACT (2-3x player height)
            impact.severity = ImpactSeverity.Moderate;
            float t = Mathf.InverseLerp(moderateDamageFallHeight, severeDamageFallHeight, fallDistance);
            impact.severityNormalized = Mathf.Lerp(0.3f, 0.6f, t);
            impact.damageAmount = Mathf.Lerp(moderateFallDamage, severeFallDamage, damageScaleCurve.Evaluate(t));
            impact.traumaIntensity = Mathf.Lerp(0.3f, 0.6f, t);
            impact.compressionAmount = landingCompressionAmount * Mathf.Lerp(0.8f, 1.2f, t);
        }
        else if (fallDistance >= minDamageFallHeight)
        {
            // LIGHT IMPACT (1-2x player height)
            impact.severity = ImpactSeverity.Light;
            float t = Mathf.InverseLerp(minDamageFallHeight, moderateDamageFallHeight, fallDistance);
            impact.severityNormalized = Mathf.Lerp(0.1f, 0.3f, t);
            impact.damageAmount = Mathf.Lerp(minFallDamage, moderateFallDamage, damageScaleCurve.Evaluate(t));
            impact.traumaIntensity = Mathf.Lerp(0.15f, 0.3f, t);
            impact.compressionAmount = landingCompressionAmount * Mathf.Lerp(0.5f, 0.8f, t);
        }
        else
        {
            // NO IMPACT (too small)
            impact.severity = ImpactSeverity.None;
            impact.severityNormalized = 0f;
            impact.damageAmount = 0f;
            impact.traumaIntensity = 0f;
            impact.compressionAmount = 0f;
        }
        
        // Context flags
        float groundAngle = Vector3.Angle(impact.landingNormal, Vector3.up);
        impact.wasOnSlope = groundAngle > 15f;
        
        // Check if sprinting via energy system (if available)
        PlayerEnergySystem energySystem = movementController != null ? movementController.GetComponent<PlayerEnergySystem>() : null;
        impact.wasSprinting = energySystem != null && energySystem.IsCurrentlySprinting;
        
        impact.wasInTrick = cameraController != null && cameraController.IsTrickActive;
        
        // 🦸 SUPERHERO LANDING TRIGGER LOGIC (UNIFIED!)
        // Triggers on:
        // 1. Massive fall (2000+ units) - always superhero worthy
        // 2. Epic airtime (2s+) + decent fall (moderate damage threshold) - hang time mastery
        // 3. Aerial tricks + decent fall - style points!
        impact.shouldTriggerSuperheroLanding = 
            (fallDistance >= ImpactThresholds.SUPERHERO_IMPACT) ||  // Big fall
            (airTime >= ImpactThresholds.EPIC_AIR_TIME && fallDistance >= moderateDamageFallHeight) || // Epic airtime
            (impact.wasInTrick && fallDistance >= moderateDamageFallHeight); // Tricks + decent fall
        
        return impact;
    }
    
    /// <summary>
    /// Apply scaled fall damage based on fall height with AAA camera effects
    /// AND trigger visual landing effects
    /// </summary>
    private void ApplyScaledFallDamage(float fallDistance)
    {
        // Determine damage tier and calculate scaled damage
        float damage = 0f;
        float traumaIntensity = 0f;
        string severity = "Light";
        GameObject effectToTrigger = null;
        
        if (fallDistance >= lethalFallHeight)
        {
            // LETHAL FALL - Instant death
            damage = lethalFallDamage;
            traumaIntensity = 1.0f; // Maximum trauma
            severity = "LETHAL";
            effectToTrigger = superheroLandingEffect;
        }
        else if (fallDistance >= severeDamageFallHeight)
        {
            // SEVERE to LETHAL range
            float t = Mathf.InverseLerp(severeDamageFallHeight, lethalFallHeight, fallDistance);
            damage = Mathf.Lerp(severeFallDamage, lethalFallDamage, damageScaleCurve.Evaluate(t));
            traumaIntensity = Mathf.Lerp(0.6f, 1.0f, t);
            severity = "SEVERE";
            effectToTrigger = epicLandingEffect;
        }
        else if (fallDistance >= moderateDamageFallHeight)
        {
            // MODERATE to SEVERE range
            float t = Mathf.InverseLerp(moderateDamageFallHeight, severeDamageFallHeight, fallDistance);
            damage = Mathf.Lerp(moderateFallDamage, severeFallDamage, damageScaleCurve.Evaluate(t));
            traumaIntensity = Mathf.Lerp(0.3f, 0.6f, t);
            severity = "MODERATE";
            effectToTrigger = mediumLandingEffect;
        }
        else
        {
            // LIGHT to MODERATE range
            float t = Mathf.InverseLerp(minDamageFallHeight, moderateDamageFallHeight, fallDistance);
            damage = Mathf.Lerp(minFallDamage, moderateFallDamage, damageScaleCurve.Evaluate(t));
            traumaIntensity = Mathf.Lerp(0.15f, 0.3f, t);
            severity = "Light";
            effectToTrigger = smallLandingEffect;
        }
        
        // Trigger visual landing effect
        if (effectToTrigger != null)
        {
            effectToTrigger.SetActive(false);
            effectToTrigger.SetActive(true);
        }
        
        // Apply damage
        if (playerHealth == null) return;
        
        Debug.Log($"<color=red>💀 [{severity} FALL DAMAGE] {damage:F0} HP from {fallDistance:F0} units (Trauma: {traumaIntensity:F2})</color>");
        
        // Apply damage directly to health, bypassing armor plates (realistic fall damage)
        playerHealth.TakeDamageBypassArmor(damage);
        
        // Add camera trauma for impact feel
        if (cameraController != null)
        {
            cameraController.AddTrauma(traumaIntensity);
        }
        
        // AAA Dramatic blood splat overlay
        playerHealth.TriggerDramaticBloodSplat(traumaIntensity);
        
        // Play fall damage sound scaled by severity
        if (traumaIntensity >= 0.6f)
        {
            GameSounds.PlayFallDamage(transform.position, 1.0f); // Loud for severe falls
        }
        else if (traumaIntensity >= 0.3f)
        {
            GameSounds.PlayFallDamage(transform.position, 0.7f); // Medium for moderate falls
        }
        else
        {
            GameSounds.PlayFallDamage(transform.position, 0.5f); // Quiet for light falls
        }
    }
    
    /// <summary>
    /// 🎯 UNIFIED IMPACT SYSTEM - Apply damage from impact data
    /// Replaces direct ApplyScaledFallDamage calls
    /// </summary>
    private void ApplyFallDamageFromImpact(ImpactData impact)
    {
        if (playerHealth == null) return;
        
        string severityName = impact.severity.ToString().ToUpper();
        
        Debug.Log($"<color=red>💀 [{severityName} FALL DAMAGE] {impact.damageAmount:F0} HP from {impact.fallDistance:F0} units (Trauma: {impact.traumaIntensity:F2})</color>");
        
        // Apply damage directly to health, bypassing armor plates (realistic fall damage)
        playerHealth.TakeDamageBypassArmor(impact.damageAmount);
        
        // NOTE: Camera trauma is now handled by AAACameraController via impact event!
        // This prevents double-trauma application
        
        // AAA Dramatic blood splat overlay
        playerHealth.TriggerDramaticBloodSplat(impact.traumaIntensity);
        
        // Play fall damage sound scaled by severity
        if (impact.traumaIntensity >= 0.6f)
        {
            GameSounds.PlayFallDamage(transform.position, 1.0f); // Loud for severe falls
        }
        else if (impact.traumaIntensity >= 0.3f)
        {
            GameSounds.PlayFallDamage(transform.position, 0.7f); // Medium for moderate falls
        }
        else
        {
            GameSounds.PlayFallDamage(transform.position, 0.5f); // Quiet for light falls
        }
    }
    
    /// <summary>
    /// 🎯 UNIFIED IMPACT SYSTEM - Trigger visual effects from impact data
    /// </summary>
    private void TriggerLandingEffectFromImpact(ImpactData impact)
    {
        GameObject effectToTrigger = null;
        
        // Select effect based on severity
        switch (impact.severity)
        {
            case ImpactSeverity.Light:
                effectToTrigger = smallLandingEffect;
                break;
            case ImpactSeverity.Moderate:
                effectToTrigger = mediumLandingEffect;
                break;
            case ImpactSeverity.Severe:
                effectToTrigger = epicLandingEffect;
                break;
            case ImpactSeverity.Lethal:
                effectToTrigger = superheroLandingEffect;
                break;
        }
        
        // Trigger effect
        if (effectToTrigger != null)
        {
            effectToTrigger.SetActive(false);
            effectToTrigger.SetActive(true);
        }
    }
    
    /// <summary>
    /// Apply collision damage from high-speed impacts with AAA camera effects
    /// </summary>
    private void ApplyCollisionDamage(float damage, float collisionSpeed)
    {
        if (playerHealth == null) return;
        
        // Calculate trauma from collision speed
        float traumaIntensity = Mathf.InverseLerp(minCollisionSpeed, severeCollisionSpeed, collisionSpeed);
        traumaIntensity = Mathf.Clamp(traumaIntensity * 0.8f, 0.2f, 0.8f); // Scale to 0.2-0.8 range
        
        Debug.Log($"<color=orange>💥 [COLLISION DAMAGE] {damage:F0} HP at {collisionSpeed:F0} units/s (Trauma: {traumaIntensity:F2})</color>");
        
        // Apply collision damage, bypassing armor plates (realistic impact damage)
        playerHealth.TakeDamageBypassArmor(damage);
        
        // Add camera trauma for collision impact
        if (cameraController != null)
        {
            cameraController.AddTrauma(traumaIntensity);
        }
        
        // AAA Dramatic blood splat overlay
        playerHealth.TriggerDramaticBloodSplat(traumaIntensity);
        
        // Play impact sound
        GameSounds.PlayFallDamage(transform.position, traumaIntensity);
    }
    
    /// <summary>
    /// Get current fall distance (useful for UI or debugging)
    /// </summary>
    public float GetCurrentFallDistance()
    {
        if (!isFalling) return 0f;
        return highestPointDuringFall - transform.position.y;
    }
    
    /// <summary>
    /// Check if currently falling
    /// </summary>
    public bool IsFalling()
    {
        return isFalling;
    }
    
    /// <summary>
    /// CRITICAL: Detect if player is on a moving platform (elevator)
    /// OPTIMIZED: Caches elevator reference, only searches once
    /// </summary>
    private void DetectPlatform()
    {
        // Fast path: If we have a current elevator, just verify we're still in it
        if (_currentElevator != null)
        {
            if (!_currentElevator.IsPlayerInElevator(controller))
            {
                Debug.Log("[FallingDamageSystem] Left elevator - fall damage RE-ENABLED");
                _currentElevator = null;
                _isOnPlatform = false;
            }
            return; // Early exit - we're done
        }
        
        // Slow path: Only runs when we don't have an elevator cached
        // This only happens once when entering a new elevator
        ElevatorController[] elevators = FindObjectsOfType<ElevatorController>();
        foreach (var elevator in elevators)
        {
            if (elevator.IsPlayerInElevator(controller))
            {
                _currentElevator = elevator;
                _isOnPlatform = true;
                Debug.Log("[FallingDamageSystem] ✅ Entered elevator - fall damage DISABLED");
                break;
            }
        }
    }
}
