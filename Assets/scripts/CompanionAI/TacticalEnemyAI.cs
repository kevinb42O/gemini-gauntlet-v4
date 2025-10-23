using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

namespace CompanionAI
{
    /// <summary>
    /// 🎯 AAA-GRADE TACTICAL ENEMY AI - DMZ Building 21 Inspired
    /// 
    /// This is a complete overhaul of enemy behavior with:
    /// - Environment awareness (indoor/outdoor detection)
    /// - Proper line of sight with wall detection
    /// - Tactical cover system
    /// - State-based AI (Patrol → Alert → Search → Combat → Retreat)
    /// - Smart movement (no glitchy indoor behavior)
    /// - Sound detection
    /// - Strategic positioning
    /// 
    /// USAGE:
    /// - Add to enemy companion GameObject
    /// - Set up layers properly (Walls, Ground, Player)
    /// - Configure patrol points for hallway routes
    /// - Adjust detection settings for difficulty
    /// </summary>
    [RequireComponent(typeof(CompanionCore))]
    public class TacticalEnemyAI : MonoBehaviour
    {
        [Header("🎯 ENEMY CONFIGURATION")]
        [Tooltip("Enable to activate enemy mode")]
        public bool isEnemy = true;
        
        [Header("🔍 DETECTION SETTINGS")]
        [Tooltip("Maximum distance to detect player")]
        [Range(1000f, 50000f)] public float detectionRange = 25000f;
        
        [Tooltip("Field of view angle in degrees")]
        [Range(30f, 180f)] public float fieldOfView = 90f;
        
        [Tooltip("How often to check for player (seconds)")]
        [Range(0.1f, 1f)] public float detectionInterval = 0.2f;
        
        [Tooltip("Time to remember last known player position")]
        [Range(3f, 30f)] public float memoryDuration = 10f;
        
        [Header("👁️ LINE OF SIGHT")]
        [Tooltip("Layers that block vision (walls, obstacles)")]
        public LayerMask visionBlockingLayers;
        
        [Tooltip("Height offset for eye position (player is 320 units tall)")]
        [Range(50f, 300f)] public float eyeHeight = 160f;
        
        [Tooltip("Number of raycasts for accurate LOS")]
        [Range(1, 5)] public int losRaycastCount = 3;
        
        [Tooltip("Raycast spread for better detection (player is 50 units wide)")]
        [Range(10f, 100f)] public float losRaycastSpread = 30f;
        
        [Header("🚶 MOVEMENT SETTINGS")]
        [Tooltip("Patrol walking speed")]
        [Range(100f, 1000f)] public float patrolSpeed = 400f;
        
        [Tooltip("Alert/search speed")]
        [Range(200f, 1500f)] public float alertSpeed = 700f;
        
        [Tooltip("Combat movement speed")]
        [Range(200f, 1200f)] public float combatSpeed = 600f;
        
        [Tooltip("Rotation speed when turning")]
        [Range(50f, 300f)] public float rotationSpeed = 120f;
        
        [Header("🏢 INDOOR BEHAVIOR")]
        [Tooltip("Detect if enemy is indoors automatically")]
        public bool autoDetectIndoors = true;
        
        [Tooltip("Force indoor behavior (overrides auto-detect)")]
        public bool forceIndoorMode = false;
        
        [Tooltip("Reduce movement speed indoors by this factor")]
        [Range(0.3f, 1f)] public float indoorSpeedMultiplier = 0.6f;
        
        [Tooltip("Disable jumping when indoors")]
        public bool disableJumpingIndoors = true;
        
        [Header("🎯 COMBAT BEHAVIOR")]
        [Tooltip("Preferred combat distance")]
        [Range(1000f, 20000f)] public float preferredCombatDistance = 8000f;
        
        [Tooltip("Minimum distance before retreating")]
        [Range(500f, 5000f)] public float minCombatDistance = 2000f;
        
        [Tooltip("Maximum distance before advancing")]
        [Range(5000f, 30000f)] public float maxCombatDistance = 15000f;
        
        [Tooltip("Accuracy (0-1, higher = more accurate)")]
        [Range(0f, 1f)] public float accuracy = 0.7f;
        
        [Tooltip("Time between shots")]
        [Range(0.1f, 3f)] public float fireRate = 0.5f;
        
        [Tooltip("Burst fire duration")]
        [Range(0.5f, 5f)] public float burstDuration = 2f;
        
        [Tooltip("Cooldown between bursts")]
        [Range(0.5f, 5f)] public float burstCooldown = 1.5f;
        
        [Header("🛡️ COVER SYSTEM")]
        [Tooltip("Enable tactical cover usage")]
        public bool useCoverSystem = true;
        
        [Tooltip("Distance to search for cover")]
        [Range(1000f, 20000f)] public float coverSearchRadius = 10000f;
        
        [Tooltip("How long to stay in cover")]
        [Range(1f, 10f)] public float coverDuration = 3f;
        
        [Tooltip("Health % to seek cover")]
        [Range(0.1f, 0.9f)] public float coverHealthThreshold = 0.5f;
        
        [Tooltip("Layers that provide cover")]
        public LayerMask coverLayers;
        
        [Header("🚶 PATROL SETTINGS")]
        [Tooltip("Patrol waypoints (leave empty for random patrol)")]
        public Transform[] patrolPoints;
        
        [Tooltip("Wait time at each patrol point")]
        [Range(0f, 10f)] public float patrolWaitTime = 2f;
        
        [Tooltip("Random patrol radius if no points set")]
        [Range(5f, 50f)] public float randomPatrolRadius = 20f;
        
        [Header("🔊 SOUND DETECTION")]
        [Tooltip("Enable sound-based detection")]
        public bool enableSoundDetection = true;
        
        [Tooltip("Range to hear player footsteps")]
        [Range(1000f, 20000f)] public float soundDetectionRange = 12000f;
        
        [Header("💀 DEATH & DAMAGE")]
        [Tooltip("Enable hit effects when damaged")]
        public bool enableHitEffects = true;
        
        [Tooltip("Hit effect color")]
        public Color hitColor = Color.red;
        
        [Tooltip("Hit effect duration")]
        [Range(0.05f, 1f)] public float hitEffectDuration = 0.2f;
        
        [Tooltip("Destroy corpse after death (seconds)")]
        [Range(1f, 60f)] public float destroyAfterDeath = 10f;
        
        [Header("📊 DEBUG")]
        public bool showDebugInfo = true;
        public bool showDebugGizmos = true;
        
        // AI State
        private enum AIState
        {
            Idle,           // Standing still
            Patrolling,     // Walking patrol route
            Alert,          // Heard/saw something, investigating
            Searching,      // Lost sight, searching last known position
            Combat,         // Actively fighting player
            TakingCover,    // In cover, peeking out
            Retreating,     // Low health, falling back
            Dead            // Eliminated
        }
        
        private AIState _currentState = AIState.Patrolling;
        private AIState _previousState = AIState.Idle;
        
        // References
        private CompanionCore _companionCore;
        private CompanionMovement _companionMovement;
        private CompanionCombat _companionCombat;
        private CompanionTargeting _companionTargeting;
        private NavMeshAgent _navAgent;
        private Transform _transform;
        private Transform _playerTransform;
        
        // Detection
        private bool _playerDetected = false;
        private bool _hasLineOfSight = false;
        private Vector3 _lastKnownPlayerPosition;
        private float _lastPlayerSeenTime;
        private float _lastDetectionCheckTime;
        
        // Hit effects
        private Renderer[] _renderers;
        private Dictionary<Renderer, Color> _originalColors = new Dictionary<Renderer, Color>();
        private Coroutine _hitEffectCoroutine;
        
        // Environment
        private bool _isIndoors = false;
        private float _currentSpeedMultiplier = 1f;
        
        // Patrol
        private int _currentPatrolIndex = 0;
        private Vector3 _patrolStartPosition;
        private bool _isWaitingAtPatrol = false;
        
        // Combat
        private bool _isFiring = false;
        private float _lastFireTime;
        private float _burstStartTime;
        private bool _isInBurst = false;
        private Vector3 _currentAimOffset = Vector3.zero;
        
        // Cover
        private Vector3 _currentCoverPosition;
        private bool _isInCover = false;
        private float _coverStartTime;
        
        // Fake player for companion system hijacking
        private GameObject _fakePlayerObject;
        private Transform _fakePlayerTransform;
        
        void Awake()
        {
            _transform = transform;
            _companionCore = GetComponent<CompanionCore>();
            _companionMovement = GetComponent<CompanionMovement>();
            _companionCombat = GetComponent<CompanionCombat>();
            _companionTargeting = GetComponent<CompanionTargeting>();
            _navAgent = GetComponent<NavMeshAgent>();
            
            if (_companionCore == null)
            {
                Debug.LogError("[TacticalEnemyAI] CompanionCore not found! Disabling.");
                enabled = false;
                return;
            }
            
            _patrolStartPosition = _transform.position;
        }
        
        void Start()
        {
            if (!isEnemy)
            {
                enabled = false;
                return;
            }
            
            InitializeEnemyAI();
        }
        
        private void InitializeEnemyAI()
        {
            // Find player
            AAAMovementController playerController = FindObjectOfType<AAAMovementController>();
            if (playerController != null)
            {
                _playerTransform = playerController.transform;
                
                if (_playerTransform == _transform)
                {
                    Debug.LogError("[TacticalEnemyAI] Found self as player! Disabling.");
                    enabled = false;
                    return;
                }
            }
            else
            {
                Debug.LogError("[TacticalEnemyAI] Player not found! Disabling.");
                enabled = false;
                return;
            }
            
            // Create fake player to hijack companion system
            _fakePlayerObject = new GameObject($"{gameObject.name}_FakePlayer");
            _fakePlayerTransform = _fakePlayerObject.transform;
            _fakePlayerTransform.position = _transform.position + Vector3.forward * 1000f;
            
            // Override companion's player reference
            _companionCore.playerTransform = _fakePlayerTransform;
            
            // Initialize hit effects
            InitializeHitEffects();
            
            // Subscribe to damage events
            CompanionCore.OnCompanionHealthChanged += OnHealthChanged;
            CompanionCore.OnCompanionDied += OnDeath;
            
            // Disable companion targeting (we handle it manually)
            if (_companionTargeting != null)
            {
                _companionTargeting.enabled = false;
            }
            
            // Configure movement for tactical behavior
            ConfigureMovementSystem();
            
            // Configure NavMesh
            ConfigureNavigation();
            
            // Detect environment
            if (autoDetectIndoors)
            {
                DetectEnvironment();
            }
            
            // Start in patrol state
            TransitionToState(AIState.Patrolling);
            
            if (showDebugInfo)
            {
                Debug.Log($"[TacticalEnemyAI] ✅ Initialized - Mode: {(_isIndoors ? "INDOOR" : "OUTDOOR")}");
            }
        }
        
        private void ConfigureMovementSystem()
        {
            if (_companionMovement == null) return;
            
            // CRITICAL: Disable the crazy tactical movement
            _companionMovement.moveWhileShooting = false;
            _companionMovement.jumpChance = 0f;
            _companionMovement.combatSpeedMultiplier = 1f; // Reset to normal
            _companionMovement.repositionInterval = 2f; // Much slower repositioning
            
            if (showDebugInfo)
            {
                Debug.Log("[TacticalEnemyAI] 🎯 Movement configured for tactical behavior");
            }
        }
        
        private void ConfigureNavigation()
        {
            if (_navAgent == null) return;
            
            _navAgent.speed = patrolSpeed;
            _navAgent.angularSpeed = rotationSpeed;
            _navAgent.acceleration = 8f;
            _navAgent.stoppingDistance = 1f;
            _navAgent.autoBraking = true;
        }
        
        private void DetectEnvironment()
        {
            // Check if there's a ceiling above (indicates indoors)
            RaycastHit hit;
            if (Physics.Raycast(_transform.position, Vector3.up, out hit, 5000f, visionBlockingLayers))
            {
                _isIndoors = true;
                if (showDebugInfo)
                {
                    Debug.Log($"[TacticalEnemyAI] 🏢 Detected INDOORS (ceiling at {hit.distance}m)");
                }
            }
            else
            {
                _isIndoors = false;
                if (showDebugInfo)
                {
                    Debug.Log("[TacticalEnemyAI] 🌳 Detected OUTDOORS");
                }
            }
            
            if (forceIndoorMode)
            {
                _isIndoors = true;
            }
        }
        
        private void InitializeHitEffects()
        {
            if (!enableHitEffects) return;
            
            _renderers = GetComponentsInChildren<Renderer>();
            _originalColors.Clear();
            
            foreach (Renderer renderer in _renderers)
            {
                if (renderer != null && renderer.material != null)
                {
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
            
            if (showDebugInfo)
            {
                Debug.Log($"[TacticalEnemyAI] 💥 Hit effects initialized with {_renderers.Length} renderers");
            }
        }
        
        private void OnHealthChanged(CompanionCore companion)
        {
            if (companion != _companionCore) return;
            
            if (!_companionCore.IsDead && enableHitEffects)
            {
                TriggerHitEffect();
            }
        }
        
        private void OnDeath(CompanionCore companion)
        {
            if (companion != _companionCore) return;
            
            TransitionToState(AIState.Dead);
            
            if (showDebugInfo)
            {
                Debug.Log("[TacticalEnemyAI] 💀 Enemy died!");
            }
            
            // Destroy after delay
            StartCoroutine(DestroyAfterDelay());
        }
        
        private void TriggerHitEffect()
        {
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
                    
                    // Add intense emission glow
                    if (renderer.material.HasProperty("_EmissionColor"))
                    {
                        renderer.material.EnableKeyword("_EMISSION");
                        renderer.material.SetColor("_EmissionColor", hitColor * 4000f); // ✅ Balanced glow
                    }
                }
            }
            
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
        
        private IEnumerator DestroyAfterDelay()
        {
            yield return new WaitForSeconds(destroyAfterDeath);
            
            if (showDebugInfo)
            {
                Debug.Log("[TacticalEnemyAI] 💥 Destroying dead enemy");
            }
            
            Destroy(gameObject);
        }
        
        void Update()
        {
            if (!isEnemy || _companionCore.IsDead) return;
            
            // Periodic detection check
            if (Time.time - _lastDetectionCheckTime >= detectionInterval)
            {
                _lastDetectionCheckTime = Time.time;
                PerformDetectionCheck();
            }
            
            // Update current state behavior
            UpdateCurrentState();
            
            // Update aim if in combat
            if (_currentState == AIState.Combat || _currentState == AIState.TakingCover)
            {
                UpdateAiming();
            }
        }
        
        private void PerformDetectionCheck()
        {
            if (_playerTransform == null) return;
            
            float distanceToPlayer = Vector3.Distance(_transform.position, _playerTransform.position);
            
            // Check if player is in range
            if (distanceToPlayer > detectionRange)
            {
                _playerDetected = false;
                _hasLineOfSight = false;
                return;
            }
            
            // Check field of view
            Vector3 directionToPlayer = (_playerTransform.position - _transform.position).normalized;
            float angleToPlayer = Vector3.Angle(_transform.forward, directionToPlayer);
            
            if (angleToPlayer > fieldOfView / 2f)
            {
                // Player outside FOV
                _playerDetected = false;
                _hasLineOfSight = false;
                
                // Check sound detection
                if (enableSoundDetection && distanceToPlayer <= soundDetectionRange)
                {
                    // Player is close enough to hear
                    _playerDetected = true;
                    _lastKnownPlayerPosition = _playerTransform.position;
                    
                    if (showDebugInfo && _currentState == AIState.Patrolling)
                    {
                        Debug.Log($"[TacticalEnemyAI] 🔊 Heard player at {distanceToPlayer:F1}m");
                    }
                }
                
                return;
            }
            
            // Check line of sight
            _hasLineOfSight = CheckLineOfSight();
            
            if (_hasLineOfSight)
            {
                _playerDetected = true;
                _lastKnownPlayerPosition = _playerTransform.position;
                _lastPlayerSeenTime = Time.time;
                
                if (showDebugInfo && _previousState != AIState.Combat)
                {
                    Debug.Log($"[TacticalEnemyAI] 👁️ Player spotted at {distanceToPlayer:F1}m!");
                }
            }
            else
            {
                // No LOS but was detected recently
                if (Time.time - _lastPlayerSeenTime < memoryDuration)
                {
                    _playerDetected = true; // Still remember player
                }
                else
                {
                    _playerDetected = false;
                }
            }
        }
        
        private bool CheckLineOfSight()
        {
            Vector3 eyePosition = _transform.position + Vector3.up * eyeHeight;
            Vector3 targetPosition = _playerTransform.position + Vector3.up * eyeHeight;
            
            // Multi-raycast for better accuracy
            for (int i = 0; i < losRaycastCount; i++)
            {
                Vector3 offset = Vector3.zero;
                // Distribute raycasts around target (player is 50 units wide, 320 tall)
                if (i == 1) offset = Vector3.right * losRaycastSpread;
                else if (i == 2) offset = Vector3.left * losRaycastSpread;
                else if (i == 3) offset = Vector3.up * losRaycastSpread;
                else if (i == 4) offset = Vector3.down * losRaycastSpread;
                
                Vector3 rayStart = eyePosition + offset;
                Vector3 rayDirection = (targetPosition - rayStart).normalized;
                float rayDistance = Vector3.Distance(rayStart, targetPosition);
                
                RaycastHit hit;
                if (Physics.Raycast(rayStart, rayDirection, out hit, rayDistance, visionBlockingLayers))
                {
                    // Something blocking view
                    if (showDebugGizmos)
                    {
                        Debug.DrawRay(rayStart, rayDirection * hit.distance, Color.red, detectionInterval);
                    }
                    continue;
                }
                else
                {
                    // Clear line of sight on at least one ray
                    if (showDebugGizmos)
                    {
                        Debug.DrawRay(rayStart, rayDirection * rayDistance, Color.green, detectionInterval);
                    }
                    return true;
                }
            }
            
            return false; // All rays blocked
        }
        
        private void UpdateCurrentState()
        {
            switch (_currentState)
            {
                case AIState.Idle:
                    UpdateIdleState();
                    break;
                    
                case AIState.Patrolling:
                    UpdatePatrolState();
                    break;
                    
                case AIState.Alert:
                    UpdateAlertState();
                    break;
                    
                case AIState.Searching:
                    UpdateSearchState();
                    break;
                    
                case AIState.Combat:
                    UpdateCombatState();
                    break;
                    
                case AIState.TakingCover:
                    UpdateCoverState();
                    break;
                    
                case AIState.Retreating:
                    UpdateRetreatState();
                    break;
            }
        }
        
        private void UpdateIdleState()
        {
            // Transition to patrol
            if (!_isWaitingAtPatrol)
            {
                TransitionToState(AIState.Patrolling);
            }
        }
        
        private void UpdatePatrolState()
        {
            // Check for player detection
            if (_playerDetected)
            {
                if (_hasLineOfSight)
                {
                    TransitionToState(AIState.Combat);
                }
                else
                {
                    TransitionToState(AIState.Alert);
                }
                return;
            }
            
            // Continue patrol
            if (!_isWaitingAtPatrol)
            {
                MoveToNextPatrolPoint();
            }
        }
        
        private void UpdateAlertState()
        {
            // Check if we can see player now
            if (_hasLineOfSight)
            {
                TransitionToState(AIState.Combat);
                return;
            }
            
            // Move to last known position
            if (_playerDetected)
            {
                _navAgent.SetDestination(_lastKnownPlayerPosition);
                
                // If reached last known position, start searching
                if (Vector3.Distance(_transform.position, _lastKnownPlayerPosition) < 2f)
                {
                    TransitionToState(AIState.Searching);
                }
            }
            else
            {
                // Lost player completely, return to patrol
                TransitionToState(AIState.Patrolling);
            }
        }
        
        private void UpdateSearchState()
        {
            // Check if we found player
            if (_hasLineOfSight)
            {
                TransitionToState(AIState.Combat);
                return;
            }
            
            // Check if memory expired
            if (Time.time - _lastPlayerSeenTime > memoryDuration)
            {
                TransitionToState(AIState.Patrolling);
                return;
            }
            
            // Search pattern around last known position
            // (Simple implementation - can be enhanced with proper search patterns)
        }
        
        private void UpdateCombatState()
        {
            // Check health for cover
            if (useCoverSystem && _companionCore.HealthNormalized < coverHealthThreshold)
            {
                TransitionToState(AIState.TakingCover);
                return;
            }
            
            // Lost sight of player
            if (!_hasLineOfSight)
            {
                TransitionToState(AIState.Searching);
                return;
            }
            
            // Manage combat positioning
            float distanceToPlayer = Vector3.Distance(_transform.position, _playerTransform.position);
            
            if (distanceToPlayer < minCombatDistance)
            {
                // Too close, back up
                Vector3 retreatDirection = (_transform.position - _playerTransform.position).normalized;
                Vector3 retreatPosition = _transform.position + retreatDirection * 2000f;
                _navAgent.SetDestination(retreatPosition);
            }
            else if (distanceToPlayer > maxCombatDistance)
            {
                // Too far, advance
                Vector3 advancePosition = _playerTransform.position + (_transform.position - _playerTransform.position).normalized * preferredCombatDistance;
                _navAgent.SetDestination(advancePosition);
            }
            else
            {
                // Good distance, strafe
                Vector3 strafeDirection = Vector3.Cross((_playerTransform.position - _transform.position).normalized, Vector3.up);
                if (Random.value > 0.5f) strafeDirection = -strafeDirection;
                
                Vector3 strafePosition = _transform.position + strafeDirection * 1500f;
                _navAgent.SetDestination(strafePosition);
            }
            
            // Handle firing
            UpdateFiring();
            
            // Look at player
            LookAtTarget(_playerTransform.position);
        }
        
        private void UpdateCoverState()
        {
            // Check if should leave cover
            if (Time.time - _coverStartTime > coverDuration)
            {
                _isInCover = false;
                TransitionToState(AIState.Combat);
                return;
            }
            
            // Peek and shoot from cover
            if (_hasLineOfSight)
            {
                UpdateFiring();
                LookAtTarget(_playerTransform.position);
            }
        }
        
        private void UpdateRetreatState()
        {
            // Find cover and retreat
            Vector3 coverPosition = FindNearestCover();
            if (coverPosition != Vector3.zero)
            {
                _navAgent.SetDestination(coverPosition);
                
                if (Vector3.Distance(_transform.position, coverPosition) < 2f)
                {
                    TransitionToState(AIState.TakingCover);
                }
            }
            else
            {
                // No cover found, just back away
                Vector3 retreatDirection = (_transform.position - _playerTransform.position).normalized;
                Vector3 retreatPosition = _transform.position + retreatDirection * 10f;
                _navAgent.SetDestination(retreatPosition);
            }
        }
        
        private void UpdateFiring()
        {
            if (_companionCombat == null) return;
            
            // Burst fire system
            if (!_isInBurst)
            {
                // Check if cooldown is over
                if (Time.time - _lastFireTime >= burstCooldown)
                {
                    // Start new burst
                    _isInBurst = true;
                    _burstStartTime = Time.time;
                    _companionCombat.StartAttacking();
                    
                    if (showDebugInfo)
                    {
                        Debug.Log("[TacticalEnemyAI] 🔫 Starting burst fire");
                    }
                }
            }
            else
            {
                // Check if burst is over
                if (Time.time - _burstStartTime >= burstDuration)
                {
                    // End burst
                    _isInBurst = false;
                    _lastFireTime = Time.time;
                    _companionCombat.StopAttacking();
                    
                    if (showDebugInfo)
                    {
                        Debug.Log("[TacticalEnemyAI] 🛑 Ending burst fire");
                    }
                }
            }
        }
        
        private void UpdateAiming()
        {
            if (_companionTargeting == null || _playerTransform == null) return;
            
            // Calculate aim offset based on accuracy
            if (Random.value > accuracy)
            {
                // Miss
                _currentAimOffset = new Vector3(
                    Random.Range(-2f, 2f),
                    Random.Range(-1f, 1f),
                    Random.Range(-2f, 2f)
                );
            }
            else
            {
                // Hit
                _currentAimOffset = Vector3.zero;
            }
            
            // Update fake player position for companion combat system
            Vector3 aimTarget = _playerTransform.position + _currentAimOffset;
            _fakePlayerTransform.position = aimTarget;
            
            // Force targeting via reflection
            var targetingType = typeof(CompanionTargeting);
            var currentTargetField = targetingType.GetField("_currentTarget", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (currentTargetField != null)
            {
                currentTargetField.SetValue(_companionTargeting, _playerTransform);
            }
        }
        
        private void LookAtTarget(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - _transform.position);
            direction.y = 0; // Keep rotation horizontal
            
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                _transform.rotation = Quaternion.RotateTowards(_transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        
        private void MoveToNextPatrolPoint()
        {
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                // Use defined patrol points
                Transform targetPoint = patrolPoints[_currentPatrolIndex];
                
                if (targetPoint != null)
                {
                    _navAgent.SetDestination(targetPoint.position);
                    
                    // Check if reached
                    if (Vector3.Distance(_transform.position, targetPoint.position) < 2f)
                    {
                        StartCoroutine(WaitAtPatrolPoint());
                    }
                }
            }
            else
            {
                // Random patrol
                Vector2 randomCircle = Random.insideUnitCircle * randomPatrolRadius;
                Vector3 randomPoint = _patrolStartPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
                
                _navAgent.SetDestination(randomPoint);
                
                if (Vector3.Distance(_transform.position, randomPoint) < 2f)
                {
                    StartCoroutine(WaitAtPatrolPoint());
                }
            }
        }
        
        private IEnumerator WaitAtPatrolPoint()
        {
            _isWaitingAtPatrol = true;
            
            if (showDebugInfo)
            {
                Debug.Log($"[TacticalEnemyAI] 🚶 Waiting at patrol point for {patrolWaitTime}s");
            }
            
            yield return new WaitForSeconds(patrolWaitTime);
            
            // Move to next point
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolPoints.Length;
            }
            
            _isWaitingAtPatrol = false;
        }
        
        private Vector3 FindNearestCover()
        {
            // Simple cover detection - find nearest wall/obstacle
            Collider[] nearbyObjects = Physics.OverlapSphere(_transform.position, coverSearchRadius, coverLayers);
            
            Vector3 bestCoverPosition = Vector3.zero;
            float bestScore = float.MinValue;
            
            foreach (Collider obj in nearbyObjects)
            {
                // Get position behind object relative to player
                Vector3 directionFromPlayer = (obj.transform.position - _playerTransform.position).normalized;
                Vector3 coverPosition = obj.transform.position + directionFromPlayer * 2f;
                
                // Score based on distance and cover quality
                float distanceToUs = Vector3.Distance(_transform.position, coverPosition);
                float distanceToPlayer = Vector3.Distance(coverPosition, _playerTransform.position);
                
                float score = distanceToPlayer - distanceToUs; // Prefer close cover that's far from player
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCoverPosition = coverPosition;
                }
            }
            
            return bestCoverPosition;
        }
        
        private void TransitionToState(AIState newState)
        {
            if (_currentState == newState) return;
            
            _previousState = _currentState;
            _currentState = newState;
            
            OnStateEnter(newState);
            
            if (showDebugInfo)
            {
                Debug.Log($"[TacticalEnemyAI] 🔄 State: {_previousState} → {newState}");
            }
        }
        
        private void OnStateEnter(AIState state)
        {
            switch (state)
            {
                case AIState.Idle:
                    _navAgent.isStopped = true;
                    _navAgent.speed = 0f;
                    break;
                    
                case AIState.Patrolling:
                    _navAgent.isStopped = false;
                    _navAgent.speed = patrolSpeed * (_isIndoors ? indoorSpeedMultiplier : 1f);
                    if (_companionCombat != null) _companionCombat.StopAttacking();
                    break;
                    
                case AIState.Alert:
                    _navAgent.isStopped = false;
                    _navAgent.speed = alertSpeed * (_isIndoors ? indoorSpeedMultiplier : 1f);
                    if (_companionCombat != null) _companionCombat.StopAttacking();
                    break;
                    
                case AIState.Searching:
                    _navAgent.isStopped = false;
                    _navAgent.speed = alertSpeed * (_isIndoors ? indoorSpeedMultiplier : 1f);
                    if (_companionCombat != null) _companionCombat.StopAttacking();
                    break;
                    
                case AIState.Combat:
                    _navAgent.isStopped = false;
                    _navAgent.speed = combatSpeed * (_isIndoors ? indoorSpeedMultiplier : 1f);
                    break;
                    
                case AIState.TakingCover:
                    _isInCover = true;
                    _coverStartTime = Time.time;
                    _currentCoverPosition = FindNearestCover();
                    if (_currentCoverPosition != Vector3.zero)
                    {
                        _navAgent.SetDestination(_currentCoverPosition);
                    }
                    break;
                    
                case AIState.Retreating:
                    _navAgent.isStopped = false;
                    _navAgent.speed = alertSpeed;
                    if (_companionCombat != null) _companionCombat.StopAttacking();
                    break;
                    
                case AIState.Dead:
                    if (_navAgent != null && _navAgent.enabled && _navAgent.isOnNavMesh)
                    {
                        _navAgent.isStopped = true;
                    }
                    if (_companionCombat != null) _companionCombat.StopAttacking();
                    break;
            }
        }
        
        void OnDestroy()
        {
            if (_fakePlayerObject != null)
            {
                Destroy(_fakePlayerObject);
            }
            
            // Unsubscribe from events
            CompanionCore.OnCompanionHealthChanged -= OnHealthChanged;
            CompanionCore.OnCompanionDied -= OnDeath;
        }
        
        void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos) return;
            
            // Detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // Combat ranges
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, minCombatDistance);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, preferredCombatDistance);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, maxCombatDistance);
            
            // Field of view
            if (Application.isPlaying)
            {
                Vector3 leftBoundary = Quaternion.Euler(0, -fieldOfView / 2f, 0) * transform.forward * detectionRange;
                Vector3 rightBoundary = Quaternion.Euler(0, fieldOfView / 2f, 0) * transform.forward * detectionRange;
                
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
                Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
            }
            
            // Last known player position
            if (Application.isPlaying && _playerDetected)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(_lastKnownPlayerPosition, 1f);
                Gizmos.DrawLine(transform.position, _lastKnownPlayerPosition);
            }
            
            // Patrol points
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < patrolPoints.Length; i++)
                {
                    if (patrolPoints[i] == null) continue;
                    
                    Gizmos.DrawWireSphere(patrolPoints[i].position, 1f);
                    
                    int nextIndex = (i + 1) % patrolPoints.Length;
                    if (patrolPoints[nextIndex] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[nextIndex].position);
                    }
                }
            }
        }
    }
}
