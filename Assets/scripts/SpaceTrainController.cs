using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Audio;

public class SpaceTrainController : MonoBehaviour
{
    [Header("🚂 Train Configuration")]
    [SerializeField] private float trainSpeed = 10f;
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private bool loopCheckpoints = true;
    [SerializeField] private float checkpointReachDistance = 2f;
    [SerializeField] private float movementSmoothness = 8f;
    
    [Header("🚉 Train Station System")]
    [SerializeField] private bool enableTrainStation = true;
    [SerializeField] private int stationCheckpointIndex = -1;
    [Tooltip("Select which checkpoint acts as the train station")]
    [SerializeField] private float slowdownDistance = 30f;
    [SerializeField] private float stationStopDuration = 20f;
    [SerializeField] private float accelerationRate = 2f;
    [SerializeField] private float decelerationRate = 3f;
    
    [Header("📍 Checkpoint System")]
    [SerializeField] private Transform[] checkpoints;
    [SerializeField] private bool useSplineInterpolation = true;
    [SerializeField] private float splineSmoothness = 0.5f;
    
    [Header("🚃 Auto-Carriage System (No Manual Setup!)")] 
    [SerializeField] private float carriageSpacing = 8f;
    [SerializeField] private bool autoFindChildCarriages = true;
    [SerializeField] private string carriageTag = "TrainCarriage";
    [SerializeField] private bool autoAddPlatformComponents = true;
    [Tooltip("Automatically adds colliders and platform components for player landing")]
    [SerializeField] private LayerMask platformLayer = 1 << 6; // Default platform layer
    [Tooltip("Carriages automatically found as children - no manual assignment needed!")]
    [SerializeField] private List<Transform> foundCarriages = new List<Transform>();
    
    [Header("🔊 Train Audio System")]
    [Tooltip("Continuous train running sound - will loop automatically")]
    [SerializeField] private AudioClip trainLoopSound;
    [Tooltip("Audio spatialization and attenuation settings")]
    [SerializeField] private AudioRolloffMode audioRolloffMode = AudioRolloffMode.Logarithmic;
    [SerializeField] private float audioMinDistance = 5f;
    [SerializeField] private float audioMaxDistance = 80f;
    [SerializeField, Range(0f, 1f)] private float audioVolume = 1f;
    [SerializeField, Range(-1f, 1f)] private float audioStereoPan = 0f;
    [SerializeField, Range(0f, 5f)] private float audioDopplerLevel = 0f;
    [SerializeField, Range(0, 256)] private int audioPriority = 128;
    [Tooltip("Optional routing to an AudioMixerGroup (e.g., SFX or Vehicles)")]
    [SerializeField] private AudioMixerGroup outputMixerGroup;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool showPath = true;
    
    // Private variables
    private int currentCheckpointIndex = 0;
    private float journeyProgress = 0f;
    private Vector3 currentVelocity;
    private List<Vector3> smoothPositionHistory = new List<Vector3>();
    private List<Quaternion> smoothRotationHistory = new List<Quaternion>();
    private const int HISTORY_SIZE = 300;
    
    // Spline calculation cache
    private List<Vector3> splinePoints = new List<Vector3>();
    private float totalSplineDistance = 0f;
    
    // Train Station System
    private enum StationState { Normal, Approaching, Stopped, Accelerating }
    private StationState currentStationState = StationState.Normal;
    private float currentSpeed;
    private float stationStopTimer = 0f;
    private bool isAtStation = false;
    
    // Audio system
    private AudioSource trainAudioSource;
    
    // Simple parent-child carriage system
    
    void Start()
    {
        InitializeTrain();
    }
    
    void FixedUpdate()
    {
        // Everything on same physics timing for smooth movement
        UpdateStationSystem();
        MoveTrain();
        UpdateCarriageHistory();
        UpdateCarriages();
    }
    
    /// <summary>
    /// Initialize the train system with automatic carriage detection
    /// </summary>
    private void InitializeTrain()
    {
        // Validate checkpoints
        if (checkpoints == null || checkpoints.Length < 2)
        {
            Debug.LogError("[SpaceTrainController] Need at least 2 checkpoints to operate!");
            enabled = false;
            return;
        }
        
        // Initialize smooth history for carriages
        smoothPositionHistory.Clear();
        smoothRotationHistory.Clear();
        
        // Start at first checkpoint
        transform.position = checkpoints[0].position;
        
        // Initialize station system
        currentSpeed = trainSpeed;
        if (enableTrainStation && (stationCheckpointIndex < 0 || stationCheckpointIndex >= checkpoints.Length))
        {
            Debug.LogWarning("[SpaceTrainController] Invalid station checkpoint index! Disabling station system.");
            enableTrainStation = false;
        }
        
        // Pre-calculate spline if using interpolation
        if (useSplineInterpolation)
        {
            CalculateSplinePoints();
        }
        
        // Auto-find and initialize carriages
        AutoFindAndInitializeCarriages();
        
        // Initialize train audio system
        InitializeTrainAudio();
        
        Debug.Log($"[SpaceTrainController] ✅ Initialized with {checkpoints.Length} checkpoints and {foundCarriages.Count} auto-detected carriages!");
        if (enableTrainStation)
        {
            Debug.Log($"[SpaceTrainController] 🚉 Station enabled at checkpoint {stationCheckpointIndex}: '{checkpoints[stationCheckpointIndex].name}'");
        }
        
        // Debug output for troubleshooting
        foreach (var carriage in foundCarriages)
        {
            Debug.Log($"[SpaceTrainController] 🔍 Found carriage: {carriage.name}");
        }
    }
    
    /// <summary>
    /// Update the train station system - handles slowdown, stop, and acceleration
    /// </summary>
    private void UpdateStationSystem()
    {
        if (!enableTrainStation || stationCheckpointIndex < 0) return;
        
        Transform stationCheckpoint = checkpoints[stationCheckpointIndex];
        float distanceToStation = Vector3.Distance(transform.position, stationCheckpoint.position);
        
        switch (currentStationState)
        {
            case StationState.Normal:
                // Check if approaching station
                if (currentCheckpointIndex == stationCheckpointIndex && distanceToStation <= slowdownDistance)
                {
                    currentStationState = StationState.Approaching;
                    Debug.Log($"[SpaceTrainController] 🚉 Approaching station - starting slowdown at distance {distanceToStation:F1}");
                }
                else
                {
                    // Maintain full speed when not near station
                    currentSpeed = trainSpeed;
                }
                break;
                
            case StationState.Approaching:
                // Gradually slow down as we approach station
                float slowdownProgress = 1f - (distanceToStation / slowdownDistance);
                slowdownProgress = Mathf.Clamp01(slowdownProgress);
                currentSpeed = Mathf.Lerp(trainSpeed, 0f, slowdownProgress * decelerationRate * Time.fixedDeltaTime);
                
                // Check if we've reached the station
                if (distanceToStation <= checkpointReachDistance)
                {
                    currentSpeed = 0f;
                    currentStationState = StationState.Stopped;
                    stationStopTimer = 0f;
                    isAtStation = true;
                    Debug.Log($"[SpaceTrainController] 🚉 Arrived at station - stopping for {stationStopDuration} seconds");
                }
                break;
                
            case StationState.Stopped:
                // Stay stopped and count timer
                currentSpeed = 0f;
                stationStopTimer += Time.fixedDeltaTime;
                
                if (stationStopTimer >= stationStopDuration)
                {
                    currentStationState = StationState.Accelerating;
                    Debug.Log($"[SpaceTrainController] 🚉 Departing station - accelerating to full speed");
                }
                break;
                
            case StationState.Accelerating:
                // Gradually speed up to full speed
                currentSpeed = Mathf.Lerp(currentSpeed, trainSpeed, accelerationRate * Time.fixedDeltaTime);
                
                // Check if we've reached full speed
                if (currentSpeed >= trainSpeed * 0.95f)
                {
                    currentSpeed = trainSpeed;
                    currentStationState = StationState.Normal;
                    isAtStation = false;
                    Debug.Log($"[SpaceTrainController] 🚉 Reached full speed - resuming normal operation");
                }
                break;
        }
    }
    
    /// <summary>
    /// Initialize the train audio system
    /// </summary>
    private void InitializeTrainAudio()
    {
        // Set up AudioSource component
        trainAudioSource = GetComponent<AudioSource>();
        if (trainAudioSource == null)
        {
            trainAudioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (trainLoopSound != null)
        {
            // 3D spatial audio that loops continuously - no volume changes
            trainAudioSource.clip = trainLoopSound;
            trainAudioSource.loop = true;
            trainAudioSource.playOnAwake = false;
            trainAudioSource.spatialBlend = 1f; // Full 3D sound
            
            // Tunable 3D audio settings
            trainAudioSource.rolloffMode = audioRolloffMode;
            trainAudioSource.minDistance = audioMinDistance;
            trainAudioSource.maxDistance = audioMaxDistance;
            trainAudioSource.volume = audioVolume;
            trainAudioSource.dopplerLevel = audioDopplerLevel;
            trainAudioSource.priority = audioPriority;
            trainAudioSource.panStereo = audioStereoPan;
            if (outputMixerGroup != null)
            {
                trainAudioSource.outputAudioMixerGroup = outputMixerGroup;
            }
            // If using a spatializer plugin (e.g., Oculus/Steam Audio), you may enable:
            // trainAudioSource.spatialize = true;
            
            // Start playing the loop
            trainAudioSource.Play();
            Debug.Log("[SpaceTrainController] 🔊 Train 3D audio initialized and started looping");
        }
        else
        {
            Debug.LogWarning("[SpaceTrainController] 🔊 No train loop sound assigned - audio disabled");
        }
    }
    
    /// <summary>
    /// Main train movement logic
    /// </summary>
    private void MoveTrain()
    {
        if (checkpoints.Length == 0) return;
        
        Vector3 targetPosition;
        Vector3 lookDirection;
        
        if (useSplineInterpolation)
        {
            // Smooth spline-based movement
            CalculateSplineMovement(out targetPosition, out lookDirection);
        }
        else
        {
            // Direct checkpoint-to-checkpoint movement
            CalculateDirectMovement(out targetPosition, out lookDirection);
        }
        
        // Smooth movement toward target position using current speed (affected by station system)
        float adjustedSmoothness = (currentSpeed / trainSpeed) * movementSmoothness;
        Vector3 smoothPosition = Vector3.Lerp(transform.position, targetPosition, adjustedSmoothness * Time.fixedDeltaTime);
        currentVelocity = (smoothPosition - transform.position) / Time.fixedDeltaTime;
        transform.position = smoothPosition;
        
        // Rotate to face movement direction
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
        
        // Position history updated in Update() method
        
        // Check if we've reached the target checkpoint
        CheckCheckpointReached(targetPosition);
    }
    
    /// <summary>
    /// Calculate smooth spline-based movement
    /// </summary>
    private void CalculateSplineMovement(out Vector3 targetPosition, out Vector3 lookDirection)
    {
        float totalProgress = (currentCheckpointIndex + journeyProgress) / checkpoints.Length;
        if (totalProgress >= 1f && loopCheckpoints)
        {
            totalProgress = totalProgress % 1f;
        }
        
        // Get position along the complete spline
        targetPosition = GetSplinePosition(totalProgress);
        
        // Calculate look direction (tangent to spline)
        float lookAheadDistance = 0.01f;
        float lookAheadProgress = totalProgress + lookAheadDistance;
        if (lookAheadProgress >= 1f && loopCheckpoints)
        {
            lookAheadProgress = lookAheadProgress % 1f;
        }
        
        Vector3 lookAheadPosition = GetSplinePosition(lookAheadProgress);
        lookDirection = (lookAheadPosition - targetPosition).normalized;
        
        // Update journey progress based on currentSpeed (pause when stopped)
        if (totalSplineDistance > 0f)
        {
            float progressSpeed = Mathf.Max(0f, currentSpeed) / totalSplineDistance;
            journeyProgress += progressSpeed * Time.fixedDeltaTime;
        }
        
        if (journeyProgress >= 1f)
        {
            journeyProgress = 0f;
            AdvanceToNextCheckpoint();
        }
    }
    
    /// <summary>
    /// Calculate direct checkpoint-to-checkpoint movement
    /// </summary>
    private void CalculateDirectMovement(out Vector3 targetPosition, out Vector3 lookDirection)
    {
        Vector3 currentTarget = checkpoints[currentCheckpointIndex].position;
        targetPosition = currentTarget;
        lookDirection = (currentTarget - transform.position).normalized;
    }
    
    /// <summary>
    /// Get position along the complete spline (0-1 progress)
    /// </summary>
    private Vector3 GetSplinePosition(float progress)
    {
        progress = Mathf.Clamp01(progress);
        
        // Find the appropriate segment
        int segmentCount = loopCheckpoints ? checkpoints.Length : checkpoints.Length - 1;
        float segmentProgress = progress * segmentCount;
        int segmentIndex = Mathf.FloorToInt(segmentProgress);
        float t = segmentProgress - segmentIndex;
        
        // Handle wraparound for looping
        if (loopCheckpoints && segmentIndex >= checkpoints.Length)
        {
            segmentIndex = 0;
        }
        
        // Get control points for Catmull-Rom spline
        Vector3 p0, p1, p2, p3;
        GetSplineControlPoints(segmentIndex, out p0, out p1, out p2, out p3);
        
        // Calculate Catmull-Rom spline position
        return CalculateCatmullRom(p0, p1, p2, p3, t);
    }
    
    /// <summary>
    /// Get control points for spline segment
    /// </summary>
    private void GetSplineControlPoints(int segmentIndex, out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3)
    {
        int checkpointCount = checkpoints.Length;
        
        if (loopCheckpoints)
        {
            // Looping: use modular arithmetic
            p0 = checkpoints[(segmentIndex - 1 + checkpointCount) % checkpointCount].position;
            p1 = checkpoints[segmentIndex].position;
            p2 = checkpoints[(segmentIndex + 1) % checkpointCount].position;
            p3 = checkpoints[(segmentIndex + 2) % checkpointCount].position;
        }
        else
        {
            // Non-looping: clamp endpoints
            int i0 = Mathf.Max(0, segmentIndex - 1);
            int i1 = segmentIndex;
            int i2 = Mathf.Min(checkpointCount - 1, segmentIndex + 1);
            int i3 = Mathf.Min(checkpointCount - 1, segmentIndex + 2);
            
            p0 = checkpoints[i0].position;
            p1 = checkpoints[i1].position;
            p2 = checkpoints[i2].position;
            p3 = checkpoints[i3].position;
        }
    }
    
    /// <summary>
    /// Calculate Catmull-Rom spline position
    /// </summary>
    private Vector3 CalculateCatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        
        Vector3 result = 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
        
        return result;
    }
    
    /// <summary>
    /// Pre-calculate spline points for optimization
    /// </summary>
    private void CalculateSplinePoints()
    {
        splinePoints.Clear();
        totalSplineDistance = 0f;
        
        int resolution = 100;
        Vector3 previousPoint = checkpoints[0].position;
        
        for (int i = 0; i <= resolution; i++)
        {
            float progress = (float)i / resolution;
            Vector3 point = GetSplinePosition(progress);
            splinePoints.Add(point);
            
            if (i > 0)
            {
                totalSplineDistance += Vector3.Distance(previousPoint, point);
            }
            previousPoint = point;
        }
    }
    
    /// <summary>
    /// Store smooth position and rotation history for carriages
    /// </summary>
    private void UpdateCarriageHistory()
    {
        smoothPositionHistory.Add(transform.position);
        smoothRotationHistory.Add(transform.rotation);
        
        // Limit history size
        while (smoothPositionHistory.Count > HISTORY_SIZE)
        {
            smoothPositionHistory.RemoveAt(0);
            smoothRotationHistory.RemoveAt(0);
        }
    }
    
    /// <summary>
    /// Check if train has reached current checkpoint
    /// </summary>
    private void CheckCheckpointReached(Vector3 targetPosition)
    {
        if (!useSplineInterpolation)
        {
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
            if (distanceToTarget <= checkpointReachDistance)
            {
                AdvanceToNextCheckpoint();
            }
        }
    }
    
    /// <summary>
    /// Advance to the next checkpoint
    /// </summary>
    private void AdvanceToNextCheckpoint()
    {
        currentCheckpointIndex++;
        
        if (currentCheckpointIndex >= checkpoints.Length)
        {
            if (loopCheckpoints)
            {
                currentCheckpointIndex = 0;
                Debug.Log("[SpaceTrainController] Completed loop, returning to start");
            }
            else
            {
                currentCheckpointIndex = checkpoints.Length - 1;
                Debug.Log("[SpaceTrainController] Reached final checkpoint");
            }
        }
    }
    
    /// <summary>
    /// Auto-find child carriages and initialize smooth followers
    /// </summary>
    private void AutoFindAndInitializeCarriages()
    {
        foundCarriages.Clear();
        
        if (autoFindChildCarriages)
        {
            // Method 1: Find by tag
            var taggedCarriages = GameObject.FindGameObjectsWithTag(carriageTag)
                .Where(go => go.transform.IsChildOf(this.transform))
                .Select(go => go.transform)
                .ToArray();
            
            // Method 2: Find by name pattern
            var namedCarriages = GetComponentsInChildren<Transform>()
                .Where(t => t != this.transform && 
                       (t.name.ToLower().Contains("carriage") || 
                        t.name.ToLower().Contains("car") ||
                        t.name.ToLower().Contains("platform")))
                .ToArray();
            
            // Combine and remove duplicates
            var allCarriages = taggedCarriages.Union(namedCarriages).ToList();
            foundCarriages = allCarriages.OrderBy(t => t.GetSiblingIndex()).ToList();
            
            Debug.Log($"[SpaceTrainController] 🔍 Auto-detected {foundCarriages.Count} carriages as children");
        }
        
        // Set initial positions and setup platform components for each carriage
        for (int i = 0; i < foundCarriages.Count; i++)
        {
            float offset = carriageSpacing * (i + 1);
            foundCarriages[i].localPosition = Vector3.back * offset;
            foundCarriages[i].localRotation = Quaternion.identity;
            
            // Auto-add platform components for player landing
            if (autoAddPlatformComponents)
            {
                SetupCarriageAsPlatform(foundCarriages[i]);
            }
            
            Debug.Log($"[SpaceTrainController] 🚃 Carriage {i + 1}: '{foundCarriages[i].name}' positioned at offset {offset}");
        }
    }
    
    /// <summary>
    /// Update all carriages to follow behind the train with smooth cornering
    /// </summary>
    private void UpdateCarriages()
    {
        if (foundCarriages.Count == 0) return;
        
        for (int i = 0; i < foundCarriages.Count; i++)
        {
            Transform carriage = foundCarriages[i];
            if (carriage != null)
            {
                float followDistance = carriageSpacing * (i + 1);
                
                if (smoothPositionHistory.Count >= 2)
                {
                    // Use historical path following for smooth cornering
                    Quaternion targetRotation;
                    Vector3 worldTargetPos = GetSmoothHistoricalPosition(followDistance, out targetRotation);
                    
                    // Temporarily unparent to set world position
                    carriage.SetParent(null);
                    carriage.position = Vector3.Lerp(carriage.position, worldTargetPos, 8f * Time.fixedDeltaTime);
                    carriage.rotation = Quaternion.Slerp(carriage.rotation, targetRotation, 6f * Time.fixedDeltaTime);
                    carriage.SetParent(this.transform);
                }
                else
                {
                    // Fallback: simple offset positioning for initial startup
                    Vector3 behindPosition = transform.position - transform.forward * followDistance;
                    carriage.position = Vector3.Lerp(carriage.position, behindPosition, 8f * Time.fixedDeltaTime);
                    carriage.rotation = Quaternion.Slerp(carriage.rotation, transform.rotation, 6f * Time.fixedDeltaTime);
                }
            }
        }
    }
    
    /// <summary>
    /// Get smoothly interpolated historical position
    /// </summary>
    public Vector3 GetSmoothHistoricalPosition(float distanceBehind, out Quaternion rotation)
    {
        rotation = transform.rotation;
        
        if (smoothPositionHistory.Count < 2)
            return transform.position;
        
        // Convert distance to approximate frame offset
        float frameOffset = distanceBehind / (trainSpeed * Time.fixedDeltaTime);
        int frameIndex = Mathf.FloorToInt(frameOffset);
        float frameFraction = frameOffset - frameIndex;
        
        frameIndex = Mathf.Clamp(frameIndex, 0, smoothPositionHistory.Count - 2);
        int nextFrameIndex = Mathf.Min(frameIndex + 1, smoothPositionHistory.Count - 1);
        
        // Get positions from end of history (most recent first)
        int historyIndex = smoothPositionHistory.Count - 1 - frameIndex;
        int nextHistoryIndex = smoothPositionHistory.Count - 1 - nextFrameIndex;
        
        historyIndex = Mathf.Clamp(historyIndex, 0, smoothPositionHistory.Count - 1);
        nextHistoryIndex = Mathf.Clamp(nextHistoryIndex, 0, smoothPositionHistory.Count - 1);
        
        Vector3 pos1 = smoothPositionHistory[historyIndex];
        Vector3 pos2 = smoothPositionHistory[nextHistoryIndex];
        Quaternion rot1 = smoothRotationHistory[historyIndex];
        Quaternion rot2 = smoothRotationHistory[nextHistoryIndex];
        
        // Smooth interpolation between positions
        Vector3 interpolatedPos = Vector3.Lerp(pos1, pos2, frameFraction);
        rotation = Quaternion.Slerp(rot1, rot2, frameFraction);
        
        return interpolatedPos;
    }
    
    /// <summary>
    /// Setup a carriage as a landable platform for the player
    /// </summary>
    private void SetupCarriageAsPlatform(Transform carriage)
    {
        // Add BoxCollider if not present
        BoxCollider platformCollider = carriage.GetComponent<BoxCollider>();
        if (platformCollider == null)
        {
            platformCollider = carriage.gameObject.AddComponent<BoxCollider>();
            platformCollider.size = new Vector3(10f, 1f, 10f); // Default platform size
            platformCollider.center = new Vector3(0f, 0.5f, 0f);
            Debug.Log($"[SpaceTrainController] ✅ Added BoxCollider to {carriage.name}");
        }
        
        // Set to platform layer
        carriage.gameObject.layer = Mathf.RoundToInt(Mathf.Log(platformLayer.value, 2));
        
        // Add Rigidbody for physics interaction (kinematic)
        Rigidbody platformRb = carriage.GetComponent<Rigidbody>();
        if (platformRb == null)
        {
            platformRb = carriage.gameObject.AddComponent<Rigidbody>();
            platformRb.isKinematic = true; // Kinematic so train controls movement
            platformRb.useGravity = false;
            Debug.Log($"[SpaceTrainController] ✅ Added kinematic Rigidbody to {carriage.name}");
        }
        
        // Try to add existing platform component (if you have one)
        // This will fail silently if the component doesn't exist
        try
        {
            var platformComponent = carriage.gameObject.GetComponent("CelestialPlatform");
            if (platformComponent == null)
            {
                // Try different platform component names
                string[] platformTypes = { "CelestialPlatform", "PlatformTrigger", "MovingPlatform", "Platform" };
                foreach (string typeName in platformTypes)
                {
                    System.Type componentType = System.Type.GetType(typeName);
                    if (componentType != null)
                    {
                        carriage.gameObject.AddComponent(componentType);
                        Debug.Log($"[SpaceTrainController] ✅ Added {typeName} component to {carriage.name}");
                        break;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.Log($"[SpaceTrainController] ℹ️ Platform component not found, using basic setup for {carriage.name}");
        }
        
        // Add tag for platform identification
        if (!carriage.gameObject.CompareTag("Platform"))
        {
            carriage.gameObject.tag = "Platform";
        }
    }
    
    /// <summary>
    /// Get current train velocity
    /// </summary>
    public Vector3 GetTrainVelocity()
    {
        return currentVelocity;
    }
    
    // Debug visualization
    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        // Draw checkpoints
        if (checkpoints != null)
        {
            for (int i = 0; i < checkpoints.Length; i++)
            {
                if (checkpoints[i] == null) continue;
                
                // Current checkpoint in red, others in yellow
                Gizmos.color = (i == currentCheckpointIndex) ? Color.red : Color.yellow;
                Gizmos.DrawSphere(checkpoints[i].position, 1f);
                Gizmos.DrawWireSphere(checkpoints[i].position, checkpointReachDistance);
                
                // Draw checkpoint numbers
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(checkpoints[i].position + Vector3.up * 2f, i.ToString());
                #endif
            }
        }
        
        // Draw path between checkpoints
        if (showPath && checkpoints != null && checkpoints.Length > 1)
        {
            Gizmos.color = Color.green;
            
            if (useSplineInterpolation && Application.isPlaying)
            {
                // Draw smooth spline path
                for (int i = 0; i < splinePoints.Count - 1; i++)
                {
                    Gizmos.DrawLine(splinePoints[i], splinePoints[i + 1]);
                }
            }
            else
            {
                // Draw direct paths
                for (int i = 0; i < checkpoints.Length; i++)
                {
                    if (checkpoints[i] == null) continue;
                    
                    int nextIndex = (i + 1) % checkpoints.Length;
                    if (!loopCheckpoints && nextIndex == 0) break;
                    
                    if (checkpoints[nextIndex] != null)
                    {
                        Gizmos.DrawLine(checkpoints[i].position, checkpoints[nextIndex].position);
                    }
                }
            }
        }
        
        // Draw train direction
        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 3f);
        }
    }
}
