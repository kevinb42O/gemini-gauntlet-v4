using UnityEngine;
using System.Collections;

/// <summary>
/// Controls a single hand (left or right) with its own independent state machine
/// This script should be attached to each individual hand GameObject
/// Each hand operates completely independently with its own state and animations
/// </summary>
public class IndividualHandController : MonoBehaviour
{
    [Header("Hand Configuration")]
    [Tooltip("Animation data for this hand (left or right)")]
    public HandAnimationData animationData;
    
    [Tooltip("The animator component for this hand")]
    public Animator handAnimator;
    
    [Tooltip("Is this a left hand? (false = right hand)")]
    public bool isLeftHand = true;
    
    [Tooltip("Hand level (1-4) - used for identification")]
    public int handLevel = 1;
    
    [Header("Debug")]
    [Tooltip("Enable debug logging for this hand")]
    public bool enableDebugLogs = false;
    
    // === HAND STATE ===
    private HandAnimationState _currentState = HandAnimationState.Idle;
    private HandAnimationState _previousState = HandAnimationState.Idle;
    private int _currentEmoteNumber = 1;
    
    // === LOCK SYSTEM ===
    private bool _isHardLocked = false;     // Cannot be interrupted by anything
    private bool _isSoftLocked = false;     // Can be interrupted by higher priority
    private float _lockDuration = 0f;
    private Coroutine _completionCoroutine = null;
    
    // === BEAM SYSTEM ===
    private bool _beamWasActiveBeforeInterruption = false;
    
    // === EVENTS ===
    public System.Action<HandAnimationState, HandAnimationState> OnStateChanged;
    
    // === PROPERTIES ===
    public HandAnimationState CurrentState => _currentState;
    public bool IsLocked => _isHardLocked;
    public bool IsSoftLocked => _isSoftLocked;
    public bool IsLeftHand => isLeftHand;
    public int HandLevel => handLevel;
    
    void Awake()
    {
        // Auto-find animator if not assigned
        if (handAnimator == null)
            handAnimator = GetComponent<Animator>();
        
        // Validate configuration
        if (animationData == null)
        {
            Debug.LogError($"[IndividualHandController] {GetHandName()}: No animation data assigned!");
        }
        
        if (handAnimator == null)
        {
            Debug.LogError($"[IndividualHandController] {GetHandName()}: No animator found!");
        }
    }
    
    void Start()
    {
        // Apply animation speed
        if (handAnimator != null && animationData != null)
        {
            handAnimator.speed = animationData.animationSpeed;
        }
        
        // Start in idle state
        ForceTransitionToState(HandAnimationState.Idle);
        
        if (enableDebugLogs)
            Debug.Log($"[IndividualHandController] {GetHandName()} initialized in Idle state");
    }
    
    /// <summary>
    /// Request a state transition with priority checking
    /// This is the main method for changing hand animations
    /// </summary>
    public bool RequestStateTransition(HandAnimationState newState, int emoteNumber = 1)
    {
        if (animationData == null) return false;
        
        // Store emote number for emote states
        if (newState == HandAnimationState.Emote)
            _currentEmoteNumber = emoteNumber;
        
        // === HARD LOCK CHECK ===
        if (_isHardLocked)
        {
            // Allow re-triggering the same state (rapid fire)
            if (_currentState == newState && IsOneShotAnimation(newState))
            {
                TransitionToState(newState);
                return true;
            }
            
            if (enableDebugLogs)
                Debug.Log($"[IndividualHandController] {GetHandName()}: HARD LOCKED in {_currentState} - rejecting {newState}");
            return false;
        }
        
        // Get priorities
        int currentPriority = animationData.priorities.GetPriority(_currentState);
        int newPriority = animationData.priorities.GetPriority(newState);
        
        // === REDUNDANT TRANSITION CHECK ===
        if (_currentState == newState && !IsOneShotAnimation(newState) && !IsBriefCombatInterrupt(newState))
        {
            return false;
        }
        
        // === SOFT LOCK CHECK ===
        if (_isSoftLocked)
        {
            // Allow same state re-triggering
            if (_currentState == newState)
            {
                TransitionToState(newState);
                return true;
            }
            
            // Block lower priority
            if (newPriority < currentPriority)
            {
                if (enableDebugLogs)
                    Debug.Log($"[IndividualHandController] {GetHandName()}: SOFT LOCKED in {_currentState} (P{currentPriority}) - rejecting {newState} (P{newPriority})");
                return false;
            }
        }
        
        // === PRIORITY-BASED INTERRUPTION ===
        
        // Higher priority always interrupts
        if (newPriority > currentPriority)
        {
            TransitionToState(newState);
            return true;
        }
        
        // SPECIAL: Shooting can interrupt sprint (active input priority)
        if (_currentState == HandAnimationState.Sprint && IsBriefCombatInterrupt(newState))
        {
            TransitionToState(newState);
            return true;
        }
        
        // Equal priority - allow transitions within same tier
        if (newPriority == currentPriority)
        {
            // Flight animations can always transition between each other
            if (IsFlightAnimation(newState) && IsFlightAnimation(_currentState))
            {
                TransitionToState(newState);
                return true;
            }
            
            // One-shots can re-trigger
            if (IsOneShotAnimation(newState))
            {
                TransitionToState(newState);
                return true;
            }
            
            // Combat can re-trigger
            if (IsBriefCombatInterrupt(newState))
            {
                TransitionToState(newState);
                return true;
            }
            
            // Movement states can transition between each other
            if (IsMovementState(newState) && IsMovementState(_currentState))
            {
                TransitionToState(newState);
                return true;
            }
        }
        
        // Lower priority cannot interrupt higher priority
        if (enableDebugLogs && newPriority < currentPriority)
        {
            Debug.Log($"[IndividualHandController] {GetHandName()}: {newState} (P{newPriority}) cannot interrupt {_currentState} (P{currentPriority})");
        }
        
        return false;
    }
    
    /// <summary>
    /// Actually perform the state transition and play animation
    /// </summary>
    private void TransitionToState(HandAnimationState newState)
    {
        if (animationData == null || handAnimator == null) return;
        
        HandAnimationState previousState = _currentState;
        
        if (enableDebugLogs)
            Debug.Log($"[IndividualHandController] {GetHandName()}: {previousState} → {newState}");
        
        // Store beam interruption state
        if (previousState == HandAnimationState.Beam && newState != HandAnimationState.Beam)
        {
            _beamWasActiveBeforeInterruption = true;
        }
        
        // Update state
        _previousState = previousState;
        _currentState = newState;
        
        // Apply lock mechanism
        _isHardLocked = RequiresHardLock(newState);
        _isSoftLocked = RequiresSoftLock(newState);
        
        // Cancel existing completion coroutine
        if (_completionCoroutine != null)
        {
            StopCoroutine(_completionCoroutine);
            _completionCoroutine = null;
        }
        
        // Get animation clip
        AnimationClip clip = GetClipForCurrentState();
        
        if (clip != null)
        {
            // Get blend time
            float blendTime = animationData.blendTimes.GetBlendTime(previousState, newState);
            
            // Play animation
            if (blendTime <= 0.001f)
            {
                handAnimator.Play(clip.name, 0, 0f);
            }
            else
            {
                handAnimator.CrossFade(clip.name, blendTime, 0, 0f);
            }
            
            // Schedule completion for one-shot animations
            if (IsOneShotAnimation(newState))
            {
                _lockDuration = clip.length;
                _completionCoroutine = StartCoroutine(OneShotAnimationComplete(clip.length));
            }
            // Schedule completion for brief combat interrupts
            else if (IsBriefCombatInterrupt(newState))
            {
                _lockDuration = animationData.combatLockDuration;
                _completionCoroutine = StartCoroutine(BriefCombatComplete(animationData.combatLockDuration));
            }
            else
            {
                _lockDuration = 0f;
            }
            
            if (enableDebugLogs)
                Debug.Log($"[IndividualHandController] {GetHandName()}: Playing {clip.name} with blend time {blendTime}s");
        }
        else
        {
            // Handle missing clip
            if (IsOneShotAnimation(newState))
            {
                if (enableDebugLogs)
                    Debug.LogError($"[IndividualHandController] {GetHandName()}: Missing clip for one-shot {newState}! Force unlocking.");
                
                // Emergency unlock
                _isHardLocked = false;
                _isSoftLocked = false;
                _lockDuration = 0f;
                _currentState = HandAnimationState.Idle;
            }
        }
        
        // Notify listeners
        OnStateChanged?.Invoke(previousState, newState);
    }
    
    /// <summary>
    /// Force transition to a state, bypassing all priority checks
    /// </summary>
    public void ForceTransitionToState(HandAnimationState newState, int emoteNumber = 1)
    {
        if (newState == HandAnimationState.Emote)
            _currentEmoteNumber = emoteNumber;
        
        // Clear all locks
        _isHardLocked = false;
        _isSoftLocked = false;
        _lockDuration = 0f;
        _beamWasActiveBeforeInterruption = false;
        
        if (_completionCoroutine != null)
        {
            StopCoroutine(_completionCoroutine);
            _completionCoroutine = null;
        }
        
        TransitionToState(newState);
        
        if (enableDebugLogs)
            Debug.Log($"[IndividualHandController] {GetHandName()}: FORCED to {newState}");
    }
    
    /// <summary>
    /// Stop beam animation immediately and unlock
    /// </summary>
    public void StopBeamImmediate()
    {
        if (_currentState == HandAnimationState.Beam)
        {
            if (enableDebugLogs)
                Debug.Log($"[IndividualHandController] {GetHandName()}: Beam stopped - IMMEDIATE UNLOCK");
            
            // Cancel completion coroutine
            if (_completionCoroutine != null)
            {
                StopCoroutine(_completionCoroutine);
                _completionCoroutine = null;
            }
            
            // Clear all locks and reset state
            _isHardLocked = false;
            _isSoftLocked = false;
            _lockDuration = 0f;
            _beamWasActiveBeforeInterruption = false;
            _currentState = HandAnimationState.Idle;
        }
    }
    
    /// <summary>
    /// Get animation clip for current state
    /// </summary>
    private AnimationClip GetClipForCurrentState()
    {
        if (animationData == null) return null;
        
        if (_currentState == HandAnimationState.Emote)
        {
            return animationData.GetEmoteClip(_currentEmoteNumber);
        }
        
        return animationData.GetClipForState(_currentState);
    }
    
    /// <summary>
    /// One-shot animation completion handler
    /// </summary>
    private IEnumerator OneShotAnimationComplete(float duration)
    {
        yield return new WaitForSeconds(duration);
        _completionCoroutine = null;
        
        if (enableDebugLogs)
            Debug.Log($"[IndividualHandController] {GetHandName()}: One-shot {_currentState} complete");
        
        // Handle emote completion
        if (_currentState == HandAnimationState.Emote)
        {
            // Resume beam if it was active before emote
            if (_beamWasActiveBeforeInterruption)
            {
                RequestStateTransition(HandAnimationState.Beam);
                _beamWasActiveBeforeInterruption = false;
                yield break;
            }
        }
        
        // Unlock
        _isHardLocked = false;
        _isSoftLocked = false;
        _lockDuration = 0f;
    }
    
    /// <summary>
    /// Brief combat animation completion handler
    /// </summary>
    private IEnumerator BriefCombatComplete(float duration)
    {
        yield return new WaitForSeconds(duration);
        _completionCoroutine = null;
        
        // Unlock
        _isHardLocked = false;
        _isSoftLocked = false;
        _lockDuration = 0f;
        
        if (enableDebugLogs)
            Debug.Log($"[IndividualHandController] {GetHandName()}: Combat complete - unlocked");
    }
    
    // === HELPER METHODS ===
    
    private bool IsOneShotAnimation(HandAnimationState state)
    {
        return state == HandAnimationState.Jump ||
               state == HandAnimationState.Land ||
               state == HandAnimationState.TakeOff ||
               state == HandAnimationState.Dive ||
               state == HandAnimationState.ArmorPlate ||
               state == HandAnimationState.Emote;
    }
    
    private bool IsBriefCombatInterrupt(HandAnimationState state)
    {
        return state == HandAnimationState.Shotgun ||
               state == HandAnimationState.Beam;
    }
    
    private bool RequiresHardLock(HandAnimationState state)
    {
        return state == HandAnimationState.ArmorPlate ||
               state == HandAnimationState.Emote;
    }
    
    private bool RequiresSoftLock(HandAnimationState state)
    {
        return state == HandAnimationState.Dive ||
               state == HandAnimationState.Slide;
    }
    
    private bool IsFlightAnimation(HandAnimationState state)
    {
        return state == HandAnimationState.FlyForward ||
               state == HandAnimationState.FlyUp ||
               state == HandAnimationState.FlyDown ||
               state == HandAnimationState.FlyStrafeLeft ||
               state == HandAnimationState.FlyStrafeRight ||
               state == HandAnimationState.FlyBoost;
    }
    
    private bool IsMovementState(HandAnimationState state)
    {
        return state == HandAnimationState.Idle ||
               state == HandAnimationState.Walk ||
               state == HandAnimationState.Sprint;
    }
    
    private string GetHandName()
    {
        return $"{(isLeftHand ? "Left" : "Right")} Hand L{handLevel}";
    }
    
    /// <summary>
    /// Nuclear reset - clear all state and force to idle
    /// </summary>
    public void NuclearReset()
    {
        if (enableDebugLogs)
            Debug.Log($"[IndividualHandController] {GetHandName()}: NUCLEAR RESET");
        
        // Stop all coroutines
        if (_completionCoroutine != null)
        {
            StopCoroutine(_completionCoroutine);
            _completionCoroutine = null;
        }
        
        // Clear all state
        _isHardLocked = false;
        _isSoftLocked = false;
        _lockDuration = 0f;
        _beamWasActiveBeforeInterruption = false;
        _currentState = HandAnimationState.Idle;
        _previousState = HandAnimationState.Idle;
        
        // Force play idle animation
        if (handAnimator != null && animationData != null)
        {
            AnimationClip idleClip = animationData.GetClipForState(HandAnimationState.Idle);
            if (idleClip != null)
            {
                handAnimator.Play(idleClip.name, 0, 0f);
            }
        }
    }
}
