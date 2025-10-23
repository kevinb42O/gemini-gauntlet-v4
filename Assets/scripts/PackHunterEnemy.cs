using UnityEngine;
using GeminiGauntlet.Audio;
using GeminiGauntlet.Missions.Integration;

using System.Collections.Generic;

/// <summary>
/// PackHunterEnemy - Simple aggressive flying enemy based on SkullEnemy
/// Pure 3D weightless flight, direct pursuit, instant kill on contact
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class PackHunterEnemy : MonoBehaviour, IDamageable
{
    [Header("Core Stats")]
    [SerializeField] private float maxHealth = 50f;
    [SerializeField] private float moveSpeed = 12f;
    
    [Header("Behavior")]
    [SerializeField] private float playerDetectionRange = 30f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float roamRadius = 15f;
    [SerializeField] private float roamSpeed = 6f;
    
    [Header("Pack Behavior")]
    [SerializeField] private bool enableSeparation = true;
    [SerializeField] private float separationRadius = 3f;         // how close others can be before pushing away
    [SerializeField] private float separationStrength = 10f;      // push speed contribution (units/sec)
    [SerializeField] private float separationMaxAdjustSpeed = 8f; // clamp for subtle but firm behavior
    
    [Header("Hit Feedback")]
    [SerializeField] private float hitFlashDuration = 0.2f;
    [SerializeField] private Color hitFlashColor = Color.red;
    
    [Header("Effects")]
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip deathSound;
    
    // Private variables
    private float currentHealth;
    private Rigidbody _rb;
    private SphereCollider _collider;
    private AudioSource audioSource;
    private bool isDeadInternal = false;
    
    // Static cached player references (like SkullEnemy)
    private static Transform _playerTransform;
    private static PlayerHealth _playerHealth;
    private static bool hasSearchedForPlayer = false;
    
    // Global registry for neighbor avoidance
    private static readonly HashSet<PackHunterEnemy> AllHunters = new HashSet<PackHunterEnemy>();
    
    // Roaming variables
    private Vector3 homePosition;
    private Vector3 roamTarget;
    private bool hasPlayerInRange = false;
    
    // Hit feedback variables
    private Renderer[] renderers;
    private Material[] originalMaterials;
    private bool isFlashing = false;
    
    // Simple states
    private enum State { Roaming, Hunting, Attacking }
    private State currentState = State.Roaming;

    void Awake()
    {
        // Component initialization
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<SphereCollider>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Configure Rigidbody for flying movement (like SkullEnemy)
        _rb.useGravity = false;
        _rb.linearDamping = 2f;
        _rb.angularDamping = 5f;
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Configure collider as trigger to prevent physics collisions
        if (_collider != null)
        {
            _collider.isTrigger = true;
        }

        // Initialize
        currentHealth = maxHealth;
        homePosition = transform.position;
        roamTarget = GetRandomRoamTarget();
        
        // Set layer to Enemy
        gameObject.layer = LayerMask.NameToLayer("Enemy");
        
        // Setup hit feedback materials
        SetupHitFeedback();
    }
    
    void OnEnable()
    {
        // Register for separation behavior
        AllHunters.Add(this);

        // Find and cache player (like SkullEnemy)
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

    void OnDisable()
    {
        AllHunters.Remove(this);
    }

    void OnDestroy()
    {
        AllHunters.Remove(this);
    }

    void FixedUpdate()
    {
        if (isDeadInternal || currentHealth <= 0) return;
        
        // Try to find player if we don't have references yet
        if (_playerTransform == null || _playerHealth == null)
        {
            FindAndCachePlayer();
        }
        
        // Only destroy if player is confirmed dead (not just missing)
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
        // Check if player is in detection range
        if (_playerTransform != null)
        {
            // Skip PlayerHealth check for movement - just check if transform exists
            float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
            hasPlayerInRange = distanceToPlayer <= playerDetectionRange;
            
            // State transitions
            switch (currentState)
            {
                case State.Roaming:
                    if (hasPlayerInRange)
                    {
                        currentState = State.Hunting;
                        Debug.Log("PackHunter: Player detected! Switching to hunting mode.");
                    }
                    else if (Vector3.Distance(transform.position, roamTarget) < 2f)
                    {
                        roamTarget = GetRandomRoamTarget();
                    }
                    break;
                    
                case State.Hunting:
                    if (!hasPlayerInRange)
                    {
                        currentState = State.Roaming;
                        roamTarget = GetRandomRoamTarget();
                        Debug.Log("PackHunter: Lost player! Returning to roaming.");
                    }
                    break;
            }
        }
        else
        {
            // No player found, keep roaming
            if (currentState != State.Roaming)
            {
                currentState = State.Roaming;
                roamTarget = GetRandomRoamTarget();
            }
        }
    }
    
    void UpdateMovement()
    {
        Vector3 targetDirection = Vector3.zero;
        float currentSpeed = roamSpeed;
        
        switch (currentState)
        {
            case State.Roaming:
                targetDirection = (roamTarget - transform.position).normalized;
                currentSpeed = roamSpeed;
                break;
                
            case State.Hunting:
                if (_playerTransform != null)
                {
                    targetDirection = (_playerTransform.position - transform.position).normalized;
                    currentSpeed = moveSpeed;
                }
                break;
        }
        
        // Apply movement
        if (targetDirection.sqrMagnitude > 0.01f)
        {
            Vector3 desiredVelocity = targetDirection * currentSpeed;

            // Apply neighbor separation (subtle but firm push away from other pack hunters)
            if (enableSeparation && AllHunters.Count > 1)
            {
                Vector3 separation = ComputeSeparationVector();
                if (separation.sqrMagnitude > 0.0001f)
                {
                    Vector3 sepAdjust = Vector3.ClampMagnitude(separation * separationStrength, separationMaxAdjustSpeed);
                    desiredVelocity += sepAdjust;
                }
            }

            _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, desiredVelocity, Time.fixedDeltaTime * 5f);
            
            // Face movement direction (after separation adjustment)
            Vector3 faceDir = (_rb.linearVelocity.sqrMagnitude > 0.01f) ? _rb.linearVelocity.normalized : targetDirection;
            Quaternion targetRotation = Quaternion.LookRotation(faceDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 8f);
        }
    }

    private void FindAndCachePlayer()
    {
        if (hasSearchedForPlayer && _playerTransform != null && _playerHealth != null) return;
        
        // Try multiple methods to find the player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // Fallback: Find by name
            player = GameObject.Find("Player");
        }
        if (player == null)
        {
            // Fallback: Find any PlayerHealth component in scene
            PlayerHealth foundHealth = FindFirstObjectByType<PlayerHealth>();
            if (foundHealth != null)
            {
                player = foundHealth.gameObject;
            }
        }
        
        if (player != null)
        {
            _playerTransform = player.transform;
            
            // Try multiple ways to get PlayerHealth
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
            Debug.Log($"PackHunter: Found player GameObject '{player.name}' - Transform: {(_playerTransform != null ? "OK" : "NULL")}, Health: {(_playerHealth != null ? "OK" : "NULL")}");
            
            if (_playerHealth == null)
            {
                var components = player.GetComponents<Component>();
                var componentNames = new string[components.Length];
                for (int i = 0; i < components.Length; i++)
                {
                    componentNames[i] = components[i].GetType().Name;
                }
                Debug.LogError($"PackHunter: PlayerHealth component not found on player GameObject '{player.name}'! Available components: {string.Join(", ", componentNames)}");
            }
        }
        else
        {
            Debug.LogWarning("PackHunter: Could not find player GameObject at all!");
        }
    }
    
    Vector3 GetRandomRoamTarget()
    {
        Vector3 randomDirection = Random.insideUnitSphere.normalized;
        return homePosition + randomDirection * Random.Range(5f, roamRadius);
    }
    
    // Compute separation vector away from nearby pack hunters within separationRadius
    Vector3 ComputeSeparationVector()
    {
        if (!enableSeparation) return Vector3.zero;
        Vector3 pos = transform.position;
        Vector3 accum = Vector3.zero;
        int count = 0;
        foreach (var other in AllHunters)
        {
            if (other == null || other == this) continue;
            if (other.isDeadInternal) continue;
            Vector3 toSelf = pos - other.transform.position;
            float dist = toSelf.magnitude;
            if (dist <= 0.0001f || dist > separationRadius) continue;
            float weight = 1f - Mathf.Clamp01(dist / separationRadius); // 1 near zero, 0 at radius
            weight *= weight; // stronger when closer
            accum += (toSelf / Mathf.Max(dist, 0.0001f)) * weight;
            count++;
        }
        if (count == 0) return Vector3.zero;
        return accum / count;
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





    // INSTANT KILL ON CONTACT - Based on SkullEnemy collision system
    void OnTriggerEnter(Collider other)
    {
        if (isDeadInternal) return;

        Debug.Log($"PackHunter: TRIGGER detected with '{other.gameObject.name}' (Tag: '{other.gameObject.tag}', Layer: {other.gameObject.layer})");

        if (other.CompareTag("Player"))
        {
            Debug.Log($"PackHunter: PLAYER TRIGGER CONFIRMED! Attempting instant kill.");
            
            // Play attack sound
            if (attackSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(attackSound);
            }
            
            // Multiple fallback methods to find PlayerHealth (like SkullEnemy)
            PlayerHealth playerHealth = _playerHealth;
            if (playerHealth == null) playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth == null) playerHealth = other.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null) playerHealth = other.GetComponentInChildren<PlayerHealth>();
            
            if (playerHealth != null && !playerHealth.isDead)
            {
                Debug.Log($"PackHunter: INSTANT KILL ACTIVATED - PlayerHealth.Die() called!");
                playerHealth.Die(); // Instant kill like SkullEnemy
            }
            else
            {
                // Try IDamageable as fallback
                IDamageable damageable = other.GetComponent<IDamageable>();
                if (damageable == null) damageable = other.GetComponentInParent<IDamageable>();
                if (damageable == null) damageable = other.GetComponentInChildren<IDamageable>();
                
                if (damageable != null)
                {
                    Debug.Log($"PackHunter: INSTANT KILL via IDamageable interface.");
                    damageable.TakeDamage(9999f, transform.position, Vector3.zero);
                }
                else
                {
                    Debug.LogError($"PackHunter: KILL FAILED - No damage method available!");
                }
            }
            
            Die(); // PackHunter dies after killing player
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isDeadInternal) return;

        Debug.Log($"PackHunter: COLLISION detected with '{collision.gameObject.name}' (Tag: '{collision.gameObject.tag}')");

        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log($"PackHunter: PLAYER COLLISION CONFIRMED! Attempting instant kill.");
            
            // Play attack sound
            if (attackSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(attackSound);
            }
            
            // Same logic as trigger
            PlayerHealth playerHealth = _playerHealth;
            if (playerHealth == null) playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth == null) playerHealth = collision.gameObject.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null) playerHealth = collision.gameObject.GetComponentInChildren<PlayerHealth>();
            
            if (playerHealth != null && !playerHealth.isDead)
            {
                Debug.Log($"PackHunter: COLLISION KILL - PlayerHealth.Die() called!");
                playerHealth.Die();
            }
            else
            {
                IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
                if (damageable == null) damageable = collision.gameObject.GetComponentInParent<IDamageable>();
                if (damageable == null) damageable = collision.gameObject.GetComponentInChildren<IDamageable>();
                
                if (damageable != null)
                {
                    Debug.Log($"PackHunter: COLLISION KILL via IDamageable.");
                    damageable.TakeDamage(9999f, transform.position, Vector3.zero);
                }
                else
                {
                    Debug.LogError($"PackHunter: COLLISION KILL FAILED!");
                }
            }
            
            Die();
        }
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (isDeadInternal) return;
        
        // Flash red when hit
        FlashHit();
        
        currentHealth -= amount;
        Debug.Log($"PackHunter: Took {amount} damage, health now {currentHealth}/{maxHealth}");
        
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
        Debug.Log("PackHunter: Starting hit flash effect");
        
        // Store original materials if not already stored
        Material[] tempOriginalMaterials = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                tempOriginalMaterials[i] = renderers[i].material;
            }
        }
        
        // Apply flash color to all renderers
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                // Create a simple colored material
                Material flashMaterial = new Material(Shader.Find("Standard"));
                flashMaterial.color = hitFlashColor;
                flashMaterial.SetFloat("_Metallic", 0f);
                flashMaterial.SetFloat("_Glossiness", 0.8f);
                renderer.material = flashMaterial;
            }
        }
        
        Debug.Log($"PackHunter: Applied flash to {renderers.Length} renderers");
        yield return new WaitForSeconds(hitFlashDuration);
        
        // Restore original materials
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && i < tempOriginalMaterials.Length && tempOriginalMaterials[i] != null)
            {
                renderers[i].material = tempOriginalMaterials[i];
            }
        }
        
        Debug.Log("PackHunter: Restored original materials");
        isFlashing = false;
    }

    void Die()
    {
        if (isDeadInternal) return;
        isDeadInternal = true;
        
        Debug.Log("PackHunter: Died - destroying immediately!");
        
        // Spawn death effect
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, transform.rotation);
        }

        // Play death sound (but don't wait for it)
        if (deathSound != null && audioSource != null)
        {
            // Create a temporary audio source for the death sound so it plays even after destruction
            GameObject tempAudio = new GameObject("PackHunter Death Sound");
            tempAudio.transform.position = transform.position;
            AudioSource tempSource = tempAudio.AddComponent<AudioSource>();
            tempSource.clip = deathSound;
            tempSource.volume = audioSource.volume;
            tempSource.pitch = audioSource.pitch;
            tempSource.Play();
            
            // Destroy the temp audio object after sound finishes
            Destroy(tempAudio, deathSound.length);
        }

        // DESTROY IMMEDIATELY - no delay
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, playerDetectionRange);

        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw roam area
        Gizmos.color = Color.green;
        Vector3 home = Application.isPlaying ? homePosition : transform.position;
        Gizmos.DrawWireSphere(home, roamRadius);
        
        // Draw current roam target
        if (Application.isPlaying && currentState == State.Roaming)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(roamTarget, 1f);
            Gizmos.DrawLine(transform.position, roamTarget);
        }
        
        // Draw separation radius for tuning
        if (enableSeparation)
        {
            Gizmos.color = new Color(0.4f, 0.6f, 1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, separationRadius);
        }
    }
}
