using UnityEngine;

/// <summary>
/// DEPRECATED - NOT NEEDED
/// User confirmed hands already shoot perfectly with HandlevelSO system
/// This script is not needed since particle effects are already handled properly
/// Keeping for reference only - DO NOT USE
/// </summary>
public class ShootingActionController : MonoBehaviour
{
    [Header("Animation Controller")]
    [Tooltip("Reference to the layered hand animation controller")]
    public LayeredHandAnimationController handAnimationController;
    
    [Header("Particle Systems")]
    [Tooltip("Left hand particle systems for different shooting types")]
    public ParticleSystem[] leftHandParticleSystems;
    
    [Tooltip("Right hand particle systems for different shooting types")]
    public ParticleSystem[] rightHandParticleSystems;
    
    [Header("Shooting Settings")]
    [Tooltip("Shotgun burst duration")]
    [Range(0.1f, 2f)]
    public float shotgunBurstDuration = 0.3f;
    
    [Tooltip("Beam fade in/out speed")]
    [Range(1f, 10f)]
    public float beamFadeSpeed = 5f;
    
    [Header("Debug")]
    [Tooltip("Enable debug logging")]
    public bool enableDebugLogs = false;
    
    // === SHOOTING STATES ===
    private bool _leftBeamActive = false;
    private bool _rightBeamActive = false;
    
    // === COROUTINES ===
    private Coroutine _leftShotgunCoroutine;
    private Coroutine _rightShotgunCoroutine;
    
    // === EVENTS ===
    public System.Action<bool, ShootingType> OnShootingStarted;
    public System.Action<bool, ShootingType> OnShootingStopped;
    
    public enum ShootingType
    {
        Shotgun,
        Beam
    }
    
    void Awake()
    {
        // Auto-find hand animation controller if not assigned
        if (handAnimationController == null)
            handAnimationController = GetComponent<LayeredHandAnimationController>();
        
        if (handAnimationController == null)
            handAnimationController = FindObjectOfType<LayeredHandAnimationController>();
    }
    
    void Start()
    {
        // Initialize all particle systems to stopped state
        StopAllParticleSystems();
        
        if (enableDebugLogs)
            Debug.Log("[ShootingActionController] Initialized - particle effects decoupled from animation states");
    }
    
    // === SHOTGUN SHOOTING ===
    
    public void FireShotgun(bool isPrimaryHand)
    {
        if (enableDebugLogs)
            Debug.Log($"[ShootingActionController] Shotgun fired - {(isPrimaryHand ? "Left" : "Right")} hand");
        
        // 1. Start particle effects IMMEDIATELY
        StartShotgunParticles(isPrimaryHand);
        
        // 2. Trigger animation overlay (does NOT interrupt movement)
        handAnimationController?.PlayShootShotgun(isPrimaryHand);
        
        // 3. Schedule particle stop
        if (isPrimaryHand)
        {
            if (_rightShotgunCoroutine != null)
                StopCoroutine(_rightShotgunCoroutine);
            _rightShotgunCoroutine = StartCoroutine(ShotgunBurstCoroutine(isPrimaryHand));
        }
        else
        {
            if (_leftShotgunCoroutine != null)
                StopCoroutine(_leftShotgunCoroutine);
            _leftShotgunCoroutine = StartCoroutine(ShotgunBurstCoroutine(isPrimaryHand));
        }
        
        // 4. Notify listeners
        OnShootingStarted?.Invoke(isPrimaryHand, ShootingType.Shotgun);
    }
    
    private void StartShotgunParticles(bool isPrimaryHand)
    {
        ParticleSystem[] particleSystems = isPrimaryHand ? rightHandParticleSystems : leftHandParticleSystems;
        
        foreach (var ps in particleSystems)
        {
            if (ps != null && ps.gameObject.name.ToLower().Contains("shotgun"))
            {
                ps.Play();
                if (enableDebugLogs)
                    Debug.Log($"[ShootingActionController] Started shotgun particles: {ps.name}");
            }
        }
    }
    
    private System.Collections.IEnumerator ShotgunBurstCoroutine(bool isPrimaryHand)
    {
        yield return new WaitForSeconds(shotgunBurstDuration);
        
        // Stop shotgun particles
        StopShotgunParticles(isPrimaryHand);
        
        // Clear coroutine reference
        if (isPrimaryHand)
            _rightShotgunCoroutine = null;
        else
            _leftShotgunCoroutine = null;
        
        // Notify listeners
        OnShootingStopped?.Invoke(isPrimaryHand, ShootingType.Shotgun);
        
        if (enableDebugLogs)
            Debug.Log($"[ShootingActionController] Shotgun burst complete - {(isPrimaryHand ? "Left" : "Right")} hand");
    }
    
    private void StopShotgunParticles(bool isPrimaryHand)
    {
        ParticleSystem[] particleSystems = isPrimaryHand ? rightHandParticleSystems : leftHandParticleSystems;
        
        foreach (var ps in particleSystems)
        {
            if (ps != null && ps.gameObject.name.ToLower().Contains("shotgun"))
            {
                ps.Stop();
                if (enableDebugLogs)
                    Debug.Log($"[ShootingActionController] Stopped shotgun particles: {ps.name}");
            }
        }
    }
    
    // === BEAM SHOOTING ===
    
    public void StartBeam(bool isPrimaryHand)
    {
        bool wasActive = isPrimaryHand ? _rightBeamActive : _leftBeamActive;
        
        if (wasActive)
        {
            if (enableDebugLogs)
                Debug.Log($"[ShootingActionController] Beam already active - {(isPrimaryHand ? "Left" : "Right")} hand");
            return;
        }
        
        if (enableDebugLogs)
            Debug.Log($"[ShootingActionController] Beam started - {(isPrimaryHand ? "Left" : "Right")} hand");
        
        // 1. Start particle effects IMMEDIATELY
        StartBeamParticles(isPrimaryHand);
        
        // 2. Trigger animation overlay (does NOT interrupt movement)
        if (isPrimaryHand)
            handAnimationController?.StartBeamRight();
        else
            handAnimationController?.StartBeamLeft();
        
        // 3. Update state
        if (isPrimaryHand)
            _rightBeamActive = true;
        else
            _leftBeamActive = true;
        
        // 4. Notify listeners
        OnShootingStarted?.Invoke(isPrimaryHand, ShootingType.Beam);
    }
    
    public void StopBeam(bool isPrimaryHand)
    {
        bool wasActive = isPrimaryHand ? _rightBeamActive : _leftBeamActive;
        
        if (!wasActive)
        {
            if (enableDebugLogs)
                Debug.Log($"[ShootingActionController] Beam already inactive - {(isPrimaryHand ? "Left" : "Right")} hand");
            return;
        }
        
        if (enableDebugLogs)
            Debug.Log($"[ShootingActionController] Beam stopped - {(isPrimaryHand ? "Left" : "Right")} hand");
        
        // 1. Stop particle effects IMMEDIATELY
        StopBeamParticles(isPrimaryHand);
        
        // 2. Stop animation overlay (movement continues uninterrupted)
        if (isPrimaryHand)
            handAnimationController?.StopBeamRight();
        else
            handAnimationController?.StopBeamLeft();
        
        // 3. Update state
        if (isPrimaryHand)
            _rightBeamActive = false;
        else
            _leftBeamActive = false;
        
        // 4. Notify listeners
        OnShootingStopped?.Invoke(isPrimaryHand, ShootingType.Beam);
    }
    
    private void StartBeamParticles(bool isPrimaryHand)
    {
        ParticleSystem[] particleSystems = isPrimaryHand ? rightHandParticleSystems : leftHandParticleSystems;
        
        foreach (var ps in particleSystems)
        {
            if (ps != null && (ps.gameObject.name.ToLower().Contains("beam") || ps.gameObject.name.ToLower().Contains("stream")))
            {
                ps.Play();
                if (enableDebugLogs)
                    Debug.Log($"[ShootingActionController] Started beam particles: {ps.name}");
            }
        }
    }
    
    private void StopBeamParticles(bool isPrimaryHand)
    {
        ParticleSystem[] particleSystems = isPrimaryHand ? rightHandParticleSystems : leftHandParticleSystems;
        
        foreach (var ps in particleSystems)
        {
            if (ps != null && (ps.gameObject.name.ToLower().Contains("beam") || ps.gameObject.name.ToLower().Contains("stream")))
            {
                ps.Stop();
                if (enableDebugLogs)
                    Debug.Log($"[ShootingActionController] Stopped beam particles: {ps.name}");
            }
        }
    }
    
    // === UTILITY METHODS ===
    
    public void StopAllShooting()
    {
        // Stop all beams
        if (_leftBeamActive) StopBeam(false);
        if (_rightBeamActive) StopBeam(true);
        
        // Stop all shotgun bursts
        if (_leftShotgunCoroutine != null)
        {
            StopCoroutine(_leftShotgunCoroutine);
            _leftShotgunCoroutine = null;
            StopShotgunParticles(false);
        }
        
        if (_rightShotgunCoroutine != null)
        {
            StopCoroutine(_rightShotgunCoroutine);
            _rightShotgunCoroutine = null;
            StopShotgunParticles(true);
        }
        
        // Stop animation overlays
        handAnimationController?.StopAllBeams();
        
        if (enableDebugLogs)
            Debug.Log("[ShootingActionController] All shooting stopped");
    }
    
    private void StopAllParticleSystems()
    {
        foreach (var ps in leftHandParticleSystems)
        {
            if (ps != null) ps.Stop();
        }
        
        foreach (var ps in rightHandParticleSystems)
        {
            if (ps != null) ps.Stop();
        }
    }
    
    // === PUBLIC PROPERTIES ===
    
    public bool IsLeftBeamActive => _leftBeamActive;
    public bool IsRightBeamActive => _rightBeamActive;
    public bool IsAnyShooting => _leftBeamActive || _rightBeamActive || _leftShotgunCoroutine != null || _rightShotgunCoroutine != null;
    
    // === BACKWARD COMPATIBILITY ===
    
    public void OnBeamStarted(bool isPrimaryHand)
    {
        StartBeam(isPrimaryHand);
    }
    
    public void OnBeamStopped(bool isPrimaryHand)
    {
        StopBeam(isPrimaryHand);
    }
    
    public void OnShotgunFired(bool isPrimaryHand)
    {
        FireShotgun(isPrimaryHand);
    }
    
    // === DEBUG METHODS ===
    
    [System.Obsolete("Debug only")]
    public void DEBUG_LogShootingState()
    {
        Debug.Log($"[ShootingActionController] Shooting State:" +
                 $"\n  Left Beam: {_leftBeamActive}" +
                 $"\n  Right Beam: {_rightBeamActive}" +
                 $"\n  Left Shotgun: {(_leftShotgunCoroutine != null)}" +
                 $"\n  Right Shotgun: {(_rightShotgunCoroutine != null)}" +
                 $"\n  Any Shooting: {IsAnyShooting}");
    }
    
    void OnDestroy()
    {
        // Clean up coroutines
        if (_leftShotgunCoroutine != null)
            StopCoroutine(_leftShotgunCoroutine);
        if (_rightShotgunCoroutine != null)
            StopCoroutine(_rightShotgunCoroutine);
    }
}
