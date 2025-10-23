// --- TowerController.cs (Fully Corrected for Inheritance) ---
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TowerController : MonoBehaviour
{
    // Static event for tower death notifications
    public static System.Action<TowerController> OnTowerDeath;
    
    [Header("Display Name")]
    public string displayName = "Tower";

    [Header("Gentle Rotation")]
    public float gentleSpinSpeed = 10f;

    [System.Serializable]
    public class SkullSpawnType
    {
        public GameObject skullPrefab;
        [Range(0f, 100f)]
        [Tooltip("Weight/chance for this skull type to spawn. Higher = more common.")]
        public float spawnWeight = 1f;
    }
    
    [Header("Skull Spawning")]
    public SkullSpawnType[] skullTypes = new SkullSpawnType[1];
    public Transform[] skullSpawnPoints;
    public string skullSpawnPointHolderName = "SkullSpawnPoints";
    public float skullSpawnInterval = 12f;
    public int maxActiveSkullsPerTower = 15;
    public float skullEjectionForce = 5f;
    public int skullsPerBurst = 5;
    public float burstSpreadAngle = 45f;
    public float burstSpawnOffsetRadius = 0.3f;
    public float delayBetweenBurstSkulls = 1;
    // FIXED: Use only property, not redundant field
    public bool IsDead { get; private set; } = false;

    [Header("Gems")]
    public GameObject[] gemPrefabs;
    public Transform[] gemSlots;

    [Header("Appearance Effects")]
    public GameObject towerAppearEffectPrefab;
    public AudioClip towerAppearSound;
    [Range(0f, 1f)] public float towerAppearSoundVolume = 0.8f;
    // NOTE: Emergence duration, depth, and curve are now handled by TowerSpawner

    [Header("Tower Death Sequence Controls")]
    public float delayBeforeCollapseStarts = 1.0f;
    public float deathSpinSpeed = 360f;
    public float deathSinkDuration = 2.0f;
    public float deathSinkDepth = 3.0f;
    public float finalDestroyDelay = 0.5f;
    public AudioClip towerCollapseSound;
    [Range(0f, 1f)] public float towerCollapseSoundVolume = 1.0f;
    public AudioClip skullSpawnSound;
    [Range(0f, 1f)] public float skullSpawnSoundVolume = 0.7f;

    // Protected members are accessible by child classes (like HybridTowerController)
    protected List<SkullEnemy> _activeSkulls = new List<SkullEnemy>();
    protected List<Gem> _initializedGems = new List<Gem>();
    protected float _nextSkullSpawnTimer;
    protected int _lastUsedSpawnPointIndex = -1; // Tracks last used spawn point for better distribution
    protected bool _isSpawningSkullBurst = false; // Flag to prevent multiple simultaneous bursts
    protected bool _canSpawnSkullsBasedOnPlayerPresence = false;
    protected bool _skullsAttackEnabled = false; // Controls if skulls should attack the player
    protected bool _skullSpawningEnabled = true; // Controls if this tower should spawn skulls at all
    
    // FIXED: Removed unused fields and emergence logic (now handled by TowerSpawner)
    protected PlatformTrigger _associatedPlatformTriggerInternal;
    public Transform _associatedPlatformTransform; // Used to track which platform this tower belongs to
    private TowerSoundManager towerSoundManager;
    protected bool _isCollapsing = false;
    protected bool _hasAppeared = false;
    protected Rigidbody _rb;
    
    // Tower Dancing State
    protected bool _isDancing = false;
    protected float _dancingStartTime = 0f;
    protected Vector3 _dancingOrigin;
    protected Vector3 _dancingTargetPosition;
    protected float _dancingSpeed = 300f;
    protected float _dancingRadius = 3000f;
    protected const float EMERGENCE_WAIT_TIME = 1f;
    protected bool _isFirstDanceMove = true; // Track if this is the first move after emergence
    
    // Skull spawn spin animation
    protected bool _isSpinningForSkullSpawn = false;
    protected float _currentSpinSpeed = 0f;
    protected const float SKULL_SPAWN_SPIN_DURATION = 1.2f;
    protected const float MAX_SKULL_SPAWN_SPIN_SPEED = 1800f;

    
    // Public accessors for debugging and visualization
    public bool SkullsAttackEnabled => _skullsAttackEnabled;
    public PlatformTrigger AssociatedPlatformTrigger => _associatedPlatformTriggerInternal;

    public static List<TowerController> ActiveTowers = new List<TowerController>();

    void Awake()
    {
        // FIXED: Add proper null checks and cleanup
        if (!ActiveTowers.Contains(this)) 
            ActiveTowers.Add(this);
            
        _rb = GetComponent<Rigidbody>();
        if (_rb != null) 
            _rb.isKinematic = true;

        towerSoundManager = GetComponent<TowerSoundManager>();
        if (towerSoundManager == null && Application.isPlaying)
        {
            towerSoundManager = gameObject.AddComponent<TowerSoundManager>();
        }

        _nextSkullSpawnTimer = skullSpawnInterval;
        
        // Auto-assign skull spawn points if not manually assigned
        if (skullSpawnPoints == null || skullSpawnPoints.Length == 0)
        {
            TryAutoAssignSkullSpawnPoints();
        }
    }

    void OnDestroy()
    {
        
        // CRITICAL: Fire death event BEFORE cleanup so ChestManager receives notification
        OnTowerDeath?.Invoke(this);
        
        // Set death flag for any remaining references
        IsDead = true;
        
        // FIXED: Proper cleanup to prevent memory leaks
        if (ActiveTowers.Contains(this))
        {
            ActiveTowers.Remove(this);
        }
        
        // Clean up active skulls to prevent orphaned references
        foreach (var skull in _activeSkulls)
        {
            if (skull != null)
            {
                Destroy(skull.gameObject);
            }
        }
        _activeSkulls.Clear();
    }

    protected virtual void Start()
    {
        // FIXED: Removed unused player cache - not needed
        // Initialize gems if they exist as children
        InitializeGems();
        _nextSkullSpawnTimer = skullSpawnInterval;
        
        // CRITICAL: Store platform association reliably
        EnsurePlatformAssociation();
        
        // CRITICAL: Ensure tower always faces "up" relative to parent platform
        AlignTowerUpWithPlatform();
        
        // CRITICAL: Tower starts INACTIVE - spawner will make it visible after emergence
        _hasAppeared = false;
    }



    void TryAutoAssignSkullSpawnPoints()
    {
        Transform holder = transform.Find(skullSpawnPointHolderName);
        if (holder != null)
        {
            skullSpawnPoints = holder.GetComponentsInChildren<Transform>(true)
                                   .Where(t => t.gameObject != holder.gameObject)
                                   .ToArray();
        }
    }

    public void SetAssociatedPlatformTrigger(PlatformTrigger trigger)
    {
        _associatedPlatformTriggerInternal = trigger;
        if (trigger != null && trigger.transform.parent != null)
        {
            _associatedPlatformTransform = trigger.transform.parent;
        }
    }

    /// <summary>
    /// Ensures platform association is properly stored and doesn't get lost
    /// Made public so TowerSpawner can call it for consistent platform tracking
    /// </summary>
    public void EnsurePlatformAssociation()
    {
        // If we don't have an associated platform yet, try to find our parent platform
        if (_associatedPlatformTransform == null && transform.parent != null)
        {
            Transform potentialPlatform = transform.parent;
            
            // If parent is a PlatformTrigger, get its parent (the actual platform)
            if (potentialPlatform.name.ToLower().Contains("trigger") || 
                potentialPlatform.GetComponent<PlatformTrigger>() != null)
            {
                if (potentialPlatform.parent != null)
                {
                    _associatedPlatformTransform = potentialPlatform.parent;
                }
                else
                {
                    // Fallback: use the trigger if no parent platform found
                    _associatedPlatformTransform = potentialPlatform;
                }
            }
            else
            {
                // Parent is the actual platform
                _associatedPlatformTransform = potentialPlatform;
            }
        }
        else if (_associatedPlatformTransform == null && transform.parent == null)
        {
            // Try to find platform by searching upwards in hierarchy or nearby
            Transform platform = FindNearbyPlatform();
            if (platform != null)
            {
                _associatedPlatformTransform = platform;
            }
            else
            {
            }
        }
        else if (_associatedPlatformTransform != null)
        {
            // Check if current association is a trigger and needs to be updated to the actual platform
            if (_associatedPlatformTransform.name.ToLower().Contains("trigger") || 
                _associatedPlatformTransform.GetComponent<PlatformTrigger>() != null)
            {
                if (_associatedPlatformTransform.parent != null)
                {
                    Transform actualPlatform = _associatedPlatformTransform.parent;
                    _associatedPlatformTransform = actualPlatform;
                }
            }
            else
            {
            }
        }
    }
    
    /// <summary>
    /// Try to find a nearby platform if parent relationship is lost
    /// </summary>
    Transform FindNearbyPlatform()
    {
        // Look for platforms within a reasonable distance
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, 10f);
        foreach (Collider col in nearbyColliders)
        {
            // Look for objects on Platform layer or with "Platform" in name
            if (col.gameObject.layer == LayerMask.NameToLayer("Platform") || 
                col.name.ToLower().Contains("platform"))
            {
                return col.transform;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Ensures the tower always faces "up" relative to its parent platform, regardless of platform tilt/rotation
    /// </summary>
    void AlignTowerUpWithPlatform()
    {
        if (transform.parent == null)
        {
            return;
        }
        
        // The tower should face "up" relative to the platform's local up direction
        // Since the tower is a child of the platform, we want the tower's local up to align with Vector3.up
        // This means the tower's local rotation should be identity (no rotation relative to parent)
        transform.localRotation = Quaternion.identity;
        
    }
    
    /// <summary>
    /// Activates any inactive child gems when the tower emerges
    /// </summary>
    void ActivateChildGems()
    {
        
        // Find all Gem components in direct children (not nested)
        Gem[] childGems = GetComponentsInChildren<Gem>(true); // true = include inactive
        
        int gemsActivated = 0;
        int directChildGems = 0;
        
        foreach (Gem gem in childGems)
        {
            if (gem != null)
            {
                bool isDirectChild = gem.transform.parent == transform;
                bool isGemslotChild = gem.transform.parent != null && gem.transform.parent.parent == transform;
                bool isInactive = !gem.gameObject.activeSelf;
                
                
                if (isDirectChild || isGemslotChild)
                {
                    directChildGems++;
                    if (isInactive)
                    {
                        gem.gameObject.SetActive(true);
                        gemsActivated++;
                    }
                    else
                    {
                    }
                }
            }
        }
        
        
        if (gemsActivated > 0)
        {
        }
        else if (directChildGems == 0)
        {
        }
        else
        {
        }
    }
    
    /// <summary>
    /// Helper method to parent an object to the platform while preserving its original scale
    /// </summary>
    void ParentToPlatformWithScalePreservation(GameObject objectToParent, string debugName = "Object")
    {
        // Find the platform this tower is on
        Transform platformParent = transform.parent;
        if (platformParent != null)
        {
            // Store original world scale before parenting
            Vector3 originalWorldScale = objectToParent.transform.lossyScale;
            
            // Set object as child of platform
            objectToParent.transform.SetParent(platformParent);
            
            // Calculate correct local scale to maintain world scale
            Vector3 parentWorldScale = platformParent.lossyScale;
            Vector3 correctedLocalScale = new Vector3(
                originalWorldScale.x / parentWorldScale.x,
                originalWorldScale.y / parentWorldScale.y,
                originalWorldScale.z / parentWorldScale.z
            );
            
            // Apply the corrected local scale
            objectToParent.transform.localScale = correctedLocalScale;
            
        }
        else
        {
        }
    }

    void InitializeGems()
    {
        foreach (Gem oldGem in _initializedGems) { if (oldGem != null && oldGem.gameObject != null) Destroy(oldGem.gameObject); }
        _initializedGems.Clear();

        if (gemPrefabs == null || gemPrefabs.Length == 0 || gemSlots == null || gemSlots.Length == 0) return;

        for (int i = 0; i < gemSlots.Length; i++)
        {
            if (gemSlots[i] == null) continue;

            GameObject prefabToUse = gemPrefabs.FirstOrDefault(p => p != null);
            if (i < gemPrefabs.Length && gemPrefabs[i] != null) prefabToUse = gemPrefabs[i];

            if (prefabToUse == null) continue;
            if (prefabToUse.GetComponent<Gem>() == null) { continue; }

            GameObject gemGO = Instantiate(prefabToUse, gemSlots[i].position, gemSlots[i].rotation, gemSlots[i]);
            
            // Parent gem directly to the tower so it stays with the tower when it moves
            // Note: gems are already parented to gemSlots[i] which is a child of the tower
            
            Gem gemScript = gemGO.GetComponent<Gem>();
            if (gemScript != null) _initializedGems.Add(gemScript);
            
            // Gems now spawn visible by default, just like towers
        }
    }

    // Helper method to get the associated platform trigger
    public PlatformTrigger GetAssociatedPlatformTrigger()
    {
        // First check if we have a direct reference
        if (_associatedPlatformTriggerInternal != null)
        {
            return _associatedPlatformTriggerInternal;
        }
        
        // If not, try to find it through platform association
        if (_associatedPlatformTransform != null)
        {
            // Check if platform has a trigger on it
            PlatformTrigger trigger = _associatedPlatformTransform.GetComponentInChildren<PlatformTrigger>(true);
            if (trigger != null)
            {
                _associatedPlatformTriggerInternal = trigger;
                return trigger;
            }
        }
        
        // Last resort - try to find any platform trigger nearby
        EnsurePlatformAssociation(); // Make sure we have a platform association
        
        // If we found a platform association, look for triggers
        if (_associatedPlatformTransform != null)
        {
            PlatformTrigger[] triggers = _associatedPlatformTransform.GetComponentsInChildren<PlatformTrigger>(true);
            if (triggers != null && triggers.Length > 0)
            {
                _associatedPlatformTriggerInternal = triggers[0];
                return triggers[0];
            }
        }
        
        return null;
    }

    protected virtual void Update()
    {
        if (IsDead || _isCollapsing || !_hasAppeared) return;

        // Handle skull spawn spin animation (overrides gentle spin)
        if (_isSpinningForSkullSpawn)
        {
            transform.Rotate(Vector3.up, _currentSpinSpeed * Time.deltaTime, Space.Self);
        }
        else if (gentleSpinSpeed != 0)
        {
            // Use Space.Self so tower spins around its own local Y-axis, maintaining platform alignment
            transform.Rotate(Vector3.up, gentleSpinSpeed * Time.deltaTime, Space.Self);
        }
        
        // Handle tower dancing movement
        if (_isDancing)
        {
            UpdateDancingMovement();
        }

        // FIXED: Only spawn skulls when all conditions are met
        if (_isSpawningSkullBurst)
        {
            // If we're already in the middle of a skull burst, don't start another one
            return;
        }
        
        // FIXED: Respect inspector settings and enforce platform detection
        if (_skullSpawningEnabled && HasValidSkullTypes() && skullSpawnPoints.Length > 0)
        {
            // FIXED: First ensure we have not exceeded max skull count from inspector
            _activeSkulls.RemoveAll(s => s == null); // Clean up null references
            if (_activeSkulls.Count >= maxActiveSkullsPerTower)
            {
                // Already at max capacity - don't spawn more skulls
                return;
            }
            
            // Verify player is still on platform using reliable platform detection
            PlatformTrigger platformTrigger = GetAssociatedPlatformTrigger();
            bool playerOnPlatform = platformTrigger != null && platformTrigger.IsPlayerOnThisPlatform();
            
            if (!playerOnPlatform && _canSpawnSkullsBasedOnPlayerPresence)
            {
                // Player left platform - stop spawning
                _skullSpawningEnabled = false;
                return;
            }
            
            // All checks passed, proceed with timer-based spawning
            _nextSkullSpawnTimer -= Time.deltaTime;
            if (_nextSkullSpawnTimer <= 0)
            {
                // Reset timer using inspector setting
                _nextSkullSpawnTimer = skullSpawnInterval;
                StartCoroutine(SpawnSkullBurstCoroutine());
            }
        }

        // Update all active skulls' attack state if needed
        foreach (SkullEnemy skull in _activeSkulls)
        {
            if (skull != null)
            {
                // Skull attack behavior is managed through AI states - no need for SetAttackEnabled
            }
        }
    }

    public void OnPlayerEnteredPlatform()
    {
        _canSpawnSkullsBasedOnPlayerPresence = true;
        
        // Resume skull spawning when player returns (if tower has already appeared)
        if (_hasAppeared)
        {
            ResumeSkullSpawning();
        }
        // NOTE: Emergence is now handled by TowerSpawner, not TowerController
    }

    public void OnPlayerLeftPlatform()
    {
        _canSpawnSkullsBasedOnPlayerPresence = false;
        // NOTE: Emergence cancellation is now handled by TowerSpawner, not TowerController
        
        // Stop spawning new skulls but keep existing ones alive and aggressive
        if (_skullSpawningEnabled)
        {
            _skullSpawningEnabled = false;
        }
        
        // Note: Skulls remain aggressive and continue to hunt the player
    }
    
    /// <summary>
    /// Stops this tower from spawning new skulls when player leaves the platform
    /// Existing skulls will remain active and continue to hunt the player
    /// </summary>
    public void StopSkullSpawning()
    {
        if (_skullSpawningEnabled)
        {
            _skullSpawningEnabled = false;
        }
    }
    
    /// <summary>
    /// Resumes skull spawning when player returns to the platform
    /// </summary>
    public void ResumeSkullSpawning()
    {
        if (!_skullSpawningEnabled)
        {
            _skullSpawningEnabled = true;
            
            // Reset the timer to start spawning quickly
            _nextSkullSpawnTimer = 1.0f;
        }
    }
    
    /// <summary>
    /// FIXED: Called by TowerSpawner when emergence animation completes
    /// TowerController no longer handles emergence - only post-emergence behavior
    /// </summary>
    public virtual void OnEmergenceComplete()
    {
        // CRITICAL: Prevent multiple calls to this method (sound spam protection)
        if (_hasAppeared)
        {
            Debug.LogWarning($"[TowerController] OnEmergenceComplete called multiple times on {name}! Ignoring duplicate call.");
            return;
        }
        
        _hasAppeared = true;
        
        // Activate child gems
        ActivateChildGems();
        
        // SIMPLIFIED: If tower emerged, player is obviously there - enable skull spawning immediately!
        _skullSpawningEnabled = true;
        
        // Use full interval from inspector settings for first spawn
        _nextSkullSpawnTimer = skullSpawnInterval;
        
        // Play appearance effects
        // WARNING: Check towerAppearEffectPrefab in Unity Inspector - if it has AudioSource components,
        // they may ALSO play TowerAppear sounds, causing duplicate/overlapping audio!
        // Consider removing AudioSources from the VFX prefab since sound is handled by TowerSoundManager
        if (towerAppearEffectPrefab != null)
        {
            Instantiate(towerAppearEffectPrefab, transform.position, transform.rotation);
        }
        
        // Play sound through sound system - ONLY ONCE!
        if (towerSoundManager != null)
        {
            Debug.Log($"[TowerController] 🔊 Playing tower appear sound for {name}");
            towerSoundManager.PlayTowerAppear(towerAppearSound, towerAppearSoundVolume);
        }
        
        // Start dancing after wait period
        StartCoroutine(StartDancingAfterWait());
    }

    /// <summary>
    /// Wait 1 second after emergence, then start dancing movement
    /// </summary>
    protected IEnumerator StartDancingAfterWait()
    {
        Debug.Log($"[TowerController] {name} StartDancingAfterWait() called, waiting {EMERGENCE_WAIT_TIME}s...");
        yield return new WaitForSeconds(EMERGENCE_WAIT_TIME);
        
        // Store origin position (current spawn height on platform)
        _dancingOrigin = transform.localPosition;
        
        _isDancing = true;
        _dancingStartTime = Time.time;
        _isFirstDanceMove = true; // Reset first move flag
        Debug.Log($"[TowerController] {name} Dancing ENABLED - origin: {_dancingOrigin}, radius: {_dancingRadius}");
        PickNewDancingTarget();
    }
    
    /// <summary>
    /// Pick a new random position that stays within platform boundaries
    /// First move prefers moving towards the center of the platform
    /// </summary>
    protected void PickNewDancingTarget()
    {
        // Get the platform trigger to determine boundaries
        PlatformTrigger platformTrigger = GetAssociatedPlatformTrigger();
        if (platformTrigger == null)
        {
            Debug.LogWarning($"[TowerController] {name} has no associated platform trigger - using fallback dancing");
            // Fallback to old behavior if no platform found
            Vector2 randomCircle = Random.insideUnitCircle * _dancingRadius;
            _dancingTargetPosition = _dancingOrigin + new Vector3(randomCircle.x, 0f, randomCircle.y);
            return;
        }
        
        // Get platform trigger bounds (in world space)
        Collider platformCollider = platformTrigger.GetComponent<Collider>();
        if (platformCollider == null)
        {
            Debug.LogWarning($"[TowerController] {name} platform trigger has no collider - using fallback dancing");
            Vector2 randomCircle = Random.insideUnitCircle * _dancingRadius;
            _dancingTargetPosition = _dancingOrigin + new Vector3(randomCircle.x, 0f, randomCircle.y);
            return;
        }
        
        Bounds platformBounds = platformCollider.bounds;
        Vector3 platformCenter = platformBounds.center;
        
        // Convert platform center to local space of the tower's parent
        Vector3 localPlatformCenter = transform.parent != null ? 
            transform.parent.InverseTransformPoint(platformCenter) : platformCenter;
        
        // FIRST MOVE: Prefer moving towards the center of the platform
        if (_isFirstDanceMove)
        {
            _isFirstDanceMove = false;
            
            // Move towards center, but not exactly to center (leave some randomness)
            Vector3 directionToCenter = (localPlatformCenter - _dancingOrigin).normalized;
            float distanceToCenter = Vector3.Distance(_dancingOrigin, localPlatformCenter);
            
            // Move 60-80% of the way towards center
            float moveDistance = distanceToCenter * Random.Range(0.6f, 0.8f);
            _dancingTargetPosition = _dancingOrigin + directionToCenter * moveDistance;
            
            // Keep Y position constant
            _dancingTargetPosition.y = _dancingOrigin.y;
            
            Debug.Log($"[TowerController] {name} FIRST DANCE MOVE towards center: {_dancingTargetPosition}");
            return;
        }
        
        // SUBSEQUENT MOVES: Stay within platform boundaries
        int maxAttempts = 10;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Generate random position within a reasonable radius
            Vector2 randomCircle = Random.insideUnitCircle * _dancingRadius;
            Vector3 candidatePosition = _dancingOrigin + new Vector3(randomCircle.x, 0f, randomCircle.y);
            
            // Convert to world space to check against platform bounds
            Vector3 worldCandidatePosition = transform.parent != null ? 
                transform.parent.TransformPoint(candidatePosition) : candidatePosition;
            
            // Check if position is within platform bounds (with a safety margin)
            float safetyMargin = 2f; // Stay 2 units away from edges
            bool withinBounds = 
                worldCandidatePosition.x >= platformBounds.min.x + safetyMargin &&
                worldCandidatePosition.x <= platformBounds.max.x - safetyMargin &&
                worldCandidatePosition.z >= platformBounds.min.z + safetyMargin &&
                worldCandidatePosition.z <= platformBounds.max.z - safetyMargin;
            
            if (!withinBounds)
            {
                continue; // Try another position
            }
            
            // Check collision with other towers
            bool tooClose = false;
            foreach (TowerController otherTower in ActiveTowers)
            {
                if (otherTower != null && otherTower != this && !otherTower.IsDead)
                {
                    float distance = Vector3.Distance(candidatePosition, otherTower.transform.localPosition);
                    if (distance < 5f)
                    {
                        tooClose = true;
                        break;
                    }
                }
            }
            
            if (!tooClose)
            {
                _dancingTargetPosition = candidatePosition;
                Debug.Log($"[TowerController] {name} picked valid dancing target within bounds (attempt {attempt + 1})");
                return;
            }
        }
        
        // If we couldn't find a valid position after max attempts, stay at current position
        Debug.LogWarning($"[TowerController] {name} couldn't find valid dancing position - staying put");
        _dancingTargetPosition = transform.localPosition;
    }
    
    /// <summary>
    /// Update tower dancing movement each frame
    /// </summary>
    protected void UpdateDancingMovement()
    {
        if (!_isDancing) return;
        
        // Move towards target
        Vector3 currentPos = transform.localPosition;
        Vector3 newPos = Vector3.MoveTowards(currentPos, _dancingTargetPosition, _dancingSpeed * Time.deltaTime);
        
        // Keep Y position constant (only move on XZ plane)
        newPos.y = _dancingOrigin.y;
        
        // Debug occasionally
        if (Time.frameCount % 120 == 0)
        {
            float dist = Vector3.Distance(currentPos, _dancingTargetPosition);
            Debug.Log($"[TowerController] {name} Dancing: Distance to target={dist:F1}");
        }
        
        transform.localPosition = newPos;
        
        // Check if reached target
        float distanceToTarget = Vector3.Distance(new Vector3(currentPos.x, 0, currentPos.z), 
                                                   new Vector3(_dancingTargetPosition.x, 0, _dancingTargetPosition.z));
        if (distanceToTarget < 0.1f)
        {
            // Pick new target
            PickNewDancingTarget();
        }
    }
    
    protected virtual IEnumerator SpawnSkullBurstCoroutine()
    {
        // If we're already spawning, don't start another burst
        if (_isSpawningSkullBurst) 
        {
            yield break;
        }
        
        // Skip spawning if tower is in invalid state
        if (IsDead || _isCollapsing || !_hasAppeared || !HasValidSkullTypes() || skullSpawnPoints.Length == 0) 
        {
            yield break;
        }
        
        // Clean up any null references in the active skulls list BEFORE counting
        _activeSkulls.RemoveAll(s => s == null);
        
        // Check if we're already at max capacity
        if (_activeSkulls.Count >= maxActiveSkullsPerTower)
        {
            yield break;
        }
        
        // Set the flag to prevent multiple simultaneous bursts
        _isSpawningSkullBurst = true;
        
        // COOL SPIN ANIMATION: Accelerate to max speed, then decelerate
        yield return StartCoroutine(SkullSpawnSpinAnimation());

        // SIMPLE: Play tower SHOOT sound when spawning skulls (ONE sound per burst, not per skull)
        if (towerSoundManager != null)
        {
            towerSoundManager.PlayTowerShoot();
        }
        
        // Calculate how many skulls we can actually spawn this burst
        // This should ALWAYS be limited by the max capacity
        int skullsToSpawnThisBurst = Mathf.Min(skullsPerBurst, maxActiveSkullsPerTower - _activeSkulls.Count);
        
        
        // If we can't spawn any skulls, exit early
        if (skullsToSpawnThisBurst <= 0)
        {
            _isSpawningSkullBurst = false; // Clear the flag
            yield break;
        }
        
        int spawnedCount = 0; // Track how many skulls we actually spawn successfully
        List<Transform> usedSpawnPoints = new List<Transform>(); // Track which spawn points we've used
        
        // Create all skulls at once in a single frame - no delays between spawns
        for (int i = 0; i < skullsToSpawnThisBurst; i++)
        {
            // Double-check we're not exceeding capacity
            if (_activeSkulls.Count >= maxActiveSkullsPerTower)
            {
                break;
            }

            // Pick a random spawn point with improved distribution
            Transform spawnPoint;
            if (skullSpawnPoints.Length > 1 && skullsToSpawnThisBurst > 1)
            {
                // For multiple spawns, try to use different spawn points when available
                List<Transform> availablePoints = new List<Transform>();
                
                // Get points we haven't used yet in this burst
                foreach (Transform point in skullSpawnPoints)
                {
                    if (!usedSpawnPoints.Contains(point))
                    {
                        availablePoints.Add(point);
                    }
                }
                
                // If we've used all points or have only one, reset and use any
                if (availablePoints.Count == 0)
                {
                    availablePoints = new List<Transform>(skullSpawnPoints);
                }
                
                // Select from remaining points
                int randomIndex = Random.Range(0, availablePoints.Count);
                spawnPoint = availablePoints[randomIndex];
                usedSpawnPoints.Add(spawnPoint); // Mark as used
                
                // Find the index in the original array for logging
                int spawnPointIndex = System.Array.IndexOf(skullSpawnPoints, spawnPoint);
            }
            else
            {
                // Simple random selection when only one point exists
                int randomIndex = Random.Range(0, skullSpawnPoints.Length);
                spawnPoint = skullSpawnPoints[randomIndex];
            }
            
            // Add a small random offset to the spawn position
            Vector3 randomOffset = Random.insideUnitSphere * burstSpawnOffsetRadius;
            Vector3 spawnPos = spawnPoint.position + randomOffset;
            
            // Calculate a random direction within the burst spread angle
            // FIXED: Find player dynamically instead of using cached reference
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            Vector3 dirToPlayer = playerObj != null ? (playerObj.transform.position - spawnPos).normalized : Vector3.forward;
            Quaternion randomRotation = Quaternion.Euler(
                Random.Range(-burstSpreadAngle, burstSpreadAngle),
                Random.Range(-burstSpreadAngle, burstSpreadAngle),
                Random.Range(-burstSpreadAngle, burstSpreadAngle));
            Vector3 adjustedDirection = randomRotation * dirToPlayer;
            
            // Pick random skull type based on weights
            GameObject selectedSkullPrefab = GetRandomSkullPrefab();
            if (selectedSkullPrefab == null)
            {
                continue; // Skip if no valid prefab
            }
            
            // Instantiate the skull WITHOUT parenting initially
            GameObject skullObj = Instantiate(selectedSkullPrefab, spawnPos, Quaternion.LookRotation(adjustedDirection));
            
            // Set an explicit scale of 2.5 (larger than the prefab's original scale)
            Vector3 desiredWorldScale = new Vector3(2.5f, 2.5f, 2.5f);
            skullObj.transform.localScale = desiredWorldScale;
            
            // IMPORTANT: Now that scale is explicitly set, we can parent it
            // Parent directly to the associated platform so skull moves with platform
            if (_associatedPlatformTransform != null)
            {
                // Parent with worldPositionStays=true
                skullObj.transform.SetParent(_associatedPlatformTransform, true);
                
                // CRITICAL FIX: Recalculate local scale to maintain desired world scale
                Vector3 parentWorldScale = _associatedPlatformTransform.lossyScale;
                Vector3 correctedLocalScale = new Vector3(
                    desiredWorldScale.x / parentWorldScale.x,
                    desiredWorldScale.y / parentWorldScale.y,
                    desiredWorldScale.z / parentWorldScale.z
                );
                skullObj.transform.localScale = correctedLocalScale;
                
                Debug.Log($"[TowerController] Skull spawned with corrected scale. Desired world: {desiredWorldScale}, Parent scale: {parentWorldScale}, Final local: {correctedLocalScale}");
            }
            else
            {
            }
            
            SkullEnemy skull = skullObj.GetComponent<SkullEnemy>();
            if (skull != null)
            {
                // FIXED: Use new SkullEnemy API - removed player parameter
                skull.InitializeSkull(_associatedPlatformTriggerInternal, this, false);
                _activeSkulls.Add(skull);
                spawnedCount++;
            }
            else
            {
            }
        }
        
        // No need for additional sound here - the tower emerge and skullspawn sounds are sufficient
        
        // Clear the flag so future bursts can spawn
        _isSpawningSkullBurst = false;
    }
    
    /// <summary>
    /// Check if tower has at least one valid skull type configured
    /// </summary>
    protected bool HasValidSkullTypes()
    {
        if (skullTypes == null || skullTypes.Length == 0)
            return false;
            
        foreach (SkullSpawnType skullType in skullTypes)
        {
            if (skullType != null && skullType.skullPrefab != null && skullType.spawnWeight > 0f)
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Get random skull prefab based on spawn weights
    /// </summary>
    protected GameObject GetRandomSkullPrefab()
    {
        if (skullTypes == null || skullTypes.Length == 0)
            return null;
        
        // Calculate total weight
        float totalWeight = 0f;
        foreach (SkullSpawnType skullType in skullTypes)
        {
            if (skullType != null && skullType.skullPrefab != null && skullType.spawnWeight > 0f)
            {
                totalWeight += skullType.spawnWeight;
            }
        }
        
        if (totalWeight <= 0f)
            return null;
        
        // Pick random value
        float randomValue = Random.Range(0f, totalWeight);
        
        // Find which skull type this value corresponds to
        float currentWeight = 0f;
        foreach (SkullSpawnType skullType in skullTypes)
        {
            if (skullType != null && skullType.skullPrefab != null && skullType.spawnWeight > 0f)
            {
                currentWeight += skullType.spawnWeight;
                if (randomValue <= currentWeight)
                {
                    return skullType.skullPrefab;
                }
            }
        }
        
        // Fallback: return first valid skull
        foreach (SkullSpawnType skullType in skullTypes)
        {
            if (skullType != null && skullType.skullPrefab != null)
                return skullType.skullPrefab;
        }
        
        return null;
    }
    
    /// <summary>
    /// Cool spin animation: slow -> fast -> slow when spawning skulls
    /// </summary>
    protected IEnumerator SkullSpawnSpinAnimation()
    {
        _isSpinningForSkullSpawn = true;
        _currentSpinSpeed = 0f;
        
        float elapsed = 0f;
        float halfDuration = SKULL_SPAWN_SPIN_DURATION / 2f;
        
        // Accelerate to max speed (first half)
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            // Smooth acceleration curve
            float smoothT = t * t * (3f - 2f * t);
            _currentSpinSpeed = Mathf.Lerp(0f, MAX_SKULL_SPAWN_SPIN_SPEED, smoothT);
            yield return null;
        }
        
        // Decelerate back to zero (second half)
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            // Smooth deceleration curve
            float smoothT = t * t * (3f - 2f * t);
            _currentSpinSpeed = Mathf.Lerp(MAX_SKULL_SPAWN_SPIN_SPEED, 0f, smoothT);
            yield return null;
        }
        
        _currentSpinSpeed = 0f;
        _isSpinningForSkullSpawn = false;
    }

    public void OnGemDestroyed(Gem destroyedGem)
    {
        if (IsDead || _isCollapsing || destroyedGem == null) return;
        
        // Count active, NON-DETACHED child gems
        Gem[] activeChildGems = GetComponentsInChildren<Gem>(false); // false = only active gems
        int activeNonDetachedGemCount = 0;
        
        foreach (Gem gem in activeChildGems)
        {
            if (gem != null && gem != destroyedGem && gem.gameObject.activeInHierarchy && !gem.IsDetached())
            {
                activeNonDetachedGemCount++;
            }
        }
        
        
        // Only collapse when ALL gems are destroyed (detached)
        if (activeNonDetachedGemCount == 0 && !_isCollapsing)
        {
            StartCollapseSequence();
        }
    }

    /// <summary>
    /// Detaches all skulls from this tower so they remain alive and independent after tower death
    /// </summary>
    private void DetachSkullsFromTower()
    {
        
        // Clear the skull list but don't destroy the skulls - let them live independently
        foreach (SkullEnemy skull in _activeSkulls)
        {
            if (skull != null)
            {
                // CRITICAL: Clear the skull's reference to this tower to prevent MissingReferenceException
                skull.ClearTowerReference();
            }
        }
        
        // Clear the tower's reference to skulls, but skulls remain alive in the world
        _activeSkulls.Clear();
        
    }

    public void StartCollapseSequence()
    {
        if (_isCollapsing || IsDead) return;
        _isCollapsing = true;
        IsDead = true;
        _canSpawnSkullsBasedOnPlayerPresence = false;
        _isDancing = false; // Stop dancing when collapsing
        _isFirstDanceMove = true; // Reset for potential respawn
        // DynamicPlayerFeedManager.Instance?.ShowKillFeed(displayName, FeedIconsSO.KillFeedType.Tower);

        if (towerSoundManager != null)
        {
            towerSoundManager.PlayTowerDeath();
        }
        
        // CRITICAL: Ensure platform association before death event
        EnsurePlatformAssociation();
        
        // SKULL INDEPENDENCE: Detach skulls from tower so they remain alive after tower death
        DetachSkullsFromTower();
        
        // --- XP SYSTEM: Grant XP for tower destruction (UPGRADED!) ---
        GeminiGauntlet.Progression.XPHooks.OnTowerDestroyed(transform.position);
        
        // Notify the spawner that this tower has died
        OnTowerDeath?.Invoke(this);
        
        
        StartCoroutine(CollapseSequenceCoroutine());
    }

    IEnumerator CollapseSequenceCoroutine()
    {
        yield return new WaitForSeconds(delayBeforeCollapseStarts);
        
        // Calculate tower height to sink underground
        float towerHeight = 5f; // Default fallback
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds combinedBounds = renderers[0].bounds;
            foreach (Renderer r in renderers)
            {
                combinedBounds.Encapsulate(r.bounds);
            }
            towerHeight = combinedBounds.size.y;
        }
        
        float timer = 0f;
        Vector3 startLocalPosition = transform.localPosition;
        Vector3 endLocalPosition = startLocalPosition - new Vector3(0f, towerHeight, 0f); // Sink by tower's own height
        float sinkDuration = 2.0f;
        
        _isSpinningForSkullSpawn = true; // Reuse spin animation flag
        _currentSpinSpeed = 0f;
        
        float halfDuration = sinkDuration / 2f;
        
        // Sink and spin simultaneously
        while (timer < sinkDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / sinkDuration;
            
            // Smooth sink using smoothstep
            float smoothProgress = progress * progress * (3f - 2f * progress);
            transform.localPosition = Vector3.Lerp(startLocalPosition, endLocalPosition, smoothProgress);
            
            // Cool spin: accelerate first half, decelerate second half
            if (timer < halfDuration)
            {
                // Accelerate
                float t = timer / halfDuration;
                float smoothT = t * t * (3f - 2f * t);
                _currentSpinSpeed = Mathf.Lerp(0f, MAX_SKULL_SPAWN_SPIN_SPEED, smoothT);
            }
            else
            {
                // Decelerate
                float t = (timer - halfDuration) / halfDuration;
                float smoothT = t * t * (3f - 2f * t);
                _currentSpinSpeed = Mathf.Lerp(MAX_SKULL_SPAWN_SPIN_SPEED, 0f, smoothT);
            }
            
            // Spin is handled in Update()
            yield return null;
        }
        
        transform.localPosition = endLocalPosition;
        _currentSpinSpeed = 0f;
        _isSpinningForSkullSpawn = false;
        
        yield return new WaitForSeconds(0.2f);
        
        if (ActiveTowers.Contains(this))
        {
            ActiveTowers.Remove(this);
        }
        
        Destroy(gameObject);
    }

    // OnPlayerEnteredPlatform moved to the top of the file (line ~326) to avoid duplicate method

    // OnPlayerLeftPlatform moved to the top of the file (line ~346) to avoid duplicate method

    public void SkullDied(SkullEnemy skull)
    {
        if (skull != null)
        {
            if (_activeSkulls.Contains(skull))
            {
                _activeSkulls.Remove(skull);
            }
            
            // SKULL INDEPENDENCE: Let skulls manage their own destruction
            // Tower no longer destroys skulls - they handle their own lifecycle
        }
    }

    void TowerDies()
    {
        if (IsDead) return; // Prevent multiple death calls
        
        IsDead = true;
        
        // NOTE: OnTowerDeath notification already sent in StartCollapseSequence()
        // Removed redundant notification to prevent double notifications to ChestManager
        
        // Start the death sequence
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        // Visuals will be destroyed with the GameObject, no need to manually disable
        yield return new WaitForSeconds(finalDestroyDelay);
        Destroy(gameObject);
    }



    void SetTowerAndGemVisualsActive(bool isActive)
    {
        Renderer[] towerRenderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in towerRenderers)
        {
            if (r.GetComponentInParent<Gem>(true) == null)
            {
                r.enabled = isActive;
            }
        }
        foreach (Gem gem in _initializedGems)
        {
            if (gem != null)
            {
                gem.gameObject.SetActive(isActive);
            }
        }
        Collider[] towerColliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider c in towerColliders)
        {
            if (c.GetComponentInParent<Gem>(true) == null)
            {
                c.enabled = isActive;
            }
        }
    }

    void PlayOneShotSoundAtPoint(AudioClip clip, Vector3 position, float volume)
    {
        if (clip == null) return;
        GameObject soundGameObject = new GameObject("TempAudio_TC_" + clip.name);
        soundGameObject.transform.position = position;
        AudioSource audioSource = soundGameObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.spatialBlend = 0.8f;
        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.Play(); // Play the actual clip that was passed in
        Destroy(soundGameObject, clip.length + 0.2f);
    }
    

}