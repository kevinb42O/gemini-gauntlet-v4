// --- EnemyThreatTracker.cs (REVOLUTIONARY REAL-TIME ENEMY TRACKING SYSTEM) ---
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 🚨💀 REVOLUTIONARY ENEMY THREAT TRACKING SYSTEM 💀🚨
/// Real-time skull enemy tracking with UI integration and companion AI notifications
/// </summary>
public class EnemyThreatTracker : MonoBehaviour
{
    #region Singleton Pattern
    
    private static EnemyThreatTracker _instance;
    public static EnemyThreatTracker Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<EnemyThreatTracker>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("EnemyThreatTracker");
                    _instance = go.AddComponent<EnemyThreatTracker>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    #endregion
    
    #region Configuration
    
    [Header("🎯 THREAT TRACKING SETTINGS")]
    [Tooltip("Maximum number of skull enemies to track simultaneously")]
    [Range(10, 200)] public int maxTrackedEnemies = 50;
    
    [Tooltip("Detection radius for skull scanning - MASSIVE RANGE for large worlds")]
    [Range(200f, 10000f)] public float detectionRadius = 4000f;
    
    [Tooltip("How often to scan for new skull enemies (seconds) - Longer intervals for large radius")]
    [Range(0.1f, 5f)] public float scanInterval = 1.0f;
    
    [Tooltip("How often to validate tracked enemies (seconds)")]
    [Range(0.05f, 2f)] public float validationInterval = 0.5f;
    
    [Header("🎮 UI INTEGRATION")]
    [Tooltip("UI Slider to show threat level (0-50)")]
    public Slider threatLevelSlider;
    
    [Tooltip("UI TextMeshPro to show threat count and status")]
    public TextMeshProUGUI threatLevelText;
    
    [Tooltip("Colors for different threat levels")]
    public Color safeColor = Color.green;
    public Color lowThreatColor = Color.yellow;
    public Color mediumThreatColor = Color.orange;
    public Color highThreatColor = Color.red;
    public Color extremeThreatColor = Color.magenta;
    
    [Header("🎯 ENEMY PREFAB ASSIGNMENTS")]
    [Tooltip("Primary skull enemy prefab to track")]
    public GameObject skullEnemyPrefab1;

    [Tooltip("Secondary skull enemy prefab to track")]
    public GameObject skullEnemyPrefab2;

    [Tooltip("Tertiary skull enemy prefab to track")]
    public GameObject skullEnemyPrefab3;
    
    [Header("🚀 PERFORMANCE SETTINGS")]
    [Tooltip("Layer mask for skull detection")]
    public LayerMask skullLayerMask = -1;
    
    #endregion
    
    #region Private Variables
    
    // Core tracking data
    private HashSet<int> _trackedSkullIDs = new HashSet<int>();
    private Dictionary<int, SkullEnemy> _trackedSkulls = new Dictionary<int, SkullEnemy>();
    private List<SkullEnemy> _skullList = new List<SkullEnemy>(); // For fast iteration
    
    // Performance optimization arrays - LARGER BUFFERS for massive detection radius
    private Collider[] _scanBuffer;
    private SkullEnemy[] _validationBuffer;
    
    // PERFORMANCE: Chunked scanning for large radius
    private int _scanChunkSize = 50; // Process 50 colliders per frame
    private int _currentScanIndex = 0;
    
    // Coroutines
    private Coroutine _scanCoroutine;
    private Coroutine _validationCoroutine;
    
    // State tracking
    private int _currentThreatLevel = 0;
    private bool _isInCombatMode = false;
    private Vector3 _lastScanPosition;
    
    // Events for companion AI integration
    public System.Action OnCombatModeActivated;
    public System.Action OnCombatModeDeactivated;
    public System.Action<int> OnThreatLevelChanged;
    
    #endregion
    
    #region Unity Lifecycle
    
    void Awake()
    {
        // Enforce singleton pattern
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Initialize performance arrays - MUCH LARGER for massive detection radius
        _scanBuffer = new Collider[maxTrackedEnemies * 4]; // 4x buffer for large radius scanning
        _validationBuffer = new SkullEnemy[maxTrackedEnemies];
        
        Debug.Log($"[EnemyThreatTracker] 🚀 MASSIVE SCALE BUFFERS: ScanBuffer={_scanBuffer.Length}, ValidationBuffer={_validationBuffer.Length}");
        
        Debug.Log("[EnemyThreatTracker] 🚨💀 THREAT TRACKING SYSTEM INITIALIZED! 💀🚨");
    }
    
    void Start()
    {
        // Initialize UI
        InitializeUI();
        
        // ⚡ CRITICAL PERFORMANCE FIX: Don't start scanning until companions actually exist!
        // This massive 2582 unit radius scan is ONLY needed for friendly companions
        // Enemy companions don't use this system at all
        // We'll start scanning when a companion is spawned (call StartTrackingCoroutines() from spawner)
        Debug.LogWarning("[EnemyThreatTracker] ⚡ PERFORMANCE MODE: Scanning disabled until companions spawn. Call StartTrackingCoroutines() to enable.");
        
        // Don't auto-start - let companion spawner start it when needed
        // StartTrackingCoroutines();
    }
    
    void OnDestroy()
    {
        // Clean up coroutines
        StopTrackingCoroutines();
        
        // Clear singleton reference
        if (_instance == this)
        {
            _instance = null;
        }
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Get the closest skull enemy to a position
    /// </summary>
    public SkullEnemy GetClosestEnemy(Vector3 position)
    {
        if (_skullList.Count == 0) return null;
        
        SkullEnemy closest = null;
        float closestDistance = float.MaxValue;
        
        foreach (SkullEnemy skull in _skullList)
        {
            if (skull == null || !skull.gameObject.activeInHierarchy) continue;
            
            float distance = Vector3.Distance(position, skull.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = skull;
            }
        }
        
        return closest;
    }
    
    /// <summary>
    /// Get all skull enemies within a radius of a position
    /// </summary>
    public List<SkullEnemy> GetEnemiesInRadius(Vector3 position, float radius)
    {
        List<SkullEnemy> enemiesInRadius = new List<SkullEnemy>();
        float radiusSquared = radius * radius;
        
        foreach (SkullEnemy skull in _skullList)
        {
            if (skull == null || !skull.gameObject.activeInHierarchy) continue;
            
            float distanceSquared = (skull.transform.position - position).sqrMagnitude;
            if (distanceSquared <= radiusSquared)
            {
                enemiesInRadius.Add(skull);
            }
        }
        
        return enemiesInRadius;
    }
    
    /// <summary>
    /// Get current threat level (0-50)
    /// </summary>
    public int GetThreatLevel()
    {
        return _currentThreatLevel;
    }
    
    /// <summary>
    /// Check if system is in combat mode
    /// </summary>
    public bool IsInCombatMode()
    {
        return _isInCombatMode;
    }
    
    /// <summary>
    /// Force immediate rescan of the area
    /// </summary>
    [ContextMenu("🔍 Force Massive Rescan")]
    public void ForceRescan()
    {
        if (Camera.main != null)
        {
            Debug.Log($"[EnemyThreatTracker] 🚀 FORCING MASSIVE RESCAN with radius {detectionRadius}!");
            StartCoroutine(ScanForSkullEnemies(Camera.main.transform.position));
        }
    }
    
    /// <summary>
    /// Show current detection settings
    /// </summary>
    [ContextMenu("📊 Show Detection Stats")]
    public void ShowDetectionStats()
    {
        Debug.Log("=== 🎯 ENEMY THREAT TRACKER STATS ===");
        Debug.Log($"Detection Radius: {detectionRadius} units (20x larger than before!)");
        Debug.Log($"Max Tracked Enemies: {maxTrackedEnemies}");
        Debug.Log($"Current Tracked: {_currentThreatLevel}");
        Debug.Log($"Scan Interval: {scanInterval}s");
        Debug.Log($"Validation Interval: {validationInterval}s");
        Debug.Log($"Scan Buffer Size: {_scanBuffer?.Length ?? 0}");
        Debug.Log($"Chunk Size: {_scanChunkSize}");
        Debug.Log($"Combat Mode: {_isInCombatMode}");
        
        if (Camera.main != null)
        {
            Vector3 scanPos = Camera.main.transform.position;
            Debug.Log($"Current Scan Position: {scanPos}");
            Debug.Log($"Scan Area Coverage: {Mathf.PI * detectionRadius * detectionRadius:F0} square units!");
        }
    }
    
    /// <summary>
    /// Manually register a skull enemy
    /// </summary>
    public void RegisterSkullEnemy(SkullEnemy skull)
    {
        if (skull == null) return;
        
        int instanceID = skull.GetInstanceID();
        if (!_trackedSkullIDs.Contains(instanceID) && _trackedSkulls.Count < maxTrackedEnemies)
        {
            _trackedSkullIDs.Add(instanceID);
            _trackedSkulls[instanceID] = skull;
            _skullList.Add(skull);
            
            UpdateThreatLevel();
            Debug.Log($"[EnemyThreatTracker] 💀 SKULL REGISTERED: {skull.name} (Total: {_currentThreatLevel})");
        }
    }
    
    /// <summary>
    /// Manually unregister a skull enemy
    /// </summary>
    public void UnregisterSkullEnemy(SkullEnemy skull)
    {
        if (skull == null) return;
        
        int instanceID = skull.GetInstanceID();
        if (_trackedSkullIDs.Contains(instanceID))
        {
            _trackedSkullIDs.Remove(instanceID);
            _trackedSkulls.Remove(instanceID);
            _skullList.Remove(skull);
            
            UpdateThreatLevel();
            Debug.Log($"[EnemyThreatTracker] ✅ SKULL UNREGISTERED: {skull.name} (Total: {_currentThreatLevel})");
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Sync enemy prefabs from CompanionAI to ensure consistent tracking
    /// Call this after assigning prefabs in CompanionAI.cs
    /// </summary>
    public void SyncPrefabsFromCompanionAI()
    {
        CompanionAILegacy companion = FindFirstObjectByType<CompanionAILegacy>();
        if (companion != null)
        {
            skullEnemyPrefab1 = companion.skullEnemyPrefab1;
            skullEnemyPrefab2 = companion.skullEnemyPrefab2;
            skullEnemyPrefab3 = companion.skullEnemyPrefab3;
            Debug.Log("[EnemyThreatTracker] ✅ Synced enemy prefabs from CompanionAI!");
        }
        else
        {
            Debug.LogWarning("[EnemyThreatTracker] ⚠️ CompanionAI not found - cannot sync prefabs!");
        }
    }
    
    /// <summary>
    /// Check if a SkullEnemy is an instance of any of the assigned prefabs
    /// </summary>
    private bool IsInstanceOfAssignedPrefabs(SkullEnemy skull)
    {
        if (skull == null) return false;

        // Check each assigned prefab
        if (skullEnemyPrefab1 != null && IsInstanceOfPrefab(skull.gameObject, skullEnemyPrefab1))
            return true;

        if (skullEnemyPrefab2 != null && IsInstanceOfPrefab(skull.gameObject, skullEnemyPrefab2))
            return true;

        if (skullEnemyPrefab3 != null && IsInstanceOfPrefab(skull.gameObject, skullEnemyPrefab3))
            return true;

        return false;
    }

    /// <summary>
    /// Check if a GameObject is an instance of a specific prefab
    /// </summary>
    private bool IsInstanceOfPrefab(GameObject obj, GameObject prefab)
    {
        if (prefab == null || obj == null) return false;

        // Check if this object is a prefab instance by comparing components and structure
        return obj.GetComponent<SkullEnemy>() != null &&
               obj.name.StartsWith(prefab.name.Replace("(Clone)", "").Trim());
    }

    #endregion
    
    #region Core Tracking Logic
    
    /// <summary>
    /// ⚡ PUBLIC API: Start the massive scanning coroutines (call this when companions spawn)
    /// </summary>
    public void StartTrackingCoroutines()
    {
        StopTrackingCoroutines(); // Ensure no duplicates
        
        _scanCoroutine = StartCoroutine(ContinuousScanning());
        _validationCoroutine = StartCoroutine(ContinuousValidation());
        
        Debug.Log("[EnemyThreatTracker] 🚀 TRACKING COROUTINES STARTED!");
    }
    
    private void StopTrackingCoroutines()
    {
        if (_scanCoroutine != null)
        {
            StopCoroutine(_scanCoroutine);
            _scanCoroutine = null;
        }
        
        if (_validationCoroutine != null)
        {
            StopCoroutine(_validationCoroutine);
            _validationCoroutine = null;
        }
    }
    
    /// <summary>
    /// Continuous scanning coroutine for new skull enemies
    /// </summary>
    private IEnumerator ContinuousScanning()
    {
        while (true)
        {
            // Use player/camera position as scan center
            Vector3 scanPosition = GetScanPosition();
            
            if (scanPosition != Vector3.zero)
            {
                yield return StartCoroutine(ScanForSkullEnemies(scanPosition));
            }
            
            yield return new WaitForSeconds(scanInterval);
        }
    }
    
    /// <summary>
    /// Continuous validation coroutine for tracked enemies
    /// </summary>
    private IEnumerator ContinuousValidation()
    {
        while (true)
        {
            ValidateTrackedEnemies();
            yield return new WaitForSeconds(validationInterval);
        }
    }
    
    /// <summary>
    /// Scan for skull enemies in the area - OPTIMIZED FOR MASSIVE RADIUS
    /// </summary>
    private IEnumerator ScanForSkullEnemies(Vector3 scanPosition)
    {
        _lastScanPosition = scanPosition;
        
        // Use OverlapSphereNonAlloc for performance
        int foundCount = Physics.OverlapSphereNonAlloc(
            scanPosition,
            detectionRadius,
            _scanBuffer,
            skullLayerMask,
            QueryTriggerInteraction.Collide
        );
        
        // Debug.Log($"[EnemyThreatTracker] 🔍 MASSIVE SCAN: Found {foundCount} colliders in {detectionRadius} unit radius");
        
        int newSkulls = 0;
        int processedThisFrame = 0;
        
        // PERFORMANCE: Process in chunks to prevent frame drops with large radius
        for (int i = 0; i < foundCount && _trackedSkulls.Count < maxTrackedEnemies; i++)
        {
            Collider collider = _scanBuffer[i];
            if (collider == null) continue;
            
            SkullEnemy skull = collider.GetComponent<SkullEnemy>();
            if (skull == null) continue;
            
            // Only track enemies that match our assigned prefabs
            if (!IsInstanceOfAssignedPrefabs(skull)) continue;
            
            int instanceID = skull.GetInstanceID();
            if (!_trackedSkullIDs.Contains(instanceID))
            {
                _trackedSkullIDs.Add(instanceID);
                _trackedSkulls[instanceID] = skull;
                _skullList.Add(skull);
                newSkulls++;
                
                Debug.Log($"[EnemyThreatTracker] 💀 NEW SKULL TRACKED: {skull.name} at distance {Vector3.Distance(scanPosition, skull.transform.position):F0}");
            }
            
            processedThisFrame++;
            
            // PERFORMANCE: Yield every chunk to maintain smooth framerate
            if (processedThisFrame >= _scanChunkSize)
            {
                processedThisFrame = 0;
                yield return null; // Wait one frame
            }
        }
        
        if (newSkulls > 0)
        {
            UpdateThreatLevel();
            // Debug.Log($"[EnemyThreatTracker] 🔍 MASSIVE SCAN COMPLETE: Found {newSkulls} new skulls (Total: {_currentThreatLevel})");
        }
        else
        {
            // Debug.Log($"[EnemyThreatTracker] 🔍 MASSIVE SCAN COMPLETE: No new skulls found (Total: {_currentThreatLevel})");
        }
    }
    
    /// <summary>
    /// Validate all tracked enemies and remove invalid ones
    /// </summary>
    private void ValidateTrackedEnemies()
    {
        if (_skullList.Count == 0) return;
        
        // Copy to validation buffer for safe iteration
        int validationCount = Mathf.Min(_skullList.Count, _validationBuffer.Length);
        for (int i = 0; i < validationCount; i++)
        {
            _validationBuffer[i] = _skullList[i];
        }
        
        int removedCount = 0;
        
        for (int i = 0; i < validationCount; i++)
        {
            SkullEnemy skull = _validationBuffer[i];
            
            // Check if skull is still valid
            if (skull == null || !skull.gameObject.activeInHierarchy || skull.IsDead())
            {
                int instanceID = skull?.GetInstanceID() ?? 0;
                
                _trackedSkullIDs.Remove(instanceID);
                _trackedSkulls.Remove(instanceID);
                _skullList.Remove(skull);
                removedCount++;
            }
        }
        
        if (removedCount > 0)
        {
            UpdateThreatLevel();
            Debug.Log($"[EnemyThreatTracker] 🧹 VALIDATION: Removed {removedCount} invalid skulls (Total: {_currentThreatLevel})");
        }
    }
    
    #endregion
    
    #region Threat Level Management
    
    private void UpdateThreatLevel()
    {
        int newThreatLevel = _skullList.Count;
        bool threatLevelChanged = newThreatLevel != _currentThreatLevel;
        
        _currentThreatLevel = newThreatLevel;
        
        // Update combat mode
        bool shouldBeInCombatMode = _currentThreatLevel > 0;
        if (shouldBeInCombatMode != _isInCombatMode)
        {
            _isInCombatMode = shouldBeInCombatMode;
            
            if (_isInCombatMode)
            {
                OnCombatModeActivated?.Invoke();
                Debug.Log("[EnemyThreatTracker] 🚨 COMBAT MODE ACTIVATED!");
            }
            else
            {
                OnCombatModeDeactivated?.Invoke();
                Debug.Log("[EnemyThreatTracker] ✅ COMBAT MODE DEACTIVATED!");
            }
        }
        
        // Update UI
        UpdateUI();
        
        // Notify listeners of threat level change
        if (threatLevelChanged)
        {
            OnThreatLevelChanged?.Invoke(_currentThreatLevel);
        }
    }
    
    #endregion
    
    #region UI Management
    
    private void InitializeUI()
    {
        if (threatLevelSlider != null)
        {
            threatLevelSlider.minValue = 0;
            threatLevelSlider.maxValue = maxTrackedEnemies;
            threatLevelSlider.value = 0;
        }
        
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        // Update slider
        if (threatLevelSlider != null)
        {
            threatLevelSlider.value = _currentThreatLevel;
        }
        
        // Update text and color
        if (threatLevelText != null)
        {
            string statusText = GetThreatStatusText();
            Color statusColor = GetThreatStatusColor();
            
            threatLevelText.text = $"SKULLS: {_currentThreatLevel}/{maxTrackedEnemies}\n{statusText}";
            threatLevelText.color = statusColor;
        }
    }
    
    private string GetThreatStatusText()
    {
        if (_currentThreatLevel == 0) return "SAFE";
        if (_currentThreatLevel <= 5) return "LOW THREAT";
        if (_currentThreatLevel <= 15) return "MEDIUM THREAT";
        if (_currentThreatLevel <= 30) return "HIGH THREAT";
        return "EXTREME THREAT";
    }
    
    private Color GetThreatStatusColor()
    {
        if (_currentThreatLevel == 0) return safeColor;
        if (_currentThreatLevel <= 5) return lowThreatColor;
        if (_currentThreatLevel <= 15) return mediumThreatColor;
        if (_currentThreatLevel <= 30) return highThreatColor;
        return extremeThreatColor;
    }
    
    #endregion
    
    #region Utility Methods
    
    private Vector3 GetScanPosition()
    {
        // Priority 1: Main camera (player head)
        if (Camera.main != null)
        {
            return Camera.main.transform.position;
        }
        
        // Priority 2: Player transform
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player.transform.position;
        }
        
        // Priority 3: Companion AI
        CompanionAILegacy companion = FindFirstObjectByType<CompanionAILegacy>();
        if (companion != null)
        {
            return companion.transform.position;
        }
        
        return Vector3.zero;
    }
    
    #endregion
}
