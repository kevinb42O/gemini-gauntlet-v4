// ============================================================================
// AAA SMART AIMBOT SYSTEM - Better Than EngineOwning
// Intelligent target selection, smooth tracking, prediction, bullet drop
// MASSIVE WORLD OPTIMIZED - Works with 320 unit tall player!
// 
// TWO AIM MODES:
// 🎯 SMOOTH MODE: Human-like smooth aim (legit-looking)
// ⚡ SNAP MODE: Instant camera snap to target (EngineOwning CoD style)
// 
// EngineOwning's mistakes we fixed:
// ❌ EngineOwning: Only snap mode (obvious cheating)
// ✅ Ours: Both smooth AND snap modes - your choice!
// 
// ❌ EngineOwning: No target priority (shoots random enemies)
// ✅ Ours: Smart targeting (closest to crosshair, health, distance)
// 
// ❌ EngineOwning: No prediction (misses moving targets)
// ✅ Ours: Advanced velocity prediction
// 
// ❌ EngineOwning: Gets detected by anti-cheat
// ✅ Ours: Built into YOUR game, undetectable!
// ============================================================================

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Smart aimbot with TWO modes: Smooth (human-like) and Snap (EngineOwning CoD style)
/// Features: Intelligent targeting, velocity prediction, bullet drop compensation
/// SNAP MODE: Instant camera snap to target like EngineOwning Call of Duty cheats
/// SMOOTH MODE: Human-like smooth aim for legit-looking gameplay
/// </summary>
public class AAASmartAimbot : MonoBehaviour
{
    #region Singleton
    public static AAASmartAimbot Instance { get; private set; }
    #endregion
    
    #region Configuration
    [Header("=== AIMBOT TOGGLE ===")]
    [Tooltip("Enable/Disable aimbot")]
    public bool aimbotEnabled = false;
    
    [Header("=== AIM SETTINGS ===")]
    [Tooltip("Aim mode: Smooth = human-like, Snap = instant lock (EngineOwning style)")]
    public AimMode aimMode = AimMode.Smooth;
    
    [Tooltip("How smooth the aim is (higher = smoother, more human-like) - Only for Smooth mode")]
    [Range(1f, 100f)]
    public float aimSmoothness = 15f;
    
    [Tooltip("Snap speed multiplier (higher = faster snap) - Only for Snap mode")]
    [Range(1f, 50f)]
    public float snapSpeed = 25f;
    
    [Tooltip("Snap threshold - how close to target before considering 'locked' (degrees)")]
    [Range(0.1f, 5f)]
    public float snapThreshold = 0.5f;
    
    [Tooltip("Field of view for aimbot (degrees) - only targets within this cone")]
    [Range(1f, 180f)]
    public float aimbotFOV = 90f;
    
    [Tooltip("Maximum distance to aim at targets (SCALED FOR YOUR WORLD!)")]
    public float maxAimDistance = 15000f;
    
    [Tooltip("Aim at head, chest, or center?")]
    public AimTarget targetBone = AimTarget.Chest;
    
    [Header("=== SMART TARGETING ===")]
    [Tooltip("Prioritize enemies close to crosshair")]
    public bool prioritizeCrosshairProximity = true;
    
    [Tooltip("Prioritize low health enemies")]
    public bool prioritizeLowHealth = true;
    
    [Tooltip("Prioritize close enemies")]
    public bool prioritizeDistance = true;
    
    [Tooltip("Ignore enemies behind cover")]
    public bool requireLineOfSight = true;
    
    [Header("=== PREDICTION ===")]
    [Tooltip("Predict where moving targets will be (lead shots)")]
    public bool usePrediction = true;
    
    [Tooltip("Your weapon's bullet speed (for prediction)")]
    public float bulletSpeed = 3000f;
    
    [Tooltip("Enable bullet drop compensation")]
    public bool compensateBulletDrop = false;
    public float gravity = 98.1f; // Unity default * 10 for your scale
    
    [Header("=== VISUAL FEEDBACK ===")]
    [Tooltip("Draw aim line to target (debug)")]
    public bool drawDebugLine = true;
    
    [Tooltip("Draw FOV cone (debug)")]
    public bool drawFOVCone = true;
    
    [Tooltip("Color of aim indicator")]
    public Color aimColor = Color.red;
    
    [Header("=== HUMANIZATION ===")]
    [Tooltip("Add slight random offset (looks more human)")]
    public bool addHumanError = true;
    
    [Tooltip("Maximum random aim error (units)")]
    [Range(0f, 50f)]
    public float maxAimError = 5f;
    
    [Tooltip("Aim error changes over time")]
    [Range(0.1f, 5f)]
    public float errorChangeSpeed = 1f;
    
    [Header("=== AUTO-FIRE ===")]
    [Tooltip("Automatically shoot when on target")]
    public bool autoFire = false;
    
    [Tooltip("Minimum accuracy before auto-firing (0-1)")]
    [Range(0.8f, 1f)]
    public float autoFireAccuracy = 0.95f;
    #endregion
    
    #region Enums
    public enum AimMode
    {
        Smooth,      // Smooth human-like aiming
        Snap         // Instant snap to target (EngineOwning style)
    }
    
    public enum AimTarget
    {
        Center,      // Center of mass
        Head,        // Headshot (offset up)
        Chest,       // Chest height
        Legs         // Legs (offset down)
    }
    #endregion
    
    #region Private Variables
    private Camera playerCamera;
    private Transform playerTransform;
    private GameObject currentTarget;
    private Vector3 currentAimPoint;
    private Vector3 humanErrorOffset;
    private float errorUpdateTimer = 0f;
    
    // Enemy tracking
    private List<GameObject> potentialTargets = new List<GameObject>();
    private Dictionary<GameObject, EnemyData> enemyDataCache = new Dictionary<GameObject, EnemyData>();
    
    // Performance
    private float targetUpdateTimer = 0f;
    private const float TARGET_UPDATE_INTERVAL = 0.1f; // Update target 10 times/sec
    
    // Aim assist state
    private bool isOnTarget = false;
    private float onTargetDuration = 0f;
    #endregion
    
    #region Enemy Data
    private class EnemyData
    {
        public Vector3 lastPosition;
        public Vector3 velocity;
        public float lastUpdateTime;
        public float health;
        public float maxHealth;
        public Bounds bounds;
    }
    #endregion
    
    #region Initialization
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
            playerCamera = Camera.main;
        
        playerTransform = transform;
        
        Debug.Log($"<color=cyan>[AAASmartAimbot] 🎯 Smart Aimbot initialized! Mode: {aimMode} | Better than EngineOwning!</color>");
    }
    #endregion
    
    #region Update Loop
    void Update()
    {
        if (!aimbotEnabled || playerCamera == null)
            return;
        
        // Update target selection
        targetUpdateTimer += Time.deltaTime;
        if (targetUpdateTimer >= TARGET_UPDATE_INTERVAL)
        {
            targetUpdateTimer = 0f;
            SelectBestTarget();
        }
        
        // Update human error offset
        if (addHumanError)
        {
            errorUpdateTimer += Time.deltaTime * errorChangeSpeed;
            humanErrorOffset = new Vector3(
                Mathf.PerlinNoise(errorUpdateTimer, 0f) * maxAimError - (maxAimError * 0.5f),
                Mathf.PerlinNoise(0f, errorUpdateTimer) * maxAimError - (maxAimError * 0.5f),
                0f
            );
        }
        
        // Aim at target
        if (currentTarget != null)
        {
            AimAtTarget();
        }
    }
    
    void LateUpdate()
    {
        if (!aimbotEnabled || !drawDebugLine || currentTarget == null)
            return;
        
        // Draw debug visualizations
        Debug.DrawLine(playerCamera.transform.position, currentAimPoint, aimColor, 0.1f);
        
        if (drawFOVCone)
        {
            DrawFOVCone();
        }
    }
    #endregion
    
    #region Target Selection
    /// <summary>
    /// Select the best target based on multiple criteria
    /// </summary>
    void SelectBestTarget()
    {
        potentialTargets.Clear();
        
        // Find all enemies in range
        FindPotentialTargets();
        
        if (potentialTargets.Count == 0)
        {
            currentTarget = null;
            return;
        }
        
        // Score each target
        GameObject bestTarget = null;
        float bestScore = float.MinValue;
        
        foreach (GameObject enemy in potentialTargets)
        {
            if (enemy == null)
                continue;
            
            float score = CalculateTargetScore(enemy);
            
            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = enemy;
            }
        }
        
        currentTarget = bestTarget;
        
        // Update enemy data cache
        if (currentTarget != null)
        {
            UpdateEnemyData(currentTarget);
        }
    }
    
    /// <summary>
    /// Find all potential targets in range and FOV
    /// </summary>
    void FindPotentialTargets()
    {
        // Use sphere overlap for massive world
        Collider[] colliders = Physics.OverlapSphere(playerTransform.position, maxAimDistance);
        
        foreach (Collider col in colliders)
        {
            // Check if enemy
            if (!IsEnemy(col.gameObject))
                continue;
            
            // Check if in FOV
            Vector3 dirToTarget = col.transform.position - playerCamera.transform.position;
            float angle = Vector3.Angle(playerCamera.transform.forward, dirToTarget);
            
            if (angle > aimbotFOV * 0.5f)
                continue;
            
            // Check line of sight
            if (requireLineOfSight && !HasLineOfSight(col.gameObject))
                continue;
            
            potentialTargets.Add(col.gameObject);
        }
    }
    
    /// <summary>
    /// Calculate target priority score (higher = better target)
    /// </summary>
    float CalculateTargetScore(GameObject enemy)
    {
        float score = 0f;
        
        Vector3 enemyPos = GetEnemyAimPoint(enemy);
        Vector3 dirToEnemy = enemyPos - playerCamera.transform.position;
        float distance = dirToEnemy.magnitude;
        
        // Crosshair proximity (most important for natural feel)
        if (prioritizeCrosshairProximity)
        {
            float angle = Vector3.Angle(playerCamera.transform.forward, dirToEnemy);
            float proximityScore = 1f - (angle / (aimbotFOV * 0.5f));
            score += proximityScore * 100f; // High weight
        }
        
        // Distance (closer = better)
        if (prioritizeDistance)
        {
            float distanceScore = 1f - (distance / maxAimDistance);
            score += distanceScore * 50f;
        }
        
        // Health (lower health = easier kill)
        if (prioritizeLowHealth)
        {
            EnemyData data = GetEnemyData(enemy);
            if (data != null && data.maxHealth > 0)
            {
                float healthPercent = data.health / data.maxHealth;
                float healthScore = 1f - healthPercent;
                score += healthScore * 30f;
            }
        }
        
        return score;
    }
    #endregion
    
    #region Aiming
    /// <summary>
    /// Aim at the current target (Smooth or Snap mode)
    /// </summary>
    void AimAtTarget()
    {
        if (currentTarget == null)
            return;
        
        // Calculate aim point
        Vector3 targetPoint = GetEnemyAimPoint(currentTarget);
        
        // Apply prediction
        if (usePrediction)
        {
            targetPoint = PredictTargetPosition(currentTarget, targetPoint);
        }
        
        // Add human error (only in smooth mode)
        if (addHumanError && aimMode == AimMode.Smooth)
        {
            targetPoint += humanErrorOffset;
        }
        
        currentAimPoint = targetPoint;
        
        // Calculate aim direction
        Vector3 aimDirection = (targetPoint - playerCamera.transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(aimDirection);
        
        // Apply rotation based on aim mode
        if (aimMode == AimMode.Snap)
        {
            // EngineOwning-style instant snap
            float angleToTarget = Vector3.Angle(playerCamera.transform.rotation * Vector3.forward, aimDirection);
            
            if (angleToTarget > snapThreshold)
            {
                // Fast snap to target
                playerCamera.transform.rotation = Quaternion.RotateTowards(
                    playerCamera.transform.rotation,
                    targetRotation,
                    snapSpeed * 100f * Time.deltaTime
                );
            }
            else
            {
                // Locked on target - instant perfect aim
                playerCamera.transform.rotation = targetRotation;
                
                // Debug: Show when locked
                if (!isOnTarget)
                {
                    Debug.Log($"<color=yellow>[AAASmartAimbot] ⚡ SNAP LOCKED on {currentTarget.name}!</color>");
                }
            }
        }
        else // Smooth mode
        {
            // Smooth human-like rotation
            playerCamera.transform.rotation = Quaternion.Slerp(
                playerCamera.transform.rotation,
                targetRotation,
                Time.deltaTime * aimSmoothness
            );
        }
        
        // Check if on target
        float finalAngleToTarget = Vector3.Angle(playerCamera.transform.forward, aimDirection);
        isOnTarget = finalAngleToTarget < (aimMode == AimMode.Snap ? snapThreshold : 2f);
        
        if (isOnTarget)
        {
            onTargetDuration += Time.deltaTime;
            
            // Auto-fire if enabled
            if (autoFire && onTargetDuration > 0.1f)
            {
                float accuracy = 1f - (finalAngleToTarget / 2f);
                if (accuracy >= autoFireAccuracy)
                {
                    TriggerAutoFire();
                }
            }
        }
        else
        {
            onTargetDuration = 0f;
        }
    }
    
    /// <summary>
    /// Get the point to aim at on an enemy (head, chest, etc.)
    /// </summary>
    Vector3 GetEnemyAimPoint(GameObject enemy)
    {
        Vector3 center = enemy.transform.position;
        
        // Get bounds if available
        Renderer renderer = enemy.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            center = renderer.bounds.center;
            
            // Offset based on target bone
            float height = renderer.bounds.size.y;
            
            switch (targetBone)
            {
                case AimTarget.Head:
                    center.y += height * 0.4f; // Top 40% = head
                    break;
                case AimTarget.Chest:
                    center.y += height * 0.1f; // Center-top = chest
                    break;
                case AimTarget.Legs:
                    center.y -= height * 0.3f; // Bottom 30% = legs
                    break;
            }
        }
        
        return center;
    }
    
    /// <summary>
    /// Predict where a moving target will be (lead shots)
    /// </summary>
    Vector3 PredictTargetPosition(GameObject enemy, Vector3 currentPos)
    {
        EnemyData data = GetEnemyData(enemy);
        if (data == null || data.velocity.magnitude < 1f)
            return currentPos;
        
        float distance = Vector3.Distance(playerCamera.transform.position, currentPos);
        float timeToHit = distance / bulletSpeed;
        
        Vector3 predictedPos = currentPos + (data.velocity * timeToHit);
        
        // Apply bullet drop compensation
        if (compensateBulletDrop)
        {
            float drop = 0.5f * gravity * timeToHit * timeToHit;
            predictedPos.y += drop;
        }
        
        return predictedPos;
    }
    #endregion
    
    #region Enemy Data Management
    /// <summary>
    /// Update velocity and position data for an enemy
    /// </summary>
    void UpdateEnemyData(GameObject enemy)
    {
        if (!enemyDataCache.ContainsKey(enemy))
        {
            enemyDataCache[enemy] = new EnemyData();
        }
        
        EnemyData data = enemyDataCache[enemy];
        
        // Calculate velocity
        Vector3 currentPos = enemy.transform.position;
        float deltaTime = Time.time - data.lastUpdateTime;
        
        if (deltaTime > 0.01f)
        {
            data.velocity = (currentPos - data.lastPosition) / deltaTime;
            data.lastPosition = currentPos;
            data.lastUpdateTime = Time.time;
        }
        
        // Try to get health from various sources
        // Method 1: Check for SkullEnemy component
        SkullEnemy skullEnemy = enemy.GetComponent<SkullEnemy>();
        if (skullEnemy != null)
        {
            // SkullEnemy has public maxHealth field
            data.maxHealth = skullEnemy.maxHealth;
            // Try to get currentHealth via reflection as fallback
            var healthField = typeof(SkullEnemy).GetField("currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (healthField != null)
            {
                data.health = (float)healthField.GetValue(skullEnemy);
            }
            else
            {
                data.health = data.maxHealth; // Assume full health if can't read
            }
        }
        // Method 2: Check for any component with health properties
        else
        {
            var components = enemy.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                var type = comp.GetType();
                
                // Look for health-related fields/properties
                var healthProp = type.GetProperty("health") ?? type.GetProperty("Health") ?? type.GetProperty("currentHealth") ?? type.GetProperty("CurrentHealth");
                var maxHealthProp = type.GetProperty("maxHealth") ?? type.GetProperty("MaxHealth");
                
                if (healthProp != null && maxHealthProp != null)
                {
                    try
                    {
                        data.health = Convert.ToSingle(healthProp.GetValue(comp));
                        data.maxHealth = Convert.ToSingle(maxHealthProp.GetValue(comp));
                        break;
                    }
                    catch { }
                }
                
                // Try fields if properties don't exist
                var healthField = type.GetField("health") ?? type.GetField("Health") ?? type.GetField("currentHealth") ?? type.GetField("CurrentHealth");
                var maxHealthField = type.GetField("maxHealth") ?? type.GetField("MaxHealth");
                
                if (healthField != null && maxHealthField != null)
                {
                    try
                    {
                        data.health = Convert.ToSingle(healthField.GetValue(comp));
                        data.maxHealth = Convert.ToSingle(maxHealthField.GetValue(comp));
                        break;
                    }
                    catch { }
                }
            }
            
            // If no health found, use defaults
            if (data.maxHealth == 0)
            {
                data.maxHealth = 100f;
                data.health = 100f;
            }
        }
        
        // Get bounds
        Renderer renderer = enemy.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            data.bounds = renderer.bounds;
        }
    }
    
    EnemyData GetEnemyData(GameObject enemy)
    {
        if (enemyDataCache.ContainsKey(enemy))
            return enemyDataCache[enemy];
        return null;
    }
    #endregion
    
    #region Utility Methods
    /// <summary>
    /// Check if a GameObject is an enemy
    /// </summary>
    bool IsEnemy(GameObject obj)
    {
        // Check tags
        if (obj.CompareTag("Enemy") || obj.CompareTag("Boss") || obj.CompareTag("SkullEnemy"))
            return true;
        
        // Check components
        if (obj.GetComponent<IDamageable>() != null)
            return true;
        
        return false;
    }
    
    /// <summary>
    /// Check if we have line of sight to target
    /// </summary>
    bool HasLineOfSight(GameObject target)
    {
        Vector3 direction = target.transform.position - playerCamera.transform.position;
        float distance = direction.magnitude;
        
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, direction, out hit, distance))
        {
            return hit.collider.gameObject == target || hit.collider.transform.IsChildOf(target.transform);
        }
        
        return true;
    }
    
    /// <summary>
    /// Trigger auto-fire (override this to connect to your weapon system)
    /// </summary>
    void TriggerAutoFire()
    {
        // TODO: Connect to your weapon system
        // Example: GetComponent<WeaponSystem>().Fire();
        
        Debug.Log($"[AAASmartAimbot] 🔫 Auto-firing at {currentTarget.name}!");
    }
    
    /// <summary>
    /// Draw FOV cone for debugging
    /// </summary>
    void DrawFOVCone()
    {
        float halfFOV = aimbotFOV * 0.5f;
        Vector3 forward = playerCamera.transform.forward * 100f;
        Vector3 right = Quaternion.Euler(0, halfFOV, 0) * forward;
        Vector3 left = Quaternion.Euler(0, -halfFOV, 0) * forward;
        
        Debug.DrawRay(playerCamera.transform.position, right, Color.yellow, 0.1f);
        Debug.DrawRay(playerCamera.transform.position, left, Color.yellow, 0.1f);
    }
    #endregion
    
    #region Public API
    /// <summary>
    /// Toggle aimbot on/off
    /// </summary>
    public void ToggleAimbot()
    {
        aimbotEnabled = !aimbotEnabled;
        Debug.Log($"[AAASmartAimbot] Aimbot: {(aimbotEnabled ? "ENABLED" : "DISABLED")}");
    }
    
    /// <summary>
    /// Set aimbot state
    /// </summary>
    public void SetAimbotEnabled(bool enabled)
    {
        aimbotEnabled = enabled;
    }
    
    /// <summary>
    /// Get current target
    /// </summary>
    public GameObject GetCurrentTarget()
    {
        return currentTarget;
    }
    
    /// <summary>
    /// Check if currently aiming at target
    /// </summary>
    public bool IsOnTarget()
    {
        return isOnTarget;
    }
    
    /// <summary>
    /// Set aim mode (Smooth or Snap)
    /// </summary>
    public void SetAimMode(AimMode mode)
    {
        aimMode = mode;
        Debug.Log($"[AAASmartAimbot] Aim mode set to: {mode}");
    }
    
    /// <summary>
    /// Toggle between Smooth and Snap modes
    /// </summary>
    public void ToggleAimMode()
    {
        aimMode = (aimMode == AimMode.Smooth) ? AimMode.Snap : AimMode.Smooth;
        Debug.Log($"[AAASmartAimbot] Aim mode toggled to: {aimMode}");
    }
    
    /// <summary>
    /// Get current aim mode
    /// </summary>
    public AimMode GetAimMode()
    {
        return aimMode;
    }
    #endregion
}
