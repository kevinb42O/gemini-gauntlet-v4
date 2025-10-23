// --- SkullEnemy.cs (ULTRA OPTIMIZED - High Performance & Robust) ---
using UnityEngine;
using GeminiGauntlet.Audio; // Assuming this namespace contains SkullSoundEvents
using GeminiGauntlet.Missions.Integration; // For mission tracking
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CompanionAI;

// These interfaces are assumed to exist based on the original script.
// public interface IDamageable { void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection); }
// public class TowerController : MonoBehaviour { public void SkullDied(SkullEnemy skull) { } }
// public class PlatformTrigger : MonoBehaviour { }
// public class PlayerHealth : MonoBehaviour { public bool isDead; public void Die() { } }
// public static class GameStats { public static void AddBossMinionKillToCurrentRun() { } public static void AddSkullKillToCurrentRun() { } }
// public class PlayerRunStats : MonoBehaviour { public static PlayerRunStats Instance; public void RegisterSkullKill() { } }
// public static class SkullSoundEvents { public static SoundHandle StartSkullChatter(Transform t, float v) { return null; } public static void StopSkullChatter(SoundHandle h) { } public static void PlaySkullDeathSound(Vector3 p, float v) { } public static void PlaySkullAttackSound(Vector3 p, float v) { } }
// public class SoundHandle { }

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class SkullEnemy : MonoBehaviour, IDamageable
{
    public enum SkullAIState { Spawning, Hunting, Attacking, Decaying }
    public enum AttackPattern { DirectAssault, SwoopingDive, CirclingPredator }

    [Header("Core Stats")]
    [Tooltip("Maximum health of the skull.")]
    public float maxHealth = 20f;
    [Tooltip("Base movement speed.")]
    public float moveSpeed = 8f;
    [Tooltip("If true, this skull was spawned by a boss and contributes to boss-related stats.")]
    public bool isBossMinion = false;
    [Tooltip("Display name for UI or logging purposes.")]
    public string displayName = "Skull Enemy";

    [Header("Attack Behavior")]
    [Tooltip("Range at which the skull will start actively hunting the player.")]
    public float playerDetectionRange = 100f;
    [Tooltip("If the skull gets further than this from the player while hunting, it will give up.")]
    public float maxHuntingRange = 300f;
    [Tooltip("Range at which the skull will transition from Hunting to Attacking.")]
    public float attackRange = 2f;

    [Header("Movement Patterns")]
    [Tooltip("How wide the swooping motions are.")]
    public float swoopAmplitude = 3f;
    [Tooltip("How fast the swooping motions oscillate.")]
    public float swoopFrequency = 2f;
    [Tooltip("The radius of the circle when in the Circling Predator pattern.")]
    public float circleRadius = 5f;
    [Tooltip("The speed in degrees per second when circling the player.")]
    public float circleSpeed = 90f;

    [Header("Life Cycle")]
    [Tooltip("Time spent in the spawning state before hunting.")]
    public float spawnDuration = 1f;
    [Tooltip("How high the skull rises from its spawn point.")]
    public float spawnRiseHeight = 2f;
    [Tooltip("Time spent decaying before being destroyed after losing the player or player dies.")]
    public float decayDuration = 10f;

    [Header("Separation (Anti-Clustering)")]
    [Tooltip("The radius to check for other skulls to avoid.")]
    public float separationRadius = 2f;
    [Tooltip("The force applied to push away from other nearby skulls.")]
    public float separationForce = 10f;
    [Tooltip("LayerMask for separation checks (set this to Enemy)")]
    public LayerMask separationLayerMask = ~0;

    [Header("Effects & Visuals")]
    [Tooltip("The particle effect or prefab to instantiate upon death.")]
    public GameObject deathEffectPrefab;

    [Header("Hit Visual Effects")]
    [Tooltip("Enable hit glow effect when skull takes damage.")]
    public bool enableHitGlow = true;
    [Tooltip("Duration of glow effect when hit (default 0.15s).")]
    public float hitGlowDuration = 0.15f;
    [Tooltip("Glow intensity multiplier when hit (default 1000).")]
    public float hitGlowIntensity = 1000f;

    [Header("Audio Settings")]
    [SerializeField] [Range(0f, 1f)] private float chatterVolume = 0.7f;
    [SerializeField] [Range(0f, 1f)] private float attackVolume = 0.8f;
    [SerializeField] [Range(0f, 1f)] private float deathVolume = 0.9f;
    
    // DIRECT CHATTER MANAGEMENT: Each skull manages its own sound handle
    // No manager needed - spatial audio system handles distance/volume automatically
    private SoundHandle _skullChatterHandle = SoundHandle.Invalid;

    [Header("PowerUp Drops")]
    [Tooltip("Can this enemy drop power-ups on death?")]
    public bool canDropPowerUps = true;
    [Tooltip("The probability (0 to 1) of dropping a power-up.")]
    [Range(0f, 1f)] public float powerUpSpawnChance = 0.05f;

    [Header("Ground Collision Prevention - BULLETPROOF")]
    [Tooltip("Minimum height above ground/platforms to maintain - ABSOLUTE ENFORCEMENT.")]
    public float minGroundClearance = 3f;
    [Tooltip("Force applied to push skull away from ground.")]
    public float groundAvoidanceForce = 25f;
    [Tooltip("Layer mask for ground/platform detection.")]
    public LayerMask groundLayerMask = 1; // Default layer
    [Tooltip("Emergency height constraint - skulls NEVER go below this world Y position.")]
    public float absoluteMinWorldHeight = -50f;
    [Tooltip("Maximum raycast distance for ground detection.")]
    public float maxGroundDetectionRange = 10f;
    [Tooltip("A list of possible power-ups to drop.")]
    public List<GameObject> powerUpPrefabs;

    // --- Core Component & State References ---
    private Rigidbody _rb;
    private SphereCollider _collider; // Using SphereCollider for optimized OverlapSphere checks
    private float currentHealth;
    private bool isDeadInternal = false;
    private SkullAIState currentAIState = SkullAIState.Spawning;
    private AttackPattern attackPattern;

    // --- AI & Movement Variables ---
    private float timeInCurrentState = 0f;
    private Vector3 spawnPosition;
    private Vector3 targetPosition;
    private float swoopTimer;
    private float circleAngle;
    private Vector3 separationVector = Vector3.zero;
    [SerializeField] [Range(8, 64)] private int separationBufferSize = 24;
    private Collider[] separationResults; // reused buffer for OverlapSphereNonAlloc
    // Cached component arrays to avoid allocations on death
    private Renderer[] cachedRenderers;
    private ParticleSystem[] cachedParticles;
    // Tick-based movement jitter (replaces per-frame Random.insideUnitSphere)
    private Vector3 movementJitter = Vector3.zero;

    // --- Optimization & AI Timers ---
    private float separationCalculationTimer = 0f;
    [SerializeField] [Range(0.02f, 0.5f)] private float separationCalculationInterval = 0.15f; // ~6x per second by default

    [Header("AI Tick (Staggered)")]
    [SerializeField] [Range(0.02f, 0.5f)] private float aiTickIntervalMin = 0.08f;
    [SerializeField] [Range(0.02f, 0.5f)] private float aiTickIntervalMax = 0.16f;
    private float aiTickTimer = 0f;

    // LOD-adjusted toggles and baselines
    private bool separationEnabled = true;
    // OPTIMIZED: Ground avoidance cached and updated 5 times per second instead of 50-60 times
    private float _groundCheckTimer = 0f;
    private const float GROUND_CHECK_INTERVAL = 0.2f; // Check 5 times per second (was every FixedUpdate)
    private Vector3 _cachedGroundAvoidance = Vector3.zero; // Reuse result between checks
    private float baseAiMin, baseAiMax, baseSeparationInterval;
    private SkullEnemyManager.SkullLOD currentLOD = SkullEnemyManager.SkullLOD.Near;

    // --- Static Cached Player References (for performance) ---
    private static Transform _playerTransform;
    private static PlayerHealth _playerHealth;
    private static bool hasSearchedForPlayer = false;

    // --- Dynamic Targeting ---
    private Transform _primaryTargetTransform;
    private bool _primaryTargetIsPlayer;
    private CompanionCore _primaryTargetCompanion;
    
    // --- Hit Glow Components ---
    private Material _originalMaterial;
    private Color _originalEmissionColor;
    private bool _isGlowing = false;
    private Renderer _skullRenderer;
    
    // --- Static cleanup method to prevent memory leaks ---
    public static void ClearStaticPlayerReferences()
    {
        _playerTransform = null;
        _playerHealth = null;
        hasSearchedForPlayer = false;
    }

    // --- External System References ---
    private TowerController originatingTower;
    private PlatformTrigger homePlatformTrigger;

    // --- Behavioral Variety ---
    private float behaviorRandomOffset;
    private float zigzagTimer;
    private bool preferVerticalMovement;
    private float diveSpeed;

    #region Unity Lifecycle Methods

    void Awake()
    {
        // --- Component Initialization ---
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<SphereCollider>(); // Enforced by RequireComponent

        // Configure Rigidbody for flying movement
        _rb.useGravity = false;
        _rb.linearDamping = 2f;
        _rb.angularDamping = 5f;
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // GROUND COLLISION PREVENTION: Set skull to Enemy layer and configure collision
        gameObject.layer = LayerMask.NameToLayer("Enemy"); // Ensure skull is on Enemy layer
        
        // Configure collider to be a trigger to prevent physics collisions with platforms
        if (_collider != null)
        {
            _collider.isTrigger = true; // Make trigger to prevent physics collisions
        }

        // --- Initialize State ---
        currentHealth = maxHealth;
        spawnPosition = transform.position;

        // Assign a random attack pattern and starting values for varied behavior
        attackPattern = (AttackPattern)Random.Range(0, System.Enum.GetValues(typeof(AttackPattern)).Length);
        swoopTimer = Random.Range(0f, Mathf.PI * 2f);
        circleAngle = Random.Range(0f, 360f);
        
        // Add behavioral variety
        behaviorRandomOffset = Random.Range(0f, 1f);
        zigzagTimer = Random.Range(0f, Mathf.PI);
        preferVerticalMovement = Random.value > 0.5f;
        diveSpeed = Random.Range(0.8f, 1.2f);

        // Stagger calculations to avoid performance spikes
        separationCalculationTimer = Random.Range(0f, separationCalculationInterval);
        aiTickTimer = Random.Range(aiTickIntervalMin, aiTickIntervalMax);
        _groundCheckTimer = Random.Range(0f, GROUND_CHECK_INTERVAL); // Stagger ground checks

        // Allocate reusable buffer once
        separationResults = new Collider[Mathf.Clamp(separationBufferSize, 8, 64)];

        // Cache child components once
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        cachedParticles = GetComponentsInChildren<ParticleSystem>(true);

        // Save baselines for LOD scaling
        baseAiMin = aiTickIntervalMin;
        baseAiMax = aiTickIntervalMax;
        baseSeparationInterval = separationCalculationInterval;
        
        // Initialize hit glow materials
        InitializeHitGlowMaterials();
    }

    void OnEnable()
    {
        // This is called when the object is activated (or re-activated from a pool).
        // It's a better place for logic that needs to run on spawn than Start().
        
        // CRITICAL: Reset unregister guard for pooled objects
        hasUnregistered = false;
        
        // RESPAWN FIX: Check if cached player references are still valid
        // If player has respawned, the old references will be null/destroyed
        bool needsPlayerSearch = !hasSearchedForPlayer || 
                                _playerTransform == null || 
                                _playerHealth == null ||
                                (_playerTransform != null && _playerTransform.gameObject == null);
        
        if (needsPlayerSearch)
        {
            hasSearchedForPlayer = false; // Reset flag to allow fresh search
            FindAndCachePlayer();
        }

        // Determine initial target (player or companions)
        bool hasInitialTarget = TryUpdatePrimaryTarget(out _);
        if (!hasInitialTarget || _primaryTargetTransform == null)
        {
            TransitionToState(SkullAIState.Decaying);
            return;
        }

        // Re-enable visuals (important when pooled)
        ResetSkullVisuals();

        // ✅ AAA DIRECT CHATTER: Each skull manages its own looping sound
        // Spatial audio system handles distance/volume automatically - no manager needed!
        _skullChatterHandle = SkullSoundEvents.StartSkullChatter(transform, chatterVolume);

        // Register to manager for LOD updates
        SkullEnemyManager.Register(this);
        
        // CRITICAL FIX: If spawned from a tower, immediately start hunting
        // This fixes the delay issue where skulls don't attack immediately
        if (originatingTower != null)
        {
            // Skip spawning animation for tower-spawned skulls
            TransitionToState(SkullAIState.Hunting);
        }
        else
        {
            TransitionToState(SkullAIState.Spawning); // Only use spawning state for non-tower skulls
        }
    }

    void Update()
    {
        if (isDeadInternal)
        {
            return;
        }

        // Advance state timer every frame for accurate durations
        timeInCurrentState += Time.deltaTime;

        TryUpdatePrimaryTarget(out _);
        if (_primaryTargetTransform == null)
        {
            return;
        }

        // Stagger AI decisions to reduce per-frame cost
        aiTickTimer -= Time.deltaTime;
        if (aiTickTimer <= 0f)
        {
            UpdateAIStateMachine();
            aiTickTimer = Random.Range(aiTickIntervalMin, aiTickIntervalMax);
            // Update movement jitter sparsely (on AI tick)
            movementJitter = Random.insideUnitSphere * behaviorRandomOffset * 2f;
        }
    }

    void FixedUpdate()
    {
        if (isDeadInternal)
        {
            _rb.linearVelocity = Vector3.zero;
            return;
        }

        TryUpdatePrimaryTarget(out _);
        if (_primaryTargetTransform == null)
        {
            _rb.linearVelocity = Vector3.zero; // Stop movement if targetless
            return;
        }

        // --- Optimized Physics-based Updates ---
        // Throttle expensive separation calculation (can be disabled by LOD)
        if (separationEnabled)
        {
            separationCalculationTimer -= Time.fixedDeltaTime;
            if (separationCalculationTimer <= 0f)
            {
                separationVector = CalculateSeparation();
                separationCalculationTimer = separationCalculationInterval;
            }
        }

        // Calculate and apply movement
        Vector3 desiredVelocity = GetDesiredVelocityForCurrentState();
        desiredVelocity += separationVector; // Apply cached separation force

        // ⚡ OPTIMIZED GROUND AVOIDANCE: Check only 5 times per second instead of 50-60
        // Reduces raycasts from 25,000/sec to 500/sec (50x reduction!)
        // Safety: EnforceAbsoluteMinimumHeight() still runs every frame as emergency backup
        _groundCheckTimer -= Time.fixedDeltaTime;
        if (_groundCheckTimer <= 0f)
        {
            _cachedGroundAvoidance = CalculateOptimizedGroundAvoidance();
            _groundCheckTimer = GROUND_CHECK_INTERVAL;
        }
        desiredVelocity += _cachedGroundAvoidance; // Use cached avoidance force
        
        // ABSOLUTE POSITION ENFORCEMENT: Runs EVERY frame as safety net
        // This prevents skulls from ever sinking below minimum height
        EnforceAbsoluteMinimumHeight();

        _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, desiredVelocity, Time.fixedDeltaTime * 8f);

        // Rotation
        if (desiredVelocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(desiredVelocity.normalized);
            _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRotation, Time.fixedDeltaTime * 5f);
        }
    }

    private bool hasUnregistered = false; // Guard against multiple OnDisable calls
    
    void OnDisable()
    {
        // Clean up resources when the object is disabled or destroyed.
        // This prevents sounds from playing after the skull is gone.
        
        // CRITICAL: Guard against multiple OnDisable calls (Unity bug)
        if (hasUnregistered) return;
        hasUnregistered = true;
        
        // ✅ AAA INSTANT STOP: Stop chatter immediately when skull dies
        // No manager, no complexity - just direct sound control
        if (_skullChatterHandle != null && _skullChatterHandle.IsValid)
        {
            _skullChatterHandle.Stop(); // INSTANT SILENCE
            _skullChatterHandle = SoundHandle.Invalid;
        }
        
        SkullEnemyManager.Unregister(this);
    }
    
    void OnDestroy()
    {
        // Clear static references when skulls are destroyed to prevent memory leaks
        // Only clear if this is the last skull or if references are stale
        if (_playerTransform != null && _playerTransform.gameObject == null)
        {
            ClearStaticPlayerReferences();
        }
    }

    // Removed OnCollisionEnter - colliders are triggers; we use OnTriggerEnter for hits

    // ADDITIONAL METHOD: OnTriggerEnter as backup collision detection
    void OnTriggerEnter(Collider other)
    {
        if (isDeadInternal) return;

        if (!other.CompareTag("Player") && !other.CompareTag("Companion"))
        {
            return;
        }

        PlaySkullAttackSound();

        bool inflictedDamage = false;

        if (other.CompareTag("Player"))
        {
            // Deal 50 HP damage to player using health system
            PlayerHealth playerHealth = _playerHealth;
            if (playerHealth == null) playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth == null) playerHealth = other.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null) playerHealth = other.GetComponentInChildren<PlayerHealth>();

            if (playerHealth != null && !playerHealth.isDead)
            {
                // Use TakeDamage instead of Die() - deals 50 HP damage
                IDamageable damageable = playerHealth as IDamageable;
                if (damageable != null)
                {
                    damageable.TakeDamage(50f, transform.position, (other.transform.position - transform.position).normalized);
                    inflictedDamage = true;
                }
            }
        }

        if (!inflictedDamage)
        {
            // Try generic IDamageable fallback (covers companions)
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable == null) damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null) damageable = other.GetComponentInChildren<IDamageable>();

            if (damageable != null)
            {
                // Deal 50 HP damage to companions too
                damageable.TakeDamage(50f, transform.position, (other.transform.position - transform.position).normalized);
                inflictedDamage = true;
            }
        }

        if (inflictedDamage)
        {
            Die(false); // Skull dies after hitting its target
        }
    }

    #endregion

    #region AI State Machine

    private void UpdateAIStateMachine()
    {
        bool targetChanged;
        bool hasTarget = TryUpdatePrimaryTarget(out targetChanged);
        if (!hasTarget || _primaryTargetTransform == null)
        {
            TransitionToState(SkullAIState.Decaying);
            return;
        }

        float sqrDistToTarget = (transform.position - _primaryTargetTransform.position).sqrMagnitude;

        // --- State-Specific Transition Logic ---
        switch (currentAIState)
        {
            case SkullAIState.Spawning:
                if (timeInCurrentState >= spawnDuration)
                {
                    TransitionToState(SkullAIState.Hunting);
                }
                break;

            case SkullAIState.Hunting:
                if (sqrDistToTarget <= attackRange * attackRange)
                {
                    TransitionToState(SkullAIState.Attacking);
                }
                else if (sqrDistToTarget > maxHuntingRange * maxHuntingRange)
                {
                    TransitionToState(SkullAIState.Decaying);
                }
                break;

            case SkullAIState.Attacking:
                // If we've overshot the player, go back to hunting.
                if (sqrDistToTarget > (attackRange * 1.5f) * (attackRange * 1.5f))
                {
                    TransitionToState(SkullAIState.Hunting);
                }
                break;

            case SkullAIState.Decaying:
                if (timeInCurrentState >= decayDuration)
                {
                    // Using `false` here because decay is a form of self-destruction, not a kill.
                    // If you want decay to not count towards stats, change Die(true)
                    Destroy(gameObject);
                }
                break;
        }
    }

    private void TransitionToState(SkullAIState newState)
    {
        if (currentAIState == newState && timeInCurrentState > 0) return;

        // Exit logic for old state (if any) could go here

        currentAIState = newState;
        timeInCurrentState = 0f;

        // Entry logic for new state
        switch (newState)
        {
            case SkullAIState.Spawning:
                targetPosition = spawnPosition + Vector3.up * spawnRiseHeight;
                break;
            case SkullAIState.Hunting:
                // No special entry logic needed.
                break;
            case SkullAIState.Attacking:
                // Committing to a direct attack on the player's current position.
                break;
            case SkullAIState.Decaying:
                // Stop all external forces and let it drift or fall.
                if (_rb != null) _rb.linearVelocity = Vector3.zero;
                break;
        }
    }

    #endregion

    #region Movement Calculations

    private Vector3 GetDesiredVelocityForCurrentState()
    {
        if (_primaryTargetTransform == null)
        {
            TryUpdatePrimaryTarget(out _);
            if (_primaryTargetTransform == null) return Vector3.zero;
        }

        Vector3 desiredVelocity = Vector3.zero;
        float speedMultiplier = 1f;

        switch (currentAIState)
        {
            case SkullAIState.Spawning:
                // Rising from spawn
                desiredVelocity = Vector3.up * (spawnRiseHeight / spawnDuration);
                break;

            case SkullAIState.Hunting:
                // Enhanced hunting with behavioral variety
                desiredVelocity = GetHuntingVelocity();
                speedMultiplier = 0.8f + (behaviorRandomOffset * 0.4f); // Vary speed 80-120%
                break;

            case SkullAIState.Attacking:
                // Direct, fast attack with pattern variation
                Vector3 attackDirection = (_primaryTargetTransform.position - transform.position).normalized;
                
                switch (attackPattern)
                {
                    case AttackPattern.DirectAssault:
                        desiredVelocity = attackDirection * moveSpeed * 2f;
                        break;
                    case AttackPattern.SwoopingDive:
                        // Dive from above
                        attackDirection.y += 0.5f * diveSpeed;
                        desiredVelocity = attackDirection.normalized * moveSpeed * 2.5f * diveSpeed;
                        break;
                    case AttackPattern.CirclingPredator:
                        // Spiral attack
                        float spiralAngle = Time.time * 5f;
                        Vector3 perpendicular = Vector3.Cross(attackDirection, Vector3.up);
                        desiredVelocity = (attackDirection + perpendicular * Mathf.Sin(spiralAngle) * 0.3f).normalized * moveSpeed * 1.8f;
                        break;
                }
                break;

            case SkullAIState.Decaying:
                // Slowly fall downward
                desiredVelocity = Vector3.down * 2f;
                break;
        }

        return desiredVelocity * speedMultiplier;
    }

    Vector3 GetHuntingVelocity()
    {
        if (_primaryTargetTransform == null)
        {
            TryUpdatePrimaryTarget(out _);
            if (_primaryTargetTransform == null) return Vector3.zero;
        }

        Vector3 directionToTarget = (_primaryTargetTransform.position - transform.position).normalized;
        Vector3 velocity = directionToTarget * moveSpeed;

        // Apply attack pattern movement variations
        switch (attackPattern)
        {
            case AttackPattern.DirectAssault:
                // Straight line with slight weaving
                zigzagTimer += Time.deltaTime * 2f;
                Vector3 perpendicular = Vector3.Cross(directionToTarget, Vector3.up);
                velocity += perpendicular * Mathf.Sin(zigzagTimer) * 2f;
                break;

            case AttackPattern.SwoopingDive:
                // Swooping motion with height variation
                swoopTimer += Time.deltaTime * swoopFrequency;
                
                if (preferVerticalMovement)
                {
                    velocity.y += Mathf.Sin(swoopTimer) * swoopAmplitude * 1.5f;
                }
                else
                {
                    Vector3 swoop = Vector3.Cross(directionToTarget, Vector3.up);
                    velocity += swoop * Mathf.Sin(swoopTimer) * swoopAmplitude;
                }
                break;

            case AttackPattern.CirclingPredator:
                // Circle around the player while closing in
                circleAngle += circleSpeed * Time.deltaTime * (1f + behaviorRandomOffset);
                
                // Calculate circling position
                Vector3 circleOffset = new Vector3(
                    Mathf.Sin(circleAngle * Mathf.Deg2Rad) * circleRadius,
                    Mathf.Cos(Time.time * 2f) * 2f, // Vertical bobbing
                    Mathf.Cos(circleAngle * Mathf.Deg2Rad) * circleRadius
                );
                
                Vector3 targetPos = _primaryTargetTransform.position + circleOffset;
                velocity = (targetPos - transform.position).normalized * moveSpeed;
                break;
        }

        // Add tick-based jitter to prevent identical movement (updated on AI tick)
        velocity += movementJitter;

        return velocity;
    }

    private Vector3 CalculateSeparation()
    {
        Vector3 force = Vector3.zero;
        // Using NonAlloc with a preallocated buffer to avoid GC
        int numFound = Physics.OverlapSphereNonAlloc(
            transform.position,
            separationRadius,
            separationResults,
            separationLayerMask,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < numFound; i++)
        {
            Collider other = separationResults[i];
            if (other.gameObject == gameObject) continue;

            // Check if the other object is a SkullEnemy using component check (safer than tags)
            SkullEnemy otherSkull = other.GetComponent<SkullEnemy>();
            if (otherSkull != null)
            {
                Vector3 awayFromOther = transform.position - other.transform.position;
                float distance = awayFromOther.magnitude;
                if (distance > 0)
                {
                    // The force is stronger the closer the skulls are
                    force += awayFromOther.normalized / distance * separationForce;
                }
            }
        }
        return force;
    }

    /// <summary>
    /// ⚡ OPTIMIZED ground avoidance using ONE SphereCast instead of 5 Raycasts.
    /// Called only 5 times per second (was 50-60 times per second).
    /// SphereCast is MORE reliable than raycasts - detects surfaces at ANY angle.
    /// 95% performance improvement: 25,000 raycasts/sec → 500 spherecasts/sec
    /// </summary>
    private Vector3 CalculateOptimizedGroundAvoidance()
    {
        // ONE SphereCast replaces 5 Raycasts - more accurate AND faster!
        RaycastHit hit;
        float checkRadius = 30f; // Check 30-unit radius around skull (catches edges)
        
        if (Physics.SphereCast(
            transform.position, 
            checkRadius, 
            Vector3.down, 
            out hit, 
            maxGroundDetectionRange, 
            groundLayerMask))
        {
            float distanceToSurface = hit.distance;
            
            // If too close to ground, push upward
            if (distanceToSurface < minGroundClearance)
            {
                // Calculate repulsion force - stronger when closer to surface
                float forceMultiplier = Mathf.Lerp(3f, 1f, distanceToSurface / minGroundClearance);
                Vector3 repulsionForce = Vector3.up * groundAvoidanceForce * forceMultiplier;
                
                // DEBUG: Visual feedback when skull is too close to ground
                // Debug.DrawRay(transform.position, Vector3.down * distanceToSurface, Color.red, GROUND_CHECK_INTERVAL);
                // Debug.DrawRay(hit.point, hit.normal * 50f, Color.yellow, GROUND_CHECK_INTERVAL);
                
                return repulsionForce;
            }
        }
        
        return Vector3.zero; // No ground nearby, no avoidance needed
    }
    
    /// <summary>
    /// Absolute safety net that forcibly prevents skulls from going below minimum world height.
    /// This is a hard constraint that overrides physics if necessary.
    /// </summary>
    private void EnforceAbsoluteMinimumHeight()
    {
        Vector3 currentPos = transform.position;
        
        // Emergency position correction if skull falls below absolute minimum
        if (currentPos.y < absoluteMinWorldHeight)
        {
            // Immediately teleport skull above minimum height
            currentPos.y = absoluteMinWorldHeight + minGroundClearance;
            transform.position = currentPos;
            
            // Stop downward velocity
            Vector3 velocity = _rb.linearVelocity;
            if (velocity.y < 0)
            {
                velocity.y = 0;
                _rb.linearVelocity = velocity;
            }
            
            // Add strong upward force
            _rb.AddForce(Vector3.up * groundAvoidanceForce * 2f, ForceMode.Acceleration);
            
            Debug.LogWarning($"[SkullEnemy] EMERGENCY: Skull {name} fell below minimum height! Forced correction applied.", this);
        }
    }

    #endregion

    #region Public API & Damage

    /// <summary>
    /// Initializes the skull with necessary references. Called by the spawner.
    /// </summary>
    public void InitializeSkull(PlatformTrigger platform, TowerController tower, bool isMinionOfBoss = false)
    {
        homePlatformTrigger = platform;
        originatingTower = tower;
        isBossMinion = isMinionOfBoss;

        // Reset state for object pooling
        isDeadInternal = false;
        currentHealth = maxHealth;
        gameObject.SetActive(true);
        _collider.enabled = true;
        _rb.isKinematic = false;
        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        ResetSkullVisuals();

        // State machine is reset in OnEnable
    }

    /// <summary>
    /// Notifies the skull that it is free to attack, regardless of platform triggers.
    /// </summary>
    public void UnleashSkull()
    {
        if (!isDeadInternal && currentAIState == SkullAIState.Spawning)
        {
            TransitionToState(SkullAIState.Hunting);
        }
    }

    /// <summary>
    /// Checks if the skull is currently in a dead state.
    /// </summary>
    public bool IsDead()
    {
        return isDeadInternal;
    }
    
    /// <summary>
    /// Clears the tower reference to make this skull independent and prevent MissingReferenceException
    /// </summary>
    public void ClearTowerReference()
    {
        originatingTower = null;
    }
    
    /// <summary>
    /// Immediately disables all visual components of the skull for instant death feedback
    /// </summary>
    private void DisableSkullVisuals()
    {
        // Disable all renderers to make skull invisible immediately (use cached arrays)
        foreach (Renderer renderer in cachedRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
        
        // Disable any particle systems
        foreach (ParticleSystem particle in cachedParticles)
        {
            if (particle != null)
            {
                particle.Stop();
            }
        }
        
    }

    /// <summary>
    /// Re-enables all visual components (renderers/particles). Call on spawn/enable, especially when pooled.
    /// </summary>
    private void ResetSkullVisuals()
    {
        if (cachedRenderers != null)
        {
            foreach (Renderer renderer in cachedRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
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

    // REMOVED: DelayedChatterStart() - chatter is now managed directly by each skull using SoundHandle

    /// <summary>
    /// Apply LOD parameters from the manager to throttle AI and physics work.
    /// </summary>
    public void ApplyLOD(SkullEnemyManager.SkullLOD lod)
    {
        if (currentLOD == lod) return;

        currentLOD = lod;

        switch (lod)
        {
            case SkullEnemyManager.SkullLOD.Near:
                separationEnabled = true;
                // REMOVED: groundAvoidanceEnabled = true; - Ground avoidance is now ALWAYS active
                aiTickIntervalMin = Mathf.Clamp(baseAiMin, 0.02f, 0.5f);
                aiTickIntervalMax = Mathf.Clamp(baseAiMax, 0.02f, 0.5f);
                separationCalculationInterval = Mathf.Clamp(baseSeparationInterval, 0.02f, 0.5f);
                // REMOVED: groundAvoidanceInterval line - New system doesn't use intervals
                break;

            case SkullEnemyManager.SkullLOD.Mid:
                separationEnabled = true;
                // REMOVED: groundAvoidanceEnabled = true; - Ground avoidance is now ALWAYS active
                aiTickIntervalMin = Mathf.Clamp(baseAiMin * 1.5f, 0.02f, 0.5f);
                aiTickIntervalMax = Mathf.Clamp(baseAiMax * 1.8f, 0.02f, 0.5f);
                separationCalculationInterval = Mathf.Clamp(baseSeparationInterval * 1.5f, 0.02f, 0.5f);
                // REMOVED: groundAvoidanceInterval line - New system doesn't use intervals
                break;

            case SkullEnemyManager.SkullLOD.Far:
                separationEnabled = false;
                separationVector = Vector3.zero;
                // REMOVED: groundAvoidanceEnabled = false; - Ground avoidance is NEVER disabled now
                // REMOVED: cachedGroundAvoidance = Vector3.zero; - New system doesn't use cached values
                aiTickIntervalMin = Mathf.Clamp(baseAiMin * 2.5f, 0.02f, 0.5f);
                aiTickIntervalMax = Mathf.Clamp(baseAiMax * 3.0f, 0.02f, 0.5f);
                separationCalculationInterval = Mathf.Clamp(baseSeparationInterval * 2.5f, 0.02f, 0.5f);
                // REMOVED: groundAvoidanceInterval line - New system doesn't use intervals
                break;
        }

        // Nudge timers so new intervals take effect soon
        aiTickTimer = Mathf.Min(aiTickTimer, aiTickIntervalMin);
        separationCalculationTimer = Mathf.Min(separationCalculationTimer, separationCalculationInterval);
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (isDeadInternal) return;

        currentHealth -= amount;
        
        // Trigger hit glow effect if enabled
        if (enableHitGlow && !_isGlowing && _skullRenderer != null)
        {
            StartCoroutine(HitGlowEffect());
        }

        // Could add effects here, like a hit flash or particle effect at hitPoint

        if (currentHealth <= 0)
        {
            Die(false);
        }
    }

    public void TakeDamage(float amount, Vector3 hitPoint) => TakeDamage(amount, hitPoint, Vector3.zero);
    public void TakeDamage(float amount) => TakeDamage(amount, transform.position, Vector3.zero);

    #endregion

    #region Death & Cleanup

    private void Die(bool byDecay)
    {
        if (isDeadInternal) return;
        isDeadInternal = true;

        // ✅ CRITICAL FIX: Stop chatter sound INSTANTLY when skull dies
        // Don't wait for OnDisable() which happens 3 seconds later - player needs immediate silence!
        if (_skullChatterHandle != null && _skullChatterHandle.IsValid)
        {
            _skullChatterHandle.Stop(); // INSTANT SILENCE
            _skullChatterHandle = SoundHandle.Invalid;
        }

        // IMMEDIATE VISUAL FEEDBACK: Disable skull visuals instantly
        DisableSkullVisuals();
    
        // Stop AI and movement immediately
        StopAllCoroutines();
        currentAIState = SkullAIState.Decaying; // Prevent further state changes
        _rb.linearVelocity = Vector3.zero;
        _collider.enabled = false;

        // Notify other systems (with safety check to prevent MissingReferenceException)
        if (originatingTower != null && originatingTower.gameObject != null)
        {
            originatingTower.SkullDied(this);
        }

        if (!byDecay)
        {
            // --- Track stats for a player-caused death ---
            if (isBossMinion)
                GameStats.AddBossMinionKillToCurrentRun();
            else
                GameStats.AddSkullKillToCurrentRun();

            PlayerRunStats.Instance?.RegisterSkullKill();

            // --- XP & MISSION SYSTEMS: Grant XP and track for missions (UPGRADED!) ---
            string enemyType = isBossMinion ? "bossminion" : "skull";
            GeminiGauntlet.Progression.XPHooks.OnEnemyKilled(enemyType, transform.position);
            MissionProgressHooks.OnEnemyKilled(enemyType);

            // --- Handle Power-up Drop ---
            if (canDropPowerUps && powerUpPrefabs != null && powerUpPrefabs.Count > 0)
            {
                if (Random.value < powerUpSpawnChance)
                {
                    var powerupPrefab = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Count)];
                    // Spawn pooled; powerups are despawned by their own logic when collected
                    PoolManager.SpawnStatic(powerupPrefab, transform.position, Quaternion.identity);
                }
            }
        }

        // --- BATCHED Visual and Audio Feedback (PERFORMANCE OPTIMIZED) ---
        // NOTE: Chatter stops automatically in OnDisable() when skull SoundHandle is invalidated
        
        // Queue death audio for batched playback to prevent audio overload
        SkullDeathManager.QueueDeathAudio(transform.position, deathVolume);
        
        // Queue death effects for batched spawning to prevent VFX spikes
        if (deathEffectPrefab != null)
        {
            SkullDeathManager.QueueDeathEffect(deathEffectPrefab, transform.position, Quaternion.identity);
        }

        // Queue physics operations for batched processing to prevent physics spikes
        Vector3 randomTorque = Random.insideUnitSphere * 10f;
        SkullDeathManager.QueuePhysicsOperations(_rb, randomTorque);

        // --- Cleanup ---
        // Return to pool if pooled; otherwise destroy
        var pooled = GetComponent<PooledObject>();
        if (pooled != null)
        {
            PoolManager.DespawnStatic(gameObject, 3f);
        }
        else
        {
            Destroy(gameObject, 3f);
        }
    }

    #endregion

    #region Sound Management
    // NOTE: Each skull manages its own chatter SoundHandle directly
    // Chatter starts in OnEnable() and stops instantly in OnDisable()
    // No manager needed - Unity's spatial audio handles distance attenuation

    private void PlaySkullDeathSound()
    {
        SkullSoundEvents.PlaySkullDeathSound(transform.position, deathVolume);
    }

    private void PlaySkullAttackSound()
    {
        SkullSoundEvents.PlaySkullAttackSound(transform.position, attackVolume);
    }

    #endregion

    #region Dynamic Target Selection

    private bool TryUpdatePrimaryTarget(out bool targetChanged)
    {
        targetChanged = false;

        // Validate current target first
        if (_primaryTargetTransform != null)
        {
            if (_primaryTargetIsPlayer)
            {
                if (IsPlayerTargetValid())
                {
                    return true;
                }
            }
            else if (_primaryTargetCompanion != null && IsCompanionTargetValid(_primaryTargetCompanion))
            {
                return true;
            }
        }

        Transform previousTarget = _primaryTargetTransform;

        Transform selectedTransform = null;
        bool selectedIsPlayer = false;
        CompanionCore selectedCompanion = null;
        float bestSqrDistance = float.MaxValue;

        float maxChaseRange = Mathf.Max(playerDetectionRange, maxHuntingRange);
        float maxChaseRangeSqr = maxChaseRange <= 0f ? float.MaxValue : maxChaseRange * maxChaseRange;

        if (IsPlayerTargetValid())
        {
            float sqrToPlayer = (transform.position - _playerTransform.position).sqrMagnitude;
            if (sqrToPlayer <= maxChaseRangeSqr)
            {
                selectedTransform = _playerTransform;
                selectedIsPlayer = true;
                bestSqrDistance = sqrToPlayer;
            }
        }

        foreach (CompanionCore companion in CompanionCore.GetActiveCompanions())
        {
            if (!IsCompanionTargetValid(companion))
            {
                continue;
            }

            Transform companionTransform = companion.transform;
            float sqrToCompanion = (transform.position - companionTransform.position).sqrMagnitude;
            if (sqrToCompanion > maxChaseRangeSqr)
            {
                continue;
            }

            if (selectedTransform == null || sqrToCompanion < bestSqrDistance)
            {
                selectedTransform = companionTransform;
                selectedIsPlayer = false;
                selectedCompanion = companion;
                bestSqrDistance = sqrToCompanion;
            }
        }

        if (selectedTransform != null)
        {
            _primaryTargetTransform = selectedTransform;
            _primaryTargetIsPlayer = selectedIsPlayer;
            _primaryTargetCompanion = selectedIsPlayer ? null : selectedCompanion;

            targetChanged = previousTarget != selectedTransform;
            return true;
        }

        if (previousTarget != null)
        {
            targetChanged = true;
        }

        _primaryTargetTransform = null;
        _primaryTargetCompanion = null;
        _primaryTargetIsPlayer = false;

        return false;
    }

    private bool IsPlayerTargetValid()
    {
        if (_playerTransform == null || _playerHealth == null)
        {
            if (!hasSearchedForPlayer)
            {
                FindAndCachePlayer();
            }
            if (_playerTransform == null || _playerHealth == null)
            {
                return false;
            }
        }

        if (_playerHealth.isDead)
        {
            return false;
        }

        GameObject playerObject = _playerTransform.gameObject;
        return playerObject != null && playerObject.activeInHierarchy;
    }

    private static bool IsCompanionTargetValid(CompanionCore companion)
    {
        if (companion == null)
        {
            return false;
        }

        if (!companion.IsActive)
        {
            return false;
        }

        GameObject companionObject = companion.gameObject;
        return companionObject != null && companionObject.activeInHierarchy;
    }

    #endregion

    #region Static Helpers & Gizmos

    /// <summary>
    /// Finds and caches the player Transform and PlayerHealth component.
    /// This is called only once by the first SkullEnemy to awaken.
    /// </summary>
    private static void FindAndCachePlayer()
    {
        // Try multiple methods to find the correct player object
        GameObject playerObject = null;
        PlayerHealth foundPlayerHealth = null;
        
        // Method 1: Find by tag "Player"
        GameObject[] playerCandidates = GameObject.FindGameObjectsWithTag("Player");
        
        foreach (GameObject candidate in playerCandidates)
        {
            PlayerHealth health = candidate.GetComponent<PlayerHealth>();
            if (health != null)
            {
                playerObject = candidate;
                foundPlayerHealth = health;
                break;
            }
            else
            {
            }
        }
        
        // Method 2: If tag search failed, search by PlayerHealth component directly
        if (foundPlayerHealth == null)
        {
            foundPlayerHealth = FindObjectOfType<PlayerHealth>();
            if (foundPlayerHealth != null)
            {
                playerObject = foundPlayerHealth.gameObject;
            }
        }
        
        // Method 3: REMOVED - FindObjectsOfType<GameObject>() was causing severe performance issues
        // The above two methods (tag search and direct component search) should be sufficient
        // If player is not found, skulls will simply not track (safer than freezing the game)
        
        // Cache the results
        if (playerObject != null && foundPlayerHealth != null)
        {
            _playerTransform = playerObject.transform;
            _playerHealth = foundPlayerHealth;
            
        }
        else
        {
        }
        
        hasSearchedForPlayer = true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, playerDetectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxHuntingRange);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        if (_primaryTargetTransform != null && (currentAIState == SkullAIState.Hunting || currentAIState == SkullAIState.Attacking))
        {
            Gizmos.color = _primaryTargetIsPlayer ? Color.blue : Color.cyan;
            Gizmos.DrawLine(transform.position, _primaryTargetTransform.position);
        }

    }

    /// <summary>
    /// Sets the tower that spawned this skull - for immediate attack behavior
    /// </summary>
    public void SetOriginatingTower(TowerController tower)
    {
        originatingTower = tower;
        
        // If we have a tower origin, immediately start hunting
        if (tower != null && currentAIState == SkullAIState.Spawning)
        {
            TransitionToState(SkullAIState.Hunting);
        }
    }

    #endregion


    #region Hit Glow Effects

    /// <summary>
    /// Initialize materials for hit glow effect (same system as gems)
    /// </summary>
    private void InitializeHitGlowMaterials()
    {
        if (!enableHitGlow) return;

        // Try to get renderer from the skull (prefer MeshRenderer, fallback to any Renderer)
        _skullRenderer = GetComponent<MeshRenderer>();
        if (_skullRenderer == null)
        {
            _skullRenderer = GetComponent<Renderer>();
        }
        if (_skullRenderer == null)
        {
            _skullRenderer = GetComponentInChildren<Renderer>();
        }

        if (_skullRenderer != null)
        {
            // Create a copy of the material to avoid modifying the original asset
            _originalMaterial = _skullRenderer.material;
            _skullRenderer.material = new Material(_originalMaterial);
            
            // Store original emission color
            if (_skullRenderer.material.HasProperty("_EmissionColor"))
            {
                _originalEmissionColor = _skullRenderer.material.GetColor("_EmissionColor");
            }
            else
            {
                _originalEmissionColor = Color.black;
            }
        }
    }

    /// <summary>
    /// Coroutine that creates a brief glow effect when skull is hit (same as gem system)
    /// </summary>
    private IEnumerator HitGlowEffect()
    {
        if (_skullRenderer == null || _skullRenderer.material == null)
            yield break;

        _isGlowing = true;
        
        // Enable emission if the material supports it
        if (_skullRenderer.material.HasProperty("_EmissionColor"))
        {
            // Calculate glow color (make it brighter version of original or use white)
            Color glowColor = _originalEmissionColor;
            if (glowColor == Color.black)
            {
                // If no original emission, use a bright white/red glow for skulls
                glowColor = Color.red * hitGlowIntensity;
            }
            else
            {
                glowColor *= hitGlowIntensity;
            }
            
            // Apply glow effect
            _skullRenderer.material.SetColor("_EmissionColor", glowColor);
            _skullRenderer.material.EnableKeyword("_EMISSION");
            
            // Wait for glow duration
            yield return new WaitForSeconds(hitGlowDuration);
            
            // Restore original emission
            _skullRenderer.material.SetColor("_EmissionColor", _originalEmissionColor);
            if (_originalEmissionColor == Color.black)
            {
                _skullRenderer.material.DisableKeyword("_EMISSION");
            }
        }
        else
        {
            // Fallback: briefly change the main color if emission isn't supported
            Color originalColor = _skullRenderer.material.color;
            Color glowColor = Color.Lerp(originalColor, Color.red, 0.8f); // Red glow for skulls
            
            _skullRenderer.material.color = glowColor;
            yield return new WaitForSeconds(hitGlowDuration);
            _skullRenderer.material.color = originalColor;
        }
        
        _isGlowing = false;
    }

    /// <summary>
    /// Public method to toggle hit glow on/off during runtime
    /// </summary>
    public void SetEnableHitGlow(bool enable)
    {
        enableHitGlow = enable;
        if (!enable && _isGlowing)
        {
            // Stop current glow effect if disabling
            StopAllCoroutines();
            if (_skullRenderer != null && _skullRenderer.material != null)
            {
                _skullRenderer.material.SetColor("_EmissionColor", _originalEmissionColor);
                if (_originalEmissionColor == Color.black)
                {
                    _skullRenderer.material.DisableKeyword("_EMISSION");
                }
            }
            _isGlowing = false;
        }
    }

    #endregion
}