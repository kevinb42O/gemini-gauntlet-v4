using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GeminiGauntlet.Audio;
using GeminiGauntlet.Missions.Integration;

public class ChestController : MonoBehaviour
{
    public enum ChestState
    {
        Hidden,      // Chest is underground/invisible
        Emerging,    // Chest is rising from the ground
        Closed,      // Chest is visible but closed
        Opening,     // Chest lid is opening
        Open,        // Chest is fully open and gems spawned
        Interacted   // Player has interacted with the chest (opened inventory)
    }
    
    public enum ChestType
    {
        Spawned,     // Dynamically spawned by ChestManager (spawns gems)
        Manual       // Manually placed in scene (no gem spawning)
    }

    [Header("Chest Configuration")]
    [Tooltip("Type of chest: Spawned (from platform conquest) or Manual (pre-placed in scene)")]
    public ChestType chestType = ChestType.Manual;
    
    [Tooltip("Current state of the chest")]
    public ChestState currentState = ChestState.Hidden;
    
    // Track if this platform has ever had towers spawned
    private bool platformHadTowers = false;
    private int lastKnownTowerCount = 0;
    
    [Header("Emergence Settings")]
    [Tooltip("How deep underground the chest starts before emerging")]
    public float emergenceDepth = 2.0f;
    [Tooltip("Time it takes for the chest to emerge from the ground")]
    public float emergenceDuration = 2.0f;
    [Tooltip("Animation curve for emergence movement")]
    public AnimationCurve emergenceCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Opening Settings")]
    [Tooltip("Delay before chest automatically opens after emerging")]
    public float openingDelay = 1.0f;
    [Tooltip("Time it takes for the chest lid to open")]
    public float openingDuration = 1.5f;
    [Tooltip("Animation curve for lid opening")]
    public AnimationCurve openingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Chest Parts")]
    [Tooltip("The main chest body (base)")]
    public Transform chestBody;
    [Tooltip("The chest lid that opens and closes")]
    public Transform chestLid;
    [Tooltip("The rotation axis for the lid opening")]
    public Vector3 lidOpenRotation = new Vector3(-90f, 0f, 0f);
    
    [Header("Gem Spawning")]
    [Tooltip("Prefab for gems to spawn from the chest")]
    public GameObject gemPrefab;
    [Tooltip("Minimum number of gems to spawn")]
    public int minGemCount = 1;
    [Tooltip("Maximum number of gems to spawn")]
    public int maxGemCount = 10;
    [Tooltip("Force applied to gems when they're ejected from chest")]
    public float gemEjectionForce = 8f;
    [Tooltip("Spread angle for gem ejection (degrees)")]
    public float gemSpreadAngle = 45f;
    [Tooltip("Height offset for gem spawn position")]
    public float gemSpawnHeight = 1f;
    [Tooltip("Should this chest spawn gems? (Auto-enabled for Spawned chests, manually set for Manual chests)")]
    public bool shouldSpawnGems = false;
    
    [Header("Self-Revive Spawning")]
    [Tooltip("Self-revive item data to spawn in chest")]
    public SelfReviveItemData selfReviveItemData;
    [Tooltip("Chance (0-100%) for self-revive to spawn in this chest")]
    [Range(0f, 100f)]
    public float selfReviveSpawnChance = 15f;
    [Tooltip("Number of self-revive items to spawn if chance succeeds")]
    [Range(1, 3)]
    public int selfReviveCount = 1;
    
    [Header("Effects")]
    [Tooltip("Particle effect when chest emerges")]
    public GameObject emergenceEffectPrefab;
    [Tooltip("Particle effect when chest opens")]
    public GameObject openingEffectPrefab;
    [Tooltip("Particle effect when gems are spawned")]
    public GameObject gemSpawnEffectPrefab;
    
    [Header("Audio")]
    [Range(0f, 1f)]
    public float audioVolume = 0.8f; // Volume multiplier for all chest sounds
    
    // Sound manager for proximity-based humming
    private ChestSoundManager chestSoundManager;
    
    [Header("Debug")]
    [Tooltip("Enable detailed debug logging")]
    public bool enableDebugLogs = false;
    
    // Private variables
    private Transform associatedPlatform;
    private Vector3 hiddenPosition;
    private Vector3 emergedPosition;
    private Quaternion lidClosedRotation;
    private Quaternion lidOpenRotation_Quaternion;
    private bool hasSpawnedGems = false;
    private List<GameObject> spawnedGems = new List<GameObject>();
    
    // Static tracking for all chests
    public static List<ChestController> AllChests = new List<ChestController>();
    
    // Static tracking for first chest opened sound (plays only once per game session)
    private static bool hasPlayedFirstChestSound = false;
    
    void Awake()
    {
        // Register this chest
        if (!AllChests.Contains(this))
        {
            AllChests.Add(this);
        }
        
        // Get or add ChestSoundManager component
        chestSoundManager = GetComponent<ChestSoundManager>();
        if (chestSoundManager == null)
        {
            chestSoundManager = gameObject.AddComponent<ChestSoundManager>();
            DebugLog($"[ChestController] ✅ Added ChestSoundManager to {gameObject.name}");
        }
        else
        {
            DebugLog($"[ChestController] ✅ Found existing ChestSoundManager on {gameObject.name}");
        }
        
        // Configure chest based on type
        ConfigureChestType();
        
        // NOTE: Don't try to find parent platform here - it may not be set yet during instantiation
        // The ChestManager will call SetPlatformAssociation() after parenting is complete
        
        // Set up positions and rotations (will be updated when platform is assigned)
        SetupChestPositions();
        
        // Start in appropriate state based on chest type
        // NOTE: Don't start humming in Awake - SoundEventsManager may not be ready yet
        if (chestType == ChestType.Manual)
        {
            // Manual chests start visible and closed (ready for interaction)
            // Set state without triggering sound (will be triggered in Start())
            currentState = ChestState.Closed;
            SetChestVisibility(true);
            DebugLog($"[ChestController] Manual chest '{name}' starting in CLOSED state (will start humming in Start())");
        }
        else
        {
            // Spawned chests start hidden (will emerge when platform is conquered)
            currentState = ChestState.Hidden;
            SetChestVisibility(false);
            transform.localPosition = hiddenPosition;
            DebugLog($"[ChestController] Spawned chest '{name}' starting in HIDDEN state");
        }
    }
    
    void OnEnable()
    {
        // Runtime spawned chests don't need complex event subscriptions
        // ChestManager handles all the tower death monitoring
    }
    
    void OnDisable()
    {
        // No events to unsubscribe from
    }
    
    void OnDestroy()
    {
        AllChests.Remove(this);
    }
    
    void Start()
    {
        // Runtime spawned chests don't need initial tower completion checks
        // ChestManager handles all tower monitoring and spawning logic
        SetupChestPositions();
        
        // Additional setup for manual chests
        if (chestType == ChestType.Manual && currentState == ChestState.Closed)
        {
            // NOW start humming - SoundEventsManager should be initialized by now
            if (chestSoundManager != null)
            {
                DebugLog($"[ChestController] 🎵 Starting humming for manual chest '{name}' in Start()");
                chestSoundManager.StartChestHumming();
            }
            DebugLog($"Manual chest '{name}' initialized and ready for interaction");
        }
    }
    
    void Update()
    {
        // Track tower presence on associated platform for chest emergence logic
        if (associatedPlatform != null && currentState == ChestState.Hidden)
        {
            UpdateTowerTracking();
        }
    }
    
    /// <summary>
    /// Update tower tracking for platform clearing detection
    /// </summary>
    private void UpdateTowerTracking()
    {
        // Find TowerSpawner on the associated platform
        TowerSpawner towerSpawner = associatedPlatform.GetComponent<TowerSpawner>();
        if (towerSpawner != null)
        {
            // Check if platform has active towers
            TowerController[] activeTowers = associatedPlatform.GetComponentsInChildren<TowerController>();
            int currentTowerCount = activeTowers.Count(t => t != null && !t.IsDead);
            
            // Track if this platform has ever had towers
            if (currentTowerCount > 0)
            {
                platformHadTowers = true;
                lastKnownTowerCount = currentTowerCount;
            }
            
            // If platform had towers but now has none, it's been cleared
            if (platformHadTowers && currentTowerCount == 0 && lastKnownTowerCount > 0)
            {
                StartImmediateEmergence();
                lastKnownTowerCount = 0; // Reset to prevent multiple triggers
            }
        }
    }
    
    void SetupChestPositions()
    {
        // For manual chests, use current position as emerged position (no hiding)
        // For spawned chests, respect the spawn position set by ChestManager
        emergedPosition = transform.localPosition; // Keep the position ChestManager set (or manual placement)
        
        if (chestType == ChestType.Manual)
        {
            // Manual chests don't need to emerge, so hidden position is same as emerged
            hiddenPosition = emergedPosition;
        }
        else
        {
            // Spawned chests hide underground before emerging
            hiddenPosition = emergedPosition - Vector3.up * emergenceDepth;
        }
        
        // Ensure chest is upright (no weird rotations)
        transform.localRotation = Quaternion.identity;
        
        
        // Set up lid rotations
        if (chestLid != null)
        {
            lidClosedRotation = chestLid.localRotation;
            lidOpenRotation_Quaternion = Quaternion.Euler(lidOpenRotation);
            
            // Ensure lid starts closed
            chestLid.localRotation = lidClosedRotation;
        }
        else
        {
        }
    }
    

    
    public void StartChestEmergence()
    {
        if (currentState != ChestState.Hidden)
        {
            return;
        }
        
        StartCoroutine(EmergenceSequence());
    }
    
    IEnumerator EmergenceSequence()
    {
        SetChestState(ChestState.Emerging);
        
        // Play emergence effect and sound
        if (emergenceEffectPrefab != null)
        {
            GameObject effect = Instantiate(emergenceEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 5f);
        }
        
        // 🎵 CHEST EMERGENCE SOUND
        
        // Play centralized chest emergence sound
        GameSounds.PlayChestEmergence(transform.position, audioVolume);
        // No tower or gem spawn sound during chest emergence as requested
        
        float timer = 0f;
        Vector3 startPos = transform.localPosition;
        
        while (timer < emergenceDuration)
        {
            float t = emergenceCurve.Evaluate(timer / emergenceDuration);
            Vector3 newPos = Vector3.Lerp(hiddenPosition, emergedPosition, t);
            transform.localPosition = newPos;
            
            // Log progress occasionally
            
            timer += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final position
        transform.localPosition = emergedPosition;
        
        SetChestState(ChestState.Closed);
        
        yield return new WaitForSeconds(openingDelay);
        
        // Start opening sequence
        StartCoroutine(OpeningSequence());
    }
    
    IEnumerator OpeningSequence()
    {
        // Allow opening from both Closed and Open states (for instant interaction)
        if (currentState != ChestState.Closed && currentState != ChestState.Open && currentState != ChestState.Opening)
        {
            yield break;
        }
        
        SetChestState(ChestState.Opening);
        
        // Play opening effect and sound
        if (openingEffectPrefab != null)
        {
            GameObject effect = Instantiate(openingEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 5f);
        }
        
        // 🎵 CHEST OPENING SOUND: Play ONLY the chest opening sound (removed duplicate sounds)
        GameSounds.PlayChestOpening(transform.position, audioVolume);
        
        float timer = 0f;
        
        // Animate lid opening with PROPER CURVE EVALUATION
        if (chestLid != null)
        {
            while (timer < openingDuration)
            {
                float normalizedTime = timer / openingDuration;
                float curveValue = openingCurve.Evaluate(normalizedTime); // FIX: Apply the curve!
                chestLid.localRotation = Quaternion.Slerp(lidClosedRotation, lidOpenRotation_Quaternion, curveValue);
                
                timer += Time.deltaTime;
                yield return null;
            }
            
            // Ensure final rotation
            chestLid.localRotation = lidOpenRotation_Quaternion;
        }
        else
        {
            // Still wait for the duration even without lid
            yield return new WaitForSeconds(openingDuration);
        }
        
        SetChestState(ChestState.Open);
        
        // Spawn gems if configured to do so (works for both Manual and Spawned chests)
        if (shouldSpawnGems && !hasSpawnedGems)
        {
            StartCoroutine(SpawnGemsSequentially());
        }
        else
        {
            DebugLog($"ChestController ({name}): Skipping gem spawn (shouldSpawnGems={shouldSpawnGems}, hasSpawnedGems={hasSpawnedGems})");
        }
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(message);
        }
    }
    
    // ----- PLAYER INTERACTION METHODS -----
    
    /// <summary>
    /// Called when player interacts with this chest
    /// </summary>
    /// <returns>True if interaction was successful, false otherwise</returns>
    public bool PlayerInteract()
    {
        // Manual chests can be interacted with when Closed - they will open AND show inventory immediately
        if (chestType == ChestType.Manual && currentState == ChestState.Closed)
        {
            // Start opening animation (visual only)
            StartCoroutine(OpeningSequence());
            
            // Set state to Interacted immediately so inventory opens
            SetChestState(ChestState.Interacted);
            
            // 🎯 MISSION TRACKING: Chest looted (player opened chest)
            MissionProgressHooks.OnChestLooted();
            
            // 🎵 INTERACTION SOUND: Play sound for first interaction
            if (!hasPlayedFirstChestSound)
            {
                GameSounds.PlayPowerUpStart(transform.position, audioVolume);
                hasPlayedFirstChestSound = true;
                DebugLog($"ChestController ({name}): Played first chest discovery sound!");
            }
            else
            {
                GameSounds.PlayUIFeedback(transform.position);
            }
            
            DebugLog($"ChestController ({name}): Manual chest - opening animation + inventory UI");
            return true; // Return true to open inventory immediately
        }
        
        // Spawned chests or already-open chests
        if (currentState == ChestState.Open)
        {
            // Set state to Interacted
            SetChestState(ChestState.Interacted);
            
            // 🎯 MISSION TRACKING: Chest looted (player opened chest)
            MissionProgressHooks.OnChestLooted();
            
            // 🎵 INTERACTION SOUND
            if (!hasPlayedFirstChestSound)
            {
                GameSounds.PlayPowerUpStart(transform.position, audioVolume);
                hasPlayedFirstChestSound = true;
            }
            else
            {
                GameSounds.PlayUIFeedback(transform.position);
            }
            
            DebugLog($"ChestController ({name}): Player interacted with open chest");
            return true;
        }
        
        // Chest not ready for interaction
        return false;
    }
    
    /// <summary>
    /// Called when player closes the chest inventory
    /// </summary>
    public void PlayerCloseInteraction()
    {
        // If we were interacted with, go back to open state
        if (currentState == ChestState.Interacted)
        {
            SetChestState(ChestState.Open);
            DebugLog($"ChestController ({name}): Player closed chest interaction");
        }
    }
    
    /// <summary>
    /// Check if this chest is in a state where the player can interact with it
    /// </summary>
    public bool IsInteractable()
    {
        // Manual chests are interactable when Closed (to trigger opening)
        if (chestType == ChestType.Manual && currentState == ChestState.Closed)
        {
            return true;
        }
        
        // All chests are interactable when Open (for inventory access)
        return currentState == ChestState.Open;
    }
    
    /// <summary>
    /// Check if this chest has already been interacted with by the player
    /// </summary>
    public bool HasBeenInteractedWith()
    {
        return currentState == ChestState.Interacted;
    }
    
    /// <summary>
    /// Get the current state of the chest
    /// </summary>
    public ChestState GetCurrentState()
    {
        return currentState;
    }
    
    /// <summary>
    /// Force the chest to be in the Open state for interaction
    /// </summary>
    public void ForceOpenState()
    {
        if (currentState != ChestState.Open)
        {
            SetChestState(ChestState.Open);
        }
    }

    private void SpawnGems()
    {
        if (hasSpawnedGems)
        {
            return;
        }
        
        if (gemPrefab == null)
        {
            return;
        }
        
        int gemCount = Random.Range(minGemCount, maxGemCount + 1);
        
        // SET FLAG IMMEDIATELY to prevent multiple calls
        hasSpawnedGems = true;
        
        for (int i = 0; i < gemCount; i++)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 1f;
            GameObject gem = Instantiate(gemPrefab, spawnPos, Quaternion.identity);
            
            if (gem == null)
            {
                continue;
            }
            
            
            // Keep original prefab scale - no forced scaling
            
            // Parent gem directly to the chest so it stays with the chest
            gem.transform.SetParent(transform, true);
            
            // Add some physics force for ejection effect
            Rigidbody gemRb = gem.GetComponent<Rigidbody>();
            if (gemRb != null)
            {
                Vector3 randomDirection = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(0.5f, 1f),
                    Random.Range(-1f, 1f)
                ).normalized;
                
                float ejectForce = gemEjectionForce;
                gemRb.AddForce(randomDirection * ejectForce, ForceMode.Impulse);
            }
            else
            {
            }
            
            // Track spawned gems
            spawnedGems.Add(gem);
            
            // Play gem spawn sound using centralized system
            GameSounds.PlayGemSpawn(gem.transform.position, audioVolume * 0.7f);
        }
        
    }
    
    // FIXED: Sequential gem spawning with proper positioning and spacing
    private IEnumerator SpawnGemsSequentially()
    {
        if (hasSpawnedGems)
        {
            yield break;
        }
        
        if (gemPrefab == null)
        {
            yield break;
        }
        
        int gemCount = Random.Range(minGemCount, maxGemCount + 1);
        
        // SET FLAG IMMEDIATELY to prevent multiple calls
        hasSpawnedGems = true;
        
        // Calculate spawn positions in a circle around chest with height variation
        Vector3 chestCenter = transform.position + Vector3.up * gemSpawnHeight;
        float angleStep = 360f / gemCount;
        float spawnRadius = 0.5f; // Radius around chest center
        
        for (int i = 0; i < gemCount; i++)
        {
            // Calculate spawn position in circle with height variation
            float angle = i * angleStep * Mathf.Deg2Rad;
            float randomRadius = Random.Range(spawnRadius * 0.3f, spawnRadius * 1.2f);
            float heightVariation = Random.Range(-0.2f, 0.4f);
            
            Vector3 spawnPos = chestCenter + new Vector3(
                Mathf.Cos(angle) * randomRadius,
                heightVariation,
                Mathf.Sin(angle) * randomRadius
            );
            
            GameObject gem = Instantiate(gemPrefab, spawnPos, Quaternion.identity);
            
            if (gem == null)
            {
                continue;
            }
            
            
            // Keep original prefab scale - no forced scaling
            
            // Parent gem directly to the chest so it stays with the chest
            gem.transform.SetParent(transform, true);
            
            // Add physics force for ejection effect with better distribution
            Rigidbody gemRb = gem.GetComponent<Rigidbody>();
            if (gemRb != null)
            {
                // Use the calculated angle for more consistent spread
                Vector3 ejectionDirection = new Vector3(
                    Mathf.Cos(angle),
                    Random.Range(0.8f, 1.2f), // More upward bias
                    Mathf.Sin(angle)
                ).normalized;
                
                // Add some randomness to the direction
                ejectionDirection += new Vector3(
                    Random.Range(-0.3f, 0.3f),
                    Random.Range(-0.1f, 0.2f),
                    Random.Range(-0.3f, 0.3f)
                );
                ejectionDirection.Normalize();
                
                float ejectForce = gemEjectionForce * Random.Range(0.8f, 1.2f);
                gemRb.AddForce(ejectionDirection * ejectForce, ForceMode.Impulse);
                
                // Add slight rotation for visual appeal
                gemRb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
            }
            
            // Make chest-spawned gems immediately collectable (skip the "need to be hit" phase)
            Gem gemComponent = gem.GetComponent<Gem>();
            if (gemComponent != null)
            {
                // Use reflection to call the protected DetachFromSource method
                var detachMethod = typeof(Gem).GetMethod("DetachFromSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (detachMethod != null)
                {
                    detachMethod.Invoke(gemComponent, null);
                }
                else
                {
                }
            }
            
            // Track spawned gems
            spawnedGems.Add(gem);
            
            // Play gem spawn sound with slight pitch variation
            GameSounds.PlayGemSpawn(gem.transform.position, audioVolume * 0.7f);
            
            // Wait before spawning next gem (shorter delay for better flow)
            if (i < gemCount - 1)
            {
                yield return new WaitForSeconds(0.15f); // Reduced from 0.3f for faster spawning
            }
        }
        
    }
    
    private void SetChestState(ChestState newState)
    {
        ChestState oldState = currentState;
        currentState = newState;
        
        DebugLog($"[ChestController] 🔄 State transition: {oldState} → {newState} on {gameObject.name}");
        
        // Handle chest humming based on state transitions
        if (chestSoundManager != null)
        {
            // Start humming when chest becomes Closed (ready for interaction)
            if (newState == ChestState.Closed)
            {
                // Always try to start humming when entering Closed state
                DebugLog($"[ChestController] 🎵 Attempting to start humming for {gameObject.name}");
                chestSoundManager.StartChestHumming();
                Debug.Log($"📦 ChestController ({name}): Started chest humming (state: {oldState} → Closed)");
            }
            // Stop humming when chest is opened or hidden
            else if (newState == ChestState.Opening || newState == ChestState.Open || 
                     newState == ChestState.Interacted || newState == ChestState.Hidden)
            {
                if (chestSoundManager.IsHumming)
                {
                    DebugLog($"[ChestController] 🛑 Stopping humming for {gameObject.name}");
                    chestSoundManager.StopChestHumming();
                    Debug.Log($"📦 ChestController ({name}): Stopped chest humming (state: {oldState} → {newState})");
                }
            }
        }
        else
        {
            Debug.LogError($"📦 ChestController ({name}): ChestSoundManager is NULL! Cannot play humming. This should never happen!");
            // Try to recover by adding the component
            chestSoundManager = gameObject.AddComponent<ChestSoundManager>();
            Debug.LogWarning($"📦 ChestController ({name}): Emergency recovery - added ChestSoundManager");
        }
        
        // Update visuals based on state
        switch (currentState)
        {
            case ChestState.Hidden:
                SetChestVisibility(false);
                transform.localPosition = hiddenPosition;
                break;
                
            case ChestState.Emerging:
            case ChestState.Closed:
            case ChestState.Opening:
            case ChestState.Open:
                SetChestVisibility(true);
                break;
        }
    }
    
    private void SetChestVisibility(bool visible)
    {
        
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = visible;
        }
        
        // Also handle colliders if needed
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = visible;
        }
    }
    
    // Public methods for external control
    public bool IsChestComplete()
    {
        return currentState == ChestState.Open;
    }
    
    public int GetSpawnedGemCount()
    {
        return spawnedGems.Count(gem => gem != null);
    }
    
    public void ForceEmergence()
    {
        if (currentState == ChestState.Hidden)
        {
            StartChestEmergence();
        }
    }
    
    // Static method to get chest for a specific platform
    public static ChestController GetChestForPlatform(Transform platform)
    {
        return AllChests.FirstOrDefault(chest => chest != null && chest.associatedPlatform == platform);
    }
    
    // Static method to check if platform has active chest
    public static bool PlatformHasActiveChest(Transform platform)
    {
        ChestController chest = GetChestForPlatform(platform);
        return chest != null && chest.currentState != ChestState.Hidden;
    }
    
    /// <summary>
    /// Configure chest behavior based on chest type
    /// </summary>
    private void ConfigureChestType()
    {
        // Both Manual and Spawned chests can spawn gems if configured to do so
        // shouldSpawnGems can be set manually in the Inspector for Manual chests
        // For Spawned chests, it's automatically enabled
        if (chestType == ChestType.Spawned)
        {
            shouldSpawnGems = true;
        }
        // For Manual chests, respect the Inspector setting (don't force it to false)
        
        DebugLog($"ChestController ({name}): Configured as {chestType} chest (shouldSpawnGems={shouldSpawnGems})");
    }
    
    /// <summary>
    /// NEW METHOD for ChestManager integration - sets platform association at runtime
    /// Marks this chest as a Spawned type (dynamically created by ChestManager)
    /// </summary>
    public void SetPlatformAssociation(Transform platform)
    {
        associatedPlatform = platform;
        
        // Mark this as a spawned chest (since ChestManager is calling this)
        chestType = ChestType.Spawned;
        ConfigureChestType();
        
        DebugLog($"ChestController ({name}): Associated with platform '{platform.name}' and marked as Spawned chest");
    }
    
    /// <summary>
    /// NEW METHOD for ChestManager - starts immediate emergence without hidden state tracking
    /// </summary>
    public void StartImmediateEmergence()
    {
        
        // Skip all the complex hidden state logic - go straight to emergence
        if (currentState == ChestState.Hidden)
        {
            StartChestEmergence();
        }
        else
        {
        }
    }
    
    // DEBUG METHODS - Remove these after testing
    [ContextMenu("Force Chest Emergence Test")]
    public void ForceEmergenceTest()
    {
        if (currentState == ChestState.Hidden)
        {
            StartChestEmergence();
        }
        else
        {
        }
    }
    
    [ContextMenu("Reset Chest to Hidden State")]
    public void ResetToHiddenState()
    {
        SetChestState(ChestState.Hidden);
        hasSpawnedGems = false;
        
        // Clear spawned gems list
        foreach (GameObject gem in spawnedGems)
        {
            if (gem != null) DestroyImmediate(gem);
        }
        spawnedGems.Clear();
    }
    
    [ContextMenu("🎵 TEST: Force Closed State (Start Humming)")]
    public void ForceClosedStateTest()
    {
        Debug.Log($"[DEBUG] Forcing chest '{name}' to CLOSED state (should start humming)");
        SetChestState(ChestState.Closed);
        
        if (chestSoundManager != null)
        {
            Debug.Log($"[DEBUG] ChestSoundManager exists, IsHumming: {chestSoundManager.IsHumming}");
        }
        else
        {
            Debug.LogError($"[DEBUG] ChestSoundManager is NULL on {name}!");
        }
    }
    
    [ContextMenu("🔍 TEST: Check Chest Audio Status")]
    public void CheckChestAudioStatus()
    {
        Debug.Log($"========== CHEST STATUS: {name} ==========");
        Debug.Log($"Current State: {currentState}");
        Debug.Log($"Chest Type: {chestType}");
        Debug.Log($"ChestSoundManager: {(chestSoundManager != null ? "EXISTS" : "NULL")}");
        
        if (chestSoundManager != null)
        {
            Debug.Log($"Is Humming: {chestSoundManager.IsHumming}");
            // Trigger the sound manager's debug check
            var debugMethod = chestSoundManager.GetType().GetMethod("DebugCheckStatus");
            if (debugMethod != null)
            {
                debugMethod.Invoke(chestSoundManager, null);
            }
        }
        
        Debug.Log($"==========================================");
    }
}
