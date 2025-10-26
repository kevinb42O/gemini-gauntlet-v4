using UnityEngine;

/// <summary>
/// Simple passthrough controller that forwards animation requests to PlayerAnimationStateManager.
/// This maintains backward compatibility while the new centralized system handles all logic.
/// </summary>
public class LayeredHandAnimationController : MonoBehaviour
{
    [Header("Hand Controllers - SINGLE MODEL ARCHITECTURE")]
    [Tooltip("Single left hand controller (visual changes via HolographicHandController)")]
    public IndividualLayeredHandController leftHandController;
    
    [Tooltip("Single right hand controller (visual changes via HolographicHandController)")]
    public IndividualLayeredHandController rightHandController;
    
    [Header("Holographic Visual Controllers")]
    [Tooltip("Left hand holographic effect controller")]
    public HolographicHandController leftHolographicController;
    
    [Tooltip("Right hand holographic effect controller")]
    public HolographicHandController rightHolographicController;
    
    [Header("System References")]
    [SerializeField] private PlayerAnimationStateManager stateManager;
    [SerializeField] private PlayerProgression playerProgression;
    
    [Header("Settings")]
    [Tooltip("Enable debug logging")]
    public bool enableDebugLogs = false;
    
    void Awake()
    {
        // Auto-find state manager
        if (stateManager == null)
            stateManager = GetComponent<PlayerAnimationStateManager>();
            
        // Auto-find player progression
        if (playerProgression == null)
            playerProgression = GetComponent<PlayerProgression>();
            
        // Auto-find hand controllers
        AutoFindHandControllers();
    }
    
    void Start()
    {
        // Setup opposite hand references for sprint synchronization
        SetupOppositeHandReferences();
    }
    
    private void AutoFindHandControllers()
    {
        // SINGLE MODEL ARCHITECTURE: Find the ONE left and ONE right hand controller
        IndividualLayeredHandController[] allControllers = FindObjectsOfType<IndividualLayeredHandController>();
        
        // Clear existing references
        leftHandController = null;
        rightHandController = null;
        
        foreach (var controller in allControllers)
        {
            // Only process player hands
            if (IsPlayerHand(controller))
            {
                if (controller.IsLeftHand)
                {
                    leftHandController = controller;
                    Debug.Log($"[LayeredHandAnimationController] Found LEFT hand controller: {controller.name}");
                }
                else
                {
                    rightHandController = controller;
                    Debug.Log($"[LayeredHandAnimationController] Found RIGHT hand controller: {controller.name}");
                }
            }
        }
        
        // Auto-find holographic controllers
        AutoFindHolographicControllers();
        
        if (leftHandController == null)
            Debug.LogWarning("[LayeredHandAnimationController] Left hand controller not found!");
        if (rightHandController == null)
            Debug.LogWarning("[LayeredHandAnimationController] Right hand controller not found!");
    }
    
    private void AutoFindHolographicControllers()
    {
        // Find holographic controllers on the hand GameObjects
        if (leftHandController != null)
        {
            leftHolographicController = leftHandController.GetComponent<HolographicHandController>();
            if (leftHolographicController == null)
                leftHolographicController = leftHandController.GetComponentInChildren<HolographicHandController>();
        }
        
        if (rightHandController != null)
        {
            rightHolographicController = rightHandController.GetComponent<HolographicHandController>();
            if (rightHolographicController == null)
                rightHolographicController = rightHandController.GetComponentInChildren<HolographicHandController>();
        }
        
        if (leftHolographicController != null)
            Debug.Log($"[LayeredHandAnimationController] Found LEFT holographic controller: {leftHolographicController.name}");
        if (rightHolographicController != null)
            Debug.Log($"[LayeredHandAnimationController] Found RIGHT holographic controller: {rightHolographicController.name}");
    }
    
    private bool IsPlayerHand(IndividualLayeredHandController controller)
    {
        if (controller == null) return false;
        
        // Check if it's under a Player GameObject
        Transform current = controller.transform;
        while (current != null)
        {
            if (current.name.ToLower().Contains("player"))
                return true;
            current = current.parent;
        }
        
        // SINGLE MODEL: Just check if it's a player hand by hierarchy
        // No need to check HandLevel since we only have 2 hands now
        return true;
    }
    
    /// <summary>
    /// Setup opposite hand references for sprint synchronization
    /// SINGLE MODEL: Just link the two hands together
    /// </summary>
    private void SetupOppositeHandReferences()
    {
        if (leftHandController != null && rightHandController != null)
        {
            leftHandController.oppositeHand = rightHandController;
            rightHandController.oppositeHand = leftHandController;
            Debug.Log("[LayeredHandAnimationController] Opposite hand references setup complete");
        }
    }
    
    private IndividualLayeredHandController GetCurrentLeftHand()
    {
        // SINGLE MODEL: Always return the same controller, visual changes handled by HolographicHandController
        return leftHandController;
    }
    
    private IndividualLayeredHandController GetCurrentRightHand()
    {
        // SINGLE MODEL: Always return the same controller, visual changes handled by HolographicHandController
        return rightHandController;
    }
    
    /// <summary>
    /// Update hand visual appearance when hand level changes
    /// Called by PlayerProgression when hand levels change
    /// PRIMARY = LEFT HAND (LMB), SECONDARY = RIGHT HAND (RMB)
    /// </summary>
    public void UpdateHandLevelVisuals(bool isPrimaryHand, int newLevel)
    {
        // CORRECT MAPPING: Primary (LMB) = LEFT hand, Secondary (RMB) = RIGHT hand
        HolographicHandController holographicController = isPrimaryHand ? leftHolographicController : rightHolographicController;
        
        if (holographicController != null)
        {
            holographicController.SetHandLevelColors(newLevel);
            Debug.Log($"[LayeredHandAnimationController] Updated {(isPrimaryHand ? "PRIMARY (LEFT)" : "SECONDARY (RIGHT)")} hand visuals to level {newLevel}");
        }
        else
        {
            Debug.LogWarning($"[LayeredHandAnimationController] Cannot update hand visuals - holographic controller not found for {(isPrimaryHand ? "primary (left)" : "secondary (right)")} hand");
        }
    }
    
    // === PASSTHROUGH METHODS - All logic handled by PlayerAnimationStateManager ===
    
    /// <summary>
    /// Set movement state - unified sprint animation (no direction variants)
    /// Animation speed is automatically synced in IndividualLayeredHandController.Update()
    /// </summary>
    public void SetMovementState(int movementState)
    {
        // Direct passthrough to individual hand controllers
        var leftHand = GetCurrentLeftHand();
        var rightHand = GetCurrentRightHand();
        
        leftHand?.SetMovementState((IndividualLayeredHandController.MovementState)movementState);
        rightHand?.SetMovementState((IndividualLayeredHandController.MovementState)movementState);
    }
    
    // === SHOOTING METHODS ===
    public void StartShootingLeft()
    {
        var hand = GetCurrentLeftHand();
        if (hand != null)
        {
            hand.TriggerShotgun();
        }
    }
    
    public void StartShootingRight()
    {
        var hand = GetCurrentRightHand();
        if (hand != null)
        {
            hand.TriggerShotgun();
        }
    }
    
    public void StopShootingLeft() { /* Shotgun is trigger-based, no stop needed */ }
    public void StopShootingRight() { /* Shotgun is trigger-based, no stop needed */ }
    
    public void StartBeamLeft() => GetCurrentLeftHand()?.StartBeamShooting();
    public void StartBeamRight() => GetCurrentRightHand()?.StartBeamShooting();
    public void StopBeamLeft() => GetCurrentLeftHand()?.StopBeamShooting();
    public void StopBeamRight() => GetCurrentRightHand()?.StopBeamShooting();
    
    /// <summary>
    /// CRITICAL: PRIMARY = LEFT HAND (LMB), SECONDARY = RIGHT HAND (RMB)
    /// </summary>
    public void PlayShootShotgun(bool isPrimaryHand)
    {
        if (isPrimaryHand)
            StartShootingLeft();  // PRIMARY = LEFT hand (LMB)
        else
            StartShootingRight(); // SECONDARY = RIGHT hand (RMB)
    }
    
    // === EMOTE METHODS (RIGHT HAND ONLY!) ===
    public void PlayEmote(int emoteIndex)
    {
        if (emoteIndex < 1 || emoteIndex > 5) return;
        
        // CRITICAL: ONLY RIGHT HAND PLAYS EMOTES - LEFT HAND HAS NO EMOTES!
        var rightHand = GetCurrentRightHand();
        
        IndividualLayeredHandController.EmoteState emoteState = (IndividualLayeredHandController.EmoteState)emoteIndex;
        
        // Only trigger RIGHT hand - left hand does NOT participate in emotes
        // Duration is determined by ACTUAL animation clip length from animator
        rightHand?.PlayEmote(emoteState);
    }
    
    // === ABILITY METHODS ===
    public void PlayApplyPlateAnimation()
    {
        GetCurrentRightHand()?.UseArmorPlate();
    }
    
    /// <summary>
    /// Play grab animation on right hand (for picking up items from chests)
    /// </summary>
    public void PlayGrabAnimation()
    {
        GetCurrentRightHand()?.PlayGrabAnimation();
    }
    
    /// <summary>
    /// Play open door animation on right hand (for interacting with keycard doors)
    /// </summary>
    public void PlayOpenDoorAnimation()
    {
        GetCurrentRightHand()?.PlayOpenDoorAnimation();
    }
    
    // === MOVEMENT METHODS ===
    public void PlayJumpBoth() => SetMovementState((int)IndividualLayeredHandController.MovementState.Jump);
    public void PlayLandBoth() => SetMovementState((int)IndividualLayeredHandController.MovementState.Land);
    public void PlaySlideBoth() => SetMovementState((int)IndividualLayeredHandController.MovementState.Slide);
    public void PlayDiveBoth() => SetMovementState((int)IndividualLayeredHandController.MovementState.Dive);
    public void PlayIdleBoth() => SetMovementState((int)IndividualLayeredHandController.MovementState.Idle);
    
    // === LEGACY COMPATIBILITY METHODS ===
    /// <summary>
    /// CRITICAL: PRIMARY = LEFT HAND (LMB), SECONDARY = RIGHT HAND (RMB)
    /// </summary>
    public void OnBeamStarted(bool isPrimaryHand)
    {
        if (isPrimaryHand) StartBeamLeft();  // PRIMARY = LEFT hand (LMB)
        else StartBeamRight();                // SECONDARY = RIGHT hand (RMB)
    }
    
    /// <summary>
    /// CRITICAL: PRIMARY = LEFT HAND (LMB), SECONDARY = RIGHT HAND (RMB)
    /// </summary>
    public void OnBeamStopped(bool isPrimaryHand)
    {
        if (isPrimaryHand) StopBeamLeft();   // PRIMARY = LEFT hand (LMB)
        else StopBeamRight();                 // SECONDARY = RIGHT hand (RMB)
    }
    
    public void OnPlayerJumped() => PlayJumpBoth();
    public void OnPlayerLanded() => PlayLandBoth();
    public void OnSlideStarted() => PlaySlideBoth();
    public void OnSlideStopped() { /* State manager handles this */ }
    public void PlayDiveAnimation() => PlayDiveBoth();
    public void StopDiveAnimation() { /* State manager handles this */ }
    
    // === UTILITY METHODS ===
    public void ForceUnlockArmorPlateAnimation()
    {
        GetCurrentRightHand()?.ForceStopAllOverlays();
    }
    
    public bool IsRightHandInArmorPlateState()
    {
        var rightHand = GetCurrentRightHand();
        return rightHand?.CurrentAbilityState == IndividualLayeredHandController.AbilityState.ArmorPlate;
    }
    
    public void SetGlobalAnimationSpeed(float speed)
    {
        leftHandController?.SetAnimationSpeed(speed);
        rightHandController?.SetAnimationSpeed(speed);
    }
    
    // === HAND LEVEL CHANGE HANDLER ===
    public void OnHandModelsChanged() 
    { 
        // DEPRECATED: No longer needed with single model architecture
        // Visual changes now handled by HolographicHandController
        Debug.Log("[LayeredHandAnimationController] OnHandModelsChanged called - no action needed (single model architecture)");
    }
    
    /// <summary>
    /// Called when hand levels change to update visual appearance
    /// </summary>
    public void OnHandLevelChanged(bool isPrimaryHand, int newLevel)
    {
        UpdateHandLevelVisuals(isPrimaryHand, newLevel);
    }
    public void OnCaptureStarted() { }
    public void OnCaptureCompleted() { }
    public void OnCaptureInterrupted() { }
    public void OnInteractPromptShown() { }
    public void OnInteractPromptHidden() { }
    public void EnterFlightMode() { }
    public void ExitFlightMode() { }
    public void SetFlightDirection(int direction) { }
    public void OnTakeOffStarted() { }
    public void OnShotgunFired(bool isPrimaryHand) => PlayShootShotgun(isPrimaryHand);
    public void StopAllBeams() { StopBeamLeft(); StopBeamRight(); }
    public void ForceStopAllOverlays() { }
}
