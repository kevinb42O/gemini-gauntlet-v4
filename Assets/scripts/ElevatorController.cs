using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// High-speed elevator system with smooth acceleration curves.
/// Travels between top and bottom floors with perfect start/stop timing.
/// Handles player containment and physics-safe movement.
/// </summary>
public class ElevatorController : MonoBehaviour
{
    // Event fired when elevator arrives at a floor
    public static event System.Action OnElevatorArrived;
    [Header("Elevator Configuration")]
    [Tooltip("The physical elevator car that moves")]
    [SerializeField] private Transform elevatorCar;
    
    [Tooltip("All floor stops - add as many as you need! Order by height (lowest to highest)")]
    [SerializeField] private Transform[] floorStops;
    
    [Tooltip("Floor level names for each stop (e.g., -1, 0, 1, 2, etc.)")]
    [SerializeField] private int[] floorLevels;
    
    [Tooltip("Which doors to open at each floor: Front, Back, or Both")]
    [SerializeField] private DoorConfiguration[] floorDoorConfigs;
    
    /// <summary>
    /// Door configuration for each floor
    /// </summary>
    [System.Serializable]
    public enum DoorConfiguration
    {
        FrontDoors,  // Open front doors only
        BackDoors,   // Open back doors only
        BothDoors    // Open both doors
    }
    
    [Header("Speed Settings")]
    [SerializeField] private float maxSpeed = 150f; // SUPER FAST! (was 50)
    [SerializeField] private float accelerationTime = 1.5f; // Time to reach max speed (faster ramp-up)
    [SerializeField] private float decelerationTime = 2.5f; // Time to slow down (smooth landing)
    
    [Header("Timing Settings")]
    [Tooltip("Delay before elevator starts moving after button press (allows doors to close)")]
    [SerializeField] private float movementStartDelay = 2.5f; // Delay before movement begins
    
    [Header("Player Containment")]
    [SerializeField] private Transform playerDetectionZone;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float detectionRadius = 3f;
    
    [Header("Audio System")]
    [SerializeField] private AudioSource elevatorAudioSource;
    [SerializeField] private AudioClip movementSound;
    [SerializeField] private AudioClip arrivalSound;
    [SerializeField] private AudioClip elevatorMusic; // Smooth jazz/muzak for that authentic elevator vibe
    
    [Header("Audio Settings")]
    [Tooltip("Distance at which elevator music starts playing (3D spatial audio) - Scaled for 320-unit player")]
    [SerializeField] private float musicStartDistance = 200f; // Increased for larger player scale
    [Tooltip("Distance at which music is at full volume - Scaled for 320-unit player")]
    [SerializeField] private float musicFullVolumeDistance = 80f; // Increased for larger player scale
    [Tooltip("Volume of elevator music (0-1)")]
    [SerializeField] private float musicVolume = 0.3f; // Quiet background music
    [Tooltip("Fade in/out time for music (seconds)")]
    [SerializeField] private float musicFadeTime = 1.5f;
    
    // Audio state tracking
    private AudioSource musicAudioSource; // Separate source for music
    private Transform playerTransform; // Cached player reference
    private bool isMusicPlaying = false;
    private Coroutine musicFadeCoroutine = null;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool enableDebugLogs = false; // Turn on for debugging
    
    // State tracking
    private bool isMoving = false;
    private int currentFloorIndex = 0; // Index in floorStops array
    private int targetFloorIndex = 0;
    private float currentSpeed = 0f;
    private Vector3 targetPosition;
    private Vector3 startPosition;
    private float journeyProgress = 0f;
    private Queue<int> floorQueue = new Queue<int>(); // Queue of requested floors
    
    // Player tracking - PERFECT SYNC SYSTEM (no parenting!)
    private List<CharacterController> playersInElevator = new List<CharacterController>();
    private Dictionary<CharacterController, Vector3> playerLocalPositions = new Dictionary<CharacterController, Vector3>();
    private Vector3 lastElevatorPosition;
    private Vector3 elevatorVelocity;
    private Vector3 lastFrameElevatorPosition; // For precise delta calculation
    
    // Coroutine reference
    private Coroutine movementCoroutine = null;

    void Awake()
    {
        if (elevatorCar == null)
        {
            elevatorCar = transform;
            Debug.LogWarning("[ElevatorController] elevatorCar not assigned, using self transform.");
        }
        
        // Validate setup
        if (floorStops == null || floorStops.Length == 0)
        {
            Debug.LogError("[ElevatorController] No floor stops assigned! Please add floor stop transforms in the Inspector!");
            return;
        }
        
        if (floorLevels == null || floorLevels.Length != floorStops.Length)
        {
            Debug.LogError($"[ElevatorController] Floor levels array size ({floorLevels?.Length ?? 0}) doesn't match floor stops ({floorStops.Length})!");
            return;
        }
        
        // Initialize door configs if not set
        if (floorDoorConfigs == null || floorDoorConfigs.Length != floorStops.Length)
        {
            Debug.LogWarning($"[ElevatorController] Door configurations not set or size mismatch. Initializing with default (FrontDoors).");
            floorDoorConfigs = new DoorConfiguration[floorStops.Length];
            for (int i = 0; i < floorDoorConfigs.Length; i++)
            {
                floorDoorConfigs[i] = DoorConfiguration.FrontDoors;
            }
        }
        
        // Validate floor stops
        for (int i = 0; i < floorStops.Length; i++)
        {
            if (floorStops[i] == null)
            {
                Debug.LogError($"[ElevatorController] Floor stop at index {i} is NULL!");
            }
        }
        
        // Log floor configuration
        Debug.Log($"[ElevatorController] Configured with {floorStops.Length} floors:");
        for (int i = 0; i < floorStops.Length; i++)
        {
            Debug.Log($"  Floor {floorLevels[i]}: {floorStops[i].position}");
        }
        
        // Start at first floor (lowest) by default
        currentFloorIndex = 0;
        if (floorStops[0] != null)
        {
            elevatorCar.position = floorStops[0].position;
            lastElevatorPosition = elevatorCar.position;
            lastFrameElevatorPosition = elevatorCar.position; // Initialize frame tracking
            Debug.Log($"[ElevatorController] Elevator initialized at floor {floorLevels[0]} (index {currentFloorIndex})");
        }
        
        // === AUDIO SYSTEM SETUP ===
        SetupAudioSystem();
        
        // Find player for distance-based music
        FindPlayer();
    }
    
    void Start()
    {
        // Broadcast that we're at the starting floor so doors can open
        // Delay slightly to ensure all systems are initialized
        StartCoroutine(OpenInitialDoors());
    }
    
    /// <summary>
    /// Open doors at starting floor after brief delay
    /// </summary>
    private IEnumerator OpenInitialDoors()
    {
        yield return new WaitForSeconds(0.5f);
        
        // Trigger arrival event for starting floor
        OnElevatorArrived?.Invoke();
        
        if (enableDebugLogs)
            Debug.Log($"[ElevatorController] Initial doors opened at floor {floorLevels[currentFloorIndex]}");
    }
    
    /// <summary>
    /// Setup 3D spatial audio system for elevator music
    /// </summary>
    private void SetupAudioSystem()
    {
        // Ensure main audio source exists
        if (elevatorAudioSource == null)
        {
            elevatorAudioSource = gameObject.AddComponent<AudioSource>();
            Debug.LogWarning("[ElevatorController] No AudioSource assigned, created one automatically.");
        }
        
        // Configure main audio source for movement sounds (scaled for 320-unit player)
        elevatorAudioSource.spatialBlend = 1.0f; // Full 3D
        elevatorAudioSource.rolloffMode = AudioRolloffMode.Linear;
        elevatorAudioSource.minDistance = 50f; // Increased for larger player scale
        elevatorAudioSource.maxDistance = 300f; // Increased for larger player scale
        elevatorAudioSource.loop = false; // We'll control looping manually
        elevatorAudioSource.dopplerLevel = 0f; // CRITICAL: Disable doppler to prevent pitch shifting
        
        // Create separate audio source for music
        musicAudioSource = gameObject.AddComponent<AudioSource>();
        musicAudioSource.spatialBlend = 1.0f; // Full 3D spatial audio
        musicAudioSource.rolloffMode = AudioRolloffMode.Linear;
        musicAudioSource.minDistance = musicFullVolumeDistance;
        musicAudioSource.maxDistance = musicStartDistance;
        musicAudioSource.loop = true; // Music loops continuously
        musicAudioSource.volume = 0f; // Start at zero, fade in
        musicAudioSource.playOnAwake = false;
        musicAudioSource.dopplerLevel = 0f; // CRITICAL: Disable doppler on music too
        
        if (elevatorMusic != null)
        {
            musicAudioSource.clip = elevatorMusic;
            Debug.Log("[ElevatorController] ✅ Elevator music system initialized (3D spatial audio)");
        }
    }
    
    /// <summary>
    /// Find player in scene for distance-based music
    /// </summary>
    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            if (enableDebugLogs) Debug.Log("[ElevatorController] Player found for music system");
        }
        else
        {
            Debug.LogWarning("[ElevatorController] Player not found! Music distance detection won't work. Make sure player has 'Player' tag.");
        }
    }

    void Update()
    {
        // Continuously check for players in elevator zone
        DetectPlayersInElevator();
        
        // Update 3D elevator music based on player distance
        UpdateElevatorMusic();
    }
    
    void FixedUpdate()
    {
        // PERFECT SYNC: Move players WITH the elevator in FixedUpdate for consistent physics timing
        // This ensures players move at the exact same Hz as physics and elevator movement
        if (isMoving && playersInElevator.Count > 0)
        {
            // Calculate precise elevator movement delta THIS frame
            Vector3 elevatorDelta = elevatorCar.position - lastFrameElevatorPosition;
            
            // Calculate velocity for external systems
            elevatorVelocity = elevatorDelta / Time.fixedDeltaTime;
            
            foreach (CharacterController player in playersInElevator)
            {
                if (player != null && player.enabled)
                {
                    // CRITICAL: Move player by exact elevator delta
                    // This ensures perfect 1:1 synchronization - no floating!
                    player.Move(elevatorDelta);
                    
                    if (enableDebugLogs && Time.frameCount % 60 == 0)
                    {
                        Debug.Log($"[ElevatorController] Player moved by {elevatorDelta.magnitude:F4} units (elevator delta)");
                    }
                }
            }
        }
        else
        {
            elevatorVelocity = Vector3.zero;
        }
        
        // Store current position for next frame's delta calculation
        lastFrameElevatorPosition = elevatorCar.position;
    }
    
    /// <summary>
    /// Smart 3D elevator music system - only plays when player is nearby
    /// Fades in/out smoothly based on distance
    /// </summary>
    private void UpdateElevatorMusic()
    {
        // Skip if no music assigned or no player
        if (elevatorMusic == null || musicAudioSource == null) return;
        
        // Try to find player if we lost reference
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return; // Still no player, skip
        }
        
        // Calculate distance to player
        float distanceToPlayer = Vector3.Distance(elevatorCar.position, playerTransform.position);
        
        // Should music be playing?
        bool shouldPlayMusic = (distanceToPlayer <= musicStartDistance);
        
        if (shouldPlayMusic && !isMusicPlaying)
        {
            // Start playing music with fade in
            if (enableDebugLogs) Debug.Log($"[ElevatorController] 🎵 Starting elevator music (distance: {distanceToPlayer:F1})");
            
            if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = StartCoroutine(FadeMusicVolume(0f, musicVolume, musicFadeTime));
            
            if (!musicAudioSource.isPlaying)
            {
                musicAudioSource.Play();
            }
            
            isMusicPlaying = true;
        }
        else if (!shouldPlayMusic && isMusicPlaying)
        {
            // Stop playing music with fade out
            if (enableDebugLogs) Debug.Log($"[ElevatorController] 🎵 Stopping elevator music (distance: {distanceToPlayer:F1})");
            
            if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = StartCoroutine(FadeMusicVolume(musicAudioSource.volume, 0f, musicFadeTime));
            
            isMusicPlaying = false;
        }
    }
    
    /// <summary>
    /// Smooth fade for elevator music volume
    /// </summary>
    private IEnumerator FadeMusicVolume(float startVolume, float targetVolume, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            musicAudioSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }
        
        musicAudioSource.volume = targetVolume;
        
        // Stop playing completely if faded to zero
        if (targetVolume <= 0.01f && musicAudioSource.isPlaying)
        {
            musicAudioSource.Stop();
        }
    }

    /// <summary>
    /// Call the elevator to a specific floor level (e.g., -1, 0, 1)
    /// </summary>
    public void CallElevatorToFloor(int floorLevel)
    {
        // Find the floor index for this level
        int floorIndex = GetFloorIndexFromLevel(floorLevel);
        
        if (floorIndex == -1)
        {
            Debug.LogError($"[ElevatorController] Invalid floor level: {floorLevel}");
            return;
        }
        
        // If already at this floor and not moving, trigger arrival event (re-opens doors if needed)
        if (currentFloorIndex == floorIndex && !isMoving)
        {
            if (enableDebugLogs) Debug.Log($"[ElevatorController] Already at floor {floorLevel} - triggering arrival event to open doors");
            OnElevatorArrived?.Invoke(); // ✅ Fire event so doors can open!
            return;
        }
        
        // Add to queue if moving, otherwise start immediately
        if (isMoving)
        {
            if (!floorQueue.Contains(floorIndex))
            {
                floorQueue.Enqueue(floorIndex);
                if (enableDebugLogs) Debug.Log($"[ElevatorController] Floor {floorLevel} added to queue");
            }
        }
        else
        {
            StartMovementToFloor(floorIndex);
        }
    }
    
    /// <summary>
    /// Get floor index from floor level number
    /// </summary>
    private int GetFloorIndexFromLevel(int floorLevel)
    {
        for (int i = 0; i < floorLevels.Length; i++)
        {
            if (floorLevels[i] == floorLevel)
            {
                return i;
            }
        }
        return -1;
    }
    
    /// <summary>
    /// Get current floor level
    /// </summary>
    public int GetCurrentFloorLevel()
    {
        if (currentFloorIndex >= 0 && currentFloorIndex < floorLevels.Length)
        {
            return floorLevels[currentFloorIndex];
        }
        return 0;
    }
    
    /// <summary>
    /// Get all available floor levels
    /// </summary>
    public int[] GetAvailableFloorLevels()
    {
        return floorLevels;
    }
    
    /// <summary>
    /// Get door configuration for current floor
    /// </summary>
    public DoorConfiguration GetCurrentFloorDoorConfig()
    {
        if (currentFloorIndex >= 0 && currentFloorIndex < floorDoorConfigs.Length)
        {
            return floorDoorConfigs[currentFloorIndex];
        }
        return DoorConfiguration.FrontDoors; // Default fallback
    }
    
    /// <summary>
    /// Get door configuration for a specific floor level
    /// </summary>
    public DoorConfiguration GetDoorConfigForFloor(int floorLevel)
    {
        int floorIndex = GetFloorIndexFromLevel(floorLevel);
        if (floorIndex >= 0 && floorIndex < floorDoorConfigs.Length)
        {
            return floorDoorConfigs[floorIndex];
        }
        return DoorConfiguration.FrontDoors; // Default fallback
    }

    /// <summary>
    /// Start elevator movement to a specific floor index
    /// </summary>
    private void StartMovementToFloor(int floorIndex)
    {
        if (floorIndex < 0 || floorIndex >= floorStops.Length)
        {
            Debug.LogError($"[ElevatorController] Invalid floor index: {floorIndex}");
            return;
        }
        
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
        }
        
        targetFloorIndex = floorIndex;
        startPosition = elevatorCar.position;
        targetPosition = floorStops[floorIndex].position;
        isMoving = true;
        journeyProgress = 0f;
        currentSpeed = 0f;
        
        if (enableDebugLogs) 
            Debug.Log($"[ElevatorController] Moving from floor {floorLevels[currentFloorIndex]} to floor {floorLevels[targetFloorIndex]}");
        
        movementCoroutine = StartCoroutine(DelayedMovementStart());
    }
    
    /// <summary>
    /// Wait for doors to close before starting movement
    /// </summary>
    private IEnumerator DelayedMovementStart()
    {
        // Wait for door close animation/delay
        if (movementStartDelay > 0)
        {
            yield return new WaitForSeconds(movementStartDelay);
        }
        
        if (enableDebugLogs)
            Debug.Log($"[ElevatorController] Doors closed - starting journey from floor {floorLevels[currentFloorIndex]} to floor {floorLevels[targetFloorIndex]}");
        
        // Start actual movement
        yield return StartCoroutine(SmoothMovementCoroutine());
    }

    /// <summary>
    /// Smooth movement with acceleration and deceleration curves
    /// </summary>
    private IEnumerator SmoothMovementCoroutine()
    {
        float totalDistance = Vector3.Distance(startPosition, targetPosition);
        float traveledDistance = 0f;
        
        // DIAGNOSTIC LOGGING
        if (enableDebugLogs)
        {
            Debug.Log($"[ElevatorController] COROUTINE STARTED!");
            Debug.Log($"[ElevatorController] Start Position: {startPosition}");
            Debug.Log($"[ElevatorController] Target Position: {targetPosition}");
            Debug.Log($"[ElevatorController] Total Distance: {totalDistance}");
            Debug.Log($"[ElevatorController] Elevator Car Position: {elevatorCar.position}");
            Debug.Log($"[ElevatorController] Players in elevator: {playersInElevator.Count}");
        }
        
        // Safety check
        if (totalDistance < 0.1f)
        {
            Debug.LogError($"[ElevatorController] PROBLEM! Distance is too small ({totalDistance}). Check your TopFloorPosition and BottomFloorPosition!");
            isMoving = false;
            yield break;
        }
        
        // Play movement sound (looping)
        if (elevatorAudioSource != null && movementSound != null)
        {
            elevatorAudioSource.clip = movementSound;
            elevatorAudioSource.loop = true; // Loop during movement
            elevatorAudioSource.volume = 1.0f; // Full volume for movement sound
            elevatorAudioSource.Play();
            
            if (enableDebugLogs) Debug.Log("[ElevatorController] 🔊 Movement sound started (looping)");
        }
        
        float timeElapsed = 0f;
        int frameCount = 0;
        currentSpeed = 0f; // Start at rest
        
        // Calculate phase durations
        float accelDuration = accelerationTime;
        float decelDuration = decelerationTime;
        
        if (enableDebugLogs)
            Debug.Log($"[ElevatorController] Entering movement loop. Max Speed: {maxSpeed}");
        
        // TIME-BASED movement with smooth curves
        while (traveledDistance < totalDistance)
        {
            frameCount++;
            if (enableDebugLogs && (frameCount == 1 || frameCount % 60 == 0))
            {
                Debug.Log($"[ElevatorController] Frame {frameCount}: Time {timeElapsed:F2}s, Traveled {traveledDistance:F2}/{totalDistance:F2}, Speed: {currentSpeed:F2}");
            }
            
            float deltaTime = Time.deltaTime;
            
            // Calculate how much distance remains
            float remainingDistance = totalDistance - traveledDistance;
            
            // TIME-BASED ACCELERATION/DECELERATION
            if (timeElapsed < accelDuration)
            {
                // ACCELERATION PHASE
                float t = timeElapsed / accelDuration;
                float smoothT = t * t * (3f - 2f * t); // Smoothstep
                currentSpeed = smoothT * maxSpeed;
            }
            else if (remainingDistance > (maxSpeed * decelDuration * 0.5f))
            {
                // CONSTANT SPEED PHASE (when far from end)
                currentSpeed = maxSpeed;
            }
            else
            {
                // DECELERATION PHASE (when approaching end)
                float stopDistance = maxSpeed * decelDuration * 0.5f;
                float t = remainingDistance / stopDistance;
                currentSpeed = Mathf.Lerp(10f, maxSpeed, t); // Linear decel from max to minimum
            }
            
            timeElapsed += deltaTime;
            
            // Move the elevator
            float moveDistance = currentSpeed * deltaTime;
            traveledDistance += moveDistance;
            
            // Ensure we don't overshoot
            if (traveledDistance > totalDistance)
            {
                traveledDistance = totalDistance;
            }
            
            journeyProgress = Mathf.Clamp01(traveledDistance / totalDistance);
            
            Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, journeyProgress);
            elevatorCar.position = newPosition;
            
            // Players move in FixedUpdate() for perfect sync!
            
            // Use WaitForFixedUpdate for physics-synchronized updates
            yield return new WaitForFixedUpdate();
        }
        
        // Ensure we reached exact target
        elevatorCar.position = targetPosition;
        lastElevatorPosition = targetPosition;
        currentFloorIndex = targetFloorIndex;
        isMoving = false;
        
        // === STOP MOVEMENT SOUND (CRITICAL - Stop the loop!) ===
        if (elevatorAudioSource != null)
        {
            if (elevatorAudioSource.isPlaying && elevatorAudioSource.loop)
            {
                elevatorAudioSource.Stop(); // Stop the looping movement sound
                elevatorAudioSource.loop = false; // Disable loop
                
                if (enableDebugLogs) Debug.Log("[ElevatorController] 🔇 Movement sound STOPPED");
            }
            
            // Play arrival sound (one-shot)
            if (arrivalSound != null)
            {
                elevatorAudioSource.PlayOneShot(arrivalSound);
                if (enableDebugLogs) Debug.Log("[ElevatorController] 🔔 Arrival sound played");
            }
        }
        
        if (enableDebugLogs) 
            Debug.Log($"[ElevatorController] Arrived at floor {floorLevels[currentFloorIndex]}. Journey time: {timeElapsed:F2}s");
        
        // Trigger arrival event so doors can open
        OnElevatorArrived?.Invoke();
        
        // Process next floor in queue if any
        if (floorQueue.Count > 0)
        {
            int nextFloor = floorQueue.Dequeue();
            if (nextFloor != currentFloorIndex)
            {
                yield return new WaitForSeconds(2f); // Brief pause at floor
                StartMovementToFloor(nextFloor);
            }
        }
    }

    /// <summary>
    /// Detect players inside the elevator - PERFECT SYNC DETECTION
    /// </summary>
    private void DetectPlayersInElevator()
    {
        if (playerDetectionZone == null) return;
        
        Collider[] detectedColliders = Physics.OverlapSphere(playerDetectionZone.position, detectionRadius, playerLayer);
        
        // Track newly detected players
        foreach (Collider col in detectedColliders)
        {
            // Check if this is a player with CharacterController
            CharacterController cc = col.GetComponent<CharacterController>();
            if (cc == null) continue;
            
            if (!playersInElevator.Contains(cc))
            {
                playersInElevator.Add(cc);
                
                // Store initial local position relative to elevator
                Vector3 localPos = elevatorCar.InverseTransformPoint(cc.transform.position);
                playerLocalPositions[cc] = localPos;
                
                if (enableDebugLogs) 
                    Debug.Log($"[ElevatorController] ✅ Player entered elevator: {cc.name} - Perfect sync enabled! Local pos: {localPos}");
            }
        }
        
        // Remove players who left the elevator
        for (int i = playersInElevator.Count - 1; i >= 0; i--)
        {
            CharacterController player = playersInElevator[i];
            if (player == null || Vector3.Distance(player.transform.position, playerDetectionZone.position) > detectionRadius)
            {
                playersInElevator.RemoveAt(i);
                if (playerLocalPositions.ContainsKey(player))
                {
                    playerLocalPositions.Remove(player);
                }
                
                if (enableDebugLogs && player != null) 
                    Debug.Log($"[ElevatorController] Player left elevator: {player.name}");
            }
        }
    }

    /// <summary>
    /// Get the platform velocity for external systems (like AAAMovementController)
    /// This allows the movement system to know it's on a moving platform
    /// </summary>
    public Vector3 GetPlatformVelocity()
    {
        return isMoving ? elevatorVelocity : Vector3.zero;
    }
    
    /// <summary>
    /// Check if a specific CharacterController is in the elevator
    /// </summary>
    public bool IsPlayerInElevator(CharacterController player)
    {
        return playersInElevator.Contains(player);
    }

    /// <summary>
    /// Emergency stop (for debugging or special cases)
    /// </summary>
    public void EmergencyStop()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }
        
        isMoving = false;
        currentSpeed = 0f;
        
        // Stop ALL audio immediately
        if (elevatorAudioSource != null)
        {
            elevatorAudioSource.Stop();
            elevatorAudioSource.loop = false;
        }
        
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
            StartCoroutine(FadeMusicVolume(musicAudioSource.volume, 0f, 0.5f));
        }
        
        Debug.LogWarning("[ElevatorController] ⚠️ EMERGENCY STOP activated - all audio stopping!");
    }
    
    /// <summary>
    /// Cleanup when elevator is destroyed or disabled
    /// </summary>
    private void OnDestroy()
    {
        // Stop all audio to prevent leaks
        if (elevatorAudioSource != null && elevatorAudioSource.isPlaying)
        {
            elevatorAudioSource.Stop();
        }
        
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Stop();
        }
        
        // Stop any running coroutines
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
        }
        
        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }
    }
    
    private void OnDisable()
    {
        // Stop music when disabled (e.g., scene change)
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Stop();
        }
    }

    /// <summary>
    /// Get current elevator status
    /// </summary>
    public bool IsMoving => isMoving;
    public int CurrentFloorIndex => currentFloorIndex;
    public int CurrentFloorLevel => GetCurrentFloorLevel();
    public float CurrentSpeed => currentSpeed;
    public int PlayersInElevator => playersInElevator.Count;

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        // Use elevatorCar position if available, otherwise use this transform
        Vector3 elevatorPos = (elevatorCar != null) ? elevatorCar.position : transform.position;
        
        // Draw floor positions
        if (floorStops != null)
        {
            for (int i = 0; i < floorStops.Length; i++)
            {
                if (floorStops[i] == null) continue;
                
                // Color code: green for current floor, yellow for others
                Gizmos.color = (i == currentFloorIndex) ? Color.green : Color.yellow;
                Gizmos.DrawWireSphere(floorStops[i].position, 2f);
                Gizmos.DrawLine(floorStops[i].position + Vector3.left * 3f, floorStops[i].position + Vector3.right * 3f);
                
                // Draw floor level number
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(floorStops[i].position + Vector3.up * 3f, $"Floor {floorLevels[i]}");
                #endif
            }
        }
        
        // Draw player detection zone
        if (playerDetectionZone != null)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.DrawWireSphere(playerDetectionZone.position, detectionRadius);
        }
        
        // Draw paths between floors
        if (floorStops != null && floorStops.Length > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < floorStops.Length - 1; i++)
            {
                if (floorStops[i] != null && floorStops[i + 1] != null)
                {
                    Gizmos.DrawLine(floorStops[i].position, floorStops[i + 1].position);
                }
            }
        }
        
        // === DRAW MUSIC RANGE (3D Audio Visualization) ===
        if (elevatorMusic != null)
        {
            // Music start distance (fade begins)
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f); // Orange, very transparent
            Gizmos.DrawWireSphere(elevatorPos, musicStartDistance);
            
            // Music full volume distance
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f); // Orange, more visible
            Gizmos.DrawWireSphere(elevatorPos, musicFullVolumeDistance);
            
            // Draw music icon at elevator position
            Gizmos.color = Color.yellow;
            Vector3 musicIconPos = elevatorPos + Vector3.up * 5f;
            Gizmos.DrawLine(musicIconPos + Vector3.left * 2f, musicIconPos + Vector3.right * 2f);
            Gizmos.DrawLine(musicIconPos + Vector3.forward * 2f, musicIconPos + Vector3.back * 2f);
        }
    }
}
