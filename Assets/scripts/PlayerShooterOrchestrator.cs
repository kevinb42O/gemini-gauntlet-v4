// --- PlayerShooterOrchestrator.cs (SIMPLIFIED - Event Driven) ---
using UnityEngine;
using GeminiGauntlet.Audio;
using GeminiGauntlet.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Central command for player shooting. It receives input events, determines the current hand level,
/// and delegates actions to the appropriate HandFiringMechanics script. It is the SOLE authority
/// for triggering weapon audio via GameSounds, ensuring tiered sounds are played correctly.
/// </summary>
public class PlayerShooterOrchestrator : MonoBehaviour
{
    public static PlayerShooterOrchestrator Instance { get; private set; }
    [Header("Core References")]
    public PlayerInputHandler inputHandler;
    public HandFiringMechanics primaryHandMechanics;
    public HandFiringMechanics secondaryHandMechanics;
    public PlayerOverheatManager overheatManager;
    
    [Header("3D Audio")]
    [Tooltip("AudioSource on LEFT hand (primary) for 3D positional shooting sounds")]
    public AudioSource primaryHandAudioSource;
    [Tooltip("AudioSource on RIGHT hand (secondary) for 3D positional shooting sounds")]
    public AudioSource secondaryHandAudioSource;

    [Header("Visuals")]
    private Transform _cameraTransform;

    [Header("Hand Configurations")]
    [Tooltip("Configurations for each level of the Primary Hand.")]
    public List<HandLevelSO> primaryHandConfigs;
    [Tooltip("Configurations for each level of the Secondary Hand.")]
    public List<HandLevelSO> secondaryHandConfigs;



    private int _currentPrimaryHandLevel = 1;
    private int _currentSecondaryHandLevel = 1;
    private SoundHandle _primaryStreamHandle;
    private SoundHandle _secondaryStreamHandle;
    
    // Per-hand shotgun sound tracking - 8 concurrent sounds per hand for 0.2s fire rate
    // With 0.2s fire rate and ~1.0s sound duration, we need 5+ slots to prevent cutoff
    // Using 8 slots provides 1.6s buffer (8 * 0.2s) - foolproof for any shotgun sound duration
    private SoundHandle[] _primaryShotgunHandles = new SoundHandle[8] { 
        SoundHandle.Invalid, SoundHandle.Invalid, SoundHandle.Invalid, SoundHandle.Invalid,
        SoundHandle.Invalid, SoundHandle.Invalid, SoundHandle.Invalid, SoundHandle.Invalid 
    };
    private SoundHandle[] _secondaryShotgunHandles = new SoundHandle[8] { 
        SoundHandle.Invalid, SoundHandle.Invalid, SoundHandle.Invalid, SoundHandle.Invalid,
        SoundHandle.Invalid, SoundHandle.Invalid, SoundHandle.Invalid, SoundHandle.Invalid 
    };
    private int _primaryShotgunIndex = 0;
    private int _secondaryShotgunIndex = 0;

    [Header("Movement Stats Per Level")]
    [Tooltip("Additional jump height provided by each level. Index 0 = Level 1.")]
    public List<float> doubleJumpHeightByLevel = new List<float> { 5f, 6f, 7f, 8f, 9f }; // Example values

    [Header("PowerUp States")]
    public bool HomingDaggersActive { get; private set; } = false;
    private Coroutine _homingDaggersCoroutine;
    
    [Header("Sword Mode System - RIGHT HAND")]
    [Tooltip("Is RIGHT sword mode currently active?")]
    public bool IsSwordModeActive { get; private set; } = false;
    
    [Tooltip("Is sword item currently equipped in RIGHT hand weapon slot? (gates mode activation)")]
    private bool _isSwordAvailable = false;
    
    [Tooltip("RIGHT hand sword damage script reference (assign the sword GameObject)")]
    public SwordDamage swordDamage;
    [Tooltip("Optional: RIGHT sword visual GameObject to show/hide when toggling modes")]
    public GameObject swordVisualGameObject;
    [Tooltip("Tracks which RIGHT sword attack to play next (1 or 2) - alternates automatically")]
    private int _currentSwordAttackIndex = 1;
    
    [Header("Sword Mode System - LEFT HAND")]
    [Tooltip("Is LEFT sword mode currently active?")]
    public bool IsLeftSwordModeActive { get; private set; } = false;
    
    [Tooltip("Is sword item currently equipped in LEFT hand weapon slot? (gates mode activation)")]
    private bool _isLeftSwordAvailable = false;
    
    [Tooltip("LEFT hand sword damage script reference (assign the left sword GameObject)")]
    public SwordDamage leftSwordDamage;
    [Tooltip("Optional: LEFT sword visual GameObject to show/hide when toggling modes")]
    public GameObject leftSwordVisualGameObject;
    [Tooltip("Tracks which LEFT sword attack to play next (1 or 2) - alternates automatically")]
    private int _currentLeftSwordAttackIndex = 1;
    
    // RIGHT HAND charged attack tracking
    private bool _isChargingSwordAttack = false;
    private float _swordChargeStartTime = 0f;
    private SoundHandle _swordChargeLoopHandle = SoundHandle.Invalid;
    
    // LEFT HAND charged attack tracking - mirror of RIGHT hand
    private bool _isChargingLeftSwordAttack = false;
    private float _leftSwordChargeStartTime = 0f;
    private SoundHandle _leftSwordChargeLoopHandle = SoundHandle.Invalid;
    
    // ATTACK LOCKOUT: Prevent attacks immediately after equipping (let reveal animation play)
    private float _rightSwordAttackLockoutUntil = -999f;  // Time until RIGHT sword can attack
    private float _leftSwordAttackLockoutUntil = -999f;   // Time until LEFT sword can attack
    private const float SWORD_EQUIP_LOCKOUT_DURATION = 1.0f; // 1 second lockout after equipping
    
    [Header("Homing Dagger Settings")]
    public GameObject homingDaggerPrefab; // Assign Dagger_Homing_ prefab in inspector
    
    [Header("Holographic Hand Integration")]
    private HolographicHandIntegration[] handIntegrations;
    
    [Header("Homing Dagger Spawn Points")]
    [Tooltip("Transform for left hand emit point - where left hand (PRIMARY/LMB) daggers spawn from")]
    public Transform leftHandEmitPoint;
    [Tooltip("Transform for right hand emit point - where right hand (SECONDARY/RMB) daggers spawn from")]
    public Transform rightHandEmitPoint;
    [Tooltip("Offset from emit point for dagger spawn position")]
    public Vector3 daggerSpawnOffset = Vector3.forward * 0.5f;
    
    [Header("Homing Dagger Properties")]
    private string _homingDaggerPoolTag = "Dagger_Homing_"; // Tag for homing dagger pool
    public float homingDaggerDamage = 25f;
    public float homingDaggerSpeed = 30f;
    public float homingTurnSpeed = 5f;
    public float homingInitialStraightFlightDuration = 0.2f; // How long daggers fly straight before homing
    public float homingDaggerCooldown = 0.3f;
    public float homingDaggerStreamFireRate = 0.5f; // How often to fire daggers during stream mode
    public float homingDaggerDetectionRadius = 50f; // How far to look for enemies
    public LayerMask enemyLayerMask; // Set this to layers containing valid homing targets
    private float _nextPrimaryHomingDaggerTime = 0f;
    private float _nextSecondaryHomingDaggerTime = 0f;
    private float _lastPrimaryDaggerFireTime = 0f;
    private float _lastSecondaryDaggerFireTime = 0f;
    
    [Tooltip("Stream dagger fire rate - how often to spawn a homing dagger during stream")]
    private float _lastPrimaryStreamDaggerTime;
    private float _lastSecondaryStreamDaggerTime;
    private Coroutine _primaryStreamDaggerCoroutine;
    private Coroutine _secondaryStreamDaggerCoroutine;
    
    // PERFORMANCE FIX: Cache LayeredHandAnimationController to avoid using 'On' every shot
    [Header("Animation System")]
    private LayeredHandAnimationController _layeredHandAnimationController;
    private PlayerAnimationStateManager _animationStateManager;
    
    // 🕷️ DUAL ROPE GRAPPLING INTEGRATION: Cache grappling system for shooting blockage checks
    private AdvancedGrapplingSystem _grapplingSystem;

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        if (Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("PlayerShooterOrchestrator: Main Camera not found! Aiming will not work.", this);
        }

        primaryHandMechanics?.Initialize(_cameraTransform, overheatManager, true);
        secondaryHandMechanics?.Initialize(_cameraTransform, overheatManager, false);
        // CRITICAL FIX: Apply initial configs immediately to prevent NULL config errors
        // This ensures HandFiringMechanics have valid configs before VFXImmediateFix or other systems access them
        primaryHandMechanics?.ApplyConfig(GetCurrentHandConfig(true));
        secondaryHandMechanics?.ApplyConfig(GetCurrentHandConfig(false));
        
        // PERFORMANCE FIX: Cache animation system references
        _layeredHandAnimationController = GetComponent<LayeredHandAnimationController>();
        _animationStateManager = GetComponent<PlayerAnimationStateManager>();
        
        // 🕷️ DUAL ROPE GRAPPLING INTEGRATION: Cache grappling system for shooting blockage checks
        _grapplingSystem = GetComponent<AdvancedGrapplingSystem>();
        
        if (_layeredHandAnimationController == null)
        {
            Debug.LogWarning("[PlayerShooterOrchestrator] LayeredHandAnimationController not found! Hand animations will be skipped.", this);
        }
        
        if (_animationStateManager == null)
        {
            Debug.LogWarning("[PlayerShooterOrchestrator] PlayerAnimationStateManager not found! Using fallback animation system.", this);
        }
        
        if (_grapplingSystem == null)
        {
            Debug.LogWarning("[PlayerShooterOrchestrator] 🕷️ AdvancedGrapplingSystem not found! Rope-shooting blockage will be disabled.", this);
        }
    }

    void Start()
    {
        // --- DEBUG LOGS FOR HAND POSITION ---
        Debug.Log($"[Debug] PlayerShooterOrchestrator position: {transform.position}");
        if (_cameraTransform != null) Debug.Log($"[Debug] Camera position: {_cameraTransform.position}");
        // --- END DEBUG LOGS ---
        
        // ⚔️ SWORD VISUAL INITIALIZATION: Ensure sword starts hidden
        if (swordVisualGameObject != null)
        {
            swordVisualGameObject.SetActive(false);
            Debug.Log("[PlayerShooterOrchestrator] ⚔️ Sword visual initialized as HIDDEN on start");
        }
        
        // 🔊 DIAGNOSTIC: Verify 3D Audio Setup
        Debug.Log("=== 3D AUDIO DIAGNOSTIC ===", this);
        if (primaryHandAudioSource == null)
            Debug.LogError("❌ PRIMARY (LEFT) hand AudioSource NOT ASSIGNED! Sounds will be 2D.", this);
        else
            Debug.Log($"✅ PRIMARY (LEFT) hand AudioSource: {primaryHandAudioSource.gameObject.name} (Spatial Blend: {primaryHandAudioSource.spatialBlend})", this);
        
        if (secondaryHandAudioSource == null)
            Debug.LogError("❌ SECONDARY (RIGHT) hand AudioSource NOT ASSIGNED! Sounds will be 2D.", this);
        else
            Debug.Log($"✅ SECONDARY (RIGHT) hand AudioSource: {secondaryHandAudioSource.gameObject.name} (Spatial Blend: {secondaryHandAudioSource.spatialBlend})", this);
        Debug.Log("===========================", this);
        
        // HAND VISIBILITY FIX: Ensure both hands are visible on startup
        EnsureHandsAreVisible();

        // Find holographic hand integrations for visual feedback
        handIntegrations = GetComponentsInChildren<HolographicHandIntegration>();
        if (handIntegrations != null && handIntegrations.Length > 0)
        {
            Debug.Log($"[PlayerShooterOrchestrator] Found {handIntegrations.Length} holographic hand integrations");
        }

        // PERSISTENCE FIX: Don't immediately initialize hand levels - let PlayerProgression restore them first
        // PlayerProgression will call HandlePrimaryHandLevelChanged/HandleSecondaryHandLevelChanged when ready
        if (PlayerProgression.Instance != null)
        {
            Debug.Log($"[Debug] PlayerShooterOrchestrator: Delaying hand initialization - PlayerProgression will notify when ready");
            // Start a coroutine to wait for proper hand level restoration
            StartCoroutine(WaitForHandLevelRestoration());
        }
        else
        {
            // Fallback if no PlayerProgression found
            Debug.LogWarning("[Debug] PlayerShooterOrchestrator: No PlayerProgression found, using default levels");
            HandlePrimaryHandLevelChanged(1);
            HandleSecondaryHandLevelChanged(1);
        }
    }

    /// <summary>
    /// PERSISTENCE FIX: Wait for PlayerProgression to restore hand levels before initializing hands
    /// This prevents the timing conflict where PlayerShooterOrchestrator locks hands at level 1
    /// </summary>
    private IEnumerator WaitForHandLevelRestoration()
    {
        float maxWaitTime = 10f; // Give up after 10 seconds
        float checkInterval = 0.1f; // Check every 0.1 seconds
        float elapsedTime = 0f;
        
        Debug.Log($"[Debug] PlayerShooterOrchestrator: Waiting for hand level restoration...");
        
        // Wait until PlayerProgression has finished its persistence restoration
        while (elapsedTime < maxWaitTime)
        {
            if (PlayerProgression.Instance != null)
            {
                // Check if hand levels have been restored from persistence (not defaults)
                int primaryLevel = PlayerProgression.Instance.primaryHandLevel;
                int secondaryLevel = PlayerProgression.Instance.secondaryHandLevel;
                
                // If HandLevelPersistenceManager exists and has different levels than (1,1), restoration is done
                if (HandLevelPersistenceManager.Instance != null)
                {
                    int savedPrimary = HandLevelPersistenceManager.Instance.CurrentPrimaryHandLevel;
                    int savedSecondary = HandLevelPersistenceManager.Instance.CurrentSecondaryHandLevel;
                    
                    // If current levels match saved levels, restoration is complete
                    if (primaryLevel == savedPrimary && secondaryLevel == savedSecondary)
                    {
                        Debug.Log($"[Debug] PlayerShooterOrchestrator: Hand restoration complete after {elapsedTime:F1}s - Primary={primaryLevel}, Secondary={secondaryLevel}");
                        break;
                    }
                }
            }
            
            yield return new WaitForSeconds(checkInterval);
            elapsedTime += checkInterval;
        }
        
        // Initialize hands with current levels (either restored or defaults)
        if (PlayerProgression.Instance != null)
        {
            int finalPrimaryLevel = PlayerProgression.Instance.primaryHandLevel;
            int finalSecondaryLevel = PlayerProgression.Instance.secondaryHandLevel;
            
            Debug.Log($"[Debug] PlayerShooterOrchestrator: Initializing hands with levels - Primary={finalPrimaryLevel}, Secondary={finalSecondaryLevel}");
            
            HandlePrimaryHandLevelChanged(finalPrimaryLevel);
            HandleSecondaryHandLevelChanged(finalSecondaryLevel);
        }
        else
        {
            Debug.LogWarning("[Debug] PlayerShooterOrchestrator: PlayerProgression lost during wait, using default levels");
            HandlePrimaryHandLevelChanged(1);
            HandleSecondaryHandLevelChanged(1);
        }
    }

    void OnEnable() => SubscribeToEvents();
    void OnDisable()
    {
        UnsubscribeFromEvents();
        HandlePrimaryHoldEnded(); // Ensure sounds are stopped
        HandleSecondaryHoldEnded();
        
        // Stop any charging sword attack
        if (_isChargingSwordAttack)
        {
            _isChargingSwordAttack = false;
            if (_swordChargeLoopHandle.IsValid)
            {
                _swordChargeLoopHandle.Stop();
                _swordChargeLoopHandle = SoundHandle.Invalid;
            }
        }
    }

    void Update()
    {
        // Sword toggle system: Hold Mouse4 (modifier) + click LMB/RMB
        // Hold Mouse4 + click LMB = Toggle LEFT hand sword
        // Hold Mouse4 + click RMB = Toggle RIGHT hand sword
        bool holdingMouse4 = Input.GetMouseButton(3);
        
        if (holdingMouse4)
        {
            // Mouse4 is held - check for LMB/RMB clicks
            if (Input.GetMouseButtonDown(0)) // LMB clicked while Mouse4 held
            {
                Debug.Log("[PlayerShooterOrchestrator] 🎮 Mouse4 + LMB: Toggle LEFT hand sword");
                ToggleLeftSwordMode();
            }
            else if (Input.GetMouseButtonDown(1)) // RMB clicked while Mouse4 held
            {
                Debug.Log("[PlayerShooterOrchestrator] 🎮 Mouse4 + RMB: Toggle RIGHT hand sword");
                ToggleSwordMode();
            }
        }
    }

    void SubscribeToEvents()
    {
        if (inputHandler == null) inputHandler = PlayerInputHandler.Instance;
        if (inputHandler != null)
        {
            inputHandler.OnPrimaryTapAction += HandlePrimaryTap;
            inputHandler.OnPrimaryHoldStartedAction += HandlePrimaryHoldStarted;
            inputHandler.OnPrimaryHoldEndedAction += HandlePrimaryHoldEnded;
            inputHandler.OnSecondaryTapAction += HandleSecondaryTap;
            inputHandler.OnSecondaryHoldStartedAction += HandleSecondaryHoldStarted;
            inputHandler.OnSecondaryHoldEndedAction += HandleSecondaryHoldEnded;
        }
        if (PlayerProgression.Instance != null)
        {
            PlayerProgression.OnPrimaryHandLevelChangedForHUD += HandlePrimaryHandLevelChanged;
            PlayerProgression.OnSecondaryHandLevelChangedForHUD += HandleSecondaryHandLevelChanged;
        }
        if (overheatManager != null)
        {
            overheatManager.OnHandFullyOverheated += HandleHandStopFiringDueToOverheat;
        }
    }

    void UnsubscribeFromEvents()
    {
        if (inputHandler != null)
        {
            inputHandler.OnPrimaryTapAction -= HandlePrimaryTap;
            inputHandler.OnPrimaryHoldStartedAction -= HandlePrimaryHoldStarted;
            inputHandler.OnPrimaryHoldEndedAction -= HandlePrimaryHoldEnded;
            inputHandler.OnSecondaryTapAction -= HandleSecondaryTap;
            inputHandler.OnSecondaryHoldStartedAction -= HandleSecondaryHoldStarted;
            inputHandler.OnSecondaryHoldEndedAction -= HandleSecondaryHoldEnded;
        }
        if (PlayerProgression.Instance != null)
        {
            PlayerProgression.OnPrimaryHandLevelChangedForHUD -= HandlePrimaryHandLevelChanged;
            PlayerProgression.OnSecondaryHandLevelChangedForHUD -= HandleSecondaryHandLevelChanged;
        }
        if (overheatManager != null)
        {
            overheatManager.OnHandFullyOverheated -= HandleHandStopFiringDueToOverheat;
        }
    }

    public HandLevelSO GetCurrentHandConfig(bool isPrimary)
    {
        var configs = isPrimary ? primaryHandConfigs : secondaryHandConfigs;
        int level = isPrimary ? _currentPrimaryHandLevel : _currentSecondaryHandLevel;

        if (configs == null || level < 1 || level > configs.Count || configs[level - 1] == null)
        {
            return null;
        }
        return configs[level - 1];
    }

    public int GetCurrentPrimaryHandLevel()
    {
        return _currentPrimaryHandLevel;
    }

    public int GetCurrentSecondaryHandLevel()
    {
        return _currentSecondaryHandLevel;
    }

    #region Event Handlers

    private void HandlePrimaryTap()
    {
        // 🕷️ DUAL ROPE GRAPPLING: Block shooting if LEFT hand rope is active
        if (_grapplingSystem != null && _grapplingSystem.IsLeftRopeActive)
        {
            Debug.Log("[PlayerShooterOrchestrator] 🕷️ Left hand shooting BLOCKED - rope active!");
            return; // Exit early - left hand is using rope, not gun
        }
        
        // ⚔️ CHECK LEFT SWORD MODE FIRST - if active, trigger sword attack instead of shooting
        if (IsLeftSwordModeActive)
        {
            // 🔒 ATTACK LOCKOUT: Prevent attack during reveal animation (1 second after equipping)
            if (Time.time < _leftSwordAttackLockoutUntil)
            {
                Debug.Log($"[LEFT SWORD] Attack locked - reveal animation playing ({_leftSwordAttackLockoutUntil - Time.time:F2}s remaining)");
                return; // Exit early - lockout active, reveal animation still playing
            }
            
            TriggerLeftSwordAttack();
            return; // Exit early - no shooting in sword mode
        }
        
        if (overheatManager.IsHandOverheated(true)) return;
        
        // Normal shooting always happens regardless of powerups
        bool normalShotFired = false;
        var config = GetCurrentHandConfig(true);
        if (config != null && primaryHandMechanics != null)
        {
            // Fire the shotgun; if it fires, orchestrator plays the audio
            normalShotFired = primaryHandMechanics.TryFireShotgun(_currentPrimaryHandLevel, config.shotgunBlastVolume);
            if (normalShotFired)
            {
                // Stop oldest shotgun sound from THIS hand only (ring buffer with 8 slots)
                // Only stop if the sound is still playing (prevents unnecessary stops)
                if (_primaryShotgunHandles[_primaryShotgunIndex].IsValid && _primaryShotgunHandles[_primaryShotgunIndex].IsPlaying)
                {
                    _primaryShotgunHandles[_primaryShotgunIndex].Stop();
                }
                
                // ✅ 3D AUDIO: Play through hand's Transform - sound follows hand position!
                _primaryShotgunHandles[_primaryShotgunIndex] = GameSounds.PlayShotgunBlastOnHand(primaryHandMechanics.emitPoint, _currentPrimaryHandLevel, config.shotgunBlastVolume);
                
                // Advance to next slot (ring buffer: 0 -> 1 -> 2 -> ... -> 7 -> 0)
                // With 8 slots and 0.2s fire rate, sounds have 1.6s to finish before being stopped
                _primaryShotgunIndex = (_primaryShotgunIndex + 1) % 8;
                
                // TRIGGER SHOTGUN ANIMATION using centralized system
                if (_animationStateManager != null)
                {
                    _animationStateManager.RequestShootingStart(true); // Primary = left hand = true for isLeftHand param
                }
                else
                {
                    // Fallback to old system
                    _layeredHandAnimationController?.PlayShootShotgun(true);
                }
            }
        }
        
        // Notify holographic hands - SHOTGUN FIRE (quick pulse, no glitch)
        NotifyHandsShotgunFire();
        
        // If homing daggers powerup is active, fire homing dagger in addition to normal shot
        if (HomingDaggersActive)
        {
            // Debug.Log("[PlayerShooterOrchestrator] HomingDaggersActive=true, attempting to fire primary homing dagger", this);
            FireHomingDaggerFromHand(true);
        }
        else
        {
            // Debug.Log("[PlayerShooterOrchestrator] HomingDaggersActive=false, skipping homing dagger", this);
        }
    }

    private void HandlePrimaryHoldStarted()
    {
        // 🕷️ DUAL ROPE GRAPPLING: Block beam shooting if LEFT hand rope is active
        if (_grapplingSystem != null && _grapplingSystem.IsLeftRopeActive)
        {
            Debug.Log("[PlayerShooterOrchestrator] 🕷️ Left hand beam shooting BLOCKED - rope active!");
            return; // Exit early - left hand is using rope, not gun
        }
        
        // ⚔️ CHECK LEFT SWORD MODE FIRST - if active, start charging sword instead of beam
        if (IsLeftSwordModeActive)
        {
            StartChargingLeftSwordAttack();
            return; // Exit early - no beam in sword mode
        }
        
        if (overheatManager == null || primaryHandMechanics == null) return;
        
        if (overheatManager.IsHandOverheated(true)) return;
        primaryHandMechanics.TryStartStream();
        
        // Notify holographic hands - BEAM START (heavy glitch)
        NotifyHandsBeamStart();
        
        // Make sure to check if handle exists before stopping
        if (_primaryStreamHandle != null && _primaryStreamHandle.IsValid) _primaryStreamHandle.Stop();
        
        var config = GetCurrentHandConfig(true);
        if (config != null && primaryHandMechanics.emitPoint != null)
        {
            // Debug.Log($"Starting primary stream at level {_currentPrimaryHandLevel} with volume {config.fireStreamVolume}");
            _primaryStreamHandle = GameSounds.PlayStreamLoop(primaryHandMechanics.emitPoint, _currentPrimaryHandLevel, config.fireStreamVolume);
            
            // TRIGGER BEAM ANIMATION using centralized system
            if (_animationStateManager != null)
            {
                _animationStateManager.RequestBeamStart(true); // Primary = left hand
            }
            else
            {
                // Fallback to old system
                _layeredHandAnimationController?.OnBeamStarted(true);
            }
        }
        
        // If homing daggers powerup is active, start firing homing daggers in stream mode
        if (HomingDaggersActive)
        {
            if (_primaryStreamDaggerCoroutine != null)
            {
                StopCoroutine(_primaryStreamDaggerCoroutine);
            }
            _primaryStreamDaggerCoroutine = StartCoroutine(StreamHomingDaggerRoutine(true));
        }
    }

    private void HandlePrimaryHoldEnded()
    {
        // ⚔️ CHECK LEFT SWORD MODE FIRST - if active, release charged sword attack
        if (IsLeftSwordModeActive)
        {
            ReleaseChargedLeftSwordAttack();
            return; // Exit early - no beam in sword mode
        }
        
        primaryHandMechanics?.StopStream();
        
        // Notify holographic hands - BEAM STOP (reset effects)
        NotifyHandsBeamStop();
        
        // Safely check and stop stream handle
        if (_primaryStreamHandle != null && _primaryStreamHandle.IsValid)
        {
            _primaryStreamHandle.Stop();
            _primaryStreamHandle = SoundHandle.Invalid;
            // Debug.Log("Primary stream stopped");
        }
        
        // Stop homing dagger stream if active
        if (_primaryStreamDaggerCoroutine != null)
        {
            StopCoroutine(_primaryStreamDaggerCoroutine);
            _primaryStreamDaggerCoroutine = null;
        }

        // STOP BEAM ANIMATION using centralized system
        if (_animationStateManager != null)
        {
            _animationStateManager.RequestBeamStop(true); // Primary = left hand - FIXED: was RequestShootingStop!
        }
        else
        {
            // Fallback to old system
            _layeredHandAnimationController?.OnBeamStopped(true);
        }
    }

    private void HandleSecondaryTap()
    {
        // 🕷️ DUAL ROPE GRAPPLING: Block shooting if RIGHT hand rope is active
        if (_grapplingSystem != null && _grapplingSystem.IsRightRopeActive)
        {
            Debug.Log("[PlayerShooterOrchestrator] 🕷️ Right hand shooting BLOCKED - rope active!");
            return; // Exit early - right hand is using rope, not gun
        }
        
        if (overheatManager.IsHandOverheated(false)) return;
        
        // SWORD MODE: If sword mode is active, trigger sword attack instead of shooting
        if (IsSwordModeActive)
        {
            // 🔒 ATTACK LOCKOUT: Prevent attack during reveal animation (1 second after equipping)
            if (Time.time < _rightSwordAttackLockoutUntil)
            {
                Debug.Log($"[RIGHT SWORD] Attack locked - reveal animation playing ({_rightSwordAttackLockoutUntil - Time.time:F2}s remaining)");
                return; // Exit early - lockout active, reveal animation still playing
            }
            
            TriggerSwordAttack();
            return; // Exit early - no shooting in sword mode
        }
        
        // Normal shooting always happens regardless of powerups
        bool normalShotFired = false;
        var config = GetCurrentHandConfig(false);
        if (config != null && secondaryHandMechanics != null)
        {
            // Fire the shotgun; if it fires, orchestrator plays the audio
            normalShotFired = secondaryHandMechanics.TryFireShotgun(_currentSecondaryHandLevel, config.shotgunBlastVolume);
            if (normalShotFired)
            {
                // Stop oldest shotgun sound from THIS hand only (ring buffer with 8 slots)
                // Only stop if the sound is still playing (prevents unnecessary stops)
                if (_secondaryShotgunHandles[_secondaryShotgunIndex].IsValid && _secondaryShotgunHandles[_secondaryShotgunIndex].IsPlaying)
                {
                    _secondaryShotgunHandles[_secondaryShotgunIndex].Stop();
                }
                
                // ✅ 3D AUDIO: Play through hand's Transform - sound follows hand position!
                _secondaryShotgunHandles[_secondaryShotgunIndex] = GameSounds.PlayShotgunBlastOnHand(secondaryHandMechanics.emitPoint, _currentSecondaryHandLevel, config.shotgunBlastVolume);
                
                // Advance to next slot (ring buffer: 0 -> 1 -> 2 -> ... -> 7 -> 0)
                // With 8 slots and 0.2s fire rate, sounds have 1.6s to finish before being stopped
                _secondaryShotgunIndex = (_secondaryShotgunIndex + 1) % 8;
                
                // TRIGGER SHOTGUN ANIMATION using centralized system
                if (_animationStateManager != null)
                {
                    _animationStateManager.RequestShootingStart(false); // Secondary = right hand = false for isLeftHand param
                }
                else
                {
                    // Fallback to old system
                    _layeredHandAnimationController?.PlayShootShotgun(false);
                }
            }
        }
        
        // Notify holographic hands - SHOTGUN FIRE (quick pulse, no glitch)
        NotifyHandsShotgunFire();
        
        // If homing daggers powerup is active, fire homing dagger in addition to normal shot
        if (HomingDaggersActive)
        {
            // Debug.Log("[PlayerShooterOrchestrator] HomingDaggersActive=true, attempting to fire secondary homing dagger", this);
            FireHomingDaggerFromHand(false);
        }
        else
        {
            // Debug.Log("[PlayerShooterOrchestrator] HomingDaggersActive=false, skipping homing dagger", this);
        }
    }

    private void HandleSecondaryHoldStarted()
    {
        // 🕷️ DUAL ROPE GRAPPLING: Block beam shooting if RIGHT hand rope is active
        if (_grapplingSystem != null && _grapplingSystem.IsRightRopeActive)
        {
            Debug.Log("[PlayerShooterOrchestrator] 🕷️ Right hand beam shooting BLOCKED - rope active!");
            return; // Exit early - right hand is using rope, not gun
        }
        
        if (overheatManager.IsHandOverheated(false)) return;
        
        // SWORD MODE: Start charging sword attack instead of beam shooting
        if (IsSwordModeActive)
        {
            StartChargingSwordAttack();
            return; // Exit early - no beam shooting in sword mode
        }
        
        secondaryHandMechanics.TryStartStream();
        
        // Notify holographic hands - BEAM START (heavy glitch)
        NotifyHandsBeamStart();
        
        // Safe check for stream handle before stopping
        if (_secondaryStreamHandle != null && _secondaryStreamHandle.IsValid) _secondaryStreamHandle.Stop();
        
        var config = GetCurrentHandConfig(false);
        if (config != null && secondaryHandMechanics.emitPoint != null)
        {
            // Debug.Log($"Starting secondary stream at level {_currentSecondaryHandLevel} with volume {config.fireStreamVolume}");
            _secondaryStreamHandle = GameSounds.PlayStreamLoop(secondaryHandMechanics.emitPoint, _currentSecondaryHandLevel, config.fireStreamVolume);
            
            // TRIGGER BEAM ANIMATION using centralized system
            if (_animationStateManager != null)
            {
                _animationStateManager.RequestBeamStart(false); // Secondary = right hand = false for isLeftHand param
            }
            else
            {
                // Fallback to old system
                _layeredHandAnimationController?.OnBeamStarted(false);
            }
        }
        
        // If homing daggers powerup is active, start firing homing daggers in stream mode
        if (HomingDaggersActive)
        {
            if (_secondaryStreamDaggerCoroutine != null)
            {
                StopCoroutine(_secondaryStreamDaggerCoroutine);
            }
            _secondaryStreamDaggerCoroutine = StartCoroutine(StreamHomingDaggerRoutine(false));
        }
    }

    private void HandleSecondaryHoldEnded()
    {
        // SWORD MODE: Release charged sword attack if charging
        if (IsSwordModeActive && _isChargingSwordAttack)
        {
            ReleaseChargedSwordAttack();
            return; // Exit early - handle sword release separately
        }
        
        secondaryHandMechanics?.StopStream();
        
        // Notify holographic hands - BEAM STOP (reset effects)
        NotifyHandsBeamStop();
        
        // Safely check and stop stream handle
        if (_secondaryStreamHandle != null && _secondaryStreamHandle.IsValid)
        {
            _secondaryStreamHandle.Stop();
            _secondaryStreamHandle = SoundHandle.Invalid;
            // Debug.Log("Secondary stream stopped");
        }
        
        // Stop homing dagger stream if active
        if (_secondaryStreamDaggerCoroutine != null)
        {
            StopCoroutine(_secondaryStreamDaggerCoroutine);
            _secondaryStreamDaggerCoroutine = null;
        }

        // STOP BEAM ANIMATION using centralized system
        if (_animationStateManager != null)
        {
            _animationStateManager.RequestBeamStop(false); // Secondary = right hand - FIXED: was RequestShootingStop!
        }
        else
        {
            // Fallback to old system
            _layeredHandAnimationController?.OnBeamStopped(false);
        }
    }

    private void HandleHandStopFiringDueToOverheat(bool isPrimaryHand)
    {
        if (isPrimaryHand) HandlePrimaryHoldEnded();
        else HandleSecondaryHoldEnded();
    }

    public void HandlePrimaryHandLevelChanged(int newLevel)
    {
        _currentPrimaryHandLevel = newLevel;

        // SINGLE MODEL ARCHITECTURE: No model swapping needed!
        // The hand model stays active, only damage config changes
        // Visual changes are handled by HolographicHandController via LayeredHandAnimationController
        
        Debug.Log($"[PlayerShooterOrchestrator] Primary hand level changed to {newLevel} - updating damage config only (single model architecture)");

        // Apply the updated config to the EXISTING mechanics script
        // primaryHandMechanics should already be initialized and pointing to the single hand model
        primaryHandMechanics?.ApplyConfig(GetCurrentHandConfig(true));
        
        // Notify animation controller about hand level change for visual updates
        _layeredHandAnimationController?.OnHandLevelChanged(true, newLevel);
    }

    public void HandleSecondaryHandLevelChanged(int newLevel)
    {
        _currentSecondaryHandLevel = newLevel;

        // SINGLE MODEL ARCHITECTURE: No model swapping needed!
        // The hand model stays active, only damage config changes
        // Visual changes are handled by HolographicHandController via LayeredHandAnimationController
        
        Debug.Log($"[PlayerShooterOrchestrator] Secondary hand level changed to {newLevel} - updating damage config only (single model architecture)");

        // Apply the updated config to the EXISTING mechanics script
        // secondaryHandMechanics should already be initialized and pointing to the single hand model
        secondaryHandMechanics?.ApplyConfig(GetCurrentHandConfig(false));
        
        // Notify animation controller about hand level change for visual updates
        _layeredHandAnimationController?.OnHandLevelChanged(false, newLevel);
    }

    #endregion

    #region Audio Handling

    // Audio is now managed by this class, not passed to HandFiringMechanics
    // Stream audio is played and tracked via _primaryStreamHandle and _secondaryStreamHandle
    // All sounds are triggered via the centralized GameSounds static API
    public void DuckHandStreamVolumes(bool ducked)
    {
        // Volume ducking is now handled by the sound system directly
        // This method is kept for compatibility
    }

    #endregion

    #region PowerUp Handling

    // Public methods to control shooting that can be called from powerup scripts
    public void StopPrimaryHandShooting()
    {
        primaryHandMechanics?.StopStream();
    }
    
    public void StopSecondaryHandShooting()
    {
        secondaryHandMechanics?.StopStream();
    }
    
    public void StartPrimaryHandShooting()
    {
        if (overheatManager.IsHandOverheated(true)) return;
        primaryHandMechanics?.TryStartStream();
    }
    
    public void StartSecondaryHandShooting()
    {
        if (overheatManager.IsHandOverheated(false)) return;
        secondaryHandMechanics?.TryStartStream();
    }
    
    public void ActivateHomingDaggersPowerUp(float duration)
    {
        Debug.Log($"[PlayerShooterOrchestrator] ActivateHomingDaggersPowerUp called with duration: {duration}", this);
        
        if (_homingDaggersCoroutine != null)
        {
            StopCoroutine(_homingDaggersCoroutine);
            Debug.Log("[PlayerShooterOrchestrator] Stopped existing homing daggers coroutine", this);
        }
        
        _homingDaggersCoroutine = StartCoroutine(HomingDaggersRoutine(duration));
        
        // Reset all timers
        _nextPrimaryHomingDaggerTime = 0f;
        _nextSecondaryHomingDaggerTime = 0f;
        _lastPrimaryStreamDaggerTime = 0f;
        _lastSecondaryStreamDaggerTime = 0f;
        
        Debug.Log("[PlayerShooterOrchestrator] Homing daggers activation setup complete", this);
    }

    private IEnumerator HomingDaggersRoutine(float duration)
    {
        HomingDaggersActive = true;
        
        Debug.Log($"Homing Daggers activated for {duration} seconds");
        
        // Clean up any existing stream coroutines
        if (_primaryStreamDaggerCoroutine != null)
        {
            StopCoroutine(_primaryStreamDaggerCoroutine);
            _primaryStreamDaggerCoroutine = null;
        }
        
        if (_secondaryStreamDaggerCoroutine != null)
        {
            StopCoroutine(_secondaryStreamDaggerCoroutine);
            _secondaryStreamDaggerCoroutine = null;
        }
        
        yield return new WaitForSeconds(duration);
        
        HomingDaggersActive = false;
        _homingDaggersCoroutine = null;
        
        // Clean up any active stream coroutines
        if (_primaryStreamDaggerCoroutine != null)
        {
            StopCoroutine(_primaryStreamDaggerCoroutine);
            _primaryStreamDaggerCoroutine = null;
        }
        
        if (_secondaryStreamDaggerCoroutine != null)
        {
            StopCoroutine(_secondaryStreamDaggerCoroutine);
            _secondaryStreamDaggerCoroutine = null;
        }
        
        Debug.Log("Homing Daggers deactivated");
    }
    
    #region Homing Dagger Functionality
    
    /// <summary>
    /// Gets the spawn position and direction for homing daggers based on hand-specific emit points
    /// 🔧 CRITICAL FIX: Use HandFiringMechanics emit points directly instead of separate references
    /// This prevents left/right confusion by going straight to the source
    /// </summary>
    /// <param name="isPrimaryHand">True for primary (LMB/left) hand, false for secondary (RMB/right) hand</param>
    /// <param name="spawnPosition">Output spawn position</param>
    /// <param name="spawnDirection">Output spawn direction</param>
    /// <returns>True if emit point was used, false if fallback to camera was used</returns>
    private bool GetHandEmitPointAndDirection(bool isPrimaryHand, out Vector3 spawnPosition, out Vector3 spawnDirection)
    {
        // 🔧 FIX: Get emit point DIRECTLY from HandFiringMechanics to avoid confusion
        HandFiringMechanics mechanics = isPrimaryHand ? primaryHandMechanics : secondaryHandMechanics;
        Transform emitPoint = mechanics != null ? mechanics.emitPoint : null;
        string handName = isPrimaryHand ? "PRIMARY (left/LMB)" : "SECONDARY (right/RMB)";
        
        // 🔍 DIAGNOSTIC: Show which emit point we're actually using
        if (emitPoint != null)
        {
            Debug.Log($"[PlayerShooterOrchestrator] 🎯 {handName} hand using emit point: {emitPoint.name} " +
                     $"at position {emitPoint.position} (parent: {emitPoint.parent?.name ?? "NULL"})", this);
        }
        
        if (emitPoint != null)
        {
            // Use hand emit point with configurable offset
            spawnPosition = emitPoint.position + emitPoint.TransformDirection(daggerSpawnOffset);
            spawnDirection = emitPoint.forward;
            
            Debug.Log($"[PlayerShooterOrchestrator] Using {handName} hand emit point: {emitPoint.name} at {spawnPosition}", this);
            return true;
        }
        else
        {
            // Fallback to camera-based spawning (original behavior)
            if (Camera.main != null)
            {
                Vector3 cameraPosition = Camera.main.transform.position;
                Vector3 cameraForward = Camera.main.transform.forward;
                spawnPosition = cameraPosition + cameraForward * 5f + Vector3.up * 2f;
                spawnDirection = cameraForward;
                
                Debug.LogWarning($"[PlayerShooterOrchestrator] {handName} hand emit point not assigned, using camera fallback at {spawnPosition}", this);
            }
            else
            {
                // Ultimate fallback if no camera
                spawnPosition = transform.position + transform.forward * 2f;
                spawnDirection = transform.forward;
                
                Debug.LogError($"[PlayerShooterOrchestrator] No camera found! Using PlayerShooterOrchestrator position fallback at {spawnPosition}", this);
            }
            return false;
        }
    }
    
    private void FireHomingDaggerFromHand(bool isPrimaryHand)
    {
        Debug.Log($"[PlayerShooterOrchestrator] FireHomingDaggerFromHand called for {(isPrimaryHand ? "Primary" : "Secondary")} hand", this);
        
        // Check cooldown first
        if (Time.time < (isPrimaryHand ? _nextPrimaryHomingDaggerTime : _nextSecondaryHomingDaggerTime))
        {
            Debug.Log($"[PlayerShooterOrchestrator] Homing dagger on cooldown for {(isPrimaryHand ? "Primary" : "Secondary")} hand", this);
            return;
        }
        
        // Update cooldown timer
        if (isPrimaryHand)
            _nextPrimaryHomingDaggerTime = Time.time + homingDaggerCooldown;
        else
            _nextSecondaryHomingDaggerTime = Time.time + homingDaggerCooldown;
        
        // Check if we have a homing dagger prefab
        if (homingDaggerPrefab == null)
        {
            Debug.LogError("[PlayerShooterOrchestrator] homingDaggerPrefab is NULL! Please assign the homing dagger prefab in the inspector.", this);
            return;
        }
        
        // ROBUST SOLUTION: Use hand-specific emit points with fallback logic
        Vector3 spawnPosition;
        Vector3 spawnDirection;
        
        // DEBUG: Check emit point status before calling GetHandEmitPointAndDirection
        Transform targetEmitPoint = isPrimaryHand ? leftHandEmitPoint : rightHandEmitPoint;
        Debug.Log($"[PlayerShooterOrchestrator] BEFORE GetHandEmitPointAndDirection - Target emit point for {(isPrimaryHand ? "left (primary/LMB)" : "right (secondary/RMB)")} hand: {(targetEmitPoint != null ? targetEmitPoint.name : "NULL")}", this);
        
        if (GetHandEmitPointAndDirection(isPrimaryHand, out spawnPosition, out spawnDirection))
        {
            Debug.Log($"[PlayerShooterOrchestrator] SUCCESS: Spawning homing dagger from {(isPrimaryHand ? "left (primary/LMB)" : "right (secondary/RMB)")} hand emit point: {spawnPosition}", this);
        }
        else
        {
            Debug.LogWarning($"[PlayerShooterOrchestrator] FALLBACK: No emit point assigned for {(isPrimaryHand ? "left (primary/LMB)" : "right (secondary/RMB)")} hand, using camera fallback at {spawnPosition}", this);
        }
        
        // Find a target for the homing dagger
        Transform target = FindNearestEnemyTransform();
        if (target != null)
        {
            Debug.Log($"[PlayerShooterOrchestrator] Found target: {target.name}", this);
        }
        else
        {
            Debug.Log("[PlayerShooterOrchestrator] No target found, dagger will fly straight", this);
        }
        
        // Instantiate the dagger
        GameObject daggerObj = Instantiate(homingDaggerPrefab, spawnPosition, Quaternion.LookRotation(spawnDirection));
        
        if (daggerObj == null)
        {
            Debug.LogError("[PlayerShooterOrchestrator] Failed to instantiate homing dagger!", this);
            return;
        }
        
        Debug.Log($"[PlayerShooterOrchestrator] Homing dagger created: {daggerObj.name}", this);
        
        // FUCK THE OLD SYSTEM - USE NEW SIMPLE HOMING DAGGER
        daggerObj.SetActive(true);
        
        // Remove the broken DaggerProjectile component if it exists
        DaggerProjectile oldDagger = daggerObj.GetComponent<DaggerProjectile>();
        if (oldDagger != null)
        {
            DestroyImmediate(oldDagger);
            Debug.Log("[PlayerShooterOrchestrator] Removed broken DaggerProjectile component", this);
        }
        
        // Add the new working SimpleHomingDagger component
        SimpleHomingDagger newDagger = daggerObj.AddComponent<SimpleHomingDagger>();
        newDagger.speed = homingDaggerSpeed;
        newDagger.damage = homingDaggerDamage;
        newDagger.lifetime = 10f;
        newDagger.homingStrength = homingTurnSpeed;
        newDagger.targetTags = new string[] {"Skull", "Gem"}; // ONLY target these tags
        
        // Ensure it has a collider for hit detection
        Collider col = daggerObj.GetComponent<Collider>();
        if (col == null)
        {
            col = daggerObj.AddComponent<CapsuleCollider>();
        }
        col.isTrigger = true; // For hit detection
        
        Debug.Log("[PlayerShooterOrchestrator] NEW SIMPLE HOMING DAGGER CREATED AND WORKING!", this);
        
        // Play sound effect
        if (SoundEventsManager.Events != null && SoundEventsManager.Events.daggerHit != null && SoundEventsManager.Events.daggerHit.Length > 0)
        {
            var randomDaggerHit = SoundEventsManager.Events.daggerHit[Random.Range(0, SoundEventsManager.Events.daggerHit.Length)];
            if (randomDaggerHit != null)
            {
                randomDaggerHit.Play3D(spawnPosition, 0.8f);
            }
        }
        
        Debug.Log("[PlayerShooterOrchestrator] Homing dagger firing complete!", this);
    }
    
    private Transform FindNearestEnemyTransform()
    {
        if (_cameraTransform == null || Camera.main == null) return null;
        
        // Try to get a target from the player's field of view first
        Transform target = TryGetTargetInView();
        if (target != null) return target;
        
        // If no target in view, look around the player in a sphere
        Vector3 origin = transform.position;
        Collider[] hitColliders = Physics.OverlapSphere(origin, homingDaggerDetectionRadius, enemyLayerMask);
        
        float closestDistance = float.MaxValue;
        Transform closestEnemy = null;
        
        foreach (var hitCollider in hitColliders)
        {
            // Skip triggers
            if (hitCollider.isTrigger) continue;
            
            // Check if it has a valid target component (SkullEnemy or similar)
            SkullEnemy enemy = hitCollider.GetComponent<SkullEnemy>();
            // Using reflection to access the isDeadInternal field since there's no IsDead() method
            if (enemy != null && !IsEnemyDead(enemy))
            {
                float distance = Vector3.Distance(origin, hitCollider.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = hitCollider.transform;
                }
            }
        }
        
        return closestEnemy;
    }
    
    private Transform TryGetTargetInView()
    {
        // Try to get a target from the center of the screen first
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return null;
        
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, homingDaggerDetectionRadius, enemyLayerMask))
        {
            SkullEnemy enemy = hit.collider.GetComponent<SkullEnemy>();
            if (enemy != null && !IsEnemyDead(enemy))
            {
                return hit.transform;
            }
        }
        
        return null;
    }
    
    // Helper method to check if an enemy is dead
    private bool IsEnemyDead(SkullEnemy enemy)
    {
        if (enemy == null) return true;
        
        // First try to check if the object is enabled
        if (!enemy.gameObject.activeInHierarchy) return true;
        
        // Check if health is depleted (a common way to determine death)
        // Access the private field using reflection
        System.Reflection.FieldInfo deadField = typeof(SkullEnemy).GetField("isDeadInternal", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        if (deadField != null)
        {
            bool isDead = (bool)deadField.GetValue(enemy);
            return isDead;
        }
        
        // Fallback: check if health is depleted
        if (enemy.maxHealth <= 0) return true;
        
        // If we can't determine, assume alive
        return false;
    }
    
    /// <summary>
    /// Coroutine to fire homing daggers at a regular interval during stream firing
    /// </summary>
    private IEnumerator StreamHomingDaggerRoutine(bool isPrimaryHand)
    {
        while (HomingDaggersActive)
        {
            // Check if the hand is overheated
            if ((isPrimaryHand && overheatManager.IsHandOverheated(true)) || 
                (!isPrimaryHand && overheatManager.IsHandOverheated(false)))
            {
                yield return new WaitForSeconds(0.1f); // Short wait before trying again
                continue;
            }
            
            // Fire a homing dagger
            FireHomingDaggerFromHand(isPrimaryHand);
            
            // Wait for the next firing opportunity
            yield return new WaitForSeconds(homingDaggerStreamFireRate);
        }
    }
    
    #endregion

    #region Audio Stop Methods
    
    /// <summary>
    /// Stop primary hand streaming audio - called from HandFiringMechanics when beam stops
    /// </summary>
    public void StopPrimaryStreamAudio()
    {
        if (_primaryStreamHandle != null && _primaryStreamHandle.IsValid)
        {
            _primaryStreamHandle.Stop();
            _primaryStreamHandle = SoundHandle.Invalid;
            // Debug.Log("[AUDIO] Primary stream audio stopped via HandFiringMechanics callback");
        }
    }
    
    /// <summary>
    /// Stop secondary hand streaming audio - called from HandFiringMechanics when beam stops
    /// </summary>
    public void StopSecondaryStreamAudio()
    {
        if (_secondaryStreamHandle != null && _secondaryStreamHandle.IsValid)
        {
            _secondaryStreamHandle.Stop();
            _secondaryStreamHandle = SoundHandle.Invalid;
            // Debug.Log("[AUDIO] Secondary stream audio stopped via HandFiringMechanics callback");
        }
    }
    
    #endregion

    #region Hand Emit Point Setup Methods
    
    /// <summary>
    /// Public method to set left hand emit point (for external scripts or inspector setup)
    /// </summary>
    /// <param name="emitPoint">Transform to use as left hand emit point</param>
    public void SetLeftHandEmitPoint(Transform emitPoint)
    {
        leftHandEmitPoint = emitPoint;
        Debug.Log($"[PlayerShooterOrchestrator] Left hand emit point set to: {(emitPoint != null ? emitPoint.name : "NULL")}", this);
    }
    
    /// <summary>
    /// Public method to set right hand emit point (for external scripts or inspector setup)
    /// </summary>
    /// <param name="emitPoint">Transform to use as right hand emit point</param>
    public void SetRightHandEmitPoint(Transform emitPoint)
    {
        rightHandEmitPoint = emitPoint;
        Debug.Log($"[PlayerShooterOrchestrator] Right hand emit point set to: {(emitPoint != null ? emitPoint.name : "NULL")}", this);
    }
    
    /// <summary>
    /// Automatically find and assign emit points from HandFiringMechanics components
    /// </summary>
    [ContextMenu("Auto-Assign Hand Emit Points")]
    public void AutoAssignHandEmitPoints()
    {
        // Try to get emit points from current hand mechanics (Primary = LMB = Left Hand)
        if (primaryHandMechanics != null && primaryHandMechanics.emitPoint != null)
        {
            SetLeftHandEmitPoint(primaryHandMechanics.emitPoint);
        }
        else
        {
            Debug.LogWarning("[PlayerShooterOrchestrator] Could not auto-assign left hand emit point - primaryHandMechanics or its emitPoint is null", this);
        }
        
        // Secondary = RMB = Right Hand
        if (secondaryHandMechanics != null && secondaryHandMechanics.emitPoint != null)
        {
            SetRightHandEmitPoint(secondaryHandMechanics.emitPoint);
        }
        else
        {
            Debug.LogWarning("[PlayerShooterOrchestrator] Could not auto-assign right hand emit point - secondaryHandMechanics or its emitPoint is null", this);
        }
    }
    
    /// <summary>
    /// Debug method to check current emit point assignments
    /// </summary>
    [ContextMenu("Debug Emit Point Status")]
    public void DebugEmitPointStatus()
    {
        Debug.Log("=== EMIT POINT DEBUG STATUS ===", this);
        Debug.Log($"Left Hand Emit Point: {(leftHandEmitPoint != null ? leftHandEmitPoint.name + " at " + leftHandEmitPoint.position : "NULL")}", this);
        Debug.Log($"Right Hand Emit Point: {(rightHandEmitPoint != null ? rightHandEmitPoint.name + " at " + rightHandEmitPoint.position : "NULL")}", this);
        Debug.Log($"Dagger Spawn Offset: {daggerSpawnOffset}", this);
        Debug.Log($"Primary Hand Mechanics: {(primaryHandMechanics != null ? primaryHandMechanics.name : "NULL")}", this);
        Debug.Log($"Secondary Hand Mechanics: {(secondaryHandMechanics != null ? secondaryHandMechanics.name : "NULL")}", this);
        
        if (primaryHandMechanics != null)
        {
            Debug.Log($"Primary Hand Mechanics Emit Point: {(primaryHandMechanics.emitPoint != null ? primaryHandMechanics.emitPoint.name + " at " + primaryHandMechanics.emitPoint.position : "NULL")}", this);
        }
        
        if (secondaryHandMechanics != null)
        {
            Debug.Log($"Secondary Hand Mechanics Emit Point: {(secondaryHandMechanics.emitPoint != null ? secondaryHandMechanics.emitPoint.name + " at " + secondaryHandMechanics.emitPoint.position : "NULL")}", this);
        }
        Debug.Log("=== END EMIT POINT DEBUG ===", this);
    }
    
    /// <summary>
    /// Test method to fire a homing dagger from each hand for testing emit points
    /// </summary>
    [ContextMenu("Test Homing Dagger Emit Points")]
    public void TestHomingDaggerEmitPoints()
    {
        if (!HomingDaggersActive)
        {
            Debug.LogWarning("[PlayerShooterOrchestrator] Homing daggers not active! Activate the powerup first.", this);
            return;
        }
        
        Debug.Log("[PlayerShooterOrchestrator] Testing homing dagger emit points...", this);
        
        // Reset cooldowns for testing
        _nextPrimaryHomingDaggerTime = 0f;
        _nextSecondaryHomingDaggerTime = 0f;
        
        // Fire from right hand (primary)
        Debug.Log("[PlayerShooterOrchestrator] Testing RIGHT HAND emit point:", this);
        FireHomingDaggerFromHand(true);
        
        // Wait a bit then fire from left hand (secondary)
        StartCoroutine(TestLeftHandAfterDelay());
    }
    
    private System.Collections.IEnumerator TestLeftHandAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        Debug.Log("[PlayerShooterOrchestrator] Testing LEFT HAND emit point:", this);
        FireHomingDaggerFromHand(false);
    }
    
    #endregion


    #endregion

    // ============================================================================
    // SWORD MODE SYSTEM - RIGHT HAND IMPLEMENTATION
    // ============================================================================
    
    /// <summary>
    /// Toggle RIGHT hand sword mode on/off (Hold RMB + Mouse4)
    /// Only affects right hand (secondary) - left hand continues shooting normally
    /// </summary>
    public void ToggleSwordMode()
    {
        // ⚔️ MANUAL TOGGLE INVENTORY SYNC:
        // When manually toggling ON, check if sword is in inventory and move to weapon slot
        // When manually toggling OFF, move sword from weapon slot back to inventory
        
        if (!IsSwordModeActive)
        {
            // TOGGLING ON
            if (!_isSwordAvailable)
            {
                // Sword not in weapon slot - check if it's in inventory and auto-equip it
                Debug.Log("[PlayerShooterOrchestrator] 🔍 Sword not in weapon slot - searching inventory...");
                
                var inventoryManager = InventoryManager.Instance;
                var weaponManager = WeaponEquipmentManager.Instance;
                
                if (inventoryManager != null && weaponManager != null && weaponManager.rightHandWeaponSlot != null)
                {
                    // Search inventory for a sword (manual iteration)
                    var allSlots = inventoryManager.GetAllInventorySlots();
                    UnifiedSlot swordSlot = null;
                    
                    foreach (var slot in allSlots)
                    {
                        if (!slot.IsEmpty && slot.CurrentItem is EquippableWeaponItemData weaponData && weaponData.weaponTypeID == "sword")
                        {
                            swordSlot = slot;
                            break;
                        }
                    }
                    
                    if (swordSlot != null)
                    {
                        Debug.Log("[PlayerShooterOrchestrator] ⚔️ Found sword in inventory - auto-equipping to weapon slot!");
                        
                        // Get sword data
                        var swordItem = swordSlot.CurrentItem;
                        int swordCount = swordSlot.ItemCount;
                        
                        // Check if weapon slot is occupied
                        if (!weaponManager.rightHandWeaponSlot.IsEmpty)
                        {
                            Debug.LogWarning("[PlayerShooterOrchestrator] ⚠️ Weapon slot occupied - cannot auto-equip!");
                            return; // Can't equip - slot full
                        }
                        
                        // Move to weapon slot
                        weaponManager.rightHandWeaponSlot.SetItem(swordItem, swordCount, bypassValidation: true);
                        swordSlot.ClearSlot();
                        Debug.Log("[PlayerShooterOrchestrator] ✅ Sword auto-equipped - activation will happen via event system!");
                        return; // Exit - SetItem will trigger events that activate sword mode
                    }
                    else
                    {
                        Debug.Log("[PlayerShooterOrchestrator] ❌ No sword found in inventory or weapon slot - cannot activate!");
                        return; // No sword anywhere - abort
                    }
                }
                else
                {
                    Debug.LogError("[PlayerShooterOrchestrator] ❌ InventoryManager or WeaponEquipmentManager not found!");
                    return; // Abort
                }
            }
            // If _isSwordAvailable is true, sword is already in weapon slot - proceed with activation below
        }
        else
        {
            // TOGGLING OFF - Move sword from weapon slot to inventory
            WeaponEquipmentManager weaponManager = WeaponEquipmentManager.Instance;
            if (weaponManager != null && weaponManager.rightHandWeaponSlot != null && !weaponManager.rightHandWeaponSlot.IsEmpty)
            {
                // Get the sword from weapon slot
                ChestItemData swordItem = weaponManager.rightHandWeaponSlot.CurrentItem;
                int swordCount = weaponManager.rightHandWeaponSlot.ItemCount;
                
                // Find first empty inventory slot
                InventoryManager inventoryManager = InventoryManager.Instance;
                if (inventoryManager != null)
                {
                    UnifiedSlot emptySlot = inventoryManager.GetFirstEmptySlot();
                    if (emptySlot != null)
                    {
                        // Move sword to inventory
                        emptySlot.SetItem(swordItem, swordCount, bypassValidation: true);
                        weaponManager.rightHandWeaponSlot.ClearSlot();
                        Debug.Log("[PlayerShooterOrchestrator] 🎒 Manual toggle OFF - moved sword to inventory!");
                        return; // Exit - ClearSlot will trigger the full deactivation flow via events
                    }
                    else
                    {
                        Debug.LogWarning("[PlayerShooterOrchestrator] ⚠️ No empty inventory slots - cannot unequip sword!");
                        // Continue with deactivation anyway (just hide sword, don't move it)
                    }
                }
            }
        }
        
        IsSwordModeActive = !IsSwordModeActive;
        
        if (IsSwordModeActive)
        {
            Debug.Log("[PlayerShooterOrchestrator] SWORD MODE ACTIVATED - Right hand now uses sword attacks");
            
            // SOUND: Play sword unsheath sound (⭐ in the eyes!)
            if (SoundEventsManager.Events != null && SoundEventsManager.Events.swordUnsheath != null)
            {
                SoundEventsManager.Events.swordUnsheath.Play3D(transform.position);
                Debug.Log("[PlayerShooterOrchestrator] 🗡️✨ Playing sword unsheath sound!");
            }
            
            // 🔧 CRITICAL FIX: Stop beam mechanics AND animation BEFORE triggering sword reveal
            // This prevents beam stop animation from playing when switching to sword mode
            if (secondaryHandMechanics != null && secondaryHandMechanics.IsStreaming)
            {
                secondaryHandMechanics.StopStream();
                
                // Stop beam sound if active
                if (_secondaryStreamHandle != null && _secondaryStreamHandle.IsValid)
                {
                    _secondaryStreamHandle.Stop();
                    _secondaryStreamHandle = SoundHandle.Invalid;
                }
                
                // Notify holographic hands - BEAM STOP
                NotifyHandsBeamStop();
                
                // STOP BEAM ANIMATION SILENTLY (without triggering beam stop animation)
                if (_animationStateManager != null)
                {
                    _animationStateManager.RequestBeamStop(false); // Secondary = right hand
                }
                else
                {
                    _layeredHandAnimationController?.OnBeamStopped(false);
                }
                
                Debug.Log("[PlayerShooterOrchestrator] Stopped beam shooting before sword mode activation");
            }
            
            // Activate sword visual GameObject
            if (swordVisualGameObject != null)
            {
                swordVisualGameObject.SetActive(true);
                Debug.Log("[PlayerShooterOrchestrator] Sword visual activated");
            }
            
            // ANIMATION: Trigger sword reveal/unsheath animation on right hand
            // This now happens AFTER beam is fully stopped, preventing animation conflict
            if (_layeredHandAnimationController != null)
            {
                var rightHand = _layeredHandAnimationController.rightHandController;
                if (rightHand != null)
                {
                    rightHand.TriggerSwordReveal();
                    Debug.Log("[PlayerShooterOrchestrator] 🗡️ Triggered sword reveal animation!");
                    
                    // 🔒 SET ATTACK LOCKOUT: Prevent attacks for 1 second to let reveal animation play
                    _rightSwordAttackLockoutUntil = Time.time + SWORD_EQUIP_LOCKOUT_DURATION;
                    Debug.Log($"[RIGHT SWORD] Attack lockout set - attacks blocked until {_rightSwordAttackLockoutUntil:F2}");
                }
                else
                {
                    Debug.LogWarning("[PlayerShooterOrchestrator] Right hand controller not found for reveal animation!");
                }
            }
        }
        else
        {
            Debug.Log("[PlayerShooterOrchestrator] SWORD MODE DEACTIVATED - Right hand back to shooting mode");
            
            // ⚔️ CRITICAL: Deactivate sword visual GameObject IMMEDIATELY
            // This overrides any animation state that might have enabled it
            if (swordVisualGameObject != null)
            {
                swordVisualGameObject.SetActive(false);
                Debug.Log("[PlayerShooterOrchestrator] ⚔️ Sword visual FORCE DISABLED (overrides animation)");
            }
            else
            {
                Debug.LogError("[PlayerShooterOrchestrator] ⚠️ SWORD VISUAL GAMEOBJECT NOT ASSIGNED! Cannot hide sword mesh!");
            }
            
            // ✅ CRITICAL FIX: Reset right hand animation back to idle when deactivating sword mode
            // NOTE: We DON'T reset to idle - animations stay as they are, only mesh visibility changes
            if (_layeredHandAnimationController != null)
            {
                var rightHand = _layeredHandAnimationController.rightHandController;
                if (rightHand != null)
                {
                    // Stop all overlay animations (sword attacks, etc.)
                    rightHand.ForceStopAllOverlays();
                    
                    Debug.Log("[PlayerShooterOrchestrator] ✅ Stopped sword overlay animations (keeping movement state)");
                }
                else
                {
                    Debug.LogWarning("[PlayerShooterOrchestrator] Right hand controller not found!");
                }
            }
            else
            {
                Debug.LogWarning("[PlayerShooterOrchestrator] LayeredHandAnimationController not found!");
            }
        }
    }
    
    /// <summary>
    /// Toggle LEFT sword mode on/off (Mouse5 or inventory) - MIRROR of ToggleSwordMode() for LEFT hand
    /// Includes inventory sync - moves swords between weapon slot and inventory
    /// </summary>
    public void ToggleLeftSwordMode()
    {
        // ⚔️ MANUAL TOGGLE INVENTORY SYNC:
        // When manually toggling ON, check if sword is in inventory and move to LEFT weapon slot
        // When manually toggling OFF, move sword from LEFT weapon slot back to inventory
        
        if (!IsLeftSwordModeActive)
        {
            // TOGGLING ON
            if (!_isLeftSwordAvailable)
            {
                // Sword not in LEFT weapon slot - check if it's in inventory and auto-equip it
                Debug.Log("[PlayerShooterOrchestrator] 🔍 LEFT Sword not in weapon slot - searching inventory...");
                
                var inventoryManager = InventoryManager.Instance;
                var weaponManager = WeaponEquipmentManager.Instance;
                
                if (inventoryManager != null && weaponManager != null && weaponManager.leftHandWeaponSlot != null)
                {
                    // Search inventory for a LEFT hand sword (manual iteration)
                    var allSlots = inventoryManager.GetAllInventorySlots();
                    UnifiedSlot swordSlot = null;
                    
                    foreach (var slot in allSlots)
                    {
                        if (!slot.IsEmpty && slot.CurrentItem is EquippableWeaponItemData weaponData && 
                            weaponData.weaponTypeID == "sword" && ((int)weaponData.allowedHands & 2) != 0) // LeftHand = 1 << 1 = 2
                        {
                            swordSlot = slot;
                            break;
                        }
                    }
                    
                    if (swordSlot != null)
                    {
                        Debug.Log("[PlayerShooterOrchestrator] ⚔️ Found LEFT sword in inventory - auto-equipping to LEFT weapon slot!");
                        
                        // Get sword data
                        var swordItem = swordSlot.CurrentItem;
                        int swordCount = swordSlot.ItemCount;
                        
                        // Check if LEFT weapon slot is occupied
                        if (!weaponManager.leftHandWeaponSlot.IsEmpty)
                        {
                            Debug.LogWarning("[PlayerShooterOrchestrator] ⚠️ LEFT Weapon slot occupied - cannot auto-equip!");
                            return; // Can't equip - slot full
                        }
                        
                        // Move to LEFT weapon slot
                        weaponManager.leftHandWeaponSlot.SetItem(swordItem, swordCount, bypassValidation: true);
                        swordSlot.ClearSlot();
                        Debug.Log("[PlayerShooterOrchestrator] ✅ LEFT Sword auto-equipped - activation will happen via event system!");
                        return; // Exit - SetItem will trigger events that activate LEFT sword mode
                    }
                    else
                    {
                        Debug.Log("[PlayerShooterOrchestrator] ❌ No LEFT sword found in inventory or weapon slot - cannot activate!");
                        return; // No sword anywhere - abort
                    }
                }
                else
                {
                    Debug.LogError("[PlayerShooterOrchestrator] ❌ InventoryManager or WeaponEquipmentManager not found!");
                    return; // Abort
                }
            }
            // If _isLeftSwordAvailable is true, sword is already in LEFT weapon slot - proceed with activation below
        }
        else
        {
            // TOGGLING OFF - Move LEFT sword from weapon slot to inventory
            WeaponEquipmentManager weaponManager = WeaponEquipmentManager.Instance;
            if (weaponManager != null && weaponManager.leftHandWeaponSlot != null && !weaponManager.leftHandWeaponSlot.IsEmpty)
            {
                // Get the sword from LEFT weapon slot
                ChestItemData swordItem = weaponManager.leftHandWeaponSlot.CurrentItem;
                int swordCount = weaponManager.leftHandWeaponSlot.ItemCount;
                
                // Find first empty inventory slot
                InventoryManager inventoryManager = InventoryManager.Instance;
                if (inventoryManager != null)
                {
                    UnifiedSlot emptySlot = inventoryManager.GetFirstEmptySlot();
                    if (emptySlot != null)
                    {
                        // Move LEFT sword to inventory
                        emptySlot.SetItem(swordItem, swordCount, bypassValidation: true);
                        weaponManager.leftHandWeaponSlot.ClearSlot();
                        Debug.Log("[PlayerShooterOrchestrator] 🎒 Manual toggle OFF - moved LEFT sword to inventory!");
                        return; // Exit - ClearSlot will trigger the full deactivation flow via events
                    }
                    else
                    {
                        Debug.LogWarning("[PlayerShooterOrchestrator] ⚠️ No empty inventory slots - cannot unequip LEFT sword!");
                        // Continue with deactivation anyway (just hide sword, don't move it)
                    }
                }
            }
        }
        
        IsLeftSwordModeActive = !IsLeftSwordModeActive;
        
        if (IsLeftSwordModeActive)
        {
            Debug.Log("[PlayerShooterOrchestrator] LEFT SWORD MODE ACTIVATED - Left hand now uses sword attacks");
            
            // SOUND: Play sword unsheath sound (⭐ in the eyes!)
            if (SoundEventsManager.Events != null && SoundEventsManager.Events.swordUnsheath != null)
            {
                SoundEventsManager.Events.swordUnsheath.Play3D(transform.position);
                Debug.Log("[PlayerShooterOrchestrator] 🗡️✨ Playing LEFT sword unsheath sound!");
            }
            
            // 🔧 CRITICAL FIX: Stop beam mechanics AND animation BEFORE triggering sword reveal
            // This prevents beam stop animation from playing when switching to LEFT sword mode
            if (primaryHandMechanics != null && primaryHandMechanics.IsStreaming)
            {
                primaryHandMechanics.StopStream();
                
                // Stop beam sound if active
                if (_primaryStreamHandle != null && _primaryStreamHandle.IsValid)
                {
                    _primaryStreamHandle.Stop();
                    _primaryStreamHandle = SoundHandle.Invalid;
                }
                
                // Notify holographic hands - BEAM STOP
                NotifyHandsBeamStop();
                
                // STOP BEAM ANIMATION SILENTLY (without triggering beam stop animation)
                if (_animationStateManager != null)
                {
                    _animationStateManager.RequestBeamStop(true); // Primary = left hand
                }
                else
                {
                    _layeredHandAnimationController?.OnBeamStopped(true);
                }
                
                Debug.Log("[PlayerShooterOrchestrator] Stopped LEFT beam shooting before sword mode activation");
            }
            
            // Activate LEFT sword visual GameObject
            if (leftSwordVisualGameObject != null)
            {
                leftSwordVisualGameObject.SetActive(true);
                Debug.Log("[PlayerShooterOrchestrator] LEFT Sword visual activated");
            }
            
            // ANIMATION: Trigger sword reveal/unsheath animation on LEFT hand
            // This now happens AFTER beam is fully stopped, preventing animation conflict
            if (_layeredHandAnimationController != null)
            {
                var leftHand = _layeredHandAnimationController.leftHandController;
                if (leftHand != null)
                {
                    leftHand.TriggerSwordReveal();
                    Debug.Log("[PlayerShooterOrchestrator] 🗡️ Triggered LEFT sword reveal animation!");
                    
                    // 🔒 SET ATTACK LOCKOUT: Prevent attacks for 1 second to let reveal animation play
                    _leftSwordAttackLockoutUntil = Time.time + SWORD_EQUIP_LOCKOUT_DURATION;
                    Debug.Log($"[LEFT SWORD] Attack lockout set - attacks blocked until {_leftSwordAttackLockoutUntil:F2}");
                }
                else
                {
                    Debug.LogWarning("[PlayerShooterOrchestrator] Left hand controller not found for reveal animation!");
                }
            }
        }
        else
        {
            Debug.Log("[PlayerShooterOrchestrator] LEFT SWORD MODE DEACTIVATED - Left hand back to shooting mode");
            
            // ⚔️ CRITICAL: Deactivate LEFT sword visual GameObject IMMEDIATELY
            // This overrides any animation state that might have enabled it
            if (leftSwordVisualGameObject != null)
            {
                leftSwordVisualGameObject.SetActive(false);
                Debug.Log("[PlayerShooterOrchestrator] ⚔️ LEFT Sword visual FORCE DISABLED (overrides animation)");
            }
            else
            {
                Debug.LogError("[PlayerShooterOrchestrator] ⚠️ LEFT SWORD VISUAL GAMEOBJECT NOT ASSIGNED! Cannot hide sword mesh!");
            }
            
            // ✅ CRITICAL FIX: Reset left hand animation back to idle when deactivating sword mode
            // NOTE: We DON'T reset to idle - animations stay as they are, only mesh visibility changes
            if (_layeredHandAnimationController != null)
            {
                var leftHand = _layeredHandAnimationController.leftHandController;
                if (leftHand != null)
                {
                    // Stop all overlay animations (sword attacks, etc.)
                    leftHand.ForceStopAllOverlays();
                    
                    Debug.Log("[PlayerShooterOrchestrator] ✅ Stopped LEFT sword overlay animations (keeping movement state)");
                }
                else
                {
                    Debug.LogWarning("[PlayerShooterOrchestrator] Left hand controller not found!");
                }
            }
            else
            {
                Debug.LogWarning("[PlayerShooterOrchestrator] LayeredHandAnimationController not found!");
            }
        }
    }
    
    /// <summary>
    /// Called by WeaponEquipmentManager to enable/disable sword availability
    /// ✅ UNIFIED SYSTEM: This method controls availability flag
    /// Active state is controlled by ToggleSwordMode() (called from Mouse4 OR inventory equip)
    /// When availability becomes false, this method FORCE deactivates sword mode if active
    /// </summary>
    public void SetSwordAvailable(bool available)
    {
        bool wasAvailable = _isSwordAvailable;
        _isSwordAvailable = available;
        
        Debug.Log($"[PlayerShooterOrchestrator] Sword availability changed: {wasAvailable} → {available}");
        
        // ✅ CRITICAL FIX: If sword becomes unavailable while active, DIRECTLY deactivate
        // Don't call ToggleSwordMode() because it checks availability and would be blocked
        if (!available && IsSwordModeActive)
        {
            Debug.Log("[PlayerShooterOrchestrator] ⚔️ Sword unequipped while active - FORCE deactivating sword mode");
            
            // Set flag to false
            IsSwordModeActive = false;
            
            // ⚔️ CRITICAL: Deactivate sword visual GameObject with error checking
            // This MUST work to prevent "floating sword" bug
            if (swordVisualGameObject != null)
            {
                swordVisualGameObject.SetActive(false);
                Debug.Log("[PlayerShooterOrchestrator] ⚔️ Sword visual FORCE DISABLED on unequip (overrides animation)");
            }
            else
            {
                Debug.LogError("[PlayerShooterOrchestrator] ⚠️ CRITICAL: SWORD VISUAL GAMEOBJECT NOT ASSIGNED! Assign sword.OBJ in inspector!");
            }
            
            // Reset right hand animation - STOP overlays only, don't change movement state
            if (_layeredHandAnimationController != null)
            {
                var rightHand = _layeredHandAnimationController.rightHandController;
                if (rightHand != null)
                {
                    rightHand.ForceStopAllOverlays();
                    Debug.Log("[PlayerShooterOrchestrator] ✅ Stopped sword overlays on unequip");
                }
            }
            
            Debug.Log("[PlayerShooterOrchestrator] ✅ Sword mode FORCE deactivated - ready for shooting");
        }
    }
    
    /// <summary>
    /// Called by WeaponEquipmentManager to enable/disable LEFT sword availability
    /// Mirror of SetSwordAvailable() but for LEFT hand
    /// </summary>
    public void SetLeftSwordAvailable(bool available)
    {
        bool wasAvailable = _isLeftSwordAvailable;
        _isLeftSwordAvailable = available;
        
        Debug.Log($"[PlayerShooterOrchestrator] LEFT Sword availability changed: {wasAvailable} → {available}");
        
        // ✅ CRITICAL FIX: If LEFT sword becomes unavailable while active, DIRECTLY deactivate
        if (!available && IsLeftSwordModeActive)
        {
            Debug.Log("[PlayerShooterOrchestrator] ⚔️ LEFT Sword unequipped while active - FORCE deactivating left sword mode");
            
            // Set flag to false
            IsLeftSwordModeActive = false;
            
            // ⚔️ CRITICAL: Deactivate LEFT sword visual GameObject
            if (leftSwordVisualGameObject != null)
            {
                leftSwordVisualGameObject.SetActive(false);
                Debug.Log("[PlayerShooterOrchestrator] ⚔️ LEFT Sword visual FORCE DISABLED on unequip");
            }
            else
            {
                Debug.LogError("[PlayerShooterOrchestrator] ⚠️ CRITICAL: LEFT SWORD VISUAL GAMEOBJECT NOT ASSIGNED!");
            }
            
            // Reset left hand animation - STOP overlays only
            if (_layeredHandAnimationController != null)
            {
                var leftHand = _layeredHandAnimationController.leftHandController;
                if (leftHand != null)
                {
                    leftHand.ForceStopAllOverlays();
                    Debug.Log("[PlayerShooterOrchestrator] ✅ Stopped LEFT sword overlays on unequip");
                }
            }
            
            Debug.Log("[PlayerShooterOrchestrator] ✅ LEFT Sword mode FORCE deactivated - ready for shooting");
        }
    }
    
    /// <summary>
    /// Check if player can use sword mode (has sword equipped)
    /// </summary>
    public bool CanUseSwordMode()
    {
        return _isSwordAvailable;
    }
    
    /// <summary>
    /// Check if player can use LEFT sword mode (has LEFT sword equipped)
    /// </summary>
    public bool CanUseLeftSwordMode()
    {
        return _isLeftSwordAvailable;
    }
    
    /// <summary>
    /// Trigger sword attack (called when RMB is clicked in sword mode)
    /// </summary>
    private void TriggerSwordAttack()
    {
        if (swordDamage == null)
        {
            Debug.LogWarning("[PlayerShooterOrchestrator] Sword damage script not assigned! Please assign the sword GameObject in the inspector.");
            return;
        }
        
        // Check if sword is ready to attack
        if (!swordDamage.IsReady())
        {
            Debug.Log("[PlayerShooterOrchestrator] Sword on cooldown");
            return;
        }
        
        Debug.Log($"[PlayerShooterOrchestrator] SWORD ATTACK {_currentSwordAttackIndex} TRIGGERED!");
        
        // SOUND: Play sword swing sound (whoosh!)
        if (SoundEventsManager.Events != null && SoundEventsManager.Events.swordSwing != null)
        {
            SoundEventsManager.Events.swordSwing.Play3D(swordDamage.transform.position);
            Debug.Log("[PlayerShooterOrchestrator] 🗡️ Playing sword swing sound");
        }
        
        // IMMEDIATE DAMAGE: Call damage right away for testing (remove animation event dependency)
        // TODO: Once you add animation event, move this call to the animation event
        swordDamage.DealDamage();
        
        // Trigger sword attack animation on right hand with current attack index
        // Animation event will call swordDamage.DealDamage() at the right frame (once you set it up)
        if (_layeredHandAnimationController != null)
        {
            // Get right hand controller (secondary = right hand)
            var rightHand = _layeredHandAnimationController.rightHandController;
            if (rightHand != null)
            {
                rightHand.TriggerSwordAttack(_currentSwordAttackIndex);
                
                // Alternate between attack 1 and 2 for next time
                _currentSwordAttackIndex = (_currentSwordAttackIndex == 1) ? 2 : 1;
            }
            else
            {
                Debug.LogWarning("[PlayerShooterOrchestrator] Right hand controller not found!");
            }
        }
        else
        {
            Debug.LogWarning("[PlayerShooterOrchestrator] LayeredHandAnimationController not found!");
        }
    }
    
    /// <summary>
    /// Start charging a powerful sword attack (hold RMB in sword mode)
    /// </summary>
    private void StartChargingSwordAttack()
    {
        if (swordDamage == null)
        {
            Debug.LogWarning("[PlayerShooterOrchestrator] Sword damage script not assigned!");
            return;
        }
        
        // Check if sword is ready to attack
        if (!swordDamage.IsReady())
        {
            Debug.Log("[PlayerShooterOrchestrator] Sword on cooldown - cannot charge");
            return;
        }
        
        _isChargingSwordAttack = true;
        _swordChargeStartTime = Time.time;
        
        Debug.Log("[PlayerShooterOrchestrator] ⚡ CHARGING SWORD ATTACK!");
        
        // ANIMATION: Trigger charge animation on right hand
        if (_layeredHandAnimationController != null)
        {
            var rightHand = _layeredHandAnimationController.rightHandController;
            if (rightHand != null)
            {
                rightHand.TriggerSwordCharge();
                Debug.Log("[PlayerShooterOrchestrator] ⚡ Triggered sword charge animation!");
            }
        }
        
        // SOUND: Play looping charge sound
        if (SoundEventsManager.Events != null && SoundEventsManager.Events.swordChargeLoop != null)
        {
            _swordChargeLoopHandle = SoundEventsManager.Events.swordChargeLoop.PlayAttached(swordDamage.transform);
            Debug.Log("[PlayerShooterOrchestrator] ⚡ Playing sword charge loop sound!");
        }
    }
    
    /// <summary>
    /// Release the charged sword attack (release RMB in sword mode)
    /// </summary>
    private void ReleaseChargedSwordAttack()
    {
        if (!_isChargingSwordAttack)
        {
            return;
        }
        
        // Stop charging
        _isChargingSwordAttack = false;
        
        // Stop charge loop sound
        if (_swordChargeLoopHandle.IsValid)
        {
            _swordChargeLoopHandle.Stop();
            _swordChargeLoopHandle = SoundHandle.Invalid;
        }
        
        // Calculate charge duration
        float chargeDuration = Time.time - _swordChargeStartTime;
        bool isFullyCharged = chargeDuration >= swordDamage.chargeTime;
        
        Debug.Log($"[PlayerShooterOrchestrator] 💥 RELEASING SWORD ATTACK! Charge duration: {chargeDuration:F2}s, Fully charged: {isFullyCharged}");
        
        if (isFullyCharged)
        {
            // FULLY CHARGED - Execute power attack
            Debug.Log("[PlayerShooterOrchestrator] 💥 FULLY CHARGED POWER ATTACK!");
            
            // SOUND: Play HEAVY sword swing sound FIRST (special whoosh!) - ALWAYS plays regardless of hit
            if (SoundEventsManager.Events != null && SoundEventsManager.Events.swordHeavySwing != null)
            {
                SoundEventsManager.Events.swordHeavySwing.Play3D(swordDamage.transform.position);
                Debug.Log("[PlayerShooterOrchestrator] 🗡️💥 Playing HEAVY power attack swing sound");
            }
            
            // ANIMATION: Trigger power attack animation on right hand
            if (_layeredHandAnimationController != null)
            {
                var rightHand = _layeredHandAnimationController.rightHandController;
                if (rightHand != null)
                {
                    rightHand.TriggerSwordPowerAttack();
                    Debug.Log("[PlayerShooterOrchestrator] 💥 Triggered power attack animation!");
                }
            }
            
            // DAMAGE: Call charged damage (animation event can also call this)
            swordDamage.DealChargedDamage();
        }
        else
        {
            // NOT FULLY CHARGED - Execute normal attack
            Debug.Log("[PlayerShooterOrchestrator] ⚔️ Not fully charged, executing normal attack");
            TriggerSwordAttack();
        }
    }
    
    // ============================================================================
    // LEFT HAND SWORD ATTACK METHODS - MIRROR OF RIGHT HAND
    // ============================================================================
    
    /// <summary>
    /// Trigger LEFT sword attack (called when LMB is clicked in LEFT sword mode)
    /// Mirror of TriggerSwordAttack() but for LEFT hand
    /// </summary>
    private void TriggerLeftSwordAttack()
    {
        if (leftSwordDamage == null)
        {
            Debug.LogWarning("[PlayerShooterOrchestrator] LEFT Sword damage script not assigned! Please assign the LEFT sword GameObject in the inspector.");
            return;
        }
        
        // Check if LEFT sword is ready to attack
        if (!leftSwordDamage.IsReady())
        {
            Debug.Log("[PlayerShooterOrchestrator] LEFT Sword on cooldown");
            return;
        }
        
        Debug.Log($"[PlayerShooterOrchestrator] LEFT SWORD ATTACK {_currentLeftSwordAttackIndex} TRIGGERED!");
        
        // SOUND: Play sword swing sound (whoosh!)
        if (SoundEventsManager.Events != null && SoundEventsManager.Events.swordSwing != null)
        {
            SoundEventsManager.Events.swordSwing.Play3D(leftSwordDamage.transform.position);
            Debug.Log("[PlayerShooterOrchestrator] 🗡️ Playing LEFT sword swing sound");
        }
        
        // IMMEDIATE DAMAGE: Call damage right away for testing (remove animation event dependency)
        // TODO: Once you add animation event, move this call to the animation event
        leftSwordDamage.DealDamage();
        
        // Trigger LEFT sword attack animation on left hand with current attack index
        // Animation event will call leftSwordDamage.DealDamage() at the right frame (once you set it up)
        if (_layeredHandAnimationController != null)
        {
            // Get left hand controller (primary = left hand)
            var leftHand = _layeredHandAnimationController.leftHandController;
            if (leftHand != null)
            {
                leftHand.TriggerSwordAttack(_currentLeftSwordAttackIndex);
                
                // Alternate between attack 1 and 2 for next time
                _currentLeftSwordAttackIndex = (_currentLeftSwordAttackIndex == 1) ? 2 : 1;
            }
            else
            {
                Debug.LogWarning("[PlayerShooterOrchestrator] Left hand controller not found!");
            }
        }
        else
        {
            Debug.LogWarning("[PlayerShooterOrchestrator] LayeredHandAnimationController not found!");
        }
    }
    
    /// <summary>
    /// Start charging a powerful LEFT sword attack (hold LMB in LEFT sword mode)
    /// Mirror of StartChargingSwordAttack() but for LEFT hand
    /// </summary>
    private void StartChargingLeftSwordAttack()
    {
        if (leftSwordDamage == null)
        {
            Debug.LogWarning("[PlayerShooterOrchestrator] LEFT Sword damage script not assigned!");
            return;
        }
        
        // Check if LEFT sword is ready to attack
        if (!leftSwordDamage.IsReady())
        {
            Debug.Log("[PlayerShooterOrchestrator] LEFT Sword on cooldown - cannot charge");
            return;
        }
        
        _isChargingLeftSwordAttack = true;
        _leftSwordChargeStartTime = Time.time;
        
        Debug.Log("[PlayerShooterOrchestrator] ⚡ CHARGING LEFT SWORD ATTACK!");
        
        // ANIMATION: Trigger charge animation on left hand
        if (_layeredHandAnimationController != null)
        {
            var leftHand = _layeredHandAnimationController.leftHandController;
            if (leftHand != null)
            {
                leftHand.TriggerSwordCharge();
                Debug.Log("[PlayerShooterOrchestrator] ⚡ Triggered LEFT sword charge animation!");
            }
        }
        
        // SOUND: Play looping charge sound
        if (SoundEventsManager.Events != null && SoundEventsManager.Events.swordChargeLoop != null)
        {
            _leftSwordChargeLoopHandle = SoundEventsManager.Events.swordChargeLoop.PlayAttached(leftSwordDamage.transform);
            Debug.Log("[PlayerShooterOrchestrator] ⚡ Playing LEFT sword charge loop sound!");
        }
    }
    
    /// <summary>
    /// Release the charged LEFT sword attack (release LMB in LEFT sword mode)
    /// Mirror of ReleaseChargedSwordAttack() but for LEFT hand
    /// </summary>
    private void ReleaseChargedLeftSwordAttack()
    {
        if (!_isChargingLeftSwordAttack)
        {
            return;
        }
        
        // Stop charging
        _isChargingLeftSwordAttack = false;
        
        // Stop charge loop sound
        if (_leftSwordChargeLoopHandle.IsValid)
        {
            _leftSwordChargeLoopHandle.Stop();
            _leftSwordChargeLoopHandle = SoundHandle.Invalid;
        }
        
        // Calculate charge duration
        float chargeDuration = Time.time - _leftSwordChargeStartTime;
        bool isFullyCharged = chargeDuration >= leftSwordDamage.chargeTime;
        
        Debug.Log($"[PlayerShooterOrchestrator] 💥 RELEASING LEFT SWORD ATTACK! Charge duration: {chargeDuration:F2}s, Fully charged: {isFullyCharged}");
        
        if (isFullyCharged)
        {
            // FULLY CHARGED - Execute power attack
            Debug.Log("[PlayerShooterOrchestrator] 💥 LEFT FULLY CHARGED POWER ATTACK!");
            
            // SOUND: Play HEAVY sword swing sound FIRST (special whoosh!) - ALWAYS plays regardless of hit
            if (SoundEventsManager.Events != null && SoundEventsManager.Events.swordHeavySwing != null)
            {
                SoundEventsManager.Events.swordHeavySwing.Play3D(leftSwordDamage.transform.position);
                Debug.Log("[PlayerShooterOrchestrator] 🗡️💥 Playing LEFT HEAVY power attack swing sound");
            }
            
            // ANIMATION: Trigger power attack animation on left hand
            if (_layeredHandAnimationController != null)
            {
                var leftHand = _layeredHandAnimationController.leftHandController;
                if (leftHand != null)
                {
                    leftHand.TriggerSwordPowerAttack();
                    Debug.Log("[PlayerShooterOrchestrator] 💥 Triggered LEFT power attack animation!");
                }
            }
            
            // DAMAGE: Call charged damage (animation event can also call this)
            leftSwordDamage.DealChargedDamage();
        }
        else
        {
            // NOT FULLY CHARGED - Execute normal attack
            Debug.Log("[PlayerShooterOrchestrator] ⚔️ LEFT Not fully charged, executing normal attack");
            TriggerLeftSwordAttack();
        }
    }

    // ============================================================================
    // HOLOGRAPHIC HAND INTEGRATION - AAA VISUAL FEEDBACK
    // ============================================================================
    
    /// <summary>
    /// Notify all holographic hands that beam shooting started (heavy glitch effect)
    /// </summary>
    private void NotifyHandsBeamStart()
    {
        if (handIntegrations == null) return;
        
        foreach (var integration in handIntegrations)
        {
            if (integration != null)
                integration.NotifyBeamStart();
        }
    }
    
    /// <summary>
    /// Notify all holographic hands that beam shooting stopped (reset effects)
    /// </summary>
    private void NotifyHandsBeamStop()
    {
        if (handIntegrations == null) return;
        
        foreach (var integration in handIntegrations)
        {
            if (integration != null)
                integration.NotifyBeamStop();
        }
    }
    
    /// <summary>
    /// Notify all holographic hands that shotgun fired (quick pulse, no glitch)
    /// </summary>
    private void NotifyHandsShotgunFire()
    {
        if (handIntegrations == null) return;
        
        foreach (var integration in handIntegrations)
        {
            if (integration != null)
                integration.NotifyShotgunFire();
        }
    }
    
    /// <summary>
    /// Ensure both hand GameObjects are visible and enabled
    /// Fixes issue where hands might be disabled after removing HandVisualManager
    /// </summary>
    private void EnsureHandsAreVisible()
    {
        LayeredHandAnimationController layeredController = GetComponent<LayeredHandAnimationController>();
        if (layeredController == null) return;
        
        // Ensure left hand is visible
        if (layeredController.leftHandController != null)
        {
            if (!layeredController.leftHandController.gameObject.activeInHierarchy)
            {
                layeredController.leftHandController.gameObject.SetActive(true);
                Debug.Log("[PlayerShooterOrchestrator] Enabled left hand GameObject");
            }
        }
        
        // Ensure right hand is visible
        if (layeredController.rightHandController != null)
        {
            if (!layeredController.rightHandController.gameObject.activeInHierarchy)
            {
                layeredController.rightHandController.gameObject.SetActive(true);
                Debug.Log("[PlayerShooterOrchestrator] Enabled right hand GameObject");
            }
        }
    }
}