using UnityEngine;
using System.Collections;
using GeminiGauntlet.Audio;
using GeminiGauntlet.UI;

/// <summary>
/// Tower Protector Cube - A deadly laser-shooting guardian that protects capture points
/// Tracks player, shoots laser beams, and becomes friendly if platform is captured while alive
/// </summary>
public class SkullSpawnerCube : MonoBehaviour, IDamageable
{
    [Header("Health System")]
    [Tooltip("Maximum health of the tower protector")]
    [SerializeField] private float maxHealth = 1000f;
    
    [Tooltip("Current health")]
    private float currentHealth;
    
    [Header("Laser Attack")]
    [Tooltip("Time between laser attacks in seconds")]
    [SerializeField] private float laserInterval = 15f;
    
    [Tooltip("Duration of laser beam in seconds")]
    [SerializeField] private float laserDuration = 5f;
    
    [Tooltip("Damage per second dealt by laser")]
    [SerializeField] private float laserDamagePerSecond = 50f;
    
    [Tooltip("Speed at which cube tracks player (degrees per second)")]
    [SerializeField] private float trackingSpeed = 45f;
    
    [Tooltip("How far ahead to aim (prediction factor, 0 = no prediction, 1 = full prediction)")]
    [SerializeField] private float aimPrediction = 0.3f;
    
    [Tooltip("Beam prefab with LineRenderer (assign Arcane Beam or similar)")]
    [SerializeField] private GameObject beamPrefab;
    
    [Tooltip("Maximum laser range")]
    [SerializeField] private float laserMaxRange = 2000f;
    
    [Header("Visual Settings")]
    [Tooltip("Normal rotation speed (degrees per second)")]
    [SerializeField] private float normalRotationSpeed = 30f;
    
    [Tooltip("Fast rotation speed when tracking (degrees per second)")]
    [SerializeField] private float trackingRotationSpeed = 90f;
    
    [Tooltip("Glow color when idle/hostile")]
    [SerializeField] private Color hostileGlowColor = new Color(1f, 0.2f, 0.2f, 1f); // Red
    
    [Tooltip("Glow color when friendly")]
    [SerializeField] private Color friendlyGlowColor = new Color(0.2f, 1f, 0.2f, 1f); // Green
    
    [Tooltip("Glow color when charging laser")]
    [SerializeField] private Color chargingGlowColor = new Color(1f, 0.5f, 0f, 1f); // Orange
    
    [Tooltip("Glow color when firing laser")]
    [SerializeField] private Color firingGlowColor = new Color(1f, 1f, 0f, 1f); // Yellow
    
    [Tooltip("Glow intensity multiplier")]
    [SerializeField] private float glowIntensity = 2f;
    
    [Tooltip("Laser beam color")]
    [SerializeField] private Color laserColor = new Color(1f, 0.2f, 0.2f, 1f); // Red
    
    [Header("Audio")]
    [Tooltip("Sound Events ScriptableObject")]
    public SoundEvents soundEvents;
    
    [Header("State")]
    [Tooltip("Is this cube friendly? (Set by PlatformCaptureSystem)")]
    public bool isFriendly = false;
    
    [Tooltip("Auto-start laser attacks when scene starts")]
    [SerializeField] private bool autoStart = true;
    
    // Internal state
    private float currentRotationSpeed;
    private Material cubeMaterial;
    private Renderer cubeRenderer;
    private Transform playerTransform;
    private bool isDead = false;
    
    // Laser state
    private bool isFiringLaser = false;
    private bool isTrackingPlayer = false;
    private float nextLaserTime = 0f;
    private SoundHandle laserSoundHandle;
    private GameObject activeBeamInstance;
    private LineRenderer beamLineRenderer;
    
    // Rotation axes for dynamic movement
    private Vector3 rotationAxis = Vector3.up;
    private float axisChangeTimer = 0f;
    private float axisChangeDuration = 5f;
    
    void Start()
    {
        currentHealth = maxHealth;
        currentRotationSpeed = normalRotationSpeed;
        
        // Ensure we have a collider for damage detection
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            // Add a box collider if none exists
            BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
            Debug.Log("[TowerProtector] ⚠️ Added BoxCollider for damage detection");
        }
        
        // Log initialization info
        Debug.Log($"[TowerProtector] ✅ Initialized - Health: {currentHealth}/{maxHealth}, Layer: {LayerMask.LayerToName(gameObject.layer)}, Has Collider: {col != null}, Implements IDamageable: {this is IDamageable}");
        
        // Get or create material for glow effect
        cubeRenderer = GetComponent<Renderer>();
        if (cubeRenderer != null)
        {
            // Create a unique material instance
            cubeMaterial = cubeRenderer.material;
            
            // Enable emission
            cubeMaterial.EnableKeyword("_EMISSION");
            
            // Set initial glow
            SetGlowColor(hostileGlowColor);
        }
        
        // Verify beam prefab is assigned
        if (beamPrefab == null)
        {
            Debug.LogWarning("[TowerProtector] ⚠️ No beam prefab assigned! Assign Arcane Beam in Inspector for visual effects.");
        }
        
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        
        // Start laser cycle if auto-start enabled
        if (autoStart)
        {
            nextLaserTime = Time.time + laserInterval;
            StartCoroutine(LaserAttackCycle());
        }
        
        Debug.Log($"[TowerProtector] Initialized - Health: {maxHealth}, Laser interval: {laserInterval}s, Duration: {laserDuration}s");
    }
    
    void Update()
    {
        if (isDead) return;
        
        // Dynamic rotation with smooth axis changes
        UpdateRotationAxis();
        
        // Rotate the cube (faster when tracking)
        float rotSpeed = isTrackingPlayer ? trackingRotationSpeed : currentRotationSpeed;
        transform.Rotate(rotationAxis, rotSpeed * Time.deltaTime, Space.World);
        
        // Add a subtle wobble for extra visual interest
        float wobble = Mathf.Sin(Time.time * 2f) * 5f;
        transform.Rotate(Vector3.right, wobble * Time.deltaTime, Space.Self);
        
        // Track player when preparing to fire
        if (isTrackingPlayer && playerTransform != null && !isFriendly)
        {
            TrackPlayer();
        }
        
        // Pulsing glow effect
        if (cubeMaterial != null && !isFiringLaser && !isTrackingPlayer)
        {
            float pulse = Mathf.PingPong(Time.time * 0.5f, 0.3f);
            Color baseColor = isFriendly ? friendlyGlowColor : hostileGlowColor;
            Color glowColor = baseColor * (glowIntensity + pulse);
            cubeMaterial.SetColor("_EmissionColor", glowColor);
        }
    }
    
    /// <summary>
    /// Smoothly change rotation axis for dynamic movement
    /// </summary>
    private void UpdateRotationAxis()
    {
        axisChangeTimer += Time.deltaTime;
        
        if (axisChangeTimer >= axisChangeDuration)
        {
            axisChangeTimer = 0f;
            
            // Pick a new random rotation axis (biased toward Y for stability)
            float rand = Random.value;
            if (rand < 0.5f)
            {
                rotationAxis = Vector3.up; // 50% chance - Y axis
            }
            else if (rand < 0.75f)
            {
                rotationAxis = new Vector3(0.3f, 1f, 0.3f).normalized; // 25% chance - diagonal
            }
            else
            {
                rotationAxis = new Vector3(0.5f, 1f, 0f).normalized; // 25% chance - other diagonal
            }
        }
    }
    
    /// <summary>
    /// Main laser attack cycle coroutine
    /// </summary>
    private IEnumerator LaserAttackCycle()
    {
        // Initial delay before first attack
        yield return new WaitForSeconds(5f);
        
        while (!isDead)
        {
            // Skip if friendly
            if (isFriendly)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }
            
            // Wait for next laser time
            while (Time.time < nextLaserTime)
            {
                yield return null;
            }
            
            // Execute laser attack
            yield return StartCoroutine(LaserAttackSequence());
            
            // Schedule next attack
            nextLaserTime = Time.time + laserInterval;
        }
    }
    
    /// <summary>
    /// Execute full laser attack sequence: track -> charge -> fire
    /// </summary>
    private IEnumerator LaserAttackSequence()
    {
        if (playerTransform == null || isFriendly || isDead)
        {
            yield break;
        }
        
        Debug.Log($"[TowerProtector] 🎯 Starting laser attack sequence!");
        
        // Phase 1: Track player (2 seconds warning)
        isTrackingPlayer = true;
        SetGlowColor(chargingGlowColor);
        yield return new WaitForSeconds(2f);
        
        // Phase 2: Fire laser
        yield return StartCoroutine(FireLaser());
        
        // Return to normal
        isTrackingPlayer = false;
        SetGlowColor(hostileGlowColor);
        
        Debug.Log("[TowerProtector] ✅ Laser attack complete!");
    }
    
    /// <summary>
    /// Fire laser beam at player for specified duration
    /// </summary>
    private IEnumerator FireLaser()
    {
        if (playerTransform == null || isFriendly || isDead)
        {
            yield break;
        }
        
        isFiringLaser = true;
        SetGlowColor(firingGlowColor);
        
        // Play laser sound
        if (soundEvents != null && soundEvents.towerLaserShoot != null)
        {
            laserSoundHandle = soundEvents.towerLaserShoot.PlayAttached(transform);
            Debug.Log("[TowerProtector] 🔊 Playing laser sound");
        }
        
        // Spawn beam visual
        if (beamPrefab != null)
        {
            activeBeamInstance = Instantiate(beamPrefab, transform.position, Quaternion.identity, transform);
            beamLineRenderer = activeBeamInstance.GetComponent<LineRenderer>();
            
            if (beamLineRenderer != null)
            {
                beamLineRenderer.positionCount = 2;
                beamLineRenderer.useWorldSpace = true;
                
                // Make beam PHAT - 5x thicker!
                float originalWidth = beamLineRenderer.startWidth;
                beamLineRenderer.startWidth = originalWidth * 5f;
                beamLineRenderer.endWidth = originalWidth * 5f;
                
                Debug.Log($"[TowerProtector] ✨ PHAT beam spawned! Width: {beamLineRenderer.startWidth} (5x original)");
            }
            else
            {
                Debug.LogWarning("[TowerProtector] ⚠️ Beam prefab has no LineRenderer component!");
            }
        }
        
        float elapsed = 0f;
        PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
        
        while (elapsed < laserDuration && !isDead && !isFriendly)
        {
            elapsed += Time.deltaTime;
            
            // Update laser targeting and damage
            if (playerTransform != null)
            {
                Vector3 startPos = transform.position;
                Vector3 targetPos = GetPredictedPlayerPosition();
                Vector3 direction = (targetPos - startPos).normalized;
                
                // Update LineRenderer positions
                if (beamLineRenderer != null)
                {
                    beamLineRenderer.SetPosition(0, startPos);
                    
                    // Raycast to find end point
                    RaycastHit hit;
                    Vector3 endPos;
                    
                    if (Physics.Raycast(startPos, direction, out hit, laserMaxRange))
                    {
                        endPos = hit.point;
                        
                        // Deal damage if hit player
                        if (hit.collider.CompareTag("Player"))
                        {
                            float damage = laserDamagePerSecond * Time.deltaTime;
                            
                            // Try to damage player using IDamageable interface
                            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                            if (damageable != null)
                            {
                                damageable.TakeDamage(damage, hit.point, direction);
                            }
                            else if (playerHealth != null)
                            {
                                // Fallback to PlayerHealth component
                                playerHealth.TakeDamage(damage, hit.point, direction);
                            }
                        }
                    }
                    else
                    {
                        endPos = startPos + direction * laserMaxRange;
                    }
                    
                    beamLineRenderer.SetPosition(1, endPos);
                }
            }
            
            yield return null;
        }
        
        // Stop and destroy beam
        if (activeBeamInstance != null)
        {
            Destroy(activeBeamInstance);
            activeBeamInstance = null;
            beamLineRenderer = null;
        }
        
        // Stop laser sound
        if (laserSoundHandle.IsValid)
        {
            laserSoundHandle.Stop();
        }
        
        isFiringLaser = false;
    }
    
    /// <summary>
    /// Smoothly rotate to face player
    /// </summary>
    private void TrackPlayer()
    {
        if (playerTransform == null) return;
        
        Vector3 targetPos = GetPredictedPlayerPosition();
        Vector3 direction = (targetPos - transform.position).normalized;
        
        // Calculate target rotation
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        
        // Smoothly rotate towards target
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            trackingSpeed * Time.deltaTime
        );
    }
    
    /// <summary>
    /// Get predicted player position based on velocity
    /// </summary>
    private Vector3 GetPredictedPlayerPosition()
    {
        if (playerTransform == null) return Vector3.zero;
        
        Vector3 currentPos = playerTransform.position;
        
        // Try to get player velocity for prediction
        Rigidbody playerRb = playerTransform.GetComponent<Rigidbody>();
        if (playerRb != null && aimPrediction > 0f)
        {
            float distance = Vector3.Distance(transform.position, currentPos);
            float timeToReach = distance / 1000f; // Assume laser travels at 1000 units/s
            Vector3 predictedOffset = playerRb.linearVelocity * timeToReach * aimPrediction;
            return currentPos + predictedOffset;
        }
        
        return currentPos;
    }
    
    /// <summary>
    /// Set the glow color with smooth transition
    /// </summary>
    private void SetGlowColor(Color targetColor)
    {
        if (cubeMaterial != null)
        {
            StartCoroutine(SmoothGlowTransition(targetColor));
        }
    }
    
    /// <summary>
    /// Smooth glow color transition
    /// </summary>
    private IEnumerator SmoothGlowTransition(Color targetColor)
    {
        if (cubeMaterial == null) yield break;
        
        Color startColor = cubeMaterial.GetColor("_EmissionColor");
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            Color currentColor = Color.Lerp(startColor, targetColor * glowIntensity, t);
            cubeMaterial.SetColor("_EmissionColor", currentColor);
            
            yield return null;
        }
        
        cubeMaterial.SetColor("_EmissionColor", targetColor * glowIntensity);
    }
    
    /// <summary>
    /// Take damage from player attacks
    /// </summary>
    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        Debug.Log($"[TowerProtector] 🎯 TakeDamage called! Amount: {amount}, IsDead: {isDead}, IsFriendly: {isFriendly}");
        
        if (isDead || isFriendly)
        {
            Debug.Log($"[TowerProtector] ❌ Damage blocked - IsDead: {isDead}, IsFriendly: {isFriendly}");
            return;
        }
        
        currentHealth -= amount;
        currentHealth = Mathf.Max(0f, currentHealth); // Clamp to 0
        
        float healthPercent = GetHealthPercent();
        Debug.Log($"[TowerProtector] 💥 Took {amount} damage! Health: {currentHealth}/{maxHealth} ({healthPercent * 100:F0}%)");
        
        // Show floating damage text
        if (FloatingTextManager.Instance != null)
        {
            Color damageColor = Color.Lerp(Color.yellow, Color.red, 1f - healthPercent);
            FloatingTextManager.Instance.ShowFloatingText(
                $"-{amount:F0}",
                hitPoint,
                damageColor
            );
        }
        
        // Flash effect on hit
        if (cubeMaterial != null)
        {
            StartCoroutine(HitFlashEffect());
        }
        else
        {
            Debug.LogWarning("[TowerProtector] ⚠️ No cubeMaterial for hit flash!");
        }
        
        // Check if dead
        if (currentHealth <= 0f)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Get current health as a percentage (0 to 1)
    /// </summary>
    public float GetHealthPercent()
    {
        return maxHealth > 0f ? currentHealth / maxHealth : 0f;
    }
    
    /// <summary>
    /// Flash red with 2x emission when hit
    /// </summary>
    private IEnumerator HitFlashEffect()
    {
        Color originalColor = cubeMaterial.GetColor("_EmissionColor");
        // Glow bright red with 2x emission intensity
        cubeMaterial.SetColor("_EmissionColor", Color.red * glowIntensity * 2f);
        yield return new WaitForSeconds(0.2f);
        cubeMaterial.SetColor("_EmissionColor", originalColor);
    }
    
    /// <summary>
    /// Handle death
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log("[TowerProtector] 💀 DESTROYED!");
        
        // Stop all attacks
        StopAllCoroutines();
        
        // Disable laser
        if (activeBeamInstance != null)
        {
            Destroy(activeBeamInstance);
            activeBeamInstance = null;
            beamLineRenderer = null;
        }
        
        // Stop laser sound
        if (laserSoundHandle.IsValid)
        {
            laserSoundHandle.Stop();
        }
        
        // Grant XP if XPGranter is attached
        GeminiGauntlet.Progression.XPGranter xpGranter = GetComponent<GeminiGauntlet.Progression.XPGranter>();
        if (xpGranter != null)
        {
            xpGranter.GrantXPManually("TowerProtectorKilled");
            Debug.Log($"[TowerProtector] 💰 XP granted: {xpGranter.XPAmount} ({xpGranter.CategoryName})");
        }
        else
        {
            Debug.LogWarning("[TowerProtector] ⚠️ No XPGranter component found! Assign XPGranter to show XP reward.");
        }
        
        // Death visual effect
        StartCoroutine(DeathSequence());
    }
    
    /// <summary>
    /// Death animation sequence
    /// </summary>
    private IEnumerator DeathSequence()
    {
        // Flash rapidly
        for (int i = 0; i < 5; i++)
        {
            if (cubeMaterial != null)
            {
                cubeMaterial.SetColor("_EmissionColor", Color.white * glowIntensity * 3f);
            }
            yield return new WaitForSeconds(0.1f);
            if (cubeMaterial != null)
            {
                cubeMaterial.SetColor("_EmissionColor", Color.black);
            }
            yield return new WaitForSeconds(0.1f);
        }
        
        // Destroy the cube
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Make this cube friendly (called by PlatformCaptureSystem)
    /// </summary>
    public void MakeFriendly()
    {
        if (isDead) return;
        
        isFriendly = true;
        Debug.Log("[TowerProtector] 💚 Now friendly!");
        
        // Stop any active laser
        if (isFiringLaser)
        {
            if (activeBeamInstance != null)
            {
                Destroy(activeBeamInstance);
                activeBeamInstance = null;
                beamLineRenderer = null;
            }
            if (laserSoundHandle.IsValid)
            {
                laserSoundHandle.Stop();
            }
            isFiringLaser = false;
        }
        
        isTrackingPlayer = false;
        SetGlowColor(friendlyGlowColor);
    }
    
    // Visualize laser range in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isFriendly ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, laserMaxRange * 0.1f);
        
        // Draw forward direction
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * 100f);
    }
}
