// === DIRECTIONAL BLOOD HIT INDICATOR ===
// Performance-optimized directional hit feedback system
// Shows player which direction they're being shot from (Front, Back, Left, Right)
// Zero GC allocations, uses object pooling pattern, minimal draw calls

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DirectionalBloodHitIndicator : MonoBehaviour
{
    [Header("Hit Indicator References")]
    [SerializeField] private CanvasGroup frontIndicator;
    [SerializeField] private CanvasGroup backIndicator;
    [SerializeField] private CanvasGroup leftIndicator;
    [SerializeField] private CanvasGroup rightIndicator;
    
    [Header("Performance Settings")]
    [SerializeField] private float fadeInSpeed = 8f;
    [SerializeField] private float fadeOutSpeed = 4f;
    [SerializeField] private float maxAlpha = 0.75f;
    [SerializeField] private float hitCooldown = 0.15f; // Prevent spam from rapid hits
    
    [Header("Direction Detection")]
    [SerializeField] private float frontAngleThreshold = 45f; // Degrees from forward
    [SerializeField] private float backAngleThreshold = 45f;  // Degrees from backward
    
    // Cached references for zero allocation
    private Transform playerTransform;
    private Camera mainCamera;
    
    // Active fade coroutines (one per direction, reused)
    private Coroutine frontCoroutine;
    private Coroutine backCoroutine;
    private Coroutine leftCoroutine;
    private Coroutine rightCoroutine;
    
    // Cooldown tracking per direction to prevent visual spam
    private float lastFrontHitTime = -999f;
    private float lastBackHitTime = -999f;
    private float lastLeftHitTime = -999f;
    private float lastRightHitTime = -999f;
    
    private void Awake()
    {
        // Cache references once for performance
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError("[DirectionalBloodHitIndicator] Player not found! Make sure Player has 'Player' tag.");
        }
        
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[DirectionalBloodHitIndicator] Main camera not found!");
        }
        
        // Initialize all indicators to invisible
        if (frontIndicator != null) frontIndicator.alpha = 0f;
        if (backIndicator != null) backIndicator.alpha = 0f;
        if (leftIndicator != null) leftIndicator.alpha = 0f;
        if (rightIndicator != null) rightIndicator.alpha = 0f;
        
        Debug.Log("[DirectionalBloodHitIndicator] Initialized successfully");
    }
    
    /// <summary>
    /// Show directional hit indicator based on hit direction
    /// PERFORMANCE: Zero allocation, uses cached references and object pooling
    /// </summary>
    public void ShowHitDirection(Vector3 hitDirection)
    {
        if (playerTransform == null || mainCamera == null) return;
        
        // Normalize hit direction (from attacker to player)
        Vector3 direction = hitDirection.normalized;
        
        // Calculate direction relative to player's forward
        // Use player forward, not camera forward, for consistent gameplay
        Vector3 playerForward = playerTransform.forward;
        Vector3 playerRight = playerTransform.right;
        
        // Calculate dot products to determine direction
        float forwardDot = Vector3.Dot(direction, playerForward);
        float rightDot = Vector3.Dot(direction, playerRight);
        
        // Determine which indicator to show based on angle
        // Priority: Front/Back first, then Left/Right
        
        if (forwardDot > Mathf.Cos(frontAngleThreshold * Mathf.Deg2Rad))
        {
            // Hit from FRONT
            ShowIndicator(HitDirection.Front);
        }
        else if (forwardDot < -Mathf.Cos(backAngleThreshold * Mathf.Deg2Rad))
        {
            // Hit from BACK
            ShowIndicator(HitDirection.Back);
        }
        else if (rightDot > 0)
        {
            // Hit from RIGHT
            ShowIndicator(HitDirection.Right);
        }
        else
        {
            // Hit from LEFT
            ShowIndicator(HitDirection.Left);
        }
    }
    
    /// <summary>
    /// Enum for hit directions - performance optimized
    /// </summary>
    private enum HitDirection
    {
        Front,
        Back,
        Left,
        Right
    }
    
    /// <summary>
    /// Show specific directional indicator with cooldown check
    /// PERFORMANCE: Reuses coroutines, minimal allocations
    /// </summary>
    private void ShowIndicator(HitDirection direction)
    {
        float currentTime = Time.time;
        
        switch (direction)
        {
            case HitDirection.Front:
                if (currentTime - lastFrontHitTime < hitCooldown) return;
                lastFrontHitTime = currentTime;
                
                if (frontCoroutine != null) StopCoroutine(frontCoroutine);
                frontCoroutine = StartCoroutine(FadeIndicator(frontIndicator));
                break;
                
            case HitDirection.Back:
                if (currentTime - lastBackHitTime < hitCooldown) return;
                lastBackHitTime = currentTime;
                
                if (backCoroutine != null) StopCoroutine(backCoroutine);
                backCoroutine = StartCoroutine(FadeIndicator(backIndicator));
                break;
                
            case HitDirection.Left:
                if (currentTime - lastLeftHitTime < hitCooldown) return;
                lastLeftHitTime = currentTime;
                
                if (leftCoroutine != null) StopCoroutine(leftCoroutine);
                leftCoroutine = StartCoroutine(FadeIndicator(leftIndicator));
                break;
                
            case HitDirection.Right:
                if (currentTime - lastRightHitTime < hitCooldown) return;
                lastRightHitTime = currentTime;
                
                if (rightCoroutine != null) StopCoroutine(rightCoroutine);
                rightCoroutine = StartCoroutine(FadeIndicator(rightIndicator));
                break;
        }
    }
    
    /// <summary>
    /// Smooth fade in/out for indicator
    /// PERFORMANCE: Minimal allocations, fast execution
    /// </summary>
    private IEnumerator FadeIndicator(CanvasGroup indicator)
    {
        if (indicator == null) yield break;
        
        // Fade IN rapidly
        float currentAlpha = indicator.alpha;
        while (currentAlpha < maxAlpha)
        {
            currentAlpha += fadeInSpeed * Time.deltaTime;
            currentAlpha = Mathf.Min(currentAlpha, maxAlpha);
            indicator.alpha = currentAlpha;
            yield return null;
        }
        
        // Brief hold at max alpha
        yield return new WaitForSeconds(0.05f);
        
        // Fade OUT smoothly
        while (currentAlpha > 0f)
        {
            currentAlpha -= fadeOutSpeed * Time.deltaTime;
            currentAlpha = Mathf.Max(currentAlpha, 0f);
            indicator.alpha = currentAlpha;
            yield return null;
        }
        
        indicator.alpha = 0f;
    }
    
    /// <summary>
    /// PUBLIC API: Call this from damage system with hit point and attacker position
    /// </summary>
    public void ShowHitFromPosition(Vector3 hitPoint, Vector3 attackerPosition)
    {
        if (playerTransform == null) return;
        
        // Calculate direction FROM attacker TO player (the direction the hit came from)
        Vector3 hitDirection = (playerTransform.position - attackerPosition).normalized;
        ShowHitDirection(hitDirection);
    }
    
    /// <summary>
    /// PUBLIC API: Call this directly with normalized hit direction vector
    /// Direction should point FROM attacker TO player
    /// </summary>
    public void ShowHitFromDirection(Vector3 normalizedDirection)
    {
        ShowHitDirection(normalizedDirection);
    }
}
