using UnityEngine;
using System.Collections;
using System.Linq;

/// <summary>
/// Lightweight coordinator that maintains compatibility with existing systems
/// Routes animation requests to individual hand controllers
/// Replaces the monolithic HandAnimationController while keeping the same public API
/// </summary>
public class HandAnimationCoordinator : MonoBehaviour
{
    [Header("Hand Controllers")]
    [Tooltip("All left hand controllers (levels 1-4)")]
    public IndividualHandController[] leftHandControllers = new IndividualHandController[4];
    
    [Tooltip("All right hand controllers (levels 1-4)")]
    public IndividualHandController[] rightHandControllers = new IndividualHandController[4];
    
    [Header("External System References")]
    [Tooltip("Reference to PlayerProgression (auto-found if null)")]
    public PlayerProgression playerProgression;
    
    [Tooltip("Reference to AAAMovementController (auto-found if null)")]
    public AAAMovementController aaaMovementController;
    
    [Tooltip("Reference to PlayerEnergySystem (auto-found if null)")]
    public PlayerEnergySystem playerEnergySystem;
    
    [Tooltip("Reference to PlayerShooterOrchestrator (auto-found if null)")]
    public PlayerShooterOrchestrator playerShooterOrchestrator;
    
    [Header("Settings")]
    [Tooltip("Enable debug logging")]
    public bool enableDebugLogs = false;
    
    // === EMOTE EVENT SYSTEM ===
    public static event System.Action<int> OnPlayerEmote;
    
    // === MOVEMENT TRACKING ===
    private bool _isCurrentlyMoving = false;
    private bool _isCurrentlySprinting = false;
    private bool _isInFlightMode = false;
    private int _currentEmoteNumber = 1;
    
    // === DIVE OVERRIDE ===
    private bool _isDiveOverrideActive = false;
    
    // === SMART LANDING SYSTEM ===
    private float _jumpStartTime = -999f;
    private bool _wasInAir = false;
    private bool _justCompletedDive = false;
    private const float MIN_AIR_TIME_FOR_LAND_ANIM = 1.0f;
    
    void Awake()
    {
        CacheComponentReferences();
        AutoFindHandControllers();
    }
    
    void Start()
    {
        // Subscribe to events
        if (playerEnergySystem != null)
            PlayerEnergySystem.OnSprintInterrupted += OnSprintInterrupted;
        
        // Initialize all hands to idle
        SetAllHandsToIdle();
        
        if (enableDebugLogs)
            Debug.Log("[HandAnimationCoordinator] Initialized with modular hand system");
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (playerEnergySystem != null)
            PlayerEnergySystem.OnSprintInterrupted -= OnSprintInterrupted;
    }
    
    void Update()
    {
        // Skip updates if dive override is active
        if (_isDiveOverrideActive) return;
        
        // Update movement animations
        UpdateMovementAnimations();
        
        // Update air state tracking
        UpdateAirStateTracking();
        
        // Check for emote input
        CheckEmoteInput();
    }
    
    // === INITIALIZATION ===
    
    private void CacheComponentReferences()
    {
        if (playerProgression == null)
            playerProgression = FindObjectOfType<PlayerProgression>();
        
        if (aaaMovementController == null)
            aaaMovementController = FindObjectOfType<AAAMovementController>();
        
        if (playerEnergySystem == null)
            playerEnergySystem = FindObjectOfType<PlayerEnergySystem>();
        
        if (playerShooterOrchestrator == null)
            playerShooterOrchestrator = FindObjectOfType<PlayerShooterOrchestrator>();
    }
    
    private void AutoFindHandControllers()
    {
        // Find all hand controllers in the scene
        IndividualHandController[] allControllers = FindObjectsOfType<IndividualHandController>();
        
        foreach (var controller in allControllers)
        {
            if (controller.IsLeftHand)
            {
                int index = Mathf.Clamp(controller.HandLevel - 1, 0, 3);
                leftHandControllers[index] = controller;
            }
            else
            {
                int index = Mathf.Clamp(controller.HandLevel - 1, 0, 3);
                rightHandControllers[index] = controller;
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[HandAnimationCoordinator] Found {leftHandControllers.Count(c => c != null)} left hands, {rightHandControllers.Count(c => c != null)} right hands");
        }
    }
    
    // === CURRENT HAND GETTERS ===
    
    private IndividualHandController GetCurrentLeftHand()
    {
        int level = playerProgression != null ? playerProgression.secondaryHandLevel : 1;
        int index = Mathf.Clamp(level - 1, 0, 3);
        return leftHandControllers[index];
    }
    
    private IndividualHandController GetCurrentRightHand()
    {
        int level = playerProgression != null ? playerProgression.primaryHandLevel : 1;
        int index = Mathf.Clamp(level - 1, 0, 3);
        return rightHandControllers[index];
    }
    
    // === MOVEMENT SYSTEM ===
    
    private void UpdateMovementAnimations()
    {
        // Get current hands
        var leftHand = GetCurrentLeftHand();
        var rightHand = GetCurrentRightHand();
        
        if (leftHand == null || rightHand == null) return;
        
        // Skip if hands are locked in high-priority states
        if (leftHand.IsLocked || rightHand.IsLocked) return;
        
        // Check input state directly (reuse existing efficient calculations)
        bool hasMovementInput = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);
        bool isSprintKeyHeld = Input.GetKey(Controls.Boost);
        bool isGrounded = aaaMovementController != null ? aaaMovementController.IsGrounded : true;
        bool hasEnergyToSprint = playerEnergySystem == null || playerEnergySystem.CanSprint;
        
        bool isCurrentlyMoving = hasMovementInput && isGrounded;
        bool isCurrentlySprinting = hasMovementInput && isSprintKeyHeld && isGrounded && hasEnergyToSprint;
        
        // Check if movement state changed
        bool movementStateChanged = (isCurrentlyMoving != _isCurrentlyMoving) || (isCurrentlySprinting != _isCurrentlySprinting);
        
        if (movementStateChanged)
        {
            _isCurrentlyMoving = isCurrentlyMoving;
            _isCurrentlySprinting = isCurrentlySprinting;
            
            // Determine target state
            HandAnimationState targetState = HandAnimationState.Idle;
            if (isCurrentlySprinting)
                targetState = HandAnimationState.Sprint;
            else if (isCurrentlyMoving)
                targetState = HandAnimationState.Walk;
            
            // Apply to both hands
            leftHand.RequestStateTransition(targetState);
            rightHand.RequestStateTransition(targetState);
            
            if (enableDebugLogs)
                Debug.Log($"[HandAnimationCoordinator] Movement: {targetState}");
        }
    }
    
    private void UpdateAirStateTracking()
    {
        if (aaaMovementController == null) return;
        
        bool isGrounded = aaaMovementController.IsGrounded;
        
        if (!isGrounded && !_wasInAir)
        {
            _wasInAir = true;
            if (_jumpStartTime <= -999f)
            {
                _jumpStartTime = Time.time;
            }
        }
    }
    
    private void CheckEmoteInput()
    {
        // DEPRECATED: This coordinator is replaced by PlayerAnimationStateManager + LayeredHandAnimationController
        // Emotes are now handled centrally with arrow keys and RIGHT HAND ONLY
        // Keeping this for backward compatibility but it should not be active
        
        var rightHand = GetCurrentRightHand();
        if (rightHand == null) return;
        
        // Don't allow emotes if right hand is playing an emote (LEFT HAND DOES NOT PLAY EMOTES!)
        if (rightHand.CurrentState == HandAnimationState.Emote)
            return;
        
        // Arrow keys for emotes (RIGHT HAND ONLY)
        if (Input.GetKeyDown(KeyCode.UpArrow)) PlayEmote(1);
        else if (Input.GetKeyDown(KeyCode.DownArrow)) PlayEmote(2);
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) PlayEmote(3);
        else if (Input.GetKeyDown(KeyCode.RightArrow)) PlayEmote(4);
    }
    
    private void OnSprintInterrupted()
    {
        if (_isCurrentlySprinting)
        {
            _isCurrentlySprinting = false;
            
            if (_isCurrentlyMoving)
            {
                var leftHand = GetCurrentLeftHand();
                var rightHand = GetCurrentRightHand();
                
                leftHand?.RequestStateTransition(HandAnimationState.Walk);
                rightHand?.RequestStateTransition(HandAnimationState.Walk);
                
                if (enableDebugLogs)
                    Debug.Log("[HandAnimationCoordinator] Sprint interrupted - switching to walk");
            }
        }
    }
    
    // === PUBLIC API - COMPATIBILITY LAYER ===
    // These methods maintain compatibility with existing systems
    
    public void PlayShootShotgun(bool isPrimaryHand)
    {
        var hand = isPrimaryHand ? GetCurrentLeftHand() : GetCurrentRightHand();
        hand?.RequestStateTransition(HandAnimationState.Shotgun);
        
        if (enableDebugLogs)
            Debug.Log($"[HandAnimationCoordinator] {(isPrimaryHand ? "Left" : "Right")} shotgun fired");
    }
    
    public void StartBeamLeft()
    {
        var leftHand = GetCurrentLeftHand();
        leftHand?.RequestStateTransition(HandAnimationState.Beam);
        
        if (enableDebugLogs)
            Debug.Log("[HandAnimationCoordinator] Left beam started");
    }
    
    public void StartBeamRight()
    {
        var rightHand = GetCurrentRightHand();
        rightHand?.RequestStateTransition(HandAnimationState.Beam);
        
        if (enableDebugLogs)
            Debug.Log("[HandAnimationCoordinator] Right beam started");
    }
    
    public void StopBeamLeft()
    {
        var leftHand = GetCurrentLeftHand();
        leftHand?.StopBeamImmediate();
        
        if (enableDebugLogs)
            Debug.Log("[HandAnimationCoordinator] Left beam stopped - immediate unlock");
    }
    
    public void StopBeamRight()
    {
        var rightHand = GetCurrentRightHand();
        rightHand?.StopBeamImmediate();
        
        if (enableDebugLogs)
            Debug.Log("[HandAnimationCoordinator] Right beam stopped - immediate unlock");
    }
    
    public void StopAllBeams()
    {
        StopBeamLeft();
        StopBeamRight();
    }
    
    public void PlayEmote(int emoteNumber)
    {
        // CRITICAL: EMOTES ARE RIGHT HAND ONLY - LEFT HAND DOES NOT PLAY EMOTES!
        var rightHand = GetCurrentRightHand();
        
        if (rightHand == null) return;
        
        // Don't allow emotes to interrupt each other (right hand only)
        if (rightHand.CurrentState == HandAnimationState.Emote)
            return;
        
        // Validate emote number
        if (emoteNumber < 1 || emoteNumber > 4)
        {
            if (enableDebugLogs) Debug.LogWarning($"[HandAnimationCoordinator] Invalid emote number: {emoteNumber}");
            return;
        }
        
        _currentEmoteNumber = emoteNumber;
        
        // Play emote ONLY on RIGHT HAND - left hand does NOT participate
        rightHand.RequestStateTransition(HandAnimationState.Emote, emoteNumber);
        
        if (enableDebugLogs)
            Debug.Log($"🎭 [HandAnimationCoordinator] Emote {emoteNumber} playing on RIGHT HAND ONLY (Left hand excluded)");
        
        // Notify companion system
        OnPlayerEmote?.Invoke(emoteNumber);
    }
    
    public void PlayEmote1() => PlayEmote(1);
    public void PlayEmote2() => PlayEmote(2);
    public void PlayEmote3() => PlayEmote(3);
    public void PlayEmote4() => PlayEmote(4);
    
    public void PlayLandBoth()
    {
        var leftHand = GetCurrentLeftHand();
        var rightHand = GetCurrentRightHand();
        
        leftHand?.RequestStateTransition(HandAnimationState.Land);
        rightHand?.RequestStateTransition(HandAnimationState.Land);
    }
    
    public void PlaySlideBoth()
    {
        var leftHand = GetCurrentLeftHand();
        var rightHand = GetCurrentRightHand();
        
        leftHand?.RequestStateTransition(HandAnimationState.Slide);
        rightHand?.RequestStateTransition(HandAnimationState.Slide);
    }
    
    public void PlayDiveBoth()
    {
        var leftHand = GetCurrentLeftHand();
        var rightHand = GetCurrentRightHand();
        
        leftHand?.RequestStateTransition(HandAnimationState.Dive);
        rightHand?.RequestStateTransition(HandAnimationState.Dive);
    }
    
    public void PlayJumpBoth()
    {
        var leftHand = GetCurrentLeftHand();
        var rightHand = GetCurrentRightHand();
        
        leftHand?.RequestStateTransition(HandAnimationState.Jump);
        rightHand?.RequestStateTransition(HandAnimationState.Jump);
    }
    
    public void PlayTakeOffBoth()
    {
        var leftHand = GetCurrentLeftHand();
        var rightHand = GetCurrentRightHand();
        
        leftHand?.RequestStateTransition(HandAnimationState.TakeOff);
        rightHand?.RequestStateTransition(HandAnimationState.TakeOff);
    }
    
    public void PlayIdleBoth()
    {
        var leftHand = GetCurrentLeftHand();
        var rightHand = GetCurrentRightHand();
        
        leftHand?.RequestStateTransition(HandAnimationState.Idle);
        rightHand?.RequestStateTransition(HandAnimationState.Idle);
    }
    
    public void PlayApplyPlateAnimation()
    {
        var rightHand = GetCurrentRightHand();
        rightHand?.RequestStateTransition(HandAnimationState.ArmorPlate);
        
        if (enableDebugLogs)
            Debug.Log("[HandAnimationCoordinator] Armor plate animation requested on right hand");
    }
    
    // === EXTERNAL SYSTEM HOOKS ===
    
    public void OnPlayerJumped()
    {
        _jumpStartTime = Time.time;
        _wasInAir = true;
        PlayJumpBoth();
        
        if (enableDebugLogs)
            Debug.Log("[HandAnimationCoordinator] Player jumped");
    }
    
    public void OnPlayerLanded()
    {
        bool shouldPlayLandAnimation = false;
        
        if (_justCompletedDive)
        {
            shouldPlayLandAnimation = true;
            _justCompletedDive = false;
        }
        else if (_wasInAir && (Time.time - _jumpStartTime) >= MIN_AIR_TIME_FOR_LAND_ANIM)
        {
            shouldPlayLandAnimation = true;
        }
        
        _wasInAir = false;
        _jumpStartTime = -999f;
        
        if (shouldPlayLandAnimation)
        {
            PlayLandBoth();
        }
        
        if (enableDebugLogs)
            Debug.Log($"[HandAnimationCoordinator] Player landed - play animation: {shouldPlayLandAnimation}");
    }
    
    public void OnSlideStarted()
    {
        PlaySlideBoth();
        if (enableDebugLogs)
            Debug.Log("[HandAnimationCoordinator] Slide started");
    }
    
    public void OnSlideStopped()
    {
        var leftHand = GetCurrentLeftHand();
        var rightHand = GetCurrentRightHand();
        
        // Force unlock slide states
        if (leftHand?.CurrentState == HandAnimationState.Slide)
            leftHand.ForceTransitionToState(HandAnimationState.Idle);
        
        if (rightHand?.CurrentState == HandAnimationState.Slide)
            rightHand.ForceTransitionToState(HandAnimationState.Idle);
        
        if (enableDebugLogs)
            Debug.Log("[HandAnimationCoordinator] Slide stopped - hands unlocked");
    }
    
    public void PlayDiveAnimation()
    {
        _isDiveOverrideActive = true;
        PlayDiveBoth();
        
        if (enableDebugLogs)
            Debug.Log("[HandAnimationCoordinator] Dive animation started");
    }
    
    public void StopDiveAnimation()
    {
        _isDiveOverrideActive = false;
        
        if (enableDebugLogs)
            Debug.Log("[HandAnimationCoordinator] Dive animation stopped");
    }
    
    public void OnBeamStarted(bool isPrimaryHand)
    {
        if (isPrimaryHand) StartBeamLeft(); else StartBeamRight();
    }
    
    public void OnBeamStopped(bool isPrimaryHand)
    {
        if (isPrimaryHand) StopBeamLeft(); else StopBeamRight();
    }
    
    public void OnShotgunFired(bool isPrimaryHand)
    {
        PlayShootShotgun(isPrimaryHand);
    }
    
    // === UTILITY METHODS ===
    
    private void SetAllHandsToIdle()
    {
        foreach (var controller in leftHandControllers)
        {
            controller?.ForceTransitionToState(HandAnimationState.Idle);
        }
        
        foreach (var controller in rightHandControllers)
        {
            controller?.ForceTransitionToState(HandAnimationState.Idle);
        }
    }
    
    public void NuclearResetAllAnimations()
    {
        foreach (var controller in leftHandControllers)
        {
            controller?.NuclearReset();
        }
        
        foreach (var controller in rightHandControllers)
        {
            controller?.NuclearReset();
        }
        
        if (enableDebugLogs)
            Debug.Log("[HandAnimationCoordinator] NUCLEAR RESET - All hands reset to idle");
    }
    
    // === BACKWARD COMPATIBILITY METHODS ===
    
    public void OnHandModelsChanged()
    {
        AutoFindHandControllers();
        NuclearResetAllAnimations();
        
        if (enableDebugLogs)
            Debug.Log("[HandAnimationCoordinator] Hand models changed - system reinitialized");
    }
    
    public void OnCaptureStarted() { }
    public void OnCaptureCompleted() { PlayIdleBoth(); }
    public void OnCaptureInterrupted() { PlayIdleBoth(); }
    public void OnInteractPromptShown() { }
    public void OnInteractPromptHidden() { }
    public void OnTakeOffStarted() { PlayTakeOffBoth(); }
    
    public void ForceUnlockArmorPlateAnimation()
    {
        var rightHand = GetCurrentRightHand();
        if (rightHand?.CurrentState == HandAnimationState.ArmorPlate)
        {
            rightHand.ForceTransitionToState(HandAnimationState.Idle);
            if (enableDebugLogs)
                Debug.Log("[HandAnimationCoordinator] Emergency: Armor plate animation force unlocked");
        }
    }
    
    public bool IsRightHandInArmorPlateState()
    {
        var rightHand = GetCurrentRightHand();
        return rightHand?.CurrentState == HandAnimationState.ArmorPlate;
    }
}
