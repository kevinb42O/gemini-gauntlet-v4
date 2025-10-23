using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CompanionAI
{
    /// <summary>
    /// Handles target detection, selection, and threat assessment
    /// </summary>
    public class CompanionTargeting : MonoBehaviour
    {
        [Header("Detection Settings")]
        [Range(1000f, 10000f)] public float detectionRadius = 5000f;
        [Range(500f, 5000f)] public float attackRange = 2000f;
        [Range(500f, 2000f)] public float emergencyThreatDistance = 800f;
        [Header("Target Prefabs")]
        public GameObject skullEnemyPrefab1;
        public GameObject skullEnemyPrefab2;
        public GameObject skullEnemyPrefab3;
        
        [Header("Target Tags")]
        public string[] targetTags = new string[] { "Gem", "Skull" };
        public string[] priorityTagOrder = new string[] { "Skull", "Gem" };
        
        [Header("Enemy Companion Targeting")]
        [Tooltip("Enable targeting of enemy companions (companions with EnemyCompanionBehavior.isEnemy=true)")]
        public bool targetEnemyCompanions = true;
        [Tooltip("Prioritize enemy companions over skulls and gems")]
        public bool prioritizeEnemyCompanions = true;
        
        [Header("Threat Assessment")]
        public bool useIntelligentThreatSystem = true;
        [Range(0.1f, 0.5f)] public float threatScanInterval = 0.2f; // PERFORMANCE: Reduced from 0.03s to 0.2s (85% reduction in physics queries)
        [Tooltip("Removed swarm logic - companion now uses smart raycast-based targeting")]
        public bool useSmartTargeting = true;
        
        [Header("Performance")]
        [Range(0.05f, 1f)] public float enemyScanInterval = 0.2f;
        
        private CompanionCore _core;
        private Transform _transform;
        
        // Target tracking
        private Transform _currentTarget;
        private Transform _lockedTarget;
        private Transform _immediateThreat;
        private float _targetLockTime;
        private bool _isTargetLocked;
        
        // Threat assessment
        private readonly Collider[] _threatBuffer = new Collider[15];
        private readonly Collider[] _enemyBuffer = new Collider[20];
        private readonly Collider[] _enemyCompanionBuffer = new Collider[10]; // Buffer for enemy companion detection
        private int _enemyCount;
        private int _threatCount;
        private float _lastThreatScanTime;
        private bool _inEmergencyMode;
        private bool _isInAggressiveHuntingMode;
        
        // Smart targeting (removed swarm complexity)
        private int _nearbySkullCount = 0;
        
        // Coroutines
        private Coroutine _targetScanCoroutine;
        
        public Transform GetCurrentTarget() => _currentTarget;
        public bool IsInEmergencyMode => _inEmergencyMode;
        public int NearbySkullCount => _nearbySkullCount;
        
        public void Initialize(CompanionCore core)
        {
            _core = core;
            _transform = transform;
            
            StartTargetingSystem();
        }
        
        private void StartTargetingSystem()
        {
            // PERFORMANCE: Use Update instead of coroutine
            _lastTargetUpdateTime = Time.time;
            // _targetScanCoroutine = StartCoroutine(TargetScanLoop()); // REMOVED - using Update now
        }
        
        private IEnumerator TargetScanLoop()
        {
            while (enabled && !_core.IsDead)
            {
                UpdateTargeting();
                yield return new WaitForSeconds(enemyScanInterval);
            }
        }
        
        // PERFORMANCE OPTIMIZATION: Use Update instead of coroutine for better control
        private float _lastTargetUpdateTime;
        
        void Update()
        {
            if (_core == null || _core.IsDead) return;
            
            // Only update at specified interval, but use Update for better performance
            if (Time.time - _lastTargetUpdateTime >= enemyScanInterval)
            {
                UpdateTargeting();
                _lastTargetUpdateTime = Time.time;
            }
        }
        
        private void UpdateTargeting()
        {
            // IMMEDIATE THREAT DETECTION - Check for very close enemies first
            Transform immediateTarget = CheckForImmediateThreats();
            if (immediateTarget != null && immediateTarget != _currentTarget)
            {
                _currentTarget = immediateTarget;
                OnTargetChanged(immediateTarget);
                return; // Skip normal scanning for immediate response
            }
            
            // Scan for immediate threats first
            ScanForImmediateThreats();
            
            // Select best target
            Transform newTarget = SelectBestTarget();
            
            if (newTarget != _currentTarget)
            {
                _currentTarget = newTarget;
                OnTargetChanged(newTarget);
            }
        }
        
        private bool ScanForImmediateThreats()
        {
            if (!useIntelligentThreatSystem) return false;
            
            float currentTime = Time.time;
            if (currentTime - _lastThreatScanTime < threatScanInterval) 
                return _immediateThreat != null;
            
            _lastThreatScanTime = currentTime;
            
            _threatCount = Physics.OverlapSphereNonAlloc(_transform.position, emergencyThreatDistance, _threatBuffer);
            
            Transform closestThreat = null;
            float closestThreatDistance = float.MaxValue;
            
            for (int i = 0; i < _threatCount; i++)
            {
                Collider threat = _threatBuffer[i];
                if (threat == null || threat.isTrigger) continue;
                
                // Check for skull enemies
                if (!IsSkullEnemy(threat)) continue;
                
                IDamageable damageable = threat.GetComponent<IDamageable>();
                if (damageable == null) continue;
                
                float distance = Vector3.Distance(_transform.position, threat.transform.position);
                if (distance < closestThreatDistance)
                {
                    closestThreatDistance = distance;
                    closestThreat = threat.transform;
                }
            }
            
            bool wasInEmergency = _inEmergencyMode;
            _immediateThreat = closestThreat;
            _inEmergencyMode = closestThreat != null;
            
            if (_inEmergencyMode && !wasInEmergency)
            {
                Debug.Log($"[CompanionTargeting] 🚨 EMERGENCY MODE ACTIVATED! Skull threat at {closestThreatDistance:F1} units");
            }
            
            return _inEmergencyMode;
        }
        
        private Transform SelectBestTarget()
        {
            // Emergency override
            if (_inEmergencyMode && _immediateThreat != null)
            {
                return _immediateThreat;
            }
            
            // PRIORITY 1: Enemy companions (if enabled and prioritized)
            if (targetEnemyCompanions && prioritizeEnemyCompanions)
            {
                Transform enemyCompanion = FindClosestEnemyCompanion();
                if (enemyCompanion != null)
                {
                    return enemyCompanion;
                }
            }
            
            // PRIORITY 2: Threat tracker priority
            if (_isInAggressiveHuntingMode && EnemyThreatTracker.Instance != null)
            {
                SkullEnemy closestTrackedSkull = EnemyThreatTracker.Instance.GetClosestEnemy(_transform.position);
                if (closestTrackedSkull != null)
                {
                    float distanceToSkull = Vector3.Distance(_transform.position, closestTrackedSkull.transform.position);
                    if (distanceToSkull <= detectionRadius)
                    {
                        return closestTrackedSkull.transform;
                    }
                }
            }
            
            // PRIORITY 3: Enemy companions (if enabled but not prioritized)
            if (targetEnemyCompanions && !prioritizeEnemyCompanions)
            {
                Transform enemyCompanion = FindClosestEnemyCompanion();
                if (enemyCompanion != null)
                {
                    return enemyCompanion;
                }
            }
            
            return SelectSmartTarget();
        }
        
        private Transform SelectSmartTarget()
        {
            List<Transform> skulls = FindTargetSkulls();
            _nearbySkullCount = skulls.Count;
            
            Transform closestSkull = null;
            float closestDistanceSqr = float.MaxValue; // Use squared distance for performance
            
            Vector3 myPos = _transform.position;
            float detectionRadiusSqr = detectionRadius * detectionRadius;
            
            foreach (Transform skull in skulls)
            {
                if (skull == null) continue;
                
                // PERFORMANCE: Use sqrMagnitude instead of Distance (no sqrt calculation)
                float distanceSqr = (skull.position - myPos).sqrMagnitude;
                if (distanceSqr <= detectionRadiusSqr && distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    closestSkull = skull;
                }
            }
            
            // Check for gems as secondary targets
            Transform closestGem = FindClosestGem();
            
            // SMART TARGETING: Always prioritize closest threat, let bigger raycast handle groups
            return closestSkull ?? closestGem;
        }
        
        // PERFORMANCE CACHE: Cache skull search results
        private List<Transform> _cachedSkulls = new List<Transform>();
        private float _lastSkullCacheTime;
        private const float SKULL_CACHE_DURATION = 0.5f; // Cache for 0.5 seconds
        
        private List<Transform> FindTargetSkulls()
        {
            // PERFORMANCE: Use cached results if recent
            if (Time.time - _lastSkullCacheTime < SKULL_CACHE_DURATION)
            {
                return _cachedSkulls;
            }
            
            _cachedSkulls.Clear();
            
            // PERFORMANCE: Use OverlapSphere instead of FindObjectsByType (much faster)
            Collider[] nearbyColliders = Physics.OverlapSphere(_transform.position, detectionRadius);
            
            foreach (Collider collider in nearbyColliders)
            {
                SkullEnemy skull = collider.GetComponent<SkullEnemy>();
                if (skull != null && skull.gameObject.activeInHierarchy)
                {
                    if (IsInstanceOfAssignedPrefab(skull.gameObject))
                    {
                        _cachedSkulls.Add(skull.transform);
                    }
                }
            }
            
            _lastSkullCacheTime = Time.time;
            return _cachedSkulls;
        }
        
        private bool IsInstanceOfAssignedPrefab(GameObject obj)
        {
            if (obj == null) return false;
            
            string objName = obj.name.Replace("(Clone)", "").Trim();
            
            if (skullEnemyPrefab1 != null && objName.StartsWith(skullEnemyPrefab1.name)) return true;
            if (skullEnemyPrefab2 != null && objName.StartsWith(skullEnemyPrefab2.name)) return true;
            if (skullEnemyPrefab3 != null && objName.StartsWith(skullEnemyPrefab3.name)) return true;
            
            return false;
        }
        
        private Transform FindClosestGem()
        {
            _enemyCount = Physics.OverlapSphereNonAlloc(_transform.position, detectionRadius, _enemyBuffer);
            
            Transform closestGem = null;
            float closestDistance = float.MaxValue;
            
            for (int i = 0; i < _enemyCount; i++)
            {
                Collider enemy = _enemyBuffer[i];
                if (enemy == null || enemy.isTrigger) continue;
                
                Gem gemComp = enemy.GetComponent<Gem>();
                if (gemComp != null && IsValidGemTarget(gemComp))
                {
                    float distance = Vector3.Distance(_transform.position, enemy.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestGem = enemy.transform;
                    }
                }
            }
            
            return closestGem;
        }
        
        private bool IsValidGemTarget(Gem gemComp)
        {
            return !gemComp.IsDetached() &&
                   !gemComp.IsCollected() &&
                   !gemComp.IsBeingAttracted() &&
                   gemComp.CurrentHealth > 0 &&
                   gemComp.transform.parent != null;
        }
        
        private bool IsSkullEnemy(Collider collider)
        {
            return collider.GetComponent<SkullEnemy>() != null;
        }
        
        private void OnTargetChanged(Transform newTarget)
        {
            if (newTarget != null)
            {
                Debug.Log($"[CompanionTargeting] New target acquired: {newTarget.name}");
            }
            else
            {
                Debug.Log("[CompanionTargeting] Target lost");
            }
        }
        
        /// <summary>
        /// IMMEDIATE THREAT DETECTION - Ultra-fast detection for close enemies
        /// Performance optimized with small detection radius
        /// </summary>
        private Transform CheckForImmediateThreats()
        {
            // Small radius for performance - only check very close threats
            float immediateRadius = emergencyThreatDistance * 0.7f; // ~280 units
            
            // Use a small buffer for performance
            Collider[] immediateBuffer = new Collider[5]; // Only check 5 closest
            int count = Physics.OverlapSphereNonAlloc(_transform.position, immediateRadius, immediateBuffer);
            
            Transform closestThreat = null;
            float closestDistance = float.MaxValue;
            
            for (int i = 0; i < count; i++)
            {
                Collider threat = immediateBuffer[i];
                if (threat == null || threat.isTrigger) continue;
                
                // Check for any valid target (enemy companions, skulls or gems)
                bool isValidTarget = false;
                
                // PRIORITY: Check for enemy companions first
                if (targetEnemyCompanions && IsEnemyCompanion(threat))
                {
                    isValidTarget = true;
                }
                // Check for skull enemies
                else if (IsSkullEnemy(threat))
                {
                    isValidTarget = true;
                }
                // Check for gems
                else
                {
                    Gem gemComp = threat.GetComponent<Gem>();
                    if (gemComp != null && IsValidGemTarget(gemComp))
                    {
                        isValidTarget = true;
                    }
                }
                
                if (isValidTarget)
                {
                    float distance = Vector3.Distance(_transform.position, threat.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestThreat = threat.transform;
                    }
                }
            }
            
            if (closestThreat != null)
            {
                Debug.Log($"[CompanionTargeting] ⚡ IMMEDIATE THREAT DETECTED: {closestThreat.name} at {closestDistance:F1} units - SHOOTING NOW!");
            }
            
            return closestThreat;
        }
        
        public void SetAggressiveHuntingMode(bool enabled)
        {
            _isInAggressiveHuntingMode = enabled;
        }
        
        /// <summary>
        /// Find the closest enemy companion within detection radius
        /// Enemy companions are identified by having EnemyCompanionBehavior component with isEnemy=true
        /// </summary>
        private Transform FindClosestEnemyCompanion()
        {
            int count = Physics.OverlapSphereNonAlloc(_transform.position, detectionRadius, _enemyCompanionBuffer);
            
            Transform closestEnemyCompanion = null;
            float closestDistanceSqr = float.MaxValue;
            Vector3 myPos = _transform.position;
            
            for (int i = 0; i < count; i++)
            {
                Collider collider = _enemyCompanionBuffer[i];
                if (collider == null || collider.isTrigger) continue;
                
                // Check if this is an enemy companion
                if (!IsEnemyCompanion(collider)) continue;
                
                // Calculate distance (use sqrMagnitude for performance)
                float distanceSqr = (collider.transform.position - myPos).sqrMagnitude;
                if (distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    closestEnemyCompanion = collider.transform;
                }
            }
            
            if (closestEnemyCompanion != null)
            {
                float distance = Mathf.Sqrt(closestDistanceSqr);
                Debug.Log($"[CompanionTargeting] 🎯 ENEMY COMPANION DETECTED: {closestEnemyCompanion.name} at {distance:F1} units - ENGAGING!");
            }
            
            return closestEnemyCompanion;
        }
        
        /// <summary>
        /// Check if a collider belongs to an enemy companion
        /// Enemy companions have EnemyCompanionBehavior component with isEnemy=true
        /// </summary>
        private bool IsEnemyCompanion(Collider collider)
        {
            if (collider == null) return false;
            
            // Get EnemyCompanionBehavior component
            EnemyCompanionBehavior enemyBehavior = collider.GetComponent<EnemyCompanionBehavior>();
            if (enemyBehavior == null) return false;
            
            // Check if it's actually an enemy (isEnemy flag must be true)
            if (!enemyBehavior.isEnemy) return false;
            
            // Check if it's alive (has CompanionCore and not dead)
            CompanionCore core = collider.GetComponent<CompanionCore>();
            if (core == null || core.IsDead) return false;
            
            return true;
        }
        
        public void Cleanup()
        {
            if (_targetScanCoroutine != null)
            {
                StopCoroutine(_targetScanCoroutine);
                _targetScanCoroutine = null;
            }
        }
    }
}