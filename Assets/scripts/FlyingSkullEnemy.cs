// --- FlyingSkullEnemy.cs - Persistent World Flying Skull ---
using UnityEngine;
using GeminiGauntlet.Audio;
using GeminiGauntlet.Missions.Integration;
using System.Collections;
using System.Collections.Generic;
using CompanionAI;

/// <summary>
/// 💀 FLYING SKULL ENEMY - Persistent world threat
/// 
/// A floating skull that:
/// - Spawns persistently in the world (not tower-based)
/// - Flies freely with erratic floating movement
/// - Simple 360° detection radius
/// - Wall/ground/ceiling aware pathfinding
/// - Chases player relentlessly when detected
/// - Indoor-optimized with obstacle avoidance
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class FlyingSkullEnemy : MonoBehaviour, IDamageable
{
    public enum FlyingSkullState { Idle, Hunting, Attacking, Dead }

    [Header("Core Stats")]
    [Tooltip("Maximum health of the flying skull.")]
    public float maxHealth = 30f;
    [Tooltip("Base flying speed.")]
    public float flySpeed = 10f;
    [Tooltip("Display name for UI or logging purposes.")]
    public string displayName = "Flying Skull";

    [Header("Detection & Attack")]
    [Tooltip("360° detection radius - skull activates when player enters this range.")]
    public float detectionRadius = 50f;
    [Tooltip("Attack range - skull deals damage within this distance.")]
    public float attackRange = 3f;
    [Tooltip("Chase range - skull gives up if player exceeds this distance.")]
    public float maxChaseRange = 150f;
    [Tooltip("How often to check for player detection (seconds).")]
    [Range(0.1f, 2f)] public float detectionInterval = 0.5f;

    [Header("Flying Behavior")]
    [Tooltip("Amplitude of floating up/down motion.")]
    public float floatAmplitude = 2f;
    [Tooltip("Speed of floating oscillation.")]
    public float floatSpeed = 1.5f;
    [Tooltip("Erratic movement intensity (0 = smooth, 1 = very erratic).")]
    [Range(0f, 1f)] public float erraticMovementIntensity = 0.6f;
    [Tooltip("How often to change erratic direction (seconds).")]
    [Range(0.5f, 3f)] public float erraticChangeInterval = 1.2f;

    [Header("Obstacle Avoidance - INDOOR OPTIMIZED")]
    [Tooltip("Minimum clearance from walls.")]
    public float wallClearance = 2f;
    [Tooltip("Minimum clearance from ground.")]
    public float groundClearance = 3f;
    [Tooltip("Minimum clearance from ceiling.")]
    public float ceilingClearance = 2f;
    [Tooltip("Obstacle detection range (raycasts).")]
    public float obstacleDetectionRange = 5f;
    [Tooltip("Force to push away from obstacles.")]
    public float avoidanceForce = 15f;
    [Tooltip("Layers that count as obstacles (walls, ground, ceiling).")]
    public LayerMask obstacleLayers = -1;

    [Header("Effects & Visuals")]
    [Tooltip("Particle effect spawned on death.")]
    public GameObject deathEffectPrefab;
    [Tooltip("Enable hit glow effect when damaged.")]
    public bool enableHitGlow = true;
    [Tooltip("Hit glow duration.")]
    public float hitGlowDuration = 0.15f;
    [Tooltip("Hit glow intensity.")]
    public float hitGlowIntensity = 1000f;

    [Header("Audio Settings")]
    [SerializeField] [Range(0f, 1f)] private float chatterVolume = 0.6f;
    [SerializeField] [Range(0f, 1f)] private float attackVolume = 0.8f;
    [SerializeField] [Range(0f, 1f)] private float deathVolume = 0.9f;
    // Direct sound management - each skull has its own SoundHandle
    private SoundHandle _skullChatterHandle = SoundHandle.Invalid;

    [Header("PowerUp Drops")]
    [Tooltip("Can drop power-ups on death?")]
    public bool canDropPowerUps = true;
    [Tooltip("Power-up drop chance (0-1).")]
    [Range(0f, 1f)] public float powerUpDropChance = 0.08f;
    [Tooltip("Possible power-up prefabs to drop.")]
    public List<GameObject> powerUpPrefabs;

    // --- Internal State ---
    private Rigidbody _rb;
    private SphereCollider _collider;
    private float currentHealth;
    private bool isDead = false;
    private FlyingSkullState currentState = FlyingSkullState.Idle;
    
    /// <summary>
    /// Public accessor for death state (used by spawn manager and other systems)
    /// </summary>
    public bool IsDead => isDead;

    // --- Targeting ---
    private static Transform _playerTransform;
    private static PlayerHealth _playerHealth;
    private static bool hasSearchedForPlayer = false;
    private Transform primaryTarget;
    private bool primaryTargetIsPlayer;
    private CompanionCore primaryTargetCompanion;
    
    /// <summary>
    /// Clears static player references to prevent memory leaks between scenes
    /// </summary>
    public static void ClearStaticPlayerReferences()
    {
        _playerTransform = null;
        _playerHealth = null;
        hasSearchedForPlayer = false;
    }

    // --- Movement ---
    private float floatTimer;
    private Vector3 erraticDirection;
    private float erraticTimer;
    private Vector3 basePosition;

    // --- Timers ---
    private float detectionTimer;

    // --- Cached Components ---
    private Renderer[] cachedRenderers;
    private ParticleSystem[] cachedParticles;
    private Renderer mainRenderer;
    private Material originalMaterial;
    private Color originalEmissionColor;
    private bool isGlowing = false;

    #region Unity Lifecycle

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<SphereCollider>();

        // Configure rigidbody for flying
        _rb.useGravity = false;
        _rb.linearDamping = 3f;
        _rb.angularDamping = 5f;
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Set to Enemy layer and make trigger
        gameObject.layer = LayerMask.NameToLayer("Enemy");
        if (_collider != null)
        {
            _collider.isTrigger = true;
        }

        // Initialize state
        currentHealth = maxHealth;
        basePosition = transform.position;
        floatTimer = Random.Range(0f, Mathf.PI * 2f);
        erraticDirection = Random.onUnitSphere;
        erraticTimer = Random.Range(0f, erraticChangeInterval);
        detectionTimer = Random.Range(0f, detectionInterval);

        // Cache components
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        cachedParticles = GetComponentsInChildren<ParticleSystem>(true);
        
        // Initialize hit glow
        InitializeHitGlow();
    }

    private bool hasUnregistered = false; // Guard against multiple OnDisable calls
    
    void OnEnable()
    {
        // CRITICAL: Reset unregister guard for pooled objects
        hasUnregistered = false;
        
        // Find player on enable
        if (!hasSearchedForPlayer || _playerTransform == null || _playerHealth == null)
        {
            FindAndCachePlayer();
        }

        // Reset state
        isDead = false;
        currentHealth = maxHealth;
        currentState = FlyingSkullState.Idle;
        ResetVisuals();

        // Start chatter sound directly (no manager needed)
        _skullChatterHandle = SkullSoundEvents.StartSkullChatter(transform, chatterVolume);
    }

    void Update()
    {
        if (isDead) return;

        // Update timers
        floatTimer += Time.deltaTime * floatSpeed;
        erraticTimer -= Time.deltaTime;
        if (erraticTimer <= 0f)
        {
            erraticDirection = Random.onUnitSphere;
            erraticTimer = erraticChangeInterval;
        }

        // Detection check
        detectionTimer -= Time.deltaTime;
        if (detectionTimer <= 0f)
        {
            UpdateDetectionAndState();
            detectionTimer = detectionInterval;
        }
    }

    void FixedUpdate()
    {
        if (isDead)
        {
            _rb.linearVelocity = Vector3.zero;
            return;
        }

        // Calculate and apply movement
        Vector3 desiredVelocity = CalculateDesiredVelocity();
        
        // Add obstacle avoidance (walls, ground, ceiling)
        desiredVelocity += CalculateObstacleAvoidance();

        _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, desiredVelocity, Time.fixedDeltaTime * 5f);

        // Smooth rotation towards movement direction
        if (desiredVelocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(desiredVelocity.normalized);
            _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRotation, Time.fixedDeltaTime * 3f);
        }
    }

    void OnDisable()
    {
        // CRITICAL: Guard against multiple OnDisable calls (Unity bug)
        if (hasUnregistered) return;
        hasUnregistered = true;
        
        // Stop chatter sound instantly (AAA quality - no fade)
        if (_skullChatterHandle != null && _skullChatterHandle.IsValid)
        {
            _skullChatterHandle.Stop(); // INSTANT SILENCE
            _skullChatterHandle = SoundHandle.Invalid;
        }
    }
    
    void OnDestroy()
    {
        // Clear static references when skulls are destroyed to prevent memory leaks
        if (_playerTransform != null && _playerTransform.gameObject == null)
        {
            ClearStaticPlayerReferences();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (!other.CompareTag("Player") && !other.CompareTag("Companion"))
            return;

        PlayAttackSound();

        bool inflictedDamage = false;

        // Damage player
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = _playerHealth != null ? _playerHealth : other.GetComponent<PlayerHealth>();
            if (playerHealth != null && !playerHealth.isDead)
            {
                playerHealth.Die();
                inflictedDamage = true;
            }
        }

        // Damage companions
        if (!inflictedDamage)
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(9999f, transform.position, Vector3.zero);
                inflictedDamage = true;
            }
        }

        // Skull dies after successful attack
        if (inflictedDamage)
        {
            Die();
        }
    }

    #endregion

    #region AI & Detection

    private void UpdateDetectionAndState()
    {
        // Update target
        bool hasTarget = TryUpdatePrimaryTarget();
        
        if (!hasTarget || primaryTarget == null)
        {
            // No target - return to idle
            if (currentState != FlyingSkullState.Idle)
            {
                currentState = FlyingSkullState.Idle;
                basePosition = transform.position; // Update base position for idle floating
            }
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, primaryTarget.position);

        // State transitions
        switch (currentState)
        {
            case FlyingSkullState.Idle:
                if (distanceToTarget <= detectionRadius)
                {
                    currentState = FlyingSkullState.Hunting;
                }
                break;

            case FlyingSkullState.Hunting:
                if (distanceToTarget <= attackRange)
                {
                    currentState = FlyingSkullState.Attacking;
                }
                else if (distanceToTarget > maxChaseRange)
                {
                    currentState = FlyingSkullState.Idle;
                    basePosition = transform.position;
                }
                break;

            case FlyingSkullState.Attacking:
                if (distanceToTarget > attackRange * 1.5f)
                {
                    currentState = FlyingSkullState.Hunting;
                }
                break;
        }
    }

    private bool TryUpdatePrimaryTarget()
    {
        // Validate current target
        if (primaryTarget != null)
        {
            if (primaryTargetIsPlayer)
            {
                if (IsPlayerValid()) return true;
            }
            else if (primaryTargetCompanion != null && IsCompanionValid(primaryTargetCompanion))
            {
                return true;
            }
        }

        // Find new target
        Transform bestTarget = null;
        bool bestIsPlayer = false;
        CompanionCore bestCompanion = null;
        float bestDistance = float.MaxValue;

        float maxRangeSqr = Mathf.Max(detectionRadius, maxChaseRange);
        maxRangeSqr *= maxRangeSqr;

        // Check player
        if (IsPlayerValid())
        {
            float distSqr = (transform.position - _playerTransform.position).sqrMagnitude;
            if (distSqr <= maxRangeSqr)
            {
                bestTarget = _playerTransform;
                bestIsPlayer = true;
                bestDistance = distSqr;
            }
        }

        // Check companions
        foreach (CompanionCore companion in CompanionCore.GetActiveCompanions())
        {
            if (!IsCompanionValid(companion)) continue;

            float distSqr = (transform.position - companion.transform.position).sqrMagnitude;
            if (distSqr <= maxRangeSqr && distSqr < bestDistance)
            {
                bestTarget = companion.transform;
                bestIsPlayer = false;
                bestCompanion = companion;
                bestDistance = distSqr;
            }
        }

        // Update target
        if (bestTarget != null)
        {
            primaryTarget = bestTarget;
            primaryTargetIsPlayer = bestIsPlayer;
            primaryTargetCompanion = bestIsPlayer ? null : bestCompanion;
            return true;
        }

        primaryTarget = null;
        primaryTargetIsPlayer = false;
        primaryTargetCompanion = null;
        return false;
    }

    private bool IsPlayerValid()
    {
        if (_playerTransform == null || _playerHealth == null)
        {
            FindAndCachePlayer();
            if (_playerTransform == null || _playerHealth == null)
                return false;
        }

        return !_playerHealth.isDead && _playerTransform.gameObject.activeInHierarchy;
    }

    private bool IsCompanionValid(CompanionCore companion)
    {
        return companion != null && companion.IsActive && companion.gameObject.activeInHierarchy;
    }

    private static void FindAndCachePlayer()
    {
        GameObject[] playerCandidates = GameObject.FindGameObjectsWithTag("Player");
        
        foreach (GameObject candidate in playerCandidates)
        {
            PlayerHealth health = candidate.GetComponent<PlayerHealth>();
            if (health != null)
            {
                _playerTransform = candidate.transform;
                _playerHealth = health;
                hasSearchedForPlayer = true;
                return;
            }
        }

        // Fallback
        PlayerHealth foundHealth = UnityEngine.Object.FindObjectOfType<PlayerHealth>();
        if (foundHealth != null)
        {
            _playerTransform = foundHealth.transform;
            _playerHealth = foundHealth;
        }

        hasSearchedForPlayer = true;
    }

    #endregion

    #region Movement Calculation

    private Vector3 CalculateDesiredVelocity()
    {
        Vector3 velocity = Vector3.zero;

        switch (currentState)
        {
            case FlyingSkullState.Idle:
                // Float around base position with erratic movement
                Vector3 floatOffset = Vector3.up * Mathf.Sin(floatTimer) * floatAmplitude;
                Vector3 targetIdlePos = basePosition + floatOffset;
                velocity = (targetIdlePos - transform.position) * flySpeed * 0.3f;
                
                // Add erratic movement
                velocity += erraticDirection * erraticMovementIntensity * flySpeed * 0.5f;
                break;

            case FlyingSkullState.Hunting:
                if (primaryTarget != null)
                {
                    // Chase target with floating motion
                    Vector3 directionToTarget = (primaryTarget.position - transform.position).normalized;
                    velocity = directionToTarget * flySpeed;

                    // Add floating oscillation
                    velocity.y += Mathf.Sin(floatTimer) * floatAmplitude * 0.5f;

                    // Add erratic movement for unpredictability
                    Vector3 perpendicular = Vector3.Cross(directionToTarget, Vector3.up);
                    velocity += perpendicular * Mathf.Sin(floatTimer * 2f) * erraticMovementIntensity * flySpeed * 0.3f;
                }
                break;

            case FlyingSkullState.Attacking:
                if (primaryTarget != null)
                {
                    // Direct fast attack
                    Vector3 attackDirection = (primaryTarget.position - transform.position).normalized;
                    velocity = attackDirection * flySpeed * 1.8f;
                }
                break;
        }

        return velocity;
    }

    private Vector3 CalculateObstacleAvoidance()
    {
        Vector3 avoidance = Vector3.zero;

        // Multi-directional raycasts for comprehensive obstacle detection
        Vector3[] checkDirections = {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right,
            Vector3.up,
            Vector3.down,
            (Vector3.forward + Vector3.right).normalized,
            (Vector3.forward + Vector3.left).normalized,
            (Vector3.back + Vector3.right).normalized,
            (Vector3.back + Vector3.left).normalized
        };

        foreach (Vector3 direction in checkDirections)
        {
            Vector3 worldDir = transform.TransformDirection(direction);
            RaycastHit hit;

            if (Physics.Raycast(transform.position, worldDir, out hit, obstacleDetectionRange, obstacleLayers))
            {
                // Determine clearance based on surface type
                float requiredClearance = wallClearance;
                
                if (Vector3.Dot(hit.normal, Vector3.up) > 0.7f)
                    requiredClearance = groundClearance; // Ground
                else if (Vector3.Dot(hit.normal, Vector3.down) > 0.7f)
                    requiredClearance = ceilingClearance; // Ceiling

                if (hit.distance < requiredClearance)
                {
                    // Push away from obstacle
                    float forceMultiplier = Mathf.Lerp(2f, 1f, hit.distance / requiredClearance);
                    avoidance += hit.normal * avoidanceForce * forceMultiplier;
                }
            }
        }

        return avoidance;
    }

    #endregion

    #region Damage & Death

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (isDead) return;

        currentHealth -= amount;

        // Hit glow effect
        if (enableHitGlow && !isGlowing && mainRenderer != null)
        {
            StartCoroutine(HitGlowEffect());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void TakeDamage(float amount, Vector3 hitPoint) => TakeDamage(amount, hitPoint, Vector3.zero);
    public void TakeDamage(float amount) => TakeDamage(amount, transform.position, Vector3.zero);

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // ✅ CRITICAL FIX: Stop chatter sound INSTANTLY when skull dies
        // Don't wait for OnDisable() which happens 3 seconds later - player needs immediate silence!
        if (_skullChatterHandle != null && _skullChatterHandle.IsValid)
        {
            _skullChatterHandle.Stop(); // INSTANT SILENCE
            _skullChatterHandle = SoundHandle.Invalid;
        }

        // Disable visuals
        DisableVisuals();

        // Stop movement
        StopAllCoroutines();
        _rb.linearVelocity = Vector3.zero;
        _collider.enabled = false;

        // Track stats
        GameStats.AddSkullKillToCurrentRun();
        PlayerRunStats.Instance?.RegisterSkullKill();

        // XP & Missions
        GeminiGauntlet.Progression.XPHooks.OnEnemyKilled("flyingskull", transform.position);
        MissionProgressHooks.OnEnemyKilled("flyingskull");

        // Power-up drop
        if (canDropPowerUps && powerUpPrefabs != null && powerUpPrefabs.Count > 0)
        {
            if (Random.value < powerUpDropChance)
            {
                GameObject powerup = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Count)];
                PoolManager.SpawnStatic(powerup, transform.position, Quaternion.identity);
            }
        }

        // Audio & Effects
        SkullDeathManager.QueueDeathAudio(transform.position, deathVolume);
        
        if (deathEffectPrefab != null)
        {
            SkullDeathManager.QueueDeathEffect(deathEffectPrefab, transform.position, Quaternion.identity);
        }

        // Cleanup
        var pooled = GetComponent<PooledObject>();
        if (pooled != null)
            PoolManager.DespawnStatic(gameObject, 3f);
        else
            Destroy(gameObject, 3f);
    }

    #endregion

    #region Audio
    // Direct sound management - each skull controls its own chatter SoundHandle
    // Chatter starts in OnEnable() and stops instantly in OnDisable()
    // No manager needed - Unity's spatial audio handles distance attenuation

    private void PlayAttackSound()
    {
        SkullSoundEvents.PlaySkullAttackSound(transform.position, attackVolume);
    }

    #endregion

    #region Visual Effects

    private void InitializeHitGlow()
    {
        if (!enableHitGlow) return;

        mainRenderer = GetComponent<Renderer>();
        if (mainRenderer == null)
            mainRenderer = GetComponentInChildren<Renderer>();

        if (mainRenderer != null)
        {
            originalMaterial = mainRenderer.material;
            mainRenderer.material = new Material(originalMaterial);

            if (mainRenderer.material.HasProperty("_EmissionColor"))
            {
                originalEmissionColor = mainRenderer.material.GetColor("_EmissionColor");
            }
            else
            {
                originalEmissionColor = Color.black;
            }
        }
    }

    private IEnumerator HitGlowEffect()
    {
        if (mainRenderer == null || mainRenderer.material == null)
            yield break;

        isGlowing = true;

        if (mainRenderer.material.HasProperty("_EmissionColor"))
        {
            Color glowColor = originalEmissionColor == Color.black ? Color.red * hitGlowIntensity : originalEmissionColor * hitGlowIntensity;
            
            mainRenderer.material.SetColor("_EmissionColor", glowColor);
            mainRenderer.material.EnableKeyword("_EMISSION");

            yield return new WaitForSeconds(hitGlowDuration);

            mainRenderer.material.SetColor("_EmissionColor", originalEmissionColor);
            if (originalEmissionColor == Color.black)
                mainRenderer.material.DisableKeyword("_EMISSION");
        }

        isGlowing = false;
    }

    private void DisableVisuals()
    {
        foreach (Renderer renderer in cachedRenderers)
        {
            if (renderer != null)
                renderer.enabled = false;
        }

        foreach (ParticleSystem particle in cachedParticles)
        {
            if (particle != null)
                particle.Stop();
        }
    }

    private void ResetVisuals()
    {
        if (cachedRenderers != null)
        {
            foreach (Renderer renderer in cachedRenderers)
            {
                if (renderer != null)
                    renderer.enabled = true;
            }
        }

        if (cachedParticles != null)
        {
            foreach (ParticleSystem particle in cachedParticles)
            {
                if (particle != null)
                {
                    particle.Clear(true);
                    particle.Play(true);
                }
            }
        }
    }

    #endregion

    #region Gizmos

    void OnDrawGizmosSelected()
    {
        // Detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Chase range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxChaseRange);

        // Attack range
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Target line
        if (Application.isPlaying && primaryTarget != null)
        {
            Gizmos.color = primaryTargetIsPlayer ? Color.blue : Color.cyan;
            Gizmos.DrawLine(transform.position, primaryTarget.position);
        }
    }

    #endregion
}
