using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GeminiGauntlet.Audio;

/// <summary>
/// Platform Capture System - King of the Hill mission logic
/// Player must stay within Central Tower radius for specified duration to capture platform
/// </summary>
public class PlatformCaptureSystem : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Central Tower that defines the capture radius")]
    public CentralTower centralTower;
    
    [Tooltip("The Platform Trigger that detects player presence on platform")]
    public PlatformTrigger platformTrigger;
    
    [Tooltip("The Tower Spawner for this platform")]
    public TowerSpawner towerSpawner;
    
    [Tooltip("The Tower Protector Cube (optional - becomes friendly if alive when captured)")]
    public SkullSpawnerCube towerProtectorCube;
    
    [Tooltip("UI controller for the capture progress slider")]
    public PlatformCaptureUI progressUI;
    
    [Header("Audio & VFX")]
    [Tooltip("Sound Events ScriptableObject for capture sounds")]
    public SoundEvents soundEvents;
    
    [Tooltip("Particle effect to activate when platform is captured (ground effect)")]
    public GameObject captureCompleteParticles;
    
    [Tooltip("Lighting controller for dramatic atmosphere changes")]
    public CaptureLightingController lightingController;
    
    [Header("Capture Settings")]
    [Tooltip("Time in seconds required to capture the platform")]
    public float captureDuration = 120f;
    
    [Tooltip("Rate at which progress drains when player leaves radius (same as fill rate by default)")]
    public float drainRate = 1f; // Will be calculated as 1/captureDuration
    
    [Header("Mission State")]
    [Tooltip("Is this capture mission active?")]
    public bool missionActive = false;
    
    [Tooltip("Has this platform been captured?")]
    public bool platformCaptured = false;
    
    // Internal state
    private float captureProgress = 0f; // 0 to captureDuration
    private bool playerOnPlatform = false;
    private bool playerInCaptureRadius = false;
    private Transform playerTransform;
    private bool isCapturing = false;
    
    void Start()
    {
        Debug.Log($"[PlatformCaptureSystem] ========== STARTING ON {gameObject.name} ==========");
        
        // Auto-find references if not assigned
        if (centralTower == null)
        {
            centralTower = GetComponentInChildren<CentralTower>();
            Debug.Log($"[PlatformCaptureSystem] Auto-found Central Tower: {(centralTower != null ? centralTower.name : "NOT FOUND")} ");
        }
        else
        {
            Debug.Log($"[PlatformCaptureSystem] Central Tower assigned: {centralTower.name}");
        }
        
        if (platformTrigger == null)
        {
            platformTrigger = GetComponentInChildren<PlatformTrigger>();
            Debug.Log($"[PlatformCaptureSystem] Auto-found Platform Trigger: {(platformTrigger != null ? platformTrigger.name : "NOT FOUND")}");
        }
        else
        {
            Debug.Log($"[PlatformCaptureSystem] Platform Trigger assigned: {platformTrigger.name}");
        }
        
        if (towerSpawner == null)
        {
            towerSpawner = GetComponent<TowerSpawner>();
            Debug.Log($"[PlatformCaptureSystem] Auto-found Tower Spawner: {(towerSpawner != null ? "YES" : "NOT FOUND")}");
        }
        else
        {
            Debug.Log($"[PlatformCaptureSystem] Tower Spawner assigned: YES");
        }
        
        // Calculate drain rate (same as fill rate)
        drainRate = 1f / captureDuration;
        
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            Debug.Log($"[PlatformCaptureSystem] Player found: {playerObj.name}");
        }
        else
        {
            Debug.LogWarning("[PlatformCaptureSystem] ⚠️ PLAYER NOT FOUND! Make sure player has 'Player' tag!");
        }
        
        // Initialize UI
        if (progressUI != null)
        {
            progressUI.Initialize("Capture Platform", captureDuration);
            progressUI.Hide(); // Start hidden
            Debug.Log($"[PlatformCaptureSystem] UI initialized and hidden");
        }
        else
        {
            Debug.LogWarning("[PlatformCaptureSystem] ⚠️ NO UI ASSIGNED! Assign PlatformCaptureUI in Inspector!");
        }
        
        // Check mission state
        if (missionActive)
        {
            Debug.Log($"[PlatformCaptureSystem] ✅ MISSION ACTIVE - Capture duration: {captureDuration}s");
        }
        else
        {
            Debug.LogWarning($"[PlatformCaptureSystem] ⚠️ MISSION INACTIVE - Check 'Mission Active' in Inspector to enable!");
        }
        
        // Start progress logging coroutine
        StartCoroutine(ProgressLoggingCoroutine());
        
        Debug.Log($"[PlatformCaptureSystem] ========== INITIALIZATION COMPLETE ==========");
    }
    
    void Update()
    {
        if (platformCaptured || !missionActive) return;
        
        // Check if player is on platform
        playerOnPlatform = platformTrigger != null && platformTrigger.IsPlayerOnThisPlatform();
        
        // Show/hide UI based on platform presence
        if (progressUI != null)
        {
            if (playerOnPlatform && !progressUI.IsVisible())
            {
                progressUI.Show();
                Debug.Log("[PlatformCaptureSystem] 📺 UI SHOWN - Player entered platform");
                
                // Show cube health if cube exists and is not dead
                if (towerProtectorCube != null && !towerProtectorCube.isFriendly)
                {
                    progressUI.ShowCubeHealth();
                }
            }
            else if (!playerOnPlatform && progressUI.IsVisible())
            {
                progressUI.Hide();
                progressUI.HideCubeHealth();
                ResetCaptureProgress(); // Reset when player leaves platform
                Debug.Log("[PlatformCaptureSystem] 📺 UI HIDDEN - Player left platform");
            }
        }
        
        // Only check capture radius if player is on platform
        if (playerOnPlatform && playerTransform != null && centralTower != null)
        {
            playerInCaptureRadius = centralTower.IsPositionInCaptureRadius(playerTransform.position);
            
            // Update capture progress
            if (playerInCaptureRadius)
            {
                // Player in radius - increase progress
                captureProgress += Time.deltaTime;
                
                if (!isCapturing)
                {
                    isCapturing = true;
                    centralTower.OnCaptureStarted();
                    
                    // Play capture start sound
                    if (soundEvents != null && soundEvents.platformCaptureStart != null)
                    {
                        soundEvents.platformCaptureStart.Play3D(centralTower.transform.position);
                        Debug.Log("[PlatformCaptureSystem] 🔊 Playing capture START sound");
                    }
                    
                    Debug.Log("[PlatformCaptureSystem] 🎯 CAPTURE STARTED - Player in radius!");
                }
                
                // Check if capture complete
                if (captureProgress >= captureDuration)
                {
                    captureProgress = captureDuration;
                    CompleteCaptureSequence();
                }
            }
            else
            {
                // Player outside radius - decrease progress
                if (captureProgress > 0f)
                {
                    captureProgress -= Time.deltaTime * drainRate;
                    captureProgress = Mathf.Max(0f, captureProgress);
                    
                    if (isCapturing)
                    {
                        isCapturing = false;
                        centralTower.OnCaptureCancelled();
                        Debug.Log("[PlatformCaptureSystem] ⚠️ CAPTURE PAUSED - Player left radius!");
                    }
                }
            }
            
            // Update UI
            if (progressUI != null)
            {
                float progressPercent = captureProgress / captureDuration;
                progressUI.UpdateProgress(progressPercent);
            }
            
            // Update lighting based on progress
            if (lightingController != null)
            {
                float progressPercent = captureProgress / captureDuration;
                lightingController.UpdateCaptureProgress(progressPercent);
            }
        }
        
        // Update cube health UI if cube exists, is hostile, and player is on platform
        if (progressUI != null && towerProtectorCube != null && !towerProtectorCube.isFriendly && playerOnPlatform)
        {
            float cubeHealthPercent = towerProtectorCube.GetHealthPercent();
            progressUI.UpdateCubeHealth(cubeHealthPercent, false);
        }
    }
    
    /// <summary>
    /// Coroutine that logs progress every 10 seconds
    /// </summary>
    private System.Collections.IEnumerator ProgressLoggingCoroutine()
    {
        float nextLogTime = 10f;
        
        while (!platformCaptured)
        {
            yield return new WaitForSeconds(10f);
            
            if (missionActive)
            {
                float progressPercent = (captureProgress / captureDuration) * 100f;
                Debug.Log($"[PlatformCaptureSystem] ⏱️ {nextLogTime}s - Progress: {progressPercent:F1}% | Player on platform: {playerOnPlatform} | In radius: {playerInCaptureRadius}");
                nextLogTime += 10f;
            }
            else
            {
                Debug.Log($"[PlatformCaptureSystem] ⏱️ {nextLogTime}s - Mission inactive");
                nextLogTime += 10f;
            }
        }
    }
    
    /// <summary>
    /// Activate the capture mission
    /// </summary>
    public void ActivateMission()
    {
        if (platformCaptured)
        {
            Debug.LogWarning("[PlatformCaptureSystem] Platform already captured!");
            return;
        }
        
        missionActive = true;
        captureProgress = 0f;
        Debug.Log("[PlatformCaptureSystem] Mission activated!");
    }
    
    /// <summary>
    /// Reset capture progress to zero
    /// </summary>
    public void ResetCaptureProgress()
    {
        captureProgress = 0f;
        isCapturing = false;
        
        if (centralTower != null)
        {
            centralTower.OnCaptureCancelled();
        }
        
        Debug.Log("[PlatformCaptureSystem] Capture progress reset!");
    }
    
    /// <summary>
    /// Complete the capture sequence - kill all enemies, destroy towers, spawn gems
    /// </summary>
    private void CompleteCaptureSequence()
    {
        if (platformCaptured) return;
        
        platformCaptured = true;
        missionActive = false;
        
        Debug.Log("🎉 [PlatformCaptureSystem] PLATFORM CAPTURED! Starting completion sequence...");
        
        StartCoroutine(CaptureCompleteSequence());
    }
    
    /// <summary>
    /// Capture complete sequence coroutine
    /// </summary>
    private IEnumerator CaptureCompleteSequence()
    {
        // 1. Play capture complete sound
        if (soundEvents != null && soundEvents.platformCaptureComplete != null)
        {
            soundEvents.platformCaptureComplete.Play3D(centralTower.transform.position);
            Debug.Log("[PlatformCaptureSystem] 🔊 Playing capture COMPLETE sound");
        }
        
        // 2. Visual feedback on Central Tower
        if (centralTower != null)
        {
            centralTower.OnPlatformCaptured();
        }
        
        // 3. Trigger victory lighting sequence
        if (lightingController != null)
        {
            lightingController.OnCaptureComplete();
            Debug.Log("[PlatformCaptureSystem] 💡 Victory lighting sequence triggered!");
        }
        
        // 4. Activate particle effects
        if (captureCompleteParticles != null)
        {
            captureCompleteParticles.SetActive(true);
            Debug.Log("[PlatformCaptureSystem] ✨ Activated capture complete particles!");
        }
        
        // 5. Hide progress UI
        if (progressUI != null)
        {
            progressUI.Hide();
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // 3. Kill all SkullEnemy instances in the scene
        Debug.Log("[PlatformCaptureSystem] Destroying all SkullEnemy instances...");
        SkullEnemy[] skulls = FindObjectsOfType<SkullEnemy>();
        foreach (SkullEnemy skull in skulls)
        {
            if (skull != null)
            {
                Destroy(skull.gameObject);
            }
        }
        
        // 4. Kill all Gem instances (they decouple from towers)
        Debug.Log("[PlatformCaptureSystem] Destroying all Gem instances...");
        Gem[] gems = FindObjectsOfType<Gem>();
        foreach (Gem gem in gems)
        {
            if (gem != null)
            {
                Destroy(gem.gameObject);
            }
        }
        
        yield return new WaitForSeconds(0.3f);
        
        // 5. Kill all regular towers (make them sink into ground)
        Debug.Log("[PlatformCaptureSystem] Destroying all towers...");
        TowerController[] towers = FindObjectsOfType<TowerController>();
        foreach (TowerController tower in towers)
        {
            if (tower != null && !tower.IsDead)
            {
                // Trigger tower death sequence (sinks into ground)
                tower.StartCollapseSequence();
            }
        }
        
        // 6. Make Tower Protector Cube friendly if it's still alive
        if (towerProtectorCube != null && !towerProtectorCube.isFriendly)
        {
            towerProtectorCube.MakeFriendly();
            Debug.Log("[PlatformCaptureSystem] 💚 Tower Protector Cube is now FRIENDLY!");
            
            // Hide health slider - no need to track friendly cube health
            if (progressUI != null)
            {
                progressUI.HideCubeHealth();
                Debug.Log("[PlatformCaptureSystem] 🎨 Cube health slider hidden (cube is friendly)");
            }
        }
        else if (towerProtectorCube == null)
        {
            // Cube was destroyed - hide health UI
            if (progressUI != null)
            {
                progressUI.HideCubeHealth();
            }
        }
        
        // 7. Disable continuous spawning on this platform
        if (towerSpawner != null)
        {
            towerSpawner.enableContinuousSpawning = false;
            Debug.Log("[PlatformCaptureSystem] Disabled continuous tower spawning");
        }
        
        yield return new WaitForSeconds(1f);
        
        // 8. Central Tower spawns reward gems
        if (centralTower != null)
        {
            centralTower.SpawnCaptureRewardGems();
        }
        
        // 9. Grant XP to player (TODO: Integrate with your XP system)
        // GeminiGauntlet.Progression.XPHooks.OnPlatformCaptured(2500);
        
        Debug.Log("✅ [PlatformCaptureSystem] Platform capture complete! Gems spawned, enemies cleared!");
    }
    
    /// <summary>
    /// Called when player dies - reset progress
    /// </summary>
    public void OnPlayerDeath()
    {
        if (!platformCaptured)
        {
            ResetCaptureProgress();
            Debug.Log("[PlatformCaptureSystem] Player died - capture progress reset!");
        }
    }
    
    // Debug visualization
    void OnDrawGizmos()
    {
        if (centralTower != null)
        {
            // Draw line from system to central tower
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, centralTower.transform.position);
        }
        
        if (towerProtectorCube != null)
        {
            // Draw line to tower protector cube
            Gizmos.color = towerProtectorCube.isFriendly ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, towerProtectorCube.transform.position);
        }
    }
}
