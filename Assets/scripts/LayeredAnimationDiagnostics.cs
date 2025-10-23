using UnityEngine;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// COMPREHENSIVE ANIMATION DIAGNOSTICS SYSTEM
/// Purpose: TOTAL FIXING OF ANIMATION SYSTEM
/// 
/// This script provides FULL logging of:
/// - Which animations are requested
/// - Which animations are actually playing
/// - Whether transitions exist in the Animator
/// - Layer weights and states
/// - Parameter values
/// - Blocking conditions
/// 
/// Attach this to the same GameObject as LayeredHandAnimationController
/// </summary>
public class LayeredAnimationDiagnostics : MonoBehaviour
{
    [Header("Diagnostic Settings")]
    [Tooltip("Enable full diagnostic logging")]
    public bool enableDiagnostics = true;
    
    [Tooltip("Log every frame (WARNING: VERY VERBOSE)")]
    public bool logEveryFrame = false;
    
    [Tooltip("Log only when animations change")]
    public bool logOnlyChanges = true;
    
    [Tooltip("Check for missing transitions")]
    public bool checkTransitions = true;
    
    [Tooltip("Log layer weights")]
    public bool logLayerWeights = true;
    
    [Tooltip("Log animator parameters")]
    public bool logParameters = true;
    
    [Tooltip("Log animation clips currently playing")]
    public bool logCurrentClips = true;
    
    [Header("References")]
    [SerializeField] private LayeredHandAnimationController layeredController;
    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private PlayerAnimationStateManager stateManager;
    [SerializeField] private IndividualLayeredHandController[] allHandControllers;
    
    [Header("Runtime Info")]
    [SerializeField] private string lastLoggedState = "";
    [SerializeField] private int diagnosticFrameCounter = 0;
    
    // Tracking previous states for change detection
    private Dictionary<IndividualLayeredHandController, AnimationState> previousStates = new Dictionary<IndividualLayeredHandController, AnimationState>();
    
    private struct AnimationState
    {
        public IndividualLayeredHandController.MovementState movement;
        public IndividualLayeredHandController.ShootingState shooting;
        public IndividualLayeredHandController.EmoteState emote;
        public IndividualLayeredHandController.AbilityState ability;
        public string currentClipName;
        public float[] layerWeights;
    }
    
    void Awake()
    {
        // Auto-find references
        if (layeredController == null)
            layeredController = GetComponent<LayeredHandAnimationController>();
        
        // Auto-find PlayerProgression
        if (playerProgression == null)
            playerProgression = FindObjectOfType<PlayerProgression>();
        
        // Auto-find PlayerAnimationStateManager
        if (stateManager == null)
            stateManager = GetComponent<PlayerAnimationStateManager>();
        
        // Find all hand controllers
        FindAllHandControllers();
        
        if (enableDiagnostics)
        {
            Debug.Log("═══════════════════════════════════════════════════════════════");
            Debug.Log("║  LAYERED ANIMATION DIAGNOSTICS SYSTEM INITIALIZED           ║");
            Debug.Log("║  Purpose: TOTAL ANIMATION SYSTEM DEBUGGING                  ║");
            Debug.Log("═══════════════════════════════════════════════════════════════");
            Debug.Log($"[AnimDiag] Found {allHandControllers.Length} hand controllers");
            
            foreach (var controller in allHandControllers)
            {
                if (controller != null)
                {
                    Debug.Log($"[AnimDiag]   - {controller.name} ({(controller.IsLeftHand ? "LEFT" : "RIGHT")} hand)");
                    
                    // Initialize previous state tracking
                    previousStates[controller] = GetCurrentState(controller);
                }
            }
        }
    }
    
    void Update()
    {
        if (!enableDiagnostics) return;
        
        diagnosticFrameCounter++;
        
        // Update active hand controllers every frame (in case hand level changes)
        UpdateActiveHandControllers();
        
        // Log every frame if enabled
        if (logEveryFrame)
        {
            LogAllHandStates("FRAME UPDATE");
        }
        // Log only on changes
        else if (logOnlyChanges)
        {
            CheckForStateChanges();
        }
        
        // Periodic full diagnostic (every 5 seconds)
        if (diagnosticFrameCounter % 300 == 0)
        {
            LogFullDiagnostic();
        }
    }
    
    private void FindAllHandControllers()
    {
        // SMART APPROACH: Only track the CURRENTLY ACTIVE hands!
        // No need to check all 8 hands - just ask which ones are active right now
        UpdateActiveHandControllers();
    }
    
    private void UpdateActiveHandControllers()
    {
        List<IndividualLayeredHandController> activeControllers = new List<IndividualLayeredHandController>();
        
        if (layeredController != null)
        {
            // Get ONLY the currently active hands based on hand level
            // This uses the same logic as LayeredHandAnimationController
            var leftHand = GetActiveLeftHand();
            var rightHand = GetActiveRightHand();
            
            if (leftHand != null && leftHand.gameObject.activeInHierarchy)
                activeControllers.Add(leftHand);
                
            if (rightHand != null && rightHand.gameObject.activeInHierarchy)
                activeControllers.Add(rightHand);
        }
        
        allHandControllers = activeControllers.ToArray();
    }
    
    private IndividualLayeredHandController GetActiveLeftHand()
    {
        if (layeredController == null) return null;
        
        // SINGLE MODEL ARCHITECTURE: Always return the same controller
        // Visual changes are handled by HolographicHandController, not model swapping
        return layeredController.leftHandController;
    }
    
    private IndividualLayeredHandController GetActiveRightHand()
    {
        if (layeredController == null) return null;
        
        // SINGLE MODEL ARCHITECTURE: Always return the same controller
        // Visual changes are handled by HolographicHandController, not model swapping
        return layeredController.rightHandController;
    }
    
    private void CheckForStateChanges()
    {
        // Now allHandControllers only contains the 2 ACTIVE hands!
        foreach (var controller in allHandControllers)
        {
            if (controller == null) continue;
            
            AnimationState currentState = GetCurrentState(controller);
            
            if (!previousStates.ContainsKey(controller))
            {
                previousStates[controller] = currentState;
                continue;
            }
            
            AnimationState prevState = previousStates[controller];
            
            // Check for any state changes
            bool hasChanged = false;
            StringBuilder changeLog = new StringBuilder();
            changeLog.AppendLine($"[AnimDiag] ═══ ANIMATION CHANGE DETECTED: {controller.name} ═══");
            
            // Movement state change
            if (currentState.movement != prevState.movement)
            {
                hasChanged = true;
                changeLog.AppendLine($"  MOVEMENT: {prevState.movement} → {currentState.movement}");
                
                // Check if transition exists
                if (checkTransitions)
                {
                    bool transitionExists = CheckTransitionExists(controller, "movementState", (int)currentState.movement);
                    changeLog.AppendLine($"    Transition exists: {(transitionExists ? "✓ YES" : "✗ NO - MISSING TRANSITION!")}");
                }
            }
            
            // Shooting state change
            if (currentState.shooting != prevState.shooting)
            {
                hasChanged = true;
                changeLog.AppendLine($"  SHOOTING: {prevState.shooting} → {currentState.shooting}");
                
                if (currentState.shooting == IndividualLayeredHandController.ShootingState.Shotgun)
                {
                    bool hasParameter = CheckParameterExists(controller, "ShotgunT");
                    changeLog.AppendLine($"    ShotgunT parameter exists: {(hasParameter ? "✓ YES" : "✗ NO - MISSING PARAMETER!")}");
                }
                else if (currentState.shooting == IndividualLayeredHandController.ShootingState.Beam)
                {
                    bool hasParameter = CheckParameterExists(controller, "IsBeamAc");
                    changeLog.AppendLine($"    IsBeamAc parameter exists: {(hasParameter ? "✓ YES" : "✗ NO - MISSING PARAMETER!")}");
                }
            }
            
            // Emote state change
            if (currentState.emote != prevState.emote)
            {
                hasChanged = true;
                changeLog.AppendLine($"  EMOTE: {prevState.emote} → {currentState.emote}");
                
                if (currentState.emote != IndividualLayeredHandController.EmoteState.None)
                {
                    bool hasParameter = CheckParameterExists(controller, "PlayEmote");
                    changeLog.AppendLine($"    PlayEmote trigger exists: {(hasParameter ? "✓ YES" : "✗ NO - MISSING PARAMETER!")}");
                }
            }
            
            // Ability state change
            if (currentState.ability != prevState.ability)
            {
                hasChanged = true;
                changeLog.AppendLine($"  ABILITY: {prevState.ability} → {currentState.ability}");
                
                if (currentState.ability == IndividualLayeredHandController.AbilityState.ArmorPlate)
                {
                    bool hasParameter = CheckParameterExists(controller, "ApplyPlate");
                    changeLog.AppendLine($"    ApplyPlate trigger exists: {(hasParameter ? "✓ YES" : "✗ NO - MISSING PARAMETER!")}");
                }
            }
            
            // Clip change
            if (currentState.currentClipName != prevState.currentClipName)
            {
                hasChanged = true;
                changeLog.AppendLine($"  CLIP: '{prevState.currentClipName}' → '{currentState.currentClipName}'");
            }
            
            // Layer weight changes (only log SIGNIFICANT changes to avoid spam)
            if (logLayerWeights && currentState.layerWeights != null && prevState.layerWeights != null)
            {
                for (int i = 0; i < Mathf.Min(currentState.layerWeights.Length, prevState.layerWeights.Length); i++)
                {
                    // Only log changes > 0.15 to avoid spam from smooth blending
                    if (Mathf.Abs(currentState.layerWeights[i] - prevState.layerWeights[i]) > 0.15f)
                    {
                        hasChanged = true;
                        string layerName = GetLayerName(i);
                        changeLog.AppendLine($"  LAYER {i} ({layerName}): {prevState.layerWeights[i]:F2} → {currentState.layerWeights[i]:F2}");
                    }
                }
            }
            
            if (hasChanged)
            {
                changeLog.AppendLine($"  Current Clip Playing: {currentState.currentClipName}");
                changeLog.AppendLine("═══════════════════════════════════════════════════════════");
                Debug.Log(changeLog.ToString());
            }
            
            // Update previous state
            previousStates[controller] = currentState;
        }
    }
    
    private AnimationState GetCurrentState(IndividualLayeredHandController controller)
    {
        AnimationState state = new AnimationState
        {
            movement = controller.CurrentMovementState,
            shooting = controller.CurrentShootingState,
            emote = controller.CurrentEmoteState,
            ability = controller.CurrentAbilityState,
            currentClipName = GetCurrentClipName(controller),
            layerWeights = GetLayerWeights(controller)
        };
        
        return state;
    }
    
    private string GetCurrentClipName(IndividualLayeredHandController controller)
    {
        if (controller == null || controller.handAnimator == null) return "NULL";
        
        // Check if animator has a controller assigned
        if (controller.handAnimator.runtimeAnimatorController == null)
            return "NO_CONTROLLER";
        
        try
        {
            // CRITICAL: Check for speed multiplier issues!
            AnimatorStateInfo stateInfo = controller.handAnimator.GetCurrentAnimatorStateInfo(0);
            if (Mathf.Abs(stateInfo.speedMultiplier - 1.0f) > 0.01f)
            {
                Debug.LogError($"[AnimDiag] 🚨 SPEED ISSUE DETECTED: {controller.name} has speedMultiplier = {stateInfo.speedMultiplier} (should be 1.0)! Current state hash: {stateInfo.shortNameHash}");
            }
            
            AnimatorClipInfo[] clipInfo = controller.handAnimator.GetCurrentAnimatorClipInfo(0);
            if (clipInfo.Length > 0)
                return clipInfo[0].clip.name;
        }
        catch (System.Exception)
        {
            return "ERROR";
        }
        
        return "NONE";
    }
    
    private bool IsPlayerHand(IndividualLayeredHandController controller)
    {
        if (controller == null) return false;
        
        // CRITICAL: Only check ACTIVE hands! Inactive hands don't need animation.
        if (!controller.gameObject.activeInHierarchy)
            return false;
        
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
    
    private float[] GetLayerWeights(IndividualLayeredHandController controller)
    {
        if (controller == null || controller.handAnimator == null) return null;
        
        // Check if animator has a controller assigned
        if (controller.handAnimator.runtimeAnimatorController == null)
            return null;
        
        try
        {
            int layerCount = controller.handAnimator.layerCount;
            float[] weights = new float[layerCount];
            
            for (int i = 0; i < layerCount; i++)
            {
                weights[i] = controller.handAnimator.GetLayerWeight(i);
            }
            
            return weights;
        }
        catch (System.Exception)
        {
            return null;
        }
    }
    
    private bool CheckTransitionExists(IndividualLayeredHandController controller, string parameterName, int targetValue)
    {
        if (controller == null || controller.handAnimator == null) return false;
        
        // This is a simplified check - in reality, you'd need to inspect the AnimatorController
        // For now, we just check if the parameter exists
        return CheckParameterExists(controller, parameterName);
    }
    
    private bool CheckParameterExists(IndividualLayeredHandController controller, string parameterName)
    {
        if (controller == null || controller.handAnimator == null) return false;
        
        foreach (AnimatorControllerParameter param in controller.handAnimator.parameters)
        {
            if (param.name == parameterName)
                return true;
        }
        
        return false;
    }
    
    private string GetLayerName(int layerIndex)
    {
        switch (layerIndex)
        {
            case 0: return "Base/Movement";
            case 1: return "Shooting";
            case 2: return "Emote";
            case 3: return "Ability";
            default: return "Unknown";
        }
    }
    
    private void LogAllHandStates(string context)
    {
        StringBuilder log = new StringBuilder();
        log.AppendLine($"[AnimDiag] ═══ {context} (Frame {diagnosticFrameCounter}) ═══");
        
        // Now allHandControllers only contains the 2 ACTIVE hands!
        foreach (var controller in allHandControllers)
        {
            if (controller == null) continue;
            
            log.AppendLine($"  {controller.name} ({(controller.IsLeftHand ? "LEFT" : "RIGHT")}):");
            log.AppendLine($"    Movement: {controller.CurrentMovementState}");
            log.AppendLine($"    Shooting: {controller.CurrentShootingState}");
            log.AppendLine($"    Emote: {controller.CurrentEmoteState}");
            log.AppendLine($"    Ability: {controller.CurrentAbilityState}");
            
            if (logCurrentClips)
            {
                string clipName = GetCurrentClipName(controller);
                log.AppendLine($"    Current Clip: {clipName}");
            }
            
            if (logLayerWeights)
            {
                float[] weights = GetLayerWeights(controller);
                if (weights != null)
                {
                    log.AppendLine($"    Layer Weights: [{string.Join(", ", System.Array.ConvertAll(weights, w => w.ToString("F2")))}]");
                }
            }
            
            if (logParameters)
            {
                LogAnimatorParameters(controller, log);
            }
        }
        
        log.AppendLine("═══════════════════════════════════════════════════════════");
        Debug.Log(log.ToString());
    }
    
    private void LogAnimatorParameters(IndividualLayeredHandController controller, StringBuilder log)
    {
        if (controller == null || controller.handAnimator == null) return;
        
        log.AppendLine("    Parameters:");
        foreach (AnimatorControllerParameter param in controller.handAnimator.parameters)
        {
            string value = "";
            switch (param.type)
            {
                case AnimatorControllerParameterType.Float:
                    value = controller.handAnimator.GetFloat(param.name).ToString("F2");
                    break;
                case AnimatorControllerParameterType.Int:
                    value = controller.handAnimator.GetInteger(param.name).ToString();
                    break;
                case AnimatorControllerParameterType.Bool:
                    value = controller.handAnimator.GetBool(param.name).ToString();
                    break;
                case AnimatorControllerParameterType.Trigger:
                    value = "(trigger)";
                    break;
            }
            log.AppendLine($"      {param.name} ({param.type}): {value}");
        }
    }
    
    private void LogFullDiagnostic()
    {
        StringBuilder log = new StringBuilder();
        log.AppendLine("╔═══════════════════════════════════════════════════════════════╗");
        log.AppendLine("║         FULL SHITTY ANIMATION FUCKING SHIT SYSTEM DIAGNOSTIC REPORT               ║");
        log.AppendLine("╚═══════════════════════════════════════════════════════════════╝");
        log.AppendLine($"Frame: {diagnosticFrameCounter} | Time: {Time.time:F2}s");
        log.AppendLine($"Active Hands: {allHandControllers.Length} (tracking ONLY currently active hands)");
        log.AppendLine("");
        
        // CRITICAL: Show PlayerAnimationStateManager state
        if (stateManager != null)
        {
            log.AppendLine("┌─ PLAYERANIMATIONSTATEMANAGER (Source of Truth) ─────────────");
            log.AppendLine(stateManager.GetStateInfo());
            log.AppendLine("└──────────────────────────────────────────────────────────────");
            log.AppendLine("");
        }
        else
        {
            log.AppendLine("⚠️ WARNING: PlayerAnimationStateManager NOT FOUND!");
            log.AppendLine("");
        }
        
        // Now allHandControllers only contains the 2 ACTIVE hands!
        foreach (var controller in allHandControllers)
        {
            if (controller == null) continue;
            
            log.AppendLine($"┌─ {controller.name} ─────────────────────────────────────");
            log.AppendLine($"│ Hand Type: {(controller.IsLeftHand ? "LEFT" : "RIGHT")}");
            log.AppendLine($"│ Animator: {(controller.handAnimator != null ? "✓ OK" : "✗ NULL")}");
            
            if (controller.handAnimator != null)
            {
                bool hasController = controller.handAnimator.runtimeAnimatorController != null;
                log.AppendLine($"│ Has Controller: {(hasController ? "✓ YES" : "✗ NO - MISSING ANIMATOR CONTROLLER!")}");
                
                if (hasController)
                {
                    log.AppendLine($"│ Layer Count: {controller.handAnimator.layerCount}");
                    log.AppendLine($"│ Parameter Count: {controller.handAnimator.parameterCount}");
                    log.AppendLine($"│ Animation Speed: {controller.handAnimator.speed}");
                }
                log.AppendLine($"│");
                log.AppendLine($"│ STATES:");
                log.AppendLine($"│   Movement: {controller.CurrentMovementState}");
                log.AppendLine($"│   Shooting: {controller.CurrentShootingState}");
                log.AppendLine($"│   Emote: {controller.CurrentEmoteState}");
                log.AppendLine($"│   Ability: {controller.CurrentAbilityState}");
                log.AppendLine($"│");
                
                // Check for required parameters
                log.AppendLine($"│ REQUIRED PARAMETERS:");
                log.AppendLine($"│   movementState: {(CheckParameterExists(controller, "movementState") ? "✓" : "✗ MISSING")}");
                log.AppendLine($"│   ShotgunT: {(CheckParameterExists(controller, "ShotgunT") ? "✓" : "✗ MISSING")}");
                log.AppendLine($"│   IsBeamAc: {(CheckParameterExists(controller, "IsBeamAc") ? "✓" : "✗ MISSING")}");
                log.AppendLine($"│   PlayEmote: {(CheckParameterExists(controller, "PlayEmote") ? "✓" : "✗ MISSING")}");
                log.AppendLine($"│   emoteIndex: {(CheckParameterExists(controller, "emoteIndex") ? "✓" : "✗ MISSING")}");
                log.AppendLine($"│   ApplyPlate: {(CheckParameterExists(controller, "ApplyPlate") ? "✓" : "✗ MISSING")}");
                log.AppendLine($"│   abilityType: {(CheckParameterExists(controller, "abilityType") ? "✓" : "✗ MISSING")}");
                log.AppendLine($"│");
                
                // Current clip info + SPEED MULTIPLIER CHECK
                string clipName = GetCurrentClipName(controller);
                log.AppendLine($"│ CURRENT CLIP: {clipName}");
                
                // CRITICAL: Check state speed multiplier AND clip length
                if (hasController)
                {
                    try
                    {
                        AnimatorStateInfo stateInfo = controller.handAnimator.GetCurrentAnimatorStateInfo(0);
                        float speedMult = stateInfo.speedMultiplier;
                        string speedStatus = Mathf.Abs(speedMult - 1.0f) < 0.01f ? "✓" : "🚨 WRONG!";
                        log.AppendLine($"│ STATE SPEED MULTIPLIER: {speedMult:F2} {speedStatus}");
                        
                        // Check actual clip length and frame rate
                        AnimatorClipInfo[] clipInfos = controller.handAnimator.GetCurrentAnimatorClipInfo(0);
                        if (clipInfos.Length > 0)
                        {
                            AnimationClip clip = clipInfos[0].clip;
                            log.AppendLine($"│ CLIP LENGTH: {clip.length:F2}s | FRAME RATE: {clip.frameRate}fps");
                        }
                    }
                    catch { }
                }
                log.AppendLine($"│");
                
                // Layer weights
                float[] weights = GetLayerWeights(controller);
                if (weights != null)
                {
                    log.AppendLine($"│ LAYER WEIGHTS:");
                    for (int i = 0; i < weights.Length; i++)
                    {
                        log.AppendLine($"│   Layer {i} ({GetLayerName(i)}): {weights[i]:F2}");
                    }
                }
            }
            
            log.AppendLine($"└───────────────────────────────────────────────────────");
            log.AppendLine("");
        }
        
        log.AppendLine("╚═══════════════════════════════════════════════════════════════╝");
        Debug.Log(log.ToString());
    }
    
    // === PUBLIC DIAGNOSTIC METHODS ===
    
    /// <summary>
    /// Call this when you request an animation to log what SHOULD happen
    /// </summary>
    public void LogAnimationRequest(string handName, string animationType, string animationName)
    {
        if (!enableDiagnostics) return;
        
        Debug.Log($"[AnimDiag] ►►► ANIMATION REQUESTED ◄◄◄");
        Debug.Log($"  Hand: {handName}");
        Debug.Log($"  Type: {animationType}");
        Debug.Log($"  Animation: {animationName}");
        Debug.Log($"  Frame: {diagnosticFrameCounter} | Time: {Time.time:F2}s");
    }
    
    /// <summary>
    /// Force log current state of all hands
    /// </summary>
    public void ForceLogCurrentState()
    {
        LogAllHandStates("FORCED DIAGNOSTIC");
    }
    
    /// <summary>
    /// Force full diagnostic report
    /// </summary>
    public void ForceFullDiagnostic()
    {
        LogFullDiagnostic();
    }
}
