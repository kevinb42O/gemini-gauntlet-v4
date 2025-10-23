using UnityEngine;

/// <summary>
/// Makes legacy VFX continuously track the camera center for stream effects
/// Attached to legacy VFX instances to provide continuous direction updates
/// </summary>
public class LegacyVFXTracker : MonoBehaviour
{
    [Header("Tracking Settings")]
    public bool isStreamEffect = true; // Stream effects track continuously, shotgun effects don't
    public float trackingSpeed = 100f; // Base particle speed
    
    private Camera _playerCamera;
    private ParticleSystem[] _particleSystems;
    private Rigidbody _playerRigidbody;
    private HandFiringMechanics _handFiring;
    
    void Start()
    {
        // Find player camera
        _playerCamera = Camera.main;
        if (_playerCamera == null)
        {
            _playerCamera = FindObjectOfType<Camera>();
        }
        
        // Get all particle systems in this effect
        _particleSystems = GetComponentsInChildren<ParticleSystem>();
        
        // Find player rigidbody for velocity inheritance
        FindPlayerRigidbody();
        
        Debug.Log($"[LEGACY VFX TRACKER] Initialized on {name} - Stream: {isStreamEffect}, Particles: {_particleSystems.Length}");
    }
    
    void FindPlayerRigidbody()
    {
        // Try to find HandFiringMechanics first
        Transform current = transform.parent;
        while (current != null && _handFiring == null)
        {
            _handFiring = current.GetComponent<HandFiringMechanics>();
            current = current.parent;
        }
        
        // Find player rigidbody
        current = transform.parent;
        while (current != null && _playerRigidbody == null)
        {
            _playerRigidbody = current.GetComponent<Rigidbody>();
            current = current.parent;
        }
        
        if (_playerRigidbody == null)
        {
            _playerRigidbody = FindObjectOfType<Rigidbody>();
        }
    }
    
    void Update()
    {
        // Only stream effects track continuously
        if (!isStreamEffect || _playerCamera == null) return;
        
        // Update transform rotation to face camera direction
        Vector3 fireDirection = _playerCamera.transform.forward;
        transform.rotation = Quaternion.LookRotation(fireDirection);
        
        // Update particle velocities continuously
        UpdateParticleVelocities(fireDirection);
    }
    
    void UpdateParticleVelocities(Vector3 fireDirection)
    {
        if (_particleSystems == null) return;
        
        // Get current player velocity
        Vector3 playerVelocity = Vector3.zero;
        if (_playerRigidbody != null)
        {
            playerVelocity = _playerRigidbody.linearVelocity;
        }
        
        // RESPECTFUL PARTICLE TRACKING: Only modify essential properties for stream tracking
        // Don't override HandFiringMechanics configurations
        
        foreach (var ps in _particleSystems)
        {
            if (ps != null && ps.isPlaying)
            {
                // ONLY update transform rotation for directional tracking
                // Let HandFiringMechanics handle all particle system property configuration
                ps.transform.rotation = Quaternion.LookRotation(fireDirection);
                
                // For legacy stream effects, only apply minimal force if NO other systems are managing it
                var forceOverLifetime = ps.forceOverLifetime;
                if (!forceOverLifetime.enabled) // Only if not already configured
                {
                    forceOverLifetime.enabled = true;
                    forceOverLifetime.space = ParticleSystemSimulationSpace.World;
                    forceOverLifetime.x = new ParticleSystem.MinMaxCurve(fireDirection.x * 25f); // Reduced force to avoid conflicts
                    forceOverLifetime.y = new ParticleSystem.MinMaxCurve(fireDirection.y * 25f);
                    forceOverLifetime.z = new ParticleSystem.MinMaxCurve(fireDirection.z * 25f);
                }
                
                // REMOVED: No longer override simulationSpace, startSpeed, velocityOverLifetime
                // These are now properly managed by HandFiringMechanics.ConfigureParticleSystems
                
                if (enableDebugLogging)
                {
                    Debug.Log($"[LEGACY VFX TRACKER] Respectful tracking {ps.name}: Direction {fireDirection}, minimal force applied");
                }
            }
        }
    }
    
    [Header("Debug Settings")]
    public bool enableDebugLogging = false;
    
    /// <summary>
    /// Call this to set up the tracker for a specific effect type
    /// </summary>
    public void Initialize(bool streamEffect, float speed = 100f)
    {
        isStreamEffect = streamEffect;
        trackingSpeed = speed;
        
        Debug.Log($"[LEGACY VFX TRACKER] Configured - Stream: {isStreamEffect}, Speed: {trackingSpeed}");
    }
}
