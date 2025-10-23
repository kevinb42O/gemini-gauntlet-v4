using UnityEngine;
using GeminiGauntlet.Audio;
using GeminiGauntlet.Missions.Integration;

/// <summary>
/// GuardianEnemy - Enemy that guards and orbits a platform, attacking intruders
/// Based on PackHunterEnemy but with platform guarding behavior
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class GuardianEnemy : MonoBehaviour, IDamageable
{
    [Header("Platform Assignment")]
    [SerializeField] private GameObject platformToGuard; // Assigned in inspector
    [SerializeField] private float orbitRadius = 8f;
    [SerializeField] private float orbitSpeed = 2f;
    [SerializeField] private float orbitHeight = 3f; // Height above platform
    
    [Header("Core Stats")]
    [SerializeField] private float maxHealth = 75f;
    [SerializeField] private float moveSpeed = 15f;
    
    [Header("Behavior")]
    [SerializeField] private float intruderDetectionRange = 20f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float returnToOrbitDistance = 25f; // Distance from platform to return to orbit
    
    [Header("Hit Feedback")]
    [SerializeField] private float hitFlashDuration = 0.2f;
    [SerializeField] private Color hitFlashColor = Color.red;
    
    [Header("Effects")]
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip alertSound; // Sound when detecting intruder
    
    // Private variables
    private float currentHealth;
    private Rigidbody _rb;
    private SphereCollider _collider;
    private AudioSource audioSource;
    private bool isDeadInternal = false;
    
    // Static cached player references
    private static Transform _playerTransform;
    private static PlayerHealth _playerHealth;
    private static bool hasSearchedForPlayer = false;
    
    // Orbit variables
    private Vector3 platformCenter;
    private float currentOrbitAngle = 0f;
    private bool hasValidPlatform = false;
    
    // Hit feedback variables
    private Renderer[] renderers;
    private Material[] originalMaterials;
    private bool isFlashing = false;
    
    // Guardian states
    private enum GuardianState { Orbiting, Pursuing, Attacking, Returning }
    private GuardianState currentState = GuardianState.Orbiting;
    private bool hasPlayedAlertSound = false;
    private bool isPassiveMode = false;
    private bool isAlertMode = false;

    void Awake()
    {
        // Component initialization
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<SphereCollider>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Configure Rigidbody for flying movement
        _rb.useGravity = false;
        _rb.linearDamping = 2f;
        _rb.angularDamping = 5f;
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Configure collider as trigger
        if (_collider != null)
        {
            _collider.isTrigger = true;
        }

        // Initialize
        currentHealth = maxHealth;
        
        // Set layer to Enemy
        gameObject.layer = LayerMask.NameToLayer("Enemy");
        
        // Setup hit feedback materials
        SetupHitFeedback();
        
        // Initialize platform guarding
        InitializePlatformGuarding();
    }
    
    void InitializePlatformGuarding()
    {
        if (platformToGuard != null)
        {
            hasValidPlatform = true;
            platformCenter = platformToGuard.transform.position;
            
            // Start at a random angle on the orbit
            currentOrbitAngle = Random.Range(0f, 360f);
            
            // Position guardian at initial orbit position
            Vector3 orbitPosition = GetOrbitPosition(currentOrbitAngle);
            transform.position = orbitPosition;
            
            Debug.Log($"GuardianEnemy: Initialized to guard platform '{platformToGuard.name}' at position {platformCenter}");
        }
        else
        {
            hasValidPlatform = false;
            Debug.LogWarning("GuardianEnemy: No platform assigned to guard! Will remain stationary.");
        }
    }
    
    Vector3 GetOrbitPosition(float angle)
    {
        if (!hasValidPlatform) return transform.position;
        
        float radians = angle * Mathf.Deg2Rad;
        float x = platformCenter.x + orbitRadius * Mathf.Cos(radians);
        float z = platformCenter.z + orbitRadius * Mathf.Sin(radians);
        float y = platformCenter.y + orbitHeight;
        
        return new Vector3(x, y, z);
    }
    
    void OnEnable()
    {
        // Find and cache player
        bool needsPlayerSearch = !hasSearchedForPlayer || 
                                _playerTransform == null || 
                                _playerHealth == null ||
                                (_playerTransform != null && _playerTransform.gameObject == null);
        
        if (needsPlayerSearch)
        {
            hasSearchedForPlayer = false;
            FindAndCachePlayer();
        }
    }

    void FixedUpdate()
    {
        if (isDeadInternal || currentHealth <= 0) return;
        
        // Try to find player if we don't have references yet
        if (_playerTransform == null || _playerHealth == null)
        {
            FindAndCachePlayer();
        }
        
        // Only destroy if player is confirmed dead
        if (_playerHealth != null && _playerHealth.isDead)
        {
            Destroy(gameObject);
            return;
        }

        // Update behavior and movement
        UpdateBehavior();
        UpdateMovement();
    }
    
    void UpdateBehavior()
    {
        if (!hasValidPlatform)
        {
            // No platform to guard, just hover in place
            return;
        }
        
        // In passive mode, just orbit and don't attack
        if (isPassiveMode)
        {
            if (currentState != GuardianState.Orbiting)
            {
                currentState = GuardianState.Orbiting;
            }
            return;
        }
        
        // Check if player is near the platform
        bool playerNearPlatform = false;
        float distanceToPlayer = float.MaxValue;
        float playerDistanceToPlatform = float.MaxValue;
        
        if (_playerTransform != null)
        {
            distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
            playerDistanceToPlatform = Vector3.Distance(platformCenter, _playerTransform.position);
            playerNearPlatform = playerDistanceToPlatform <= intruderDetectionRange;
        }
        
        // State transitions
        switch (currentState)
        {
            case GuardianState.Orbiting:
                if (playerNearPlatform || isAlertMode)
                {
                    currentState = GuardianState.Pursuing;
                    hasPlayedAlertSound = false;
                    Debug.Log($"GuardianEnemy: Intruder detected near platform! Distance: {playerDistanceToPlatform:F1}");
                }
                break;
                
            case GuardianState.Pursuing:
                if (!playerNearPlatform)
                {
                    // Player left the guarded area
                    currentState = GuardianState.Returning;
                    Debug.Log("GuardianEnemy: Intruder fled! Returning to orbit.");
                }
                else if (distanceToPlayer <= attackRange)
                {
                    currentState = GuardianState.Attacking;
                    Debug.Log("GuardianEnemy: In attack range!");
                }
                
                // Play alert sound once when starting pursuit
                if (!hasPlayedAlertSound && alertSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(alertSound);
                    hasPlayedAlertSound = true;
                }
                break;
                
            case GuardianState.Attacking:
                if (!playerNearPlatform)
                {
                    currentState = GuardianState.Returning;
                    Debug.Log("GuardianEnemy: Target escaped! Returning to orbit.");
                }
                else if (distanceToPlayer > attackRange * 1.5f)
                {
                    currentState = GuardianState.Pursuing;
                }
                break;
                
            case GuardianState.Returning:
                // Check if we're close enough to the orbit path to resume orbiting
                Vector3 targetOrbitPos = GetOrbitPosition(currentOrbitAngle);
                float distanceToOrbit = Vector3.Distance(transform.position, targetOrbitPos);
                
                if (distanceToOrbit < 2f)
                {
                    currentState = GuardianState.Orbiting;
                    Debug.Log("GuardianEnemy: Resumed orbiting.");
                }
                
                // If player comes back while returning, pursue again
                if (playerNearPlatform)
                {
                    currentState = GuardianState.Pursuing;
                    hasPlayedAlertSound = false;
                    Debug.Log("GuardianEnemy: Intruder returned during return! Pursuing again.");
                }
                break;
        }
    }
    
    void UpdateMovement()
    {
        if (!hasValidPlatform && currentState == GuardianState.Orbiting)
        {
            // No platform, just hover
            _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 2f);
            return;
        }
        
        Vector3 targetDirection = Vector3.zero;
        float currentSpeed = orbitSpeed;
        Vector3 targetPosition = transform.position;
        
        switch (currentState)
        {
            case GuardianState.Orbiting:
                // Update orbit angle
                currentOrbitAngle += orbitSpeed * Time.fixedDeltaTime * 10f; // 10f is a speed multiplier
                if (currentOrbitAngle >= 360f) currentOrbitAngle -= 360f;
                
                // Get target orbit position
                targetPosition = GetOrbitPosition(currentOrbitAngle);
                targetDirection = (targetPosition - transform.position).normalized;
                currentSpeed = orbitSpeed * 3f; // Multiply for smoother orbiting
                break;
                
            case GuardianState.Pursuing:
            case GuardianState.Attacking:
                if (_playerTransform != null)
                {
                    targetDirection = (_playerTransform.position - transform.position).normalized;
                    currentSpeed = moveSpeed;
                }
                break;
                
            case GuardianState.Returning:
                // Return to orbit position
                targetPosition = GetOrbitPosition(currentOrbitAngle);
                targetDirection = (targetPosition - transform.position).normalized;
                currentSpeed = moveSpeed * 0.8f; // Slightly slower when returning
                break;
        }
        
        // Apply movement
        if (targetDirection.sqrMagnitude > 0.01f)
        {
            Vector3 desiredVelocity = targetDirection * currentSpeed;
            _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, desiredVelocity, Time.fixedDeltaTime * 5f);
            
            // Face movement direction (or player when attacking)
            Vector3 lookDirection = targetDirection;
            if ((currentState == GuardianState.Pursuing || currentState == GuardianState.Attacking) && _playerTransform != null)
            {
                lookDirection = (_playerTransform.position - transform.position).normalized;
            }
            
            if (lookDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 8f);
            }
        }
    }

    private void FindAndCachePlayer()
    {
        if (hasSearchedForPlayer && _playerTransform != null && _playerHealth != null) return;
        
        // Try multiple methods to find the player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = GameObject.Find("Player");
        }
        if (player == null)
        {
            PlayerHealth foundHealth = FindFirstObjectByType<PlayerHealth>();
            if (foundHealth != null)
            {
                player = foundHealth.gameObject;
            }
        }
        
        if (player != null)
        {
            _playerTransform = player.transform;
            
            _playerHealth = player.GetComponent<PlayerHealth>();
            if (_playerHealth == null)
            {
                _playerHealth = player.GetComponentInChildren<PlayerHealth>();
            }
            if (_playerHealth == null)
            {
                _playerHealth = player.GetComponentInParent<PlayerHealth>();
            }
            
            hasSearchedForPlayer = true;
            Debug.Log($"GuardianEnemy: Found player - Transform: {(_playerTransform != null ? "OK" : "NULL")}, Health: {(_playerHealth != null ? "OK" : "NULL")}");
        }
    }
    
    void SetupHitFeedback()
    {
        renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalMaterials[i] = renderers[i].material;
            }
        }
    }

    // INSTANT KILL ON CONTACT
    void OnTriggerEnter(Collider other)
    {
        if (isDeadInternal) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log($"GuardianEnemy: Player contact! Instant kill activated.");
            
            // Play attack sound
            if (attackSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(attackSound);
            }
            
            // Find PlayerHealth
            PlayerHealth playerHealth = _playerHealth;
            if (playerHealth == null) playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth == null) playerHealth = other.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null) playerHealth = other.GetComponentInChildren<PlayerHealth>();
            
            if (playerHealth != null && !playerHealth.isDead)
            {
                Debug.Log($"GuardianEnemy: Killing player!");
                playerHealth.Die();
            }
            else
            {
                // Try IDamageable as fallback
                IDamageable damageable = other.GetComponent<IDamageable>();
                if (damageable == null) damageable = other.GetComponentInParent<IDamageable>();
                if (damageable == null) damageable = other.GetComponentInChildren<IDamageable>();
                
                if (damageable != null)
                {
                    damageable.TakeDamage(9999f, transform.position, Vector3.zero);
                }
            }
            
            // Guardian doesn't die after killing player, returns to guarding
            currentState = GuardianState.Returning;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isDeadInternal) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log($"GuardianEnemy: Player collision! Instant kill activated.");
            
            // Play attack sound
            if (attackSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(attackSound);
            }
            
            PlayerHealth playerHealth = _playerHealth;
            if (playerHealth == null) playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth == null) playerHealth = collision.gameObject.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null) playerHealth = collision.gameObject.GetComponentInChildren<PlayerHealth>();
            
            if (playerHealth != null && !playerHealth.isDead)
            {
                playerHealth.Die();
            }
            else
            {
                IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
                if (damageable == null) damageable = collision.gameObject.GetComponentInParent<IDamageable>();
                if (damageable == null) damageable = collision.gameObject.GetComponentInChildren<IDamageable>();
                
                if (damageable != null)
                {
                    damageable.TakeDamage(9999f, transform.position, Vector3.zero);
                }
            }
            
            // Guardian returns to guarding after attack
            currentState = GuardianState.Returning;
        }
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (isDeadInternal) return;
        
        // Flash red when hit
        FlashHit();
        
        currentHealth -= amount;
        Debug.Log($"GuardianEnemy: Took {amount} damage, health now {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void FlashHit()
    {
        if (isFlashing || renderers == null || renderers.Length == 0) return;
        
        StartCoroutine(HitFlashCoroutine());
    }
    
    System.Collections.IEnumerator HitFlashCoroutine()
    {
        isFlashing = true;
        Debug.Log("GuardianEnemy: Hit flash effect");
        
        // Store original materials
        Material[] tempOriginalMaterials = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                tempOriginalMaterials[i] = renderers[i].material;
            }
        }
        
        // Apply flash color
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                Material flashMaterial = new Material(Shader.Find("Standard"));
                flashMaterial.color = hitFlashColor;
                flashMaterial.SetFloat("_Metallic", 0f);
                flashMaterial.SetFloat("_Glossiness", 0.8f);
                renderer.material = flashMaterial;
            }
        }
        
        yield return new WaitForSeconds(hitFlashDuration);
        
        // Restore original materials
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && i < tempOriginalMaterials.Length && tempOriginalMaterials[i] != null)
            {
                renderers[i].material = tempOriginalMaterials[i];
            }
        }
        
        isFlashing = false;
    }

    void Die()
    {
        if (isDeadInternal) return;
        isDeadInternal = true;
        
        Debug.Log("GuardianEnemy: Destroyed!");
        
        // Spawn death effect
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, transform.rotation);
        }

        // Play death sound
        if (deathSound != null && audioSource != null)
        {
            GameObject tempAudio = new GameObject("Guardian Death Sound");
            tempAudio.transform.position = transform.position;
            AudioSource tempSource = tempAudio.AddComponent<AudioSource>();
            tempSource.clip = deathSound;
            tempSource.volume = audioSource.volume;
            tempSource.pitch = audioSource.pitch;
            tempSource.Play();
            
            Destroy(tempAudio, deathSound.length);
        }

        // Destroy immediately
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        // Platform guarding visualization
        if (platformToGuard != null || hasValidPlatform)
        {
            Vector3 center = hasValidPlatform ? platformCenter : platformToGuard.transform.position;
            
            // Draw orbit path
            Gizmos.color = Color.cyan;
            DrawCircle(center + Vector3.up * orbitHeight, orbitRadius, 32);
            
            // Draw detection range around platform
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, intruderDetectionRange);
            
            // Draw current position on orbit
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                Vector3 orbitPos = GetOrbitPosition(currentOrbitAngle);
                Gizmos.DrawWireSphere(orbitPos, 0.5f);
                Gizmos.DrawLine(transform.position, orbitPos);
            }
        }
        else
        {
            // No platform assigned - show warning
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 2f);
        }

        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw state indicator
        if (Application.isPlaying)
        {
            Vector3 stateIndicatorPos = transform.position + Vector3.up * 2f;
            switch (currentState)
            {
                case GuardianState.Orbiting:
                    Gizmos.color = Color.green;
                    break;
                case GuardianState.Pursuing:
                    Gizmos.color = Color.yellow;
                    break;
                case GuardianState.Attacking:
                    Gizmos.color = Color.red;
                    break;
                case GuardianState.Returning:
                    Gizmos.color = Color.blue;
                    break;
            }
            Gizmos.DrawCube(stateIndicatorPos, Vector3.one * 0.5f);
        }
    }
    
    void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 previousPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }
    
    // Public methods for CaptureAndDestroyPlatform integration
    public void SetPlatformToGuard(GameObject platform, float orbitRad, float orbitSpd, float orbitHt)
    {
        platformToGuard = platform;
        orbitRadius = orbitRad;
        orbitSpeed = orbitSpd;
        orbitHeight = orbitHt;
        
        if (platform != null)
        {
            hasValidPlatform = true;
            platformCenter = platform.transform.position;
            Debug.Log($"GuardianEnemy: Assigned to guard platform '{platform.name}'");
        }
    }
    
    public void SetStartingAngle(float angle)
    {
        currentOrbitAngle = angle;
        if (hasValidPlatform)
        {
            Vector3 startPosition = GetOrbitPosition(currentOrbitAngle);
            transform.position = startPosition;
        }
    }
    
    public void SetPassiveMode(bool passive)
    {
        isPassiveMode = passive;
        if (passive)
        {
            currentState = GuardianState.Orbiting;
            Debug.Log("GuardianEnemy: Entering passive mode");
        }
    }
    
    public void SetAlertMode(bool alert)
    {
        isAlertMode = alert;
        if (alert && !isPassiveMode)
        {
            currentState = GuardianState.Pursuing;
            hasPlayedAlertSound = false;
            Debug.Log("GuardianEnemy: ALERT MODE ACTIVATED!");
        }
    }
}
