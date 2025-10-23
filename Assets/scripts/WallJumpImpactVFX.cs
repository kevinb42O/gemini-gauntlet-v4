using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// AAA Wall Jump Impact VFX System
/// 
/// Creates cinematic ripple effects at wall jump impact points with:
/// - Speed-based intensity scaling (faster = more dramatic)
/// - Fresnel glow that only shows during the ripple
/// - Supports various surface sizes and orientations
/// - Pool system for performance
/// 
/// Technical Features:
/// - World-space ripple positioning on impact surface
/// - Dynamic shader parameter control
/// - Effect lifecycle management
/// - Performance-optimized pooling
/// 
/// Integration: Called from AAAMovementController.PerformWallJump()
/// </summary>
public class WallJumpImpactVFX : MonoBehaviour
{
    [Header("=== IMPACT VFX SETTINGS ===")]
    [Tooltip("Prefab containing the ripple effect (quad with material)")]
    public GameObject rippleEffectPrefab;
    
    [Tooltip("Maximum number of simultaneous effects (performance limit)")]
    public int maxSimultaneousEffects = 8;
    
    [Tooltip("Base effect duration in seconds")]
    public float baseDuration = 2f;
    
    [Tooltip("Effect scale multiplier (adjusts ripple size)")]
    public float effectScale = 1f;
    
    [Header("=== SPEED SCALING ===")]
    [Tooltip("Minimum speed for effect (below this = no effect)")]
    public float minSpeedThreshold = 300f;
    
    [Tooltip("Speed at which effect reaches maximum intensity")]
    public float maxIntensitySpeed = 1500f;
    
    [Tooltip("Minimum effect intensity (at threshold speed)")]
    [Range(0f, 1f)]
    public float minIntensity = 0.3f;
    
    [Tooltip("Maximum effect intensity (at max speed)")]
    [Range(0f, 2f)]
    public float maxIntensity = 1.5f;
    
    [Header("=== SURFACE POSITIONING ===")]
    [Tooltip("Distance to offset effect from surface (prevents z-fighting)")]
    public float surfaceOffset = 0.01f;
    
    [Tooltip("Maximum distance from impact point to search for surface")]
    public float surfaceSearchDistance = 50f;
    
    [Header("=== DEBUG ===")]
    [Tooltip("Show debug logs and visualizations")]
    public bool enableDebug = false;
    
    // Singleton pattern for easy access
    public static WallJumpImpactVFX Instance { get; private set; }
    
    // Effect pooling
    private Queue<RippleEffect> effectPool = new Queue<RippleEffect>();
    private List<RippleEffect> activeEffects = new List<RippleEffect>();
    
    // Shader property IDs (cached for performance)
    private int _impactTime_ID;
    private int _intensity_ID;
    private int _duration_ID;
    private int _impactPosition_ID;
    
    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            InitializeSystem();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    private void InitializeSystem()
    {
        // Cache shader property IDs for performance
        _impactTime_ID = Shader.PropertyToID("_ImpactTime");
        _intensity_ID = Shader.PropertyToID("_Intensity");
        _duration_ID = Shader.PropertyToID("_Duration");
        _impactPosition_ID = Shader.PropertyToID("_ImpactPosition");
        
        // Pre-populate effect pool
        for (int i = 0; i < maxSimultaneousEffects; i++)
        {
            CreatePooledEffect();
        }
        
        if (enableDebug)
        {
            Debug.Log($"[WallJumpImpactVFX] System initialized with {maxSimultaneousEffects} pooled effects", this);
        }
    }
    
    private void CreatePooledEffect()
    {
        if (rippleEffectPrefab == null) return;
        
        GameObject effectObj = Instantiate(rippleEffectPrefab, transform);
        effectObj.SetActive(false);
        
        RippleEffect effect = new RippleEffect
        {
            gameObject = effectObj,
            transform = effectObj.transform,
            renderer = effectObj.GetComponent<Renderer>(),
            material = null
        };
        
        // Get material instance for shader property control
        if (effect.renderer != null)
        {
            effect.material = effect.renderer.material; // Creates instance
        }
        
        effectPool.Enqueue(effect);
    }
    
    /// <summary>
    /// Trigger wall jump impact effect at specified position
    /// Called from AAAMovementController.PerformWallJump()
    /// </summary>
    /// <param name="impactPoint">World position where player contacted wall</param>
    /// <param name="wallNormal">Surface normal at impact point</param>
    /// <param name="playerSpeed">Player's speed at time of impact</param>
    /// <param name="wallCollider">The wall collider that was impacted (optional)</param>
    public void TriggerImpactEffect(Vector3 impactPoint, Vector3 wallNormal, float playerSpeed, Collider wallCollider = null)
    {
        // Check speed threshold
        if (playerSpeed < minSpeedThreshold)
        {
            if (enableDebug)
            {
                Debug.Log($"[WallJumpImpactVFX] Impact speed {playerSpeed:F1} below threshold {minSpeedThreshold}, skipping effect", this);
            }
            return;
        }
        
        // Get effect from pool
        RippleEffect effect = GetPooledEffect();
        if (effect == null)
        {
            if (enableDebug)
            {
                Debug.LogWarning("[WallJumpImpactVFX] No available effects in pool, skipping", this);
            }
            return;
        }
        
        // Calculate intensity based on speed
        float normalizedSpeed = Mathf.InverseLerp(minSpeedThreshold, maxIntensitySpeed, playerSpeed);
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, normalizedSpeed);
        
        // Position effect on surface
        Vector3 effectPosition = CalculateEffectPosition(impactPoint, wallNormal, wallCollider);
        Quaternion effectRotation = CalculateEffectRotation(wallNormal);
        
        // Setup effect
        effect.transform.position = effectPosition;
        effect.transform.rotation = effectRotation;
        effect.transform.localScale = Vector3.one * effectScale;
        
        // Configure shader properties
        if (effect.material != null)
        {
            effect.material.SetFloat(_impactTime_ID, Time.time);
            effect.material.SetFloat(_intensity_ID, intensity);
            effect.material.SetFloat(_duration_ID, baseDuration);
            effect.material.SetVector(_impactPosition_ID, effectPosition);
        }
        
        // Activate and track effect
        effect.gameObject.SetActive(true);
        effect.startTime = Time.time;
        effect.duration = baseDuration;
        effect.intensity = intensity;
        activeEffects.Add(effect);
        
        if (enableDebug)
        {
            Debug.Log($"[WallJumpImpactVFX] Triggered effect at {effectPosition} with intensity {intensity:F2} (speed: {playerSpeed:F1})", this);
            Debug.DrawRay(effectPosition, wallNormal * 100f, Color.cyan, 2f);
        }
    }
    
    private Vector3 CalculateEffectPosition(Vector3 impactPoint, Vector3 wallNormal, Collider wallCollider)
    {
        // Offset slightly from surface to prevent z-fighting
        Vector3 basePosition = impactPoint + (wallNormal * surfaceOffset);
        
        // If we have the wall collider, try to find the exact surface point
        if (wallCollider != null)
        {
            // Raycast back towards the surface to get precise positioning
            Ray ray = new Ray(impactPoint + wallNormal * surfaceSearchDistance, -wallNormal);
            RaycastHit hit;
            
            if (wallCollider.Raycast(ray, out hit, surfaceSearchDistance * 2f))
            {
                basePosition = hit.point + (hit.normal * surfaceOffset);
                if (enableDebug)
                {
                    Debug.Log($"[WallJumpImpactVFX] Refined position using surface raycast", this);
                }
            }
        }
        
        return basePosition;
    }
    
    private Quaternion CalculateEffectRotation(Vector3 wallNormal)
    {
        // Orient effect to face outward from wall surface
        // The ripple effect should be perpendicular to the wall normal
        return Quaternion.LookRotation(wallNormal, Vector3.up);
    }
    
    private RippleEffect GetPooledEffect()
    {
        // Try to get from pool first
        if (effectPool.Count > 0)
        {
            return effectPool.Dequeue();
        }
        
        // If pool is empty, try to recycle oldest active effect
        if (activeEffects.Count > 0)
        {
            RippleEffect oldestEffect = activeEffects[0];
            activeEffects.RemoveAt(0);
            oldestEffect.gameObject.SetActive(false);
            return oldestEffect;
        }
        
        // Create new effect as last resort
        CreatePooledEffect();
        return effectPool.Count > 0 ? effectPool.Dequeue() : null;
    }
    
    private void ReturnEffectToPool(RippleEffect effect)
    {
        effect.gameObject.SetActive(false);
        activeEffects.Remove(effect);
        effectPool.Enqueue(effect);
    }
    
    void Update()
    {
        // Update active effects and return completed ones to pool
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            RippleEffect effect = activeEffects[i];
            
            if (Time.time >= effect.startTime + effect.duration)
            {
                ReturnEffectToPool(effect);
            }
        }
        
        // Debug display
        if (enableDebug && activeEffects.Count > 0)
        {
            // Update debug info (optional - can be removed for performance)
        }
    }
    
    /// <summary>
    /// Data structure for pooled ripple effects
    /// </summary>
    private class RippleEffect
    {
        public GameObject gameObject;
        public Transform transform;
        public Renderer renderer;
        public Material material;
        public float startTime;
        public float duration;
        public float intensity;
    }
    
    // Public API for external configuration
    public void SetEffectScale(float scale)
    {
        effectScale = Mathf.Max(0.1f, scale);
    }
    
    public void SetBaseDuration(float duration)
    {
        baseDuration = Mathf.Max(0.1f, duration);
    }
    
    public void SetSpeedThresholds(float minSpeed, float maxSpeed)
    {
        minSpeedThreshold = Mathf.Max(0f, minSpeed);
        maxIntensitySpeed = Mathf.Max(minSpeedThreshold + 0.1f, maxSpeed);
    }
    
    public void SetIntensityRange(float minInt, float maxInt)
    {
        minIntensity = Mathf.Clamp(minInt, 0f, 2f);
        maxIntensity = Mathf.Clamp(maxInt, minIntensity, 5f);
    }
}