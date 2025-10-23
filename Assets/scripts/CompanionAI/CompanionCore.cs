using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using GeminiGauntlet.Progression;
using GeminiGauntlet.UI;

namespace CompanionAI
{
    /// <summary>
    /// Core companion controller - manages state and coordinates other systems
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [DefaultExecutionOrder(0)]
    public class CompanionCore : MonoBehaviour, IDamageable
    {
        [Header("Player References")]
        public Transform playerTransform;
        
        [Header("Health System")]
        public float maxHealth = 1f;
        
        [Header("💀 INSTANT DEATH SYSTEM")]
        [Tooltip("Companion dies instantly when enemy touches them")]
        public bool instantDeathOnContact = true;
        [Tooltip("Tags that cause instant death on contact")]
        public string[] lethalEnemyTags = new string[] { "Skull", "Enemy" };
        
        [Header("⚡ DAMAGE TEXT OPTIMIZATION")]
        [Tooltip("Minimum time between damage text displays (prevents spam lag)")]
        [Range(0.1f, 2f)] public float damageTextCooldown = 0.5f;
        
        [Header("💎 LOOT DROP SYSTEM")]
        [Tooltip("Should this companion drop armor plates on death?")]
        public bool dropPlatesOnDeath = true;
        [Tooltip("Number of plates to drop (1-3)")]
        [Range(1, 3)] public int platesToDrop = 1;
        [Tooltip("Armor plate item data to drop")]
        public ArmorPlateItemData plateItemData;
        [Tooltip("Prefab for the plate item (with PlateItem script and model)")]
        public GameObject plateItemPrefab;
        [Tooltip("Height offset for dropping plates")]
        public float dropHeight = 1f;
        
        // State Management
        public enum CompanionState { Following, Engaging, Attacking, Dead }
        [SerializeField] private CompanionState _currentState = CompanionState.Following;
        
        // Health and Death System
        private bool _isDead = false;
        private float _health = 1f;
        private bool _spawnEventRaised = false;
        private float _lastDamageTextTime = -999f;
        private float _accumulatedDamage = 0f;
        
        // Cached Components
        private NavMeshAgent _navAgent;
        private Transform _transform;
        private Rigidbody _rigidbody;
        private Collider _collider;
        // System References
        public CompanionMovement MovementSystem { get; private set; }
        public CompanionCombat CombatSystem { get; private set; }
        public CompanionTargeting TargetingSystem { get; private set; }
        
        // Audio and Visual systems - will be added via GetComponent after compilation
        private MonoBehaviour _audioSystem;
        private MonoBehaviour _visualEffects;

        public MonoBehaviour AudioSystem => _audioSystem;
        public MonoBehaviour VisualEffects => _visualEffects;

        [Header("Companion Data Reference")]
        public CompanionData companionProfile;

        private static readonly HashSet<CompanionData> _sessionCompanions = new HashSet<CompanionData>();

        private static readonly HashSet<CompanionCore> _activeCompanions = new HashSet<CompanionCore>();

        public static event Action<CompanionCore> OnCompanionSpawned;
        public static event Action<CompanionCore> OnCompanionDied;
        public static event Action<CompanionCore> OnCompanionRemoved;
        public static event Action<CompanionCore> OnCompanionHealthChanged;

        public static IReadOnlyCollection<CompanionData> GetSessionCompanions() => _sessionCompanions;

        public static IEnumerable<CompanionCore> GetActiveCompanions() => _activeCompanions;

        public static void AwardXPToSessionCompanions(int amount)
        {
            if (amount <= 0)
            {
                Debug.Log($"[CompanionCore] AwardXPToSessionCompanions: Skipped (amount={amount})");
                return;
            }

            Debug.Log($"[CompanionCore] AwardXPToSessionCompanions: Distributing {amount} XP to {_sessionCompanions.Count} companions");

            foreach (var profile in _sessionCompanions)
            {
                if (profile == null)
                {
                    Debug.LogWarning("[CompanionCore] AwardXPToSessionCompanions: Encountered null profile in session companions");
                    continue;
                }

                profile.EnsureProgressionInitialized();
                int beforeLevel = profile.companionLevel;
                int beforeXP = profile.currentXP;
                float beforeProgress = profile.GetLevelProgress01();

                profile.AddXP(amount);

                profile.SaveProgression();

                profile.EnsureProgressionInitialized();
                int afterLevel = profile.companionLevel;
                int afterXP = profile.currentXP;
                float afterProgress = profile.GetLevelProgress01();

                Debug.Log($"[CompanionCore] {profile.companionName}: +{amount} XP | Level {beforeLevel}->{afterLevel} | XP {beforeXP}->{afterXP} | Progress {beforeProgress:P0}->{afterProgress:P0}");
            }
        }

        public static void ResetSessionCompanions()
        {
            _sessionCompanions.Clear();

            if (_activeCompanions.Count > 0)
            {
                var snapshot = new List<CompanionCore>(_activeCompanions);
                foreach (var companion in snapshot)
                {
                    OnCompanionRemoved?.Invoke(companion);
                }

                _activeCompanions.Clear();
            }
        }

        private void OnApplicationQuit()
        {
            foreach (var profile in _sessionCompanions)
            {
                profile?.SaveProgression();
            }
        }

        public CompanionState CurrentState => _currentState;
        public bool IsActive => !_isDead;
        public bool IsDead => _isDead;
        public float Health => _health;
        public float MaxHealth => maxHealth;
        public float HealthNormalized => maxHealth <= 0f ? 0f : Mathf.Clamp01(_health / Mathf.Max(0.0001f, maxHealth));
        public NavMeshAgent NavAgent => _navAgent;
        public Transform PlayerTransform => playerTransform;
        public string DisplayName => companionProfile != null ? companionProfile.companionName : gameObject.name;

        void Awake()
        {
            _transform = transform;
            _navAgent = GetComponent<NavMeshAgent>();
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            _health = maxHealth;

            _activeCompanions.Add(this);

            InitializeCompanionProfile();

            // Ensure we have required components for instant death system
            if (_rigidbody == null)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody>();
                _rigidbody.freezeRotation = true;
                Debug.Log("[CompanionCore]  Added Rigidbody for instant death system");
            }
            
            if (_collider == null)
            {
                _collider = gameObject.AddComponent<CapsuleCollider>();
                Debug.Log("[CompanionCore]  Added Collider for instant death system");
            }
            
            // Cache optional systems before initialization so they can be wired automatically
            CacheOptionalSystems();

            // Initialize all systems
            InitializeSystems();
        }

        private void CacheOptionalSystems()
        {
            if (_audioSystem == null)
            {
                var audioComponent = GetComponent<CompanionAudio>() ?? GetComponentInChildren<CompanionAudio>();
                if (audioComponent != null)
                {
                    _audioSystem = audioComponent;
                    Debug.Log("[CompanionCore] Cached CompanionAudio component");
                }
            }

            if (_visualEffects == null)
            {
                var vfxComponent = GetComponent<CompanionVisualEffects>() ?? GetComponentInChildren<CompanionVisualEffects>();
                if (vfxComponent != null)
                {
                    _visualEffects = vfxComponent;
                    Debug.Log("[CompanionCore] Cached CompanionVisualEffects component");
                }
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
                    Debug.Log("[CompanionCore] Auto-found player: " + player.name);
                }
            }

            InitializeCompanionProfile();

            RaiseSpawnEventIfNeeded();
        }

        private void InitializeCompanionProfile()
        {
            if (companionProfile == null)
            {
                return;
            }

            companionProfile.EnsureProgressionInitialized();
            _sessionCompanions.Add(companionProfile);
            Debug.Log($"[CompanionCore] Registered session companion: {companionProfile.companionName}");
        }

        private void InitializeSystems()
        {
            MovementSystem = GetComponent<CompanionMovement>();
            MovementSystem?.Initialize(this);

            CombatSystem = GetComponent<CompanionCombat>();
            CombatSystem?.Initialize(this);
            TargetingSystem = GetComponent<CompanionTargeting>();
            TargetingSystem?.Initialize(this);

            if (_audioSystem != null)
            {
                var initMethod = _audioSystem.GetType().GetMethod("Initialize");
                initMethod?.Invoke(_audioSystem, new object[] { this });
            }

            if (_visualEffects != null)
            {
                var initMethod = _visualEffects.GetType().GetMethod("Initialize");
                initMethod?.Invoke(_visualEffects, new object[] { this });
            }
        }

        private void RaiseSpawnEventIfNeeded()
        {
            if (_spawnEventRaised)
            {
                return;
            }

            _spawnEventRaised = true;
            OnCompanionSpawned?.Invoke(this);
            OnCompanionHealthChanged?.Invoke(this);
        }
        
        void Update()
        {
            // 💀 PERFORMANCE: Skip ALL calculations if dead
            if (_isDead) return;
            
            UpdateState();
        }
        
        private void UpdateState()
        {
            Transform currentTarget = TargetingSystem?.GetCurrentTarget();
            
            if (currentTarget != null)
            {
                // IMMEDIATE THREAT DETECTION - Start shooting immediately if target is detected
                // No distance checks - shoot first, optimize position later
                SetState(CompanionState.Attacking);
            }
            else
            {
                SetState(CompanionState.Following);
            }
        }
        
        public void SetState(CompanionState newState)
        {
            if (_currentState == newState) return;
            
            _currentState = newState;
            OnStateChanged(newState);
        }
        
        private void OnStateChanged(CompanionState newState)
        {
            switch (newState)
            {
                case CompanionState.Following:
                    MovementSystem?.SetFollowingMode();
                    CombatSystem?.StopAttacking();
                    CallVisualEffectsMethod("ShowFollowingEffects");
                    break;
                    
                case CompanionState.Engaging:
                    // Rarely used now - treat same as attacking
                    MovementSystem?.SetEngagingMode();
                    CombatSystem?.StartAttacking(); // Start shooting immediately
                    CallVisualEffectsMethod("ShowCombatEffects");
                    break;
                    
                case CompanionState.Attacking:
                    // SHOOT FIRST - Start attacking immediately, movement will adapt
                    CombatSystem?.StartAttacking();
                    CallVisualEffectsMethod("ShowCombatEffects");
                    break;
                    
                case CompanionState.Dead:
                    MovementSystem?.Stop();
                    CombatSystem?.StopAttacking();
                    CallVisualEffectsMethod("ShowDeathEffects");
                    break;
            }
        }
        // IDamageable implementation
        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (_isDead) return;

            _health = Mathf.Max(0f, _health - amount);
            OnCompanionHealthChanged?.Invoke(this);
            
            // ⚡ PERFORMANCE: Accumulate damage and show text only on cooldown
            _accumulatedDamage += amount;
            float timeSinceLastText = Time.time - _lastDamageTextTime;
            
            if (timeSinceLastText >= damageTextCooldown)
            {
                ShowDamageText(_accumulatedDamage, hitPoint);
                _lastDamageTextTime = Time.time;
                _accumulatedDamage = 0f;
            }

            if (_health <= 0)
            {
                Die();
            }

            Debug.Log($"[CompanionCore] Took {amount} damage at {hitPoint}. Health: {_health}");
        }
        
        /// <summary>
        /// Show floating damage text above enemy (with cooldown to prevent lag)
        /// </summary>
        private void ShowDamageText(float damage, Vector3 hitPoint)
        {
            if (FloatingTextManager.Instance != null && damage > 0)
            {
                // Format damage text as "-XXXhp"
                string damageText = $"-{Mathf.RoundToInt(damage)}hp";
                
                // Softer orange-red color (less intense than pure red)
                Color damageColor = new Color(1f, 0.4f, 0.2f); // Orange-red
                
                // Smaller text size for damage (30 instead of default 48)
                FloatingTextManager.Instance.ShowFloatingText(damageText, hitPoint, damageColor, 30);
            }
        }

        private void Die()
        {
            if (_isDead) return; // Prevent multiple death calls
            _isDead = true;
            _health = 0f;
            OnCompanionHealthChanged?.Invoke(this);
            SetState(CompanionState.Dead);

            PerformInstantDeath();
            
            // Drop loot (plates) before death effects
            DropLoot();
            
            // Grant XP if XPGranter component exists
            GrantXPOnDeath();

            OnCompanionDied?.Invoke(this);

            Debug.Log("[CompanionCore] 💀 COMPANION DIED INSTANTLY! All systems shut down.");
        }
        
        /// <summary>
        /// Grant XP on death if XPGranter component is attached
        /// </summary>
        private void GrantXPOnDeath()
        {
            XPGranter xpGranter = GetComponent<XPGranter>();
            if (xpGranter != null && !xpGranter.HasGrantedXP)
            {
                xpGranter.GrantXPManually("Enemy Death");
                Debug.Log($"[CompanionCore] 💰 Granted {xpGranter.XPAmount} XP for killing {gameObject.name}");
            }
        }
        
        /// <summary>
        /// 💎 Drop loot (armor plates) when companion dies
        /// </summary>
        private void DropLoot()
        {
            if (!dropPlatesOnDeath || plateItemPrefab == null || plateItemData == null)
            {
                if (dropPlatesOnDeath)
                {
                    Debug.LogWarning("[CompanionCore] 💎 Cannot drop plates - missing prefab or data!");
                }
                return;
            }
            
            // Calculate drop position (slightly above companion, at center)
            Vector3 dropPosition = transform.position + Vector3.up * dropHeight;
            
            // Spawn the plate item prefab
            GameObject droppedPlate = Instantiate(plateItemPrefab, dropPosition, Quaternion.identity);
            
            // Configure the PlateItem component
            PlateItem plateItem = droppedPlate.GetComponent<PlateItem>();
            if (plateItem != null)
            {
                plateItem.SetPlateData(plateItemData, platesToDrop);
                Debug.Log($"[CompanionCore] 💎 Dropped {platesToDrop}x {plateItemData.itemName} at {dropPosition}");
            }
            else
            {
                Debug.LogError("[CompanionCore] 💎 PlateItem component not found on prefab!");
            }
        }

        /// <summary>
        /// 💀 INSTANT DEATH: Completely shut down companion and make it fall over
        /// </summary>
        private void PerformInstantDeath()
        {
            // 🛑 DISABLE NavMeshAgent (can't destroy - script requires it)
            if (_navAgent != null && _navAgent.enabled && _navAgent.isOnNavMesh)
            {
                _navAgent.isStopped = true;
                _navAgent.updatePosition = false;
                _navAgent.updateRotation = false;
                _navAgent.enabled = false;
                Debug.Log("[CompanionCore] 🛑 NavMeshAgent DISABLED");
            }
            else if (_navAgent != null)
            {
                _navAgent.enabled = false;
                Debug.Log("[CompanionCore] 🛑 NavMeshAgent force disabled (not on NavMesh)");
            }
            
            // 🛑 DESTROY CharacterController if exists (also keeps upright)
            CharacterController charController = GetComponent<CharacterController>();
            if (charController != null)
            {
                charController.enabled = false;
                Destroy(charController);
                Debug.Log("[CompanionCore] 🗑️ CharacterController DESTROYED");
            }

            // 🛑 STOP ALL SYSTEMS immediately with proper shutdown
            if (MovementSystem != null)
            {
                MovementSystem.InstantDeathStop(); // Custom death shutdown
            }
            if (CombatSystem != null) CombatSystem.enabled = false;
            if (TargetingSystem != null) TargetingSystem.enabled = false;
            if (_audioSystem != null) _audioSystem.enabled = false;

            // 💀 MAKE COMPANION FALL OVER (ragdoll effect) - BEFORE turning white
            if (_rigidbody != null)
            {
                // CRITICAL: Make rigidbody non-kinematic
                _rigidbody.isKinematic = false;
                _rigidbody.freezeRotation = false; // Allow falling over
                _rigidbody.constraints = RigidbodyConstraints.None; // Remove all constraints
                _rigidbody.useGravity = true; // Ensure gravity is on
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous; // Better collision
                
                // MASSIVE downward force to slam him down
                _rigidbody.AddForce(Vector3.down * 50000f, ForceMode.Impulse);

                // HUGE torque to make him fall FLAT (90 degrees)
                // Pick a random direction to fall (forward, backward, left, right)
                Vector3 fallDirection = UnityEngine.Random.value > 0.5f ? transform.forward : -transform.forward;
                if (UnityEngine.Random.value > 0.5f)
                {
                    fallDirection = UnityEngine.Random.value > 0.5f ? transform.right : -transform.right;
                }
                
                // Apply MASSIVE torque perpendicular to fall direction
                Vector3 torqueAxis = Vector3.Cross(fallDirection, Vector3.up).normalized;
                _rigidbody.AddTorque(torqueAxis * 100000f, ForceMode.Impulse); // HUGE torque!
                
                Debug.Log($"[CompanionCore] 💀 Ragdoll physics enabled - MASSIVE FALL! isKinematic: {_rigidbody.isKinematic}, useGravity: {_rigidbody.useGravity}");
            }
            else
            {
                Debug.LogError("[CompanionCore] ❌ NO RIGIDBODY - Cannot fall over!");
            }

            // 🎨 TURN WHITE (death color) - AFTER ragdoll setup
            TurnWhitePermanently();

            // 🛑 DISABLE THIS SCRIPT to stop all Update() calls
            this.enabled = false;

            Debug.Log("[CompanionCore] 💀 INSTANT DEATH COMPLETE: Companion is now a lifeless ragdoll");
        }
        
        /// <summary>
        /// 🎨 Turn all materials white on death - PERMANENTLY
        /// </summary>
        private void TurnWhitePermanently()
        {
            // CRITICAL: Stop all hit effect coroutines that might restore color
            StopAllCoroutines();
            
            // Also stop hit effects on EnemyCompanionBehavior if it exists
            EnemyCompanionBehavior enemyBehavior = GetComponent<EnemyCompanionBehavior>();
            if (enemyBehavior != null)
            {
                enemyBehavior.StopAllCoroutines();
            }
            
            // Also stop hit effects on TacticalEnemyAI if it exists
            TacticalEnemyAI tacticalAI = GetComponent<TacticalEnemyAI>();
            if (tacticalAI != null)
            {
                tacticalAI.StopAllCoroutines();
            }
            
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    // Set color to white
                    if (renderer.material.HasProperty("_Color"))
                    {
                        renderer.material.color = Color.white;
                    }
                    else if (renderer.material.HasProperty("_BaseColor"))
                    {
                        renderer.material.SetColor("_BaseColor", Color.white);
                    }
                    
                    // Disable emission if any
                    if (renderer.material.HasProperty("_EmissionColor"))
                    {
                        renderer.material.SetColor("_EmissionColor", Color.black);
                        renderer.material.DisableKeyword("_EMISSION");
                    }
                }
            }
            
            Debug.Log($"[CompanionCore] 🎨 Turned white PERMANENTLY - {renderers.Length} renderers updated, all coroutines stopped");
        }

        void OnDisable()
        {
            if (companionProfile != null)
            {
                companionProfile.SaveProgression();
            }

            MovementSystem?.Cleanup();
            CombatSystem?.Cleanup();
            TargetingSystem?.Cleanup();

            // Cleanup audio and visual systems using reflection
            if (_audioSystem != null)
            {
                var cleanupMethod = _audioSystem.GetType().GetMethod("Cleanup");
                cleanupMethod?.Invoke(_audioSystem, null);
            }
            if (_visualEffects != null)
            {
                var cleanupMethod = _visualEffects.GetType().GetMethod("Cleanup");
                cleanupMethod?.Invoke(_visualEffects, null);
            }
        }

        void OnDestroy()
        {
            if (_activeCompanions.Remove(this))
            {
                OnCompanionRemoved?.Invoke(this);
            }
        }
        
        // Helper methods for reflection-based calls
        private void CallAudioMethod(string methodName, params object[] parameters)
        {
            if (_audioSystem == null)
            {
                return;
            }

            var method = _audioSystem.GetType().GetMethod(methodName);
            method?.Invoke(_audioSystem, parameters);
        }
        
        private void CallVisualEffectsMethod(string methodName, params object[] parameters)
        {
            if (_visualEffects != null)
            {
                var method = _visualEffects.GetType().GetMethod(methodName);
                method?.Invoke(_visualEffects, parameters);
            }
        }
        
        // Public methods for other systems to call audio
        public void PlayShotgunSound()
        {
            CallAudioMethod("PlayShotgunSound");
        }
        
        public void PlayStreamSound()
        {
            CallAudioMethod("PlayStreamSound");
        }
        
        public void StopStreamSound()
        {
            CallAudioMethod("StopStreamSound");
        }
        
        // 💀 DEBUG METHODS for testing instant death system
        [ContextMenu("💀 Test Instant Death")]
        public void TestInstantDeath()
        {
            Debug.Log("[CompanionCore] 🧪 TESTING INSTANT DEATH SYSTEM...");
            Die();
        }
        
        [ContextMenu("📊 Show Death System Status")]
        public void ShowDeathSystemStatus()
        {
            Debug.Log("=== 💀 INSTANT DEATH SYSTEM STATUS ===");
            Debug.Log($"Is Dead: {_isDead}");
            Debug.Log($"Instant Death Enabled: {instantDeathOnContact}");
            Debug.Log($"Lethal Tags: [{string.Join(", ", lethalEnemyTags)}]");
            Debug.Log($"Health: {_health}/{maxHealth}");
            Debug.Log($"NavAgent Enabled: {_navAgent?.enabled ?? false}");
            Debug.Log($"Rigidbody Exists: {_rigidbody != null}");
            Debug.Log($"Collider Exists: {_collider != null}");
            Debug.Log($"Script Enabled: {enabled}");
            Debug.Log($"Movement System Enabled: {MovementSystem?.enabled ?? false}");
            Debug.Log($"Combat System Enabled: {CombatSystem?.enabled ?? false}");
            Debug.Log($"Targeting System Enabled: {TargetingSystem?.enabled ?? false}");
        }
    }
}