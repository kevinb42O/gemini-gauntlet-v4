// --- CompanionAI.cs (ULTRA HIGH PERFORMANCE) ---
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using GeminiGauntlet.Animation;

/// <summary>
/// LEGACY: High-performance companion AI that follows the player and engages enemies.
/// 
/// ⚠️ THIS IS THE OLD MONOLITHIC VERSION - USE CompanionAI.CompanionCore INSTEAD!
/// 
/// This script has been replaced by a modular system in the CompanionAI namespace.
/// Use CompanionAIMigrationHelper to safely migrate to the new system.
///
/// RECOMMENDED SETUP: Clone your existing player prefab and add this script to it!
/// This gives the companion:
/// - Same hands, animations, and visual effects as the player
/// - Proper emit points for shooting
/// - All existing PlayerShooterOrchestrator functionality
/// - The script automatically disables conflicting player components
///
/// PERFORMANCE OPTIMIZATIONS:
/// - Cached component references
/// - Optimized enemy detection with spatial partitioning
/// - Minimal Update() calls using coroutines
/// - Smart state transitions
/// - Distance-based LOD for expensive operations
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class CompanionAILegacy : MonoBehaviour, IDamageable
{
    #region Public Inspector Variables
    
    [Header("Player References")]
    [Tooltip("Player transform to follow")]
    public Transform playerTransform;
    
    [Header("Companion Shooting System")]
    [Tooltip("Left hand emit point for shooting")]
    public Transform leftHandEmitPoint;
    
    [Tooltip("Right hand emit point for shooting")]
    public Transform rightHandEmitPoint;
    
    [Tooltip("Shotgun particle system prefab")]
    public GameObject shotgunParticlePrefab;
    
    [Tooltip("Stream particle system prefab")]
    public GameObject streamParticlePrefab;
    
    [Header("Following Behavior")]
    [Tooltip("Distance to maintain from player when following")]
    [Range(50f, 500f)]
    public float followingDistance = 200f;
    
    [Tooltip("Minimum distance before companion starts moving toward player")]
    [Range(20f, 200f)]
    public float minFollowDistance = 100f;
    
    [Tooltip("Maximum distance before companion teleports to player (prevents getting stuck)")]
    [Range(1000f, 10000f)]
    public float maxFollowDistance = 5000f;
    
    [Header("Movement Speeds")]
    [Tooltip("Walking speed (normal following)")]
    [Range(100f, 1000f)]
    public float walkingSpeed = 400f;
    
    [Tooltip("Running speed (urgent following/combat)")]
    [Range(200f, 1500f)]
    public float runningSpeed = 750f;
    
    [Header("Combat Behavior")]
    [Tooltip("Range to detect enemies")]
    [Range(500f, 3000f)]
    public float detectionRadius = 2500f; // Much more reasonable detection range
    
    [Tooltip("Range to start attacking enemies")]
    [Range(100f, 2000f)]
    public float attackRange = 800f;
    
    [Tooltip("Distance threshold for stream vs shotgun")]
    [Range(200f, 1000f)]
    public float streamThreshold = 600f;
    
    [Tooltip("Time between shotgun attacks")]
    [Range(0.05f, 0.5f)]
    public float shotgunCooldown = 0.1f;
    
    [Tooltip("Stream damage per tick")]
    [Range(50f, 500f)]
    public float streamDamage = 200f;
    
    [Tooltip("Shotgun damage per shot")]
    [Range(100f, 1000f)]
    public float shotgunDamage = 500f;
    
    [Tooltip("Time between starting/stopping beam attacks")]
    [Range(0.5f, 3f)]
    public float beamCooldown = 1.5f;
    
    [Tooltip("How long to beam before switching to shotgun")]
    [Range(1f, 5f)]
    public float beamDuration = 2f;
    
    [Header("Dynamic Movement")]
    [Tooltip("Should companion move while shooting")]
    public bool moveWhileShooting = true;
    
    [Tooltip("Combat speed multiplier for aggressive movement - 100X ULTRA FAST!")]
    [Range(1f, 500f)]
    public float combatSpeedMultiplier = 300f; // 300X ULTRA FAST COMBAT!
    
    [Tooltip("How often to change combat position (CONSTANT MOVEMENT)")]
    [Range(0.2f, 2f)]
    public float repositionInterval = 0.5f; // CONSTANT repositioning!
    
    [Tooltip("Random jump chance (0-1)")]
    [Range(0f, 1f)]
    public float jumpChance = 1.0f; // CONSTANT JUMPING!
    
    [Tooltip("Jump force")]
    [Range(100f, 5000f)]
    public float jumpForce = 2000f; // MUCH HIGHER JUMP FORCE!
    
    [Header("Area Damage System")]
    [Tooltip("Damage radius for area attacks - HUGE RADIUS FOR EASY SKULL KILLING!")]
    [Range(50f, 500f)]
    public float damageRadius = 300f; // MASSIVE radius for easy skull elimination!
    
    [Tooltip("Should use area damage instead of single target")]
    public bool useAreaDamage = true;
    
    [Header("🎯 TARGET PREFAB ASSIGNMENTS")]
    [Tooltip("Primary skull enemy prefab to target")]
    public GameObject skullEnemyPrefab1;

    [Tooltip("Secondary skull enemy prefab to target")]
    public GameObject skullEnemyPrefab2;

    [Tooltip("Tertiary skull enemy prefab to target")]
    public GameObject skullEnemyPrefab3;

    [Header("Target Tags (Legacy Support)")]
    [Tooltip("Target tags that the companion will hunt")]
    public string[] targetTags = new string[] { "Gem", "Skull" };

    [Tooltip("EMERGENCY: Distance at which skulls become immediate threat")]
    [Range(200f, 800f)]
    public float emergencyThreatDistance = 400f;

    [Tooltip("How often to scan for immediate threats (lower = more responsive)")]
    [Range(0.02f, 0.1f)]
    public float threatScanInterval = 0.03f; // ULTRA FAST threat detection!

    [Tooltip("Should use intelligent threat prioritization")]
    public bool useIntelligentThreatSystem = true;

    [Tooltip("Tags in priority order (first = highest priority)")]
    public string[] priorityTagOrder = new string[] { "Skull", "Gem" }; // SKULLS ARE DANGEROUS!
    
    [Header("🚀 SWARM COMBAT OPTIMIZATION")]
    [Tooltip("When multiple skulls detected, use spray tactics instead of precise targeting")]
    public bool useSwarmSprayTactics = true;
    [Tooltip("Minimum skulls nearby to activate spray mode")]
    [Range(2, 10)] public int swarmThreshold = 3;
    [Tooltip("Time to keep spraying in direction during target transitions")]
    [Range(1f, 5f)] public float sprayTransitionTime = 2f;
    [Tooltip("Rotation speed multiplier for faster skull tracking")]
    [Range(2f, 10f)] public float skullTrackingSpeedMultiplier = 5f;
    
    [Header("Ground Detection")]
    [Tooltip("Layers considered as ground for movement")]
    public LayerMask groundLayers = -1;
    
    [Header("Performance Settings")]
    [Tooltip("How often to update AI logic (lower = better performance)")]
    [Range(0.02f, 0.2f)]
    public float updateInterval = 0.05f; // LIGHTNING FAST decisions!
    
    [Tooltip("How often to scan for enemies (lower = better performance)")]
    [Range(0.05f, 0.5f)]
    public float enemyScanInterval = 0.1f; // RAPID target acquisition!
    
    [Header("Audio")]
    public AudioClip shotgunSFX;
    public AudioClip streamLoopSFX;
    [Range(0f,1f)] public float shotgunVolume = 0.9f;
    [Range(0f,1f)] public float streamVolume = 0.7f;
    
    [Header("💚 Combat Visual Effects")]
    [Tooltip("Green glow effect prefab shown when following (not in combat)")]
    public GameObject followingGlowEffectPrefab;
    
    [Tooltip("Red glow effect prefab shown when in combat")]
    public GameObject combatGlowEffectPrefab;
    
    #endregion
    
    #region Private Variables
    
    // 🎯 THREAT TRACKING INTEGRATION
    private bool _isInAggressiveHuntingMode = false;
    private int _currentThreatLevel = 0;
    
    // AI State Management
    private enum CompanionState { Following, Engaging, Attacking, Dead }
    private CompanionState _currentState = CompanionState.Following;
    
    // Health and Death System
    private bool _isDead = false;
    private float _health = 1f; // One hit kill
    
    // Cached Components (Performance Optimization)
    private NavMeshAgent _navAgent;
    private Transform _transform;
    private AudioSource _audioSource; // Companion's AudioSource for all sounds
    
    // Combat Variables
    private Transform _currentTarget;
    private float _lastShotgunTime;
    private float _lastBeamToggleTime;
    private bool _isBeaming;
    
    // Independent Shooting System
    private GameObject _leftShotgunParticle;
    // 🔥💥 Dual-Wielding System Variables
    private GameObject _rightStreamEffect; // Only right hand streams now
    private ParticleSystem _rightStreamPS;
    private Coroutine _streamDamageCoroutine;
    private Coroutine _streamPositionCoroutine; // Track position update coroutine
    private Vector3 _lastPlayerPosition;
    private float _lastDistanceToPlayer;
    private AudioSource _streamAudioSource;
    
    // Performance Optimization Variables
    private Coroutine _aiUpdateCoroutine;
    private Coroutine _enemyScanCoroutine;
    
    // Dynamic Movement Variables
    private float _lastRepositionTime;
    private bool _isRepositioning;
    private float _lastJumpTime;
    private Rigidbody _rigidbody;
    private Vector3 _strafeDirection;
    private float _lastStrafeChange;
    
    // Target Locking Variables
    private Transform _lockedTarget;
    private float _targetLockTime;
    private bool _isTargetLocked;
    
    // 🚨 DYNAMIC THREAT ASSESSMENT SYSTEM
    private readonly Collider[] _threatBuffer = new Collider[15]; // For immediate threat detection
    private readonly Collider[] _enemyBuffer = new Collider[20]; // For general enemy detection
    private int _enemyCount;
    private int _threatCount;
    private Transform _immediateThreat;
    private float _lastThreatScanTime;
    private bool _inEmergencyMode;
    
    // 🚀 SWARM COMBAT OPTIMIZATION VARIABLES
    private bool _isInSwarmMode = false;
    private Vector3 _lastKnownTargetDirection = Vector3.forward;
    private float _sprayTransitionTimer = 0f;
    private int _nearbySkullCount = 0;
    private bool _isSprayTransitioning = false;
    
    // 💚 Combat Visual Effects
    private GameObject _followingGlowEffect;
    private GameObject _combatGlowEffect;
    
    // 🚀 ULTRA HIGH PERFORMANCE SKULL CACHE SYSTEM
    private SkullEnemy[] _cachedAllSkulls = null; // Cache ALL skulls once
    private float _lastSkullCacheTime = -999f;
    private const float SKULL_CACHE_DURATION = 0.5f; // Cache for 0.5 seconds
    private List<Transform> _filteredSkullsCache = new List<Transform>(); // Reusable list
    private int _lastSceneSkullCount = -1; // Track scene changes
    
    #endregion

    #region Helper Methods

    /// <summary>
    /// Get the priority level of a tag (higher number = higher priority)
    /// </summary>
    private int GetTagPriority(string tagName)
    {
        if (string.IsNullOrEmpty(tagName) || priorityTagOrder.Length == 0)
            return 0;

        for (int i = 0; i < priorityTagOrder.Length; i++)
        {
            if (priorityTagOrder[i] == tagName)
            {
                return priorityTagOrder.Length - i; // Higher index = lower priority
            }
        }

        return 0; // Default priority
    }

    /// <summary>
    /// 🚨 EMERGENCY THREAT SCANNER - Ultra-fast skull detection for survival!
    /// </summary>
    private bool ScanForImmediateThreats()
    {
        if (!useIntelligentThreatSystem) return false;

        float currentTime = Time.time;
        if (currentTime - _lastThreatScanTime < threatScanInterval) return _immediateThreat != null;

        _lastThreatScanTime = currentTime;

        // ULTRA FAST threat detection - smaller radius, higher frequency
        _threatCount = Physics.OverlapSphereNonAlloc(_transform.position, emergencyThreatDistance, _threatBuffer);

        Transform closestThreat = null;
        float closestThreatDistance = float.MaxValue;

        for (int i = 0; i < _threatCount; i++)
        {
            Collider threat = _threatBuffer[i];
            if (threat == null || threat.isTrigger) continue;

            // PRIORITY: Skulls are immediate threats!
            bool isSkull = false;
            try
            {
                isSkull = threat.CompareTag("Skull");
            }
            catch (UnityException)
            {
                isSkull = threat.GetComponent<SkullEnemy>() != null;
            }

            if (!isSkull) continue;

            // Check if skull is alive and dangerous
            IDamageable damageable = threat.GetComponent<IDamageable>();
            if (damageable == null) continue;

            float distance = Vector3.Distance(_transform.position, threat.transform.position);
            if (distance < closestThreatDistance)
            {
                closestThreatDistance = distance;
                closestThreat = threat.transform;
            }
        }

        // EMERGENCY MODE ACTIVATION
        bool wasInEmergency = _inEmergencyMode;
        _immediateThreat = closestThreat;
        _inEmergencyMode = closestThreat != null;

        if (_inEmergencyMode && !wasInEmergency)
        {
            Debug.Log($"[CompanionAI] 🚨 EMERGENCY MODE ACTIVATED! Skull threat at {closestThreatDistance:F1} units: {closestThreat.name}");
        }
        else if (!_inEmergencyMode && wasInEmergency)
        {
            Debug.Log("[CompanionAI] ✅ Emergency mode deactivated - threats neutralized!");
        }

        return _inEmergencyMode;
    }

    /// <summary>
    /// 🧠 INTELLIGENT TARGET SELECTION - Context-aware threat prioritization (ENHANCED DEBUGGING)
    /// </summary>
    private Transform SelectBestTarget()
    {
        // ⚡ PERFORMANCE: Don't scan if this is an enemy companion
        var enemyBehavior = GetComponent<CompanionAI.EnemyCompanionBehavior>();
        if (enemyBehavior != null) return null;
        
        // Debug.Log("[CompanionAI] 🎯 STARTING TARGET SELECTION PROCESS");

        // 🚨 EMERGENCY OVERRIDE: Immediate skull threats take absolute priority
        if (ScanForImmediateThreats() && _immediateThreat != null)
        {
            Debug.Log($"[CompanionAI] 🚨 EMERGENCY TARGET OVERRIDE! Switching to immediate threat: {_immediateThreat.name}");
            return _immediateThreat;
        }

        // 🎯 THREAT TRACKER PRIORITY: If in aggressive hunting mode, prioritize tracked skulls
        if (_isInAggressiveHuntingMode && EnemyThreatTracker.Instance != null)
        {
            Debug.Log("[CompanionAI] 🔍 CHECKING THREAT TRACKER FOR SKULLS...");
            SkullEnemy closestTrackedSkull = EnemyThreatTracker.Instance.GetClosestEnemy(_transform.position);
            if (closestTrackedSkull != null)
            {
                float distanceToSkull = Vector3.Distance(_transform.position, closestTrackedSkull.transform.position);
                if (distanceToSkull <= detectionRadius)
                {
                    Debug.Log($"[CompanionAI] 🎯🚨 THREAT TRACKER TARGET: {closestTrackedSkull.name} at {distanceToSkull:F1}m!");
                    return closestTrackedSkull.transform;
                }
            }
            else
            {
                Debug.Log("[CompanionAI] ❌ THREAT TRACKER: No skulls found");
            }
        }

        // 🚀 SWARM OPTIMIZATION: Use performance-optimized targeting for skull clusters
        Transform selectedTarget = SelectSwarmOptimizedTarget();
        Debug.Log($"[CompanionAI] ✅ TARGET SELECTION COMPLETE: {(selectedTarget != null ? selectedTarget.name : "NO TARGET FOUND")}");
        return selectedTarget;
    }

    /// <summary>
    /// 🎯 TARGETED ENEMY DETECTION - Only look for assigned prefabs and gems
    /// </summary>
    private Transform SelectSwarmOptimizedTarget()
    {
        // 🎯 Find only our assigned skull prefabs (much more efficient!)
        List<Transform> skulls = FindTargetSkulls();
        _nearbySkullCount = skulls.Count;

        Debug.Log($"[CompanionAI] 🎯 TARGETED SCAN: Found {skulls.Count} assigned skull instances");

        // Find the closest skull within detection range
        Transform closestSkull = null;
        float closestDistance = float.MaxValue;

        foreach (Transform skull in skulls)
        {
            if (skull == null) continue;

            float distance = Vector3.Distance(_transform.position, skull.position);
            if (distance <= detectionRadius && distance < closestDistance)
            {
                closestDistance = distance;
                closestSkull = skull;
                Debug.Log($"[CompanionAI] 💀 TARGETED SKULL FOUND: {skull.name} at {distance:F1} units");
            }
        }

        // 🎯 Also check for gems as secondary targets
        Transform closestGem = null;
        float closestGemDistance = float.MaxValue;

        // Use physics detection for gems since we don't have specific gem prefabs
        _enemyCount = Physics.OverlapSphereNonAlloc(_transform.position, detectionRadius, _enemyBuffer);

        for (int i = 0; i < _enemyCount; i++)
        {
            Collider enemy = _enemyBuffer[i];
            if (enemy == null || enemy.isTrigger) continue;

            Gem gemComp = enemy.GetComponent<Gem>();
            if (gemComp != null && IsValidGemTarget(gemComp))
            {
                float distance = Vector3.Distance(_transform.position, enemy.transform.position);
                if (distance < closestGemDistance)
                {
                    closestGemDistance = distance;
                    closestGem = enemy.transform;
                }
            }
        }

        // 🚀 SWARM MODE ACTIVATION - only for skulls
        _isInSwarmMode = useSwarmSprayTactics && _nearbySkullCount >= swarmThreshold;

        if (_isInSwarmMode)
        {
            Debug.Log($"[CompanionAI] 🚀💀 SWARM MODE ACTIVATED! {_nearbySkullCount} skulls detected - using spray tactics!");
        }

        // Return closest skull (priority) or closest gem
        if (closestSkull != null)
        {
            Debug.Log($"[CompanionAI] 🎯 SELECTED SKULL: {closestSkull.name} at {closestDistance:F1} units");
            return closestSkull;
        }
        else if (closestGem != null)
        {
            Debug.Log($"[CompanionAI] 💎 SELECTED GEM: {closestGem.name} at {closestGemDistance:F1} units");
            return closestGem;
        }

        Debug.Log($"[CompanionAI] 📊 TARGETED RESULTS: {skulls.Count} skulls found, No valid targets selected");
        return null;
    }
    
    /// <summary>
    /// 🚀 ULTRA HIGH PERFORMANCE SKULL CACHE - Call FindObjectsByType ONCE and cache results!
    /// PERFORMANCE: Reduces 30 FindObjectsByType calls/second to 2 calls/second (15x improvement!)
    /// </summary>
    private void RefreshSkullCache()
    {
        float currentTime = Time.time;
        
        // Check if cache is still fresh
        bool cacheExpired = (currentTime - _lastSkullCacheTime) > SKULL_CACHE_DURATION;
        
        if (!cacheExpired && _cachedAllSkulls != null)
        {
            // Cache is fresh - SKIP refresh entirely!
            return;
        }
        
        // 🔥 CRITICAL: Call FindObjectsByType ONLY ONCE (when cache expires)!
        _cachedAllSkulls = FindObjectsByType<SkullEnemy>(FindObjectsSortMode.None);
        _lastSkullCacheTime = currentTime;
        _lastSceneSkullCount = _cachedAllSkulls.Length;
        
        Debug.Log($"[CompanionAI] 🚀 SKULL CACHE REFRESHED: Found {_cachedAllSkulls.Length} total skulls in scene (cache valid for {SKULL_CACHE_DURATION}s)");
    }
    
    /// <summary>
    /// 🎯 TARGETED SKULL DETECTION - Filter cached skulls by assigned prefabs (ZERO FindObjectsByType calls!)
    /// </summary>
    private List<Transform> FindTargetSkulls()
    {
        // 🚀 PERFORMANCE: Refresh cache if needed (max once per 0.5 seconds)
        RefreshSkullCache();
        
        // Clear and reuse the same list (avoid GC allocations)
        _filteredSkullsCache.Clear();
        
        // If no cache, return empty list
        if (_cachedAllSkulls == null || _cachedAllSkulls.Length == 0)
        {
            return _filteredSkullsCache;
        }
        
        // 🔥 SINGLE PASS: Filter cached skulls by assigned prefabs
        foreach (SkullEnemy skull in _cachedAllSkulls)
        {
            // Skip null or inactive skulls
            if (skull == null || !skull.gameObject.activeInHierarchy)
                continue;
            
            // Check if this skull matches any of our assigned prefabs
            bool matchesPrefab1 = skullEnemyPrefab1 != null && IsInstanceOfPrefab(skull.gameObject, skullEnemyPrefab1);
            bool matchesPrefab2 = skullEnemyPrefab2 != null && IsInstanceOfPrefab(skull.gameObject, skullEnemyPrefab2);
            bool matchesPrefab3 = skullEnemyPrefab3 != null && IsInstanceOfPrefab(skull.gameObject, skullEnemyPrefab3);
            
            if (matchesPrefab1 || matchesPrefab2 || matchesPrefab3)
            {
                _filteredSkullsCache.Add(skull.transform);
            }
        }
        
        return _filteredSkullsCache;
    }

    /// <summary>
    /// 🔄 FORCE SKULL CACHE REFRESH - Call this when skulls spawn/die for instant updates
    /// PUBLIC API: External systems (spawn managers, skull death handlers) can call this
    /// </summary>
    public void InvalidateSkullCache()
    {
        _lastSkullCacheTime = -999f; // Force immediate refresh on next scan
        Debug.Log($"[CompanionAI] 🔄 Skull cache invalidated - will refresh on next scan");
    }
    
    /// <summary>
    /// Check if a GameObject is an instance of a specific prefab
    /// </summary>
    private bool IsInstanceOfPrefab(GameObject obj, GameObject prefab)
    {
        if (prefab == null || obj == null) return false;

        // Check if this object is a prefab instance by comparing components and structure
        // This is a simple check - you might need to adjust based on your prefab structure
        return obj.GetComponent<SkullEnemy>() != null &&
               obj.name.StartsWith(prefab.name.Replace("(Clone)", "").Trim());
    }

    /// <summary>
    /// Fast gem validation without expensive checks
    /// </summary>
    private bool IsValidGemTarget(Gem gemComp)
    {
        return !gemComp.IsDetached() &&
               !gemComp.IsCollected() &&
               !gemComp.IsBeingAttracted() &&
               gemComp.CurrentHealth > 0 &&
               gemComp.transform.parent != null;
    }

    /// <summary>
    /// 🎯 INTELLIGENT TARGET SCORING - Considers threat, distance, priority
    /// </summary>
    private float CalculateTargetScore(Collider enemy, string enemyTag, float distance)
    {
        float score = 0f;
        
        // Base priority from tag order
        int tagPriority = GetTagPriority(enemyTag);
        score += tagPriority * 100f;
        
        // Distance factor (closer = higher score)
        float distanceFactor = Mathf.Max(0f, 1f - (distance / detectionRadius));
        score += distanceFactor * 50f;
        
        // 💀 SPECIAL SKULL THREAT ASSESSMENT
        if (enemyTag == "Skull")
        {
            // Skulls are MAXIMUM PRIORITY - they're deadly!
            score += 200f; // Increased base priority
            
            // 🚨 EMERGENCY: If skull is very close, MASSIVE priority boost!
            if (distance < emergencyThreatDistance)
            {
                score += 1000f; // EMERGENCY PRIORITY - MUST KILL SKULLS FIRST!
            }
            
            Debug.Log($"[CompanionAI] 💀 SKULL PRIORITY BOOST: {enemy.name} gets {score} points!");
        }
        else if (enemyTag == "Gem")
        {
            // Gems are strategic targets but much lower priority than skulls
            score += 25f;
        }
        
        return score;
    }

    /// <summary>
    /// Get enemy tag with fallback to component detection - ENHANCED FOR SKULL DETECTION
    /// </summary>
    private string GetEnemyTag(Collider enemy)
    {
        // PRIORITY 1: Check for SkullEnemy component FIRST (highest priority)
        if (enemy.GetComponent<SkullEnemy>() != null)
        {
            Debug.Log($"[CompanionAI] 💀 SKULL DETECTED: {enemy.name}!");
            return "Skull";
        }
        
        // PRIORITY 2: Check for Gem component
        if (enemy.GetComponent<Gem>() != null)
        {
            return "Gem";
        }
        
        // PRIORITY 3: Fallback to tag checking
        foreach (string tag in targetTags)
        {
            try
            {
                if (enemy.CompareTag(tag)) return tag;
            }
            catch (UnityException)
            {
                // Tag doesn't exist, already handled by component checks above
            }
        }
        
        return null;
    }

    /// <summary>
    /// Normalize tag names to avoid undefined tag errors (maps "Skulls" -> "Skull")
    /// </summary>
    private static string NormalizeTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return tag;
        return tag == "Skulls" ? "Skull" : tag;
    }
    
    #endregion

    #region Unity Lifecycle
    
    void Awake()
    {
        // Cache components for performance
        _transform = transform;
        _navAgent = GetComponent<NavMeshAgent>();
        _audioSource = GetComponent<AudioSource>(); // Cache companion's AudioSource
        _rigidbody = GetComponent<Rigidbody>();

        // 🔊 AUDIO SETUP DEBUGGING
        if (_audioSource != null)
        {
            Debug.Log($"[CompanionAI] ✅ AudioSource found! Name: {_audioSource.name}, Volume: {_audioSource.volume}, Mute: {_audioSource.mute}");
            // Configure AudioSource for 3D audio
            _audioSource.spatialBlend = 1f; // Full 3D audio
            _audioSource.minDistance = 1f; // Minimum distance for volume
            _audioSource.maxDistance = 500f; // Maximum distance for volume
        }
        else
        {
            Debug.LogError("[CompanionAI] ❌ NO AUDIOSOURCE FOUND! Please add an AudioSource component to the companion GameObject!", this);
        }
        
        // Configure NavMeshAgent for LIGHTNING FAST companion behavior
        _navAgent.stoppingDistance = followingDistance * 0.8f;
        _navAgent.acceleration = 500f; // ULTRA FAST acceleration for 300x speed!
        _navAgent.angularSpeed = 2000f; // INSTANT turning for 300x speed!
        _navAgent.speed = walkingSpeed; // Start with walking speed
        
        // Keep companion upright at all times
        if (GetComponent<Rigidbody>() != null)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true; // Prevent falling over
            rb.useGravity = true; // But still affected by gravity for ground contact
        }
    }
    
    void Start()
    {
        // Auto-find player if not assigned
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                Debug.Log("[CompanionAI] Auto-found player: " + player.name);
            }
        }
        
        // Validate essential components
        if (playerTransform == null)
        {
            playerTransform = FindFirstObjectByType<PlayerInputHandler>()?.transform;
            if (playerTransform == null)
            {
                Debug.LogError("[CompanionAI] No player found! Please assign playerTransform in inspector.", this);
                enabled = false;
                return;
            }
        }
        
        // 🚨 CRITICAL SAFETY CHECK: Ensure companion emit points are NOT player emit points!
        if (leftHandEmitPoint != null && leftHandEmitPoint.name.ToLower().Contains("player"))
        {
            Debug.LogError("[CompanionAI] ❌ LEFT HAND EMIT POINT IS PLAYER'S! This will break player animations!", this);
            leftHandEmitPoint = null;
        }
        
        if (rightHandEmitPoint != null && rightHandEmitPoint.name.ToLower().Contains("player"))
        {
            Debug.LogError("[CompanionAI] ❌ RIGHT HAND EMIT POINT IS PLAYER'S! This will break player animations!", this);
            rightHandEmitPoint = null;
        }
        
        Debug.Log($"[CompanionAI] ✅ EMIT POINTS VERIFIED: Left={leftHandEmitPoint?.name}, Right={rightHandEmitPoint?.name}");
        if (leftHandEmitPoint == null || rightHandEmitPoint == null)
        {
            Debug.LogError("[CompanionAI] Hand emit points not assigned! Please assign them in the inspector.", this);
            enabled = false;
            return;
        }
        
        // Initialize independent shooting system
        InitializeShootingSystem();
        
        // Validate particle prefabs
        if (shotgunParticlePrefab == null || streamParticlePrefab == null)
        {
            Debug.LogError("[CompanionAI] Particle prefabs not assigned! Please assign them in the inspector.", this);
            enabled = false;
            return;
        }
        
        // ⚡ CRITICAL PERFORMANCE FIX: Only start AI if this is a FRIENDLY companion (not an enemy)
        // Enemy companions use EnemyCompanionBehavior instead and don't need this expensive scanning
        // Check if this companion has EnemyCompanionBehavior - if so, disable this script entirely
        var enemyBehavior = GetComponent<CompanionAI.EnemyCompanionBehavior>();
        if (enemyBehavior != null)
        {
            Debug.LogWarning($"[CompanionAI] {gameObject.name} has EnemyCompanionBehavior - disabling CompanionAILegacy to prevent duplicate AI!", this);
            enabled = false;
            return;
        }
        
        // Start AI coroutines for performance optimization
        _aiUpdateCoroutine = StartCoroutine(AIUpdateLoop());
        _enemyScanCoroutine = StartCoroutine(EnemyScanLoop());
        
        // 🔊 VALIDATE AUDIO CLIPS
        if (shotgunSFX == null)
        {
            Debug.LogWarning("[CompanionAI] ⚠️ Shotgun SFX not assigned! Please assign a shotgun sound in the inspector.", this);
        }
        else
        {
            Debug.Log($"[CompanionAI] ✅ Shotgun SFX assigned: {shotgunSFX.name}");
        }

        if (streamLoopSFX == null)
        {
            Debug.LogWarning("[CompanionAI] ⚠️ Stream Loop SFX not assigned! Please assign a stream sound in the inspector.", this);
        }
        else
        {
            Debug.Log($"[CompanionAI] ✅ Stream Loop SFX assigned: {streamLoopSFX.name}");
        }

        // 💚 Initialize combat visual effects
        InitializeCombatVisualEffects();

        // 🔗 Subscribe to threat tracker events for visual feedback
        SubscribeToThreatTrackerEvents();

        // 🎭 Subscribe to player emote events for mirroring
        SubscribeToPlayerEmoteEvents();
    }
    
    void OnDisable()
    {
        // Clean up coroutines
        if (_aiUpdateCoroutine != null)
        {
            StopCoroutine(_aiUpdateCoroutine);
            _aiUpdateCoroutine = null;
        }
        
        if (_enemyScanCoroutine != null)
        {
            StopCoroutine(_enemyScanCoroutine);
            _enemyScanCoroutine = null;
        }
        
        if (_streamDamageCoroutine != null)
        {
            StopCoroutine(_streamDamageCoroutine);
            _streamDamageCoroutine = null;
        }
        
        if (_streamPositionCoroutine != null)
        {
            StopCoroutine(_streamPositionCoroutine);
            _streamPositionCoroutine = null;
        }
        
        // Stop any active shooting
        StopShooting();
        
        // Ensure companion's AudioSource is stopped on disable
        if (_audioSource != null && _audioSource.isPlaying)
        {
            _audioSource.Stop();
            _audioSource.clip = null; // Clear the clip
        }

        // 💚 Clean up visual effects
        CleanupVisualEffects();

        // Unsubscribe from emote events to prevent memory leaks
        HandAnimationController.OnPlayerEmote -= HandlePlayerEmote;

        // 🔗 Unsubscribe from threat tracker events
        if (EnemyThreatTracker.Instance != null)
        {
            EnemyThreatTracker.Instance.OnCombatModeActivated -= HandleCombatModeActivated;
            EnemyThreatTracker.Instance.OnCombatModeDeactivated -= HandleCombatModeDeactivated;
        }
    }

    
    #endregion
    
    #region Independent Shooting System
    
    /// <summary>
    /// 🔥💥 Initialize dual-wielding system - Clean and simple!
    /// </summary>
    private void InitializeShootingSystem()
    {
        // Just log - no pre-initialization needed for dual wielding
        Debug.Log("[CompanionAI] 🔥💥 Dual-wielding system ready! Left=Shotgun, Right=Stream");
    }
    
    /// <summary>
    /// 🔧 IMPROVED TARGETING: Fix aiming calculation and ensure damage is applied
    /// </summary>
    private void FireLeftHandShotgun()
    {
        if (leftHandEmitPoint != null && shotgunParticlePrefab != null && _currentTarget != null)
        {
            // 🔧 FIX: Better aiming calculation - aim at the center of the target, not just position
            Vector3 targetPosition = _currentTarget.position;
            Collider targetCollider = _currentTarget.GetComponent<Collider>();

            // If target has a collider, aim at the center of it for better accuracy
            if (targetCollider != null)
            {
                targetPosition = targetCollider.bounds.center;
                Debug.Log($"[CompanionAI] 🔧 IMPROVED AIMING: Aiming at collider center of {_currentTarget.name} at {targetPosition}");
            }

            Vector3 directionToTarget = (targetPosition - leftHandEmitPoint.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

            // Spawn shotgun effect
            GameObject shotgunEffect = Instantiate(shotgunParticlePrefab, leftHandEmitPoint.position, targetRotation);

            // Just emit - don't modify the particle system settings!
            ParticleSystem ps = shotgunEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                Debug.Log("[CompanionAI] 💥 LEFT HAND SHOTGUN FIRED! (COMPANION ONLY - NO PLAYER INTERFERENCE)");
            }

            // 🔊 Play shotgun SFX using companion's AudioSource
            if (shotgunSFX != null && _audioSource != null)
            {
                Debug.Log($"[CompanionAI] 🔊🔫 PLAYING SHOTGUN SOUND! Clip: {shotgunSFX.name}, Volume: {shotgunVolume}, AudioSource: {_audioSource.name}");
                _audioSource.PlayOneShot(shotgunSFX, shotgunVolume);
            }
            else
            {
                Debug.LogWarning($"[CompanionAI] ❌ CANNOT PLAY SHOTGUN SOUND! SFX: {(shotgunSFX != null ? shotgunSFX.name : "NULL")}, AudioSource: {(_audioSource != null ? _audioSource.name : "NULL")}");
            }

            Destroy(shotgunEffect, 3f);
        }
        else
        {
            Debug.LogWarning("[CompanionAI] ❌ CANNOT FIRE SHOTGUN: Missing emit point, prefab, or target!");
        }
    }

    /// <summary>
    /// 🔧 IMPROVED STREAM TARGETING: Better aiming calculation for stream attacks
    /// </summary>
    private void StartRightHandStream()
    {
        if (rightHandEmitPoint != null && streamParticlePrefab != null && _currentTarget != null)
        {
            // Stop any existing stream FIRST
            StopRightHandStream();

            // 🔧 FIX: Better aiming calculation - aim at the center of the target
            Vector3 targetPosition = _currentTarget.position;
            Collider targetCollider = _currentTarget.GetComponent<Collider>();

            // If target has a collider, aim at the center of it for better accuracy
            if (targetCollider != null)
            {
                targetPosition = targetCollider.bounds.center;
                Debug.Log($"[CompanionAI] 🔧 IMPROVED STREAM AIMING: Aiming at collider center of {_currentTarget.name} at {targetPosition}");
            }

            Vector3 directionToTarget = (targetPosition - rightHandEmitPoint.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

            // Spawn stream effect AT emit point
            _rightStreamEffect = Instantiate(streamParticlePrefab, rightHandEmitPoint.position, targetRotation);

            // 🚨 CRITICAL: Parent to emit point so it follows the companion!
            _rightStreamEffect.transform.SetParent(rightHandEmitPoint);
            _rightStreamEffect.transform.localPosition = Vector3.zero; // Reset to emit point exactly

            _rightStreamPS = _rightStreamEffect.GetComponent<ParticleSystem>();

            if (_rightStreamPS != null)
            {
                _rightStreamPS.Play();
                Debug.Log("[CompanionAI] 🔥 RIGHT HAND STREAM STARTED! (COMPANION ONLY - NO PLAYER INTERFERENCE)");
            }

            // 🔊 Start looping stream audio using companion's AudioSource
            if (streamLoopSFX != null && _audioSource != null)
            {
                Debug.Log($"[CompanionAI] 🔊🔥 PLAYING STREAM SOUND! Clip: {streamLoopSFX.name}, Volume: {streamVolume}, AudioSource: {_audioSource.name}");
                if (_streamAudioSource != null)
                {
                    _streamAudioSource.Stop();
                    _streamAudioSource = null;
                }

                // Use companion's AudioSource for stream sound
                _audioSource.clip = streamLoopSFX;
                _audioSource.loop = true;
                _audioSource.spatialBlend = 1f; // 3D audio
                _audioSource.volume = streamVolume;
                _audioSource.Play();
                Debug.Log($"[CompanionAI] 🔊🔥 STREAM AUDIO STARTED! IsPlaying: {_audioSource.isPlaying}, Loop: {_audioSource.loop}");
            }
            else
            {
                Debug.LogWarning($"[CompanionAI] ❌ CANNOT PLAY STREAM SOUND! SFX: {(streamLoopSFX != null ? streamLoopSFX.name : "NULL")}, AudioSource: {(_audioSource != null ? _audioSource.name : "NULL")}");
            }

            // Start continuous damage AND position updates
            if (_streamDamageCoroutine != null)
            {
                StopCoroutine(_streamDamageCoroutine);
            }
            _streamDamageCoroutine = StartCoroutine(ContinuousStreamDamage());
            _isBeaming = true;

            // Start stream position update coroutine
            if (_streamPositionCoroutine != null)
            {
                StopCoroutine(_streamPositionCoroutine);
            }
            _streamPositionCoroutine = StartCoroutine(UpdateStreamPosition());
        }
        else
        {
            Debug.LogWarning("[CompanionAI] ❌ CANNOT START STREAM: Missing emit point, prefab, or target!");
        }
    }

    /// <summary>
    /// Stop right hand stream with COMPLETE cleanup
    /// </summary>
    private void StopRightHandStream()
    {
        _isBeaming = false; // Stop immediately to halt all coroutines
        
        // Stop particle system first
        if (_rightStreamPS != null)
        {
            _rightStreamPS.Stop();
            _rightStreamPS.Clear(); // Clear existing particles immediately
        }
        
        // Stop stream audio using companion's AudioSource
        if (_audioSource != null && _audioSource.isPlaying)
        {
            _audioSource.Stop();
            _audioSource.clip = null; // Clear the clip
        }
        
        // Destroy the stream effect GameObject with immediate cleanup
        if (_rightStreamEffect != null)
        {
            // Unparent first to prevent any issues
            _rightStreamEffect.transform.SetParent(null);
            
            // Immediate destruction for clean cleanup
            DestroyImmediate(_rightStreamEffect);
            _rightStreamEffect = null;
            _rightStreamPS = null;
        }
        
        // Stop damage coroutine
        if (_streamDamageCoroutine != null)
        {
            StopCoroutine(_streamDamageCoroutine);
            _streamDamageCoroutine = null;
        }
        
        // Stop position update coroutine
        if (_streamPositionCoroutine != null)
        {
            StopCoroutine(_streamPositionCoroutine);
            _streamPositionCoroutine = null;
        }
        
        Debug.Log("[CompanionAI] 🔥 RIGHT HAND STREAM COMPLETELY STOPPED AND CLEANED!");
    }

    /// <summary>
    /// 💥 Deal shotgun damage - separate from visual effects (ENHANCED DEBUGGING)
    /// </summary>
    private void DealShotgunDamage()
    {
        if (_currentTarget == null)
        {
            Debug.LogWarning("[CompanionAI] ❌ NO CURRENT TARGET for shotgun damage!");
            return;
        }

        Debug.Log($"[CompanionAI] 🔫 DEALING SHOTGUN DAMAGE to {_currentTarget.name} (Damage: {shotgunDamage})");

        if (useAreaDamage)
        {
            // AREA DAMAGE - hit multiple enemies at once!
            Collider[] hitEnemies = Physics.OverlapSphere(_currentTarget.position, damageRadius);
            int enemiesHit = 0;
            int validTargetsFound = 0;

            Debug.Log($"[CompanionAI] 💥 AREA DAMAGE SCAN: Found {hitEnemies.Length} colliders in radius {damageRadius}");

            foreach (Collider enemy in hitEnemies)
            {
                if (enemy == null) continue;

                IDamageable damageable = enemy.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    validTargetsFound++;

                    // Check if enemy has any of our target tags
                    bool isValidTarget = false;
                    foreach (string tag in targetTags)
                    {
                        string compareTag = NormalizeTag(tag);
                        try
                        {
                            if (enemy.CompareTag(compareTag))
                            {
                                isValidTarget = true;
                                Debug.Log($"[CompanionAI] ✅ TARGET TAG MATCH: {enemy.name} has tag {compareTag}");
                                break;
                            }
                        }
                        catch (UnityException)
                        {
                            // Tag doesn't exist, try component check
                            if (compareTag == "Gem" && enemy.GetComponent<Gem>() != null)
                            {
                                isValidTarget = true;
                                Debug.Log($"[CompanionAI] ✅ GEM COMPONENT FOUND: {enemy.name}");
                                break;
                            }
                            else if (compareTag == "Skull" && enemy.GetComponent<SkullEnemy>() != null)
                            {
                                isValidTarget = true;
                                Debug.Log($"[CompanionAI] ✅ SKULL COMPONENT FOUND: {enemy.name}");
                                break;
                            }
                        }
                    }

                    if (isValidTarget)
                    {
                        // Final safety: never damage detached/attracted/collected/parentless/zero-health gems
                        Gem gemComp = enemy.GetComponent<Gem>();
                        if (gemComp != null)
                        {
                            if (gemComp.IsDetached() || gemComp.IsCollected() || gemComp.IsBeingAttracted() || gemComp.CurrentHealth <= 0 || gemComp.transform.parent == null)
                            {
                                Debug.Log($"[CompanionAI] ❌ SKIPPING INVALID GEM: {enemy.name} (Detached: {gemComp.IsDetached()}, Collected: {gemComp.IsCollected()}, Attracted: {gemComp.IsBeingAttracted()}, Health: {gemComp.CurrentHealth}, Parent: {gemComp.transform.parent?.name})");
                                continue;
                            }
                        }

                        Vector3 hitPoint = enemy.transform.position;
                        Vector3 hitDirection = (enemy.transform.position - _transform.position).normalized;
                        damageable.TakeDamage(shotgunDamage, hitPoint, hitDirection);
                        enemiesHit++;

                        // Enhanced debug for skull enemies
                        SkullEnemy skullComp = enemy.GetComponent<SkullEnemy>();
                        if (skullComp != null)
                        {
                            Debug.Log($"[CompanionAI] 💀💥 SHOTGUN HIT SKULL: {enemy.name} - Applied {shotgunDamage} damage!");
                        }
                        else
                        {
                            Debug.Log($"[CompanionAI] 💥 SHOTGUN HIT GEM: {enemy.name} - Applied {shotgunDamage} damage!");
                        }
                    }
                    else
                    {
                        Debug.Log($"[CompanionAI] ❌ INVALID TARGET: {enemy.name} - No matching tag or component");
                    }
                }
                else
                {
                    Debug.Log($"[CompanionAI] ❌ NO DAMAGEABLE: {enemy.name} - Missing IDamageable component");
                }
            }

            Debug.Log($"[CompanionAI] 🎯 AREA DAMAGE RESULTS: Found {validTargetsFound} valid targets, hit {enemiesHit} enemies");
        }
        else
        {
            // Single target damage - ENHANCED FOR SKULLENEMY
            IDamageable damageable = _currentTarget.GetComponent<IDamageable>();
            SkullEnemy skullEnemy = _currentTarget.GetComponent<SkullEnemy>();

            if (damageable != null)
            {
                Vector3 hitPoint = _currentTarget.position;
                Vector3 hitDirection = (_currentTarget.position - _transform.position).normalized;
                damageable.TakeDamage(shotgunDamage, hitPoint, hitDirection);

                if (skullEnemy != null)
                {
                    Debug.Log($"[CompanionAI] 💀💥 SINGLE TARGET SHOTGUN HIT SKULL! Dealt {shotgunDamage} damage to {_currentTarget.name}");
                }
                else
                {
                    Debug.Log($"[CompanionAI] 💥 SINGLE TARGET SHOTGUN! Dealt {shotgunDamage} damage to {_currentTarget.name}");
                }
            }
            else
            {
                Debug.LogWarning($"[CompanionAI] ❌ SINGLE TARGET {_currentTarget.name} has no IDamageable component!");
            }
        }
    }

    /// <summary>
    /// 🔥💀 AREA STREAM DAMAGE - Hits ALL enemies within massive radius! (SKULL KILLING MACHINE)
    /// </summary>
    private IEnumerator ContinuousStreamDamage()
    {
        Debug.Log($"[CompanionAI] 🔄 STARTING MASSIVE AREA STREAM DAMAGE! Radius: {damageRadius} units");

        while (_isBeaming)
        {
            // 🚨 AREA DAMAGE: Find ALL enemies within massive radius!
            Collider[] enemiesInRange = Physics.OverlapSphere(_transform.position, damageRadius);
            int skullsHit = 0;
            int gemsHit = 0;

            Debug.Log($"[CompanionAI] 💥🔥 AREA STREAM DAMAGE: Found {enemiesInRange.Length} colliders in {damageRadius} radius");

            foreach (Collider enemy in enemiesInRange)
            {
                if (enemy == null || enemy.isTrigger) continue;

                // Check if enemy has damageable component
                IDamageable damageable = enemy.GetComponent<IDamageable>();
                if (damageable == null) continue;

                // Check if it's a skull or gem
                SkullEnemy skullComp = enemy.GetComponent<SkullEnemy>();
                Gem gemComp = enemy.GetComponent<Gem>();

                // Only damage skulls and valid gems
                if (skullComp != null || (gemComp != null && IsValidGemTarget(gemComp)))
                {
                    Vector3 hitPoint = enemy.transform.position;
                    Vector3 hitDirection = (enemy.transform.position - _transform.position).normalized;
                    damageable.TakeDamage(streamDamage, hitPoint, hitDirection);

                    if (skullComp != null)
                    {
                        skullsHit++;
                        Debug.Log($"[CompanionAI] 💀🔥 STREAM HIT SKULL: {enemy.name} - Applied {streamDamage} area damage!");
                    }
                    else
                    {
                        gemsHit++;
                        Debug.Log($"[CompanionAI] 💎🔥 STREAM HIT GEM: {enemy.name} - Applied {streamDamage} area damage!");
                    }
                }
            }

            if (skullsHit > 0 || gemsHit > 0)
            {
                Debug.Log($"[CompanionAI] 🎯💥 MASSIVE AREA STREAM RESULTS: Hit {skullsHit} skulls + {gemsHit} gems!");
            }

            yield return new WaitForSeconds(0.05f); // ULTRA FAST area damage every 0.05 seconds!
        }

        Debug.Log("[CompanionAI] 🛑 MASSIVE AREA STREAM DAMAGE ENDED");
        _streamDamageCoroutine = null;
    }

    /// <summary>
    /// 🎯 Continuously update stream position and rotation to follow target - SWARM OPTIMIZED
    /// </summary>
    private IEnumerator UpdateStreamPosition()
    {
        while (_isBeaming && _rightStreamEffect != null)
        {
            Vector3 targetDirection;
            
            // 🚀 SWARM MODE: Use spray transition logic for smooth targeting
            if (_isInSwarmMode && _isSprayTransitioning && _currentTarget == null)
            {
                // Keep spraying in last known direction during target transitions
                targetDirection = _lastKnownTargetDirection;
                Debug.Log("[CompanionAI] 🚀🔥 SPRAY TRANSITION: Maintaining beam direction!");
            }
            else if (_currentTarget != null && rightHandEmitPoint != null)
            {
                // Normal target tracking
                targetDirection = (_currentTarget.position - rightHandEmitPoint.position).normalized;
                _lastKnownTargetDirection = targetDirection; // Update for future spray transitions
            }
            else
            {
                // Fallback: keep last known direction
                targetDirection = _lastKnownTargetDirection;
            }
            
            if (rightHandEmitPoint != null)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                
                // 🚀 FASTER rotation in swarm mode for better tracking
                float rotationSpeed = _isInSwarmMode ? 15f : 10f;
                
                _rightStreamEffect.transform.rotation = Quaternion.Slerp(
                    _rightStreamEffect.transform.rotation, 
                    targetRotation, 
                    Time.deltaTime * rotationSpeed
                );
                
                // Ensure position stays at emit point
                _rightStreamEffect.transform.position = rightHandEmitPoint.position;
            }
            
            yield return new WaitForSeconds(0.02f); // Update 50 times per second for smooth tracking
        }
    }
    
    #endregion
    
    #region AI Core Logic (Performance Optimized)
    
    /// <summary>
    /// Main AI update loop running at configurable intervals for performance
    /// </summary>
    private IEnumerator AIUpdateLoop()
    {
        while (enabled && playerTransform != null)
        {
            try
            {
                UpdateAILogic();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CompanionAI] Error in AI update: {e.Message}", this);
            }
            
            yield return new WaitForSeconds(updateInterval);
        }
    }
    
    /// <summary>
    /// Separate enemy scanning loop for performance optimization
    /// </summary>
    private IEnumerator EnemyScanLoop()
    {
        while (enabled && playerTransform != null)
        {
            try
            {
                ScanForEnemies();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CompanionAI] Error in enemy scan: {e.Message}", this);
            }
            
            yield return new WaitForSeconds(enemyScanInterval);
        }
    }
    
    /// <summary>
    /// Main AI logic with state machine
    /// </summary>
    private void UpdateAILogic()
    {
        if (playerTransform == null || _isDead) return;
        
        // Cache frequently used values
        Vector3 playerPos = playerTransform.position;
        float distanceToPlayer = Vector3.Distance(_transform.position, playerPos);
        _lastDistanceToPlayer = distanceToPlayer;
        _lastPlayerPosition = playerPos;
        
        // Only teleport if EXTREMELY far away (much higher threshold)
        if (distanceToPlayer > maxFollowDistance * 2f) // Double the threshold
        {
            TeleportToPlayer();
            return;
        }
        
        // State machine logic
        switch (_currentState)
        {
            case CompanionState.Following:
                HandleFollowingState(distanceToPlayer);
                break;
                
            case CompanionState.Engaging:
                HandleEngagingState();
                break;
                
            case CompanionState.Attacking:
                HandleAttackingState();
                break;
                
            case CompanionState.Dead:
                // Do nothing when dead
                break;
        }
    }
    
    #endregion
    
    #region State Handlers
    
    /// <summary>
    /// Handle following the player (smooth following)
    /// </summary>
    private void HandleFollowingState(float distanceToPlayer)
    {
        // PRIORITY: Check if we found ANY target to engage - COMBAT FIRST!
        if (_currentTarget != null && IsTargetValid(_currentTarget))
        {
            Debug.Log($"[CompanionAI] 🎯 TARGET ACQUIRED! Switching from Following to Engaging: {_currentTarget.name}");
            TransitionToState(CompanionState.Engaging);
            return;
        }
        
        // INTELLIGENT SPEED CONTROL - smooth following behavior
        if (distanceToPlayer > followingDistance * 2f) // Far away - RUN!
        {
            _navAgent.speed = runningSpeed; // SPRINT to catch up!
            MoveTowardsPlayer();
            Debug.Log($"[CompanionAI] 🏃‍♂️ SPRINTING to catch up! Speed: {runningSpeed}");
        }
        else if (distanceToPlayer > followingDistance * 1.2f) // Medium distance - fast walk
        {
            _navAgent.speed = walkingSpeed; // Normal walking speed
            MoveTowardsPlayer();
        }
        else if (distanceToPlayer < minFollowDistance)
        {
            // Move away from player if too close
            _navAgent.speed = walkingSpeed * 0.5f; // Slow backing away
            Vector3 awayFromPlayer = (_transform.position - playerTransform.position).normalized;
            Vector3 moveAwayPosition = _transform.position + awayFromPlayer * followingDistance;
            
            if (IsPositionOnGround(moveAwayPosition))
            {
                _navAgent.SetDestination(moveAwayPosition);
            }
        }
        else
        {
            // In good range, maintain position with slow movement
            _navAgent.speed = walkingSpeed * 0.3f; // Very slow when in good position
        }
    }
    
    /// <summary>
    /// Handle moving towards an enemy
    /// </summary>
    private void HandleEngagingState()
    {
        // Check if target is still valid
        if (!IsTargetValid(_currentTarget))
        {
            _currentTarget = null;
            TransitionToState(CompanionState.Following);
            return;
        }
        
        float distanceToTarget = Vector3.Distance(_transform.position, _currentTarget.position);
        
        // Check if close enough to attack
        if (distanceToTarget <= attackRange)
        {
            Debug.Log($"[CompanionAI] 🔥 ENTERING ATTACK MODE! Target: {_currentTarget.name} at distance {distanceToTarget:F1}");
            TransitionToState(CompanionState.Attacking);
            return;
        }
        
        // Move towards target with ULTRA COMBAT SPEED
        _navAgent.speed = runningSpeed * combatSpeedMultiplier; // 3X FASTER COMBAT!
        _navAgent.SetDestination(_currentTarget.position);
        Debug.Log($"[CompanionAI] ⚡ ULTRA FAST ENGAGEMENT! Moving to {_currentTarget.name} at speed {_navAgent.speed:F0}!");
        
        // Only abandon target if player is EXTREMELY far away (much higher threshold for combat)
        if (_lastDistanceToPlayer > maxFollowDistance * 1.5f) // Much higher threshold during combat
        {
            Debug.Log($"[CompanionAI] ⚠️ Player too far during combat! Abandoning target to follow player.");
            _currentTarget = null;
            TransitionToState(CompanionState.Following);
        }
    }
    
    /// <summary>
    /// Handle attacking the current target
    /// </summary>
    private void HandleAttackingState()
    {
        // Check if target is still valid
        if (!IsTargetValid(_currentTarget))
        {
            StopShooting();
            _currentTarget = null;
            TransitionToState(CompanionState.Following);
            return;
        }
        
        float distanceToTarget = Vector3.Distance(_transform.position, _currentTarget.position);
        
        // Check if target moved out of attack range
        if (distanceToTarget > attackRange * 1.2f) // Small buffer to prevent oscillation
        {
            StopShooting();
            TransitionToState(CompanionState.Engaging);
            return;
        }
        
        // Face the target
        FaceTarget(_currentTarget);
        
        // Handle shooting logic - RELENTLESS ASSAULT!
        Debug.Log($"[CompanionAI] 🔥 ATTACKING {_currentTarget.name} at distance {distanceToTarget:F1}!");
        HandleShooting();
        
        // Only abandon combat if player is EXTREMELY far away
        if (_lastDistanceToPlayer > maxFollowDistance * 2f) // Much higher threshold during combat
        {
            Debug.Log($"[CompanionAI] ⚠️ Player extremely far during combat! Abandoning target to follow player.");
            StopShooting();
            _currentTarget = null;
            TransitionToState(CompanionState.Following);
        }
    }
    
    #endregion
    
    #region Combat Logic
    
    /// <summary>
    /// 🔥💥 DUAL-WIELDING COMBAT SYSTEM - Left shotgun, Right stream!
    /// </summary>
    private void HandleShooting()
    {
        if (_currentTarget == null) 
        {
            // EMERGENCY RESET if we have no target
            _isTargetLocked = false;
            _lockedTarget = null;
            StopRightHandStream();
            _isSprayTransitioning = false;
            return;
        }
        
        // 🚀 SWARM MODE: Simplified target locking for performance
        if (_isInSwarmMode)
        {
            HandleSwarmCombat();
        }
        else
        {
            HandlePrecisionCombat();
        }
    }
    
    /// <summary>
    /// 🚀💀 SWARM COMBAT MODE - Spray and pray tactics for maximum skull elimination
    /// </summary>
    private void HandleSwarmCombat()
    {
        // 🚀 PERFORMANCE: Minimal target validation in swarm mode
        if (_currentTarget == null || !_currentTarget.gameObject.activeInHierarchy)
        {
            _currentTarget = null;
            return;
        }
        
        // Store target direction for spray transitions
        _lastKnownTargetDirection = (_currentTarget.position - _transform.position).normalized;
        
        // 🚀 ULTRA FAST turning for swarm combat
        FaceTargetFast(_currentTarget, skullTrackingSpeedMultiplier);
        
        // Handle spray transition timing
        if (_isSprayTransitioning)
        {
            _sprayTransitionTimer -= Time.deltaTime;
            if (_sprayTransitionTimer <= 0f)
            {
                _isSprayTransitioning = false;
            }
        }
        
        float currentTime = Time.time;
        float distanceToTarget = Vector3.Distance(_transform.position, _currentTarget.position);
        
        // 🚀 SWARM TACTIC: Prioritize area damage for maximum skull elimination
        if (distanceToTarget <= streamThreshold)
        {
            // CLOSE RANGE: Shotgun spam + stream combo for maximum area coverage
            if (currentTime >= _lastShotgunTime + shotgunCooldown * 0.5f) // Faster shotgun in swarm mode
            {
                FireLeftHandShotgun();
                DealShotgunDamage();
                _lastShotgunTime = currentTime;
            }
            
            // Continuous stream for area denial
            if (!_isBeaming)
            {
                StartRightHandStream();
            }
            
            Debug.Log($"[CompanionAI] 🚀💥💀 SWARM CLOSE COMBAT: Rapid shotgun + stream at {distanceToTarget:F1}!");
        }
        else
        {
            // LONG RANGE: Stream focus with occasional shotgun
            if (!_isBeaming)
            {
                StartRightHandStream();
            }
            
            if (currentTime >= _lastShotgunTime + shotgunCooldown * 2f)
            {
                FireLeftHandShotgun();
                DealShotgunDamage();
                _lastShotgunTime = currentTime;
            }
            
            Debug.Log($"[CompanionAI] 🚀🔥💀 SWARM LONG RANGE: Stream focus at {distanceToTarget:F1}!");
        }
        
        // 🚀 RELENTLESS MOVEMENT in swarm mode
        HandleRelentlessMovement();
    }
    
    /// <summary>
    /// 🎯 PRECISION COMBAT MODE - Traditional targeting for single enemies
    /// </summary>
    private void HandlePrecisionCombat()
    {
        // TARGET LOCKING - stick to target until dead!
        if (!_isTargetLocked || _lockedTarget != _currentTarget)
        {
            _lockedTarget = _currentTarget;
            _isTargetLocked = true;
            _targetLockTime = Time.time;
            Debug.Log($"[CompanionAI] 🎯 PRECISION TARGET LOCKED: {_lockedTarget.name}!");
        }
        
        // FORCE UNLOCK if target becomes invalid or timeout
        if (_isTargetLocked && (Time.time - _targetLockTime > 10f || !IsValidTarget(_currentTarget)))
        {
            _isTargetLocked = false;
            _lockedTarget = null;
            StopRightHandStream();
            Debug.Log("[CompanionAI] 🔓 FORCE UNLOCK! Seeking new target!");
        }
        
        // Standard dual-wielding logic
        FaceTarget(_currentTarget);
        HandleRelentlessMovement();
        
        float currentTime = Time.time;
        float distanceToTarget = Vector3.Distance(_transform.position, _currentTarget.position);
        
        if (distanceToTarget <= streamThreshold)
        {
            // Close range combat
            if (currentTime >= _lastShotgunTime + shotgunCooldown)
            {
                FireLeftHandShotgun();
                DealShotgunDamage();
                _lastShotgunTime = currentTime;
            }
            
            if (!_isBeaming)
            {
                StartRightHandStream();
            }
        }
        else
        {
            // Long range combat
            if (!_isBeaming)
            {
                StartRightHandStream();
            }
            
            if (currentTime >= _lastShotgunTime + shotgunCooldown * 3f)
            {
                FireLeftHandShotgun();
                DealShotgunDamage();
                _lastShotgunTime = currentTime;
            }
        }
    }
    
    /// <summary>
    /// 🛑 Stop all shooting activities - Dual wielding system
    /// </summary>
    private void StopShooting()
    {
        StopRightHandStream();
        Debug.Log("[CompanionAI] 🛑 All dual-wielding stopped!");
    }
    
    /// <summary>
    /// Face the target smoothly
    /// </summary>
    private void FaceTarget(Transform target)
    {
        if (target == null) return;
        
        Vector3 direction = (target.position - _transform.position).normalized;
        direction.y = 0; // Keep companion upright
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRotation, Time.deltaTime * 15f); // MUCH FASTER ROTATION!
        }
    }
    
    /// <summary>
    /// Face the target with ultra-fast turning for swarm combat
    /// </summary>
    private void FaceTargetFast(Transform target, float speedMultiplier = 1f)
    {
        if (target == null) return;

        Vector3 direction = (target.position - _transform.position).normalized;
        direction.y = 0; // Keep companion upright

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float rotationSpeed = 20f * speedMultiplier; // ULTRA FAST rotation for swarm combat!
            _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
    
    #endregion
    
    #region Dynamic Combat Movement
    
    /// <summary>
    /// RELENTLESS MOVEMENT - NEVER STOP MOVING DURING COMBAT!
    /// </summary>
    private void HandleRelentlessMovement()
    {
        if (_currentTarget == null) return;
        
        float currentTime = Time.time;
        
        // CONSTANT REPOSITIONING - much more frequent!
        if (currentTime >= _lastRepositionTime + repositionInterval)
        {
            _lastRepositionTime = currentTime;
            // No need for separate position calculation - handled below
        }
        
        // CONSTANT STRAFING - change direction frequently
        if (currentTime >= _lastStrafeChange + 0.3f) // Change strafe every 0.3 seconds!
        {
            _strafeDirection = Random.insideUnitSphere;
            _strafeDirection.y = 0; // Keep horizontal
            _strafeDirection.Normalize();
            _lastStrafeChange = currentTime;
        }
        
        // FREQUENT JUMPING for evasion
        if (currentTime >= _lastJumpTime + 0.8f && Random.value < jumpChance)
        {
            PerformCombatJump();
            _lastJumpTime = currentTime;
        }
        
        // DYNAMIC MOVEMENT - always moving with purpose!
        float distanceToTarget = Vector3.Distance(_transform.position, _currentTarget.position);
        Vector3 targetPos;
        
        if (distanceToTarget > attackRange * 1.2f)
        {
            // Move closer while strafing - AGGRESSIVE APPROACH
            Vector3 toTarget = (_currentTarget.position - _transform.position).normalized;
            targetPos = _transform.position + (toTarget + _strafeDirection * 0.3f) * 5000f; // 100X BIGGER RADIUS!
            Debug.Log($"[CompanionAI] ⚡ AGGRESSIVE APPROACH! Moving closer while strafing");
        }
        else if (distanceToTarget < attackRange * 0.3f)
        {
            // Back away while strafing - TACTICAL RETREAT
            Vector3 awayFromTarget = (_transform.position - _currentTarget.position).normalized;
            targetPos = _transform.position + (awayFromTarget + _strafeDirection * 0.5f) * 8000f; // 100X BIGGER RADIUS!
            Debug.Log($"[CompanionAI] ⚡ TACTICAL RETREAT! Backing away while strafing");
        }
        else
        {
            // Perfect range - PURE STRAFING for evasion
            targetPos = _transform.position + _strafeDirection * 6000f; // 100X BIGGER RADIUS!
            Debug.Log($"[CompanionAI] ⚡ PURE EVASIVE STRAFING! Perfect combat range");
        }
        
        _navAgent.speed = runningSpeed * combatSpeedMultiplier; // 300X ULTRA FAST COMBAT!
        _navAgent.SetDestination(targetPos);
        // Companion is constantly moving during combat via NavMeshAgent destination updates
    }
    
    
    /// <summary>
    /// Perform a combat jump for natural movement
    /// </summary>
    private void PerformCombatJump()
    {
        if (_rigidbody != null && _rigidbody.linearVelocity.y < 1f) // Only jump if grounded
        {
            Vector3 jumpDirection = Vector3.up + Random.insideUnitSphere * 0.3f;
            jumpDirection.y = Mathf.Abs(jumpDirection.y); // Always jump up
            
            _rigidbody.AddForce(jumpDirection * jumpForce, ForceMode.Impulse);
            Debug.Log("[CompanionAI] 🦘 Combat jump! Natural movement!");
        }
    }
    
    #endregion
    
    #region Enemy Detection (Optimized)
    
    /// <summary>
    /// 🧠 INTELLIGENT ENEMY SCANNING - Maximum performance for skull clusters
    /// </summary>
    private void ScanForEnemies()
    {
        // Don't scan if dead
        if (_isDead) return;
        
        // ⚡ PERFORMANCE: Don't scan if this is an enemy companion (they use EnemyCompanionBehavior instead)
        var enemyBehavior = GetComponent<CompanionAI.EnemyCompanionBehavior>();
        if (enemyBehavior != null) return;

        // Debug.Log($"[CompanionAI] 🔍 SCANNING FOR ENEMIES - Hunting Mode: {_isInAggressiveHuntingMode}, Current State: {_currentState}, Threat Level: {_currentThreatLevel}");

        // 🚀 SWARM OPTIMIZATION: Reduce target switching frequency in swarm mode
        if (_isInSwarmMode && _currentTarget != null && _currentTarget.gameObject.activeInHierarchy)
        {
            // In swarm mode, stick with current target longer to reduce performance overhead
            IDamageable currentDamageable = _currentTarget.GetComponent<IDamageable>();
            if (currentDamageable != null)
            {
                // Only switch if current target is definitely dead or out of range
                float distanceToTarget = Vector3.Distance(_transform.position, _currentTarget.position);
                if (distanceToTarget <= detectionRadius * 1.2f) // Give some buffer
                {
                    Debug.Log($"[CompanionAI] 🚀 SWARM MODE: Sticking with current target {_currentTarget.name}");
                    return; // Keep current target for performance
                }
            }
        }
        
        // ULTRA FAST TARGET SWITCHING - check current target validity
        bool currentTargetValid = _currentTarget != null && IsValidTarget(_currentTarget);
        
        // If current target is dead, immediately unlock and find new target
        if (!currentTargetValid)
        {
            _isTargetLocked = false;
            _lockedTarget = null;
            _currentTarget = null;
            
            // 🚀 Start spray transition if we were in swarm mode
            if (_isInSwarmMode)
            {
                _isSprayTransitioning = true;
                _sprayTransitionTimer = sprayTransitionTime;
                Debug.Log("[CompanionAI] 🚀💀 SWARM TARGET ELIMINATED! Starting spray transition...");
            }
            else
            {
                Debug.Log("[CompanionAI] 💀 TARGET ELIMINATED! Seeking new target...");
            }
        }

        // 🚨 REVOLUTIONARY TARGET SELECTION - Use swarm-optimized system
        Transform newBestTarget = SelectBestTarget();
        
        // Simple target switching logic for performance
        if (_currentTarget == null && newBestTarget != null)
        {
            _currentTarget = newBestTarget;
            _isTargetLocked = false; // Allow relocking
            
            string targetType = newBestTarget.GetComponent<SkullEnemy>() != null ? "SKULL" : "GEM";
            float distance = Vector3.Distance(_transform.position, newBestTarget.position);
            
            if (_isInSwarmMode)
            {
                Debug.Log($"[CompanionAI] 🚀💀 SWARM TARGET ACQUIRED: {targetType} {newBestTarget.name} at {distance:F1}m!");
            }
            else
            {
                Debug.Log($"[CompanionAI] 🎯 PRECISION TARGET ACQUIRED: {targetType} {newBestTarget.name} at {distance:F1}m!");
            }
            
            // IMMEDIATELY transition to engaging if we're currently following
            if (_currentState == CompanionState.Following)
            {
                Debug.Log($"[CompanionAI] 🔥 IMMEDIATE COMBAT TRANSITION! Switching from Following to Engaging!");
                TransitionToState(CompanionState.Engaging);
            }
        }
        else if (_currentTarget == null)
        {
            Debug.Log("[CompanionAI] ❌ NO TARGETS FOUND! Continuing search...");
            _isInSwarmMode = false; // Exit swarm mode if no targets
            _isSprayTransitioning = false;
        }
    }
    
    /// <summary>
    /// 🚨 CRITICAL: Validate if a target is still valid for shooting (especially detached gems!)
    /// </summary>
    private bool IsValidTarget(Transform target)
    {
        if (target == null) return false;
        
        // Check if target still exists and is active
        if (target.gameObject == null || !target.gameObject.activeInHierarchy) return false;
        
        // Check if still has damageable component
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable == null) return false;
        
        // Check if still within detection range
        float distance = Vector3.Distance(_transform.position, target.position);
        return distance <= detectionRadius;
    }
    
    // Backward-compatibility alias used by existing calls
    private bool IsTargetValid(Transform target)
    {
        return IsValidTarget(target);
    }
    
    /// <summary>
    /// Move towards the player's position
    /// </summary>
    private void MoveTowardsPlayer()
    {
        if (playerTransform == null) return;
        
        Vector3 targetPosition = GetFollowPosition();
        
        // Check if the target position is on the ground
        if (IsPositionOnGround(targetPosition))
        {
            _navAgent.SetDestination(targetPosition);
        }
        else
        {
            // Fallback to player position if follow position is not valid
            _navAgent.SetDestination(playerTransform.position);
        }
    }
    
    /// <summary>
    /// Get the ideal follow position behind/beside the player (scaled for large world)
    /// </summary>
    private Vector3 GetFollowPosition()
    {
        Vector3 playerPos = playerTransform.position;
        Vector3 playerForward = playerTransform.forward;
        
        // Position behind and to the side of the player (scaled for large world)
        Vector3 followOffset = (-playerForward * followingDistance * 0.6f) + (playerTransform.right * followingDistance * 0.4f);
        
        return playerPos + followOffset;
    }
    
    /// <summary>
    /// Check if a position is on valid ground
    /// </summary>
    private bool IsPositionOnGround(Vector3 position)
    {
        // Raycast downward to check for ground
        return Physics.Raycast(position + Vector3.up, Vector3.down, 2f, groundLayers);
    }
    
    /// <summary>
    /// Emergency teleport to player when too far away
    /// </summary>
    private void TeleportToPlayer()
    {
        if (playerTransform == null) return;
        
        Vector3 teleportPos = GetFollowPosition();
        
        // Make sure the teleport position is on the ground
        if (Physics.Raycast(teleportPos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f, groundLayers))
        {
            teleportPos = hit.point + Vector3.up * 0.1f;
        }
        
        _transform.position = teleportPos;
        
        // Only warp NavMeshAgent if NavMesh is available
        if (_navAgent != null && _navAgent.isOnNavMesh)
        {
            try
            {
                _navAgent.Warp(teleportPos);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CompanionAI] NavMesh warp failed: {e.Message}. Position set directly.");
            }
        }
        
        Debug.Log("[CompanionAI] Teleported to player (was too far away)");
    }
    
    #endregion
    
    #region State Management
    
    /// <summary>
    /// Transition to a new AI state
    /// </summary>
    private void TransitionToState(CompanionState newState)
    {
        if (_currentState == newState) return;
        
        // Exit current state
        switch (_currentState)
        {
            case CompanionState.Attacking:
                StopShooting();
                break;
        }
        
        // Enter new state
        _currentState = newState;
        
        switch (newState)
        {
            case CompanionState.Following:
                _navAgent.stoppingDistance = followingDistance * 0.8f;
                break;
                
            case CompanionState.Engaging:
                _navAgent.stoppingDistance = attackRange * 0.8f;
                break;
                
            case CompanionState.Attacking:
                _navAgent.ResetPath(); // Stop moving when attacking
                break;
        }
    }
    
    #endregion
    
    #region Health and Death System
    
    /// <summary>
    /// IDamageable implementation - companion dies in one hit
    /// </summary>
    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (_isDead) return;
        
        // One hit kill
        _health = 0f;
        Die();
    }
    
    /// <summary>
    /// Handle companion death
    /// </summary>
    private void Die()
    {
        if (_isDead) return;
        
        _isDead = true;
        _currentState = CompanionState.Dead;
        
        // Stop all movement and shooting
        if (_navAgent != null)
        {
            _navAgent.isStopped = true;
            _navAgent.enabled = false;
        }
        
        StopShooting();
        
        // Stop AI coroutines
        if (_aiUpdateCoroutine != null)
        {
            StopCoroutine(_aiUpdateCoroutine);
            _aiUpdateCoroutine = null;
        }
        
        if (_enemyScanCoroutine != null)
        {
            StopCoroutine(_enemyScanCoroutine);
            _enemyScanCoroutine = null;
        }
        
        // Fall over (unfreeze rotation)
        if (GetComponent<Rigidbody>() != null)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.freezeRotation = false; // Allow falling over
            
            // Add some dramatic death physics
            rb.AddForce(Vector3.up * 200f + Random.insideUnitSphere * 100f, ForceMode.Impulse);
        }
        
        Debug.Log("[CompanionAI] Companion has died!");
    }
    
    #endregion
    
    #region Debug & Gizmos
    
    void OnDrawGizmosSelected()
    {
        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw following distance
        if (playerTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerTransform.position, followingDistance);
            
            // Draw line to current target
            if (_currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _currentTarget.position);
            }
        }
    }
    
    #endregion
    
    #region Threat Tracking Integration
    
    /// <summary>
    /// 🚨 Called by EnemyThreatTracker when combat mode is activated
    /// </summary>
    public void OnCombatModeActivated(int threatLevel)
    {
        _isInAggressiveHuntingMode = true;
        _currentThreatLevel = threatLevel;

        Debug.Log($"[CompanionAI] 🚨⚔️ AGGRESSIVE HUNTING MODE ACTIVATED! {threatLevel} skulls detected!");

        // Increase combat parameters for hunting mode
        combatSpeedMultiplier = Mathf.Min(5f, combatSpeedMultiplier + 1f); // Even faster!
        detectionRadius = Mathf.Min(100f, detectionRadius + 20f); // Wider detection

        // Force immediate target scan
        StartCoroutine(ForceTargetScan());
    }
    
    /// <summary>
    /// ✅ Called by EnemyThreatTracker when combat mode is deactivated
    /// </summary>
    public void OnCombatModeDeactivated()
    {
        _isInAggressiveHuntingMode = false;
        _currentThreatLevel = 0;
        
        Debug.Log("[CompanionAI] ✅🕊️ AGGRESSIVE HUNTING MODE DEACTIVATED! All skulls eliminated!");
        
        // Reset combat parameters
        combatSpeedMultiplier = 300f; // Back to ULTRA FAST 300x speed
        
        // Clear current target and return to following
        _currentTarget = null;
        _isTargetLocked = false;
        StopShooting();
        TransitionToState(CompanionState.Following);
    }
    
    /// <summary>
    /// 🎯 Force immediate target scan for hunting mode
    /// </summary>
    private IEnumerator ForceTargetScan()
    {
        yield return new WaitForSeconds(0.1f);
        
        // Get closest skull from threat tracker
        if (EnemyThreatTracker.Instance != null)
        {
            SkullEnemy closestSkull = EnemyThreatTracker.Instance.GetClosestEnemy(_transform.position);
            if (closestSkull != null)
            {
                _currentTarget = closestSkull.transform;
                _isTargetLocked = true;
                _targetLockTime = Time.time;
                TransitionToState(CompanionState.Engaging);
                
                Debug.Log($"[CompanionAI] 🎯💀 HUNTING TARGET LOCKED: {closestSkull.name}!");
            }
        }
    }
    
    /// <summary>
    /// 🎵 TEST AUDIO SYSTEM - Call this to test if companion audio is working
    /// </summary>
    public void TestAudioSystem()
    {
        Debug.Log("[CompanionAI] 🎵 TESTING AUDIO SYSTEM...");

        if (_audioSource == null)
        {
            Debug.LogError("[CompanionAI] ❌ AudioSource is NULL! Cannot test audio.");
            return;
        }

        if (shotgunSFX == null)
        {
            Debug.LogError("[CompanionAI] ❌ Shotgun SFX is NULL! Cannot test shotgun sound.");
            return;
        }

        if (streamLoopSFX == null)
        {
            Debug.LogError("[CompanionAI] ❌ Stream Loop SFX is NULL! Cannot test stream sound.");
            return;
        }

        // Test shotgun sound
        Debug.Log("[CompanionAI] 🔫 TESTING SHOTGUN SOUND...");
        _audioSource.PlayOneShot(shotgunSFX, 0.5f); // Lower volume for testing

        // Test stream sound after 2 seconds
        StartCoroutine(TestStreamAudio());
    }

    private IEnumerator TestStreamAudio()
    {
        yield return new WaitForSeconds(2f);

        Debug.Log("[CompanionAI] 🔥 TESTING STREAM SOUND...");
        _audioSource.clip = streamLoopSFX;
        _audioSource.loop = true;
        _audioSource.volume = 0.3f; // Lower volume for testing
        _audioSource.spatialBlend = 1f;
        _audioSource.Play();

        // Stop after 3 seconds
        yield return new WaitForSeconds(3f);
        _audioSource.Stop();
        _audioSource.clip = null;
        Debug.Log("[CompanionAI] 🛑 AUDIO TEST COMPLETE!");
    }
    
    #endregion
    
    #region Combat Visual Effects
    
    /// <summary>
    /// 💚 Initialize combat visual effects - instantiate glow prefabs
    /// </summary>
    private void InitializeCombatVisualEffects()
    {
        // Instantiate following glow effect (green)
        if (followingGlowEffectPrefab != null)
        {
            _followingGlowEffect = Instantiate(followingGlowEffectPrefab, _transform.position, _transform.rotation, _transform);
            Debug.Log("[CompanionAI] 💚 Following glow effect instantiated!");
        }
        else
        {
            Debug.LogWarning("[CompanionAI] ⚠️ Following glow effect prefab not assigned!");
        }

        // Instantiate combat glow effect (red) - initially inactive
        if (combatGlowEffectPrefab != null)
        {
            _combatGlowEffect = Instantiate(combatGlowEffectPrefab, _transform.position, _transform.rotation, _transform);
            _combatGlowEffect.SetActive(false); // Start inactive
            Debug.Log("[CompanionAI] 🔴 Combat glow effect instantiated (inactive)!");
        }
        else
        {
            Debug.LogWarning("[CompanionAI] ⚠️ Combat glow effect prefab not assigned!");
        }
    }

    /// <summary>
    /// 🔗 Subscribe to EnemyThreatTracker events for visual feedback
    /// </summary>
    private void SubscribeToThreatTrackerEvents()
    {
        if (EnemyThreatTracker.Instance != null)
        {
            EnemyThreatTracker.Instance.OnCombatModeActivated += HandleCombatModeActivated;
            EnemyThreatTracker.Instance.OnCombatModeDeactivated += HandleCombatModeDeactivated;
            Debug.Log("[CompanionAI] 🔗 Subscribed to EnemyThreatTracker events!");
        }
        else
        {
            Debug.LogWarning("[CompanionAI] ⚠️ EnemyThreatTracker instance not found!");
        }
    }

    /// <summary>
    /// 🎭 Subscribe to player emote events for hilarious mirroring
    /// </summary>
    private void SubscribeToPlayerEmoteEvents()
    {
        HandAnimationController.OnPlayerEmote += HandlePlayerEmote;
        Debug.Log("[CompanionAI] 🎭 Subscribed to player emote events for mirroring!");
    }

    /// <summary>
    /// 🤣 Handle player emote - MIRROR THE EMOTE for hilarious effect!
    /// </summary>
    private void HandlePlayerEmote(int emoteNumber)
    {
        Debug.Log($"[CompanionAI] 🎭 Player did emote {emoteNumber}! Companion acknowledges it.");

        // CompanionHandEmote system has been removed - companions no longer mirror emotes
        Debug.Log($"[CompanionAI] 🤣 Companion acknowledges player emote {emoteNumber} but no longer mirrors it!");
        
        // Could add other companion reactions here instead of emote mirroring
        // For example: audio reactions, movement responses, etc.
        
        // Make companion face the player briefly as a reaction
        StartCoroutine(FacePlayerDuringEmote());
    }

    /// <summary>
    /// 🎯 Make companion face player during emote for better visual effect
    /// </summary>
    private IEnumerator FacePlayerDuringEmote()
    {
        if (playerTransform == null) yield break;

        float emoteDuration = 2.0f; // Match the emote duration in ProceduralHandAnimator
        float elapsed = 0f;

        while (elapsed < emoteDuration)
        {
            // Make companion face the player
            Vector3 directionToPlayer = playerTransform.position - _transform.position;
            directionToPlayer.y = 0f; // Keep companion upright

            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRotation, Time.deltaTime * 8f);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.Log("[CompanionAI] ✅ Emote facing completed, companion can resume normal behavior");
    }

    /// <summary>
    /// 🎯 Handle combat mode activation - show red glow
    /// </summary>
    private void HandleCombatModeActivated()
    {
        if (_followingGlowEffect != null)
        {
            _followingGlowEffect.SetActive(false);
            Debug.Log("[CompanionAI] 💚 Following glow effect deactivated!");
        }

        if (_combatGlowEffect != null)
        {
            _combatGlowEffect.SetActive(true);
            Debug.Log("[CompanionAI] 🔴 Combat glow effect activated!");
        }
    }

    /// <summary>
    /// 🏠 Handle combat mode deactivation - show green glow
    /// </summary>
    private void HandleCombatModeDeactivated()
    {
        if (_combatGlowEffect != null)
        {
            _combatGlowEffect.SetActive(false);
            Debug.Log("[CompanionAI] 🔴 Combat glow effect deactivated!");
        }

        if (_followingGlowEffect != null)
        {
            _followingGlowEffect.SetActive(true);
            Debug.Log("[CompanionAI] 💚 Following glow effect activated!");
        }
    }

    /// <summary>
    /// 🧹 Clean up visual effects
    /// </summary>
    private void CleanupVisualEffects()
    {
        if (_followingGlowEffect != null)
        {
            Destroy(_followingGlowEffect);
            _followingGlowEffect = null;
            Debug.Log("[CompanionAI] 💚 Following glow effect destroyed!");
        }

        if (_combatGlowEffect != null)
        {
            Destroy(_combatGlowEffect);
            _combatGlowEffect = null;
            Debug.Log("[CompanionAI] 🔴 Combat glow effect destroyed!");
        }
    }

    #endregion
}
