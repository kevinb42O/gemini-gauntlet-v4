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
    
    [Tooltip("Maximum range to detect and attack player (0 = infinite range)")]
    [SerializeField] private float detectionRange = 3000f;
    
    [Tooltip("OPTIONAL: Static beam prefab (from Beams/Static Beams folder: FireBeamStatic, etc.) - has LineRenderer")]
    [SerializeField] private GameObject staticBeamPrefab;
    
    [Tooltip("OPTIONAL: Particle effect prefab for beam visual (from Beam Blast folder) - spawns at start/end")]
    [SerializeField] private GameObject beamParticleEffect;
    
    [Tooltip("Transform where laser emits from (if null, uses cube center)")]
    [SerializeField] private Transform laserEmitPoint;
    
    [Tooltip("Maximum laser range")]
    [SerializeField] private float laserMaxRange = 2000f;
    
    [Tooltip("Laser beam width (for built-in LineRenderer)")]
    [SerializeField] private float laserBeamWidth = 10f;
    
    [Tooltip("Laser beam color (red for fire, purple for arcane, etc.)")]
    [SerializeField] private Color laserBeamColor = new Color(1f, 0.3f, 0f, 1f); // Orange/red fire
    
    [Header("Visual Settings")]
    [Tooltip("Normal rotation speed (degrees per second)")]
    [SerializeField] private float normalRotationSpeed = 30f;
    
    [Tooltip("Fast rotation speed when tracking (degrees per second)")]
    [SerializeField] private float trackingRotationSpeed = 90f;
    
    [Tooltip("Hit flash duration in seconds")]
    [SerializeField] private float hitFlashDuration = 0.15f;
    
    [Tooltip("Hit flash emission intensity multiplier")]
    [SerializeField] private float hitFlashIntensity = 5f;
    
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
    private bool laserCycleStarted = false;
    private float playerCheckInterval = 1f;
    private float nextPlayerCheckTime = 0f;
    private Collider towerCollider;
    
    // Laser state
    private bool isFiringLaser = false;
    private bool isTrackingPlayer = false;
    private float nextLaserTime = 0f;
    private SoundHandle laserSoundHandle;
    private GameObject activeBeamInstance;
    private LineRenderer beamLineRenderer;
    private GameObject beamStartEffect;
    private GameObject beamEndEffect;
    
    // Rotation axes for dynamic movement
    private Vector3 rotationAxis = Vector3.up;
    private float axisChangeTimer = 0f;
    private float axisChangeDuration = 5f;
    
    void Start()
    {
        currentHealth = maxHealth;
        currentRotationSpeed = normalRotationSpeed;
        
        // Ensure we have a collider for damage detection
        towerCollider = GetComponent<Collider>();
        if (towerCollider == null)
        {
            // Add a box collider if none exists
            towerCollider = gameObject.AddComponent<BoxCollider>();
            Debug.Log("[TowerProtector] ⚠️ Added BoxCollider for damage detection");
        }
        
        // Log initialization info
        Debug.Log($"[TowerProtector] ✅ Initialized - Health: {currentHealth}/{maxHealth}, Layer: {LayerMask.LayerToName(gameObject.layer)}, Has Collider: {towerCollider != null}, Implements IDamageable: {this is IDamageable}");
        
        // Get or create material for glow effect
        cubeRenderer = GetComponent<Renderer>();
        if (cubeRenderer != null)
        {
            // Create a unique material instance
            cubeMaterial = cubeRenderer.material;
            
            // Enable emission and make sure material supports it
            if (cubeMaterial.HasProperty("_EmissionColor"))
            {
                cubeMaterial.EnableKeyword("_EMISSION");
                
                // Set initial glow
                SetGlowColor(hostileGlowColor);
                Debug.Log("[TowerProtector] ✅ Emission enabled on material");
            }
            else
            {
                Debug.LogWarning("[TowerProtector] ⚠️ Material doesn't support emission! Use a Standard or URP/Lit material.");
            }
        }
        else
        {
            Debug.LogWarning("[TowerProtector] ⚠️ No Renderer component found!");
        }
        
        // Info about particle effects (optional)
        if (staticBeamPrefab != null)
        {
            Debug.Log($"[TowerProtector] ✅ Static beam prefab assigned: {staticBeamPrefab.name}");
        }
        else
        {
            Debug.Log("[TowerProtector] ℹ️ No static beam prefab assigned - will use built-in LineRenderer beam");
        }
        
        if (beamParticleEffect == null)
        {
            Debug.Log("[TowerProtector] ℹ️ No particle effect assigned - using beam only");
        }
        else
        {
            Debug.Log($"[TowerProtector] ✅ Particle effect assigned: {beamParticleEffect.name}");
        }
        
        // Verify laser emit point (auto-create if missing)
        if (laserEmitPoint == null)
        {
            Debug.Log("[TowerProtector] ℹ️ No laser emit point assigned, using cube center. Assign a child Transform for offset emission!");
        }
        
        // Find player
        FindPlayer();
        
        // Start laser cycle if auto-start enabled
        if (autoStart)
        {
            StartLaserCycle();
        }
        else
        {
            Debug.Log("[TowerProtector] ⚠️ Auto-start DISABLED - laser will not attack automatically. Use context menu 'Test/Start Laser Attack' to trigger.");
        }
        
        Debug.Log($"[TowerProtector] Initialized - Health: {maxHealth}, Laser interval: {laserInterval}s, Duration: {laserDuration}s, Detection range: {detectionRange}, Player found: {playerTransform != null}");
    }
    
    void Update()
    {
        if (isDead) return;
        
        // Periodically check for player if not found
        if (playerTransform == null && Time.time >= nextPlayerCheckTime)
        {
            FindPlayer();
            nextPlayerCheckTime = Time.time + playerCheckInterval;
        }
        
        // Auto-start laser cycle if player is detected and in range (and not started yet)
        if (!laserCycleStarted && playerTransform != null && !isFriendly && IsPlayerInRange())
        {
            Debug.Log("[TowerProtector] 🎯 Player detected in range! Starting laser attack cycle...");
            StartLaserCycle();
        }
        
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
    /// Find player in scene
    /// </summary>
    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            Debug.Log($"[TowerProtector] ✅ Player found: {playerObj.name} at {playerObj.transform.position}");
        }
        else
        {
            Debug.LogWarning("[TowerProtector] ⚠️ No GameObject with 'Player' tag found! Make sure player has 'Player' tag assigned.");
        }
    }
    
    /// <summary>
    /// Check if player is within detection range
    /// </summary>
    private bool IsPlayerInRange()
    {
        if (playerTransform == null) return false;
        if (detectionRange <= 0f) return true; // Infinite range
        
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        return distance <= detectionRange;
    }
    
    /// <summary>
    /// Start the laser attack cycle
    /// </summary>
    private void StartLaserCycle()
    {
        if (laserCycleStarted)
        {
            Debug.LogWarning("[TowerProtector] ⚠️ Laser cycle already started!");
            return;
        }
        
        laserCycleStarted = true;
        nextLaserTime = Time.time + laserInterval;
        StartCoroutine(LaserAttackCycle());
        Debug.Log($"[TowerProtector] 🚀 Laser attack cycle STARTED! First attack in {laserInterval}s");
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
        Debug.Log("[TowerProtector] 🔄 Laser attack cycle coroutine started!");
        
        // Initial delay before first attack
        float initialDelay = 5f;
        Debug.Log($"[TowerProtector] ⏳ Waiting {initialDelay}s before first attack...");
        yield return new WaitForSeconds(initialDelay);
        
        while (!isDead)
        {
            // Skip if friendly
            if (isFriendly)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }
            
            // Skip if player not in range
            if (!IsPlayerInRange())
            {
                Debug.Log("[TowerProtector] ⚠️ Player out of range, waiting...");
                yield return new WaitForSeconds(2f);
                continue;
            }
            
            // Wait for next laser time
            float waitTime = Mathf.Max(0f, nextLaserTime - Time.time);
            if (waitTime > 0f)
            {
                Debug.Log($"[TowerProtector] ⏳ Waiting {waitTime:F1}s until next attack...");
                while (Time.time < nextLaserTime)
                {
                    yield return null;
                }
            }
            
            Debug.Log($"[TowerProtector] 🎯 EXECUTING LASER ATTACK! Player: {playerTransform?.name}, Distance: {(playerTransform != null ? Vector3.Distance(transform.position, playerTransform.position) : 0f):F0}");
            
            // Execute laser attack
            yield return StartCoroutine(LaserAttackSequence());
            
            // Schedule next attack
            nextLaserTime = Time.time + laserInterval;
            Debug.Log($"[TowerProtector] ✅ Attack complete. Next attack in {laserInterval}s");
        }
        
        Debug.Log("[TowerProtector] 💀 Laser attack cycle ended (dead)");
    }
    
    /// <summary>
    /// Execute full laser attack sequence: track -> charge -> fire
    /// </summary>
    private IEnumerator LaserAttackSequence()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("[TowerProtector] ❌ Cannot attack - player transform is NULL!");
            yield break;
        }
        
        if (isFriendly)
        {
            Debug.Log("[TowerProtector] ❌ Cannot attack - friendly mode enabled!");
            yield break;
        }
        
        if (isDead)
        {
            Debug.Log("[TowerProtector] ❌ Cannot attack - dead!");
            yield break;
        }
        
        Debug.Log($"[TowerProtector] 🎯 Starting laser attack sequence! Target: {playerTransform.name}");
        
        // Phase 1: Track player (2 seconds warning)
        Debug.Log("[TowerProtector] 📡 Phase 1: TRACKING player...");
        isTrackingPlayer = true;
        SetGlowColor(chargingGlowColor);
        yield return new WaitForSeconds(2f);
        
        // Phase 2: Fire laser
        Debug.Log("[TowerProtector] 🔥 Phase 2: FIRING laser...");
        yield return StartCoroutine(FireLaser());
        
        // Return to normal
        isTrackingPlayer = false;
        SetGlowColor(hostileGlowColor);
        
        Debug.Log("[TowerProtector] ✅ Laser attack sequence complete!");
    }
    
    /// <summary>
    /// Fire laser beam at player for specified duration
    /// </summary>
    private IEnumerator FireLaser()
    {
        if (playerTransform == null || isFriendly || isDead)
        {
            Debug.LogWarning($"[TowerProtector] ❌ FireLaser aborted! Player: {playerTransform != null}, Friendly: {isFriendly}, Dead: {isDead}");
            yield break;
        }
        
        isFiringLaser = true;
        SetGlowColor(firingGlowColor);
        
        Vector3 emitPosition = laserEmitPoint != null ? laserEmitPoint.position : transform.position;
        
        Debug.Log($"[TowerProtector] 🔥 FIRING LASER! Duration: {laserDuration}s, Static beam: {staticBeamPrefab != null}");
        
        // Play laser sound
        if (soundEvents != null && soundEvents.towerLaserShoot != null)
        {
            laserSoundHandle = soundEvents.towerLaserShoot.PlayAttached(transform);
            Debug.Log("[TowerProtector] 🔊 Playing laser sound");
        }
        else
        {
            Debug.LogWarning("[TowerProtector] ⚠️ No sound events or towerLaserShoot clip assigned!");
        }
        
        // Use Static Beam prefab if assigned, otherwise create built-in LineRenderer
        if (staticBeamPrefab != null)
        {
            Debug.Log($"[TowerProtector] 🎨 Spawning static beam prefab: {staticBeamPrefab.name}");
            
            activeBeamInstance = Instantiate(staticBeamPrefab, emitPosition, Quaternion.identity);
            activeBeamInstance.transform.SetParent(laserEmitPoint != null ? laserEmitPoint : transform);
            
            // Find LineRenderer (should be on root or in children)
            beamLineRenderer = activeBeamInstance.GetComponent<LineRenderer>();
            if (beamLineRenderer == null)
            {
                beamLineRenderer = activeBeamInstance.GetComponentInChildren<LineRenderer>();
            }
            
            if (beamLineRenderer != null)
            {
                beamLineRenderer.positionCount = 2;
                beamLineRenderer.useWorldSpace = true;
                
                // Make beam thicker
                float originalWidth = beamLineRenderer.startWidth;
                beamLineRenderer.startWidth = originalWidth * 3f;
                beamLineRenderer.endWidth = originalWidth * 3f;
                
                Debug.Log($"[TowerProtector] ✅ Static beam configured! Width: {beamLineRenderer.startWidth:F2} (3x original), LineRenderer found: True");
            }
            else
            {
                Debug.LogError($"[TowerProtector] ❌ Static beam prefab has no LineRenderer! Falling back to built-in beam.");
                Destroy(activeBeamInstance);
                activeBeamInstance = null;
            }
        }
        
        // Fallback: Create built-in LineRenderer if no static beam or static beam failed
        if (beamLineRenderer == null)
        {
            Debug.Log("[TowerProtector] 🔧 Creating built-in LineRenderer beam...");
            
            activeBeamInstance = new GameObject("LaserBeam");
            activeBeamInstance.transform.SetParent(laserEmitPoint != null ? laserEmitPoint : transform);
            activeBeamInstance.transform.localPosition = Vector3.zero;
            
            beamLineRenderer = activeBeamInstance.AddComponent<LineRenderer>();
            beamLineRenderer.positionCount = 2;
            beamLineRenderer.useWorldSpace = true;
            beamLineRenderer.startWidth = laserBeamWidth;
            beamLineRenderer.endWidth = laserBeamWidth;
            
            // Set material (create simple emissive material)
            Material beamMat = new Material(Shader.Find("Sprites/Default"));
            beamMat.color = laserBeamColor;
            beamLineRenderer.material = beamMat;
            beamLineRenderer.startColor = laserBeamColor;
            beamLineRenderer.endColor = laserBeamColor;
            
            // Add glow effect if URP
            beamLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            beamLineRenderer.receiveShadows = false;
            
            Debug.Log($"[TowerProtector] ✅ Built-in LineRenderer beam created! Width: {laserBeamWidth}, Color: {laserBeamColor}");
        }
        
        // Spawn optional particle effects at beam start/end
        if (beamParticleEffect != null)
        {
            beamStartEffect = Instantiate(beamParticleEffect, emitPosition, Quaternion.identity);
            Debug.Log($"[TowerProtector] ✨ Spawned particle effect at beam start");
        }
        
        float elapsed = 0f;
        PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
        int frameCount = 0;
        
        Debug.Log($"[TowerProtector] 🔄 Entering laser firing loop for {laserDuration}s, BeamLineRenderer: {beamLineRenderer != null}");
        
        while (elapsed < laserDuration && !isDead && !isFriendly)
        {
            elapsed += Time.deltaTime;
            frameCount++;
            
            // Update laser targeting and damage
            if (playerTransform != null)
            {
                Vector3 startPos = laserEmitPoint != null ? laserEmitPoint.position : transform.position;
                Vector3 targetPos = GetPredictedPlayerPosition();
                Vector3 direction = (targetPos - startPos).normalized;
                
                // Debug every 30 frames
                if (frameCount % 30 == 0)
                {
                    Debug.Log($"[TowerProtector] 🎯 Laser update: Time {elapsed:F1}/{laserDuration}s, Start: {startPos}, Target: {targetPos}, LineRenderer: {beamLineRenderer != null}");
                }
                
                // Update LineRenderer positions
                if (beamLineRenderer != null)
                {
                    beamLineRenderer.SetPosition(0, startPos);
                    
                    // Raycast to find end point - IGNORE the tower's own collider!
                    RaycastHit hit;
                    Vector3 endPos;
                    
                    // Start raycast slightly forward to avoid hitting self
                    Vector3 rayOrigin = startPos + direction * 5f; // Start 5 units forward
                    float adjustedRange = laserMaxRange - 5f;
                    
                    if (Physics.Raycast(rayOrigin, direction, out hit, adjustedRange))
                    {
                        // Double-check we didn't hit ourselves (in case collider is huge)
                        if (hit.collider == towerCollider)
                        {
                            // Ignore self-hit, trace again from further out
                            rayOrigin = startPos + direction * 20f;
                            adjustedRange = laserMaxRange - 20f;
                            
                            if (!Physics.Raycast(rayOrigin, direction, out hit, adjustedRange))
                            {
                                // No hit, max range
                                endPos = startPos + direction * laserMaxRange;
                                if (frameCount == 1)
                                {
                                    Debug.Log($"[TowerProtector] ⚠️ Raycast MISSED (after ignoring self) - no hit within {laserMaxRange:F0} units");
                                }
                            }
                            else
                            {
                                endPos = hit.point;
                            }
                        }
                        else
                        {
                            endPos = hit.point;
                        }
                        
                        // Debug on first hit
                        if (frameCount == 1)
                        {
                            Debug.Log($"[TowerProtector] 🎯 Raycast HIT: {hit.collider.name} (Tag: {hit.collider.tag}) at distance {hit.distance:F0}");
                        }
                        
                        // Update end particle effect position if exists
                        if (beamEndEffect != null)
                        {
                            beamEndEffect.transform.position = endPos;
                        }
                        
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
                            
                            if (frameCount % 60 == 0)
                            {
                                Debug.Log($"[TowerProtector] 💥 Damaging player: {damage:F1} HP");
                            }
                        }
                    }
                    else
                    {
                        endPos = startPos + direction * laserMaxRange;
                        
                        if (frameCount == 1)
                        {
                            Debug.Log($"[TowerProtector] ⚠️ Raycast MISSED - no hit within {laserMaxRange:F0} units");
                        }
                        
                        // Update end particle effect position if exists
                        if (beamEndEffect != null)
                        {
                            beamEndEffect.transform.position = endPos;
                        }
                    }
                    
                    beamLineRenderer.SetPosition(1, endPos);
                    
                    // Update start particle effect position
                    if (beamStartEffect != null)
                    {
                        beamStartEffect.transform.position = startPos;
                    }
                }
            }
            
            yield return null;
        }
        
        Debug.Log($"[TowerProtector] ✅ Laser firing loop completed after {elapsed:F1}s ({frameCount} frames)");
        
        // Stop and destroy beam
        if (activeBeamInstance != null)
        {
            Debug.Log($"[TowerProtector] 🗑️ Destroying beam instance: {activeBeamInstance.name}");
            Destroy(activeBeamInstance);
            activeBeamInstance = null;
            beamLineRenderer = null;
        }
        
        // Destroy particle effects
        if (beamStartEffect != null)
        {
            Destroy(beamStartEffect);
            beamStartEffect = null;
        }
        if (beamEndEffect != null)
        {
            Destroy(beamEndEffect);
            beamEndEffect = null;
        }
        
        // Stop laser sound
        if (laserSoundHandle.IsValid)
        {
            laserSoundHandle.Stop();
            Debug.Log("[TowerProtector] 🔇 Laser sound stopped");
        }
        
        isFiringLaser = false;
        Debug.Log("[TowerProtector] ✅ FireLaser() complete!");
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
        
        // Target player's CENTER MASS, not their feet!
        // For a 320-unit tall character, add ~160 units to Y (half height)
        Vector3 currentPos = playerTransform.position;
        Vector3 centerMassOffset = Vector3.up * 160f; // Adjust this if your character height is different
        Vector3 targetPos = currentPos + centerMassOffset;
        
        // Try to get player velocity for prediction
        Rigidbody playerRb = playerTransform.GetComponent<Rigidbody>();
        if (playerRb != null && aimPrediction > 0f)
        {
            float distance = Vector3.Distance(transform.position, targetPos);
            float timeToReach = distance / 1000f; // Assume laser travels at 1000 units/s
            Vector3 predictedOffset = playerRb.linearVelocity * timeToReach * aimPrediction;
            return targetPos + predictedOffset;
        }
        
        return targetPos;
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
    /// Flash red with bright emission when hit - ENHANCED for visibility
    /// </summary>
    private IEnumerator HitFlashEffect()
    {
        if (cubeMaterial == null || !cubeMaterial.HasProperty("_EmissionColor"))
        {
            Debug.LogWarning("[TowerProtector] ⚠️ Cannot flash - material missing or no emission support!");
            yield break;
        }
        
        Color originalColor = cubeMaterial.GetColor("_EmissionColor");
        
        // SUPER BRIGHT flash - white/red with high intensity
        Color flashColor = Color.white * glowIntensity * hitFlashIntensity;
        
        Debug.Log($"[TowerProtector] ⚡ HIT FLASH! Intensity: {hitFlashIntensity}x, Duration: {hitFlashDuration}s");
        
        // Flash WHITE first (most visible)
        cubeMaterial.SetColor("_EmissionColor", flashColor);
        yield return new WaitForSeconds(hitFlashDuration * 0.5f);
        
        // Then flash RED
        cubeMaterial.SetColor("_EmissionColor", Color.red * glowIntensity * hitFlashIntensity * 0.7f);
        yield return new WaitForSeconds(hitFlashDuration * 0.5f);
        
        // Return to original
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
        
        // Draw laser emit point if assigned
        if (laserEmitPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(laserEmitPoint.position, 20f);
            Gizmos.DrawLine(transform.position, laserEmitPoint.position);
        }
    }
    
    /// <summary>
    /// Context menu helper to create laser emit point
    /// </summary>
    [ContextMenu("Setup/Create Laser Emit Point")]
    private void CreateLaserEmitPoint()
    {
        // Check if already exists
        Transform existingEmitPoint = transform.Find("LaserEmitPoint");
        if (existingEmitPoint != null)
        {
            Debug.LogWarning("[TowerProtector] Laser emit point already exists! Select it to reposition.");
            laserEmitPoint = existingEmitPoint;
            return;
        }
        
        // Create new emit point
        GameObject emitPointObj = new GameObject("LaserEmitPoint");
        emitPointObj.transform.SetParent(transform);
        emitPointObj.transform.localPosition = Vector3.zero;
        emitPointObj.transform.localRotation = Quaternion.identity;
        
        // Assign to field
        laserEmitPoint = emitPointObj.transform;
        
        Debug.Log("[TowerProtector] ✅ Laser emit point created! Position it where you want the laser to shoot from.");
        
        #if UNITY_EDITOR
        UnityEditor.Selection.activeGameObject = emitPointObj;
        #endif
    }
    
    /// <summary>
    /// Context menu helper to auto-assign static beam prefab
    /// </summary>
    [ContextMenu("Setup/Auto-Assign Fire Static Beam")]
    private void AutoAssignFireStaticBeam()
    {
        #if UNITY_EDITOR
        string prefabPath = "Assets/MagicArsenal/Effects/Prefabs/Beams/Static Beams/FireBeamStatic.prefab";
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (prefab != null)
        {
            staticBeamPrefab = prefab;
            Debug.Log("[TowerProtector] ✅ Fire Static Beam assigned!");
            UnityEditor.EditorUtility.SetDirty(this);
        }
        else
        {
            Debug.LogError($"[TowerProtector] ❌ Could not find static beam prefab at: {prefabPath}");
        }
        #endif
    }
    
    /// <summary>
    /// Context menu helper to auto-assign particle effect (OPTIONAL)
    /// </summary>
    private void AutoAssignFireParticleEffect()
    {
        #if UNITY_EDITOR
        string prefabPath = "Assets/MagicArsenal/Effects/Prefabs/Beam Blast/FireBeamBlast.prefab";
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (prefab != null)
        {
            beamParticleEffect = prefab;
            Debug.Log("[TowerProtector] ✅ Fire particle effect assigned (optional visual enhancement)!");
            UnityEditor.EditorUtility.SetDirty(this);
        }
        else
        {
            Debug.LogError($"[TowerProtector] ❌ Could not find particle prefab at: {prefabPath}");
        }
        #endif
    }
    
    /// <summary>
    /// Context menu helper to setup emission material
    /// </summary>
    [ContextMenu("Setup/Enable Material Emission")]
    private void EnableMaterialEmission()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend == null)
        {
            Debug.LogError("[TowerProtector] ❌ No Renderer component found!");
            return;
        }
        
        Material mat = rend.sharedMaterial;
        if (mat == null)
        {
            Debug.LogError("[TowerProtector] ❌ No material assigned to renderer!");
            return;
        }
        
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", hostileGlowColor * glowIntensity);
            Debug.Log("[TowerProtector] ✅ Emission enabled on material!");
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(mat);
            #endif
        }
        else
        {
            Debug.LogError("[TowerProtector] ❌ Material doesn't support _EmissionColor! Change shader to Standard or URP/Lit.");
        }
    }
    
    /// <summary>
    /// Context menu helper for complete setup
    /// </summary>
    [ContextMenu("Setup/Complete Auto-Setup")]
    private void CompleteAutoSetup()
    {
        Debug.Log("[TowerProtector] 🔧 Running complete auto-setup...");
        
        EnableMaterialEmission();
        AutoAssignFireStaticBeam();
        CreateLaserEmitPoint();
        
        Debug.Log("[TowerProtector] ✅ Complete auto-setup finished!");
        Debug.Log("[TowerProtector] ℹ️ Fire Static Beam assigned. Optionally run 'Setup/Auto-Assign Fire Particle Effect' for extra visual flair.");
    }
    
    /// <summary>
    /// Test: Manually trigger laser attack (ignores autoStart setting)
    /// </summary>
    [ContextMenu("Test/Start Laser Attack")]
    private void TestStartLaserAttack()
    {
        if (Application.isPlaying)
        {
            if (playerTransform == null)
            {
                FindPlayer();
            }
            
            if (playerTransform == null)
            {
                Debug.LogError("[TowerProtector] ❌ Cannot start laser attack - no player found!");
                return;
            }
            
            StartLaserCycle();
        }
        else
        {
            Debug.LogWarning("[TowerProtector] ⚠️ Must be in Play Mode to test laser attack!");
        }
    }
    
    /// <summary>
    /// Test: Immediately fire laser (skip cooldown)
    /// </summary>
    [ContextMenu("Test/Fire Laser Now")]
    private void TestFireLaserNow()
    {
        if (Application.isPlaying)
        {
            if (playerTransform == null)
            {
                FindPlayer();
            }
            
            if (playerTransform == null)
            {
                Debug.LogError("[TowerProtector] ❌ Cannot fire laser - no player found!");
                return;
            }
            
            Debug.Log("[TowerProtector] 🧪 TEST: Firing laser immediately!");
            StartCoroutine(LaserAttackSequence());
        }
        else
        {
            Debug.LogWarning("[TowerProtector] ⚠️ Must be in Play Mode to test laser!");
        }
    }
    
    /// <summary>
    /// Debug: Log current state
    /// </summary>
    [ContextMenu("Debug/Log Current State")]
    private void DebugLogCurrentState()
    {
        Debug.Log("=== TOWER PROTECTOR STATE ===");
        Debug.Log($"Is Dead: {isDead}");
        Debug.Log($"Is Friendly: {isFriendly}");
        Debug.Log($"Is Firing Laser: {isFiringLaser}");
        Debug.Log($"Is Tracking Player: {isTrackingPlayer}");
        Debug.Log($"Laser Cycle Started: {laserCycleStarted}");
        Debug.Log($"Auto Start: {autoStart}");
        Debug.Log($"Player Found: {playerTransform != null}");
        if (playerTransform != null)
        {
            Debug.Log($"Player Name: {playerTransform.name}");
            Debug.Log($"Player Position: {playerTransform.position}");
            Debug.Log($"Distance to Player: {Vector3.Distance(transform.position, playerTransform.position):F1}");
            Debug.Log($"Player In Range: {IsPlayerInRange()}");
        }
        Debug.Log($"Static Beam Prefab Assigned: {staticBeamPrefab != null}");
        if (staticBeamPrefab != null)
        {
            Debug.Log($"Static Beam Prefab Name: {staticBeamPrefab.name}");
        }
        Debug.Log($"Particle Effect Assigned: {beamParticleEffect != null}");
        if (beamParticleEffect != null)
        {
            Debug.Log($"Particle Effect Name: {beamParticleEffect.name}");
        }
        Debug.Log($"Laser Beam Width: {laserBeamWidth}");
        Debug.Log($"Laser Beam Color: {laserBeamColor}");
        Debug.Log($"Laser Emit Point Assigned: {laserEmitPoint != null}");
        Debug.Log($"Material Has Emission: {cubeMaterial != null && cubeMaterial.HasProperty("_EmissionColor")}");
        Debug.Log($"Detection Range: {detectionRange}");
        Debug.Log($"Next Laser Time: {nextLaserTime:F1} (Current: {Time.time:F1}, Wait: {Mathf.Max(0, nextLaserTime - Time.time):F1}s)");
        Debug.Log("=============================");
    }
}
