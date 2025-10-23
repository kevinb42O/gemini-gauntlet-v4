// ============================================================================
// HOLOGRAPHIC HAND INTEGRATION - AAA QUALITY SYSTEM REACTIONS
// Connects holographic shader to ALL game systems for dynamic visual feedback
// Reacts to: Landing, Jumping, Wall Jumps, Beam/Shotgun, Damage, Energy, etc.
// ============================================================================

using UnityEngine;
using System.Collections;

[RequireComponent(typeof(HolographicHandController))]
public class HolographicHandIntegration : MonoBehaviour
{
    [Header("System References")]
    [Tooltip("Auto-find if not assigned")]
    public AAAMovementController movementController;
    
    [Tooltip("Auto-find if not assigned")]
    public CleanAAACrouch crouchController;
    
    [Tooltip("Auto-find if not assigned")]
    public PlayerEnergySystem energySystem;
    
    [Tooltip("Auto-find if not assigned")]
    public PlayerHealth playerHealth;
    
    [Tooltip("Auto-find if not assigned")]
    public PlayerShooterOrchestrator shooterOrchestrator;
    
    [Header("Landing Impact Settings")]
    [Range(0f, 2f)]
    public float landingImpactIntensity = 0.8f;
    
    [Range(0f, 1f)]
    public float landingImpactDuration = 0.3f;
    
    [Range(0f, 10f)]
    public float hardLandingThreshold = 5f; // Fall height for hard landing
    
    [Header("Jump/Air Settings")]
    [Range(0f, 3f)]
    public float jumpBoostMultiplier = 1.5f;
    
    [Range(0f, 1f)]
    public float jumpBoostDuration = 0.2f;
    
    [Range(0f, 2f)]
    public float airGlowIntensity = 0.3f; // Subtle glow while airborne
    
    [Header("Wall Jump Settings")]
    [Range(0f, 2f)]
    public float wallJumpImpulseIntensity = 1.2f;
    
    [Range(0f, 1f)]
    public float wallJumpImpulseDuration = 0.4f;
    
    [Header("Beam Shooting Settings")]
    [Range(0f, 1f)]
    public float beamGlitchIntensity = 0.7f; // Heavy glitch during beam
    
    [Range(0f, 5f)]
    public float beamScanSpeedMultiplier = 2.5f; // Scan lines speed up
    
    [Range(0f, 3f)]
    public float beamEmissionBoost = 1.5f;
    
    [Header("Shotgun Shooting Settings")]
    [Range(0f, 3f)]
    public float shotgunPulseIntensity = 2.0f; // Quick pulse, no glitch
    
    [Range(0f, 0.5f)]
    public float shotgunPulseDuration = 0.15f;
    
    [Header("Energy Level Settings")]
    [Range(0f, 1f)]
    public float lowEnergyThreshold = 0.2f; // 20% energy
    
    [Range(0f, 0.5f)]
    public float lowEnergyGlitchIntensity = 0.3f;
    
    [Range(0f, 2f)]
    public float lowEnergyScanSlowdown = 0.5f; // Scan lines slow down
    
    [Header("Damage Settings")]
    [Range(0f, 1f)]
    public float damageGlitchIntensity = 0.8f;
    
    [Range(0f, 1f)]
    public float damageGlitchDuration = 0.5f;
    
    // Private state tracking
    private HolographicHandController handController;
    private bool wasGrounded = true;
    private bool wasInAir = false;
    private float fallStartHeight = 0f;
    private bool isBeamActive = false;
    private bool isShotgunActive = false;
    private float baseEmissionIntensity;
    private float baseScanLineSpeed;
    private float lastShotgunTime = -999f;
    
    // Coroutine tracking
    private Coroutine currentEffectCoroutine;
    
    void Awake()
    {
        handController = GetComponent<HolographicHandController>();
        
        // Auto-find references
        if (movementController == null)
            movementController = GetComponentInParent<AAAMovementController>();
        
        if (crouchController == null)
            crouchController = GetComponentInParent<CleanAAACrouch>();
        
        if (energySystem == null)
            energySystem = GetComponentInParent<PlayerEnergySystem>();
        
        if (playerHealth == null)
            playerHealth = GetComponentInParent<PlayerHealth>();
        
        if (shooterOrchestrator == null)
            shooterOrchestrator = GetComponentInParent<PlayerShooterOrchestrator>();
    }
    
    void Start()
    {
        // Store base values
        baseEmissionIntensity = handController.emissionIntensity;
        baseScanLineSpeed = handController.scanLineSpeed;
        
        // Subscribe to events
        SubscribeToEvents();
        
        Debug.Log($"[HolographicHandIntegration] Initialized on {gameObject.name}");
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    void Update()
    {
        // Track air state for landing detection
        UpdateAirStateTracking();
        
        // Update continuous effects
        UpdateEnergyLevelEffects();
        UpdateBeamEffects();
    }
    
    // ============================================================================
    // EVENT SUBSCRIPTION
    // ============================================================================
    
    private void SubscribeToEvents()
    {
        if (playerHealth != null)
        {
            // Subscribe to damage events if available
            // playerHealth.OnDamageTaken += HandleDamageTaken;
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        if (playerHealth != null)
        {
            // playerHealth.OnDamageTaken -= HandleDamageTaken;
        }
    }
    
    // ============================================================================
    // AIR STATE TRACKING & LANDING DETECTION
    // ============================================================================
    
    private void UpdateAirStateTracking()
    {
        if (movementController == null) return;
        
        bool isGrounded = movementController.IsGrounded;
        bool isFalling = movementController.IsFalling;
        
        // Detect takeoff
        if (wasGrounded && !isGrounded)
        {
            OnTakeoff();
        }
        
        // Detect landing
        if (!wasGrounded && isGrounded && wasInAir)
        {
            float fallDistance = fallStartHeight - transform.position.y;
            OnLanding(fallDistance);
        }
        
        // Track air state
        if (!isGrounded && !wasInAir)
        {
            wasInAir = true;
            fallStartHeight = transform.position.y;
        }
        else if (isGrounded)
        {
            wasInAir = false;
        }
        
        // Update airborne glow
        if (!isGrounded && airGlowIntensity > 0f)
        {
            handController.SetBoostMultiplier(1f + airGlowIntensity);
        }
        else if (isGrounded && !isBeamActive)
        {
            handController.SetBoostMultiplier(1f);
        }
        
        wasGrounded = isGrounded;
    }
    
    // ============================================================================
    // LANDING REACTIONS
    // ============================================================================
    
    private void OnTakeoff()
    {
        // Quick pulse on jump
        if (currentEffectCoroutine != null)
            StopCoroutine(currentEffectCoroutine);
        
        currentEffectCoroutine = StartCoroutine(JumpPulseEffect());
    }
    
    private void OnLanding(float fallDistance)
    {
        if (fallDistance > hardLandingThreshold)
        {
            // Hard landing - strong impact effect
            if (currentEffectCoroutine != null)
                StopCoroutine(currentEffectCoroutine);
            
            currentEffectCoroutine = StartCoroutine(HardLandingImpact());
            Debug.Log($"[HolographicHandIntegration] Hard landing detected! Fall distance: {fallDistance:F2}m");
        }
        else if (fallDistance > 1f)
        {
            // Normal landing - subtle pulse
            if (currentEffectCoroutine != null)
                StopCoroutine(currentEffectCoroutine);
            
            currentEffectCoroutine = StartCoroutine(NormalLandingPulse());
        }
    }
    
    private IEnumerator HardLandingImpact()
    {
        float elapsed = 0f;
        
        while (elapsed < landingImpactDuration)
        {
            float t = elapsed / landingImpactDuration;
            float intensity = Mathf.Lerp(landingImpactIntensity, 0f, t);
            
            // Glitch effect
            handController.SetGlitchIntensity(intensity);
            
            // Boost emission
            handController.emissionIntensity = baseEmissionIntensity * (1f + intensity);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Reset
        handController.SetGlitchIntensity(0f);
        handController.emissionIntensity = baseEmissionIntensity;
    }
    
    private IEnumerator NormalLandingPulse()
    {
        float elapsed = 0f;
        float duration = landingImpactDuration * 0.5f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float boost = Mathf.Lerp(1.3f, 1f, t);
            
            handController.SetBoostMultiplier(boost);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        handController.SetBoostMultiplier(1f);
    }
    
    private IEnumerator JumpPulseEffect()
    {
        float elapsed = 0f;
        
        while (elapsed < jumpBoostDuration)
        {
            float t = elapsed / jumpBoostDuration;
            float boost = Mathf.Lerp(jumpBoostMultiplier, 1f, t);
            
            handController.SetBoostMultiplier(boost);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        handController.SetBoostMultiplier(1f);
    }
    
    // ============================================================================
    // WALL JUMP DETECTION & REACTION
    // ============================================================================
    
    public void OnWallJump()
    {
        if (currentEffectCoroutine != null)
            StopCoroutine(currentEffectCoroutine);
        
        currentEffectCoroutine = StartCoroutine(WallJumpImpulseEffect());
        Debug.Log("[HolographicHandIntegration] Wall jump impulse triggered!");
    }
    
    private IEnumerator WallJumpImpulseEffect()
    {
        float elapsed = 0f;
        
        while (elapsed < wallJumpImpulseDuration)
        {
            float t = elapsed / wallJumpImpulseDuration;
            
            // Strong initial pulse that fades
            float intensity = Mathf.Lerp(wallJumpImpulseIntensity, 0f, Mathf.Pow(t, 0.5f));
            
            // Boost effects
            handController.SetBoostMultiplier(1f + intensity);
            handController.SetGlitchIntensity(intensity * 0.3f); // Subtle glitch
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Reset
        handController.SetBoostMultiplier(1f);
        handController.SetGlitchIntensity(0f);
    }
    
    // ============================================================================
    // BEAM SHOOTING EFFECTS (Heavy Glitch)
    // ============================================================================
    
    public void OnBeamStart()
    {
        isBeamActive = true;
        Debug.Log("[HolographicHandIntegration] Beam started - activating heavy glitch!");
    }
    
    public void OnBeamStop()
    {
        isBeamActive = false;
        
        // Reset effects
        handController.SetGlitchIntensity(0f);
        handController.scanLineSpeed = baseScanLineSpeed;
        handController.emissionIntensity = baseEmissionIntensity;
        
        Debug.Log("[HolographicHandIntegration] Beam stopped - effects reset");
    }
    
    private void UpdateBeamEffects()
    {
        if (!isBeamActive) return;
        
        // Heavy glitch while beam is active
        float glitchVariation = Mathf.PerlinNoise(Time.time * 10f, 0f) * 0.3f;
        handController.SetGlitchIntensity(beamGlitchIntensity + glitchVariation);
        
        // Speed up scan lines
        handController.scanLineSpeed = baseScanLineSpeed * beamScanSpeedMultiplier;
        
        // Boost emission
        handController.emissionIntensity = baseEmissionIntensity * beamEmissionBoost;
    }
    
    // ============================================================================
    // SHOTGUN SHOOTING EFFECTS (Quick Pulse, No Glitch)
    // ============================================================================
    
    public void OnShotgunFire()
    {
        // Prevent spam
        if (Time.time - lastShotgunTime < 0.1f) return;
        lastShotgunTime = Time.time;
        
        StartCoroutine(ShotgunPulseEffect());
    }
    
    private IEnumerator ShotgunPulseEffect()
    {
        float elapsed = 0f;
        
        while (elapsed < shotgunPulseDuration)
        {
            float t = elapsed / shotgunPulseDuration;
            float boost = Mathf.Lerp(shotgunPulseIntensity, 1f, t);
            
            handController.SetBoostMultiplier(boost);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        handController.SetBoostMultiplier(1f);
    }
    
    // ============================================================================
    // ENERGY LEVEL EFFECTS
    // ============================================================================
    
    private void UpdateEnergyLevelEffects()
    {
        if (energySystem == null) return;
        if (isBeamActive) return; // Don't interfere with beam effects
        
        float energyPercent = energySystem.CurrentEnergy / energySystem.MaxEnergy;
        
        if (energyPercent < lowEnergyThreshold)
        {
            // Low energy - glitch and slow scan lines
            float lowEnergyAmount = 1f - (energyPercent / lowEnergyThreshold);
            
            handController.SetGlitchIntensity(lowEnergyAmount * lowEnergyGlitchIntensity);
            handController.scanLineSpeed = baseScanLineSpeed * Mathf.Lerp(1f, lowEnergyScanSlowdown, lowEnergyAmount);
        }
        else
        {
            // Normal energy
            handController.SetGlitchIntensity(0f);
            handController.scanLineSpeed = baseScanLineSpeed;
        }
    }
    
    // ============================================================================
    // DAMAGE REACTION
    // ============================================================================
    
    public void OnDamageTaken(float damage)
    {
        StartCoroutine(DamageGlitchEffect());
    }
    
    private IEnumerator DamageGlitchEffect()
    {
        float elapsed = 0f;
        
        while (elapsed < damageGlitchDuration)
        {
            float t = elapsed / damageGlitchDuration;
            float intensity = Mathf.Lerp(damageGlitchIntensity, 0f, t);
            
            handController.SetGlitchIntensity(intensity);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        handController.SetGlitchIntensity(0f);
    }
    
    // ============================================================================
    // PUBLIC API FOR EXTERNAL SYSTEMS
    // ============================================================================
    
    /// <summary>
    /// Call this from your shooting system when beam starts
    /// </summary>
    public void NotifyBeamStart()
    {
        OnBeamStart();
    }
    
    /// <summary>
    /// Call this from your shooting system when beam stops
    /// </summary>
    public void NotifyBeamStop()
    {
        OnBeamStop();
    }
    
    /// <summary>
    /// Call this from your shooting system when shotgun fires
    /// </summary>
    public void NotifyShotgunFire()
    {
        OnShotgunFire();
    }
    
    /// <summary>
    /// Call this from wall jump detection
    /// </summary>
    public void NotifyWallJump()
    {
        OnWallJump();
    }
    
    /// <summary>
    /// Call this from damage system
    /// </summary>
    public void NotifyDamage(float damageAmount)
    {
        OnDamageTaken(damageAmount);
    }
}
