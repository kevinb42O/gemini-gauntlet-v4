// --- TowerIdleAwakeBehaviour.cs ---
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(TowerController))]
public class TowerIdleAwakeBehaviour : MonoBehaviour
{
    [Header("Tower State")]
    [SerializeField] private bool _isAwake = false;
    
    [Header("Rotation Settings")]
    [SerializeField] private float _idleRotationSpeed = 15f;
    [SerializeField] private float _awakeRotationSpeed = 30f;
    [SerializeField] private float _wakeupSpinDuration = 0.5f;
    [SerializeField] private float _wakeupSpinSpeed = 720f;
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem _wakeupParticles;
    [SerializeField] private AudioClip _wakeupSound;
    [SerializeField] [Range(0f, 1f)] private float _wakeupSoundVolume = 0.8f;
    
    // References
    private TowerController _towerController;
    private Transform _towerRotationTransform;
    private Coroutine _wakeupCoroutine;
    private TowerSoundManager towerSoundManager;
    
    private void Awake()
    {
        _towerController = GetComponent<TowerController>();
        if (_towerController == null)
        {
            Debug.LogError("[TowerIdleAwakeBehaviour] TowerController component not found!", this);
        }
        
        // Get TowerSoundManager component
        towerSoundManager = GetComponent<TowerSoundManager>();
        if (towerSoundManager == null)
        {
            Debug.LogWarning("[TowerIdleAwakeBehaviour] TowerSoundManager component not found!", this);
        }
        
        // Find the main rotation transform - this is the object that will rotate
        // By default, use this transform, but you can override with a child transform
        _towerRotationTransform = transform;
    }
    
    private void OnEnable()
    {
        // Subscribe to platform events via the tower controller
        if (_towerController != null)
        {
            // Set the initial rotation speed
            _towerController.gentleSpinSpeed = _idleRotationSpeed;
        }
    }
    
    private void OnDisable()
    {
        if (_wakeupCoroutine != null)
        {
            StopCoroutine(_wakeupCoroutine);
            _wakeupCoroutine = null;
        }
    }
    
    // Called by TowerController when player enters platform gravity zone
    public void OnPlayerEnteredGravityZone()
    {
        if (!_isAwake)
        {
            WakeUp();
        }
    }
    
    // Called by TowerController when player leaves platform gravity zone
    public void OnPlayerLeftGravityZone()
    {
        // Optional: Could implement going back to sleep here
        // For now, once awake, stays awake
    }
    
    private void WakeUp()
    {
        if (_wakeupCoroutine != null)
        {
            StopCoroutine(_wakeupCoroutine);
        }
        
        _wakeupCoroutine = StartCoroutine(WakeUpSequence());
    }
    
    private IEnumerator WakeUpSequence()
    {
        // Play wakeup effects
        if (_wakeupParticles != null)
        {
            _wakeupParticles.Play();
        }
        
        if (towerSoundManager != null)
        {
            towerSoundManager.PlayTowerWakeup();
        }
        
        // Store original rotation speed
        float originalSpeed = _towerController.gentleSpinSpeed;
        
        // Fast spin for wakeup
        _towerController.gentleSpinSpeed = _wakeupSpinSpeed;
        
        // Wait for wakeup duration
        yield return new WaitForSeconds(_wakeupSpinDuration);
        
        // Set to awake rotation speed
        _towerController.gentleSpinSpeed = _awakeRotationSpeed;
        
        // Mark as awake
        _isAwake = true;
        
        // Start spawning skulls
        // Note: TowerController now handles player detection internally
        
        _wakeupCoroutine = null;
    }
    
    // Public accessor for state
    public bool IsAwake => _isAwake;
}
