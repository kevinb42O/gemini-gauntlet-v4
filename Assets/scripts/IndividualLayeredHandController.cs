using UnityEngine;
using System.Collections;
using GeminiGauntlet.Audio;

/// <summary>
/// Simple individual hand controller that directly interfaces with Unity Animator.
/// All logic is handled by PlayerAnimationStateManager - this is just a thin wrapper.
/// </summary>
public class IndividualLayeredHandController : MonoBehaviour
{
    [Header("Hand Configuration")]
    [Tooltip("The animator component for this hand")]
    public Animator handAnimator;
    
    [Tooltip("Is this a left hand? (false = right hand)")]
    public bool isLeftHand = true;
    
    [Header("Debug")]
    [Tooltip("Enable debug logging for this hand")]
    public bool enableDebugLogs = true;
    
    [Header("Sound Events")]
    [Tooltip("Reference to SoundEvents asset for interaction sounds")]
    public SoundEvents soundEvents;
    
    // Jump alternation toggle (static so all hands stay synchronized)
    private static bool _useJumpVariation2 = false;
    
    // Track if we already toggled this frame (prevent double-toggle when both hands update)
    private static int _lastToggleFrame = -1;
    
    // Callback for state changes
    private PlayerAnimationStateManager _stateManager;
    
    [Header("Layer Settings")]
    [Tooltip("Enable smooth layer weight blending (false = instant snap)")]
    public bool enableLayerBlending = false;
    
    [Tooltip("Speed of layer weight transitions (only used if enableLayerBlending is true)")]
    [Range(0.1f, 10f)]
    public float layerBlendSpeed = 10f;
    
    // === LAYER INDICES ===
    private const int BASE_LAYER = 0;      // Movement animations (disabled when override layers active)
    private const int SHOOTING_LAYER = 1;   // Shooting gestures (OVERRIDE - disables base layer)
    private const int EMOTE_LAYER = 2;      // Emotes (OVERRIDE - disables base layer)
    private const int ABILITY_LAYER = 3;    // Abilities like armor plates (OVERRIDE - disables base layer)
    
    // === LAYER WEIGHTS ===
    // NOTE: Base Layer (Layer 0) weight is ALWAYS 1.0 in Unity and cannot be changed!
    // We only track overlay layer weights (Shooting, Emote, Ability)
    private float _targetShootingWeight = 0f;
    private float _targetEmoteWeight = 0f;
    private float _targetAbilityWeight = 0f;
    
    private float _currentShootingWeight = 0f;
    private float _currentEmoteWeight = 0f;
    private float _currentAbilityWeight = 0f;
    
    // === PROPERTIES ===
    public bool IsLeftHand => isLeftHand;
   
    // === ANIMATION STATES ===
    public enum MovementState
    {
        Idle = 0,
        Walk = 1,
        Sprint = 2,
        Jump = 3,
        Land = 4,
        TakeOff = 5,
        Slide = 6,
        Dive = 7,
        FlyForward = 8,
        FlyUp = 9,
        FlyDown = 10,
        FlyStrafeLeft = 11,
        FlyStrafeRight = 12,
        FlyBoost = 13,
        Falling = 14  // Falling animation - can be overridden by shooting layer
    }
    
    public enum ShootingState
    {
        None = 0,
        Shotgun = 1,
        Beam = 2
    }
    
    public enum EmoteState
    {
        None = 0,
        Emote1 = 1,
        Emote2 = 2,
        Emote3 = 3,
        Emote4 = 4,
        Emote5 = 5
    }
    
    public enum AbilityState
    {
        None = 0,
        ArmorPlate = 1,
        Grab = 2,
        OpenDoor = 3
    }
    
    // Current states for external queries
    public MovementState CurrentMovementState { get; private set; } = MovementState.Idle;
    public ShootingState CurrentShootingState { get; private set; } = ShootingState.None;
    public EmoteState CurrentEmoteState { get; private set; } = EmoteState.None;
    public AbilityState CurrentAbilityState { get; private set; } = AbilityState.None;
    
    // Reference to opposite hand (for optional sync features if needed in future)
    public IndividualLayeredHandController oppositeHand { get; set; }
    
    // Coroutine references for proper cleanup
    private Coroutine _resetShootingCoroutine = null;
    private Coroutine _emoteMonitorCoroutine = null;
    
    // Sprint animation speed tracking
    private AAAMovementController _movementController;
    private float _targetAnimatorSpeed = 1.0f;
    private float _currentAnimatorSpeed = 1.0f;
    [SerializeField] private float animatorSpeedSmoothTime = 0.1f; // Smooth speed transitions
    
    void Awake()
    {
        if (handAnimator == null)
            handAnimator = GetComponent<Animator>();
        
        // Find state manager
        _stateManager = GetComponentInParent<PlayerAnimationStateManager>();
        if (_stateManager == null)
        {
            // Try finding in scene if not in parent hierarchy
            _stateManager = FindObjectOfType<PlayerAnimationStateManager>();
        }
        
        // Find movement controller for sprint speed sync
        _movementController = GetComponentInParent<AAAMovementController>();
        if (_movementController == null)
        {
            _movementController = FindObjectOfType<AAAMovementController>();
        }
        
        // Auto-find SoundEvents if not assigned
        if (soundEvents == null)
        {
            soundEvents = Resources.Load<SoundEvents>("SoundEvents");
            if (soundEvents == null)
            {
                Debug.LogWarning($"[{name}] SoundEvents not found in Resources folder. Interaction sounds will not play.");
            }
        }
    }
    
    void Start()
    {
        // CRITICAL FIX: Initialize animator layer weights to match code state
        // This prevents left hand from appearing in shooting animation at start
        InitializeLayerWeights();
    }
    
    /// <summary>
    /// CRITICAL FIX: Initialize animator layer weights on startup
    /// Ensures animator starts with correct layer weights (0 for all overlays)
    /// Fixes issue where left hand appears in shooting animation at start
    /// </summary>
    private void InitializeLayerWeights()
    {
        if (handAnimator == null)
        {
            Debug.LogWarning($"[{name}] Cannot initialize layer weights - animator is null!");
            return;
        }
        
        // Set initial layer weights to match code defaults
        // Base Layer (Layer 0) is always 1.0 - no need to track it
        _currentShootingWeight = 0f;
        _currentEmoteWeight = 0f;
        _currentAbilityWeight = 0f;
        
        _targetShootingWeight = 0f;
        _targetEmoteWeight = 0f;
        _targetAbilityWeight = 0f;
        
        // Apply to animator immediately
        ApplyLayerWeightsToAnimator();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{name}] Layer weights initialized - Shooting: {_currentShootingWeight}, Emote: {_currentEmoteWeight}, Ability: {_currentAbilityWeight} (Base layer always 1.0)");
        }
    }
    
    void Update()
    {
        // PERFORMANCE OPTIMIZED: Only update if blending is enabled
        // When blending is disabled, weights are applied immediately in SetTargetWeight()
        if (enableLayerBlending)
        {
            // Only update if weights need changing (avoids unnecessary calculations)
            if (HasWeightChanges())
            {
                UpdateLayerWeights();
            }
        }
        
        // 🚀 SMOOTH SPRINT ANIMATION SPEED SYNC
        // Update animator speed smoothly based on current movement state
        if (CurrentMovementState == MovementState.Sprint && _movementController != null)
        {
            // Get normalized sprint speed from movement controller (0.0 = walk, 1.0 = full sprint)
            float normalizedSpeed = _movementController.NormalizedSprintSpeed;
            
            // Map to animation speed range (0.7 = slow start, 1.3 = faster at full sprint!)
            _targetAnimatorSpeed = Mathf.Lerp(0.7f, 1.3f, normalizedSpeed);
        }
        else
        {
            // Non-sprint states use normal speed
            _targetAnimatorSpeed = 1.0f;
        }
        
        // Smooth transition to target speed (prevents jittery changes)
        if (Mathf.Abs(_currentAnimatorSpeed - _targetAnimatorSpeed) > 0.001f)
        {
            _currentAnimatorSpeed = Mathf.Lerp(_currentAnimatorSpeed, _targetAnimatorSpeed, Time.deltaTime / animatorSpeedSmoothTime);
            
            if (handAnimator != null)
            {
                handAnimator.speed = _currentAnimatorSpeed;
            }
        }
        // With blending disabled, no Update() work needed at all!
    }
    
    /// <summary>
    /// Cleanup coroutines when component is destroyed or disabled
    /// CRITICAL: Prevents errors from coroutines running after object destruction
    /// </summary>
    void OnDestroy()
    {
        // Stop all running coroutines to prevent errors
        if (_resetShootingCoroutine != null) StopCoroutine(_resetShootingCoroutine);
        if (_emoteMonitorCoroutine != null) StopCoroutine(_emoteMonitorCoroutine);
    }
    
    void OnDisable()
    {
        // Also cleanup on disable (when hand is deactivated)
        if (_resetShootingCoroutine != null) StopCoroutine(_resetShootingCoroutine);
        if (_emoteMonitorCoroutine != null) StopCoroutine(_emoteMonitorCoroutine);
    }
    
    /// <summary>
    /// Check if any layer weights need updating (optimization to avoid unnecessary updates)
    /// OPTIMIZED: Only checks overlay layers (Base Layer 0 is always 1.0)
    /// </summary>
    private bool HasWeightChanges()
    {
        const float EPSILON = 0.001f; // Threshold for "close enough"
        
        return Mathf.Abs(_currentShootingWeight - _targetShootingWeight) > EPSILON ||
               Mathf.Abs(_currentEmoteWeight - _targetEmoteWeight) > EPSILON ||
               Mathf.Abs(_currentAbilityWeight - _targetAbilityWeight) > EPSILON;
    }
    
    /// <summary>
    /// Updates animator layer weights for smooth blending between animation layers.
    /// NATURAL APPROACH: Just set the layer weights - let Unity Animator's blend modes handle the rest!
    /// </summary>
    private void UpdateLayerWeights()
    {
        if (handAnimator == null) return;
        
        // NO FORCED LOGIC - Just smoothly transition layer weights
        // Unity's Override/Additive blend modes will handle the actual blending behavior
        
        // Update layer weights - instant snap or smooth blend based on settings
        // NOTE: Base Layer (0) is NOT updated as it's always 1.0 in Unity
        if (enableLayerBlending)
        {
            // Smooth blending for overlay layers only
            _currentShootingWeight = Mathf.Lerp(_currentShootingWeight, _targetShootingWeight, layerBlendSpeed * Time.deltaTime);
            _currentEmoteWeight = Mathf.Lerp(_currentEmoteWeight, _targetEmoteWeight, layerBlendSpeed * Time.deltaTime);
            _currentAbilityWeight = Mathf.Lerp(_currentAbilityWeight, _targetAbilityWeight, layerBlendSpeed * Time.deltaTime);
        }
        else
        {
            // Instant snap - NO blending (for overlay layers only)
            _currentShootingWeight = _targetShootingWeight;
            _currentEmoteWeight = _targetEmoteWeight;
            _currentAbilityWeight = _targetAbilityWeight;
        }
        
        ApplyLayerWeightsToAnimator();
    }
    
    /// <summary>
    /// Apply current layer weights to the animator (separated for performance optimization)
    /// </summary>
    private void ApplyLayerWeightsToAnimator()
    {
        if (handAnimator == null) return;
        
        // Apply weights to animator layers - ONLY if they exist!
        try
        {
            // BASE LAYER (Layer 0) - ALWAYS 1.0 in Unity, cannot be changed!
            // Unity's Override blend mode on higher layers will handle masking
            // No need to call SetLayerWeight(0, ...) - it does nothing!
            
            // SHOOTING LAYER - Required for all hands
            if (handAnimator.layerCount > SHOOTING_LAYER)
            {
                handAnimator.SetLayerWeight(SHOOTING_LAYER, _currentShootingWeight);
            }
            else if (_targetShootingWeight > 0)
            {
                // Shooting layer missing - silently reset
                _targetShootingWeight = 0;
                CurrentShootingState = ShootingState.None;
            }
            
            // EMOTE LAYER - Optional (only right hand needs this)
            if (handAnimator.layerCount > EMOTE_LAYER)
            {
                handAnimator.SetLayerWeight(EMOTE_LAYER, _currentEmoteWeight);
            }
            else if (_targetEmoteWeight > 0)
            {
                // Emote layer missing - silently reset
                _targetEmoteWeight = 0;
                CurrentEmoteState = EmoteState.None;
            }
            
            // ABILITY LAYER - Optional (only right hand needs this)
            if (handAnimator.layerCount > ABILITY_LAYER)
            {
                handAnimator.SetLayerWeight(ABILITY_LAYER, _currentAbilityWeight);
            }
            else if (_targetAbilityWeight > 0)
            {
                // Ability layer missing - silently reset
                _targetAbilityWeight = 0;
                CurrentAbilityState = AbilityState.None;
            }
        }
        catch (System.Exception)
        {
            // Silently handle animator errors
        }
    }
    
    /// <summary>
    /// Set target weight and apply immediately if blending is disabled (performance optimization)
    /// </summary>
    private void SetTargetWeight(ref float targetWeight, float newWeight)
    {
        targetWeight = newWeight;
        
        // If blending is disabled, apply immediately instead of waiting for Update()
        if (!enableLayerBlending)
        {
            UpdateLayerWeights();
        }
    }
    
    // === MOVEMENT ANIMATIONS (Base Layer) ===
    
    /// <summary>
    /// Set movement state - simple unified sprint animation (no direction variants)
    /// Animation speed for sprints is automatically synced via Update() loop
    /// </summary>
    public void SetMovementState(MovementState newState)
    {
        // 🎯 JUMP ALTERNATION: Handle BEFORE early return check!
        // Jump needs to toggle even if we're "already" in jump state
        if (newState == MovementState.Jump)
        {
            // CRITICAL: Only toggle ONCE per frame (not once per hand!)
            // Both hands get called in the same frame, so we need to prevent double-toggle
            int currentFrame = Time.frameCount;
            bool shouldToggle = (_lastToggleFrame != currentFrame);
            
            if (shouldToggle)
            {
                // Toggle for this jump (before setting animator)
                _useJumpVariation2 = !_useJumpVariation2;
                _lastToggleFrame = currentFrame;
                
                if (enableDebugLogs)
                    Debug.Log($"[{name}] 🔄 TOGGLED! useJump2 is now: {_useJumpVariation2}");
            }
            
            // Set the animator bool parameter to CURRENT value
            if (handAnimator != null)
            {
                handAnimator.SetBool("useJump2", _useJumpVariation2);
                
                // Force animator to evaluate the new parameter value immediately
                handAnimator.Update(0f);
                
                if (enableDebugLogs)
                    Debug.Log($"[{name}] 🎬 Jump Animation: useJump2 = {_useJumpVariation2}");
            }
        }
        
        // Skip if already in that state (prevents infinite loops and spam)
        // BUT we already handled jump toggle above!
        if (CurrentMovementState == newState)
        {
            return; // CRITICAL: Already in this state, skip
        }
        
        CurrentMovementState = newState;
        
        if (handAnimator != null)
        {
            // Simple and clean - let Unity's Animator state machine handle all transitions
            handAnimator.SetInteger("movementState", (int)newState);
        }
    }
    
    // === SHOOTING ANIMATIONS (Override Layer) ===
    
    public void TriggerShotgun()
    {
        // CRITICAL: If beam is active, stop it first!
        if (CurrentShootingState == ShootingState.Beam)
        {
            if (handAnimator != null && handAnimator.layerCount > SHOOTING_LAYER)
            {
                handAnimator.SetBool("IsBeamAc", false);
            }
            
            if (enableDebugLogs)
                Debug.Log($"[{name}] Shotgun interrupted beam shooting");
        }
        
        // PRIORITY: Shooting interrupts emotes - force stop emote if active
        if (CurrentEmoteState != EmoteState.None)
        {
            CurrentEmoteState = EmoteState.None;
            SetTargetWeight(ref _targetEmoteWeight, 0f);
            
            // Stop emote monitoring coroutine
            if (_emoteMonitorCoroutine != null)
            {
                StopCoroutine(_emoteMonitorCoroutine);
                _emoteMonitorCoroutine = null;
            }
            
            if (enableDebugLogs)
                Debug.Log($"[{name}] Shooting interrupted emote (priority)");
        }
        
        // ALLOW RAPID FIRE: Don't block if already shooting - just retrigger!
        CurrentShootingState = ShootingState.Shotgun;
        
        // Cancel any existing reset coroutine to prevent premature state reset
        if (_resetShootingCoroutine != null)
        {
            StopCoroutine(_resetShootingCoroutine);
            _resetShootingCoroutine = null;
        }
        
        // CRITICAL FIX: Force shooting layer to 1.0 IMMEDIATELY (bypass blending)
        // This prevents blend-through from previous shot's reset
        SetTargetWeight(ref _targetShootingWeight, 1f);
        _currentShootingWeight = 1f;
        if (handAnimator != null && handAnimator.layerCount > SHOOTING_LAYER)
        {
            handAnimator.SetLayerWeight(SHOOTING_LAYER, 1f);
            
            // SetTrigger ALWAYS fires even if already in shotgun state (if animator allows it)
            handAnimator.SetTrigger("ShotgunT");
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning($"[{name}] Cannot trigger shotgun - animator or shooting layer missing!");
        }
        
        // Reset shooting state after brief animation (shotgun is quick)
        _resetShootingCoroutine = StartCoroutine(ResetShootingState(0.5f));
    }
    
    public void StartBeamShooting()
    {
        // CRITICAL: If shotgun is active, clear its reset coroutine!
        if (CurrentShootingState == ShootingState.Shotgun)
        {
            if (_resetShootingCoroutine != null)
            {
                StopCoroutine(_resetShootingCoroutine);
                _resetShootingCoroutine = null;
            }
            
            if (enableDebugLogs)
                Debug.Log($"[{name}] Beam shooting interrupted shotgun");
        }
        
        // PRIORITY: Shooting interrupts emotes - force stop emote if active
        if (CurrentEmoteState != EmoteState.None)
        {
            CurrentEmoteState = EmoteState.None;
            SetTargetWeight(ref _targetEmoteWeight, 0f);
            
            // Stop emote monitoring coroutine
            if (_emoteMonitorCoroutine != null)
            {
                StopCoroutine(_emoteMonitorCoroutine);
                _emoteMonitorCoroutine = null;
            }
            
            if (enableDebugLogs)
                Debug.Log($"[{name}] Beam shooting interrupted emote (priority)");
        }
        
        CurrentShootingState = ShootingState.Beam;
        
        // Enable shooting layer with HIGH PRIORITY weight!
        SetTargetWeight(ref _targetShootingWeight, 1f);
        
        // Validate animator and parameter exist before setting
        if (handAnimator != null && handAnimator.layerCount > SHOOTING_LAYER)
        {
            handAnimator.SetBool("IsBeamAc", true);
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning($"[{name}] Cannot start beam - animator or shooting layer missing!");
        }
    }
    
    public void StopBeamShooting()
    {
        CurrentShootingState = ShootingState.None;
        
        // Disable shooting layer
        SetTargetWeight(ref _targetShootingWeight, 0f);
        
        // Validate animator before setting parameter
        if (handAnimator != null && handAnimator.layerCount > SHOOTING_LAYER)
        {
            handAnimator.SetBool("IsBeamAc", false);
        }
        
        // 🔧 CRITICAL FIX: Reset base layer idle animation to start from beginning
        // This prevents the "snap down" effect when shooting layer weight goes to 0
        ResetBaseLayerToIdle();
        
        // CRITICAL: Notify state manager that beam shooting stopped
        if (_stateManager != null)
        {
            _stateManager.NotifyBeamCompleted(isLeftHand);
        }
    }
    
    /// <summary>
    /// Trigger sword attack animation (RIGHT HAND ONLY in sword mode)
    /// Uses the Shooting layer with SwordAttack1T or SwordAttack2T triggers
    /// </summary>
    /// <param name="attackIndex">Which attack to play (1 or 2)</param>
    public void TriggerSwordAttack(int attackIndex = 1)
    {
        // CRITICAL: If beam is active, stop it first!
        if (CurrentShootingState == ShootingState.Beam)
        {
            if (handAnimator != null && handAnimator.layerCount > SHOOTING_LAYER)
            {
                handAnimator.SetBool("IsBeamAc", false);
            }
            
            if (enableDebugLogs)
                Debug.Log($"[{name}] Sword attack interrupted beam shooting");
        }
        
        // PRIORITY: Shooting interrupts emotes
        if (CurrentEmoteState != EmoteState.None)
        {
            CurrentEmoteState = EmoteState.None;
            SetTargetWeight(ref _targetEmoteWeight, 0f);
            
            if (_emoteMonitorCoroutine != null)
            {
                StopCoroutine(_emoteMonitorCoroutine);
                _emoteMonitorCoroutine = null;
            }
            
            if (enableDebugLogs)
                Debug.Log($"[{name}] Sword attack interrupted emote");
        }
        
        // Set to shotgun state (sword uses same layer as shotgun)
        CurrentShootingState = ShootingState.Shotgun;
        
        // Cancel any existing reset coroutine
        if (_resetShootingCoroutine != null)
        {
            StopCoroutine(_resetShootingCoroutine);
            _resetShootingCoroutine = null;
        }
        
        // Force shooting layer to 1.0 immediately
        SetTargetWeight(ref _targetShootingWeight, 1f);
        _currentShootingWeight = 1f;
        
        if (handAnimator != null && handAnimator.layerCount > SHOOTING_LAYER)
        {
            handAnimator.SetLayerWeight(SHOOTING_LAYER, 1f);
            
            // Trigger the appropriate sword attack animation based on index
            // Trigger the appropriate sword attack animation based on index
            string triggerName = (attackIndex == 2) ? "SwordAttack2T" : "SwordAttack1T";
            handAnimator.SetTrigger(triggerName);
            
            if (enableDebugLogs)
                Debug.Log($"[{name}] SWORD ATTACK {attackIndex} ANIMATION TRIGGERED! (Trigger: {triggerName})");
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning($"[{name}] Cannot trigger sword attack - animator or shooting layer missing!");
        }
        
        // Reset shooting state after sword animation (sword swing is quick like shotgun)
        _resetShootingCoroutine = StartCoroutine(ResetShootingState(0.7f));
    }
    
    /// <summary>
    /// Trigger sword reveal/unsheath animation when entering sword mode
    /// Uses the Shooting layer with SwordRevealT trigger
    /// CRITICAL: Shooting Layer must have "Override" checked in Animator (like Emote layer)
    /// </summary>
    public void TriggerSwordReveal()
    {
        // Stop any active beam
        if (CurrentShootingState == ShootingState.Beam)
        {
            if (handAnimator != null && handAnimator.layerCount > SHOOTING_LAYER)
            {
                handAnimator.SetBool("IsBeamAc", false);
            }
        }
        
        // Stop any active emotes
        if (CurrentEmoteState != EmoteState.None)
        {
            CurrentEmoteState = EmoteState.None;
            SetTargetWeight(ref _targetEmoteWeight, 0f);
            
            if (_emoteMonitorCoroutine != null)
            {
                StopCoroutine(_emoteMonitorCoroutine);
                _emoteMonitorCoroutine = null;
            }
        }
        
        // Set to shotgun state (sword animations use shooting layer)
        CurrentShootingState = ShootingState.Shotgun;
        
        // Cancel any existing reset coroutine
        if (_resetShootingCoroutine != null)
        {
            StopCoroutine(_resetShootingCoroutine);
            _resetShootingCoroutine = null;
        }
        
        // Force shooting layer to 1.0 immediately
        // With Override enabled, this automatically disables base layer (same as emotes!)
        SetTargetWeight(ref _targetShootingWeight, 1f);
        _currentShootingWeight = 1f;
        
        if (handAnimator != null && handAnimator.layerCount > SHOOTING_LAYER)
        {
            handAnimator.SetLayerWeight(SHOOTING_LAYER, 1f);
            
            // Trigger sword reveal animation
            handAnimator.SetTrigger("SwordRevealT");
            
            if (enableDebugLogs)
                Debug.Log($"[{name}] 🗡️ SWORD REVEAL ANIMATION TRIGGERED! (Shooting layer weight: 1.0, Override active)");
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning($"[{name}] Cannot trigger sword reveal - animator or shooting layer missing!");
        }
        
        // 🔧 FIX: Wait for ACTUAL animation length instead of hardcoded time!
        // This monitors the animator state and waits for the animation to actually finish
        _resetShootingCoroutine = StartCoroutine(ResetShootingStateWhenAnimationFinishes("SwordReveal"));
    }
    
    /// <summary>
    /// Trigger sword charge animation (hold sword up)
    /// Uses the Shooting layer with SwordChargeT trigger
    /// </summary>
    public void TriggerSwordCharge()
    {
        // CRITICAL: If beam is active, stop it first!
        if (CurrentShootingState == ShootingState.Beam)
        {
            if (handAnimator != null && handAnimator.layerCount > SHOOTING_LAYER)
            {
                handAnimator.SetBool("IsBeamAc", false);
            }
            
            if (enableDebugLogs)
                Debug.Log($"[{name}] Sword charge interrupted beam shooting");
        }
        
        // PRIORITY: Shooting interrupts emotes
        if (CurrentEmoteState != EmoteState.None)
        {
            CurrentEmoteState = EmoteState.None;
            SetTargetWeight(ref _targetEmoteWeight, 0f);
            
            if (_emoteMonitorCoroutine != null)
            {
                StopCoroutine(_emoteMonitorCoroutine);
                _emoteMonitorCoroutine = null;
            }
            
            if (enableDebugLogs)
                Debug.Log($"[{name}] Sword charge interrupted emote");
        }
        
        // Set to shotgun state (sword uses same layer as shotgun)
        CurrentShootingState = ShootingState.Shotgun;
        
        // Cancel any existing reset coroutine
        if (_resetShootingCoroutine != null)
        {
            StopCoroutine(_resetShootingCoroutine);
            _resetShootingCoroutine = null;
        }
        
        // Force shooting layer to 1.0 immediately
        SetTargetWeight(ref _targetShootingWeight, 1f);
        _currentShootingWeight = 1f;
        
        if (handAnimator != null && handAnimator.layerCount > SHOOTING_LAYER)
        {
            handAnimator.SetLayerWeight(SHOOTING_LAYER, 1f);
            
            // Trigger sword charge animation
            handAnimator.SetTrigger("SwordChargeT");
            
            if (enableDebugLogs)
                Debug.Log($"[{name}] ⚡ SWORD CHARGE ANIMATION TRIGGERED!");
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning($"[{name}] Cannot trigger sword charge - animator or shooting layer missing!");
        }
        
        // Don't reset shooting state - let the release handle it
    }
    
    /// <summary>
    /// Trigger sword power attack animation (release charged attack)
    /// Uses the Shooting layer with SwordPowerAttackT trigger
    /// </summary>
    public void TriggerSwordPowerAttack()
    {
        if (handAnimator != null && handAnimator.layerCount > SHOOTING_LAYER)
        {
            handAnimator.SetLayerWeight(SHOOTING_LAYER, 1f);
            
            // Trigger power attack animation
            handAnimator.SetTrigger("SwordPowerAttackT");
            
            if (enableDebugLogs)
                Debug.Log($"[{name}] 💥 SWORD POWER ATTACK ANIMATION TRIGGERED!");
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning($"[{name}] Cannot trigger sword power attack - animator or shooting layer missing!");
        }
        
        // Reset shooting state after power attack animation (longer than normal attack)
        _resetShootingCoroutine = StartCoroutine(ResetShootingState(1.0f));
    }
    
    
    private IEnumerator ResetShootingState(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // CRITICAL FIX: Instantly drop shooting layer weight to 0 FIRST
        // This immediately stops the shooting animation from showing
        SetTargetWeight(ref _targetShootingWeight, 0f);
        
        // Force immediate weight application (bypass blending system)
        _currentShootingWeight = 0f;
        if (handAnimator != null && handAnimator.layerCount > SHOOTING_LAYER)
        {
            handAnimator.SetLayerWeight(SHOOTING_LAYER, 0f);
        }
        
        // 🔧 CRITICAL FIX: Reset base layer idle animation to start from beginning
        // This prevents the "snap down" effect when shooting layer weight goes to 0
        ResetBaseLayerToIdle();
        
        // Wait 2 frames for animator to fully process the weight change
        // This ensures the shooting layer is completely invisible before next shot
        yield return null;
        yield return null;
        
        CurrentShootingState = ShootingState.None;
        
        // Clear coroutine reference
        _resetShootingCoroutine = null;
        
        // CRITICAL: Notify state manager that shooting stopped
        if (_stateManager != null)
        {
            _stateManager.NotifyShootingCompleted(isLeftHand);
        }
    }
    
    /// <summary>
    /// 🔧 NEW FIX: Waits for actual animation to finish instead of hardcoded delay
    /// This prevents premature layer weight reset that causes snap-to-idle issues
    /// </summary>
    private IEnumerator ResetShootingStateWhenAnimationFinishes(string stateName)
    {
        if (handAnimator == null || handAnimator.layerCount <= SHOOTING_LAYER)
        {
            yield break;
        }
        
        // Wait 1 frame for the animator to process the trigger
        yield return null;
        
        // Get the state info to check animation length
        AnimatorStateInfo stateInfo = handAnimator.GetCurrentAnimatorStateInfo(SHOOTING_LAYER);
        
        // SAFETY: If we're not in the expected state, wait for it (max 1 second)
        float waitTimeout = 0f;
        while (!stateInfo.IsName(stateName) && waitTimeout < 1f)
        {
            yield return null;
            waitTimeout += Time.deltaTime;
            stateInfo = handAnimator.GetCurrentAnimatorStateInfo(SHOOTING_LAYER);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{name}] 🎬 Animation '{stateName}' started. Length: {stateInfo.length}s. Waiting for completion...");
        }
        
        // Get animation length from the state
        float animationLength = stateInfo.length;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{name}] ⏱️ Waiting for {animationLength:F2} seconds for animation to complete...");
        }
        
        // SIMPLEST SOLUTION: Just wait for the animation duration!
        // This works regardless of transitions, state changes, or normalizedTime weirdness
        // We use 90% of the length to allow for a smooth transition at the end
        yield return new WaitForSeconds(animationLength * 0.9f);
        
        if (enableDebugLogs)
        {
            stateInfo = handAnimator.GetCurrentAnimatorStateInfo(SHOOTING_LAYER);
            Debug.Log($"[{name}] ✅ Animation '{stateName}' duration complete! (Waited: {animationLength * 0.9f:F2}s, Current normalizedTime: {stateInfo.normalizedTime:F2})");
        }
        
        // Now safely reset the shooting state
        SetTargetWeight(ref _targetShootingWeight, 0f);
        _currentShootingWeight = 0f;
        
        if (handAnimator != null && handAnimator.layerCount > SHOOTING_LAYER)
        {
            handAnimator.SetLayerWeight(SHOOTING_LAYER, 0f);
        }
        
        // 🔧 CRITICAL FIX: Reset base layer idle animation to start from beginning
        // This prevents the "snap down" effect when shooting layer weight goes to 0
        ResetBaseLayerToIdle();
        
        yield return null;
        yield return null;
        
        CurrentShootingState = ShootingState.None;
        _resetShootingCoroutine = null;
        
        if (_stateManager != null)
        {
            _stateManager.NotifyShootingCompleted(isLeftHand);
        }
    }
    
    // === EMOTE ANIMATIONS (Override Layer) ===
    
    public void PlayEmote(EmoteState emoteState)
    {
        Debug.Log($"[{name}] 🎭 PlayEmote called with emoteState: {emoteState}");
        
        // PRIORITY: Shooting blocks emotes - don't start emote if shooting
        if (CurrentShootingState != ShootingState.None)
        {
            Debug.LogWarning($"[{name}] ❌ Emote BLOCKED - shooting is active (priority)");
            return;
        }
        
        // NO LOCKING - Let animator handle everything naturally with exit time!
        // Emotes can be interrupted by anything as long as exit time is respected
        
        CurrentEmoteState = emoteState;
        
        // Enable emote layer - let Unity Animator blend naturally
        SetTargetWeight(ref _targetEmoteWeight, 1f);
        
        Debug.Log($"[{name}] ✅ Emote layer weight set to 1.0, CurrentEmoteState = {CurrentEmoteState}");
        
        // Validate animator and layer exist before setting parameters
        if (handAnimator != null && handAnimator.layerCount > EMOTE_LAYER)
        {
            int emoteIndexValue = (int)emoteState;
            
            Debug.Log($"[{name}] 🎬 Setting animator parameters: emoteIndex={emoteIndexValue}, triggering PlayEmote");
            
            // Set the emoteIndex FIRST before triggering
            handAnimator.SetInteger("emoteIndex", emoteIndexValue);
            
            // Then trigger the emote - Unity Animator will handle the rest!
            handAnimator.SetTrigger("PlayEmote");
            
            // Force immediate update
            handAnimator.Update(0f);
            
            Debug.Log($"[{name}] 🎬 Animator updated - emote should be playing now");
        }
        else
        {
            Debug.LogError($"[{name}] ❌ handAnimator is NULL or emote layer missing! Cannot play emote!");
            // Cleanup state since emote failed
            CurrentEmoteState = EmoteState.None;
            SetTargetWeight(ref _targetEmoteWeight, 0f);
            return;
        }
        
        // NO MANUAL TRACKING - Unity Animator handles completion via exit time!
        // The layer weight will naturally blend back when animator exits the emote state
        _emoteMonitorCoroutine = StartCoroutine(MonitorEmoteLayerNaturally());
    }
    
    /// <summary>
    /// Monitors the emote layer to detect when animator naturally exits the emote.
    /// This respects Unity's exit time and allows natural interruptions.
    /// </summary>
    private IEnumerator MonitorEmoteLayerNaturally()
    {
        // Wait a frame for animator to enter emote state
        yield return null;
        
        if (handAnimator == null) yield break;
        
        // Get initial state info
        AnimatorStateInfo initialState = handAnimator.GetCurrentAnimatorStateInfo(EMOTE_LAYER);
        float emoteDuration = initialState.length;
        
        if (enableDebugLogs)
            Debug.Log($"[{name}] Emote started - Duration: {emoteDuration}s, State: {initialState.shortNameHash}");
        
        // Wait for the animation to complete based on its actual length
        // Add a small buffer to ensure animation finishes
        yield return new WaitForSeconds(emoteDuration + 0.1f);
        
        // Animation complete - unlock hand
        CurrentEmoteState = EmoteState.None;
        SetTargetWeight(ref _targetEmoteWeight, 0f);
        _emoteMonitorCoroutine = null;
        
        if (enableDebugLogs)
            Debug.Log($"[{name}] Emote completed naturally after {emoteDuration}s");
    }
    
    
    // === ABILITY ANIMATIONS (Override Layer) ===
    
    public void UseArmorPlate()
    {
        if (CurrentAbilityState != AbilityState.None)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[{name}] UseArmorPlate blocked - already in ability state: {CurrentAbilityState}");
            return;
        }
        
        CurrentAbilityState = AbilityState.ArmorPlate;
        
        // Validate animator and ability layer exist
        if (handAnimator != null && handAnimator.layerCount > ABILITY_LAYER)
        {
            if (enableDebugLogs)
                Debug.Log($"[{name}] 🎬 ARMOR PLATE ANIMATION TRIGGERED - Setting up Ability Layer");
            
            // Set integer parameter FIRST (transition condition checks this)
            handAnimator.SetInteger("abilityType", (int)AbilityState.ArmorPlate);
            
            // Set trigger LAST (this initiates the transition)
            handAnimator.SetTrigger("ApplyPlate");
            
            // Force animator to update immediately so transition happens this frame
            handAnimator.Update(0f);
            
            if (enableDebugLogs)
            {
                AnimatorStateInfo abilityState = handAnimator.GetCurrentAnimatorStateInfo(ABILITY_LAYER);
                Debug.Log($"[{name}] Ability Layer State: {abilityState.shortNameHash}, Weight: {handAnimator.GetLayerWeight(ABILITY_LAYER)}");
            }
        }
        else
        {
            Debug.LogError($"[{name}] ❌ UseArmorPlate failed - handAnimator is NULL or ability layer missing!");
            // Cleanup state since ability failed
            CurrentAbilityState = AbilityState.None;
            return;
        }
        
        // Set target weight through system (no redundant direct SetLayerWeight call)
        SetTargetWeight(ref _targetAbilityWeight, 1f);
        
        // Start completion tracking (typical armor plate duration)
        StartCoroutine(CompleteAbilityAfterDuration(2.0f));
    }
    
    /// <summary>
    /// Track ability animation completion using ACTUAL clip length from animator
    /// This respects the animation's exit time properly
    /// </summary>
    private IEnumerator TrackAbilityCompletion(string animationName)
    {
        // Wait one frame for animator to transition to the ability state
        yield return null;
        
        float duration = 1.0f; // Default fallback
        
        if (handAnimator != null)
        {
            // DETAILED DEBUGGING: Check layer count and current state
            Debug.Log($"[{name}] 🔍 Animator has {handAnimator.layerCount} layers");
            Debug.Log($"[{name}] 🔍 Ability Layer ({ABILITY_LAYER}) weight: {handAnimator.GetLayerWeight(ABILITY_LAYER)}");
            
            // Get the actual animation clip length from the Ability layer
            AnimatorStateInfo stateInfo = handAnimator.GetCurrentAnimatorStateInfo(ABILITY_LAYER);
            
            Debug.Log($"[{name}] 🔍 Current state on Ability Layer: Hash={stateInfo.shortNameHash}, NormalizedTime={stateInfo.normalizedTime}, Length={stateInfo.length}");
            Debug.Log($"[{name}] 🔍 Looking for animation names: '{animationName}', 'R_{animationName}', 'L_{animationName}'");
            
            // Check if we're actually in the ability animation state
            if (stateInfo.IsName(animationName) || 
                stateInfo.IsName($"R_{animationName}") || 
                stateInfo.IsName($"L_{animationName}"))
            {
                duration = stateInfo.length;
                Debug.Log($"[{name}] ✅ Found animation! Duration: {duration}s");
            }
            else
            {
                Debug.LogWarning($"[{name}] ⚠️ Animation '{animationName}' NOT FOUND on first check! Waiting 0.1s...");
                
                // Not in the expected state yet, wait a bit longer
                yield return new WaitForSeconds(0.1f);
                
                stateInfo = handAnimator.GetCurrentAnimatorStateInfo(ABILITY_LAYER);
                duration = stateInfo.length;
                
                Debug.Log($"[{name}] 🔍 After delay - State Hash: {stateInfo.shortNameHash}, Length: {duration}s");
                
                // Check all possible state names
                bool foundState = stateInfo.IsName(animationName) || 
                                 stateInfo.IsName($"R_{animationName}") || 
                                 stateInfo.IsName($"L_{animationName}");
                
                if (!foundState)
                {
                    Debug.LogError($"[{name}] ❌ CRITICAL: Animation '{animationName}' STILL NOT FOUND after delay!");
                    Debug.LogError($"[{name}] ❌ This means the Animator transition is NOT working!");
                    Debug.LogError($"[{name}] ❌ Check Unity Animator: Layer 3 (Ability) needs transitions from 'Any State' to '{animationName}'");
                }
                else
                {
                    Debug.Log($"[{name}] ✅ Found animation after delay! Duration: {duration}s");
                }
            }
        }
        else
        {
            Debug.LogError($"[{name}] ❌ handAnimator is NULL in TrackAbilityCompletion!");
        }
        
        Debug.Log($"[{name}] ⏱️ Waiting {duration}s for animation to complete...");
        
        // Wait for the ACTUAL animation to complete
        yield return new WaitForSeconds(duration);
        
        // Animation complete - unlock hand
        CurrentAbilityState = AbilityState.None;
        
        // Disable ability layer
        SetTargetWeight(ref _targetAbilityWeight, 0f);
        
        Debug.Log($"[{name}] ✅ {animationName} animation completed and hand unlocked");
    }
    
    private IEnumerator CompleteAbilityAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        
        CurrentAbilityState = AbilityState.None;
        
        // Disable ability layer
        SetTargetWeight(ref _targetAbilityWeight, 0f);
    }
    
    /// <summary>
    /// Play grab animation (for picking up items from chests)
    /// </summary>
    public void PlayGrabAnimation()
    {
        if (CurrentAbilityState != AbilityState.None)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[{name}] PlayGrabAnimation blocked - already in ability state: {CurrentAbilityState}");
            return;
        }
        
        CurrentAbilityState = AbilityState.Grab;
        
        // Validate animator and ability layer exist
        if (handAnimator != null && handAnimator.layerCount > ABILITY_LAYER)
        {
            if (enableDebugLogs)
                Debug.Log($"[{name}] 🎬 GRAB ANIMATION TRIGGERED - Setting up Ability Layer");
            
            // Play grab sound
            if (soundEvents != null && soundEvents.grabItemSound != null)
            {
                soundEvents.grabItemSound.Play3D(transform.position);
                if (enableDebugLogs)
                    Debug.Log($"[{name}] 🔊 Playing grab item sound");
            }
            
            // Set integer parameter FIRST
            handAnimator.SetInteger("abilityType", (int)AbilityState.Grab);
            
            // Set trigger to initiate transition
            handAnimator.SetTrigger("PlayGrab");
            
            // Force immediate update
            handAnimator.Update(0f);
        }
        else
        {
            Debug.LogError($"[{name}] ❌ PlayGrabAnimation failed - handAnimator is NULL or ability layer missing!");
            // Cleanup state since ability failed
            CurrentAbilityState = AbilityState.None;
            return;
        }
        
        // Set target weight through system (no redundant direct SetLayerWeight call)
        SetTargetWeight(ref _targetAbilityWeight, 1f);
        
        // Start completion tracking using ACTUAL animation clip length
        StartCoroutine(TrackAbilityCompletion("Grab"));
    }
    
    /// <summary>
    /// Play open door animation (for interacting with keycard doors)
    /// </summary>
    public void PlayOpenDoorAnimation()
    {
        if (CurrentAbilityState != AbilityState.None)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[{name}] PlayOpenDoorAnimation blocked - already in ability state: {CurrentAbilityState}");
            return;
        }
        
        CurrentAbilityState = AbilityState.OpenDoor;
        
        // Validate animator and ability layer exist
        if (handAnimator != null && handAnimator.layerCount > ABILITY_LAYER)
        {
            if (enableDebugLogs)
                Debug.Log($"[{name}] 🎬 OPEN DOOR ANIMATION TRIGGERED - Setting up Ability Layer");
            
            // Play open door sound
            if (soundEvents != null && soundEvents.openDoorSound != null)
            {
                soundEvents.openDoorSound.Play3D(transform.position);
                if (enableDebugLogs)
                    Debug.Log($"[{name}] 🔊 Playing open door sound");
            }
            
            // Set integer parameter FIRST
            handAnimator.SetInteger("abilityType", (int)AbilityState.OpenDoor);
            
            // Set trigger to initiate transition
            handAnimator.SetTrigger("PlayOpenDoor");
            
            // Force immediate update
            handAnimator.Update(0f);
        }
        else
        {
            Debug.LogError($"[{name}] ❌ PlayOpenDoorAnimation failed - handAnimator is NULL or ability layer missing!");
            // Cleanup state since ability failed
            CurrentAbilityState = AbilityState.None;
            return;
        }
        
        // Set target weight through system (no redundant direct SetLayerWeight call)
        SetTargetWeight(ref _targetAbilityWeight, 1f);
        
        // Start completion tracking using ACTUAL animation clip length
        StartCoroutine(TrackAbilityCompletion("OpenDoor"));
    }
    
    // === UTILITY METHODS ===
    
    /// <summary>
    /// 🔧 CRITICAL FIX: Reset base layer to idle animation from the beginning
    /// This prevents the "snap down" effect when overlay layers (shooting/emote/ability) end
    /// The base layer was playing in the background - we need to restart it from frame 0
    /// </summary>
    private void ResetBaseLayerToIdle()
    {
        if (handAnimator == null) return;
        
        // Force base layer back to idle state at normalized time 0 (start of animation)
        // This ensures smooth transition when shooting/emote/ability layer weight drops to 0
        if (CurrentMovementState == MovementState.Idle)
        {
            // Get the current state info to find what state we're in
            AnimatorStateInfo currentState = handAnimator.GetCurrentAnimatorStateInfo(BASE_LAYER);
            
            // Replay the current state (idle) from the beginning
            // This works with animator parameters - we just reset the playback position
            handAnimator.Play(currentState.fullPathHash, BASE_LAYER, 0f);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[{name}] ✅ Reset base layer idle animation to start (normalizedTime = 0)");
            }
        }
        else
        {
            // If we're not in idle (e.g., walking, sprinting), the base layer animation should continue naturally
            // Don't reset it - just let the current movement animation play
            if (enableDebugLogs)
            {
                Debug.Log($"[{name}] ℹ️ Not resetting base layer - current movement state is {CurrentMovementState}, not Idle");
            }
        }
    }
    
    public void SetAnimationSpeed(float speed)
    {
        if (handAnimator != null)
        {
            // Natural animation speed - respect the requested speed
            handAnimator.speed = speed;
        }
    }
    
    public void ForceStopAllOverlays()
    {
        // Force stop all overlay animations
        CurrentShootingState = ShootingState.None;
        CurrentEmoteState = EmoteState.None;
        CurrentAbilityState = AbilityState.None;
        
        // Reset all layer weights to 0
        SetTargetWeight(ref _targetShootingWeight, 0f);
        SetTargetWeight(ref _targetEmoteWeight, 0f);
        SetTargetWeight(ref _targetAbilityWeight, 0f);
        
        if (handAnimator != null)
        {
            handAnimator.SetBool("IsBeamAc", false);
            // Reset other parameters as needed
        }
        
        // 🔧 CRITICAL FIX: Reset base layer idle animation to start from beginning
        // This prevents the "snap down" effect when all overlay layers are force-stopped
        ResetBaseLayerToIdle();
        
        // Clear coroutine reference before stopping all
        _resetShootingCoroutine = null;
        
        // Stop all running coroutines
        StopAllCoroutines();
    }
    
    // === DEBUG METHODS ===
    
    public void DEBUG_LogCurrentState()
    {
        Debug.Log($"[IndividualLayeredHandController] {name} - Movement: {CurrentMovementState}, Shooting: {CurrentShootingState}, Emote: {CurrentEmoteState}, Ability: {CurrentAbilityState}");
    }
}
