using UnityEngine;

/// <summary>
/// Automatically detects what platform the player is on and updates the 
/// PlatformRelativeAudioManager to fix warped 3D sounds
/// </summary>
[RequireComponent(typeof(PlatformRelativeAudioManager))]
public class PlayerPlatformAudioBridge : MonoBehaviour 
{
    [Tooltip("Player character's transform")]
    public Transform playerTransform;
    
    [Tooltip("Maximum distance to check for platforms")]
    [Range(1f, 10f)]
    public float platformDetectionRadius = 3f;
    
    [Tooltip("Mask for platform layers")]
    public LayerMask platformLayerMask = -1; // Default to everything
    
    // References
    private PlatformRelativeAudioManager _audioManager;
    private Transform _currentPlatform;
    
    // Detection parameters
    private float _checkInterval = 0.5f;
    private float _nextCheckTime;
    
    private void Awake()
    {
        _audioManager = GetComponent<PlatformRelativeAudioManager>();
        
        // Auto-find player if not assigned
        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }
    }
    
    private void Update()
    {
        if (Time.time < _nextCheckTime || playerTransform == null)
            return;
            
        _nextCheckTime = Time.time + _checkInterval;
        DetectPlayerPlatform();
    }
    
    private void DetectPlayerPlatform()
    {
        Transform detectedPlatform = null;
        
        // Cast a sphere downward from player position
        RaycastHit[] hits = Physics.SphereCastAll(
            playerTransform.position + Vector3.up * 0.5f,
            platformDetectionRadius, 
            Vector3.down, 
            platformDetectionRadius * 2f,
            platformLayerMask
        );
        
        // Find the highest hit platform
        float highestY = float.MinValue;
        foreach (var hit in hits)
        {
            // Skip the player itself
            if (hit.transform == playerTransform || hit.transform.IsChildOf(playerTransform))
                continue;
                
            // Check if this is a platform (static, has collider, etc.)
            if (IsPlatform(hit.transform) && hit.point.y > highestY)
            {
                highestY = hit.point.y;
                detectedPlatform = GetRootPlatform(hit.transform);
            }
        }
        
        // If platform changed, update the audio manager
        if (detectedPlatform != _currentPlatform)
        {
            _currentPlatform = detectedPlatform;
            _audioManager.SetCurrentPlatform(detectedPlatform);
        }
    }
    
    private bool IsPlatform(Transform obj)
    {
        // Simple heuristic: most platforms are static objects with colliders
        // You can customize this for your game's specific platform detection
        Collider collider = obj.GetComponent<Collider>();
        if (collider == null || collider.isTrigger)
            return false;
            
        // Check for platform components instead of tags
        if (obj.GetComponent<CelestialPlatform>() != null)
            return true;
            
        // If it's big enough and has a flat-ish top, probably a platform
        Bounds bounds = collider.bounds;
        return bounds.size.y < bounds.size.x && bounds.size.y < bounds.size.z;
    }
    
    private Transform GetRootPlatform(Transform platform)
    {
        // Find the highest parent that could be considered a "platform system"
        Transform current = platform;
        Transform parent = platform.parent;
        
        while (parent != null)
        {
            // Stop at scene root or rigidbody boundary (which defines independent movement)
            if (parent.GetComponent<Rigidbody>() != null)
                break;
                
            // Special case for platform systems
            if (parent.name.Contains("Planet") || parent.name.Contains("Orbital"))
                return parent;
                
            current = parent;
            parent = parent.parent;
        }
        
        return current;
    }
}
