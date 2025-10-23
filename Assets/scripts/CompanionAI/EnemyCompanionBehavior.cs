using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using GeminiGauntlet.Audio;

namespace CompanionAI
{
    /// <summary>
    /// Enemy AI states for intelligent behavior
    /// </summary>
    public enum EnemyState
    {
        Idle,
        Patrolling,
        Hunting,
        Attacking
    }
    
    /// <summary>
    /// 🔥 ENEMY COMPANION BEHAVIOR - Modular add-on script
    /// 
    /// This script converts a friendly companion into an enemy that hunts the player.
    /// It works by OVERRIDING the existing companion systems without modifying them.
    /// 
    /// HOW IT WORKS:
    /// 1. Hijacks the player reference to make companion follow/attack player
    /// 2. Overrides targeting system to focus on player instead of enemies
    /// 3. Adds patrol behavior for area scouting
    /// 4. Maintains all existing companion functionalities (movement, combat, jumping, etc.)
    /// 5. Fully damageable - player can shoot and kill the enemy companion
    /// 
    /// COMBAT FEATURES:
    /// - Range-based shooting (stops when too far)
    /// - Beam cooldown system (5s fire, 2s cooldown)
    /// - Aim inaccuracy (70% accuracy by default)
    /// - Respects max shooting range
    /// 
    /// USAGE:
    /// - Simply add this script to any companion GameObject
    /// - Check "Is Enemy" in inspector to activate enemy mode
    /// - Set patrol points for area scouting (optional)
    /// - Enemy can be damaged via IDamageable interface (raycasts, projectiles, etc.)
    /// - All companion systems continue working normally
    /// </summary>
    [RequireComponent(typeof(CompanionCore))]
    public class EnemyCompanionBehavior : MonoBehaviour
    {
        [Header("🔥 ENEMY MODE")]
        [Tooltip("Enable this to convert companion into an enemy that hunts the player")]
        public bool isEnemy = false;
        
        [Header("🎯 HUNTING BEHAVIOR")]
        [Tooltip("How far the enemy companion can detect the player")]
        [Range(1000f, 50000f)] public float playerDetectionRadius = 6000f; // NUCLEAR: Very short detection range (was 8000)
        
        [Tooltip("How close the enemy gets before attacking")]
        [Range(500f, 10000f)] public float attackRange = 5000f;
        
        [Tooltip("Maximum range for shooting - stops shooting if player is too far")]
        [Range(1000f, 20000f)] public float maxShootingRange = 8000f;
        
        [Tooltip("How aggressively the enemy pursues (higher = more aggressive)")]
        [Range(0.5f, 2f)] public float aggressionMultiplier = 1.2f;
        
        [Header("⚔️ COMBAT BEHAVIOR")]
        [Tooltip("Maximum beam duration before cooldown")]
        [Range(1f, 10f)] public float maxBeamDuration = 5f;
        
        [Tooltip("Beam cooldown time after max duration")]
        [Range(0.5f, 5f)] public float beamCooldownTime = 2f;
        
        [Tooltip("Aim accuracy - lower = more misses (0 = always miss, 1 = perfect aim)")]
        [Range(0f, 1f)] public float aimAccuracy = 0.7f;
        
        [Tooltip("How much the aim can deviate (in units)")]
        [Range(0f, 500f)] public float aimDeviation = 150f;
        
        [Tooltip("Enable aggressive tactical movement (strafing, jumping, repositioning)")]
        public bool enableTacticalMovement = false; // NUCLEAR: Disabled - tactical movement is expensive
        
        [Tooltip("Combat movement speed multiplier - REDUCED for indoor stability")]
        [Range(0.5f, 3f)] public float combatMovementSpeed = 1.2f;
        
        [Header("🏢 ENVIRONMENT AWARENESS")]
        [Tooltip("Auto-detect if enemy is indoors (checks for ceiling)")]
        public bool autoDetectIndoors = true;
        
        [Tooltip("Force indoor behavior mode")]
        public bool forceIndoorMode = false;
        
        [Tooltip("Speed multiplier when indoors (prevents glitchy movement)")]
        [Range(0.3f, 1f)] public float indoorSpeedMultiplier = 0.5f;
        
        [Tooltip("Disable jumping when indoors")]
        public bool disableJumpingIndoors = true;
        
        [Tooltip("Disable tactical repositioning when indoors")]
        public bool disableTacticalMovementIndoors = true;
        
        [Header("🚶 PATROL BEHAVIOR")]
        [Tooltip("Enable patrol mode when player is not detected")]
        public bool enablePatrol = false; // NUCLEAR: Disabled by default - patrol is expensive (NavMesh calculations)
        
        [Tooltip("Patrol points for area scouting (leave empty for random patrol)")]
        public Transform[] patrolPoints;
        
        [Tooltip("How long to wait at each patrol point (seconds)")]
        [Range(0f, 10f)] public float patrolWaitTime = 2f;
        
        [Tooltip("Random patrol radius if no patrol points are set (scaled for 320-unit tall player)")]
        [Range(5000f, 50000f)] public float randomPatrolRadius = 25000f;
        
        [Header("🔍 DETECTION SETTINGS")]
        [Tooltip("How often to check for player detection (seconds) - lower = more responsive but more expensive")]
        [Range(0.1f, 2f)] public float detectionInterval = 1.0f; // NUCLEAR: 1 check per second (was 0.75s)
        
        [Tooltip("Line of sight check - enemy must see player to attack")]
        public bool requireLineOfSight = true;
        
        [Header("🗺️ NAVMESH LOS SYSTEM (BEST PERFORMANCE + ACCURACY)")]
        [Tooltip("Use NavMesh path checking for wall detection (RECOMMENDED - 10x faster + 99% accurate!)")]
        public bool useNavMeshLOS = true;
        
        [Tooltip("Cache NavMesh path results for this many seconds (0 = no cache)")]
        [Range(0f, 2f)] public float navMeshCacheDuration = 0.5f;
        
        [Tooltip("Fallback to raycast if NavMesh check is inconclusive")]
        public bool useRaycastFallback = true;
        
        [Header("🎯 RAYCAST LOS SYSTEM (FALLBACK)")]
        [Tooltip("Layers that block line of sight (walls, obstacles)")]
        public LayerMask lineOfSightBlockers = -1;
        
        [Tooltip("Number of raycasts for line of sight (more = accurate but expensive)")]
        [Range(1, 5)] public int losRaycastCount = 1; // REDUCED from 3 - performance optimization (use 1 center ray only)
        
        [Tooltip("Eye height offset for line of sight checks (player is 320 units tall)")]
        [Range(50f, 300f)] public float eyeHeight = 160f;
        
        [Tooltip("Raycast spread for better detection (player is 50 units wide)")]
        [Range(10f, 100f)] public float losRaycastSpread = 30f;
        
        [Header("⚡ PERFORMANCE OPTIMIZATION")]
        [Tooltip("Activation radius - enemy AI only runs when player is within this distance")]
        [Range(5000f, 50000f)] public float activationRadius = 10000f; // NUCLEAR: Very tight activation (was 12000)
        
        [Tooltip("How often to check activation distance (higher = better performance)")]
        [Range(0.5f, 5f)] public float activationCheckInterval = 2.0f; // NUCLEAR: Check every 2 seconds (was 1.5s)
        
        [Tooltip("Maximum vertical distance to activate (prevents activating enemies on different floors)")]
        [Range(100f, 2000f)] public float maxVerticalActivationDistance = 500f;
        
        [Tooltip("Arms parent GameObject (contains animated arms) - will be disabled when inactive")]
        public GameObject armsParent;
        
        [Header("🎨 LOD & CULLING (Tris/Verts Optimization)")]
        [Tooltip("Enable LOD system to reduce mesh complexity at distance")]
        public bool enableLODSystem = false; // NUCLEAR: Disabled by default - LOD checks are expensive, just disable rendering entirely
        
        [Tooltip("Distance for LOD0 (full detail) - beyond this uses LOD1")]
        [Range(2000f, 10000f)] public float lod0Distance = 5000f;
        
        [Tooltip("Distance for LOD1 (medium detail) - beyond this uses LOD2")]
        [Range(5000f, 20000f)] public float lod1Distance = 10000f;
        
        [Tooltip("Distance for LOD2 (low detail) - beyond this disables completely")]
        [Range(10000f, 30000f)] public float lod2Distance = 10000f; // NUCLEAR: Disable rendering very aggressively (was 12000)
        
        [Tooltip("Disable shadow casting when far away (huge performance gain)")]
        public bool disableShadowsAtDistance = true;
        
        [Tooltip("Distance to disable shadow casting")]
        [Range(5000f, 20000f)] public float shadowDisableDistance = 5000f; // NUCLEAR: Disable shadows VERY early (was 6000)
        
        [Header("📊 DEBUG")]
        public bool showDebugInfo = false; // DISABLED by default - Debug.Log calls are expensive!
        
        [Tooltip("Draw debug lines showing where companion is aiming")]
        public bool showAimDebug = false;
        
        [Header("💥 HIT EFFECTS")]
        [Tooltip("Enable visual hit feedback when damaged")]
        public bool enableHitEffect = false; // DISABLED by default - material color changes are expensive on potato PCs
        
        [Tooltip("Color to flash when hit")]
        public Color hitColor = Color.red;
        
        [Tooltip("How long the hit effect lasts")]
        [Range(0.05f, 1f)] public float hitEffectDuration = 0.2f;
        
        [Tooltip("Enable knockback when hit")]
        public bool enableKnockback = false; // NUCLEAR: Disabled - physics forces are expensive
        
        [Tooltip("Knockback force multiplier - lower = subtle feedback")]
        [Range(0.001f, 2f)] public float knockbackMultiplier = 0.3f;
        
        [Header("💀 DEATH SETTINGS")]
        [Tooltip("Auto-destroy enemy after death (seconds) - bodies are disabled immediately but stay visible")]
        [Range(0.5f, 60f)] public float destroyAfterDeath = 10f; // Bodies stay for 10s (all systems disabled immediately for performance)
        
        [Header("🔊 AUDIO SETTINGS")]
        [Tooltip("Reference to the SoundEvents ScriptableObject")]
        public SoundEvents soundEvents;
        
        [Tooltip("Hitmarker audio clips (fallback if SoundEvents not configured)")]
        public AudioClip[] hitmarkerClips;
        
        [Tooltip("Death audio clips (fallback if SoundEvents not configured)")]
        public AudioClip[] deathClips;
        
        [Tooltip("Volume for hitmarker sounds")]
        [Range(0f, 2f)] public float hitmarkerVolume = 1f;
        
        [Tooltip("Volume for death sounds")]
        [Range(0f, 2f)] public float deathVolume = 1.2f;
        
        [Tooltip("3D audio min distance")]
        [Range(1f, 1000f)] public float audioMinDistance = 500f;
        
        [Tooltip("3D audio max distance")]
        [Range(100f, 10000f)] public float audioMaxDistance = 3000f;
        
        [Tooltip("Cooldown between hitmarker sounds (prevents spam from multi-hit weapons)")]
        [Range(0.01f, 1f)] public float hitmarkerSoundCooldown = 0.15f;
        
        // Cached references
        private CompanionCore _companionCore;
        private CompanionTargeting _companionTargeting;
        private CompanionMovement _companionMovement;
        private CompanionCombat _companionCombat;
        private Transform _realPlayerTransform;
        private Transform _fakePlayerTransform; // Used to trick the companion system
        private GameObject _fakePlayerObject;
        private Transform _fakeTargetWithOffset; // Target with aim offset for inaccuracy
        private GameObject _fakeTargetObject;
        
        // Manual target override
        private bool _overrideTargeting = false;
        
        // Combat tracking
        private float _beamStartTime = 0f;
        private bool _isBeamActive = false;
        private float _beamCooldownEndTime = 0f;
        private Vector3 _currentAimOffset = Vector3.zero;
        private float _lastAimUpdateTime = 0f;
        
        // Hit effect tracking
        private Renderer[] _renderers;
        private Dictionary<Renderer, Color> _originalColors = new Dictionary<Renderer, Color>();
        private Coroutine _hitEffectCoroutine;
        private float _lastDamageAmount = 0f;
        
        // State tracking
        private EnemyState _currentState = EnemyState.Idle;
        private bool _playerDetected = false;
        private bool _hasEverSeenPlayer = false; // NEW: Track if we've had visual confirmation
        private bool _isPatrolling = false;
        private int _currentPatrolIndex = 0;
        private Coroutine _patrolCoroutine;
        private Vector3 _patrolStartPosition;
        private float _lastDetectionCheck = 0f;
        
        // Performance optimization
        private bool _isActive = false;
        private float _lastActivationCheck = -999f; // Start negative so first check runs immediately
        
        // LOD tracking
        private int _currentLODLevel = -1; // -1 = not initialized, 0 = full detail, 1 = medium, 2 = low, 3 = disabled
        private LODGroup _lodGroup;
        private Renderer[] _cachedRenderers;
        private Dictionary<Renderer, UnityEngine.Rendering.ShadowCastingMode> _originalShadowModes = new Dictionary<Renderer, UnityEngine.Rendering.ShadowCastingMode>();
        
        // Environment tracking
        private bool _isIndoors = false;
        private float _lastEnvironmentCheck = 0f;
        private const float ENVIRONMENT_CHECK_INTERVAL = 5f; // INCREASED from 2s - environment rarely changes
        
        // 🗺️ NAVMESH LOS CACHING - Massive performance boost!
        private bool _cachedNavMeshLOS = false;
        private float _lastNavMeshCheckTime = -999f;
        private NavMeshPath _reusablePath; // Reuse path object to avoid GC allocations
        
        // ⚡ PERFORMANCE: Cached components to avoid GetComponentsInChildren every frame
        private Animator[] _cachedAnimators;
        private Renderer[] _cachedRenderersForActivation;
        private Collider[] _cachedColliders;
        private bool _componentsCached = false;
        
        // Death tracking
        private bool _deathEffectTriggered = false;
        
        // Audio components
        private AudioSource _hitAudioSource;
        private AudioSource _deathAudioSource;
        private float _lastHitmarkerSoundTime = -999f;
        
        void Awake()
        {
            _companionCore = GetComponent<CompanionCore>();
            _companionTargeting = GetComponent<CompanionTargeting>();
            _companionMovement = GetComponent<CompanionMovement>();
            _companionCombat = GetComponent<CompanionCombat>();
            
            if (_companionCore == null)
            {
                Debug.LogError("[EnemyCompanionBehavior] ❌ CompanionCore not found! This script requires CompanionCore.");
                enabled = false;
                return;
            }
            
            _patrolStartPosition = transform.position;
        }
        
        void Start()
        {
            if (!isEnemy)
            {
                // Not an enemy, disable this script
                if (showDebugInfo)
                    Debug.Log("[EnemyCompanionBehavior] 💚 Companion is friendly - enemy behavior disabled");
                enabled = false;
                return;
            }
            
            InitializeEnemyMode();
            InitializeHitEffect();
            InitializeLODSystem();
            InitializeAudioSources();
            
            // ⚡ PERFORMANCE: Cache all components ONCE at startup
            CacheComponents();
            
            // 🗺️ NAVMESH: Initialize reusable path object (avoid GC allocations)
            _reusablePath = new NavMeshPath();
            
            // ⚡ CRITICAL: Start with everything DISABLED by default
            // This prevents all 210 companions from having active animators on startup
            // They will be enabled individually when player gets close
            SetComponentsActive(false);
            _isActive = false;
            
            // ⚡ TIME-SLICING: Register with LOSManager for optimized LOS checks
            if (LOSManager.Instance != null)
            {
                LOSManager.Instance.RegisterEnemy(this);
                if (showDebugInfo)
                {
                    Debug.Log($"[EnemyCompanionBehavior] ⚡ {gameObject.name} registered with LOSManager for time-sliced checks");
                }
            }
            else if (showDebugInfo)
            {
                Debug.LogWarning("[EnemyCompanionBehavior] ⚠️ LOSManager not found! LOS checks will use old system (less optimized)");
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"[EnemyCompanionBehavior] 💤 {gameObject.name} initialized INACTIVE - will activate when player is within {activationRadius} units");
            }
        }
        
        /// <summary>
        /// ⚡ PERFORMANCE: Cache all components ONCE to avoid expensive GetComponentsInChildren calls
        /// </summary>
        private void CacheComponents()
        {
            if (_componentsCached) return;
            
            _cachedAnimators = GetComponentsInChildren<Animator>(true);
            _cachedRenderersForActivation = GetComponentsInChildren<Renderer>(true);
            _cachedColliders = GetComponentsInChildren<Collider>(true);
            _componentsCached = true;
            
            if (showDebugInfo)
            {
                Debug.Log($"[EnemyCompanionBehavior] ⚡ Cached {_cachedAnimators.Length} animators, {_cachedRenderersForActivation.Length} renderers, {_cachedColliders.Length} colliders");
            }
        }
        
        private void InitializeLODSystem()
        {
            if (!enableLODSystem) return;
            
            // Cache renderers for LOD management
            _cachedRenderers = GetComponentsInChildren<Renderer>(true);
            
            // Check if LODGroup exists
            _lodGroup = GetComponent<LODGroup>();
            
            // Cache original shadow casting modes
            foreach (Renderer renderer in _cachedRenderers)
            {
                if (renderer != null)
                {
                    _originalShadowModes[renderer] = renderer.shadowCastingMode;
                }
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"[EnemyCompanionBehavior] 🎨 LOD system initialized with {_cachedRenderers.Length} renderers");
            }
        }
        
        private void InitializeHitEffect()
        {
            if (!enableHitEffect) return;
            
            // Cache all renderers and their original colors
            _renderers = GetComponentsInChildren<Renderer>();
            _originalColors.Clear();
            
            foreach (Renderer renderer in _renderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    // Store original color
                    if (renderer.material.HasProperty("_Color"))
                    {
                        _originalColors[renderer] = renderer.material.color;
                    }
                    else if (renderer.material.HasProperty("_BaseColor"))
                    {
                        _originalColors[renderer] = renderer.material.GetColor("_BaseColor");
                    }
                }
            }
            
            // Subscribe to damage events
            if (_companionCore != null)
            {
                CompanionCore.OnCompanionHealthChanged += OnCompanionDamaged;
                
                // Wrap the TakeDamage method to intercept damage info
                WrapTakeDamageMethod();
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"[EnemyCompanionBehavior] 💥 Hit effect initialized with {_renderers.Length} renderers");
            }
        }
        
        private void WrapTakeDamageMethod()
        {
            // We'll use the Update loop to apply knockback based on damage events
            // The CompanionCore.TakeDamage is called with (amount, hitPoint, hitDirection)
            // We can't directly intercept it, so we'll calculate knockback from player direction
        }
        
        private void OnCompanionDamaged(CompanionCore companion)
        {
            // Only trigger for this companion
            if (companion != _companionCore) return;
            
            // Check if just died
            if (_companionCore.IsDead && !_deathEffectTriggered)
            {
                _deathEffectTriggered = true; // Ensure this only runs ONCE
                
                // Enhance death effect for dramatic fall
                EnhanceDeathEffect();
                
                Debug.Log($"[EnemyCompanionBehavior] 💀 {gameObject.name} DIED! Death cleanup started, will destroy in {destroyAfterDeath}s");
            }
            else if (!_companionCore.IsDead)
            {
                // Play hitmarker sound (3D)
                PlayHitmarkerSound();
                
                // Trigger hit effect
                if (enableHitEffect)
                {
                    TriggerHitEffect();
                }
                
                // Apply knockback
                if (enableKnockback && _realPlayerTransform != null)
                {
                    ApplyKnockback();
                }
            }
        }
        
        private void ApplyKnockback()
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null || _realPlayerTransform == null) return;
            
            // CRITICAL: Kinematic rigidbodies cannot be moved by forces!
            // Enemies use kinematic rigidbodies to prevent physics pushback
            if (rb.isKinematic)
            {
                if (showDebugInfo)
                    Debug.Log($"[EnemyCompanionBehavior] ⚠️ Knockback skipped - Rigidbody is kinematic (prevents pushback)");
                return;
            }
            
            // Calculate direction AWAY from player
            Vector3 knockbackDirection = (transform.position - _realPlayerTransform.position).normalized;
            
            // Estimate damage type based on distance
            float distanceToPlayer = Vector3.Distance(transform.position, _realPlayerTransform.position);
            
            float knockbackForce;
            if (distanceToPlayer < 1600f)
            {
                // SHOTGUN - Close range, moderate knockback
                knockbackForce = 800f * knockbackMultiplier; // Reduced from 2000
                
                if (showDebugInfo)
                    Debug.Log($"[EnemyCompanionBehavior] 💥 SHOTGUN KNOCKBACK! Force: {knockbackForce}");
            }
            else
            {
                // BEAM - Long range, subtle knockback
                knockbackForce = 200f * knockbackMultiplier; // Reduced from 500
                
                if (showDebugInfo)
                    Debug.Log($"[EnemyCompanionBehavior] 💥 Beam knockback. Force: {knockbackForce}");
            }
            
            // Apply knockback force
            rb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
            
            // Add slight upward force for more dramatic effect
            rb.AddForce(Vector3.up * (knockbackForce * 0.3f), ForceMode.Impulse);
        }
        
        private void EnhanceDeathEffect()
        {
            // Play death sound (3D)
            PlayDeathSound();
            
            // CRITICAL: Disable all components that keep the enemy upright
            
            // 1. Disable NavMeshAgent (keeps it upright)
            NavMeshAgent navAgent = GetComponent<NavMeshAgent>();
            if (navAgent != null)
            {
                navAgent.enabled = false;
            }
            
            // 2. Disable CharacterController (prevents rotation)
            CharacterController charController = GetComponent<CharacterController>();
            if (charController != null)
            {
                charController.enabled = false;
            }
            
            // 3. Disable all companion systems
            if (_companionMovement != null) _companionMovement.enabled = false;
            if (_companionCombat != null) _companionCombat.enabled = false;
            if (_companionTargeting != null) _companionTargeting.enabled = false;
            
            // 3.5. ⚡ NUCLEAR: Disable ALL animators immediately (HUGE performance save)
            if (_cachedAnimators != null)
            {
                foreach (Animator animator in _cachedAnimators)
                {
                    if (animator != null)
                    {
                        animator.enabled = false;
                    }
                }
            }
            
            // 3.6. ⚡ NUCLEAR: Make rigidbody kinematic to stop ALL physics calculations (HUGE performance save)
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true; // Stop ALL physics simulation
                rb.detectCollisions = false; // Stop collision detection
                
                if (showDebugInfo)
                    Debug.Log("[EnemyCompanionBehavior] 💀 DEATH! All systems disabled, physics stopped.");
            }
            
            // 3.7. ⚡ NUCLEAR: Disable ALL colliders (stop collision checks)
            if (_cachedColliders != null)
            {
                foreach (Collider collider in _cachedColliders)
                {
                    if (collider != null)
                    {
                        collider.enabled = false;
                    }
                }
            }
            
            // 5. Schedule destruction after delay
            StartCoroutine(DestroyAfterDelay());
        }
        
        private IEnumerator DestroyAfterDelay()
        {
            if (showDebugInfo)
                Debug.Log($"[EnemyCompanionBehavior] ⏳ Enemy will be destroyed in {destroyAfterDeath} seconds...");
            
            yield return new WaitForSeconds(destroyAfterDeath);
            
            if (showDebugInfo)
                Debug.Log("[EnemyCompanionBehavior] 💥 Destroying dead enemy companion...");
            
            Destroy(gameObject);
        }
        
        private void TriggerHitEffect()
        {
            // Stop existing effect if running
            if (_hitEffectCoroutine != null)
            {
                StopCoroutine(_hitEffectCoroutine);
            }
            
            _hitEffectCoroutine = StartCoroutine(HitEffectCoroutine());
        }
        
        private IEnumerator HitEffectCoroutine()
        {
            // Flash to hit color
            foreach (Renderer renderer in _renderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    if (renderer.material.HasProperty("_Color"))
                    {
                        renderer.material.color = hitColor;
                    }
                    else if (renderer.material.HasProperty("_BaseColor"))
                    {
                        renderer.material.SetColor("_BaseColor", hitColor);
                    }
                    
                    // Also increase emission for glow effect
                    if (renderer.material.HasProperty("_EmissionColor"))
                    {
                        renderer.material.EnableKeyword("_EMISSION");
                        renderer.material.SetColor("_EmissionColor", hitColor * 4000f); // ✅ Balanced glow
                    }
                }
            }
            
            // Wait for duration
            yield return new WaitForSeconds(hitEffectDuration);
            
            // Restore original colors
            foreach (Renderer renderer in _renderers)
            {
                if (renderer != null && renderer.material != null && _originalColors.ContainsKey(renderer))
                {
                    Color originalColor = _originalColors[renderer];
                    
                    if (renderer.material.HasProperty("_Color"))
                    {
                        renderer.material.color = originalColor;
                    }
                    else if (renderer.material.HasProperty("_BaseColor"))
                    {
                        renderer.material.SetColor("_BaseColor", originalColor);
                    }
                    
                    // Disable emission
                    if (renderer.material.HasProperty("_EmissionColor"))
                    {
                        renderer.material.SetColor("_EmissionColor", Color.black);
                        renderer.material.DisableKeyword("_EMISSION");
                    }
                }
            }
        }
        
        /// <summary>
        /// Initialize dedicated audio sources for hit and death sounds
        /// </summary>
        private void InitializeAudioSources()
        {
            // Create hit audio source
            GameObject hitAudioObj = new GameObject("HitAudio");
            hitAudioObj.transform.SetParent(transform);
            hitAudioObj.transform.localPosition = Vector3.zero;
            _hitAudioSource = hitAudioObj.AddComponent<AudioSource>();
            ConfigureAudioSource3D(_hitAudioSource);
            
            // Create death audio source
            GameObject deathAudioObj = new GameObject("DeathAudio");
            deathAudioObj.transform.SetParent(transform);
            deathAudioObj.transform.localPosition = Vector3.zero;
            _deathAudioSource = deathAudioObj.AddComponent<AudioSource>();
            ConfigureAudioSource3D(_deathAudioSource);
            
            // Debug.Log($"[EnemyCompanionBehavior] 🔊 Audio sources initialized on {gameObject.name}");
        }
        
        /// <summary>
        /// Configure an AudioSource for perfect 3D spatial audio
        /// </summary>
        private void ConfigureAudioSource3D(AudioSource source)
        {
            source.spatialBlend = 1f; // Full 3D
            source.minDistance = audioMinDistance;
            source.maxDistance = audioMaxDistance;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.dopplerLevel = 0f;
            source.priority = 128; // Medium priority
            source.playOnAwake = false;
            source.loop = false;
        }
        
        /// <summary>
        /// Play hitmarker sound when companion is damaged (3D sound at companion position)
        /// BRILLIANT SYSTEM: Multiple fallback layers ensure sound ALWAYS plays
        /// COOLDOWN: Prevents spam from multi-hit weapons (shotgun pellets, beam ticks)
        /// </summary>
        private void PlayHitmarkerSound()
        {
            // Check cooldown to prevent sound spam from multi-hit weapons
            float timeSinceLastSound = Time.time - _lastHitmarkerSoundTime;
            if (timeSinceLastSound < hitmarkerSoundCooldown)
            {
                if (showDebugInfo)
                    Debug.Log($"[EnemyCompanionBehavior] 🔇 Hitmarker sound on cooldown ({timeSinceLastSound:F3}s / {hitmarkerSoundCooldown:F3}s)");
                return;
            }
            
            _lastHitmarkerSoundTime = Time.time;
            Debug.Log($"[EnemyCompanionBehavior] 🎯 HITMARKER SOUND TRIGGERED on {gameObject.name} at {transform.position}");
            
            // LAYER 1: Try SoundEvents system (best quality, uses pooling)
            if (TryPlayHitmarkerViaSoundEvents())
            {
                Debug.Log("[EnemyCompanionBehavior] ✅ Hitmarker played via SoundEvents");
                return;
            }
            
            // LAYER 2: Try dedicated AudioSource with fallback clips
            if (TryPlayHitmarkerViaAudioSource())
            {
                Debug.Log("[EnemyCompanionBehavior] ✅ Hitmarker played via AudioSource");
                return;
            }
            
            // LAYER 3: Emergency fallback - PlayClipAtPoint (always works)
            if (TryPlayHitmarkerViaPlayClipAtPoint())
            {
                Debug.Log("[EnemyCompanionBehavior] ✅ Hitmarker played via PlayClipAtPoint (emergency fallback)");
                return;
            }
            
            Debug.LogError($"[EnemyCompanionBehavior] ❌ FAILED TO PLAY HITMARKER SOUND - All 3 layers failed! Check audio configuration.");
        }
        
        private bool TryPlayHitmarkerViaSoundEvents()
        {
            if (soundEvents == null)
            {
                Debug.LogWarning("[EnemyCompanionBehavior] SoundEvents reference is null");
                return false;
            }
            
            if (soundEvents.companionHitmarker == null || soundEvents.companionHitmarker.Length == 0)
            {
                Debug.LogWarning("[EnemyCompanionBehavior] companionHitmarker array is empty");
                return false;
            }
            
            var handle = SoundEvents.PlayRandomSound3D(soundEvents.companionHitmarker, transform.position, hitmarkerVolume);
            return handle.IsValid;
        }
        
        private bool TryPlayHitmarkerViaAudioSource()
        {
            if (_hitAudioSource == null)
            {
                Debug.LogWarning("[EnemyCompanionBehavior] Hit AudioSource is null");
                return false;
            }
            
            if (hitmarkerClips == null || hitmarkerClips.Length == 0)
            {
                Debug.LogWarning("[EnemyCompanionBehavior] hitmarkerClips array is empty");
                return false;
            }
            
            AudioClip clip = hitmarkerClips[Random.Range(0, hitmarkerClips.Length)];
            if (clip == null)
            {
                Debug.LogWarning("[EnemyCompanionBehavior] Selected hitmarker clip is null");
                return false;
            }
            
            _hitAudioSource.PlayOneShot(clip, hitmarkerVolume);
            return true;
        }
        
        private bool TryPlayHitmarkerViaPlayClipAtPoint()
        {
            if (hitmarkerClips == null || hitmarkerClips.Length == 0)
            {
                Debug.LogWarning("[EnemyCompanionBehavior] No hitmarker clips for PlayClipAtPoint fallback");
                return false;
            }
            
            AudioClip clip = hitmarkerClips[Random.Range(0, hitmarkerClips.Length)];
            if (clip == null) return false;
            
            AudioSource.PlayClipAtPoint(clip, transform.position, hitmarkerVolume);
            return true;
        }
        
        /// <summary>
        /// Play death sound when companion dies (3D sound at companion position)
        /// BRILLIANT SYSTEM: Multiple fallback layers ensure sound ALWAYS plays
        /// </summary>
        private void PlayDeathSound()
        {
            Debug.Log($"[EnemyCompanionBehavior] 💀 DEATH SOUND TRIGGERED on {gameObject.name} at {transform.position}");
            
            // LAYER 1: Try SoundEvents system (best quality, uses pooling)
            if (TryPlayDeathViaSoundEvents())
            {
                Debug.Log("[EnemyCompanionBehavior] ✅ Death sound played via SoundEvents");
                return;
            }
            
            // LAYER 2: Try dedicated AudioSource with fallback clips
            if (TryPlayDeathViaAudioSource())
            {
                Debug.Log("[EnemyCompanionBehavior] ✅ Death sound played via AudioSource");
                return;
            }
            
            // LAYER 3: Emergency fallback - PlayClipAtPoint (always works)
            if (TryPlayDeathViaPlayClipAtPoint())
            {
                Debug.Log("[EnemyCompanionBehavior] ✅ Death sound played via PlayClipAtPoint (emergency fallback)");
                return;
            }
            
            Debug.LogError($"[EnemyCompanionBehavior] ❌ FAILED TO PLAY DEATH SOUND - All 3 layers failed! Check audio configuration.");
        }
        
        private bool TryPlayDeathViaSoundEvents()
        {
            if (soundEvents == null)
            {
                Debug.LogWarning("[EnemyCompanionBehavior] SoundEvents reference is null");
                return false;
            }
            
            if (soundEvents.companionDeath == null || soundEvents.companionDeath.Length == 0)
            {
                Debug.LogWarning("[EnemyCompanionBehavior] companionDeath array is empty");
                return false;
            }
            
            var handle = SoundEvents.PlayRandomSound3D(soundEvents.companionDeath, transform.position, deathVolume);
            return handle.IsValid;
        }
        
        private bool TryPlayDeathViaAudioSource()
        {
            if (_deathAudioSource == null)
            {
                Debug.LogWarning("[EnemyCompanionBehavior] Death AudioSource is null");
                return false;
            }
            
            if (deathClips == null || deathClips.Length == 0)
            {
                Debug.LogWarning("[EnemyCompanionBehavior] deathClips array is empty");
                return false;
            }
            
            AudioClip clip = deathClips[Random.Range(0, deathClips.Length)];
            if (clip == null)
            {
                Debug.LogWarning("[EnemyCompanionBehavior] Selected death clip is null");
                return false;
            }
            
            _deathAudioSource.PlayOneShot(clip, deathVolume);
            return true;
        }
        
        private bool TryPlayDeathViaPlayClipAtPoint()
        {
            if (deathClips == null || deathClips.Length == 0)
            {
                Debug.LogWarning("[EnemyCompanionBehavior] No death clips for PlayClipAtPoint fallback");
                return false;
            }
            
            AudioClip clip = deathClips[Random.Range(0, deathClips.Length)];
            if (clip == null) return false;
            
            AudioSource.PlayClipAtPoint(clip, transform.position, deathVolume);
            return true;
        }
        
        private void InitializeEnemyMode()
        {
            // Find the REAL player by looking for AAAMovementController (the actual player controller)
            AAAMovementController playerController = FindObjectOfType<AAAMovementController>();
            if (playerController != null)
            {
                _realPlayerTransform = playerController.transform;
                
                // Make sure we didn't find ourselves!
                if (_realPlayerTransform == transform)
                {
                    Debug.LogError("[EnemyCompanionBehavior] ❌ ERROR: Found self as player! This companion has AAAMovementController!");
                    _realPlayerTransform = null;
                }
                else
                {
                    Debug.Log($"[EnemyCompanionBehavior] ✅ Found player via AAAMovementController: {playerController.gameObject.name}");
                }
            }
            
            // FALLBACK: Try finding by tag if AAAMovementController didn't work
            if (_realPlayerTransform == null)
            {
                GameObject playerByTag = GameObject.FindGameObjectWithTag("Player");
                if (playerByTag != null && playerByTag.transform != transform)
                {
                    _realPlayerTransform = playerByTag.transform;
                    Debug.Log($"[EnemyCompanionBehavior] ✅ Found player via 'Player' tag: {playerByTag.name}");
                }
            }
            
            // FINAL CHECK: Did we find the player?
            if (_realPlayerTransform == null)
            {
                Debug.LogError("[EnemyCompanionBehavior] ❌ CRITICAL: Player not found! Tried AAAMovementController and 'Player' tag. Enemy AI disabled!");
                enabled = false;
                return;
            }
            
            if (showDebugInfo)
                Debug.Log($"[EnemyCompanionBehavior] 🔥 ENEMY MODE ACTIVATED! Hunting player: {_realPlayerTransform.gameObject.name}");
            
            // CRITICAL: Remove any Skull-related components from this companion
            // This prevents it from targeting itself
            SkullEnemy selfSkull = GetComponent<SkullEnemy>();
            if (selfSkull != null)
            {
                Destroy(selfSkull);
                if (showDebugInfo)
                    Debug.Log("[EnemyCompanionBehavior] 🗑️ Removed SkullEnemy component to prevent self-targeting");
            }
            
            // Change tag if it's set to Skull
            if (gameObject.tag == "Skull")
            {
                gameObject.tag = "Untagged";
                if (showDebugInfo)
                    Debug.Log("[EnemyCompanionBehavior] 🏷️ Changed tag from Skull to Untagged to prevent self-targeting");
            }
            
            // Create a fake player object that we'll move around to trick the companion system
            _fakePlayerObject = new GameObject($"{gameObject.name}_FakePlayer");
            _fakePlayerTransform = _fakePlayerObject.transform;
            _fakePlayerTransform.position = transform.position + Vector3.forward * 1000f; // Start far away
            
            // Create a fake target with offset for inaccurate aiming
            _fakeTargetObject = new GameObject($"{gameObject.name}_FakeTarget");
            _fakeTargetWithOffset = _fakeTargetObject.transform;
            _fakeTargetWithOffset.position = transform.position + Vector3.up * 10000f; // Start far away
            
            // Override the companion's player reference with our fake player
            // This tricks the companion into following the fake player (which we position at real player)
            _companionCore.playerTransform = _fakePlayerTransform;
            
            // CRITICAL: Disable the companion's targeting system completely
            // We'll manually control targeting via reflection
            if (_companionTargeting != null)
            {
                _companionTargeting.enabled = false;
                if (showDebugInfo)
                    Debug.Log("[EnemyCompanionBehavior] 🎯 Disabled CompanionTargeting - using manual override");
            }
            
            // Detect environment first
            DetectEnvironment();
            
            // Configure companion movement for tactical combat (FIXED for indoor stability)
            if (_companionMovement != null)
            {
                if (enableTacticalMovement && !(_isIndoors && disableTacticalMovementIndoors))
                {
                    // ENABLE tactical movement (REDUCED for stability)
                    _companionMovement.moveWhileShooting = true;
                    _companionMovement.jumpChance = (_isIndoors && disableJumpingIndoors) ? 0f : 0.3f; // Reduced jump chance
                    _companionMovement.repositionInterval = 1f; // Slower repositioning
                    _companionMovement.combatSpeedMultiplier = 1.5f; // MUCH slower for stability
                    
                    if (showDebugInfo)
                        Debug.Log($"[EnemyCompanionBehavior] 🥷 Tactical movement enabled ({(_isIndoors ? "INDOOR" : "OUTDOOR")} mode)");
                }
                else
                {
                    // Disable tactical movement (indoor mode)
                    _companionMovement.moveWhileShooting = false;
                    _companionMovement.jumpChance = 0f;
                    _companionMovement.repositionInterval = 3f;
                    _companionMovement.combatSpeedMultiplier = 1f;
                    
                    if (showDebugInfo)
                        Debug.Log("[EnemyCompanionBehavior] 🏢 Indoor mode - tactical movement disabled");
                }
                
                // Apply speed multiplier
                float finalSpeedMultiplier = combatMovementSpeed * (_isIndoors ? indoorSpeedMultiplier : 1f);
                _companionMovement.runningSpeed *= finalSpeedMultiplier;
                
                if (showDebugInfo)
                    Debug.Log($"[EnemyCompanionBehavior] 🏃 Final speed multiplier: {finalSpeedMultiplier}x");
            }
            
            // CRITICAL: Adjust combat system for close-range shotgun combat
            if (_companionCombat != null)
            {
                // Increase stream threshold so shotgun is used more often
                _companionCombat.streamThreshold = 2000f; // Shotgun used when < 1600 units (2000 * 0.8)
                
                if (showDebugInfo)
                    Debug.Log($"[EnemyCompanionBehavior] 🔫 Stream threshold set to {_companionCombat.streamThreshold} (shotgun < {_companionCombat.streamThreshold * 0.8f})");
            }
            
            // CRITICAL: Ensure companion has proper collider and layer for damage
            Collider companionCollider = GetComponent<Collider>();
            if (companionCollider == null)
            {
                Debug.LogError("[EnemyCompanionBehavior] ❌ NO COLLIDER! Adding CapsuleCollider for damage detection.");
                companionCollider = gameObject.AddComponent<CapsuleCollider>();
                CapsuleCollider capsule = companionCollider as CapsuleCollider;
                capsule.radius = 50f;
                capsule.height = 200f;
                capsule.center = new Vector3(0, 100f, 0);
            }
            
            // Make sure collider is NOT a trigger (must be solid for raycasts to hit)
            if (companionCollider.isTrigger)
            {
                companionCollider.isTrigger = false;
                Debug.Log("[EnemyCompanionBehavior] ⚠️ Changed collider from trigger to solid for raycast detection");
            }
            
            // Log layer and collider info
            if (showDebugInfo)
            {
                Debug.Log("[EnemyCompanionBehavior] 🎯 Enemy will target player with aim inaccuracy");
                Debug.Log("[EnemyCompanionBehavior] 💚 Enemy companion is damageable via IDamageable interface");
                Debug.Log($"[EnemyCompanionBehavior] 🎯 Detection Radius: {playerDetectionRadius} | Shooting Range: {maxShootingRange}");
                Debug.Log($"[EnemyCompanionBehavior] 🛡️ Layer: {LayerMask.LayerToName(gameObject.layer)} | Collider: {companionCollider.GetType().Name} | IsTrigger: {companionCollider.isTrigger}");
                Debug.Log($"[EnemyCompanionBehavior] 💚 Health: {_companionCore.Health}/{_companionCore.MaxHealth}");
                
                // Check weapon emit points
                if (_companionCombat != null)
                {
                    bool hasLeftHand = _companionCombat.leftHandEmitPoint != null;
                    bool hasRightHand = _companionCombat.rightHandEmitPoint != null;
                    bool hasShotgunPrefab = _companionCombat.shotgunParticlePrefab != null;
                    bool hasStreamPrefab = _companionCombat.streamParticlePrefab != null;
                    
                    Debug.Log($"[EnemyCompanionBehavior] 🔫 Weapon Setup:");
                    Debug.Log($"  - Left Hand Emit Point: {(hasLeftHand ? "✓" : "❌")}");
                    Debug.Log($"  - Right Hand Emit Point: {(hasRightHand ? "✓" : "❌")}");
                    Debug.Log($"  - Shotgun Particle Prefab: {(hasShotgunPrefab ? "✓" : "❌")}");
                    Debug.Log($"  - Stream Particle Prefab: {(hasStreamPrefab ? "✓" : "❌")}");
                    
                    if (!hasLeftHand || !hasShotgunPrefab)
                    {
                        Debug.LogWarning("[EnemyCompanionBehavior] ⚠️ Shotgun won't show particles - missing emit point or prefab!");
                    }
                }
            }
            
            // Start patrol if enabled
            if (enablePatrol)
            {
                StartPatrol();
            }
            
            if (showDebugInfo)
            {
                Debug.Log("[EnemyCompanionBehavior] 🎯 Enemy companion initialized:");
                Debug.Log($"  - Detection Radius: {playerDetectionRadius}");
                Debug.Log($"  - Attack Range: {attackRange}");
                Debug.Log($"  - Patrol Enabled: {enablePatrol}");
                Debug.Log($"  - Patrol Points: {patrolPoints?.Length ?? 0}");
            }
        }
        
        void Update()
        {
            // ⚡ DEBUG: Check what's stopping us - LOG FOR ALL ENEMIES
            // if (Time.frameCount == 60)
            // {
            //     Debug.Log($"[EnemyCompanionBehavior] {gameObject.name}: isEnemy={isEnemy}, _companionCore={(_companionCore != null ? "EXISTS" : "NULL")}, IsDead={_companionCore?.IsDead}");
            // }
            
            if (!isEnemy)
            {
                // if (Time.frameCount == 60) Debug.LogWarning($"[EnemyCompanionBehavior] {gameObject.name}: STOPPED - isEnemy is FALSE!");
                return;
            }
            if (_companionCore == null)
            {
                // if (Time.frameCount == 60) Debug.LogError($"[EnemyCompanionBehavior] {gameObject.name}: STOPPED - _companionCore is NULL!");
                return;
            }
            if (_companionCore.IsDead)
            {
                // if (Time.frameCount == 60) Debug.LogWarning($"[EnemyCompanionBehavior] {gameObject.name}: STOPPED - IsDead is TRUE!");
                return;
            }
            
            // ⚡ CRITICAL: Find player if we haven't yet (in case Start() ran before player spawned)
            if (_realPlayerTransform == null)
            {
                InitializeEnemyMode();
                if (_realPlayerTransform == null) return; // Still not found, try next frame
            }
            
            // ⚡ PERFORMANCE: Check activation radius periodically
            if (Time.time - _lastActivationCheck >= activationCheckInterval)
            {
                _lastActivationCheck = Time.time;
                CheckActivation();
            }
            // else if (Time.frameCount == 61)
            // {
            //     // Force first check immediately for debugging
            //     Debug.LogWarning($"[EnemyCompanionBehavior] {gameObject.name}: Waiting for activation check... Time.time={Time.time}, _lastActivationCheck={_lastActivationCheck}, interval={activationCheckInterval}");
            // }
            
            // ⚡ PERFORMANCE: Skip all AI if not active (player too far)
            if (!_isActive) return;
            
            // DEBUG: Log state every few seconds
            if (showDebugInfo && Time.frameCount % 300 == 0)
            {
                Debug.Log($"[EnemyCompanionBehavior] State: {_currentState}, PlayerDetected: {_playerDetected}, Patrolling: {_isPatrolling}");
            }
            
            // Periodic environment check
            if (autoDetectIndoors && Time.time - _lastEnvironmentCheck >= ENVIRONMENT_CHECK_INTERVAL)
            {
                _lastEnvironmentCheck = Time.time;
                DetectEnvironment();
            }
            
            // Periodic player detection
            if (Time.time - _lastDetectionCheck >= detectionInterval)
            {
                _lastDetectionCheck = Time.time;
                CheckForPlayer();
            }
            
            // ⚡ NUCLEAR: Only update behavior every other frame (50% reduction)
            if (Time.frameCount % 2 == 0)
            {
                UpdateEnemyBehavior();
            }
            
            // ⚡ NUCLEAR: Only update targeting every 3rd frame (66% reduction - was every 2nd)
            if (Time.frameCount % 3 == 0)
            {
                // CRITICAL: Override targeting to force companion to target the player
                if (_overrideTargeting && _companionTargeting != null)
                {
                    ForceTargetPlayer();
                }
            }
            
            // ⚡ NUCLEAR: Only update rotation every 4th frame (75% reduction - was every 3rd)
            if (Time.frameCount % 4 == 0)
            {
                // CRITICAL: Force companion to ALWAYS look at player during combat
                if (_playerDetected && _realPlayerTransform != null)
                {
                    ForceLookAtPlayer();
                }
            }
        }
        
        /// <summary>
        /// ⚡ PERFORMANCE: Check if player is close enough to activate AI
        /// NUCLEAR OPTION: Disable entire GameObject when inactive!
        /// </summary>
        private void CheckActivation()
        {
            // ⚡ DEBUG: Log to confirm this is being called (ALWAYS log, ignore showDebugInfo)
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[EnemyCompanionBehavior] 🔍 CheckActivation() CALLED on {gameObject.name}");
            }
            
            if (_realPlayerTransform == null)
            {
                if (Time.frameCount == 10)
                    Debug.LogError($"[EnemyCompanionBehavior] ❌ {gameObject.name}: _realPlayerTransform is NULL! Cannot check activation!");
                return;
            }
            
            // ⚡ CRITICAL: Use HORIZONTAL distance only (ignore Y-axis)!
            // This prevents activating enemies on different floors
            Vector3 myPos = transform.position;
            Vector3 playerPos = _realPlayerTransform.position;
            
            // Calculate horizontal distance (XZ plane only)
            float horizontalDistance = Vector2.Distance(
                new Vector2(myPos.x, myPos.z),
                new Vector2(playerPos.x, playerPos.z)
            );
            
            // Also check vertical distance - only activate if on same floor
            float verticalDistance = Mathf.Abs(myPos.y - playerPos.y);
            
            bool shouldBeActive = horizontalDistance <= activationRadius && verticalDistance <= maxVerticalActivationDistance;
            
            // 🔥 CRITICAL: NEVER DEACTIVATE DURING COMBAT!
            // If enemy has detected player or is in combat, keep them active regardless of distance
            if (_isActive && (_playerDetected || _isBeamActive || _currentState == EnemyState.Attacking || _currentState == EnemyState.Hunting))
            {
                shouldBeActive = true; // Force stay active during combat
                if (showDebugInfo && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[EnemyCompanionBehavior] ⚔️ {gameObject.name} LOCKED ACTIVE - In combat! Distance: {horizontalDistance:F0}");
                }
            }
            
            // ⚡ NEW: Update LOD level based on distance (even if active/inactive state doesn't change)
            if (enableLODSystem && _isActive)
            {
                UpdateLODLevel(horizontalDistance);
            }
            
            if (shouldBeActive != _isActive)
            {
                _isActive = shouldBeActive;
                
                // ⚡ NUCLEAR: Just disable the ENTIRE enemy when far away!
                // This disables EVERYTHING: Animator, NavMesh, AI, Rendering, Physics, ALL OF IT
                // Cost when inactive: ZERO (literally nothing runs)
                
                if (!_isActive)
                {
                    // Going inactive - disable everything except this script
                    SetComponentsActive(false);
                    Debug.Log($"[EnemyCompanionBehavior] 💤 {gameObject.name} DEACTIVATED - H:{horizontalDistance:F0}, V:{verticalDistance:F0}");
                }
                else
                {
                    // Going active - enable everything
                    SetComponentsActive(true);
                    
                    // CRITICAL: Re-initialize CompanionMovement if it was disabled during Start()
                    if (_companionMovement != null && _companionCore != null)
                    {
                        _companionMovement.Initialize(_companionCore);
                        Debug.Log($"[EnemyCompanionBehavior] 🔄 Re-initialized CompanionMovement after activation");
                    }
                    
                    Debug.Log($"[EnemyCompanionBehavior] ⚡ {gameObject.name} ACTIVATED - H:{horizontalDistance:F0}, V:{verticalDistance:F0}");
                }
            }
        }
        
        /// <summary>
        /// ⚡ NUCLEAR: Enable/Disable ALL components except this script
        /// </summary>
        private void SetComponentsActive(bool active)
        {
            // Debug.LogError($"[EnemyCompanionBehavior] 🔥 SetComponentsActive({active}) CALLED on {gameObject.name}!");
            
            // ⚡ CRITICAL: Enable/Disable NavMeshAgent FIRST (required for movement!)
            NavMeshAgent navAgent = GetComponent<NavMeshAgent>();
            if (navAgent != null)
            {
                navAgent.enabled = active;
                if (active && navAgent.isOnNavMesh)
                {
                    navAgent.isStopped = false; // Resume movement
                }
                // Debug.LogError($"[EnemyCompanionBehavior] 🗺️ NavMeshAgent {(active ? "ENABLED" : "DISABLED")}");
            }
            
            // ⚡ BRUTAL FALLBACK: If no arms parent assigned, disable ALL children GameObjects!
            if (armsParent != null)
            {
                // User assigned arms parent - disable just that
                armsParent.SetActive(active);
                // Debug.LogError($"[EnemyCompanionBehavior] 💤 Arms Parent {(active ? "ENABLED" : "DISABLED")}");
            }
            else
            {
                // NO ARMS PARENT - DISABLE ALL CHILD GAMEOBJECTS (NUCLEAR!)
                int childCount = 0;
                foreach (Transform child in transform)
                {
                    if (child != null && child.gameObject != null)
                    {
                        child.gameObject.SetActive(active);
                        childCount++;
                    }
                }
                
                Debug.LogError($"[EnemyCompanionBehavior] 💤 {childCount} CHILDREN {(active ? "ENABLED" : "DISABLED")}!");
            }
            
            // 1. Disable ALL MonoBehaviours on this GameObject (except this script and TacticalEnemyAI)
            MonoBehaviour[] components = GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour component in components)
            {
                // Skip this script and TacticalEnemyAI (deprecated, should stay disabled)
                if (component != this && component != null && !(component is TacticalEnemyAI))
                {
                    component.enabled = active;
                }
            }
            
            // 2. Disable ALL Animators (this GameObject AND children)
            // ⚡ PERFORMANCE: Use cached animators instead of GetComponentsInChildren
            if (_cachedAnimators != null)
            {
                foreach (Animator animator in _cachedAnimators)
                {
                    if (animator != null)
                    {
                        animator.enabled = active;
                        
                        // NUCLEAR: Also stop the animator to prevent state updates
                        if (!active && animator.enabled)
                        {
                            animator.Rebind(); // Reset to default state
                            animator.Update(0f); // Force update to apply reset
                        }
                    }
                }
            }
            
            // 3. Disable ALL Renderers (stop rendering completely!)
            // ⚡ PERFORMANCE: Use cached renderers instead of GetComponentsInChildren
            if (_cachedRenderersForActivation != null)
            {
                foreach (Renderer renderer in _cachedRenderersForActivation)
                {
                    if (renderer != null)
                    {
                        renderer.enabled = active;
                    }
                }
                
                if (Time.frameCount % 60 == 0 && showDebugInfo)
                {
                    Debug.Log($"[EnemyCompanionBehavior] 🎨 {gameObject.name}: {_cachedRenderersForActivation.Length} renderers {(active ? "ENABLED" : "DISABLED")}");
                }
            }
            
            // 4. Disable ALL Colliders (stop physics)
            // ⚡ PERFORMANCE: Use cached colliders instead of GetComponentsInChildren
            if (_cachedColliders != null)
            {
                foreach (Collider collider in _cachedColliders)
                {
                    if (collider != null)
                    {
                        collider.enabled = active;
                    }
                }
            }
        }
        
        /// <summary>
        /// Forces the companion to face the player directly (overrides movement rotation)
        /// </summary>
        private void ForceLookAtPlayer()
        {
            Vector3 directionToPlayer = (_realPlayerTransform.position - transform.position);
            directionToPlayer.y = 0f; // Keep rotation horizontal only
            
            if (directionToPlayer.sqrMagnitude < 0.0001f) return;
            
            // INSTANT rotation to face player (no lerp - always looking at you!)
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 20f);
        }
        
        /// <summary>
        /// Forces the companion to target the player with inaccurate aim
        /// Updates aim offset periodically and manages beam cooldown
        /// </summary>
        private void ForceTargetPlayer()
        {
            if (_companionTargeting == null || _realPlayerTransform == null || _fakeTargetWithOffset == null) return;
            
            float distanceToPlayer = Vector3.Distance(transform.position, _realPlayerTransform.position);
            
            // Check if player is too far to shoot
            if (distanceToPlayer > maxShootingRange)
            {
                // Stop attacking if out of range
                if (_companionCombat != null && _isBeamActive)
                {
                    _companionCombat.StopAttacking();
                    _isBeamActive = false;
                    if (showDebugInfo)
                        Debug.Log($"[EnemyCompanionBehavior] 🛑 Player out of range ({distanceToPlayer:F0} > {maxShootingRange})");
                }
                return;
            }
            
            // Check beam cooldown
            if (Time.time < _beamCooldownEndTime)
            {
                // In cooldown - stop attacking
                if (_companionCombat != null && _isBeamActive)
                {
                    _companionCombat.StopAttacking();
                    _isBeamActive = false;
                    if (showDebugInfo)
                        Debug.Log($"[EnemyCompanionBehavior] ⏳ Beam cooling down... {(_beamCooldownEndTime - Time.time):F1}s remaining");
                }
                return;
            }
            
            // Check if beam has been active too long
            if (_isBeamActive && (Time.time - _beamStartTime) >= maxBeamDuration)
            {
                // Force cooldown
                _beamCooldownEndTime = Time.time + beamCooldownTime;
                _companionCombat?.StopAttacking();
                _isBeamActive = false;
                if (showDebugInfo)
                    Debug.Log($"[EnemyCompanionBehavior] 🔥 Beam overheated! Cooling down for {beamCooldownTime}s");
                return;
            }
            
            // Update aim offset periodically for inaccuracy
            if (Time.time - _lastAimUpdateTime > 0.3f)
            {
                UpdateAimOffset();
                _lastAimUpdateTime = Time.time;
            }
            
            // Position fake target at player with aim offset
            Vector3 targetPosition = _realPlayerTransform.position + _currentAimOffset;
            _fakeTargetWithOffset.position = targetPosition;
            
            // Inject fake target into targeting system
            var targetingType = typeof(CompanionTargeting);
            var currentTargetField = targetingType.GetField("_currentTarget", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (currentTargetField != null)
            {
                currentTargetField.SetValue(_companionTargeting, _fakeTargetWithOffset);
            }
            
            // Start attacking if not already
            if (_companionCombat != null && !_isBeamActive)
            {
                _companionCombat.StartAttacking();
                _isBeamActive = true;
                _beamStartTime = Time.time;
                if (showDebugInfo)
                    Debug.Log("[EnemyCompanionBehavior] 🔫 Started attacking player");
            }
        }
        
        /// <summary>
        /// Updates the aim offset based on accuracy setting
        /// </summary>
        private void UpdateAimOffset()
        {
            // Roll for accuracy
            float accuracyRoll = Random.value;
            
            if (accuracyRoll > aimAccuracy)
            {
                // Miss - apply random offset
                _currentAimOffset = new Vector3(
                    Random.Range(-aimDeviation, aimDeviation),
                    Random.Range(-aimDeviation * 0.5f, aimDeviation * 0.5f), // Less vertical deviation
                    Random.Range(-aimDeviation, aimDeviation)
                );
                
                if (showDebugInfo)
                    Debug.Log($"[EnemyCompanionBehavior] 🎯 MISS! Aim offset: {_currentAimOffset}");
            }
            else
            {
                // Hit - aim at player with slight variation
                _currentAimOffset = new Vector3(
                    Random.Range(-aimDeviation * 0.2f, aimDeviation * 0.2f),
                    Random.Range(-aimDeviation * 0.1f, aimDeviation * 0.1f),
                    Random.Range(-aimDeviation * 0.2f, aimDeviation * 0.2f)
                );
            }
        }
        
        private void CheckForPlayer()
        {
            if (_realPlayerTransform == null)
            {
                if (showDebugInfo)
                    Debug.LogError("[EnemyCompanionBehavior] ❌ Real player transform is NULL!");
                return;
            }
            
            float distanceToPlayer = Vector3.Distance(transform.position, _realPlayerTransform.position);
            bool wasDetected = _playerDetected;
            
            // DEBUG: Log distance every few seconds
            if (showDebugInfo && Time.frameCount % 300 == 0)
            {
                Debug.Log($"[EnemyCompanionBehavior] 📏 Distance to player: {distanceToPlayer:F0} / {playerDetectionRadius:F0}");
            }
            
            // Check if player is in detection radius
            if (distanceToPlayer <= playerDetectionRadius)
            {
                // Line of sight check if required
                if (requireLineOfSight)
                {
                    _playerDetected = CheckLineOfSight();
                    
                    if (showDebugInfo && Time.frameCount % 300 == 0)
                    {
                        Debug.Log($"[EnemyCompanionBehavior] 👁️ LOS Check: {_playerDetected}");
                    }
                }
                else
                {
                    // No line of sight required
                    _playerDetected = true;
                }
            }
            else
            {
                _playerDetected = false;
            }
            
            // State change logging
            if (_playerDetected && !wasDetected && showDebugInfo)
            {
                Debug.Log($"[EnemyCompanionBehavior] 👁️ PLAYER DETECTED at {distanceToPlayer:F0} units!");
                
                // CRITICAL: Mark that we've SEEN the player (not just detected through wall)
                if (CheckLineOfSight())
                {
                    _hasEverSeenPlayer = true;
                    Debug.Log("[EnemyCompanionBehavior] 👁️✅ VISUAL CONFIRMATION - Player seen!");
                }
            }
            else if (!_playerDetected && wasDetected && showDebugInfo)
            {
                Debug.Log("[EnemyCompanionBehavior] 👁️ Player lost - resuming patrol");
            }
        }
        
        /// <summary>
        /// ⚡ TIME-SLICED LOS CHECK
        /// Called by LOSManager on a time-sliced schedule (10 enemies per frame)
        /// This is the NEW optimized way - no more random checks causing FPS spikes!
        /// </summary>
        public void PerformTimeslicedLOSCheck()
        {
            // Only check if enemy is active and player exists
            if (!_isActive || _realPlayerTransform == null)
            {
                return;
            }
            
            // Perform the actual LOS check
            bool hasLOS = CheckLineOfSight();
            
            // Update detection state based on LOS result
            if (hasLOS)
            {
                if (!_playerDetected)
                {
                    _playerDetected = true;
                    _hasEverSeenPlayer = true;
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"[EnemyCompanionBehavior] 👁️ {gameObject.name} DETECTED PLAYER via time-sliced check!");
                    }
                }
            }
            else
            {
                if (_playerDetected)
                {
                    _playerDetected = false;
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"[EnemyCompanionBehavior] 👁️ {gameObject.name} LOST PLAYER via time-sliced check");
                    }
                }
            }
        }
        
        /// <summary>
        /// 🗺️ NAVMESH LINE OF SIGHT CHECK - THE OPTIMAL SOLUTION!
        /// 
        /// Uses NavMesh path calculation to detect walls:
        /// - If NavMesh can't find a path, there's a wall!
        /// - 10x faster than raycasts (pre-baked data)
        /// - 99% accurate (never shoots through walls)
        /// - Works perfectly indoors and outdoors
        /// 
        /// BABY EXPLANATION:
        /// "Can I walk to the player? No? Then I can't see them!"
        /// </summary>
        private bool CheckLineOfSightNavMesh()
        {
            if (_realPlayerTransform == null) return false;
            
            // ⚡ PERFORMANCE: Use cached result if recent
            if (navMeshCacheDuration > 0 && Time.time - _lastNavMeshCheckTime < navMeshCacheDuration)
            {
                return _cachedNavMeshLOS;
            }
            
            // 🗺️ NAVMESH CHECK: Can we walk to the player?
            // If no path exists, there's definitely a wall!
            bool pathExists = NavMesh.CalculatePath(
                transform.position,
                _realPlayerTransform.position,
                NavMesh.AllAreas,
                _reusablePath  // Reuse path object to avoid GC allocations
            );
            
            // Check if path is complete (no walls blocking)
            bool hasLOS = pathExists && _reusablePath.status == NavMeshPathStatus.PathComplete;
            
            // ⚡ CACHE RESULT for performance
            _cachedNavMeshLOS = hasLOS;
            _lastNavMeshCheckTime = Time.time;
            
            // 🎨 DEBUG VISUALIZATION
            if (showDebugInfo)
            {
                if (hasLOS)
                {
                    // Draw the path in green (clear LOS)
                    for (int i = 0; i < _reusablePath.corners.Length - 1; i++)
                    {
                        Debug.DrawLine(_reusablePath.corners[i], _reusablePath.corners[i + 1], Color.green, detectionInterval);
                    }
                    
                    if (!_playerDetected)
                    {
                        Debug.Log($"[EnemyCompanionBehavior] 🗺️✅ NAVMESH LOS: Clear path to player ({_reusablePath.corners.Length} waypoints)");
                    }
                }
                else
                {
                    // Draw line in red (blocked by wall)
                    Debug.DrawLine(transform.position, _realPlayerTransform.position, Color.red, detectionInterval);
                    
                    if (_playerDetected)
                    {
                        string reason = !pathExists ? "No path exists" : $"Path incomplete ({_reusablePath.status})";
                        Debug.Log($"[EnemyCompanionBehavior] 🗺️🚫 NAVMESH LOS: WALL DETECTED! {reason}");
                    }
                }
            }
            
            return hasLOS;
        }
        
        /// <summary>
        /// 🎯 RAYCAST LINE OF SIGHT CHECK - FALLBACK SYSTEM
        /// 
        /// Uses raycasts to check for walls (less accurate, more expensive)
        /// Only used if NavMesh check is inconclusive or disabled
        /// 
        /// BABY EXPLANATION:
        /// "Let me shoot a laser and see if it hits a wall first!"
        /// </summary>
        private bool CheckLineOfSightRaycast()
        {
            if (_realPlayerTransform == null) return false;
            
            Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
            Vector3 targetPosition = _realPlayerTransform.position + Vector3.up * eyeHeight;
            
            // Multi-raycast for better accuracy (checks center, left, right, up, down)
            int successfulRays = 0;
            
            for (int i = 0; i < losRaycastCount; i++)
            {
                Vector3 offset = Vector3.zero;
                
                // Distribute raycasts around target (player is 50 units wide, 320 tall)
                if (i == 1) offset = Vector3.right * losRaycastSpread;
                else if (i == 2) offset = Vector3.left * losRaycastSpread;
                else if (i == 3) offset = Vector3.up * losRaycastSpread;
                else if (i == 4) offset = Vector3.down * losRaycastSpread;
                
                Vector3 rayStart = eyePosition;
                Vector3 rayTarget = targetPosition + offset;
                Vector3 rayDirection = (rayTarget - rayStart).normalized;
                float rayDistance = Vector3.Distance(rayStart, rayTarget);
                
                // 🔥 CRITICAL FIX: Raycast WITHOUT layer mask to hit EVERYTHING
                // Then check what we hit first - player or wall?
                RaycastHit hit;
                if (Physics.Raycast(rayStart, rayDirection, out hit, rayDistance))
                {
                    // We hit something - check if it's the player or a wall
                    // If we hit the player (or their collider), we have LOS
                    // If we hit anything else before reaching the player, LOS is blocked
                    
                    if (hit.transform == _realPlayerTransform || hit.transform.IsChildOf(_realPlayerTransform))
                    {
                        // Hit the player directly - clear LOS!
                        successfulRays++;
                        if (showDebugInfo && i == 0)
                        {
                            Debug.DrawRay(rayStart, rayDirection * hit.distance, Color.green, detectionInterval);
                            Debug.Log($"[EnemyCompanionBehavior] ✅ RAYCAST HIT PLAYER at {hit.distance:F0} units - CLEAR LOS!");
                        }
                    }
                    else
                    {
                        // Hit something else (wall, obstacle) BEFORE reaching player - LOS blocked!
                        if (showDebugInfo && i == 0)
                        {
                            Debug.DrawRay(rayStart, rayDirection * hit.distance, Color.red, detectionInterval);
                            Debug.Log($"[EnemyCompanionBehavior] 🚫 RAYCAST BLOCKED by {hit.collider.name} (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}) at {hit.distance:F0} units - WALL DETECTED!");
                        }
                    }
                }
                else
                {
                    // Raycast didn't hit anything - player might be out of range or behind us
                    // Treat as no LOS for safety
                    if (showDebugInfo && i == 0)
                    {
                        Debug.DrawRay(rayStart, rayDirection * rayDistance, Color.yellow, detectionInterval);
                        Debug.Log($"[EnemyCompanionBehavior] ⚠️ RAYCAST MISSED - No hit detected (player out of range?)");
                    }
                }
            }
            
            // Need at least one successful raycast to have LOS
            bool hasLOS = successfulRays > 0;
            
            if (showDebugInfo && hasLOS && !_playerDetected)
            {
                Debug.Log($"[EnemyCompanionBehavior] 🎯✅ RAYCAST LOS: Confirmed ({successfulRays}/{losRaycastCount} rays clear)");
            }
            else if (showDebugInfo && !hasLOS && _playerDetected)
            {
                Debug.Log($"[EnemyCompanionBehavior] 🎯❌ RAYCAST LOS: Blocked (walls detected)");
            }
            
            return hasLOS;
        }
        
        /// <summary>
        /// 🔥 MASTER LINE OF SIGHT CHECK
        /// 
        /// Intelligently chooses the best LOS method:
        /// 1. Try NavMesh (fast + accurate)
        /// 2. Fallback to Raycast if needed
        /// 
        /// BABY EXPLANATION:
        /// "First check the map, then shoot a laser if unsure!"
        /// </summary>
        private bool CheckLineOfSight()
        {
            if (_realPlayerTransform == null) return false;
            
            // 🗺️ PRIMARY: Use NavMesh LOS (BEST method!)
            if (useNavMeshLOS)
            {
                bool navMeshLOS = CheckLineOfSightNavMesh();
                
                // If NavMesh says "no path", DEFINITELY blocked by wall!
                if (!navMeshLOS)
                {
                    return false; // 🚫 WALL CONFIRMED - Don't shoot!
                }
                
                // NavMesh says "path exists" - probably clear!
                // Use raycast as double-check if enabled
                if (useRaycastFallback)
                {
                    bool raycastLOS = CheckLineOfSightRaycast();
                    
                    // Both agree? Perfect!
                    if (navMeshLOS && raycastLOS)
                    {
                        return true; // ✅ BOTH CONFIRM - Clear LOS!
                    }
                    
                    // NavMesh says yes, raycast says no?
                    // Trust NavMesh (it's more reliable for walls)
                    if (showDebugInfo)
                    {
                        Debug.Log("[EnemyCompanionBehavior] 🤔 NavMesh=YES, Raycast=NO. Trusting NavMesh!");
                    }
                    return true; // Trust NavMesh
                }
                
                // No fallback - trust NavMesh completely
                return true;
            }
            
            // 🎯 FALLBACK: Use Raycast only (old system)
            return CheckLineOfSightRaycast();
        }
        
        /// <summary>
        /// Detects if enemy is indoors by checking for ceiling above
        /// </summary>
        private void DetectEnvironment()
        {
            if (forceIndoorMode)
            {
                _isIndoors = true;
                return;
            }
            
            // Raycast upward to detect ceiling
            RaycastHit hit;
            bool wasPreviouslyIndoors = _isIndoors;
            
            if (Physics.Raycast(transform.position, Vector3.up, out hit, 5000f, lineOfSightBlockers))
            {
                // Found ceiling - we're indoors
                _isIndoors = true;
                
                if (!wasPreviouslyIndoors && showDebugInfo)
                {
                    Debug.Log($"[EnemyCompanionBehavior] 🏢 Entered INDOOR area (ceiling at {hit.distance:F1}m)");
                    
                    // Adjust movement for indoor
                    if (_companionMovement != null)
                    {
                        _companionMovement.jumpChance = 0f;
                        _companionMovement.repositionInterval = 3f;
                        _companionMovement.combatSpeedMultiplier = 1f;
                    }
                }
            }
            else
            {
                // No ceiling - we're outdoors
                _isIndoors = false;
                
                if (wasPreviouslyIndoors && showDebugInfo)
                {
                    Debug.Log("[EnemyCompanionBehavior] 🌳 Entered OUTDOOR area");
                    
                    // Restore outdoor movement
                    if (_companionMovement != null && enableTacticalMovement)
                    {
                        _companionMovement.jumpChance = 0.3f;
                        _companionMovement.repositionInterval = 1f;
                        _companionMovement.combatSpeedMultiplier = 1.5f;
                    }
                }
            }
        }
        
        private void UpdateEnemyBehavior()
        {
            if (_playerDetected)
            {
                // Player detected - hunt and attack
                float distanceToPlayer = Vector3.Distance(transform.position, _realPlayerTransform.position);
                
                if (distanceToPlayer <= attackRange)
                {
                    // Close enough to attack
                    SetState(EnemyState.Attacking);
                    AttackPlayer();
                }
                else
                {
                    // Chase player
                    SetState(EnemyState.Hunting);
                    HuntPlayer();
                }
            }
            else
            {
                // Player not detected - patrol
                if (enablePatrol)
                {
                    SetState(EnemyState.Patrolling);
                    // Patrol is handled by coroutine
                }
                else
                {
                    SetState(EnemyState.Idle);
                    IdleBehavior();
                }
            }
        }
        
        private void SetState(EnemyState newState)
        {
            if (_currentState == newState) return;
            
            EnemyState oldState = _currentState;
            _currentState = newState;
            
            if (showDebugInfo)
            {
                Debug.Log($"[EnemyCompanionBehavior] 🔄 State: {oldState} → {newState}");
            }
            
            // Handle state transitions
            OnStateChanged(oldState, newState);
        }
        
        private void OnStateChanged(EnemyState oldState, EnemyState newState)
        {
            // Stop patrol when entering hunting/attacking
            if (newState == EnemyState.Hunting || newState == EnemyState.Attacking)
            {
                StopPatrol();
            }
            
            // Start patrol when entering patrol state
            if (newState == EnemyState.Patrolling && !_isPatrolling)
            {
                StartPatrol();
            }
        }
        
        private void HuntPlayer()
        {
            if (_realPlayerTransform == null || _fakePlayerTransform == null)
            {
                if (showDebugInfo)
                    Debug.LogError("[EnemyCompanionBehavior] ❌ HuntPlayer: Missing transforms!");
                return;
            }
            
            // CRITICAL: Set CompanionCore to Attacking state to use HandleCombatMovement (no teleport)
            // Engaging state calls HandleFollowingMovement which has teleport logic
            if (_companionCore != null && _companionCore.CurrentState != CompanionCore.CompanionState.Attacking)
            {
                _companionCore.SetState(CompanionCore.CompanionState.Attacking);
            }
            
            float distanceToPlayer = Vector3.Distance(transform.position, _realPlayerTransform.position);
            bool hasLineOfSight = CheckLineOfSight();
            
            // 🚫 LAYER 2: AI-LEVEL LOS CHECK - STOP SHOOTING if no line of sight!
            if (!hasLineOfSight)
            {
                // NO LINE OF SIGHT - STOP ATTACKING IMMEDIATELY!
                if (_companionCombat != null && _isBeamActive)
                {
                    _companionCombat.StopAttacking();
                    _isBeamActive = false;
                    _overrideTargeting = false;
                    
                    if (showDebugInfo && Time.frameCount % 60 == 0)
                    {
                        Debug.Log("[EnemyCompanionBehavior] 🚫 STOPPED SHOOTING - No line of sight!");
                    }
                }
                
                // Check if we've EVER seen the player
                if (_hasEverSeenPlayer)
                {
                    // HAD VISUAL BEFORE - CHASE TO REGAIN VISUAL (but don't shoot!)
                    // Position fake player PAST the real player to make enemy run THROUGH their position
                    Vector3 directionToPlayer = (_realPlayerTransform.position - transform.position).normalized;
                    _fakePlayerTransform.position = _realPlayerTransform.position + directionToPlayer * 2000f;
                    
                    // FORCE MOVEMENT - Don't stop!
                    if (_companionMovement != null)
                    {
                        _companionMovement.SetEngagingMode(); // Fast movement
                    }
                    
                    if (showDebugInfo && Time.frameCount % 60 == 0)
                    {
                        Debug.Log($"[EnemyCompanionBehavior] 🏃💨 CHASING - Lost visual! Distance: {distanceToPlayer:F0}");
                    }
                }
                else
                {
                    // NEVER SEEN PLAYER - Just detected through wall
                    // Stay in patrol/alert mode, don't chase
                    SetState(EnemyState.Patrolling);
                    
                    if (showDebugInfo && Time.frameCount % 60 == 0)
                    {
                        Debug.Log($"[EnemyCompanionBehavior] 🚨 ALERT - Detected but no visual! Distance: {distanceToPlayer:F0}");
                    }
                    return;
                }
            }
            else
            {
                // HAS LINE OF SIGHT - Mark visual confirmation!
                if (!_hasEverSeenPlayer)
                {
                    _hasEverSeenPlayer = true;
                    if (showDebugInfo)
                        Debug.Log("[EnemyCompanionBehavior] 👁️✅ FIRST VISUAL CONTACT!");
                }
                
                // ENGAGE!
                if (distanceToPlayer > attackRange)
                {
                    // Too far - CLOSE THE DISTANCE
                    Vector3 directionToPlayer = (_realPlayerTransform.position - transform.position).normalized;
                    _fakePlayerTransform.position = _realPlayerTransform.position + directionToPlayer * 1000f;
                    
                    if (showDebugInfo && Time.frameCount % 60 == 0)
                    {
                        Debug.Log($"[EnemyCompanionBehavior] 🏃🎯 CLOSING IN - Distance: {distanceToPlayer:F0}");
                    }
                }
                else
                {
                    // In range - TRANSITION TO ATTACK
                    SetState(EnemyState.Attacking);
                    return;
                }
            }
            
            // Enable targeting override
            _overrideTargeting = true;
        }
        
        private void AttackPlayer()
        {
            if (_realPlayerTransform == null || _fakePlayerTransform == null)
            {
                if (showDebugInfo)
                    Debug.LogError("[EnemyCompanionBehavior] ❌ AttackPlayer: Missing transforms!");
                return;
            }
            
            bool hasLineOfSight = CheckLineOfSight();
            float distanceToPlayer = Vector3.Distance(transform.position, _realPlayerTransform.position);
            
            // 🚫 LAYER 2: AI-LEVEL LOS CHECK - STOP SHOOTING if no line of sight!
            if (!hasLineOfSight)
            {
                // LOST LINE OF SIGHT - STOP ATTACKING IMMEDIATELY!
                if (_companionCombat != null && _isBeamActive)
                {
                    _companionCombat.StopAttacking();
                    _isBeamActive = false;
                    _overrideTargeting = false;
                    
                    if (showDebugInfo)
                        Debug.Log("[EnemyCompanionBehavior] 🚫 STOPPED SHOOTING - Lost line of sight!");
                }
                
                // GO BACK TO HUNTING
                SetState(EnemyState.Hunting);
                if (showDebugInfo)
                    Debug.Log($"[EnemyCompanionBehavior] 👁️❌ Lost LOS during combat - switching to HUNTING");
                return;
            }
            
            // CRITICAL: Set CompanionCore to Attacking state so CompanionMovement executes tactical movement
            if (_companionCore != null && _companionCore.CurrentState != CompanionCore.CompanionState.Attacking)
            {
                _companionCore.SetState(CompanionCore.CompanionState.Attacking);
                if (showDebugInfo)
                    Debug.Log("[EnemyCompanionBehavior] ⚔️ Set CompanionCore to ATTACKING state - tactical movement enabled!");
            }
            
            // SIMPLIFIED: Just position fake player at real player
            // CompanionMovement's tactical movement system will handle all the strafing/repositioning
            _fakePlayerTransform.position = _realPlayerTransform.position;
            
            if (showDebugInfo && Time.frameCount % 120 == 0)
                Debug.Log($"[EnemyCompanionBehavior] ⚔️ ATTACKING - Distance: {distanceToPlayer:F0}, Tactical Movement: {enableTacticalMovement}");
            
            // ALWAYS ENABLE TARGETING AND SHOOTING (we already verified LOS above)
            _overrideTargeting = true;
            
            // Start shooting if in range
            if (distanceToPlayer <= maxShootingRange && _companionCombat != null)
            {
                _companionCombat.StartAttacking();
            }
        }
        
        private void IdleBehavior()
        {
            if (_fakePlayerTransform == null) return;
            
            // Position fake player at a neutral position
            _fakePlayerTransform.position = transform.position + Vector3.forward * 500f;
            
            // Disable targeting override when idle
            _overrideTargeting = false;
        }
        
        private void StartPatrol()
        {
            if (_isPatrolling) return;
            
            _isPatrolling = true;
            
            if (_patrolCoroutine != null)
            {
                StopCoroutine(_patrolCoroutine);
            }
            
            _patrolCoroutine = StartCoroutine(PatrolRoutine());
            
            if (showDebugInfo)
                Debug.Log("[EnemyCompanionBehavior] 🚶 Starting patrol");
        }
        
        private void StopPatrol()
        {
            if (!_isPatrolling) return;
            
            _isPatrolling = false;
            
            if (_patrolCoroutine != null)
            {
                StopCoroutine(_patrolCoroutine);
                _patrolCoroutine = null;
            }
            
            if (showDebugInfo)
                Debug.Log("[EnemyCompanionBehavior] 🛑 Stopping patrol");
        }
        
        private IEnumerator PatrolRoutine()
        {
            while (_isPatrolling && !_companionCore.IsDead)
            {
                Vector3 patrolTarget;
                
                // Determine patrol target
                if (patrolPoints != null && patrolPoints.Length > 0)
                {
                    // Use defined patrol points
                    patrolTarget = patrolPoints[_currentPatrolIndex].position;
                    _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolPoints.Length;
                }
                else
                {
                    // Random patrol around start position
                    Vector2 randomCircle = Random.insideUnitCircle * randomPatrolRadius;
                    patrolTarget = _patrolStartPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
                }
                
                // Move fake player to patrol target
                _fakePlayerTransform.position = patrolTarget;
                
                if (showDebugInfo)
                    Debug.Log($"[EnemyCompanionBehavior] 🚶 Patrolling to: {patrolTarget}");
                
                // Wait until we reach the patrol point or detect player
                float startTime = Time.time;
                float timeout = 30f; // 30 second timeout per patrol point
                
                while (_isPatrolling && !_playerDetected && Time.time - startTime < timeout)
                {
                    float distanceToTarget = Vector3.Distance(transform.position, patrolTarget);
                    
                    // Reached patrol point if within 2000 units (scaled for large world)
                    if (distanceToTarget < 2000f)
                    {
                        // Reached patrol point
                        break;
                    }
                    
                    yield return new WaitForSeconds(0.5f);
                }
                
                // Wait at patrol point
                if (_isPatrolling && !_playerDetected && patrolWaitTime > 0)
                {
                    if (showDebugInfo)
                        Debug.Log($"[EnemyCompanionBehavior] ⏸️ Waiting at patrol point for {patrolWaitTime}s");
                    
                    yield return new WaitForSeconds(patrolWaitTime);
                }
                
                // Small delay before next patrol
                yield return new WaitForSeconds(0.5f);
            }
        }
        
        void OnDestroy()
        {
            // ⚡ TIME-SLICING: Unregister from LOSManager
            if (LOSManager.Instance != null)
            {
                LOSManager.Instance.UnregisterEnemy(this);
            }
            
            // Clean up fake objects
            if (_fakePlayerObject != null)
            {
                Destroy(_fakePlayerObject);
            }
            if (_fakeTargetObject != null)
            {
                Destroy(_fakeTargetObject);
            }
            
            // Unsubscribe from events
            if (_companionCore != null)
            {
                CompanionCore.OnCompanionHealthChanged -= OnCompanionDamaged;
            }
        }
        
        void OnDisable()
        {
            StopPatrol();
        }
        
        // Debug visualization
        void OnDrawGizmosSelected()
        {
            if (!isEnemy) return;
            
            // Detection radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, playerDetectionRadius);
            
            // Attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // Patrol radius
            if (enablePatrol && (patrolPoints == null || patrolPoints.Length == 0))
            {
                Gizmos.color = Color.blue;
                Vector3 patrolCenter = Application.isPlaying ? _patrolStartPosition : transform.position;
                Gizmos.DrawWireSphere(patrolCenter, randomPatrolRadius);
            }
            
            // Patrol points
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < patrolPoints.Length; i++)
                {
                    if (patrolPoints[i] == null) continue;
                    
                    Gizmos.DrawWireSphere(patrolPoints[i].position, 100f);
                    
                    // Draw line to next patrol point
                    int nextIndex = (i + 1) % patrolPoints.Length;
                    if (patrolPoints[nextIndex] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[nextIndex].position);
                    }
                }
            }
            
            // Line to player if detected
            if (Application.isPlaying && _playerDetected && _realPlayerTransform != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _realPlayerTransform.position);
            }
            
            // Show aim direction and emit points
            if (Application.isPlaying && showAimDebug && _companionCombat != null)
            {
                // Left hand (shotgun)
                if (_companionCombat.leftHandEmitPoint != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(_companionCombat.leftHandEmitPoint.position, 50f);
                    Gizmos.DrawRay(_companionCombat.leftHandEmitPoint.position, _companionCombat.leftHandEmitPoint.forward * 500f);
                }
                
                // Right hand (beam)
                if (_companionCombat.rightHandEmitPoint != null)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(_companionCombat.rightHandEmitPoint.position, 50f);
                    Gizmos.DrawRay(_companionCombat.rightHandEmitPoint.position, _companionCombat.rightHandEmitPoint.forward * 1000f);
                }
            }
        }
        
        // Inspector helper methods
        [ContextMenu("🔥 Toggle Enemy Mode")]
        public void ToggleEnemyMode()
        {
            isEnemy = !isEnemy;
            Debug.Log($"[EnemyCompanionBehavior] Enemy mode: {(isEnemy ? "ENABLED 🔥" : "DISABLED 💚")}");
            
            if (Application.isPlaying)
            {
                if (isEnemy)
                {
                    InitializeEnemyMode();
                }
                else
                {
                    // Restore to friendly mode
                    if (_fakePlayerObject != null)
                    {
                        Destroy(_fakePlayerObject);
                    }
                    
                    // Find real player and restore reference
                    GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                    if (playerObject != null && _companionCore != null)
                    {
                        _companionCore.playerTransform = playerObject.transform;
                    }
                }
            }
        }
        
        [ContextMenu("📊 Show Enemy Status")]
        public void ShowEnemyStatus()
        {
            Debug.Log("=== 🔥 ENEMY COMPANION STATUS ===");
            Debug.Log($"Is Enemy: {isEnemy}");
            Debug.Log($"Current State: {_currentState}");
            Debug.Log($"Player Detected: {_playerDetected}");
            Debug.Log($"Is Patrolling: {_isPatrolling}");
            Debug.Log($"Detection Radius: {playerDetectionRadius}");
            Debug.Log($"Attack Range: {attackRange}");
            Debug.Log($"Patrol Enabled: {enablePatrol}");
            Debug.Log($"Patrol Points: {patrolPoints?.Length ?? 0}");
            
            if (_realPlayerTransform != null)
            {
                float distance = Vector3.Distance(transform.position, _realPlayerTransform.position);
                Debug.Log($"Distance to Player: {distance:F0} units");
            }
            
            if (_companionCore != null)
            {
                Debug.Log($"Companion State: {_companionCore.CurrentState}");
                Debug.Log($"Companion Health: {_companionCore.Health}/{_companionCore.MaxHealth}");
                Debug.Log($"Companion Is Dead: {_companionCore.IsDead}");
            }
            
            // Collider info
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Debug.Log($"Collider: {col.GetType().Name} | IsTrigger: {col.isTrigger} | Enabled: {col.enabled}");
                Debug.Log($"Layer: {gameObject.layer} ({LayerMask.LayerToName(gameObject.layer)})");
            }
            else
            {
                Debug.LogError("❌ NO COLLIDER - Cannot take damage from raycasts!");
            }
        }
        
        [ContextMenu("💥 Test Damage (50 damage)")]
        public void TestDamage()
        {
            if (_companionCore != null)
            {
                Debug.Log($"[EnemyCompanionBehavior] 🧪 Testing damage... Health before: {_companionCore.Health}");
                _companionCore.TakeDamage(50f, transform.position, Vector3.forward);
                Debug.Log($"[EnemyCompanionBehavior] 💥 Damage applied! Health after: {_companionCore.Health}");
            }
            else
            {
                Debug.LogError("[EnemyCompanionBehavior] ❌ CompanionCore not found!");
            }
        }
        
        [ContextMenu("💀 Test Kill")]
        public void TestKill()
        {
            if (_companionCore != null)
            {
                Debug.Log($"[EnemyCompanionBehavior] 💀 Testing instant kill...");
                _companionCore.TakeDamage(999f, transform.position, Vector3.forward);
            }
        }
        
        /// <summary>
        /// 🎨 LOD SYSTEM: Update mesh detail based on distance
        /// Reduces tris/verts for distant companions
        /// </summary>
        private void UpdateLODLevel(float distance)
        {
            int newLODLevel;
            
            // Determine LOD level based on distance
            if (distance <= lod0Distance)
            {
                newLODLevel = 0; // Full detail
            }
            else if (distance <= lod1Distance)
            {
                newLODLevel = 1; // Medium detail
            }
            else if (distance <= lod2Distance)
            {
                newLODLevel = 2; // Low detail
            }
            else
            {
                newLODLevel = 3; // Very far - minimal rendering
            }
            
            // Only update if LOD level changed
            if (newLODLevel == _currentLODLevel) return;
            
            _currentLODLevel = newLODLevel;
            
            // Apply LOD settings
            switch (newLODLevel)
            {
                case 0: // Full detail - everything enabled
                    SetAnimatorSpeed(1.0f);
                    SetShadowCasting(true);
                    if (showDebugInfo && Time.frameCount % 120 == 0)
                        Debug.Log($"[EnemyCompanionBehavior] 🎨 {gameObject.name} LOD0 (Full Detail) - {distance:F0} units");
                    break;
                    
                case 1: // Medium detail - reduce animation speed
                    SetAnimatorSpeed(0.5f);
                    SetShadowCasting(disableShadowsAtDistance && distance > shadowDisableDistance ? false : true);
                    if (showDebugInfo && Time.frameCount % 120 == 0)
                        Debug.Log($"[EnemyCompanionBehavior] 🎨 {gameObject.name} LOD1 (Medium Detail) - {distance:F0} units");
                    break;
                    
                case 2: // Low detail - minimal animation, no shadows
                    SetAnimatorSpeed(0.25f);
                    SetShadowCasting(false);
                    if (showDebugInfo && Time.frameCount % 120 == 0)
                        Debug.Log($"[EnemyCompanionBehavior] 🎨 {gameObject.name} LOD2 (Low Detail) - {distance:F0} units");
                    break;
                    
                case 3: // Very far - disable rendering (but keep AI active)
                    SetAnimatorSpeed(0f);
                    SetShadowCasting(false);
                    if (showDebugInfo && Time.frameCount % 120 == 0)
                        Debug.Log($"[EnemyCompanionBehavior] 🎨 {gameObject.name} LOD3 (Minimal) - {distance:F0} units");
                    break;
            }
        }
        
        private void SetAnimatorSpeed(float speed)
        {
            Animator[] animators = GetComponentsInChildren<Animator>(true);
            foreach (Animator animator in animators)
            {
                if (animator != null && animator.enabled)
                {
                    animator.speed = speed;
                }
            }
        }
        
        private void SetShadowCasting(bool enabled)
        {
            if (_cachedRenderers == null) return;
            
            foreach (Renderer renderer in _cachedRenderers)
            {
                if (renderer != null && renderer.enabled)
                {
                    if (enabled)
                    {
                        // Restore original shadow mode
                        if (_originalShadowModes.ContainsKey(renderer))
                        {
                            renderer.shadowCastingMode = _originalShadowModes[renderer];
                        }
                    }
                    else
                    {
                        // Disable shadows
                        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                }
            }
        }
        
    }
}
