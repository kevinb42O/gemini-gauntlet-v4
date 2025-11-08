// ============================================================================
// DYNAMIC PLAYER LIGHT CONTROLLER - AAA Reactive Lighting System
// Responds to player actions and health states with smooth color transitions
// Zero performance impact - uses efficient tweening and cached references
// ============================================================================

using UnityEngine;
using System.Collections;
using GeminiGauntlet.Audio;

/// <summary>
/// High-performance dynamic lighting system that responds to player actions:
/// - Beam firing: Smooth transition to blue
/// - Shotgun firing: Orange flash with quick dim
/// - Low health: Red warning pulsation
/// - Damage taken: Bright red flash
/// 
/// Uses cached references and efficient lerping for zero performance impact.
/// </summary>
public class DynamicPlayerLightController : MonoBehaviour
{
    [Header("Light Reference")]
    [Tooltip("The PlayerLight (point light) to control")]
    public Light playerLight;
    
    [Header("Large World Auto-Configuration")]
    [Tooltip("Auto-configure light range for 320-unit player (recommended)")]
    public bool autoConfigureForLargeWorld = true;
    [Tooltip("Light range for massive world (auto-set to 2000 units)")]
    [Range(500f, 5000f)]
    public float lightRange = 2000f;
    [Tooltip("Light cookie size multiplier for large scale")]
    [Range(1f, 10f)]
    public float cookieSizeMultiplier = 3f;
    
    [Header("Base Light Settings")]
    [Tooltip("Default light color when idle")]
    public Color baseColor = Color.white;
    [Tooltip("Default light intensity when idle (scaled for 320-unit player)")]
    [Range(0f, 50000f)]
    public float baseIntensity = 15f;
    
    [Header("Beam Mode Colors")]
    [Tooltip("Color when firing beam weapons")]
    public Color beamColor = new Color(0.2f, 0.7f, 1f); // Blue
    [Tooltip("Intensity multiplier when firing beam (scaled for large world)")]
    [Range(0.5f, 3f)]
    public float beamIntensityMultiplier = 1.3f;
    [Tooltip("Speed of transition to beam color")]
    [Range(0.1f, 5f)]
    public float beamTransitionSpeed = 2f;
    
    [Header("Shotgun Flash Settings")]
    [Tooltip("Color for shotgun muzzle flash")]
    public Color shotgunColor = new Color(1f, 0.6f, 0.2f); // Orange
    [Tooltip("Peak intensity during shotgun flash (scaled for massive world)")]
    [Range(10f, 75000f)]
    public float shotgunFlashIntensity = 35f;
    [Tooltip("Duration of shotgun flash effect")]
    [Range(0.05f, 0.5f)]
    public float shotgunFlashDuration = 0.15f;
    [Tooltip("Speed of flash fade-out")]
    [Range(1f, 20f)]
    public float shotgunFadeSpeed = 8f;
    
    [Header("Health Warning System")]
    [Tooltip("Color when health is critically low")]
    public Color lowHealthColor = new Color(1f, 0.1f, 0.1f); // Red
    [Tooltip("Health percentage threshold for warning (0.0-1.0)")]
    [Range(0.1f, 0.5f)]
    public float lowHealthThreshold = 0.25f;
    [Tooltip("Pulse speed for low health warning")]
    [Range(0.5f, 5f)]
    public float lowHealthPulseSpeed = 1.5f;
    [Tooltip("Minimum intensity during low health pulse (scaled for large world)")]
    [Range(5f, 75000f)]
    public float lowHealthMinIntensity = 8f;
    [Tooltip("Maximum intensity during low health pulse (scaled for large world)")]
    [Range(15f, 75000f)]
    public float lowHealthMaxIntensity = 25f;
    
    [Header("Damage Flash Settings")]
    [Tooltip("Color when taking damage")]
    public Color damageColor = new Color(1f, 0f, 0f); // Bright red
    [Tooltip("Peak intensity during damage flash (scaled for massive world)")]
    [Range(20f, 75000f)]
    public float damageFlashIntensity = 50f;
    [Tooltip("Duration of damage flash effect")]
    [Range(0.1f, 1f)]
    public float damageFlashDuration = 0.3f;
    [Tooltip("Speed of damage flash fade")]
    [Range(2f, 15f)]
    public float damageFadeSpeed = 10f;
    
    [Header("Wall Jump Flash Settings")]
    [Tooltip("Color when performing wall jumps")]
    public Color wallJumpColor = new Color(0.2f, 1f, 0.5f); // Bright green
    [Tooltip("Peak intensity during wall jump flash (scaled for massive world)")]
    [Range(15f, 75000f)]
    public float wallJumpFlashIntensity = 30f;
    [Tooltip("Duration of wall jump flash effect")]
    [Range(0.1f, 1f)]
    public float wallJumpFlashDuration = 0.4f;
    [Tooltip("Speed of wall jump flash fade")]
    [Range(1f, 10f)]
    public float wallJumpFadeSpeed = 5f;
    
    [Header("Spawn Animation Settings")]
    [Tooltip("Enable epic spawn animation when player spawns")]
    public bool enableSpawnAnimation = true;
    [Tooltip("Spawn animation duration")]
    [Range(1f, 10f)]
    public float spawnAnimationDuration = 4f;
    [Tooltip("Number of orbits during spawn")]
    [Range(1, 5)]
    public int spawnOrbitCount = 2;
    [Tooltip("Orbit radius around player")]
    [Range(50f, 500f)]
    public float spawnOrbitRadius = 200f;
    [Tooltip("Spawn effect colors (will cycle through these)")]
    public Color[] spawnEffectColors = new Color[]
    {
        new Color(1f, 0.8f, 0.2f),    // Gold
        new Color(0.2f, 0.8f, 1f),    // Cyan
        new Color(1f, 0.2f, 0.8f),    // Pink
        new Color(0.8f, 1f, 0.2f),    // Lime
        new Color(0.8f, 0.2f, 1f)     // Purple
    };
    [Tooltip("Peak intensity during spawn animation")]
    [Range(20f, 75000f)]
    public float spawnEffectIntensity = 100f;
    
    [Header("Sword Mode Settings")]
    [Tooltip("Color when sword mode is active")]
    public Color swordModeColor = new Color(0.8f, 0.2f, 1f); // Purple/Magenta
    [Tooltip("Intensity when in sword mode (scaled for massive world)")]
    [Range(5f, 75000f)]
    public float swordModeIntensity = 20f;
    [Tooltip("Speed of transition to sword mode color")]
    [Range(0.1f, 5f)]
    public float swordModeTransitionSpeed = 1.5f;
    
    [Header("System References")]
    [Tooltip("Auto-find PlayerShooterOrchestrator for shooting events")]
    public bool autoFindShooterOrchestrator = true;
    [Tooltip("Auto-find PlayerHealth for health/damage events")]
    public bool autoFindPlayerHealth = true;
    [Tooltip("Auto-find AAAMovementController for wall jump events")]
    public bool autoFindMovementController = true;
    
    // Cached references for performance
    private PlayerShooterOrchestrator shooterOrchestrator;
    private PlayerHealth playerHealth;
    private AAAMovementController movementController;
    
    // Current state tracking
    private Color currentTargetColor;
    private float currentTargetIntensity;
    private bool isLeftBeamActive = false;
    private bool isRightBeamActive = false;
    private bool isLowHealth = false;
    private bool isSwordModeActive = false;
    
    // Effect coroutines (only one active at a time for efficiency)
    private Coroutine activeEffectCoroutine;
    private Coroutine lowHealthPulseCoroutine;
    private Coroutine spawnAnimationCoroutine;
    
    // Performance optimization - cached initial values
    private Color originalColor;
    private float originalIntensity;
    private Vector3 originalLightPosition; // For spawn animation
    private Transform lightTransform; // Cache light transform for animations
    
    // State priority system (higher = more important)
    private enum LightState
    {
        Base = 0,
        SwordMode = 1,
        Beam = 2,
        LowHealth = 3,
        WallJump = 4,
        Shotgun = 5,
        Damage = 6,
        SpawnAnimation = 7
    }
    private LightState currentState = LightState.Base;
    
    void Awake()
    {
        // Auto-find PlayerLight if not assigned
        if (playerLight == null)
        {
            playerLight = GameObject.Find("PlayerLight")?.GetComponent<Light>();
            if (playerLight == null)
            {
                // Try finding any light on the player
                playerLight = GetComponentInChildren<Light>();
            }
        }
        
        if (playerLight == null)
        {
            Debug.LogError("[DynamicPlayerLightController] No PlayerLight found! Please assign manually or ensure a Light named 'PlayerLight' exists.");
            enabled = false;
            return;
        }
        
        // Cache original settings
        originalColor = playerLight.color;
        originalIntensity = playerLight.intensity;
        originalLightPosition = playerLight.transform.localPosition;
        lightTransform = playerLight.transform;
        
        // Auto-configure for large world scale
        if (autoConfigureForLargeWorld)
        {
            ConfigureForLargeWorld();
        }
        
        // Initialize current state
        currentTargetColor = baseColor;
        currentTargetIntensity = baseIntensity;
        
        Debug.Log("[DynamicPlayerLightController] ✅ Initialized with PlayerLight", this);
    }
    
    void Start()
    {
        FindSystemReferences();
        SubscribeToEvents();
        
        // Set initial light state
        ApplyLightSettings(baseColor, baseIntensity, instant: true);
        
        // Start epic spawn animation
        if (enableSpawnAnimation)
        {
            StartSpawnAnimation();
        }
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
        
        // Stop all coroutines
        if (activeEffectCoroutine != null)
        {
            StopCoroutine(activeEffectCoroutine);
        }
        if (lowHealthPulseCoroutine != null)
        {
            StopCoroutine(lowHealthPulseCoroutine);
        }
        if (spawnAnimationCoroutine != null)
        {
            StopCoroutine(spawnAnimationCoroutine);
        }
    }
    
    // ============================================================================
    // LARGE WORLD AUTO-CONFIGURATION
    // ============================================================================
    
    private void ConfigureForLargeWorld()
    {
        if (playerLight == null) return;
        
        // Configure light range for massive world (320-unit player)
        playerLight.range = lightRange;
        
        // Ensure light type is Point Light for best performance in large worlds
        if (playerLight.type != LightType.Point)
        {
            playerLight.type = LightType.Point;
            Debug.Log("[DynamicPlayerLightController] ✅ Set light type to Point Light for optimal large world performance");
        }
        
        // Configure shadows for large world performance
        // Real-time shadows can be expensive with high range - use soft shadows for best quality/performance balance
        if (playerLight.shadows == LightShadows.None)
        {
            playerLight.shadows = LightShadows.Soft;
            Debug.Log("[DynamicPlayerLightController] ✅ Enabled soft shadows for better large world lighting");
        }
        
        // Set shadow resolution appropriate for large scale
        // Higher resolution needed due to large range, but cap for performance
        playerLight.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Medium;
        
        // Configure culling mask if not set (usually Everything)
        if (playerLight.cullingMask == 0)
        {
            playerLight.cullingMask = -1; // Everything
        }
        
        // Set render mode for best performance in large worlds
        playerLight.renderMode = LightRenderMode.Auto;
        
        Debug.Log($"[DynamicPlayerLightController] ✅ Auto-configured for large world - Range: {lightRange}, Intensity: {baseIntensity}, Shadows: {playerLight.shadows}");
    }
    
    // ============================================================================
    // SYSTEM INITIALIZATION
    // ============================================================================
    
    private void FindSystemReferences()
    {
        // Find PlayerShooterOrchestrator for shooting events
        if (autoFindShooterOrchestrator && shooterOrchestrator == null)
        {
            shooterOrchestrator = FindObjectOfType<PlayerShooterOrchestrator>();
            if (shooterOrchestrator != null)
            {
                Debug.Log("[DynamicPlayerLightController] ✅ Found PlayerShooterOrchestrator");
            }
            else
            {
                Debug.LogWarning("[DynamicPlayerLightController] ⚠️ PlayerShooterOrchestrator not found - shooting effects disabled");
            }
        }
        
        // Find PlayerHealth for health/damage events
        if (autoFindPlayerHealth && playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth != null)
            {
                Debug.Log("[DynamicPlayerLightController] ✅ Found PlayerHealth");
            }
            else
            {
                Debug.LogWarning("[DynamicPlayerLightController] ⚠️ PlayerHealth not found - health effects disabled");
            }
        }
        
        // Find AAAMovementController for wall jump events
        if (autoFindMovementController && movementController == null)
        {
            movementController = FindObjectOfType<AAAMovementController>();
            if (movementController != null)
            {
                Debug.Log("[DynamicPlayerLightController] ✅ Found AAAMovementController");
            }
            else
            {
                Debug.LogWarning("[DynamicPlayerLightController] ⚠️ AAAMovementController not found - wall jump effects disabled");
            }
        }
    }
    
    private void SubscribeToEvents()
    {
        // Subscribe to shooting events
        if (shooterOrchestrator != null && shooterOrchestrator.inputHandler != null)
        {
            // Primary hand (left) - separate tracking
            shooterOrchestrator.inputHandler.OnPrimaryHoldStartedAction += OnLeftBeamStarted;
            shooterOrchestrator.inputHandler.OnPrimaryHoldEndedAction += OnLeftBeamEnded;
            shooterOrchestrator.inputHandler.OnPrimaryTapAction += OnLeftShotgunFired;
            
            // Secondary hand (right) - separate tracking
            shooterOrchestrator.inputHandler.OnSecondaryHoldStartedAction += OnRightBeamStarted;
            shooterOrchestrator.inputHandler.OnSecondaryHoldEndedAction += OnRightBeamEnded;
            shooterOrchestrator.inputHandler.OnSecondaryTapAction += OnRightShotgunFired;
            
            Debug.Log("[DynamicPlayerLightController] ✅ Subscribed to dual-hand shooting events");
        }
        
        // Subscribe to health events
        if (playerHealth != null)
        {
            PlayerHealth.OnHealthChangedForHUD += OnHealthChanged;
            Debug.Log("[DynamicPlayerLightController] ✅ Subscribed to health events");
            
            // Hook into damage detection via custom damage event
            DamageEventBroadcaster damageEventBroadcaster = playerHealth.GetComponent<DamageEventBroadcaster>();
            if (damageEventBroadcaster == null)
            {
                damageEventBroadcaster = playerHealth.gameObject.AddComponent<DamageEventBroadcaster>();
                Debug.Log("[DynamicPlayerLightController] ✅ Added DamageEventBroadcaster for precise damage detection");
            }
            damageEventBroadcaster.OnDamageTaken_Instance += OnDamageTaken;
        }
        
        // Subscribe to sword mode events
        if (shooterOrchestrator != null)
        {
            // Subscribe to sword mode property changes directly
            PlayerShooterOrchestrator.OnSwordModeChanged += OnSwordModeChanged;
            PlayerShooterOrchestrator.OnLeftSwordModeChanged += OnLeftSwordModeChanged;
            Debug.Log("[DynamicPlayerLightController] ✅ Subscribed to sword mode events");
        }
        
        // Subscribe to wall jump events from AAAMovementController
        if (movementController != null)
        {
            AAAMovementController.OnWallJumpPerformed += OnWallJumpPerformed;
            Debug.Log("[DynamicPlayerLightController] ✅ Subscribed to wall jump events");
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        // Unsubscribe from shooting events
        if (shooterOrchestrator != null && shooterOrchestrator.inputHandler != null)
        {
            shooterOrchestrator.inputHandler.OnPrimaryHoldStartedAction -= OnLeftBeamStarted;
            shooterOrchestrator.inputHandler.OnPrimaryHoldEndedAction -= OnLeftBeamEnded;
            shooterOrchestrator.inputHandler.OnPrimaryTapAction -= OnLeftShotgunFired;
            shooterOrchestrator.inputHandler.OnSecondaryHoldStartedAction -= OnRightBeamStarted;
            shooterOrchestrator.inputHandler.OnSecondaryHoldEndedAction -= OnRightBeamEnded;
            shooterOrchestrator.inputHandler.OnSecondaryTapAction -= OnRightShotgunFired;
        }
        
        // Unsubscribe from health events
        PlayerHealth.OnHealthChangedForHUD -= OnHealthChanged;
        
        // Unsubscribe from wall jump events
        AAAMovementController.OnWallJumpPerformed -= OnWallJumpPerformed;
        
        // Unsubscribe from sword mode events
        PlayerShooterOrchestrator.OnSwordModeChanged -= OnSwordModeChanged;
        PlayerShooterOrchestrator.OnLeftSwordModeChanged -= OnLeftSwordModeChanged;
        
        // Unsubscribe from damage events
        if (playerHealth != null)
        {
            DamageEventBroadcaster damageEventBroadcaster = playerHealth.GetComponent<DamageEventBroadcaster>();
            if (damageEventBroadcaster != null)
            {
                damageEventBroadcaster.OnDamageTaken_Instance -= OnDamageTaken;
            }
        }
    }
    
    // ============================================================================
    // EVENT HANDLERS - SMART DUAL-HAND SYSTEM
    // ============================================================================
    
    private void OnLeftBeamStarted()
    {
        if (!enabled || playerLight == null) return;
        
        isLeftBeamActive = true;
        UpdateBeamState();
    }
    
    private void OnLeftBeamEnded()
    {
        if (!enabled || playerLight == null) return;
        
        isLeftBeamActive = false;
        UpdateBeamState();
    }
    
    private void OnRightBeamStarted()
    {
        if (!enabled || playerLight == null) return;
        
        isRightBeamActive = true;
        UpdateBeamState();
    }
    
    private void OnRightBeamEnded()
    {
        if (!enabled || playerLight == null) return;
        
        isRightBeamActive = false;
        UpdateBeamState();
    }
    
    private void UpdateBeamState()
    {
        bool anyBeamActive = isLeftBeamActive || isRightBeamActive;
        
        if (anyBeamActive)
        {
            // Enter beam mode
            SetLightState(LightState.Beam);
            if (activeEffectCoroutine != null)
            {
                StopCoroutine(activeEffectCoroutine);
            }
            activeEffectCoroutine = StartCoroutine(TransitionToBeamMode());
        }
        else
        {
            // Exit beam mode
            DetermineNewStateAfterBeam();
        }
    }
    
    private void OnLeftShotgunFired()
    {
        if (!enabled || playerLight == null) return;
        TriggerShotgunFlash();
    }
    
    private void OnRightShotgunFired()
    {
        if (!enabled || playerLight == null) return;
        TriggerShotgunFlash();
    }
    
    private void TriggerShotgunFlash()
    {
        // Shotgun flash overrides everything temporarily
        SetLightState(LightState.Shotgun);
        
        if (activeEffectCoroutine != null)
        {
            StopCoroutine(activeEffectCoroutine);
        }
        activeEffectCoroutine = StartCoroutine(ShotgunFlashEffect());
    }
    
    private void TriggerWallJumpFlash()
    {
        // Wall jump flash overrides most things temporarily (but not damage)
        if (currentState != LightState.Damage && currentState != LightState.SpawnAnimation)
        {
            SetLightState(LightState.WallJump);
            
            if (activeEffectCoroutine != null)
            {
                StopCoroutine(activeEffectCoroutine);
            }
            activeEffectCoroutine = StartCoroutine(WallJumpFlashEffect());
        }
    }
    
    private void OnHealthChanged(float currentHealth, float maxHealth)
    {
        if (!enabled || playerLight == null) return;
        
        float healthPercentage = currentHealth / maxHealth;
        bool wasLowHealth = isLowHealth;
        isLowHealth = healthPercentage <= lowHealthThreshold;
        
        // Handle low health state changes
        if (isLowHealth && !wasLowHealth)
        {
            // Just entered low health
            UpdateCurrentState();
        }
        else if (!isLowHealth && wasLowHealth)
        {
            // Just recovered from low health
            StopLowHealthPulse();
            UpdateCurrentState();
        }
    }
    
    private void OnDamageTaken(float damageAmount)
    {
        if (!enabled || playerLight == null) return;
        
        // Trigger damage flash effect
        TriggerDamageFlash();
    }
    
    private void OnWallJumpPerformed(Vector3 wallJumpPosition)
    {
        if (!enabled || playerLight == null) return;
        
        // Trigger wall jump flash effect
        TriggerWallJumpFlash();
    }
    
    private void OnSwordModeChanged(bool isActive)
    {
        if (!enabled || playerLight == null) return;
        
        bool wasSwordModeActive = isSwordModeActive;
        isSwordModeActive = isActive;
        
        if (isActive != wasSwordModeActive)
        {
            UpdateCurrentState();
            Debug.Log($"[DynamicPlayerLightController] Right sword mode changed: {isSwordModeActive}");
        }
    }
    
    private void OnLeftSwordModeChanged(bool isActive)
    {
        if (!enabled || playerLight == null) return;
        
        bool wasSwordModeActive = isSwordModeActive;
        // Update combined sword mode state (either hand active = sword mode active)
        bool newSwordModeActive = isActive || (shooterOrchestrator != null && shooterOrchestrator.IsSwordModeActive);
        
        if (newSwordModeActive != wasSwordModeActive)
        {
            isSwordModeActive = newSwordModeActive;
            UpdateCurrentState();
            Debug.Log($"[DynamicPlayerLightController] Left sword mode changed, combined state: {isSwordModeActive}");
        }
    }
    
    // ============================================================================
    // SMART STATE MANAGEMENT SYSTEM
    // ============================================================================
    
    private void UpdateCurrentState()
    {
        // Don't interrupt high-priority temporary effects
        if (currentState == LightState.Damage || currentState == LightState.Shotgun)
        {
            return;
        }
        
        // Determine the highest priority persistent state
        LightState newState = DetermineCurrentPersistentState();
        
        if (newState != currentState)
        {
            SetLightState(newState);
            TransitionToNewState(newState);
        }
    }
    
    private LightState DetermineCurrentPersistentState()
    {
        // Priority order: Beam > LowHealth > SwordMode > Base
        
        if (isLeftBeamActive || isRightBeamActive)
        {
            return LightState.Beam;
        }
        
        if (isLowHealth)
        {
            return LightState.LowHealth;
        }
        
        if (isSwordModeActive)
        {
            return LightState.SwordMode;
        }
        
        return LightState.Base;
    }
    
    private void DetermineNewStateAfterBeam()
    {
        LightState newState = DetermineCurrentPersistentState();
        SetLightState(newState);
        
        if (activeEffectCoroutine != null)
        {
            StopCoroutine(activeEffectCoroutine);
        }
        activeEffectCoroutine = StartCoroutine(TransitionFromBeamMode());
    }
    
    private void TransitionToNewState(LightState newState)
    {
        if (activeEffectCoroutine != null)
        {
            StopCoroutine(activeEffectCoroutine);
        }
        
        switch (newState)
        {
            case LightState.Beam:
                activeEffectCoroutine = StartCoroutine(TransitionToBeamMode());
                break;
                
            case LightState.SwordMode:
                activeEffectCoroutine = StartCoroutine(TransitionToSwordMode());
                break;
                
            case LightState.LowHealth:
                activeEffectCoroutine = StartCoroutine(TransitionToLowHealthMode());
                break;
                
            case LightState.Base:
                activeEffectCoroutine = StartCoroutine(TransitionToBaseMode());
                break;
        }
    }
    
    // ============================================================================
    // EFFECT COROUTINES - ENHANCED FOR DUAL-HAND & SWORD MODE
    // ============================================================================
    
    private IEnumerator TransitionToBeamMode()
    {
        Color startColor = playerLight.color;
        float startIntensity = playerLight.intensity;
        Color targetColor = beamColor;
        float targetIntensity = baseIntensity * beamIntensityMultiplier;
        
        float elapsed = 0f;
        float duration = 1f / beamTransitionSpeed;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t); // Smooth easing
            
            ApplyLightSettings(
                Color.Lerp(startColor, targetColor, t),
                Mathf.Lerp(startIntensity, targetIntensity, t),
                instant: false
            );
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we end up exactly at target
        ApplyLightSettings(targetColor, targetIntensity, instant: true);
        activeEffectCoroutine = null;
    }
    
    private IEnumerator TransitionToSwordMode()
    {
        Color startColor = playerLight.color;
        float startIntensity = playerLight.intensity;
        Color targetColor = swordModeColor;
        float targetIntensity = swordModeIntensity;
        
        float elapsed = 0f;
        float duration = 1f / swordModeTransitionSpeed;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t);
            
            ApplyLightSettings(
                Color.Lerp(startColor, targetColor, t),
                Mathf.Lerp(startIntensity, targetIntensity, t),
                instant: false
            );
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        ApplyLightSettings(targetColor, targetIntensity, instant: true);
        activeEffectCoroutine = null;
    }
    
    private IEnumerator TransitionToLowHealthMode()
    {
        Color startColor = playerLight.color;
        float startIntensity = playerLight.intensity;
        Color targetColor = lowHealthColor;
        float targetIntensity = lowHealthMaxIntensity;
        
        float elapsed = 0f;
        float duration = 0.5f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t);
            
            ApplyLightSettings(
                Color.Lerp(startColor, targetColor, t),
                Mathf.Lerp(startIntensity, targetIntensity, t),
                instant: false
            );
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        ApplyLightSettings(targetColor, targetIntensity, instant: true);
        activeEffectCoroutine = null;
        
        // Start pulsing
        StartLowHealthPulse();
    }
    
    private IEnumerator TransitionToBaseMode()
    {
        Color startColor = playerLight.color;
        float startIntensity = playerLight.intensity;
        
        float elapsed = 0f;
        float duration = 0.5f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t);
            
            ApplyLightSettings(
                Color.Lerp(startColor, baseColor, t),
                Mathf.Lerp(startIntensity, baseIntensity, t),
                instant: false
            );
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        ApplyLightSettings(baseColor, baseIntensity, instant: true);
        activeEffectCoroutine = null;
    }
    
    // ============================================================================
    // EPIC SPAWN ANIMATION & WALL JUMP EFFECTS
    // ============================================================================
    
    private void StartSpawnAnimation()
    {
        if (spawnAnimationCoroutine != null)
        {
            StopCoroutine(spawnAnimationCoroutine);
        }
        
        SetLightState(LightState.SpawnAnimation);
        spawnAnimationCoroutine = StartCoroutine(SpawnAnimationCoroutine());
    }
    
    private IEnumerator SpawnAnimationCoroutine()
    {
        Vector3 centerPosition = lightTransform.position;
        
        // Phase 1: Dramatic intensity buildup with color cycling (1.5s)
        float phase1Duration = 1.5f;
        float elapsed = 0f;
        
        while (elapsed < phase1Duration)
        {
            float t = elapsed / phase1Duration;
            
            // Cycle through spawn colors
            int colorIndex = Mathf.FloorToInt(t * spawnEffectColors.Length * 3f) % spawnEffectColors.Length;
            Color currentColor = spawnEffectColors[colorIndex];
            
            // Pulse intensity dramatically
            float pulseValue = Mathf.Sin(t * Mathf.PI * 8f) * 0.5f + 0.5f; // Fast pulsing
            float intensity = Mathf.Lerp(baseIntensity, spawnEffectIntensity, pulseValue * t);
            
            ApplyLightSettings(currentColor, intensity, instant: true);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Phase 2: Orbital movement with trail effect (2.5s)
        float phase2Duration = 2.5f;
        elapsed = 0f;
        
        while (elapsed < phase2Duration)
        {
            float t = elapsed / phase2Duration;
            
            // Calculate orbital position
            float orbitAngle = t * spawnOrbitCount * 2f * Mathf.PI;
            Vector3 orbitOffset = new Vector3(
                Mathf.Cos(orbitAngle) * spawnOrbitRadius,
                Mathf.Sin(orbitAngle * 0.7f) * spawnOrbitRadius * 0.5f, // Vertical figure-8
                Mathf.Sin(orbitAngle) * spawnOrbitRadius
            );
            
            // Apply orbital position
            lightTransform.position = centerPosition + orbitOffset;
            
            // Color transition through rainbow
            int colorIndex = Mathf.FloorToInt(t * spawnEffectColors.Length) % spawnEffectColors.Length;
            int nextColorIndex = (colorIndex + 1) % spawnEffectColors.Length;
            float colorBlend = (t * spawnEffectColors.Length) % 1f;
            
            Color currentColor = Color.Lerp(spawnEffectColors[colorIndex], spawnEffectColors[nextColorIndex], colorBlend);
            
            // Intensity reduces as it returns to center
            float intensityT = 1f - Mathf.Abs(t - 0.5f) * 2f; // Peak at middle, fade at ends
            float intensity = Mathf.Lerp(baseIntensity, spawnEffectIntensity * 0.7f, intensityT);
            
            ApplyLightSettings(currentColor, intensity, instant: true);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Phase 3: Return to center with final flash (1s)
        Vector3 startPos = lightTransform.position;
        float phase3Duration = 1f;
        elapsed = 0f;
        
        while (elapsed < phase3Duration)
        {
            float t = elapsed / phase3Duration;
            t = Mathf.SmoothStep(0f, 1f, t); // Smooth return
            
            // Move back to center
            lightTransform.position = Vector3.Lerp(startPos, centerPosition, t);
            
            // Final color transition to base
            Color currentColor = Color.Lerp(spawnEffectColors[spawnEffectColors.Length - 1], baseColor, t);
            float intensity = Mathf.Lerp(spawnEffectIntensity * 0.5f, baseIntensity, t);
            
            ApplyLightSettings(currentColor, intensity, instant: true);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final position and state
        lightTransform.localPosition = originalLightPosition;
        ApplyLightSettings(baseColor, baseIntensity, instant: true);
        
        // Return to appropriate state
        SetLightState(LightState.Base);
        UpdateCurrentState();
        
        spawnAnimationCoroutine = null;
        Debug.Log("[DynamicPlayerLightController] ✨ Epic spawn animation completed!");
    }
    
    private IEnumerator WallJumpFlashEffect()
    {
        // Store current persistent state for restoration
        LightState preFlashState = DetermineCurrentPersistentState();
        Color preFlashColor = GetColorForState(preFlashState);
        float preFlashIntensity = GetIntensityForState(preFlashState);
        
        // Instant bright green flash
        ApplyLightSettings(wallJumpColor, wallJumpFlashIntensity, instant: true);
        
        // Hold briefly
        yield return new WaitForSeconds(wallJumpFlashDuration);
        
        // Slowly fade back to appropriate state
        float elapsed = 0f;
        float fadeTime = 1f / wallJumpFadeSpeed;
        
        while (elapsed < fadeTime)
        {
            float t = elapsed / fadeTime;
            t = Mathf.SmoothStep(0f, 1f, t);
            
            ApplyLightSettings(
                Color.Lerp(wallJumpColor, preFlashColor, t),
                Mathf.Lerp(wallJumpFlashIntensity, preFlashIntensity, t),
                instant: false
            );
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        ApplyLightSettings(preFlashColor, preFlashIntensity, instant: true);
        activeEffectCoroutine = null;
        
        // Restore appropriate persistent state
        SetLightState(preFlashState);
        
        // Resume any special effects
        if (preFlashState == LightState.LowHealth)
        {
            StartLowHealthPulse();
        }
    }
    
    private IEnumerator TransitionFromBeamMode()
    {
        Color startColor = playerLight.color;
        float startIntensity = playerLight.intensity;
        
        // Determine target based on current persistent state
        LightState targetState = DetermineCurrentPersistentState();
        Color targetColor = GetColorForState(targetState);
        float targetIntensity = GetIntensityForState(targetState);
        
        float elapsed = 0f;
        float duration = 1f / beamTransitionSpeed;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t);
            
            ApplyLightSettings(
                Color.Lerp(startColor, targetColor, t),
                Mathf.Lerp(startIntensity, targetIntensity, t),
                instant: false
            );
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        ApplyLightSettings(targetColor, targetIntensity, instant: true);
        activeEffectCoroutine = null;
        
        // Start any special effects for the target state
        if (targetState == LightState.LowHealth)
        {
            StartLowHealthPulse();
        }
    }
    
    private Color GetColorForState(LightState state)
    {
        switch (state)
        {
            case LightState.SwordMode: return swordModeColor;
            case LightState.LowHealth: return lowHealthColor;
            case LightState.Beam: return beamColor;
            case LightState.WallJump: return wallJumpColor;
            default: return baseColor;
        }
    }
    
    private float GetIntensityForState(LightState state)
    {
        switch (state)
        {
            case LightState.SwordMode: return swordModeIntensity;
            case LightState.LowHealth: return lowHealthMaxIntensity;
            case LightState.Beam: return baseIntensity * beamIntensityMultiplier;
            case LightState.WallJump: return wallJumpFlashIntensity;
            default: return baseIntensity;
        }
    }
    
    private IEnumerator ShotgunFlashEffect()
    {
        // Store current state for restoration (before flash interruption)
        LightState preFlashState = DetermineCurrentPersistentState();
        Color preFlashColor = GetColorForState(preFlashState);
        float preFlashIntensity = GetIntensityForState(preFlashState);
        
        // Instant flash to orange
        ApplyLightSettings(shotgunColor, shotgunFlashIntensity, instant: true);
        
        // Hold briefly
        yield return new WaitForSeconds(shotgunFlashDuration);
        
        // Fade back to appropriate state
        float elapsed = 0f;
        float fadeTime = 1f / shotgunFadeSpeed;
        
        while (elapsed < fadeTime)
        {
            float t = elapsed / fadeTime;
            t = Mathf.SmoothStep(0f, 1f, t);
            
            ApplyLightSettings(
                Color.Lerp(shotgunColor, preFlashColor, t),
                Mathf.Lerp(shotgunFlashIntensity, preFlashIntensity, t),
                instant: false
            );
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Restore exact previous state
        ApplyLightSettings(preFlashColor, preFlashIntensity, instant: true);
        activeEffectCoroutine = null;
        
        // Restore appropriate persistent state
        SetLightState(preFlashState);
        
        // Resume any special effects
        if (preFlashState == LightState.LowHealth)
        {
            StartLowHealthPulse();
        }
    }
    
    private void TriggerDamageFlash()
    {
        // Damage flash has highest priority - interrupt everything
        if (activeEffectCoroutine != null)
        {
            StopCoroutine(activeEffectCoroutine);
        }
        
        SetLightState(LightState.Damage);
        activeEffectCoroutine = StartCoroutine(DamageFlashEffect());
    }
    
    private IEnumerator DamageFlashEffect()
    {
        // Store current persistent state for restoration
        LightState preFlashState = DetermineCurrentPersistentState();
        Color preFlashColor = GetColorForState(preFlashState);
        float preFlashIntensity = GetIntensityForState(preFlashState);
        
        // Instant bright red flash
        ApplyLightSettings(damageColor, damageFlashIntensity, instant: true);
        
        // Hold briefly
        yield return new WaitForSeconds(damageFlashDuration);
        
        // Fade back to appropriate state
        float elapsed = 0f;
        float fadeTime = 1f / damageFadeSpeed;
        
        while (elapsed < fadeTime)
        {
            float t = elapsed / fadeTime;
            t = Mathf.SmoothStep(0f, 1f, t);
            
            ApplyLightSettings(
                Color.Lerp(damageColor, preFlashColor, t),
                Mathf.Lerp(damageFlashIntensity, preFlashIntensity, t),
                instant: false
            );
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        ApplyLightSettings(preFlashColor, preFlashIntensity, instant: true);
        activeEffectCoroutine = null;
        
        // Restore appropriate persistent state
        SetLightState(preFlashState);
        
        // Resume any special effects
        if (preFlashState == LightState.LowHealth)
        {
            StartLowHealthPulse();
        }
    }
    
    private void StartLowHealthPulse()
    {
        if (lowHealthPulseCoroutine != null)
        {
            StopCoroutine(lowHealthPulseCoroutine);
        }
        lowHealthPulseCoroutine = StartCoroutine(LowHealthPulseEffect());
    }
    
    private void StopLowHealthPulse()
    {
        if (lowHealthPulseCoroutine != null)
        {
            StopCoroutine(lowHealthPulseCoroutine);
            lowHealthPulseCoroutine = null;
        }
    }
    
    private IEnumerator LowHealthPulseEffect()
    {
        while (isLowHealth && currentState == LightState.LowHealth && enabled)
        {
            // Pulse intensity between min and max
            float time = Time.time * lowHealthPulseSpeed;
            float pulseValue = Mathf.Sin(time) * 0.5f + 0.5f; // 0 to 1
            float intensity = Mathf.Lerp(lowHealthMinIntensity, lowHealthMaxIntensity, pulseValue);
            
            ApplyLightSettings(lowHealthColor, intensity, instant: true);
            
            yield return null;
        }
        
        lowHealthPulseCoroutine = null;
    }
    
    private void ReturnToBaseState()
    {
        if (activeEffectCoroutine != null)
        {
            StopCoroutine(activeEffectCoroutine);
        }
        activeEffectCoroutine = StartCoroutine(TransitionToBase());
    }
    
    private IEnumerator TransitionToBase()
    {
        Color startColor = playerLight.color;
        float startIntensity = playerLight.intensity;
        
        float elapsed = 0f;
        float duration = 0.5f; // Smooth transition back to base
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t);
            
            ApplyLightSettings(
                Color.Lerp(startColor, baseColor, t),
                Mathf.Lerp(startIntensity, baseIntensity, t),
                instant: false
            );
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        ApplyLightSettings(baseColor, baseIntensity, instant: true);
        activeEffectCoroutine = null;
    }
    
    // ============================================================================
    // UTILITY METHODS
    // ============================================================================
    
    private void SetLightState(LightState newState)
    {
        // Only update if new state has higher or equal priority
        if (newState >= currentState)
        {
            currentState = newState;
        }
    }
    
    private void ApplyLightSettings(Color color, float intensity, bool instant)
    {
        if (playerLight == null) return;
        
        if (instant)
        {
            playerLight.color = color;
            playerLight.intensity = intensity;
        }
        else
        {
            // For smooth transitions, the coroutines handle the interpolation
            playerLight.color = color;
            playerLight.intensity = intensity;
        }
        
        currentTargetColor = color;
        currentTargetIntensity = intensity;
    }
    
    // ============================================================================
    // EDITOR HELPERS & DEBUG
    // ============================================================================
    
    [ContextMenu("Configure for Large World")]
    private void ConfigureForLargeWorldManual()
    {
        if (playerLight != null)
        {
            ConfigureForLargeWorld();
            Debug.Log("[DynamicPlayerLightController] ✅ Manually configured for large world");
        }
        else
        {
            Debug.LogWarning("[DynamicPlayerLightController] ⚠️ No PlayerLight assigned for configuration");
        }
    }
    
    [ContextMenu("Test Wall Jump Flash")]
    private void TestWallJumpFlash()
    {
        if (Application.isPlaying)
        {
            TriggerWallJumpFlash();
        }
    }
    
    [ContextMenu("Test Epic Spawn Animation")]
    private void TestSpawnAnimation()
    {
        if (Application.isPlaying)
        {
            StartSpawnAnimation();
        }
    }
    
    [ContextMenu("Test Sword Mode")]
    private void TestSwordMode()
    {
        if (Application.isPlaying)
        {
            isSwordModeActive = !isSwordModeActive;
            UpdateCurrentState();
        }
    }
    
    [ContextMenu("Test Beam Mode")]
    private void TestBeamMode()
    {
        if (Application.isPlaying)
        {
            isLeftBeamActive = !isLeftBeamActive;
            UpdateBeamState();
        }
    }
    
    [ContextMenu("Test Shotgun Flash")]
    private void TestShotgunFlash()
    {
        if (Application.isPlaying)
        {
            TriggerShotgunFlash();
        }
    }
    
    [ContextMenu("Test Damage Flash")]
    private void TestDamageFlash()
    {
        if (Application.isPlaying)
        {
            TriggerDamageFlash();
        }
    }
    
    [ContextMenu("Test Low Health Warning")]
    private void TestLowHealthWarning()
    {
        if (Application.isPlaying)
        {
            isLowHealth = true;
            SetLightState(LightState.LowHealth);
            StartLowHealthPulse();
        }
    }
    
    [ContextMenu("Return to Base")]
    private void TestReturnToBase()
    {
        if (Application.isPlaying)
        {
            isLowHealth = false;
            isLeftBeamActive = false;
            isRightBeamActive = false;
            isSwordModeActive = false;
            StopLowHealthPulse();
            UpdateCurrentState();
        }
    }
    
    void OnValidate()
    {
        // Clamp values to reasonable ranges for large world
        lowHealthThreshold = Mathf.Clamp01(lowHealthThreshold);
        beamIntensityMultiplier = Mathf.Max(0.1f, beamIntensityMultiplier);
        lightRange = Mathf.Clamp(lightRange, 500f, 5000f);
        
        // Ensure large world intensities are reasonable
        baseIntensity = Mathf.Clamp(baseIntensity, 1f, 50000f);
        swordModeIntensity = Mathf.Clamp(swordModeIntensity, 5f, 75000f);
        wallJumpFlashIntensity = Mathf.Clamp(wallJumpFlashIntensity, 15f, 75000f);
        shotgunFlashIntensity = Mathf.Clamp(shotgunFlashIntensity, 10f, 75000f);
        damageFlashIntensity = Mathf.Clamp(damageFlashIntensity, 20f, 75000f);
        lowHealthMinIntensity = Mathf.Clamp(lowHealthMinIntensity, 5f, 75000f);
        lowHealthMaxIntensity = Mathf.Clamp(lowHealthMaxIntensity, 15f, 75000f);
        spawnEffectIntensity = Mathf.Clamp(spawnEffectIntensity, 20f, 75000f);
        
        // Update light immediately in editor if assigned
        if (playerLight != null && !Application.isPlaying)
        {
            playerLight.color = baseColor;
            playerLight.intensity = baseIntensity;
            
            // Apply large world configuration in editor
            if (autoConfigureForLargeWorld)
            {
                playerLight.range = lightRange;
            }
        }
    }
}