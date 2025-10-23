using UnityEngine;
using System.Collections;

/// <summary>
/// Simple companion hand animator - only handles idle, beam, and shotgun animations
/// </summary>
public class CompanionHandAnimator : MonoBehaviour
{
    [Header("Hand Animators - ASSIGN MANUALLY")]
    [Tooltip("Left hand animator - drag from hierarchy")]
    public Animator leftHandAnimator;
    
    [Tooltip("Right hand animator - drag from hierarchy")]
    public Animator rightHandAnimator;
    
    [Header("Animation Clips - ASSIGN MANUALLY")]
    [Tooltip("Left hand idle animation clip")]
    public AnimationClip leftIdleClip;
    
    [Tooltip("Right hand idle animation clip")]
    public AnimationClip rightIdleClip;
    
    [Tooltip("Left hand beam animation clip")]
    public AnimationClip leftBeamClip;
    
    [Tooltip("Right hand beam animation clip")]
    public AnimationClip rightBeamClip;
    
    [Tooltip("Left hand shotgun animation clip")]
    public AnimationClip leftShotgunClip;
    
    [Tooltip("Right hand shotgun animation clip")]
    public AnimationClip rightShotgunClip;
    
    [Header("Animation Settings")]
    [Tooltip("Cross fade duration for smooth transitions")]
    [Range(0.1f, 1f)] public float crossFadeDuration = 0.2f;
    
    [Tooltip("How long shotgun animation plays before returning to idle")]
    [Range(0.2f, 2f)] public float shotgunAnimationDuration = 0.5f;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    // Animation state tracking
    private bool _leftBeaming = false;
    private bool _rightBeaming = false;
    private Coroutine _leftShotgunCoroutine;
    private Coroutine _rightShotgunCoroutine;
    
    // No animation hashes needed - using direct clip references
    
    void Start()
    {
        // Validate setup
        ValidateSetup();
        
        // Start with idle animations
        PlayIdleBoth();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[CompanionHandAnimator] Initialized - Left: {(leftHandAnimator ? leftHandAnimator.name : "NULL")}, Right: {(rightHandAnimator ? rightHandAnimator.name : "NULL")}");
        }
    }
    
    private void ValidateSetup()
    {
        bool hasErrors = false;
        
        if (leftHandAnimator == null)
        {
            Debug.LogError("[CompanionHandAnimator] ❌ Left Hand Animator not assigned! Please drag it from the hierarchy.", this);
            hasErrors = true;
        }
        
        if (rightHandAnimator == null)
        {
            Debug.LogError("[CompanionHandAnimator] ❌ Right Hand Animator not assigned! Please drag it from the hierarchy.", this);
            hasErrors = true;
        }
        
        if (leftIdleClip == null)
        {
            Debug.LogWarning("[CompanionHandAnimator] ⚠️ Left Idle Clip not assigned!", this);
        }
        
        if (rightIdleClip == null)
        {
            Debug.LogWarning("[CompanionHandAnimator] ⚠️ Right Idle Clip not assigned!", this);
        }
        
        if (leftBeamClip == null)
        {
            Debug.LogWarning("[CompanionHandAnimator] ⚠️ Left Beam Clip not assigned!", this);
        }
        
        if (rightBeamClip == null)
        {
            Debug.LogWarning("[CompanionHandAnimator] ⚠️ Right Beam Clip not assigned!", this);
        }
        
        if (leftShotgunClip == null)
        {
            Debug.LogWarning("[CompanionHandAnimator] ⚠️ Left Shotgun Clip not assigned!", this);
        }
        
        if (rightShotgunClip == null)
        {
            Debug.LogWarning("[CompanionHandAnimator] ⚠️ Right Shotgun Clip not assigned!", this);
        }
        
        if (!hasErrors)
        {
            Debug.Log("[CompanionHandAnimator] ✅ Setup validation complete!");
        }
    }
    
    // No auto-find needed - user assigns everything manually
    
    private void PlayAnimationClip(Animator animator, AnimationClip clip, string debugName = "")
    {
        // CRITICAL: Check if animator is enabled AND active in hierarchy
        if (animator == null || !animator.enabled || !animator.gameObject.activeInHierarchy) return;
        
        if (clip == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[CompanionHandAnimator] ⚠️ No clip assigned for {debugName}");
            return;
        }
        
        if (crossFadeDuration > 0f)
        {
            animator.CrossFade(clip.name, crossFadeDuration, 0, 0f);
        }
        else
        {
            animator.Play(clip.name, 0, 0f);
        }
        
        if (enableDebugLogs && !string.IsNullOrEmpty(debugName))
        {
            Debug.Log($"[CompanionHandAnimator] 🎭 Playing {debugName} ({clip.name}) on {animator.name}");
        }
    }
    
    #region Public API - Called by CompanionCombat
    
    /// <summary>
    /// Play idle animation on left hand
    /// </summary>
    public void PlayIdleLeft()
    {
        if (!_leftBeaming) // Don't interrupt beam with idle
        {
            PlayAnimationClip(leftHandAnimator, leftIdleClip, "Left Idle");
        }
    }
    
    /// <summary>
    /// Play idle animation on right hand
    /// </summary>
    public void PlayIdleRight()
    {
        if (!_rightBeaming) // Don't interrupt beam with idle
        {
            PlayAnimationClip(rightHandAnimator, rightIdleClip, "Right Idle");
        }
    }
    
    /// <summary>
    /// Play idle animation on both hands
    /// </summary>
    public void PlayIdleBoth()
    {
        PlayIdleLeft();
        PlayIdleRight();
    }
    
    /// <summary>
    /// Start beam animation on left hand
    /// </summary>
    public void StartBeamLeft()
    {
        _leftBeaming = true;
        PlayAnimationClip(leftHandAnimator, leftBeamClip, "Left Beam Start");
    }
    
    /// <summary>
    /// Start beam animation on right hand
    /// </summary>
    public void StartBeamRight()
    {
        _rightBeaming = true;
        PlayAnimationClip(rightHandAnimator, rightBeamClip, "Right Beam Start");
    }
    
    /// <summary>
    /// Start beam animation on both hands
    /// </summary>
    public void StartBeamBoth()
    {
        StartBeamLeft();
        StartBeamRight();
    }
    
    /// <summary>
    /// Stop beam animation on left hand and return to idle
    /// </summary>
    public void StopBeamLeft()
    {
        _leftBeaming = false;
        PlayAnimationClip(leftHandAnimator, leftIdleClip, "Left Beam Stop");
    }
    
    /// <summary>
    /// Stop beam animation on right hand and return to idle
    /// </summary>
    public void StopBeamRight()
    {
        _rightBeaming = false;
        PlayAnimationClip(rightHandAnimator, rightIdleClip, "Right Beam Stop");
    }
    
    /// <summary>
    /// Stop beam animation on both hands and return to idle
    /// </summary>
    public void StopBeamBoth()
    {
        StopBeamLeft();
        StopBeamRight();
    }
    
    /// <summary>
    /// Play shotgun animation on left hand (returns to idle automatically)
    /// </summary>
    public void PlayShotgunLeft()
    {
        // Stop any existing shotgun coroutine
        if (_leftShotgunCoroutine != null)
        {
            StopCoroutine(_leftShotgunCoroutine);
        }
        
        PlayAnimationClip(leftHandAnimator, leftShotgunClip, "Left Shotgun");
        _leftShotgunCoroutine = StartCoroutine(ReturnToIdleAfterShotgun(true));
    }
    
    /// <summary>
    /// Play shotgun animation on right hand (returns to idle automatically)
    /// </summary>
    public void PlayShotgunRight()
    {
        // Stop any existing shotgun coroutine
        if (_rightShotgunCoroutine != null)
        {
            StopCoroutine(_rightShotgunCoroutine);
        }
        
        PlayAnimationClip(rightHandAnimator, rightShotgunClip, "Right Shotgun");
        _rightShotgunCoroutine = StartCoroutine(ReturnToIdleAfterShotgun(false));
    }
    
    /// <summary>
    /// Play shotgun animation on both hands (returns to idle automatically)
    /// </summary>
    public void PlayShotgunBoth()
    {
        PlayShotgunLeft();
        PlayShotgunRight();
    }
    
    #endregion
    
    private IEnumerator ReturnToIdleAfterShotgun(bool isLeftHand)
    {
        yield return new WaitForSeconds(shotgunAnimationDuration);
        
        if (isLeftHand)
        {
            PlayIdleLeft();
            _leftShotgunCoroutine = null;
        }
        else
        {
            PlayIdleRight();
            _rightShotgunCoroutine = null;
        }
    }
    
    #region Testing Methods
    
    [ContextMenu("Validate Setup")]
    public void ValidateSetupManual()
    {
        ValidateSetup();
    }
    
    [ContextMenu("Test All Animations")]
    public void TestAllAnimations()
    {
        Debug.Log("[CompanionHandAnimator] 🧪 Testing all animations...");
        StartCoroutine(TestAnimationSequence());
    }
    
    private IEnumerator TestAnimationSequence()
    {
        Debug.Log("Testing idle animations...");
        PlayIdleBoth();
        yield return new WaitForSeconds(2f);
        
        Debug.Log("Testing shotgun animations...");
        PlayShotgunBoth();
        yield return new WaitForSeconds(2f);
        
        Debug.Log("Testing beam start...");
        StartBeamBoth();
        yield return new WaitForSeconds(3f);
        
        Debug.Log("Testing beam stop...");
        StopBeamBoth();
        yield return new WaitForSeconds(1f);
        
        Debug.Log("✅ Animation test complete!");
    }
    
    #endregion
    
    void OnDisable()
    {
        // Clean up coroutines
        if (_leftShotgunCoroutine != null)
        {
            StopCoroutine(_leftShotgunCoroutine);
            _leftShotgunCoroutine = null;
        }
        
        if (_rightShotgunCoroutine != null)
        {
            StopCoroutine(_rightShotgunCoroutine);
            _rightShotgunCoroutine = null;
        }
    }
}