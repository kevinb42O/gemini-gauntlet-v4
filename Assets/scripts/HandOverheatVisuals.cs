// --- HandOverheatVisuals.cs (FOR SPREADING PREFAB EFFECT - NO SHADER) ---
using UnityEngine;
using System.Collections.Generic; // Needed for List

public class HandOverheatVisuals : MonoBehaviour
{
    [Header("Hand Identification")]
    [Tooltip("CRITICAL MAPPING:\n• isPrimary = TRUE = LEFT hand (LMB/Primary Fire)\n• isPrimary = FALSE = RIGHT hand (RMB/Secondary Fire)\nThis must match your hand's position!")]
    public bool isPrimary = true;

    [Header("Main Wildfire (Leading Edge)")]
    [Tooltip("The 'Wildfire' prefab for the leading edge of the fire. This one will move.")]
    public GameObject wildfireLeadingEdgePrefab;

    [Header("Trail Wildfire (Spreading Segments)")]
    [Tooltip("The 'Wildfire' prefab to use for trail segments. Can be same as leading edge or a simpler one.")]
    public GameObject wildfireTrailSegmentPrefab;
    
    [Header("Instant Cooldown Effect")]
    [Tooltip("Particle effect prefab to show when Instant Cooldown powerup is active.")]
    public GameObject instantCooldownEffectPrefab;
    [Tooltip("Transform where the instant cooldown effect should be positioned. Assign this in the inspector!")]
    public Transform instantCooldownEffectPosition;
    
    [Header("Double Damage Effect")]
    [Tooltip("Particle effect prefab to show when Double Damage powerup is active.")]
    public GameObject doubleDamageEffectPrefab;
    [Tooltip("Transform where the double damage effect should be positioned. Assign this in the inspector!")]
    public Transform doubleDamageEffectPosition;
    
    [Header("Max Hand Upgrade Effect")]
    [Tooltip("Particle effect prefab to show when Max Hand Upgrade powerup is active.")]
    public GameObject maxHandUpgradeEffectPrefab;
    [Tooltip("Transform where the max hand upgrade effect should be positioned. Assign this in the inspector!")]
    public Transform maxHandUpgradeEffectPosition;

    [Tooltip("The points defining the path. 0 is start (hand), last is end (shoulder). Min 2 points.")]
    public Transform[] pathPoints = new Transform[4];

    [Header("Appearance Control")]
    [Tooltip("Should trail/leading edge be visible even at 0 heat?")]
    public bool effectActiveAtZeroHeat = true;
    [Range(0f, 1f)]
    [Tooltip("Minimum heat (0-1) to show any part of the effect.")]
    public float visibilityThreshold = 0.001f;


    private GameObject _leadingEdgeInstance;
    private List<GameObject> _activeTrailSegmentInstances = new List<GameObject>();
    private List<ParticleSystem> _activeTrailSegmentPS = new List<ParticleSystem>();
    
    // Instant Cooldown Effect
    private GameObject _instantCooldownEffectInstance;
    private ParticleSystem _instantCooldownParticleSystem;
    private bool _isInstantCooldownActive = false;
    
    // Double Damage Effect
    private GameObject _doubleDamageEffectInstance;
    private ParticleSystem _doubleDamageParticleSystem;
    private bool _isDoubleDamageActive = false;
    
    // Max Hand Upgrade Effect
    private GameObject _maxHandUpgradeEffectInstance;
    private ParticleSystem _maxHandUpgradeParticleSystem;
    private bool _isMaxHandUpgradeActive = false;
    
    // Current state for LateUpdate positioning
    private float _currentNormalizedHeat = 0f;
    private bool _currentIsOverheated = false;
    private bool _shouldShowEffect = false;

    void Awake()
    {
        Debug.Log($"🔥 HandOverheatVisuals ({gameObject.name}): Awake called, initializing... [isPrimary={isPrimary}]", this);
        
        if (wildfireLeadingEdgePrefab == null)
        {
            Debug.LogError($"❌ HandOverheatVisuals ({gameObject.name}): Wildfire Leading Edge Prefab not assigned! COMPONENT WILL BE DISABLED AND WON'T REGISTER!", this);
            enabled = false; return;
        }
        if (wildfireTrailSegmentPrefab == null)
        {
            Debug.LogWarning($"⚠️ HandOverheatVisuals ({gameObject.name}): Wildfire Trail Segment Prefab not assigned. Trail will not spread, only leading edge will show.", this);
        }

        bool pathValid = true;
        if (pathPoints == null || pathPoints.Length < 2)
        { 
            pathValid = false; 
            Debug.LogError($"❌ HandOverheatVisuals ({gameObject.name}): Path Points array requires at least 2 points. Found {pathPoints?.Length ?? 0}. COMPONENT WILL BE DISABLED AND WON'T REGISTER!", this); 
        }
        else
        {
            for (int i = 0; i < pathPoints.Length; i++)
            { 
                if (pathPoints[i] == null) 
                { 
                    pathValid = false; 
                    Debug.LogError($"❌ HandOverheatVisuals ({gameObject.name}): Path Point at index {i} is NULL! Assign a transform here. COMPONENT WILL BE DISABLED AND WON'T REGISTER!", this); 
                    break; 
                } 
            }
        }
        if (!pathValid) { enabled = false; return; }

        _leadingEdgeInstance = Instantiate(wildfireLeadingEdgePrefab, transform); // Parent to this
        _leadingEdgeInstance.SetActive(false); // Start inactive
        
        // Initialize instant cooldown effect if prefab is assigned
        if (instantCooldownEffectPrefab != null)
        {
            // Use specified position if assigned, otherwise use this transform as fallback
            Transform parentTransform = instantCooldownEffectPosition != null ? instantCooldownEffectPosition : transform;
            Vector3 spawnPosition = parentTransform.position;
            Quaternion spawnRotation = parentTransform.rotation;
            
            _instantCooldownEffectInstance = Instantiate(instantCooldownEffectPrefab, spawnPosition, spawnRotation, parentTransform);
            _instantCooldownEffectInstance.SetActive(false); // Start inactive
            _instantCooldownParticleSystem = _instantCooldownEffectInstance.GetComponentInChildren<ParticleSystem>();
            
            if (_instantCooldownParticleSystem == null)
            {
                Debug.LogWarning($"HandOverheatVisuals ({gameObject.name}): Instant Cooldown Effect prefab has no ParticleSystem component!", this);
            }
            
            if (instantCooldownEffectPosition != null)
            {
                Debug.Log($"HandOverheatVisuals ({gameObject.name}): Instant Cooldown effect positioned at {instantCooldownEffectPosition.name}", this);
            }
            else
            {
                Debug.LogWarning($"HandOverheatVisuals ({gameObject.name}): No instant cooldown effect position assigned! Using default position.", this);
            }
        }
        
        // Initialize double damage effect if prefab is assigned
        if (doubleDamageEffectPrefab != null)
        {
            // Use specified position if assigned, otherwise use this transform as fallback
            Transform parentTransform = doubleDamageEffectPosition != null ? doubleDamageEffectPosition : transform;
            Vector3 spawnPosition = parentTransform.position;
            Quaternion spawnRotation = parentTransform.rotation;
            
            _doubleDamageEffectInstance = Instantiate(doubleDamageEffectPrefab, spawnPosition, spawnRotation, parentTransform);
            _doubleDamageEffectInstance.SetActive(false); // Start inactive
            _doubleDamageParticleSystem = _doubleDamageEffectInstance.GetComponentInChildren<ParticleSystem>();
            
            if (_doubleDamageParticleSystem == null)
            {
                Debug.LogWarning($"HandOverheatVisuals ({gameObject.name}): Double Damage Effect prefab has no ParticleSystem component!", this);
            }
            
            if (doubleDamageEffectPosition != null)
            {
                Debug.Log($"HandOverheatVisuals ({gameObject.name}): Double Damage effect positioned at {doubleDamageEffectPosition.name}", this);
            }
            else
            {
                Debug.LogWarning($"HandOverheatVisuals ({gameObject.name}): No double damage effect position assigned! Using default position.", this);
            }
        }
        
        // Initialize max hand upgrade effect if prefab is assigned
        if (maxHandUpgradeEffectPrefab != null)
        {
            // Use specified position if assigned, otherwise use this transform as fallback
            Transform parentTransform = maxHandUpgradeEffectPosition != null ? maxHandUpgradeEffectPosition : transform;
            Vector3 spawnPosition = parentTransform.position;
            Quaternion spawnRotation = parentTransform.rotation;
            
            _maxHandUpgradeEffectInstance = Instantiate(maxHandUpgradeEffectPrefab, spawnPosition, spawnRotation, parentTransform);
            _maxHandUpgradeEffectInstance.SetActive(false); // Start inactive
            _maxHandUpgradeParticleSystem = _maxHandUpgradeEffectInstance.GetComponentInChildren<ParticleSystem>();
            
            if (_maxHandUpgradeParticleSystem == null)
            {
                Debug.LogWarning($"HandOverheatVisuals ({gameObject.name}): Max Hand Upgrade Effect prefab has no ParticleSystem component!", this);
            }
            
            if (maxHandUpgradeEffectPosition != null)
            {
                Debug.Log($"HandOverheatVisuals ({gameObject.name}): Max Hand Upgrade effect positioned at {maxHandUpgradeEffectPosition.name}", this);
            }
            else
            {
                Debug.LogWarning($"HandOverheatVisuals ({gameObject.name}): No max hand upgrade effect position assigned! Using default position.", this);
            }
        }

        // Initialize lists to hold segment instances/PS
        if (wildfireTrailSegmentPrefab != null && pathPoints.Length > 1)
        {
            for (int i = 0; i < pathPoints.Length - 1; i++)
            {
                _activeTrailSegmentInstances.Add(null); // Add placeholders
                _activeTrailSegmentPS.Add(null);
            }
        }
        UpdateVisuals(0f, false); // Set initial state
        Debug.Log($"HandOverheatVisuals ({gameObject.name}): Initialization complete. PathPoints: {pathPoints?.Length}, LeadingEdge: {wildfireLeadingEdgePrefab?.name}, TrailSegment: {wildfireTrailSegmentPrefab?.name}", this);
    }
    
    void Start()
    {
        Debug.Log($"HandOverheatVisuals ({gameObject.name}): Start called. Component is active and ready.", this);
        
        // AUTO-FIX: Detect correct hand and set isPrimary automatically
        AutoDetectAndFixHandMapping();
        
        // Register with PlayerOverheatManager
        RegisterWithOverheatManager();
    }
    
    void LateUpdate()
    {
        // Update effect positions in LateUpdate to ensure they follow animations smoothly
        // This runs AFTER all Update() and animation updates, preventing lag
        UpdateEffectPositions();
    }
    
    /// <summary>
    /// AUTO-FIX: Automatically detect if this is left or right hand and set isPrimary correctly
    /// NEW MAPPING: Primary (isPrimary=TRUE) = LEFT hand, Secondary (isPrimary=FALSE) = RIGHT hand
    /// </summary>
    private void AutoDetectAndFixHandMapping()
    {
        bool detectedIsLeftHand = IsLeftHandByHierarchy();
        bool correctIsPrimaryValue = detectedIsLeftHand; // LEFT hand should be isPrimary=true
        
        if (isPrimary != correctIsPrimaryValue)
        {
            Debug.LogWarning($"[HandOverheatVisuals AUTO-FIX] {gameObject.name}: " +
                $"Detected as {(detectedIsLeftHand ? "LEFT" : "RIGHT")} hand, " +
                $"but isPrimary was set to {isPrimary}. Auto-fixing to isPrimary={correctIsPrimaryValue}!", this);
            isPrimary = correctIsPrimaryValue;
        }
        else
        {
            Debug.Log($"[HandOverheatVisuals] {gameObject.name}: Correctly configured as isPrimary={isPrimary} " +
                $"({(detectedIsLeftHand ? "LEFT" : "RIGHT")} hand)", this);
        }
    }
    
    /// <summary>
    /// Detect if this hand is LEFT or RIGHT by checking hierarchy names
    /// </summary>
    private bool IsLeftHandByHierarchy()
    {
        Transform current = transform;
        while (current != null)
        {
            string name = current.name.ToLower();
            
            // Check for left indicators
            if (name.Contains("left") || name.Contains("_l_") || name.Contains("_l1") || 
                name.Contains("lhand") || name.Contains("lefthand") || name.Contains("primary"))
            {
                return true;
            }
            
            // Check for right indicators
            if (name.Contains("right") || name.Contains("_r_") || name.Contains("_r1") || 
                name.Contains("rhand") || name.Contains("righthand") || name.Contains("secondary"))
            {
                return false;
            }
            
            current = current.parent;
        }
        
        // Fallback: check position relative to player center
        // In Unity's default setup, left hand should have negative local X position
        Vector3 localPos = transform.localPosition;
        Debug.LogWarning($"[HandOverheatVisuals] {gameObject.name}: Could not determine hand from hierarchy names, " +
            $"using position fallback (localX={localPos.x:F2})", this);
        return localPos.x < 0; // Negative X = left side
    }
    
    private void RegisterWithOverheatManager()
    {
        PlayerOverheatManager overheatManager = FindObjectOfType<PlayerOverheatManager>();
        if (overheatManager == null)
        {
            Debug.LogError($"HandOverheatVisuals ({gameObject.name}): Could not find PlayerOverheatManager in scene! Fire spread will not work.", this);
            return;
        }
        
        Debug.Log($"HandOverheatVisuals ({gameObject.name}): Registering with PlayerOverheatManager as {(isPrimary ? "PRIMARY" : "SECONDARY")} hand.", this);
        overheatManager.SetActiveHandOverheatVisuals(isPrimary, this);
        
        Debug.Log($"[HAND MAPPING] {gameObject.name}: isPrimary={isPrimary}, will respond to {(isPrimary ? "LEFT-CLICK" : "RIGHT-CLICK")} input", this);
    }

    public void UpdateVisuals(float normalizedHeat, bool isEffectivelyOverheated)
    {
        if (pathPoints == null || pathPoints.Length < 2) return;
        
        // Store current state for LateUpdate to use
        _currentNormalizedHeat = Mathf.Clamp01(normalizedHeat);
        _currentIsOverheated = isEffectivelyOverheated;
        _shouldShowEffect = effectActiveAtZeroHeat || _currentNormalizedHeat >= visibilityThreshold || _currentIsOverheated;
        
        // Debug logging to help diagnose issues
        // Debug.Log($"HandOverheatVisuals ({gameObject.name}): UpdateVisuals called - Heat={_currentNormalizedHeat:F3}, Overheated={_currentIsOverheated}, ShowEffect={_shouldShowEffect}, Threshold={visibilityThreshold}, ActiveAtZero={effectActiveAtZeroHeat}", this);

        // --- 1. Leading Edge Wildfire Prefab Control ---
        if (_leadingEdgeInstance != null)
        {
            _leadingEdgeInstance.SetActive(_shouldShowEffect);
        }

        // --- 2. Trail Segment Control ---
        if (wildfireTrailSegmentPrefab != null && _activeTrailSegmentInstances.Count == (pathPoints.Length - 1))
        {
            float segmentNormalizedLength = (pathPoints.Length - 1 > 0) ? 1.0f / (pathPoints.Length - 1) : 1.0f;

            for (int i = 0; i < pathPoints.Length - 1; i++) // For each segment
            {
                // A segment is "hot" if the normalizedHeat has passed the *start* of this segment.
                // The start of segment 'i' is at normalized progress i * segmentNormalizedLength.
                float segmentStartProgress = i * segmentNormalizedLength;
                bool segmentShouldBeActiveBasedOnHeat = _currentIsOverheated || (_currentNormalizedHeat >= segmentStartProgress);

                // Overall visibility check for this segment
                bool segmentShouldBeActive = _shouldShowEffect && segmentShouldBeActiveBasedOnHeat;

                GameObject currentInstance = _activeTrailSegmentInstances[i];
                ParticleSystem currentPS = _activeTrailSegmentPS[i];

                if (segmentShouldBeActive)
                {
                    if (currentInstance == null) // If no instance exists for this segment, create it
                    {
                        Vector3 segmentMidPoint = Vector3.Lerp(pathPoints[i].position, pathPoints[i + 1].position, 0.5f);
                        Quaternion segmentRotation = pathPoints[i].rotation; // Default to start point's rotation
                        Vector3 segmentDirection = pathPoints[i + 1].position - pathPoints[i].position;
                        if (segmentDirection.sqrMagnitude > 0.001f)
                        {
                            segmentRotation = Quaternion.LookRotation(segmentDirection.normalized, pathPoints[i].up);
                        }

                        currentInstance = Instantiate(wildfireTrailSegmentPrefab, segmentMidPoint, segmentRotation, transform);
                        _activeTrailSegmentInstances[i] = currentInstance;
                        currentPS = currentInstance.GetComponentInChildren<ParticleSystem>(true); // Get main PS
                        _activeTrailSegmentPS[i] = currentPS;

                        if (currentPS != null)
                        {
                            currentPS.Play(true);
                        }
                        else
                        {
                            Debug.LogWarning($"HandOverheatVisuals: Trail segment prefab for segment {i} is missing a ParticleSystem in its children or itself.", this);
                        }
                    }
                    else if (!currentInstance.activeSelf) // If instance exists but is inactive, reactivate
                    {
                        currentInstance.SetActive(true);
                        if (currentPS != null && !currentPS.isPlaying)
                        {
                            currentPS.Play(true);
                        }
                    }
                }
                else // Segment should NOT be active
                {
                    if (currentInstance != null && currentInstance.activeSelf)
                    {
                        currentInstance.SetActive(false); // Deactivate for now
                        if (currentPS != null && currentPS.isPlaying)
                        {
                            currentPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // Or just StopEmitting if you want fade out
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Update effect positions in LateUpdate to follow animations smoothly without lag
    /// </summary>
    private void UpdateEffectPositions()
    {
        if (pathPoints == null || pathPoints.Length < 2) return;
        
        // Update leading edge position
        if (_leadingEdgeInstance != null && _leadingEdgeInstance.activeSelf)
        {
            float leadingEdgeTargetProgress = _currentIsOverheated ? 1f : _currentNormalizedHeat;
            SetObjectAlongPath(_leadingEdgeInstance, leadingEdgeTargetProgress);
        }
        
        // Update trail segment positions
        if (wildfireTrailSegmentPrefab != null && _activeTrailSegmentInstances.Count == (pathPoints.Length - 1))
        {
            for (int i = 0; i < pathPoints.Length - 1; i++)
            {
                GameObject currentInstance = _activeTrailSegmentInstances[i];
                if (currentInstance != null && currentInstance.activeSelf)
                {
                    // Recalculate segment position based on current path point positions
                    Vector3 segmentMidPoint = Vector3.Lerp(pathPoints[i].position, pathPoints[i + 1].position, 0.5f);
                    currentInstance.transform.position = segmentMidPoint;
                    
                    // Update rotation to follow path direction
                    Vector3 segmentDirection = pathPoints[i + 1].position - pathPoints[i].position;
                    if (segmentDirection.sqrMagnitude > 0.001f)
                    {
                        currentInstance.transform.rotation = Quaternion.LookRotation(segmentDirection.normalized, pathPoints[i].up);
                    }
                }
            }
        }
    }

    private void SetObjectAlongPath(GameObject obj, float normalizedProgress)
    {
        if (obj == null || pathPoints.Length < 2) return;

        normalizedProgress = Mathf.Clamp01(normalizedProgress);

        if (pathPoints.Length == 2) // Simple case: single segment
        {
            obj.transform.position = Vector3.Lerp(pathPoints[0].position, pathPoints[1].position, normalizedProgress);
            obj.transform.rotation = Quaternion.Lerp(pathPoints[0].rotation, pathPoints[1].rotation, normalizedProgress);
            return;
        }

        // Calculate total path length (can be cached if pathPoints don't change)
        float totalPathLength = 0f;
        for (int i = 0; i < pathPoints.Length - 1; i++)
        {
            totalPathLength += Vector3.Distance(pathPoints[i].position, pathPoints[i + 1].position);
        }

        if (totalPathLength < 0.001f) // Path has no real length
        {
            obj.transform.position = pathPoints[0].position;
            obj.transform.rotation = pathPoints[0].rotation;
            return;
        }

        float targetDistanceAlongPath = normalizedProgress * totalPathLength;
        float accumulatedDistance = 0f;

        for (int i = 0; i < pathPoints.Length - 1; i++)
        {
            Transform p1 = pathPoints[i];
            Transform p2 = pathPoints[i + 1];
            float currentSegmentLength = Vector3.Distance(p1.position, p2.position);

            if (targetDistanceAlongPath <= accumulatedDistance + currentSegmentLength + 0.0001f) // Check if target is within or at the end of this segment
            {
                float progressWithinCurrentSegment = (currentSegmentLength > 0.001f) ?
                    Mathf.Clamp01((targetDistanceAlongPath - accumulatedDistance) / currentSegmentLength) : 0f;

                obj.transform.position = Vector3.Lerp(p1.position, p2.position, progressWithinCurrentSegment);
                obj.transform.rotation = Quaternion.Lerp(p1.rotation, p2.rotation, progressWithinCurrentSegment);
                return;
            }
            accumulatedDistance += currentSegmentLength;
        }

        // Fallback: if loop finishes (shouldn't happen if normalizedProgress is 0-1 and totalPathLength > 0), place at the end
        obj.transform.position = pathPoints[pathPoints.Length - 1].position;
        obj.transform.rotation = pathPoints[pathPoints.Length - 1].rotation;
    }

    public void ResetAllVisuals()
    {
        UpdateVisuals(0f, false);
    }
    
    /// <summary>
    /// Activate the instant cooldown particle effect
    /// </summary>
    public void ActivateInstantCooldownEffect()
    {
        if (_instantCooldownEffectInstance == null || _instantCooldownParticleSystem == null)
        {
            Debug.LogWarning($"HandOverheatVisuals ({gameObject.name}): Cannot activate instant cooldown effect - prefab or particle system not found!", this);
            return;
        }
        
        _isInstantCooldownActive = true;
        _instantCooldownEffectInstance.SetActive(true);
        _instantCooldownParticleSystem.Play();
        
        string positionInfo = instantCooldownEffectPosition != null ? 
            $" at position {instantCooldownEffectPosition.name}" : 
            " at default position";
        
        Debug.Log($"HandOverheatVisuals ({gameObject.name}): Instant Cooldown effect activated for {(isPrimary ? "PRIMARY" : "SECONDARY")} hand{positionInfo}", this);
    }
    
    /// <summary>
    /// Deactivate the instant cooldown particle effect
    /// </summary>
    public void DeactivateInstantCooldownEffect()
    {
        if (_instantCooldownEffectInstance == null || _instantCooldownParticleSystem == null)
        {
            return;
        }
        
        _isInstantCooldownActive = false;
        _instantCooldownParticleSystem.Stop();
        _instantCooldownEffectInstance.SetActive(false);
        
        Debug.Log($"HandOverheatVisuals ({gameObject.name}): Instant Cooldown effect deactivated for {(isPrimary ? "PRIMARY" : "SECONDARY")} hand", this);
    }
    
    /// <summary>
    /// Check if instant cooldown effect is currently active
    /// </summary>
    public bool IsInstantCooldownEffectActive()
    {
        return _isInstantCooldownActive;
    }
    
    /// <summary>
    /// Activate the double damage particle effect
    /// </summary>
    public void ActivateDoubleDamageEffect()
    {
        if (_doubleDamageEffectInstance == null || _doubleDamageParticleSystem == null)
        {
            Debug.LogWarning($"HandOverheatVisuals ({gameObject.name}): Cannot activate double damage effect - prefab or particle system not found!", this);
            return;
        }
        
        _isDoubleDamageActive = true;
        _doubleDamageEffectInstance.SetActive(true);
        _doubleDamageParticleSystem.Play();
        
        string positionInfo = doubleDamageEffectPosition != null ? 
            $" at position {doubleDamageEffectPosition.name}" : 
            " at default position";
        
        Debug.Log($"HandOverheatVisuals ({gameObject.name}): Double Damage effect activated for {(isPrimary ? "PRIMARY" : "SECONDARY")} hand{positionInfo}", this);
    }
    
    /// <summary>
    /// Deactivate the double damage particle effect
    /// </summary>
    public void DeactivateDoubleDamageEffect()
    {
        if (_doubleDamageEffectInstance == null || _doubleDamageParticleSystem == null)
        {
            return;
        }
        
        _isDoubleDamageActive = false;
        _doubleDamageParticleSystem.Stop();
        _doubleDamageEffectInstance.SetActive(false);
        
        Debug.Log($"HandOverheatVisuals ({gameObject.name}): Double Damage effect deactivated for {(isPrimary ? "PRIMARY" : "SECONDARY")} hand", this);
    }
    
    /// <summary>
    /// Check if double damage effect is currently active
    /// </summary>
    public bool IsDoubleDamageEffectActive()
    {
        return _isDoubleDamageActive;
    }
    
    /// <summary>
    /// Activate the max hand upgrade particle effect
    /// </summary>
    public void ActivateMaxHandUpgradeEffect()
    {
        if (_maxHandUpgradeEffectInstance == null || _maxHandUpgradeParticleSystem == null)
        {
            Debug.LogWarning($"HandOverheatVisuals ({gameObject.name}): Cannot activate max hand upgrade effect - prefab or particle system not found!", this);
            return;
        }
        
        _isMaxHandUpgradeActive = true;
        _maxHandUpgradeEffectInstance.SetActive(true);
        _maxHandUpgradeParticleSystem.Play();
        
        string positionInfo = maxHandUpgradeEffectPosition != null ? 
            $" at position {maxHandUpgradeEffectPosition.name}" : 
            " at default position";
        
        Debug.Log($"HandOverheatVisuals ({gameObject.name}): Max Hand Upgrade effect activated for {(isPrimary ? "PRIMARY" : "SECONDARY")} hand{positionInfo}", this);
    }
    
    /// <summary>
    /// Deactivate the max hand upgrade particle effect
    /// </summary>
    public void DeactivateMaxHandUpgradeEffect()
    {
        if (_maxHandUpgradeEffectInstance == null || _maxHandUpgradeParticleSystem == null)
        {
            return;
        }
        
        _isMaxHandUpgradeActive = false;
        _maxHandUpgradeParticleSystem.Stop();
        _maxHandUpgradeEffectInstance.SetActive(false);
        
        Debug.Log($"HandOverheatVisuals ({gameObject.name}): Max Hand Upgrade effect deactivated for {(isPrimary ? "PRIMARY" : "SECONDARY")} hand", this);
    }
    
    /// <summary>
    /// Check if max hand upgrade effect is currently active
    /// </summary>
    public bool IsMaxHandUpgradeEffectActive()
    {
        return _isMaxHandUpgradeActive;
    }

    void OnDestroy()
    {
        if (_leadingEdgeInstance != null)
        {
            Destroy(_leadingEdgeInstance);
        }
        foreach (var instance in _activeTrailSegmentInstances)
        {
            if (instance != null) Destroy(instance);
        }
        _activeTrailSegmentInstances.Clear();
        _activeTrailSegmentPS.Clear();
        
        // Cleanup instant cooldown effect
        if (_instantCooldownEffectInstance != null)
        {
            Destroy(_instantCooldownEffectInstance);
        }
        
        // Cleanup double damage effect
        if (_doubleDamageEffectInstance != null)
        {
            Destroy(_doubleDamageEffectInstance);
        }
        
        // Cleanup max hand upgrade effect
        if (_maxHandUpgradeEffectInstance != null)
        {
            Destroy(_maxHandUpgradeEffectInstance);
        }
    }
}