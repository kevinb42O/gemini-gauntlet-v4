// --- PowerUp.cs (Physics-Free Optimized Version) ---
using UnityEngine;
using GeminiGauntlet.Audio;

[RequireComponent(typeof(Collider))]
public abstract class PowerUp : MonoBehaviour
{
    [Header("Core Power-Up Settings")]
    public PowerUpType powerUpType;

    [Tooltip("Visual effect spawned when collected.")]
    public GameObject pickupEffectPrefab;

    [Range(0f, 1f)] public float soundVolume = 0.8f;

    [Tooltip("Time in seconds before this power-up despawns if not collected.")]
    public float lifetime = 20f;

    [Header("Visual Animation")]
    [Tooltip("Speed for the vertical bobbing animation.")]
    public float bobSpeed = 2f;
    [Tooltip("Height of the vertical bobbing animation.")]
    public float bobHeight = 0.2f;
    [Tooltip("Speed for the rotation animation.")]
    public float rotationSpeed = 50f;

    [Header("Light Settings")]
    [Tooltip("Intensity of the point light")]
    public float lightIntensity = 2f;
    [Tooltip("Range of the point light")]
    public float lightRange = 3f;
    [Tooltip("Color of the point light")]
    public Color lightColor = Color.white;

    private Vector3 _startPosition;
    private Collider _collider;
    private bool _isCollected = false;
    private Light _pointLight;

    // --- SETUP ---
    protected virtual void Awake()
    {
        _collider = GetComponent<Collider>();
        
        // CONFIGURABLE: Scale powerup based on powerup type for better balance
        float scaleMultiplier = GetScaleMultiplier();
        transform.localScale *= scaleMultiplier;
        
        // Rotate 90 degrees to stand up
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        
        // Set layer to PowerUp
        gameObject.layer = LayerMask.NameToLayer("PowerUp");
        
        // Configure collider as trigger for player detection
        // NO RIGIDBODY NEEDED - powerups spawn at ground level and use pure trigger detection
        if (_collider != null)
        {
            _collider.isTrigger = true;
            
            // Configure collider based on type with proper null checks
            if (_collider is MeshCollider meshCollider)
            {
                meshCollider.convex = true;
            }
            else if (_collider is SphereCollider sphereCollider)
            {
                sphereCollider.radius = GetColliderRadius();
                sphereCollider.center = Vector3.zero;
            }
            
            Debug.Log($"PowerUp ({name}): Trigger collider configured - Type: {_collider.GetType().Name}, Radius: {GetColliderRadius()}", this);
        }
        else
        {
            Debug.LogError($"PowerUp ({name}): No Collider component found!", this);
        }

        // Setup point light for visual feedback
        SetupPointLight();
    }

    protected virtual void Start()
    {
        _startPosition = transform.position;
        if (lifetime > 0)
        {
            Destroy(gameObject, lifetime);
        }
    }

    // --- VISUALS ---
    protected virtual void Update()
    {
        // Always rotate the power-up for visual appeal
        if (rotationSpeed > 0)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        // Bob up and down for visual feedback
        if (bobHeight > 0 && bobSpeed > 0)
        {
            float newY = _startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    // --- COLLECTION ---
    // NOTE: No physics needed - powerups use pure trigger-based collection
    // Spawned at ground level by PoolManager, no falling or collision required

    [Header("Interaction Settings")]
    [Tooltip("Range within which player can press E to interact")]
    public float interactionRange = 3f;
    [Tooltip("Show interaction range in scene view for debugging")]
    public bool showInteractionRange = true;
    [Tooltip("Enable collision-based pickup (walk into it to collect)")]
    public bool enableCollisionPickup = true;
    [Tooltip("Enable E key interaction pickup")]
    public bool enableInteractionPickup = true;

    // --- NEW COLLISION & INTERACTION PICKUP SYSTEM ---
    private Transform _playerTransform;
    private LayeredHandAnimationController _handAnimationController;
    private bool _isPlayerNearby = false;
    
    public bool IsCollected() => _isCollected;
    public bool IsGrounded() => true; // Powerups spawn at ground level, always grounded
    public bool IsPlayerNearby() => _isPlayerNearby;
    
    /// <summary>
    /// Check if player is within interaction range for E key pickup
    /// </summary>
    public bool IsWithinInteractionRange(Vector3 playerPosition)
    {
        if (_isCollected) return false;
        
        float distance = Vector3.Distance(transform.position, playerPosition);
        return distance <= interactionRange;
    }
    
    /// <summary>
    /// COLLISION-BASED PICKUP: Called when player walks into powerup
    /// </summary>
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (_isCollected) return;
        
        // Check if it's the player
        if (other.CompareTag("Player") && enableCollisionPickup)
        {
            Debug.Log($"PowerUp ({name}): Collision pickup triggered by {other.name}", this);
            CollectPowerUp(other.gameObject);
        }
    }
    
    /// <summary>
    /// Track when player is nearby for E key interaction
    /// </summary>
    protected virtual void OnTriggerStay(Collider other)
    {
        if (_isCollected) return;
        
        if (other.CompareTag("Player"))
        {
            _isPlayerNearby = true;
            _playerTransform = other.transform;
            
            // Check for E key press
            if (enableInteractionPickup && Input.GetKeyDown(Controls.Interact))
            {
                Debug.Log($"PowerUp ({name}): E key interaction triggered", this);
                CollectPowerUp(other.gameObject);
            }
        }
    }
    
    /// <summary>
    /// Track when player leaves interaction range
    /// </summary>
    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerNearby = false;
            _playerTransform = null;
        }
    }
    
    /// <summary>
    /// UNIFIED COLLECTION METHOD: Handles both collision and E key pickup
    /// </summary>
    private void CollectPowerUp(GameObject playerObject)
    {
        if (_isCollected)
        {
            Debug.LogWarning($"PowerUp ({name}): Already collected, ignoring collection attempt");
            return;
        }
        
        // Get required components from player
        PlayerProgression playerProgression = playerObject.GetComponent<PlayerProgression>();
        PlayerShooterOrchestrator pso = playerObject.GetComponent<PlayerShooterOrchestrator>();
        PlayerAOEAbility aoeAbility = playerObject.GetComponent<PlayerAOEAbility>();
        PlayerHealth playerHealth = playerObject.GetComponent<PlayerHealth>();
        
        // Get hand animation controller for grab animation
        if (_handAnimationController == null)
        {
            _handAnimationController = playerObject.GetComponent<LayeredHandAnimationController>();
        }

        if (playerProgression != null)
        {
            _isCollected = true;
            Debug.Log($"PowerUp ({name}): Collection successful - Type: {powerUpType}", this);

            // PLAY GRAB ANIMATION on right hand
            if (_handAnimationController != null)
            {
                _handAnimationController.PlayGrabAnimation();
                Debug.Log($"PowerUp ({name}): Playing grab animation", this);
            }
            else
            {
                Debug.LogWarning($"PowerUp ({name}): LayeredHandAnimationController not found - skipping grab animation", this);
            }

            // Notify PowerupDisplay that a powerup was collected
            NotifyPowerupDisplay();

            // Delegate the specific effect logic to the child class
            ApplyPowerUpEffect(playerProgression, pso, aoeAbility, playerHealth);

            // Play pickup effects
            if (pickupEffectPrefab != null)
            {
                Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
            }

            // NOTE: Powerup collection sound removed - sound now plays on activation instead of pickup

            // Destroy the power-up
            Destroy(gameObject);
        }
        else
        {
            Debug.LogError($"PowerUp ({name}): PlayerProgression component is null - cannot apply powerup effect!", this);
        }
    }

    /// <summary>
    /// Notify all PowerupDisplay components that this powerup was collected
    /// </summary>
    private void NotifyPowerupDisplay()
    {
        PowerupDisplay[] displays = FindObjectsOfType<PowerupDisplay>();
        foreach (PowerupDisplay display in displays)
        {
            display.OnPowerupCollected(powerUpType, GetPowerupDuration());
        }
        Debug.Log($"PowerUp ({name}): Notified {displays.Length} PowerupDisplay components");
    }

    /// <summary>
    /// Get the duration for this powerup type (override in child classes for specific durations)
    /// </summary>
    protected virtual float GetPowerupDuration()
    {
        switch (powerUpType)
        {
            case PowerUpType.MaxHandUpgrade:
                return 15f; // Default duration
            case PowerUpType.HomingDaggers:
            case PowerUpType.AOEAttack:
                return 0f; // Charge-based, show indefinitely
            case PowerUpType.DoubleGems:
                return 30f; // Default duration
            case PowerUpType.SlowTime:
                return 10f; // Default duration
            case PowerUpType.GodMode:
                return 15f; // Default duration
            case PowerUpType.InstantCooldown:
                return 8f; // Default duration
            case PowerUpType.DoubleDamage:
                return 15f; // Default duration
            default:
                return 0f;
        }
    }
    
    /// <summary>
    /// Draw interaction range in scene view for debugging
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (showInteractionRange)
        {
            // Interaction range (E key)
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f); // Cyan
            Gizmos.DrawWireSphere(transform.position, interactionRange);
            
            // Trigger collider range (collision pickup)
            if (_collider != null && _collider is SphereCollider sphereCollider)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.2f); // Green
                Gizmos.DrawWireSphere(transform.position, sphereCollider.radius);
            }
            
            // Draw red if collected
            if (_isCollected)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, interactionRange * 0.5f);
            }
            
            // Draw yellow if player nearby
            if (_isPlayerNearby)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, _playerTransform.position);
            }
        }
    }

    // This is the method each specific power-up MUST implement.
    protected abstract void ApplyPowerUpEffect(PlayerProgression progression, PlayerShooterOrchestrator pso, PlayerAOEAbility aoeAbility, PlayerHealth playerHealth);

    private void SetupPointLight()
    {
        // Create a new GameObject for the light
        GameObject lightObj = new GameObject("PowerUpLight");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = Vector3.zero;
        
        // Add and configure the light component
        _pointLight = lightObj.AddComponent<Light>();
        _pointLight.type = LightType.Point;
        _pointLight.intensity = lightIntensity;
        _pointLight.range = lightRange;
        
        // Set color based on power-up type
        switch (powerUpType)
        {
            case PowerUpType.MaxHandUpgrade:
                _pointLight.color = new Color(1f, 0.5f, 0f); // Orange - represents power/upgrade
                break;
            case PowerUpType.HomingDaggers:
                _pointLight.color = new Color(0f, 0.8f, 1f); // Cyan - represents precision/tracking
                break;
            case PowerUpType.AOEAttack:
                _pointLight.color = new Color(1f, 0f, 0f); // Red - represents area damage
                break;
            case PowerUpType.DoubleGems:
                _pointLight.color = new Color(1f, 0.8f, 0f); // Yellow - represents wealth/gems
                break;
            case PowerUpType.SlowTime:
                _pointLight.color = new Color(0.5f, 0f, 1f); // Purple - represents time manipulation
                break;
            case PowerUpType.GodMode:
                _pointLight.color = new Color(1f, 1f, 1f); // White - represents invincibility
                break;
            case PowerUpType.InstantCooldown:
                _pointLight.color = new Color(0f, 1f, 1f); // Bright cyan - represents cooling/ice
                break;
            case PowerUpType.DoubleDamage:
                _pointLight.color = new Color(1f, 0.2f, 0.2f); // Bright red - represents increased damage
                break;
            default:
                _pointLight.color = Color.white;
                break;  
        }
    }
    
    /// <summary>
    /// Get scale multiplier based on powerup type for better visual balance
    /// </summary>
    protected virtual float GetScaleMultiplier()
    {
        switch (powerUpType)
        {
            case PowerUpType.MaxHandUpgrade:
            case PowerUpType.GodMode:
                return 3.5f; // Larger for important powerups
            case PowerUpType.AOEAttack:
            case PowerUpType.HomingDaggers:
                return 3.0f; // Standard size for combat powerups
            case PowerUpType.DoubleGems:
            case PowerUpType.SlowTime:
            case PowerUpType.InstantCooldown:
            case PowerUpType.DoubleDamage:
                return 2.5f; // Smaller for utility powerups
            default:
                return 3.0f; // Default size
        }
    }

  /// <summary>
/// Get collider radius based on powerup type for trigger detection
/// </summary>
protected virtual float GetColliderRadius()
{
    switch (powerUpType)
    {
        case PowerUpType.MaxHandUpgrade:
        case PowerUpType.GodMode:
            return 2.5f; // Larger pickup area for important powerups
        case PowerUpType.AOEAttack:
        case PowerUpType.HomingDaggers:
            return 2.2f; // Slightly larger for combat powerups
        default:
            return 2.0f; // Standard pickup area
    }
}

    protected virtual void OnDisable()
    {
        // CRITICAL: Clean up light when object is disabled to prevent memory leaks
        CleanupLight();
    }
    
    protected virtual void OnDestroy()
    {
        // CRITICAL: Clean up light when object is destroyed to prevent memory leaks
        CleanupLight();
    }
    
    /// <summary>
    /// EXPERT LEVEL: Centralized light cleanup to prevent memory leaks
    /// Called from both OnDisable() and OnDestroy() to cover all edge cases
    /// </summary>
    private void CleanupLight()
    {
        if (_pointLight != null)
        {
            // Destroy the light's GameObject (not just the component)
            if (_pointLight.gameObject != null)
            {
                Destroy(_pointLight.gameObject);
            }
            _pointLight = null;
        }
    }
}