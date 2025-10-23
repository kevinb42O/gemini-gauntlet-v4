using UnityEngine;

/// <summary>
/// PERFORMANCE OPTIMIZATION: Consolidates all player ground detection raycasts into a single unified system.
/// Reduces 8-10 raycasts per frame down to 1-2 raycasts per frame.
/// 
/// CRITICAL: This is an OPTIONAL optimization. All dependent scripts have fallback behavior if this is missing.
/// </summary>
public class PlayerRaycastManager : MonoBehaviour
{
    [Header("Ground Detection Settings")]
    [SerializeField] private LayerMask groundMask = -1;
    [Tooltip("Distance to check for ground. For 320-unit character, use 80-100. Standard 2m character uses 0.3.")]
    [SerializeField] private float groundCheckDistance = 100f;
    [Tooltip("Radius of sphere cast. Should match CharacterController radius minus skin. For 50-radius character, use 48.")]
    [SerializeField] private float sphereCastRadius = 48f;
    
    [Header("Ground Normal Detection Settings")]
    [SerializeField] private bool useRaycastForGroundNormal = true;
    [Tooltip("Distance for ground normal detection. For 320-unit character, use 160-200. Standard 2m character uses 1.0.")]
    [SerializeField] private float groundNormalRaycastDistance = 180f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // Cached ground detection results (updated once per physics frame)
    private bool _isGrounded;
    private RaycastHit _groundHit;
    private Vector3 _groundNormal = Vector3.up;
    private float _groundDistance;
    private bool _hasValidGroundHit;
    private bool _hasValidGroundNormal;
    
    // Cached components
    private CharacterController _controller;
    private Transform _transform;
    
    // Frame tracking to prevent duplicate updates
    private int _lastGroundCheckFrame = -1;
    private int _lastNormalCheckFrame = -1;
    
    // Temporary ignore grounding (for ramp launches, etc.)
    private float _ignoreGroundingUntil = -999f;
    
    #region Public API
    
    /// <summary>
    /// Is the player currently grounded? Updated once per physics frame.
    /// </summary>
    public bool IsGrounded => _isGrounded;
    
    /// <summary>
    /// The RaycastHit from the ground check. Only valid if HasValidGroundHit is true.
    /// </summary>
    public RaycastHit GroundHit => _groundHit;
    
    /// <summary>
    /// Ground normal vector. Defaults to Vector3.up if no ground detected.
    /// </summary>
    public Vector3 GroundNormal => _groundNormal;
    
    /// <summary>
    /// Distance to ground. Only valid if HasValidGroundHit is true.
    /// </summary>
    public float GroundDistance => _groundDistance;
    
    /// <summary>
    /// True if the ground hit data is valid and fresh.
    /// </summary>
    public bool HasValidGroundHit => _hasValidGroundHit;
    
    /// <summary>
    /// True if the ground normal data is valid and fresh.
    /// </summary>
    public bool HasValidGroundNormal => _hasValidGroundNormal;
    
    /// <summary>
    /// Temporarily ignore grounding until specified time (for ramp launches, etc.)
    /// </summary>
    public void IgnoreGroundingUntil(float time)
    {
        _ignoreGroundingUntil = time;
    }
    
    #endregion
    
    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _transform = transform;
    }
    
    private void FixedUpdate()
    {
        // Perform unified ground check once per physics frame
        PerformGroundCheck();
        PerformGroundNormalCheck();
    }
    
    /// <summary>
    /// Unified ground detection using SphereCast (most reliable method)
    /// </summary>
    private void PerformGroundCheck()
    {
        // Prevent duplicate checks in same frame
        if (_lastGroundCheckFrame == Time.frameCount) return;
        _lastGroundCheckFrame = Time.frameCount;
        
        // Respect temporary ignore window (e.g., after ramp launch)
        if (Time.time < _ignoreGroundingUntil)
        {
            _isGrounded = false;
            _hasValidGroundHit = false;
            if (showDebugInfo)
                Debug.Log("[PlayerRaycastManager] Ignoring grounding check (temporary ignore window)");
            return;
        }
        
        if (_controller == null)
        {
            _isGrounded = false;
            _hasValidGroundHit = false;
            return;
        }
        
        // Calculate sphere cast parameters
        // Use configured radius or fall back to controller radius
        float radius = sphereCastRadius > 0.1f ? sphereCastRadius : Mathf.Max(_controller.radius - 2f, 0.1f);
        Vector3 origin = _transform.position + Vector3.up * (radius + 10f);
        
        // Perform the unified ground check
        _isGrounded = Physics.SphereCast(
            origin, 
            radius, 
            Vector3.down, 
            out _groundHit, 
            groundCheckDistance + 0.1f, 
            groundMask, 
            QueryTriggerInteraction.Ignore
        );
        
        if (_isGrounded)
        {
            _hasValidGroundHit = true;
            _groundDistance = _groundHit.distance;
            _groundNormal = _groundHit.normal; // Cache basic normal from ground hit
            
            if (showDebugInfo)
            {
                Debug.Log($"[PlayerRaycastManager] GROUNDED at distance {_groundDistance:F3} on {_groundHit.collider.gameObject.name}");
            }
        }
        else
        {
            _hasValidGroundHit = false;
            _groundDistance = float.MaxValue;
            _groundNormal = Vector3.up; // Default to up when not grounded
            
            if (showDebugInfo)
            {
                Debug.Log("[PlayerRaycastManager] NOT GROUNDED");
            }
        }
    }
    
    /// <summary>
    /// Unified ground normal detection for slope calculations
    /// </summary>
    private void PerformGroundNormalCheck()
    {
        // Prevent duplicate checks in same frame
        if (_lastNormalCheckFrame == Time.frameCount) return;
        _lastNormalCheckFrame = Time.frameCount;
        
        if (_controller == null || !_isGrounded)
        {
            _hasValidGroundNormal = false;
            _groundNormal = Vector3.up;
            return;
        }
        
        // Calculate raycast parameters (scaled for large characters)
        Vector3 origin = _transform.position + Vector3.up * (_controller.radius + 10f);
        float rayLen = Mathf.Max(groundNormalRaycastDistance, _controller.height * 0.5f + 25f);
        
        // Try raycast first (more accurate for normal calculation)
        if (useRaycastForGroundNormal)
        {
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit rHit, rayLen, groundMask, QueryTriggerInteraction.Ignore))
            {
                if (rHit.collider != null && !rHit.collider.isTrigger)
                {
                    _groundNormal = rHit.normal;
                    _hasValidGroundNormal = true;
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"[PlayerRaycastManager] Ground normal from raycast: {_groundNormal}");
                    }
                    return;
                }
            }
        }
        
        // Fallback: Use normal from ground hit (already cached)
        if (_hasValidGroundHit)
        {
            _groundNormal = _groundHit.normal;
            _hasValidGroundNormal = true;
            
            if (showDebugInfo)
            {
                Debug.Log($"[PlayerRaycastManager] Ground normal from spherecast: {_groundNormal}");
            }
        }
        else
        {
            _hasValidGroundNormal = false;
            _groundNormal = Vector3.up;
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!showDebugInfo || !Application.isPlaying) return;
        
        // Draw ground detection sphere
        if (_controller != null)
        {
            float radius = sphereCastRadius > 0.1f ? sphereCastRadius : Mathf.Max(_controller.radius - 2f, 0.1f);
            Vector3 origin = transform.position + Vector3.up * (radius + 10f);
            
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(origin, radius);
            Gizmos.DrawRay(origin, Vector3.down * (groundCheckDistance + 10f));
            
            // Draw ground hit point
            if (_hasValidGroundHit)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(_groundHit.point, _controller.radius * 0.2f); // Scale sphere to character size
                
                // Draw ground normal (scaled for visibility)
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(_groundHit.point, _groundNormal * (_controller.radius * 2f));
            }
        }
    }
}
