// --- HandFiringMechanics.cs (IMPROVED - Unified HandLevelSO System) ---
// REFACTORED: Audio handling removed - now exclusively managed by PlayerShooterOrchestrator
using UnityEngine;
// using GeminiGauntlet.Audio; // removed - audio centralized in PlayerShooterOrchestrator
using GeminiGauntlet.Animation;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MagicArsenal;

public class HandFiringMechanics : MonoBehaviour
{
    [Header("Core References (REQUIRED - Assign in Prefab)")]
    [SerializeField, Tooltip("Point from which VFX and damage originates. CRITICAL: Must be assigned!")]
    public Transform emitPoint;

    [Header("Accuracy Settings")]
    [Tooltip("ACCURACY SETTING:\n• TRUE = Use emit point as raycast origin (more accurate at long range, but may not hit exactly where crosshair points)\n• FALSE = Use camera as raycast origin (perfect center-screen accuracy, hits exactly where you're looking)")]
    public bool useEmitPointAsOrigin = false;
    
    [Header("Tuning")]
    [Tooltip("Horizontal angle offset for firing direction. Positive values aim right, negative values aim left.")]
    public float firingAngleOffset = 0f;
    
    [Header("Performance Limits")]
    [SerializeField, Tooltip("Maximum active shotgun VFX instances (prevents particle buildup)")]
    private int maxActiveShotgunVFX = 10;
    
    [Header("Debug & Validation")]
    [SerializeField, Tooltip("Show detailed debug logs for troubleshooting")]
    private bool enableDebugLogging = false;
    
    [ContextMenu("Run Diagnostics")]
    public void RunDiagnostics()
    {
        
        // Core References
        
        // Accuracy Settings
        
        // State Information
        
        // Config Validation
        if (_currentConfig != null)
        {
            
            // Hand Level Information
            int currentHandLevel = GetHandLevel();
            float currentShotgunScale = GetShotgunScaleForHandLevel(currentHandLevel);
        }
        
        // REMOVED: Active Beam Information for deprecated MagicBeamStatic system
        
    }

    // --- State & Configuration (Managed by Orchestrator) ---
    private HandLevelSO _currentConfig;
    private Transform _cameraTransform;
    private PlayerOverheatManager _overheatManager;
    private bool _isPrimaryHand;
    // Animation system references
    private LayeredHandAnimationController _handAnimationController;
    private PlayerAnimationStateManager _animationStateManager;
    // Camera shake controller reference
    private AAACameraController _cameraController;

    // --- Stream/Beam Weapon State ---
    private bool _isCurrentlyStreaming = false;
    private float _nextStreamDamageTime = 0f;
    // REMOVED: private GameObject _activeBeamInstance; // DEPRECATED MagicBeamStatic system - not used
    private GameObject _activeLegacyStreamInstance; // Legacy stream VFX instance (ACTUAL system being used)
    private HashSet<Collider> _currentStreamTargets = new HashSet<Collider>();

    // --- Shotgun Weapon State ---
    private float _nextShotgunFireTime = 0f;
    
    // ⚡ PERFORMANCE: Track active shotgun VFX to prevent buildup
    private static List<GameObject> _activeShotgunVFX = new List<GameObject>();
    private static List<List<ParticleSystem>> _activeDetachedParticles = new List<List<ParticleSystem>>();
    
    // ⚡ MEMORY LEAK FIX: Track last cleanup time for periodic null removal
    private static float _lastStaticListCleanupTime = 0f;
    private const float STATIC_LIST_CLEANUP_INTERVAL = 5f; // Clean every 5 seconds
    
    // --- Hit Detection Cache ---
    private RaycastHit[] _hitBuffer = new RaycastHit[50]; // Reusable buffer to reduce allocations
    
    // --- Approx Player Velocity Cache (used when no Rigidbody/CharacterController is found) ---
    private Vector3 _lastRootPosForVel;
    private float _lastVelSampleTime = -1f;
    private Vector3 _cachedApproxVelocity = Vector3.zero;

    public bool IsStreaming => _isCurrentlyStreaming;
    public bool IsStreamActive => _isCurrentlyStreaming; // For ProceduralHandAnimator integration
    // REMOVED: public GameObject ActiveBeam => _activeBeamInstance; // DEPRECATED - MagicBeamStatic not used
    
    public HandLevelSO GetCurrentHandConfig()
    {
        return _currentConfig;
    }
    public float TimeSinceLastShotgun => Time.time - _nextShotgunFireTime + (_currentConfig?.shotgunCooldown ?? 1f); // For ProceduralHandAnimator integration

    #region Initialization & Validation
    
    /// <summary>
    /// Validates inspector settings and component setup
    /// </summary>
#if UNITY_EDITOR
    void OnValidate()
    {
        // PERFORMANCE FIX: Skip validation during play mode to prevent scene update freezes
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            return;
            
        ValidateSetup();
    }
#endif
    
    /// <summary>
    /// Comprehensive setup validation
    /// </summary>
    private void ValidateSetup()
    {
        if (emitPoint == null)
        {
        }
        
        // Validate emitPoint is actually a child transform (disabled to reduce console spam)
        // if (emitPoint != null && !IsChildOf(emitPoint, transform))
        // {
        //     Debug.LogWarning($"⚠️ {name}: emitPoint should be a child of this transform for proper VFX positioning.", this);
        // }
    }
    
    /// <summary>
    /// Check if a transform is a child of another transform
    /// </summary>
    private bool IsChildOf(Transform child, Transform parent)
    {
        Transform current = child.parent;
        while (current != null)
        {
            if (current == parent) return true;
            current = current.parent;
        }
        return false;
    }

    public void Initialize(Transform cameraTransform, PlayerOverheatManager overheatManager, bool isPrimary)
    {
        _cameraTransform = cameraTransform;
        _overheatManager = overheatManager;
        _isPrimaryHand = isPrimary;
        
        // Find animation system components
        _animationStateManager = GetComponentInParent<PlayerAnimationStateManager>();
        if (_animationStateManager == null)
        {
            _animationStateManager = FindObjectOfType<PlayerAnimationStateManager>();
        }
        
        // Fallback to LayeredHandAnimationController if no centralized system
        if (_animationStateManager == null)
        {
            _handAnimationController = GetComponentInParent<LayeredHandAnimationController>();
            if (_handAnimationController == null)
            {
                _handAnimationController = FindObjectOfType<LayeredHandAnimationController>();
            }
            
            if (_handAnimationController == null)
            {
                Debug.LogWarning($"[HandFiringMechanics] No animation system found. Hand animations will be skipped.", this);
            }
        }
        
        // Find camera controller for shake effects
        if (_cameraTransform != null)
        {
            _cameraController = _cameraTransform.GetComponent<AAACameraController>();
            if (_cameraController == null)
            {
                // Try finding it in parent hierarchy
                _cameraController = _cameraTransform.GetComponentInParent<AAACameraController>();
            }
            
            if (enableDebugLogging)
            {
                if (_cameraController != null)
                {
                    Debug.Log($"[HandFiringMechanics] Camera controller found for {(_isPrimaryHand ? "Primary" : "Secondary")} hand shake effects");
                }
                else
                {
                    Debug.LogWarning($"[HandFiringMechanics] No AAACameraController found for {(_isPrimaryHand ? "Primary" : "Secondary")} hand - camera shake disabled");
                }
            }
        }
        
        // CRITICAL FIX: Ensure emitPoint is assigned for upgraded hands
        if (emitPoint == null)
        {
            if (!AutoAssignEmitPoint())
            {
                enabled = false; // Disable component to prevent errors
                return;
            }
        }
        
    }
    
    /// <summary>
    /// Automatically finds and assigns emitPoint for upgraded hands
    /// </summary>
    /// <returns>True if emitPoint was successfully assigned</returns>
    private bool AutoAssignEmitPoint()
    {
        // Try common emitPoint names and locations
        string[] emitPointNames = { "EmitPoint", "FirePoint", "MuzzlePoint", "VFXPoint", "Emit Point" };
        
        foreach (string pointName in emitPointNames)
        {
            Transform found = transform.Find(pointName);
            if (found != null)
            {
                emitPoint = found;
                return true;
            }
        }
        
        // If no named emitPoint found, look for any child transform
        Transform[] children = GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            if (child != transform && child.name.ToLower().Contains("emit"))
            {
                emitPoint = child;
                return true;
            }
        }
        
        // Last resort: use the hand transform itself
        if (emitPoint == null)
        {
            emitPoint = transform;
            return true;
        }
        
        return false; // Failed to assign emitPoint
    }

    public void ApplyConfig(HandLevelSO config)
    {
        if (config == null)
        {
            return;
        }
        
        _currentConfig = config;
        
        // Stop any active effects from the previous config
        if (_isCurrentlyStreaming)
        {
            StopStream();
        }
        
        // CRITICAL FIX: Validate VFX setup after config change
        ValidateVFXSetup();
    }
    
    /// <summary>
    /// Validates that VFX can work properly after hand upgrades
    /// </summary>
    private void ValidateVFXSetup()
    {
        if (_currentConfig == null)
        {
            return;
        }
        
        // Check emitPoint assignment
        if (emitPoint == null)
        {
            // Try to find EmitPoint as a child transform
            Transform foundEmitPoint = transform.Find("EmitPoint");
            if (foundEmitPoint == null)
            {
                foundEmitPoint = GetComponentInChildren<Transform>();
            }
            emitPoint = foundEmitPoint;
        }
        
        // FIXED: Validate ACTUAL stream VFX being used (streamVFX), not deprecated streamBeamPrefab
        if (_currentConfig.streamVFX == null)
        {
            Debug.LogWarning($"[HandFiringMechanics] streamVFX is null in config {_currentConfig.name} - beam weapons will not work");
        }
        
        if (_currentConfig.hasShotgunMode && _currentConfig.legacyShotgunVFX == null)
        {
        }
        else if (_currentConfig.hasShotgunMode)
        {
        }
    }

    #endregion

    // SetStreamVolumeDucked method removed - audio is now managed by PlayerShooterOrchestrator

    #region Firing API (Called by Orchestrator)

    public void TryStartStream()
    {
        // Early validation checks
        if (_isCurrentlyStreaming) return;
        
        if (!ValidateStreamingRequirements())
        {
            return;
        }

        _isCurrentlyStreaming = true;
        _nextStreamDamageTime = Time.time; // Deal damage immediately on start
        _currentStreamTargets.Clear(); // Reset target tracking

        // Instantiate LEGACY STREAM VFX (the one you're actually using)
        if (emitPoint != null && _currentConfig.streamVFX != null)
        {
            try
            {
                // CRITICAL FIX: Calculate fire direction from camera (center screen)
                Vector3 fireDirection = GetFireDirection();
                Quaternion beamRotation = Quaternion.LookRotation(fireDirection);
                
                // ✅ PERFECT FIX: Parent to emit point for INSTANT position tracking (zero lag!)
                // Particles follow emit point automatically via transform hierarchy
                GameObject legacyStreamEffect = Instantiate(_currentConfig.streamVFX, emitPoint.position, beamRotation, emitPoint);
                
                // Check particle systems in legacy stream VFX
                ParticleSystem[] legacyStreamParticles = legacyStreamEffect.GetComponentsInChildren<ParticleSystem>();
                
                foreach (var ps in legacyStreamParticles)
                {
                    if (ps != null)
                    {
                        var main = ps.main;
                        
                        // CRITICAL: Use world space so particles don't spin with hand rotation
                        // But position is inherited from parent transform (emitPoint)
                        main.simulationSpace = ParticleSystemSimulationSpace.World;
                        
                        // Force start the particle system if it's not playing
                        if (!ps.isPlaying)
                        {
                            ps.Play();
                        }
                    }
                }
                
                // ENHANCED STREAM VFX: Use same robust particle configuration as shotgun
                Vector3 playerVelocity = GetPlayerVelocity();
                if (!ConfigureParticleSystems(legacyStreamEffect, fireDirection, _currentConfig.streamMaxDistance, playerVelocity))
                {
                }
                
                // NO NEED for BeamPositionFollower - parenting handles position automatically!
                // Rotation is updated every frame in LateUpdate() via UpdateBeamRotation()
                
                // Store reference for cleanup when stream stops
                _activeLegacyStreamInstance = legacyStreamEffect;
                
                // REMOVED: Animation triggering is now handled EXCLUSIVELY by PlayerShooterOrchestrator
                // This prevents double-triggering and ensures proper hand mapping after refactor
                // PlayerShooterOrchestrator.HandlePrimaryHoldStarted/HandleSecondaryHoldStarted handle this
                
                // Trigger beam camera shake
                if (_cameraController != null)
                {
                    if (_isPrimaryHand)
                    {
                        _cameraController.StartPrimaryBeamShake();
                    }
                    else
                    {
                        _cameraController.StartSecondaryBeamShake();
                    }
                }
            }
            catch (System.Exception e)
            {
            }
        }
        else
        {
            _isCurrentlyStreaming = false;
            return;
        }
        
        // Audio is now handled by PlayerShooterOrchestrator via GameSounds

        // Inform Overheat Manager
        _overheatManager?.SetHandFiringState(_isPrimaryHand, true);
        
    }

    public void StopStream()
    {
        if (!_isCurrentlyStreaming) return;
        _isCurrentlyStreaming = false;

        // REMOVED: Animation triggering is now handled EXCLUSIVELY by PlayerShooterOrchestrator
        // This prevents double-triggering and ensures proper hand mapping after refactor
        // PlayerShooterOrchestrator.HandlePrimaryHoldEnded/HandleSecondaryHoldEnded handle this

        // Stop beam camera shake
        if (_cameraController != null)
        {
            if (_isPrimaryHand)
            {
                _cameraController.StopPrimaryBeamShake();
            }
            else
            {
                _cameraController.StopSecondaryBeamShake();
            }
        }

        // Clear target tracking
        _currentStreamTargets.Clear();

        // REMOVED: MagicBeamStatic cleanup - that system is deprecated and not used
        
        // Stop emitting new particles but let existing particles finish their lifetime
        if (_activeLegacyStreamInstance != null)
        {
            // Stop all particle systems from emitting (but don't clear existing particles!)
            ParticleSystem[] particleSystems = _activeLegacyStreamInstance.GetComponentsInChildren<ParticleSystem>();
            float maxLifetime = 0f;
            
            foreach (var ps in particleSystems)
            {
                if (ps != null)
                {
                    ps.Stop(false, ParticleSystemStopBehavior.StopEmitting); // Stop emitting, keep existing particles alive!
                    
                    // Track max lifetime to know when to destroy VFX GameObject
                    float psLifetime = ps.main.startLifetime.constantMax;
                    if (psLifetime > maxLifetime)
                    {
                        maxLifetime = psLifetime;
                    }
                }
            }
            
            // Destroy VFX GameObject after all particles have finished (add 0.5s buffer)
            Destroy(_activeLegacyStreamInstance, maxLifetime + 0.5f);
            _activeLegacyStreamInstance = null;
        }
        
        // Notify PlayerShooterOrchestrator to stop the streaming audio
        if (PlayerShooterOrchestrator.Instance != null)
        {
            if (_isPrimaryHand)
            {
                PlayerShooterOrchestrator.Instance.StopPrimaryStreamAudio();
            }
            else
            {
                PlayerShooterOrchestrator.Instance.StopSecondaryStreamAudio();
            }
        }

        // Inform Overheat Manager
        _overheatManager?.SetHandFiringState(_isPrimaryHand, false);
    }



    /// <summary>
    /// Try to fire shotgun - returns true if successfully fired, false if on cooldown or invalid config
    /// </summary>
    /// <param name="handLevel">The current hand level for sound variation</param>
    /// <param name="soundVolume">Volume for sound effect</param>
    /// <returns>True if shotgun fired, false if on cooldown or invalid config</returns>
    public bool TryFireShotgun(int handLevel = 0, float soundVolume = 1.0f)
    {
        // Validate requirements first
        if (!ValidateShotgunRequirements())
        {
            return false;
        }

        if (_isCurrentlyStreaming)
        {
            StopStream();
        }

        // We've passed all validation checks - set the next fire time
        _nextShotgunFireTime = Time.time + _currentConfig.shotgunCooldown;

        // Use GetFireDirection() for PERFECT center-screen accuracy (same as beam system)
        Vector3 fireDirection = GetFireDirection();
        Vector3 raycastOrigin = GetRaycastOrigin(); // FIXED: Use proper origin method instead of hardcoded emitPoint

        // Animation is triggered directly by PlayerShooterOrchestrator for better timing control
        // Removed duplicate animation call to prevent conflicts

        // UNIFIED SHOTGUN VFX SYSTEM - Fire new prefab VFX
        FireShotgunEffects(fireDirection, _currentConfig.shotgunMaxDistance);
        
        // Trigger shotgun camera shake
        if (_cameraController != null)
        {
            if (_isPrimaryHand)
            {
                _cameraController.TriggerPrimaryShotgunShake();
            }
            else
            {
                _cameraController.TriggerSecondaryShotgunShake();
            }
        }
        
        // Audio is now handled by PlayerShooterOrchestrator via GameSounds API

        // Perform improved hit detection using HandLevelSO settings
        // Debug layer mask to see what layers are included
        int gemLayer = LayerMask.NameToLayer("gems");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        bool hasGems = (_currentConfig.damageLayerMask.value & (1 << gemLayer)) != 0;
        bool hasEnemy = (_currentConfig.damageLayerMask.value & (1 << enemyLayer)) != 0;
        
        PerformHitDetection(raycastOrigin, fireDirection, _currentConfig.shotgunDamage, _currentConfig.shotgunDamageRadius, _currentConfig.shotgunMaxDistance, _currentConfig.damageLayerMask, "SHOTGUN");
        
        // Add heat if overheat manager exists
        _overheatManager?.AddHeatToHand(_isPrimaryHand, _overheatManager.shotgunHeatCost);
        
        // Return true since we successfully fired
        return true;
    }

    private void FireShotgunEffects(Vector3 fireDirection, float maxDistance)
    {
        
        // Simple legacy burst only (no projectile/magic missile)
        if (emitPoint == null)
        {
            return;
        }

        // Only use legacy shotgun VFX - no fallbacks
        GameObject prefab = _currentConfig.legacyShotgunVFX;
        if (prefab == null)
        {
            return;
        }
        
        CreateShotgunVFX(prefab, fireDirection, maxDistance);
    }

    private void CreateShotgunVFX(GameObject vfxPrefab, Vector3 fireDirection, float maxDistance)
    {
        try
        {
            // CRITICAL: Use ONLY raycast direction, completely ignore animated emitPoint rotation
            // Particles follow raycast (screen center), NOT arm animation
            Quaternion raycastRotation = Quaternion.LookRotation(fireDirection);
            GameObject go = Instantiate(vfxPrefab, emitPoint.position, raycastRotation);
            if (!go.activeSelf) go.SetActive(true);

            // Debug.Log($"[ShotgunVFX] RAYCAST Direction: {fireDirection}, ignoring arm animation");

            // Configure all particle systems to ONLY follow raycast direction
            var systems = go.GetComponentsInChildren<ParticleSystem>(true);
            float maxLifetime = 0f;
            
            // ⚡ CRITICAL FIX: Store detached particle systems for manual cleanup
            List<ParticleSystem> detachedParticles = new List<ParticleSystem>();
            
            // ⚡ PERFORMANCE: Enforce max active VFX limit
            if (_activeShotgunVFX.Count >= maxActiveShotgunVFX)
            {
                // Force cleanup oldest VFX
                GameObject oldestVFX = _activeShotgunVFX[0];
                List<ParticleSystem> oldestParticles = _activeDetachedParticles.Count > 0 ? _activeDetachedParticles[0] : null;
                
                if (oldestVFX != null)
                {
                    Destroy(oldestVFX);
                }
                
                if (oldestParticles != null)
                {
                    foreach (var ps in oldestParticles)
                    {
                        if (ps != null && ps.gameObject != null)
                        {
                            Destroy(ps.gameObject);
                        }
                    }
                }
                
                _activeShotgunVFX.RemoveAt(0);
                if (_activeDetachedParticles.Count > 0)
                {
                    _activeDetachedParticles.RemoveAt(0);
                }
                
                Debug.LogWarning($"[ShotgunVFX] ⚠️ Max VFX limit ({maxActiveShotgunVFX}) reached - force cleaned oldest VFX");
            }
            
            // Track this VFX
            _activeShotgunVFX.Add(go);
            _activeDetachedParticles.Add(detachedParticles);
            
            foreach (var ps in systems)
            {
                if (ps != null)
                {
                    // FORCE world space - completely detached from arm animation
                    var main = ps.main;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                    
                    // FORCE exact raycast rotation - ignore any parent transforms
                    ps.transform.rotation = raycastRotation;
                    ps.transform.SetParent(null); // Completely detach from any animated hierarchy
                    
                    // ⚡ CRITICAL: Track detached particles for cleanup
                    detachedParticles.Add(ps);
                    
                    // Configure shape to emit ONLY in raycast direction
                    var shape = ps.shape;
                    if (shape.enabled)
                    {
                        shape.rotation = Vector3.zero;
                        shape.alignToDirection = true; // Align particles to emission direction
                    }
                    
                    // OVERRIDE any velocity - use ONLY raycast direction
                    var velocityOverLifetime = ps.velocityOverLifetime;
                    velocityOverLifetime.enabled = true;
                    velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
                    
                    // FORCE particles to follow raycast direction, not arm animation
                    float speed = 150f;
                    velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(fireDirection.x * speed);
                    velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(fireDirection.y * speed);
                    velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(fireDirection.z * speed);
                    
                    // Disable any force modules that might interfere
                    var forceOverLifetime = ps.forceOverLifetime;
                    forceOverLifetime.enabled = false;
                    
                    // Ensure no inheritance from animated transforms
                    var inheritVelocity = ps.inheritVelocity;
                    inheritVelocity.enabled = false;
                    
                    ps.Play(true);
                    
                    // Track the longest particle lifetime for cleanup timing
                    float particleLifetime = main.startLifetime.constantMax + main.duration;
                    if (particleLifetime > maxLifetime)
                    {
                        maxLifetime = particleLifetime;
                    }
                    
                    Debug.Log($"[ShotgunVFX] Particle {ps.name} DECOUPLED from arm animation, following raycast only");
                }
            }
            
            // CRITICAL FIX: Schedule automatic cleanup after all particles finish
            // Add extra buffer time to ensure all particles are completely finished
            float cleanupDelay = maxLifetime + 2f; // 2 second buffer
            StartCoroutine(CleanupShotgunVFXAfterDelay(go, cleanupDelay));
            
            // ⚡ CRITICAL: Also cleanup detached particles separately
            StartCoroutine(CleanupDetachedParticles(detachedParticles, cleanupDelay));
            
            Debug.Log($"[ShotgunVFX] Scheduled cleanup in {cleanupDelay} seconds for {go.name} + {detachedParticles.Count} detached particles");
            
            return;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ShotgunVFX] Error creating shotgun VFX: {e.Message}");
        }
    }



    /// <summary>
    /// Validates all requirements for streaming weapons
    /// </summary>
    private bool ValidateStreamingRequirements()
    {
        if (_currentConfig == null)
        {
            return false;
        }
        
        // FIXED: Check for streamVFX (the actual VFX being used), NOT deprecated streamBeamPrefab
        if (_currentConfig.streamVFX == null)
        {
            return false;
        }
        
        if (emitPoint == null)
        {
            return false;
        }
        
        if (_cameraTransform == null)
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Validates all requirements for shotgun weapons
    /// </summary>
    private bool ValidateShotgunRequirements()
    {
        if (_currentConfig == null)
        {
            return false;
        }
        
        if (_currentConfig.shotgunDamage <= 0)
        {
        }
        
        if (emitPoint == null)
        {
            return false;
        }
        
        if (_cameraTransform == null)
        {
            return false;
        }
        
        // Check cooldown
        if (Time.time < _nextShotgunFireTime)
        {
            // Exception occurred in cone calculation
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Configures ONLY the cone/secondary particle systems - leaves LineRenderer beam alone!
    /// </summary>
    /// <returns>True if configuration was successful</returns>
    private bool ConfigureParticleSystems(GameObject effect, Vector3 fireDirection, float maxDistance, Vector3 playerVelocity)
    {
        // DISABLED: Let particle systems spawn with their original prefab settings
        // No radius, speed, or size modifications - just natural spawning
        
        // Hand level calculation complete
        
        return true; // Always return success since we're not modifying anything
    }
    
    /// <summary>
    /// Gets the current hand level (1-4) from config name or defaults to 1
    /// </summary>
    private int GetHandLevel()
    {
        if (_currentConfig == null) return 1;
        
        string configName = _currentConfig.name.ToLower();
        
        // Extract level from config names like "Hand_Level_2" or "HandLevel3" etc.
        if (configName.Contains("level_4") || configName.Contains("level4") || configName.Contains("l4"))
            return 4;
        if (configName.Contains("level_3") || configName.Contains("level3") || configName.Contains("l3"))
            return 3;
        if (configName.Contains("level_2") || configName.Contains("level2") || configName.Contains("l2"))
            return 2;
        if (configName.Contains("level_1") || configName.Contains("level1") || configName.Contains("l1"))
            return 1;
            
        // Fallback: assume level 1
        return 1;
    }
    
    /// <summary>
    /// Gets the cone radius from HandLevelSO config (with fallback)
    /// </summary>
    private float GetConeRadiusFromConfig()
    {
        if (_currentConfig == null)
        {
            return 0.5f; // Fallback radius
        }
        
        // Try to get coneRadius field using reflection (for compatibility)
        try
        {
            var coneRadiusField = _currentConfig.GetType().GetField("coneRadius");
            if (coneRadiusField != null)
            {
                float configRadius = (float)coneRadiusField.GetValue(_currentConfig);
                // Exception occurred in cone calculation
                return configRadius;
            }
        }
        catch (System.Exception e)
        {
            // Exception occurred in cone calculation
        }
        
        // Fallback: Use shotgunDamageRadius as a base or calculate from hand level
        if (_currentConfig.shotgunDamageRadius > 0)
        {
            // Use a fraction of shotgun damage radius as cone radius
            float fallbackRadius = _currentConfig.shotgunDamageRadius * 0.1f; // 10% of shotgun damage radius
            // Exception occurred in cone calculation
            return fallbackRadius;
        }
        
        // Final fallback: Hand level based calculation
        int handLevel = GetHandLevel();
        float finalFallback = 0.1f + (handLevel * 0.15f);
        // Hand level calculation complete
        return finalFallback;
    }
    
    /// <summary>
    /// Determines if a particle system is the secondary single particle system to EXCLUDE from speed modifications
    /// (These should remain visible with their original settings)
    /// </summary>
    private bool IsSecondaryParticleSystem(ParticleSystem ps)
    {
        if (ps == null) return false;
        
        string name = ps.name.ToLower();
        
        // Look for specific secondary single particle system patterns to EXCLUDE from speed changes
        // These patterns identify the "normal single particle vfx" that should remain visible
        return name.Contains("l4_particle 1") || 
               name.Contains("l3_particle 1") ||
               name.Contains("l2_particle 1") ||
               name.Contains("l1_particle 1") ||
               name.Contains("single_particle") ||
               name.Contains("secondary_particle") ||
               (name.Contains("particle") && name.Contains(" 1") && !name.Contains("system"));
    }
    
    /// <summary>
    /// Configures particle system with 250 forward speed (applies to ALL particles except secondary single)
    /// </summary>
    private bool ConfigureParticleSystemForward(ParticleSystem ps, Vector3 fireDirection, float maxDistance, int handLevel)
    {
        try
        {
            // Validate particle system state
            if (ps == null || ps.transform == null)
            {
                return false;
            }
            
            // Orient the particle system
            ps.transform.rotation = Quaternion.LookRotation(fireDirection);
            
            var main = ps.main;
            var shape = ps.shape;
            var velocityOverLifetime = ps.velocityOverLifetime;
            
            // Check if this is a cone/trail particle that needs radius scaling
            bool isConeParticle = ps.name.ToLower().Contains("cone") || 
                                 ps.name.ToLower().Contains("trail") || 
                                 ps.name.ToLower().Contains("spread");
            
            // Exception occurred in cone calculation
            {
                string particleType = isConeParticle ? "CONE/TRAIL" : "GENERAL";
            }
            
            // Configure shape for cone particles only
            if (isConeParticle && shape.enabled)
            {
                // USE HANDLEVELSO CONE RADIUS - Perfect control for each hand level!
                float coneRadius = GetConeRadiusFromConfig();
                
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = coneRadius; // Use radius directly from HandLevelSO!
                shape.radiusThickness = 0f; // Emit from center
                shape.randomDirectionAmount = 0.2f; // Slight spread
                shape.sphericalDirectionAmount = 0f;
                
                // Exception occurred in cone calculation
            }
            
            // Configure basic properties
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startSpeed = 250f; // Half speed - perfect balance
            main.startLifetime = Mathf.Clamp(maxDistance / 50f, 0.5f, 2f);
            
            // Note: Overall prefab scaling handles size - no need for individual particle scaling
            
            // Configure velocity for expanding cone
            if (velocityOverLifetime.enabled)
            {
                velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
                // Create slight expansion over lifetime
                float expansionSpeed = handLevel * 2f; // More expansion for higher levels
                velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-expansionSpeed, expansionSpeed);
                velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-expansionSpeed, expansionSpeed);
                velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(250f); // Half speed - perfect balance
            }
            
            // Restart particle system with new settings
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play();
            
            // Exception occurred in cone calculation
            
            return true;
        }
        catch (System.Exception e)
        {
            return false;
        }
    }
    
    
    /// <summary>
    /// Gets the shotgun particle speed multiplier based on hand level.
    /// Level 1 = fast (50), Level 2 = faster (100), Level 3 = very fast (200), Level 4 = hyperspeed (400)
    /// </summary>
    private float GetShotgunSpeedForHandLevel(int handLevel)
    {
        switch (handLevel)
        {
            case 1:
                return 50f; // Fast
            case 2:
                return 100f; // Faster
            case 3:
                return 200f; // Very fast
            case 4:
                return 400f; // Hyperspeed
            default:
                return 50f;
        }
    }

    /// <summary>
    /// Gets the shotgun effect scale multiplier based on hand level.
    /// Level 1 = 2.5x (base), Level 2 = 5x, Level 3 = 10x, Level 4 = 15x
    /// </summary>
    private float GetShotgunScaleForHandLevel(int handLevel)
    {
        float baseScale = 2.5f; // Base scale for level 1
        
        switch (handLevel)
        {
            case 1:
                return baseScale; // 2.5x
            case 2:
                return baseScale * 2f; // 5x (2x the base)
            case 3:
                return baseScale * 4f; // 10x (4x the base)
            case 4:
                return baseScale * 6f; // 15x (6x the base)
            default:
                return baseScale;
        }
    }

    private Vector3 GetPlayerVelocity()
    {
        // 1) CharacterController velocity (most accurate for kinematic controllers)
        CharacterController controller = GetComponentInParent<CharacterController>();
        if (controller != null)
        {
            return controller.velocity;
        }
        
        // 2) Rigidbody velocity
        Rigidbody playerRigidbody = GetComponentInParent<Rigidbody>();
        if (playerRigidbody != null)
        {
            if (TryGetRigidbodyVelocity(playerRigidbody, out var vel))
                return vel;
        }
        
        // 3) Fallback: approximate using root transform displacement over time
        Transform root = transform.root != null ? transform.root : transform;
        float now = Time.time;
        if (_lastVelSampleTime < 0f)
        {
            _lastRootPosForVel = root.position;
            _lastVelSampleTime = now;
            return Vector3.zero;
        }
        float dt = now - _lastVelSampleTime;
        if (dt > 0.0001f)
        {
            _cachedApproxVelocity = (root.position - _lastRootPosForVel) / dt;
            _lastRootPosForVel = root.position;
            _lastVelSampleTime = now;
        }
        return _cachedApproxVelocity;
    }

    // Rigidbody helper to read velocity; prefers 'linearVelocity' if available
    private bool TryGetRigidbodyVelocity(Rigidbody rb, out Vector3 velocity)
    {
        velocity = Vector3.zero;
        if (rb == null) return false;
        // Prefer linearVelocity if available; otherwise use standard velocity
        var linProp = rb.GetType().GetProperty("linearVelocity", BindingFlags.Public | BindingFlags.Instance);
        if (linProp != null && linProp.CanRead)
        {
            try
            {
                object value = linProp.GetValue(rb, null);
                if (value is Vector3 vec)
                {
                    velocity = vec;
                    return true;
                }
            }
            catch { /* ignore and fall back */ }
        }
        velocity = rb.linearVelocity;
        return true;
    }
    
    #endregion

    #region Update Loop

    void Update()
    {
        // Early exit if component is disabled or missing critical references
        if (!enabled || _currentConfig == null) return;
        
        // ⚡ MEMORY LEAK FIX: Periodic cleanup of null references from static lists
        if (Time.time - _lastStaticListCleanupTime >= STATIC_LIST_CLEANUP_INTERVAL)
        {
            CleanupNullReferencesFromStaticLists();
            _lastStaticListCleanupTime = Time.time;
        }
        
        // Handle damage timing in Update (doesn't need LateUpdate)
        if (_isCurrentlyStreaming && _cameraTransform != null && emitPoint != null)
        {
            // Apply damage at consistent intervals (improved timing)
            if (Time.time >= _nextStreamDamageTime)
            {
                Vector3 fireDirection = GetFireDirection();
                ApplyStreamDamage(fireDirection);
                // Use a consistent interval instead of Hz-based calculation
                _nextStreamDamageTime = Time.time + (1f / Mathf.Max(_currentConfig.streamDamageRateHz, 1f));
            }
        }
    }
    
    // ✅ CRITICAL FIX: Move beam rotation update to LateUpdate for zero-delay tracking
    // This ensures rotation updates AFTER all animations complete, eliminating visible lag
    void LateUpdate()
    {
        // Only update beam rotation if actively streaming and all components are valid
        if (_isCurrentlyStreaming && _cameraTransform != null && emitPoint != null)
        {
            UpdateBeamRotation();
        }
    }
    
    /// <summary>
    /// ✅ ZERO-DELAY FIX: Updates beam rotation in LateUpdate (after all animations)
    /// This ensures perfect tracking even when moving super fast
    /// Position is auto-synced via parent transform (emitPoint), rotation follows camera
    /// </summary>
    private void UpdateBeamRotation()
    {
        if (_cameraTransform == null)
        { 
            StopStream();
            return;
        }

        // Calculate the fire direction with the angle offset
        Vector3 fireDirection = GetFireDirection();

        // ✅ PERFECT SYNC: Beam is parented to emitPoint (position auto-updates), we only update rotation
        if (_activeLegacyStreamInstance != null)
        {
            // Update beam local rotation to point where you're looking (relative to emit point parent)
            // Use localRotation since beam is now parented to emitPoint
            Quaternion targetWorldRotation = Quaternion.LookRotation(fireDirection);
            _activeLegacyStreamInstance.transform.rotation = targetWorldRotation;
            
            // Also update all particle system rotations to match camera direction
            ParticleSystem[] particles = _activeLegacyStreamInstance.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                if (ps != null)
                {
                    // Keep particles pointing in camera direction (world rotation)
                    ps.transform.rotation = targetWorldRotation;
                }
            }
        }
    }

    /// <summary>
    /// Applies stream damage to targets in the firing direction using HandLevelSO settings
    /// </summary>
    private void ApplyStreamDamage(Vector3 fireDirection)
    {
        // COPY EXACT SHOTGUN WORKING SETUP - Simple and direct approach
        Vector3 fireDirection_fixed = _cameraTransform != null ? _cameraTransform.forward : emitPoint.forward;
        Vector3 raycastOrigin = GetRaycastOrigin(); // FORCE CAMERA ORIGIN: Perfect close combat for streams too
        
        // Calculate damage per tick using HandLevelSO settings
        float damageThisTick = _currentConfig.streamDamagePerSecond / Mathf.Max(_currentConfig.streamDamageRateHz, 1f);
        
        // DEBUG: Show HandLevelSO values being used
        if (Time.frameCount % 60 == 0) // Every second at 60fps
        {
            Debug.Log($"[HandFiringMechanics] HANDLEVELSO CONFIG VALUES:\n" +
                      $"streamDamageRadius: {_currentConfig.streamDamageRadius}\n" +
                      $"streamMaxDistance: {_currentConfig.streamMaxDistance}\n" +
                      $"streamDamagePerSecond: {_currentConfig.streamDamagePerSecond}\n" +
                      $"Using SHOTGUN-style raycast (simple & working)\n" +
                      $"Config name: {_currentConfig.name}");
        }
        
        // Perform improved hit detection using EXACT SHOTGUN SETUP (same as working shotgun mode)
        PerformHitDetection(raycastOrigin, fireDirection_fixed, damageThisTick, _currentConfig.streamDamageRadius, _currentConfig.streamMaxDistance, _currentConfig.damageLayerMask, "STREAM");
    }
    
    /// <summary>
    /// Gets the raycast origin - starts 200 units behind camera for large spherecast (radius 350)
    /// </summary>
    private Vector3 GetRaycastOrigin()
    {
        if (useEmitPointAsOrigin)
        {
            // Use emit point for more realistic but less accurate shooting
            return emitPoint != null ? emitPoint.position : transform.position;
        }
        else
        {
            // Start 200 units behind camera for large spherecast coverage
            if (_cameraTransform != null)
            {
                Vector3 cameraPos = _cameraTransform.position;
                Vector3 cameraForward = _cameraTransform.forward;
                
                // Start raycast 250 units BEHIND camera position for spherecast radius 350
                Vector3 backwardOrigin = cameraPos - (cameraForward * 350f);
                
                return backwardOrigin;
            }
        }
        
        return transform.position; // Fallback
    }
    
    /// <summary>
    /// Calculates fire direction - AAA PARALLEL SHOOTING (no crossover)
    /// Both hands shoot in SAME direction (camera forward) for perfect dual-wielding
    /// </summary>
    private Vector3 GetFireDirection()
    {
        if (_cameraTransform == null) return Vector3.forward;
        
        Camera camera = _cameraTransform.GetComponent<Camera>();
        if (camera != null)
        {
            // AAA SOLUTION: Use camera forward direction for PARALLEL shooting
            // Both hands shoot in EXACT SAME DIRECTION - no convergence, no crossover
            Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            Ray centerRay = camera.ScreenPointToRay(screenCenter);
            
            // Return the ray DIRECTION (not converging to a point)
            // This ensures both hands shoot parallel toward screen center
            Vector3 parallelDirection = centerRay.direction.normalized;
            
            // Debug.Log($"[GetFireDirection] Parallel direction: {parallelDirection} (no crossover)");
            
            return parallelDirection;
        }
        
        // Fallback to camera forward (also parallel)
        Vector3 fallback = _cameraTransform.forward;
        Debug.Log($"[GetFireDirection] Fallback parallel direction: {fallback}");
        return fallback;
    }
    
    
    
    /// <summary>
    /// Improved hit detection system with consistent accuracy across all ranges using HandLevelSO settings
    /// </summary>
    private void PerformHitDetection(Vector3 origin, Vector3 direction, float damage, float radius, float maxDistance, LayerMask layerMask, string weaponType)
    {
        // DEBUG: Show actual raycast parameters being used
        Debug.Log($"[HandFiringMechanics] {weaponType} RAYCAST DEBUG:\n" +
                  $"Origin: {origin}\n" +
                  $"Direction: {direction}\n" +
                  $"Radius: {radius}\n" +
                  $"MaxDistance: {maxDistance}\n" +
                  $"LayerMask: {layerMask.value}");
        
        // Use the reusable buffer to reduce allocations
        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            radius,
            direction,
            _hitBuffer,
            maxDistance,
            layerMask
        );
        
        Debug.Log($"[HandFiringMechanics] SphereCast hit {hitCount} objects");
        
        
        // Process all hits
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _hitBuffer[i];
            
            // Validate hit distance for consistency
            float hitDistance = Vector3.Distance(origin, hit.point);
            if (hitDistance > maxDistance) continue;
            
            // REMOVED: Extra radius validation that was rejecting close hits with large radius
            // The SphereCast already validates radius - no need for double-checking
            
            // SIMPLIFIED: Line-of-sight check only for distant targets to avoid camera geometry issues
            // Close targets (within 5 units) bypass line-of-sight for perfect close combat
            if (hitDistance > 5f && !HasClearLineOfSight(origin, hit.point, hit.collider))
            {
                continue;
            }
            
            if (hit.transform.TryGetComponent<IDamageable>(out var damageable))
            {
                // For stream weapons, avoid duplicate hits on the same target in rapid succession
                if (weaponType == "STREAM")
                {
                    if (_currentStreamTargets.Contains(hit.collider))
                        continue;
                    
                    _currentStreamTargets.Add(hit.collider);
                    // Remove targets after a short delay to allow re-hitting (using config-based timing)
                    float removeDelay = (1f / Mathf.Max(_currentConfig.streamDamageRateHz, 1f)) * 0.5f;
                    StartCoroutine(RemoveStreamTargetAfterDelay(hit.collider, removeDelay));
                }
                
                damageable.TakeDamage(damage, hit.point, direction);
                
            }
        }
        
        if (hitCount == 0)
        {
        }
    }
    
    /// <summary>
    /// Checks if there's a clear line-of-sight between the shooter and target (no towers blocking)
    /// </summary>
    private bool HasClearLineOfSight(Vector3 origin, Vector3 targetPoint, Collider targetCollider)
    {
        // Calculate direction and distance to target
        Vector3 directionToTarget = (targetPoint - origin).normalized;
        float distanceToTarget = Vector3.Distance(origin, targetPoint);
        
        // Perform a raycast to check for obstructions
        // We use a slightly shorter distance to avoid hitting the target itself
        float checkDistance = distanceToTarget - 0.1f;
        
        // Create a layer mask that includes towers but excludes gems and other shootable objects
        // We want to block shots by towers but allow shooting gems and enemies
        LayerMask obstructionMask = LayerMask.GetMask("Default"); // Adjust layer names as needed
        
        if (Physics.Raycast(origin, directionToTarget, out RaycastHit hit, checkDistance, obstructionMask))
        {
            // If we hit something, check if it's a tower or other obstruction
            TowerController tower = hit.collider.GetComponent<TowerController>();
            if (tower != null)
            {
                // We hit a tower - line of sight is blocked
                return false;
            }
            
            // Check if we hit a platform or other solid geometry that should block shots
            // You can add more obstruction checks here based on your game's layer setup
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Default") ||
                hit.collider.GetComponent<CelestialPlatform>() != null ||
                hit.collider.name.ToLower().Contains("platform"))
            {
                return false;
            }
        }
        
        // No obstructions found - line of sight is clear
        return true;
    }
    
    /// <summary>
    /// Removes a target from the stream target set after a delay to allow re-hitting
    /// </summary>
    private System.Collections.IEnumerator RemoveStreamTargetAfterDelay(Collider target, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (target != null)
        {
            _currentStreamTargets.Remove(target);
        }
    }
    
    /// <summary>
    /// CRITICAL FIX: Destroys shotgun VFX GameObject after particles finish to prevent memory leaks
    /// </summary>
    private System.Collections.IEnumerator CleanupShotgunVFXAfterDelay(GameObject vfxObject, float delay)
    {
        if (vfxObject == null)
        {
            Debug.LogWarning("[CleanupShotgunVFX] VFX object is null - cannot cleanup");
            yield break;
        }
        
        Debug.Log($"[CleanupShotgunVFX] Starting cleanup timer for {vfxObject.name} - will destroy in {delay} seconds");
        
        yield return new WaitForSeconds(delay);
        
        // Double-check the object still exists before destroying
        if (vfxObject != null)
        {
            string objectName = vfxObject.name;
            
            // Stop all particle systems before destroying to ensure clean shutdown
            ParticleSystem[] particleSystems = vfxObject.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                if (ps != null && ps.isPlaying)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
            
            // Remove from tracking list
            _activeShotgunVFX.Remove(vfxObject);
            
            // Destroy the GameObject and all its children
            Destroy(vfxObject);
            
            // Debug.Log($"[CleanupShotgunVFX] Successfully destroyed shotgun VFX: {objectName}");
        }
        else
        {
            Debug.Log("[CleanupShotgunVFX] VFX object was already destroyed - cleanup complete");
        }
    }
    
    /// <summary>
    /// ⚡ CRITICAL FIX: Destroys detached particle systems that were orphaned by SetParent(null)
    /// This prevents the 484k triangle buildup from orphaned particles
    /// </summary>
    private System.Collections.IEnumerator CleanupDetachedParticles(List<ParticleSystem> particles, float delay)
    {
        if (particles == null || particles.Count == 0)
        {
            yield break;
        }
        
        Debug.Log($"[CleanupDetachedParticles] Tracking {particles.Count} detached particles for cleanup in {delay} seconds");
        
        yield return new WaitForSeconds(delay);
        
        int cleanedCount = 0;
        foreach (var ps in particles)
        {
            if (ps != null && ps.gameObject != null)
            {
                // Stop the particle system
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                
                // Destroy the GameObject (not just the component)
                Destroy(ps.gameObject);
                cleanedCount++;
            }
        }
        
        // Remove from tracking list
        _activeDetachedParticles.Remove(particles);
        
        // Debug.Log($"[CleanupDetachedParticles] ✅ Cleaned up {cleanedCount}/{particles.Count} detached particle systems");
    }

    #endregion

    void OnDisable()
    {
        StopStream();
        
        // ⚡ MEMORY LEAK FIX: Clean up static lists when component is disabled
        CleanupNullReferencesFromStaticLists();
    }
    
    void OnDestroy()
    {
        // ⚡ MEMORY LEAK FIX: Final cleanup on destroy
        CleanupNullReferencesFromStaticLists();
    }
    
    /// <summary>
    /// ⚡ MEMORY LEAK FIX: Removes null references from static lists to prevent memory leaks
    /// This handles edge cases where GameObjects are destroyed externally or during scene transitions
    /// </summary>
    private static void CleanupNullReferencesFromStaticLists()
    {
        int nullVFXCount = 0;
        int nullParticleListCount = 0;
        
        // Clean up null VFX GameObjects
        for (int i = _activeShotgunVFX.Count - 1; i >= 0; i--)
        {
            if (_activeShotgunVFX[i] == null)
            {
                _activeShotgunVFX.RemoveAt(i);
                nullVFXCount++;
            }
        }
        
        // Clean up null particle lists and null particles within lists
        for (int i = _activeDetachedParticles.Count - 1; i >= 0; i--)
        {
            List<ParticleSystem> particleList = _activeDetachedParticles[i];
            
            if (particleList == null)
            {
                _activeDetachedParticles.RemoveAt(i);
                nullParticleListCount++;
                continue;
            }
            
            // Clean up null particles within the list
            bool hasNullParticles = false;
            for (int j = particleList.Count - 1; j >= 0; j--)
            {
                if (particleList[j] == null || particleList[j].gameObject == null)
                {
                    particleList.RemoveAt(j);
                    hasNullParticles = true;
                }
            }
            
            // If the list is now empty, remove it
            if (particleList.Count == 0)
            {
                _activeDetachedParticles.RemoveAt(i);
                nullParticleListCount++;
            }
        }
        
        // Log cleanup results if any nulls were found
        if (nullVFXCount > 0 || nullParticleListCount > 0)
        {
            Debug.Log($"[HandFiringMechanics] ✅ Static list cleanup: Removed {nullVFXCount} null VFX objects and {nullParticleListCount} null/empty particle lists");
        }
    }
    
}