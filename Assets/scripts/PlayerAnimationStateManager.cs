using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// SINGLE SOURCE OF TRUTH for all player animation states.
/// All animation decisions flow through this manager to prevent conflicts.
/// Validates states and coordinates with all relevant systems before updating animators.
/// </summary>
public class PlayerAnimationStateManager : MonoBehaviour
{
    [Header("System References")]
    [SerializeField] private LayeredHandAnimationController handAnimationController;
    [SerializeField] private AAAMovementController movementController;
    [SerializeField] private CleanAAACrouch crouchController;
    [SerializeField] private PlayerEnergySystem energySystem;
    [SerializeField] private ArmorPlateSystem armorPlateSystem;
    
    [Header("Animation State")]
    [SerializeField] private PlayerAnimationState currentState = PlayerAnimationState.Idle;
    [SerializeField] private bool isLeftHandLocked = false;
    [SerializeField] private bool isRightHandLocked = false;
    
    [Header("Action States")]
    [SerializeField] private bool isLeftHandShooting = false;
    [SerializeField] private bool isRightHandShooting = false;
    [SerializeField] private bool isLeftHandBeaming = false;
    [SerializeField] private bool isRightHandBeaming = false;
    [SerializeField] private bool isEmoting = false;
    [SerializeField] private bool isApplyingArmorPlate = false;
    
    // Combined states for convenience
    public bool IsShooting => isLeftHandShooting || isRightHandShooting;
    public bool IsBeaming => isLeftHandBeaming || isRightHandBeaming;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    [Header("SPRINT ONLY TEST MODE")]
    [SerializeField] [Tooltip("TESTING: Disables ALL animations except Sprint - for isolated testing")]
    private bool sprintOnlyTestMode = false; // ✅ DISABLED - Sprint works perfectly!
    
    // State tracking
    private PlayerAnimationState previousState = PlayerAnimationState.Idle;
    private float lastStateChangeTime = 0f;
    private float lastManualStateChangeTime = -999f; // Track manual state changes
    private const float STATE_CHANGE_COOLDOWN = 0.05f; // Prevent spam
    private const float MANUAL_STATE_OVERRIDE_DURATION = 0.1f; // How long manual states override auto-detection
    
    // One-shot animation tracking (Jump, Land)
    private bool isPlayingOneShotAnimation = false;
    private float oneShotAnimationEndTime = -999f;
    private const float LAND_ANIMATION_DURATION = 0.5f; // Land animation plays for 0.5 seconds
    private const float JUMP_ANIMATION_DURATION = 0.6f; // Jump animation plays for 0.6 seconds (increased to ensure full play)
    
    // Action tracking
    private Dictionary<string, float> actionCooldowns = new Dictionary<string, float>();
    private const float ACTION_COOLDOWN = 0.1f;
    
    // Idle delay system - only return to idle after period of inactivity
    private float lastActivityTime = 0f;
    [Header("Idle Delay")]
    [SerializeField] [Tooltip("Seconds of no input before returning to idle")]
    private float idleDelayDuration = 3f;
    
    public enum PlayerAnimationState
    {
        Idle = 0,
        Walk = 1,
        Sprint = 2,
        Jump = 3,
        Land = 4,
        TakeOff = 5,
        Slide = 6,
        Dive = 7,
        Flight = 8,
        Falling = 14  // Falling animation - matches IndividualLayeredHandController.MovementState.Falling
    }
    
    // Public Properties - READ ONLY
    public PlayerAnimationState CurrentState => currentState;
    public bool IsLeftHandLocked => isLeftHandLocked;
    public bool IsRightHandLocked => isRightHandLocked;
    public bool IsEmoting => isEmoting;
    public bool IsApplyingArmorPlate => isApplyingArmorPlate;
    
    void Awake()
    {
        // Auto-find references if not assigned
        if (handAnimationController == null)
            handAnimationController = GetComponent<LayeredHandAnimationController>();
        if (movementController == null)
            movementController = GetComponent<AAAMovementController>();
        if (crouchController == null)
            crouchController = GetComponent<CleanAAACrouch>();
        if (energySystem == null)
            energySystem = GetComponent<PlayerEnergySystem>();
        if (armorPlateSystem == null)
            armorPlateSystem = GetComponent<ArmorPlateSystem>();
        
        // Initialize activity time to allow idle at start
        lastActivityTime = Time.time - idleDelayDuration; // Start in idle-ready state
    }
    
    void Update()
    {
        // Handle emote input in Update (for responsive input detection)
        HandleEmoteInput();
    }
    
    void LateUpdate()
    {
        // CRITICAL: Run auto-detection in LateUpdate so manual triggers (from other scripts' Update) happen FIRST
        UpdateMovementState();
        UpdateActionStates();
        CleanupExpiredCooldowns();
    }
    
    /// <summary>
    /// Handle emote input directly in the centralized system
    /// EMOTES ARE RIGHT-HAND ONLY - Arrow Keys
    /// </summary>
    private void HandleEmoteInput()
    {
        // Check for emote keys (Arrow Keys - RIGHT HAND ONLY)
        if (Input.GetKeyDown(Controls.Emote1)) // Up Arrow
        {
            RequestEmote(1);
        }
        else if (Input.GetKeyDown(Controls.Emote2)) // Down Arrow
        {
            RequestEmote(2);
        }
        else if (Input.GetKeyDown(Controls.Emote3)) // Left Arrow
        {
            RequestEmote(3);
        }
        else if (Input.GetKeyDown(Controls.Emote4)) // Right Arrow
        {
            RequestEmote(4);
        }
    }
    
    #region Movement State Management
    
    /// <summary>
    /// Updates the primary movement animation state based on all input systems
    /// </summary>
    private void UpdateMovementState()
    {
        // CRITICAL: Check if one-shot animation should be unlocked early due to grounded state change
        if (isPlayingOneShotAnimation)
        {
            // If we're playing Jump animation and just became grounded, unlock IMMEDIATELY!
            if (currentState == PlayerAnimationState.Jump && movementController != null && movementController.IsGrounded)
            {
                isPlayingOneShotAnimation = false;
                lastManualStateChangeTime = -999f;
                // Fall through to immediately detect Sprint/Walk/Idle
            }
            // 🎯 NEW: If Jump animation timer expires but player is still airborne, transition to Falling!
            else if (currentState == PlayerAnimationState.Jump && Time.time >= oneShotAnimationEndTime)
            {
                bool isStillAirborne = movementController != null && !movementController.IsGrounded;
                if (isStillAirborne)
                {
                    // Jump animation complete but still in air - transition to falling
                    isPlayingOneShotAnimation = false;
                    lastManualStateChangeTime = -999f;
                    // Fall through to detect Falling state
                }
                else
                {
                    // Landed during jump animation
                    isPlayingOneShotAnimation = false;
                    lastManualStateChangeTime = -999f;
                    // Fall through to detect grounded states
                }
            }
            // Otherwise wait for timer
            else if (Time.time < oneShotAnimationEndTime)
            {
                // SILENT PROTECTION - Let one-shot animation continue
                return;
            }
            else
            {
                // Timer expired (for Land or other one-shots)
                isPlayingOneShotAnimation = false;
                lastManualStateChangeTime = -999f;
                // Fall through to immediately detect and apply correct state
            }
        }
        
        // CRITICAL: Don't override manual state changes for a short duration
        // This prevents auto-detection from fighting with manual SetMovementState() calls
        // NOTE: One-shot completion clears this timer, so Sprint resumes INSTANTLY after Jump/Land!
        if (Time.time - lastManualStateChangeTime < MANUAL_STATE_OVERRIDE_DURATION)
        {
            return; // Skip auto-detection, let manual state persist
        }
        
        PlayerAnimationState targetState = DetermineMovementState();
        
        if (CanChangeMovementState(targetState))
        {
            // This is an auto-detected state change, not manual
            previousState = currentState;
            currentState = targetState;
            lastStateChangeTime = Time.time;
            
            // 🎯 DIRECTIONAL SPRINT: Detect sprint direction and pass to animators
            IndividualLayeredHandController.SprintDirection sprintDir = IndividualLayeredHandController.SprintDirection.Forward;
            if (targetState == PlayerAnimationState.Sprint && movementController != null)
            {
                sprintDir = DetermineSprintDirection();
            }
            
            // Update hand animators with sprint direction
            if (handAnimationController != null)
            {
                handAnimationController.SetMovementState((int)currentState, sprintDir);
            }
        }
    }
    
    /// <summary>
    /// 🎯 Determine sprint direction from movement input
    /// Uses AAAMovementController's tracked sprint input for accurate direction
    /// </summary>
    private IndividualLayeredHandController.SprintDirection DetermineSprintDirection()
    {
        if (movementController == null)
            return IndividualLayeredHandController.SprintDirection.Forward;
        
        Vector2 sprintInput = movementController.CurrentSprintInput;
        
        // Thresholds for direction detection
        const float DIAGONAL_THRESHOLD = 0.5f; // Both X and Y above this = diagonal
        const float CARDINAL_THRESHOLD = 0.3f; // Single axis above this = cardinal direction
        
        float absX = Mathf.Abs(sprintInput.x);
        float absY = Mathf.Abs(sprintInput.y);
        
        // Backward sprint (S key)
        if (sprintInput.y < -CARDINAL_THRESHOLD)
        {
            if (absX > DIAGONAL_THRESHOLD)
            {
                // Diagonal backward
                return sprintInput.x < 0 ? 
                    IndividualLayeredHandController.SprintDirection.BackwardLeft : 
                    IndividualLayeredHandController.SprintDirection.BackwardRight;
            }
            return IndividualLayeredHandController.SprintDirection.Backward;
        }
        
        // Forward sprint (W key) - default/most common
        if (sprintInput.y > CARDINAL_THRESHOLD)
        {
            if (absX > DIAGONAL_THRESHOLD)
            {
                // Diagonal forward
                return sprintInput.x < 0 ? 
                    IndividualLayeredHandController.SprintDirection.ForwardLeft : 
                    IndividualLayeredHandController.SprintDirection.ForwardRight;
            }
            return IndividualLayeredHandController.SprintDirection.Forward;
        }
        
        // Pure strafe (A or D key only)
        if (absX > CARDINAL_THRESHOLD)
        {
            return sprintInput.x < 0 ? 
                IndividualLayeredHandController.SprintDirection.StrafeLeft : 
                IndividualLayeredHandController.SprintDirection.StrafeRight;
        }
        
        // Fallback to forward
        return IndividualLayeredHandController.SprintDirection.Forward;
    }
    
    /// <summary>
    /// Determines what the movement state should be based on all systems
    /// </summary>
    private PlayerAnimationState DetermineMovementState()
    {
        // 🔥 SPRINT ONLY TEST MODE - EVERYTHING ELSE DISABLED 🔥
        if (sprintOnlyTestMode)
        {
            // ONLY check for sprint, ignore everything else
            if (energySystem != null && energySystem.IsCurrentlySprinting)
            {
                MarkActivity();
                return PlayerAnimationState.Sprint;
            }
            else
            {
                return PlayerAnimationState.Idle;
            }
        }
        
        // Priority order: Dive > Slide > Flight > Sprint > Walk > Falling > Idle
        // NOTE: Jump/Land are one-shot animations handled manually with timer locks
        // NOTE: Falling is auto-detected when airborne and not jumping/landing/diving
        // Dive/Slide are maintained by auto-detection while their state flags are active
        
        // CRITICAL: Don't auto-detect dive/slide/sprint when player is in the air (except during actual dive)
        bool isGrounded = movementController != null && movementController.IsGrounded;
        
        // Dive has highest priority - if diving (actual airborne dive), always return Dive
        if (crouchController != null && crouchController.IsDiving)
        {
            MarkActivity(); // Player is diving = active
            return PlayerAnimationState.Dive;
        }
        
        // Slide has second priority - if sliding, always return Slide (only when grounded)
        if (crouchController != null && crouchController.IsSliding && isGrounded)
        {
            MarkActivity(); // Player is sliding = active
            return PlayerAnimationState.Slide;
        }
        
        // Check for flight (flight mode is handled by external systems)
        // We'll detect flight mode through other means or external calls
        
        // Check for sprint - ONLY when grounded!
        if (energySystem != null && energySystem.IsCurrentlySprinting && isGrounded)
        {
            MarkActivity(); // Player is sprinting = active
            return PlayerAnimationState.Sprint;
        }
        
        // Check for walk (detect movement input)
        bool hasMovementInput = Input.GetKey(Controls.MoveForward) || Input.GetKey(Controls.MoveBackward) || 
                               Input.GetKey(Controls.MoveLeft) || Input.GetKey(Controls.MoveRight);
        
        if (hasMovementInput && movementController != null && movementController.IsGrounded)
        {
            MarkActivity(); // Player is moving = active
            return PlayerAnimationState.Walk;
        }
        
        // 🎯 NEW: Falling animation - plays when airborne and not in a one-shot animation
        // This uses AAAMovementController.IsFalling as single source of truth
        // Shooting layer will naturally override this (shooting layer is Override blend mode)
        if (movementController != null && movementController.IsFalling && !isGrounded)
        {
            MarkActivity(); // Player is falling = active
            return PlayerAnimationState.Falling;
        }
        
        // IDLE DELAY: Only return to idle after period of inactivity
        float timeSinceLastActivity = Time.time - lastActivityTime;
        if (timeSinceLastActivity < idleDelayDuration)
        {
            // Not enough time has passed, keep current state (don't force idle)
            return currentState;
        }
        
        // Default to idle after delay period
        return PlayerAnimationState.Idle;
    }
    
    /// <summary>
    /// Validates if we can change to the target movement state
    /// </summary>
    private bool CanChangeMovementState(PlayerAnimationState targetState)
    {
        // Prevent spam changes
        if (Time.time - lastStateChangeTime < STATE_CHANGE_COOLDOWN)
            return false;
            
        // Don't change if already in target state
        if (currentState == targetState)
            return false;
            
        // Check if hands are locked in critical actions
        if (IsInCriticalAction())
        {
            // Only allow high-priority movement states to interrupt
            return IsHighPriorityMovementState(targetState);
        }
        
        return true;
    }
    
    /// <summary>
    /// Sets the movement state and updates all animators
    /// PUBLIC so movement systems can trigger immediate state changes
    /// </summary>
    public void SetMovementState(PlayerAnimationState newState)
    {
        // Sprint only test mode - block all manual triggers
        if (sprintOnlyTestMode)
        {
            return; // Ignore all manual state changes in test mode
        }
        previousState = currentState;
        currentState = newState;
        lastStateChangeTime = Time.time;
        
        // CRITICAL: When manually setting state, prevent auto-detection from overriding it for a short time
        // This prevents the Update() loop from immediately changing it back
        lastManualStateChangeTime = Time.time;
        
        // One-shot animations (Jump, Land) need to complete before auto-detection can override
        if (newState == PlayerAnimationState.Jump)
        {
            isPlayingOneShotAnimation = true;
            oneShotAnimationEndTime = Time.time + JUMP_ANIMATION_DURATION;
        }
        else if (newState == PlayerAnimationState.Land)
        {
            isPlayingOneShotAnimation = true;
            oneShotAnimationEndTime = Time.time + LAND_ANIMATION_DURATION;
        }
        
        // Mark activity for any non-idle state
        if (newState != PlayerAnimationState.Idle)
        {
            MarkActivity(); // Manual state changes (Jump, Land, etc.) = active
        }
        
        // 🎯 DIRECTIONAL SPRINT: Detect sprint direction for manual state changes too
        IndividualLayeredHandController.SprintDirection sprintDir = IndividualLayeredHandController.SprintDirection.Forward;
        if (newState == PlayerAnimationState.Sprint && movementController != null)
        {
            sprintDir = DetermineSprintDirection();
        }
        
        // Update hand animators with sprint direction
        if (handAnimationController != null)
        {
            handAnimationController.SetMovementState((int)currentState, sprintDir);
        }
    }
    
    /// <summary>
    /// Overload that accepts int for easier calling from other systems
    /// </summary>
    public void SetMovementState(int stateIndex)
    {
        SetMovementState((PlayerAnimationState)stateIndex);
    }
    
    private bool IsHighPriorityMovementState(PlayerAnimationState state)
    {
        return state == PlayerAnimationState.Jump || 
               state == PlayerAnimationState.Land ||
               state == PlayerAnimationState.Dive;
    }
    
    #endregion
    
    #region Action State Management
    
    /// <summary>
    /// Updates all action states (shooting, emoting, etc.)
    /// </summary>
    private void UpdateActionStates()
    {
        // Action states are managed by their respective systems
        // This method just tracks their current status
        
        // Update shooting state from external systems
        // (Will be called by shooting systems)
        
        // Update emoting state from external systems
        // (Will be called by emote systems)
        
        // Update armor plate state (check if system is currently applying plates)
        if (armorPlateSystem != null)
        {
            // ArmorPlateSystem doesn't have IsApplyingPlate() method
            // We'll track this through the request/completion system instead
            // The state will be managed by our own tracking when RequestArmorPlate() is called
        }
    }
    
    /// <summary>
    /// Updates which hands are locked based on current actions
    /// </summary>
    private void UpdateHandLockStates()
    {
        // Right hand locks
        bool rightHandShouldBeLocked = isApplyingArmorPlate || isEmoting;
        
        // Left hand locks (currently none, but extensible)
        bool leftHandShouldBeLocked = false;
        
        if (isLeftHandLocked != leftHandShouldBeLocked)
        {
            isLeftHandLocked = leftHandShouldBeLocked;
        }
        
        if (isRightHandLocked != rightHandShouldBeLocked)
        {
            isRightHandLocked = rightHandShouldBeLocked;
        }
    }
    
    private bool IsInCriticalAction()
    {
        return isApplyingArmorPlate; // Armor plates cannot be interrupted
    }
    
    #endregion
    
    #region Public API - Actions
    
    /// <summary>
    /// Mark that player is actively doing something (prevents idle)
    /// </summary>
    private void MarkActivity()
    {
        lastActivityTime = Time.time;
    }
    
    /// <summary>
    /// Request to start shooting animation
    /// </summary>
    public bool RequestShootingStart(bool isLeftHand = false)
    {
        string actionKey = isLeftHand ? "ShootLeft" : "ShootRight";
        
        if (!CanPerformAction(actionKey))
            return false;
            
        bool handLocked = isLeftHand ? isLeftHandLocked : isRightHandLocked;
        if (handLocked)
        {
            return false;
        }
        
        // Set per-hand shooting state
        if (isLeftHand)
            isLeftHandShooting = true;
        else
            isRightHandShooting = true;
        
        MarkActivity(); // Player is shooting = active
        SetActionCooldown(actionKey);
        
        // Update animator
        if (handAnimationController != null)
        {
            if (isLeftHand)
                handAnimationController.StartShootingLeft();
            else
                handAnimationController.StartShootingRight();
        }
        
        return true;
    }
    
    /// <summary>
    /// Request to stop shooting animation
    /// </summary>
    public void RequestShootingStop(bool isLeftHand = false)
    {
        // Clear per-hand shooting state
        if (isLeftHand)
            isLeftHandShooting = false;
        else
            isRightHandShooting = false;
        
        // Update animator
        if (handAnimationController != null)
        {
            if (isLeftHand)
                handAnimationController.StopShootingLeft();
            else
                handAnimationController.StopShootingRight();
        }
    }
    
    /// <summary>
    /// Request to start beam animation
    /// </summary>
    public bool RequestBeamStart(bool isLeftHand = false)
    {
        string actionKey = isLeftHand ? "BeamLeft" : "BeamRight";
        
        if (!CanPerformAction(actionKey))
            return false;
            
        bool handLocked = isLeftHand ? isLeftHandLocked : isRightHandLocked;
        if (handLocked)
        {
            return false;
        }
        
        // Set per-hand beaming state
        if (isLeftHand)
            isLeftHandBeaming = true;
        else
            isRightHandBeaming = true;
        
        MarkActivity(); // Player is beaming = active
        SetActionCooldown(actionKey);
        
        // Update animator
        if (handAnimationController != null)
        {
            if (isLeftHand)
                handAnimationController.StartBeamLeft();
            else
                handAnimationController.StartBeamRight();
        }
        
        return true;
    }
    
    /// <summary>
    /// Request to stop beam animation
    /// </summary>
    public void RequestBeamStop(bool isLeftHand = false)
    {
        // Clear per-hand beaming state
        if (isLeftHand)
            isLeftHandBeaming = false;
        else
            isRightHandBeaming = false;
        
        // Update animator
        if (handAnimationController != null)
        {
            if (isLeftHand)
                handAnimationController.StopBeamLeft();
            else
                handAnimationController.StopBeamRight();
        }
    }
    
    /// <summary>
    /// Request to play emote animation
    /// </summary>
    public bool RequestEmote(int emoteIndex)
    {
        Debug.Log($"[PlayerAnimationStateManager] 🎭 RequestEmote({emoteIndex}) called");
        
        if (!CanPerformAction("Emote"))
        {
            Debug.LogWarning($"[PlayerAnimationStateManager] ❌ Emote BLOCKED by cooldown");
            return false;
        }
            
        if (isRightHandLocked)
        {
            Debug.LogWarning($"[PlayerAnimationStateManager] ❌ Emote BLOCKED - right hand is locked");
            return false;
        }
        
        Debug.Log($"[PlayerAnimationStateManager] ✅ Emote approved - triggering animation");
        
        isEmoting = true;
        MarkActivity(); // Player is emoting = active
        SetActionCooldown("Emote");
        UpdateHandLockStates();
        
        // Update animator
        if (handAnimationController != null)
        {
            Debug.Log($"[PlayerAnimationStateManager] 🎬 Calling handAnimationController.PlayEmote({emoteIndex})");
            handAnimationController.PlayEmote(emoteIndex);
        }
        else
        {
            Debug.LogError($"[PlayerAnimationStateManager] ❌ handAnimationController is NULL!");
        }
        
        // Start emote completion tracking
        StartCoroutine(TrackEmoteCompletion());
        return true;
    }
    
    /// <summary>
    /// Request to play armor plate animation (called by ArmorPlateSystem)
    /// DOES NOT apply plates - only triggers animation
    /// </summary>
    public bool RequestArmorPlate()
    {
        if (!CanPerformAction("ArmorPlate"))
            return false;
            
        if (isRightHandLocked)
        {
            return false;
        }
        
        // ONLY trigger the animation - do NOT call ArmorPlateSystem (prevents circular calls)
        isApplyingArmorPlate = true;
        MarkActivity(); // Player is using armor plate = active
        UpdateHandLockStates();
        
        // Trigger animation on hand controller
        if (handAnimationController != null)
        {
            handAnimationController.PlayApplyPlateAnimation();
        }
        
        // Start tracking completion
        StartCoroutine(TrackArmorPlateCompletion());
        
        return true;
    }
    
    #endregion
    
    #region Action Completion Tracking
    
    /// <summary>
    /// Tracks emote completion and unlocks hand when done
    /// </summary>
    private IEnumerator TrackEmoteCompletion()
    {
        // Wait for emote to complete (duration will be handled by animator)
        yield return new WaitForSeconds(3.0f); // Default emote duration
        
        isEmoting = false;
        UpdateHandLockStates();
    }
    
    /// <summary>
    /// Tracks armor plate completion and unlocks hand when done
    /// </summary>
    private IEnumerator TrackArmorPlateCompletion()
    {
        // Wait for armor plate animation to complete
        yield return new WaitForSeconds(2.5f); // Armor plate animation duration
        
        isApplyingArmorPlate = false;
        UpdateHandLockStates();
    }
    
    /// <summary>
    /// Called by IndividualLayeredHandController when shooting animation completes
    /// </summary>
    public void NotifyShootingCompleted(bool isLeftHand)
    {
        if (isLeftHand)
        {
            isLeftHandShooting = false;
        }
        else
        {
            isRightHandShooting = false;
        }
    }
    
    /// <summary>
    /// Called by IndividualLayeredHandController when beam animation completes
    /// </summary>
    public void NotifyBeamCompleted(bool isLeftHand)
    {
        if (isLeftHand)
        {
            isLeftHandBeaming = false;
        }
        else
        {
            isRightHandBeaming = false;
        }
    }
    
    /// <summary>
    /// Called by external systems when actions complete (legacy support)
    /// </summary>
    public void NotifyActionCompleted(string actionType)
    {
        switch (actionType.ToLower())
        {
            case "emote":
                isEmoting = false;
                break;
            case "armorplate":
                isApplyingArmorPlate = false;
                break;
        }
        
        UpdateHandLockStates();
    }
    
    #endregion
    
    #region Cooldown System
    
    private bool CanPerformAction(string actionKey)
    {
        return !actionCooldowns.ContainsKey(actionKey) || 
               Time.time >= actionCooldowns[actionKey];
    }
    
    private void SetActionCooldown(string actionKey)
    {
        actionCooldowns[actionKey] = Time.time + ACTION_COOLDOWN;
    }
    
    private void CleanupExpiredCooldowns()
    {
        var keysToRemove = new List<string>();
        foreach (var kvp in actionCooldowns)
        {
            if (Time.time >= kvp.Value)
                keysToRemove.Add(kvp.Key);
        }
        
        foreach (string key in keysToRemove)
        {
            actionCooldowns.Remove(key);
        }
    }
    
    #endregion
    
    #region Utility
    
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[PlayerAnimationStateManager] {message}");
        }
    }
    
    /// <summary>
    /// Force unlock all hands (emergency use only)
    /// </summary>
    public void ForceUnlockAllHands()
    {
        isLeftHandLocked = false;
        isRightHandLocked = false;
        isEmoting = false;
        isApplyingArmorPlate = false;
        isLeftHandShooting = false;
        isRightHandShooting = false;
        isLeftHandBeaming = false;
        isRightHandBeaming = false;
    }
    
    /// <summary>
    /// Get detailed state info for debugging
    /// </summary>
    public string GetStateInfo()
    {
        return $"State: {currentState} | LeftLocked: {isLeftHandLocked} | RightLocked: {isRightHandLocked} | " +
               $"L_Shoot: {isLeftHandShooting} | R_Shoot: {isRightHandShooting} | L_Beam: {isLeftHandBeaming} | R_Beam: {isRightHandBeaming} | " +
               $"Combined_Shooting: {IsShooting} | Combined_Beaming: {IsBeaming} | Emoting: {isEmoting} | ArmorPlate: {isApplyingArmorPlate}";
    }
    
    #endregion
}
