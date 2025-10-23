using UnityEngine;

/// <summary>
/// ScriptableObject that defines all animation data and behavior for a hand (left or right)
/// This allows for data-driven configuration without changing code
/// </summary>
[CreateAssetMenu(fileName = "HandAnimationData", menuName = "Hand System/Hand Animation Data")]
public class HandAnimationData : ScriptableObject
{
    [Header("Basic Movement Animations")]
    [Tooltip("Idle animation clip")]
    public AnimationClip idleClip;
    [Tooltip("Walk animation clip")]
    public AnimationClip walkClip;
    [Tooltip("Sprint animation clip")]
    public AnimationClip sprintClip;
    [Tooltip("Jump animation clip")]
    public AnimationClip jumpClip;
    [Tooltip("Landing animation clip")]
    public AnimationClip landClip;
    
    [Header("Combat Animations")]
    [Tooltip("Shotgun firing animation clip")]
    public AnimationClip shotgunClip;
    [Tooltip("Beam/Stream loop animation clip")]
    public AnimationClip beamLoopClip;
    
    [Header("Tactical Animations")]
    [Tooltip("Slide animation clip")]
    public AnimationClip slideClip;
    [Tooltip("Dive animation clip")]
    public AnimationClip diveClip;
    [Tooltip("Take off animation clip")]
    public AnimationClip takeOffClip;
    
    [Header("Flight Animations")]
    [Tooltip("Fly forward animation clip")]
    public AnimationClip flyForwardClip;
    [Tooltip("Fly up animation clip")]
    public AnimationClip flyUpClip;
    [Tooltip("Fly down animation clip")]
    public AnimationClip flyDownClip;
    [Tooltip("Fly strafe left animation clip")]
    public AnimationClip flyStrafeLeftClip;
    [Tooltip("Fly strafe right animation clip")]
    public AnimationClip flyStrafeRightClip;
    [Tooltip("Fly boost animation clip")]
    public AnimationClip flyBoostClip;
    
    [Header("Ability Animations")]
    [Tooltip("Armor plate application animation (right hand only)")]
    public AnimationClip armorPlateClip;
    
    [Header("Emote Animations")]
    [Tooltip("Emote 1 animation clip")]
    public AnimationClip emote1Clip;
    [Tooltip("Emote 2 animation clip")]
    public AnimationClip emote2Clip;
    [Tooltip("Emote 3 animation clip")]
    public AnimationClip emote3Clip;
    [Tooltip("Emote 4 animation clip")]
    public AnimationClip emote4Clip;
    
    [Header("Animation Settings")]
    [Range(0.1f, 2.0f)]
    [Tooltip("Global animation speed multiplier")]
    public float animationSpeed = 1.0f;
    
    [Range(0.0f, 0.5f)]
    [Tooltip("Default crossfade duration for smooth transitions")]
    public float defaultCrossFadeDuration = 0.2f;
    
    [Header("Priority Configuration")]
    [Tooltip("Priority values for different animation types (higher = more important)")]
    public HandAnimationPriorities priorities = new HandAnimationPriorities();
    
    [Header("Blend Time Configuration")]
    [Tooltip("Blend times for different transition types")]
    public HandAnimationBlendTimes blendTimes = new HandAnimationBlendTimes();
    
    [Header("Combat Timing")]
    [Tooltip("How long shooting animations lock the hand")]
    public float combatLockDuration = 1.5f;
    
    /// <summary>
    /// Get animation clip for a specific state
    /// </summary>
    public AnimationClip GetClipForState(HandAnimationState state)
    {
        switch (state)
        {
            case HandAnimationState.Idle: return idleClip;
            case HandAnimationState.Walk: return walkClip;
            case HandAnimationState.Sprint: return sprintClip;
            case HandAnimationState.Jump: return jumpClip;
            case HandAnimationState.Land: return landClip;
            case HandAnimationState.Slide: return slideClip;
            case HandAnimationState.Dive: return diveClip;
            case HandAnimationState.Shotgun: return shotgunClip;
            case HandAnimationState.Beam: return beamLoopClip;
            case HandAnimationState.FlyForward: return flyForwardClip;
            case HandAnimationState.FlyUp: return flyUpClip;
            case HandAnimationState.FlyDown: return flyDownClip;
            case HandAnimationState.FlyStrafeLeft: return flyStrafeLeftClip;
            case HandAnimationState.FlyStrafeRight: return flyStrafeRightClip;
            case HandAnimationState.FlyBoost: return flyBoostClip;
            case HandAnimationState.TakeOff: return takeOffClip;
            case HandAnimationState.ArmorPlate: return armorPlateClip;
            case HandAnimationState.Emote: return GetEmoteClip(1); // Default to emote 1, will be overridden
            default: return idleClip;
        }
    }
    
    /// <summary>
    /// Get emote clip by number
    /// </summary>
    public AnimationClip GetEmoteClip(int emoteNumber)
    {
        switch (emoteNumber)
        {
            case 1: return emote1Clip;
            case 2: return emote2Clip;
            case 3: return emote3Clip;
            case 4: return emote4Clip;
            default: return emote1Clip;
        }
    }
}

/// <summary>
/// Animation state enum - defines all possible hand animation states
/// </summary>
public enum HandAnimationState
{
    Idle = 0,
    Walk = 1,
    Sprint = 2,
    Jump = 3,
    Land = 4,
    Slide = 5,
    Dive = 6,
    Shotgun = 7,
    Beam = 8,
    FlyForward = 9,
    FlyUp = 10,
    FlyDown = 11,
    FlyStrafeLeft = 12,
    FlyStrafeRight = 13,
    FlyBoost = 14,
    TakeOff = 15,
    ArmorPlate = 16,
    Emote = 17
}

/// <summary>
/// Priority configuration for different animation types
/// Higher values = higher priority (can interrupt lower priority)
/// </summary>
[System.Serializable]
public class HandAnimationPriorities
{
    [Header("Priority Hierarchy (Higher = More Important)")]
    public int idle = 0;
    public int flight = 3;
    public int tactical = 4;
    public int walk = 5;
    public int oneShot = 6;
    public int shooting = 7;        // Shooting overrides everything except armor plates
    public int sprint = 8;
    public int armorPlate = 10;     // Armor plates have highest priority
    public int emote = 9;
    
    /// <summary>
    /// Get priority for a specific state
    /// </summary>
    public int GetPriority(HandAnimationState state)
    {
        switch (state)
        {
            case HandAnimationState.Idle:
                return idle;
            
            case HandAnimationState.FlyForward:
            case HandAnimationState.FlyUp:
            case HandAnimationState.FlyDown:
            case HandAnimationState.FlyStrafeLeft:
            case HandAnimationState.FlyStrafeRight:
            case HandAnimationState.FlyBoost:
                return flight;
            
            case HandAnimationState.Dive:
            case HandAnimationState.Slide:
                return tactical;
            
            case HandAnimationState.Walk:
                return walk;
            
            case HandAnimationState.Jump:
            case HandAnimationState.Land:
            case HandAnimationState.TakeOff:
                return oneShot;
            
            case HandAnimationState.Shotgun:
            case HandAnimationState.Beam:
                return shooting;  // Shooting has high priority
            
            case HandAnimationState.Sprint:
                return sprint;
            
            case HandAnimationState.ArmorPlate:
                return armorPlate;  // Highest priority - cannot be interrupted
            
            case HandAnimationState.Emote:
                return emote;
            
            default:
                return idle;
        }
    }
}

/// <summary>
/// Blend time configuration for different transition types
/// </summary>
[System.Serializable]
public class HandAnimationBlendTimes
{
    [Header("Blend Times (seconds)")]
    public float instant = 0.0f;       // Combat actions
    public float veryFast = 0.05f;     // Critical transitions
    public float fast = 0.1f;          // Combat-to-movement
    public float normal = 0.2f;        // Default
    public float smooth = 0.3f;        // Movement-to-movement
    public float slow = 0.4f;          // Emotes, cinematic
    
    /// <summary>
    /// Get appropriate blend time for transitioning between states
    /// </summary>
    public float GetBlendTime(HandAnimationState fromState, HandAnimationState toState)
    {
        // Shooting is always instant for snappy feel
        if (toState == HandAnimationState.Shotgun)
            return instant;
        
        // Emotes are smooth for polish
        if (toState == HandAnimationState.Emote)
            return slow;
        
        // Tactical actions are very fast
        if (toState == HandAnimationState.Dive || toState == HandAnimationState.Slide)
            return veryFast;
        
        // Critical transitions are very fast
        if (toState == HandAnimationState.TakeOff || toState == HandAnimationState.Jump)
            return veryFast;
        
        // Movement-to-movement transitions are smooth
        if (IsMovementState(toState) && IsMovementState(fromState))
            return smooth;
        
        // Combat to movement is smooth
        if (IsMovementState(toState) && IsCombatState(fromState))
            return smooth;
        
        // Movement to combat is fast
        if (IsCombatState(toState) && IsMovementState(fromState))
            return fast;
        
        // Default
        return normal;
    }
    
    private bool IsMovementState(HandAnimationState state)
    {
        return state == HandAnimationState.Walk || 
               state == HandAnimationState.Sprint || 
               state == HandAnimationState.Idle;
    }
    
    private bool IsCombatState(HandAnimationState state)
    {
        return state == HandAnimationState.Shotgun || 
               state == HandAnimationState.Beam;
    }
}
