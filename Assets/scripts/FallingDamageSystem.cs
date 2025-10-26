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
    
    [Header("Falling Wind Sound")]
    [SerializeField] private float windSoundSpeedThreshold = 1500f; // Minimum speed (any direction) to trigger wind sound (faster than sprint)
    [SerializeField] private float windSoundMinVolume = 0.3f; // Volume at threshold speed
    [SerializeField] private float windSoundMaxVolume = 1.0f; // Volume at max speed
    [SerializeField] private float windSoundMaxSpeed = 3000f; // Speed for maximum volume (terminal velocity)
    [Tooltip("Hysteresis offset to prevent jittery on/off (stop threshold is lower)")]
    [SerializeField] private float windSoundHysteresis = 100f; // Stop at 1400 if threshold is 1500
    [Tooltip("Cooldown after jump/landing before wind sound can start (prevents glitchy triggering on jumps)")]
    [SerializeField] private float windSoundJumpCooldown = 0.5f; // Half second cooldown after jump
    
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
    private bool isWindSoundPlaying = false;
    private SoundHandle windSoundHandle = SoundHandle.Invalid;
    private float lastLandingProcessedTime = -999f; // Anti-spam cooldown
    private float lastJumpOrLandingTime = -999f; // Track last jump/landing for wind sound cooldown
    
    // Collision damage tracking
    private float lastCollisionDamageTime = -999f;
    private Vector3 lastVelocity = Vector3.zero;
    
    // CRITICAL: Platform movement tracking
    private ElevatorController _currentElevator = null;
    private bool _isOnPlatform = false;
    
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerHealth = GetComponent<PlayerHealth>();
        movementController = GetComponent<AAAMovementController>();
        
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
        
        // CRITICAL: Skip fall detection if on moving platform!
        if (!_isOnPlatform)
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
                
                Debug.Log("[FallingDamageSystem] ✅ On moving platform - fall tracking cancelled");
            }
        }
        
        wasGroundedLastFrame = isGrounded;
    }
    
    /// <summary>
    /// Detect high-speed collisions with CharacterController
    /// </summary>
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!enableCollisionDamage || playerHealth == null || movementController == null) return;
        
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
        isWindSoundPlaying = false;
        
        // Record jump time to prevent wind sound from triggering immediately
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
    /// 🌬️ SIMPLE WIND SOUND SYSTEM - Speed based with dynamic volume
    /// Works with ANY movement - falling, flying, grappling, etc.
    /// FIXED: Now respects jump/landing cooldown to prevent glitchy triggering
    /// </summary>
    private void UpdateWindSound()
    {
        if (movementController == null) return;
        
        // Get TOTAL speed in any direction
        float currentSpeed = movementController.Velocity.magnitude;
        
        // Check if we're in the cooldown period after a jump/landing
        float timeSinceJumpOrLanding = Time.time - lastJumpOrLandingTime;
        bool inCooldown = timeSinceJumpOrLanding < windSoundJumpCooldown;
        
        // Determine if wind should play based on speed with HYSTERESIS
        bool shouldPlay = false;
        if (isWindSoundPlaying)
        {
            // Currently playing - use lower threshold to stop (prevents jitter)
            shouldPlay = currentSpeed >= (windSoundSpeedThreshold - windSoundHysteresis);
        }
        else
        {
            // Not playing - use normal threshold to start AND check cooldown
            shouldPlay = currentSpeed >= windSoundSpeedThreshold && !inCooldown;
        }
        
        // Start wind sound if needed
        if (shouldPlay && !isWindSoundPlaying)
        {
            windSoundHandle = GameSounds.StartFallingWindLoop(transform, windSoundMinVolume);
            isWindSoundPlaying = true;
            Debug.Log($"[FallingDamageSystem] 🌬️ Wind sound STARTED at speed: {currentSpeed:F1} units/s");
        }
        // Stop wind sound if needed
        else if (!shouldPlay && isWindSoundPlaying)
        {
            if (windSoundHandle.IsValid)
            {
                windSoundHandle.Stop();
            }
            GameSounds.StopFallingWindLoop();
            isWindSoundPlaying = false;
            windSoundHandle = SoundHandle.Invalid;
            Debug.Log($"[FallingDamageSystem] 🌬️ Wind sound STOPPED at speed: {currentSpeed:F1} units/s");
        }
        // Update volume based on speed if playing
        else if (isWindSoundPlaying && windSoundHandle.IsValid)
        {
            // Calculate volume based on speed (louder = faster)
            float speedPercent = Mathf.InverseLerp(windSoundSpeedThreshold, windSoundMaxSpeed, currentSpeed);
            float targetVolume = Mathf.Lerp(windSoundMinVolume, windSoundMaxVolume, speedPercent);
            
            // Set volume on the sound handle
            windSoundHandle.SetVolume(targetVolume);
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
