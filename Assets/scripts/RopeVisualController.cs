// --- RopeVisualController.cs ---
// Handles visual effects for rope swing using Arcane LineRenderer prefabs
// Supports dynamic rope effects, energy-based visuals, and catenary curve
using UnityEngine;

/// <summary>
/// Visual controller for rope swing system.
/// Uses Arcane LineRenderer prefabs for magical rope aesthetics.
/// Features:
/// - Dynamic rope curve (catenary/parabolic sag)
/// - Energy-based visual effects (color, width, particles)
/// - Smooth attachment/release animations
/// </summary>
public class RopeVisualController : MonoBehaviour
{
    [Header("=== 🎨 ROPE VISUAL PREFAB ===")]
    [Tooltip("Arcane LineRenderer prefab (same as companion cube laser)")]
    [SerializeField] private GameObject ropeLinePrefab;
    
    [Tooltip("Spawn rope at hand emit point? (If false, spawns at player center)")]
    [SerializeField] private bool useHandEmitPoint = true;
    
    [Tooltip("Hand emit point transform (for shooting from hand)")]
    [SerializeField] private Transform handEmitPoint;
    
    [Header("=== 📐 ROPE CURVE ===")]
    [Tooltip("Enable catenary curve (realistic rope sag)")]
    [SerializeField] private bool enableCurve = true;
    
    [Tooltip("Number of segments in rope curve (more = smoother, but more expensive)")]
    [Range(2, 20)]
    [SerializeField] private int curveSegments = 8;
    
    [Tooltip("Rope sag amount (0 = straight line, 1 = heavy sag)")]
    [Range(0f, 2f)]
    [SerializeField] private float sagAmount = 0.3f;
    
    [Tooltip("Sag reduces with speed (taut rope when swinging fast)")]
    [SerializeField] private bool dynamicSag = true;
    
    [Header("=== ⚡ ENERGY-BASED EFFECTS ===")]
    [Tooltip("Rope width scales with swing energy")]
    [SerializeField] private bool energyScalesWidth = true;
    
    [Tooltip("Base rope width")]
    [SerializeField] private float baseWidth = 15f;
    
    [Tooltip("Max rope width at high energy")]
    [SerializeField] private float maxWidth = 40f;
    
    [Tooltip("Energy threshold for max width")]
    [SerializeField] private float maxEnergyThreshold = 3000f;
    
    [Tooltip("Rope color gradient (low energy -> high energy)")]
    [SerializeField] private Gradient energyColorGradient;
    
    [Header("=== ✨ PARTICLE EFFECTS ===")]
    [Tooltip("Particle system at rope anchor point")]
    [SerializeField] private GameObject anchorParticlesPrefab;
    
    [Tooltip("Particle system along rope (energy trails)")]
    [SerializeField] private GameObject ropeTrailParticlesPrefab;
    
    [Tooltip("Spawn particles along rope every X units")]
    [SerializeField] private float particleSpacing = 500f;
    
    [Header("=== 🎬 ANIMATION ===")]
    [Tooltip("Rope shoot-out animation speed")]
    [SerializeField] private float shootAnimationSpeed = 10f;
    
    [Tooltip("Rope retract animation speed")]
    [SerializeField] private float retractAnimationSpeed = 15f;
    
    [Header("=== 🐛 DEBUG ===")]
    [Tooltip("Show debug info")]
    [SerializeField] private bool showDebug = false;
    
    // === RUNTIME STATE ===
    private GameObject ropeLineInstance;
    private LineRenderer lineRenderer;
    private GameObject anchorParticlesInstance;
    private GameObject[] ropeTrailParticles;
    
    private bool isActive = false;
    private Vector3 currentAnchor = Vector3.zero;
    private float currentRopeLength = 0f;
    private float animationProgress = 0f; // 0-1 for shoot/retract animation
    
    // === DEFAULT GRADIENT ===
    private void InitializeDefaultGradient()
    {
        if (energyColorGradient == null || energyColorGradient.colorKeys.Length == 0)
        {
            energyColorGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(new Color(0.3f, 0.8f, 1f), 0f);    // Cyan (low energy)
            colorKeys[1] = new GradientColorKey(new Color(0.5f, 0.3f, 1f), 0.5f);  // Purple (medium)
            colorKeys[2] = new GradientColorKey(new Color(1f, 0.3f, 0.8f), 1f);    // Magenta (high energy)
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);
            
            energyColorGradient.SetKeys(colorKeys, alphaKeys);
        }
    }
    
    void Awake()
    {
        InitializeDefaultGradient();
        
        // Auto-find hand emit point if not assigned
        if (useHandEmitPoint && handEmitPoint == null)
        {
            // Try to find HandFiringMechanics component
            HandFiringMechanics handMechanics = FindObjectOfType<HandFiringMechanics>();
            if (handMechanics != null && handMechanics.emitPoint != null)
            {
                handEmitPoint = handMechanics.emitPoint;
                if (showDebug) Debug.Log("[ROPE VISUAL] Found hand emit point from HandFiringMechanics");
            }
        }
    }
    
    public void OnRopeAttached(Vector3 anchor, float ropeLength)
    {
        currentAnchor = anchor;
        currentRopeLength = ropeLength;
        isActive = true;
        animationProgress = 0f;
        
        // Spawn rope line
        if (ropeLinePrefab != null)
        {
            Vector3 spawnPos = GetRopeStartPosition();
            ropeLineInstance = Instantiate(ropeLinePrefab, spawnPos, Quaternion.identity, transform);
            lineRenderer = ropeLineInstance.GetComponent<LineRenderer>();
            
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = enableCurve ? curveSegments : 2;
                lineRenderer.useWorldSpace = true;
                lineRenderer.startWidth = baseWidth;
                lineRenderer.endWidth = baseWidth;
                
                if (showDebug) Debug.Log($"[ROPE VISUAL] ✨ Rope spawned! Segments: {lineRenderer.positionCount}");
            }
            else
            {
                Debug.LogWarning("[ROPE VISUAL] Rope prefab has no LineRenderer component!");
            }
        }
        else
        {
            Debug.LogWarning("[ROPE VISUAL] No rope line prefab assigned! Using debug visualization only.");
        }
        
        // Spawn anchor particles
        if (anchorParticlesPrefab != null)
        {
            anchorParticlesInstance = Instantiate(anchorParticlesPrefab, anchor, Quaternion.identity);
        }
        
        // Spawn rope trail particles
        if (ropeTrailParticlesPrefab != null && particleSpacing > 0f)
        {
            int particleCount = Mathf.Max(1, Mathf.FloorToInt(ropeLength / particleSpacing));
            ropeTrailParticles = new GameObject[particleCount];
            
            for (int i = 0; i < particleCount; i++)
            {
                float t = (i + 1f) / (particleCount + 1f); // Distribute along rope
                Vector3 particlePos = Vector3.Lerp(GetRopeStartPosition(), anchor, t);
                ropeTrailParticles[i] = Instantiate(ropeTrailParticlesPrefab, particlePos, Quaternion.identity, transform);
            }
        }
    }
    
    public void OnRopeReleased()
    {
        isActive = false;
        animationProgress = 1f; // Start retract animation
        
        // Start retract animation (will destroy after completion)
        StartCoroutine(RetractAnimation());
    }
    
    public void UpdateRope(Vector3 playerPosition, Vector3 anchor, float swingEnergy, float tensionScalar)
    {
        if (!isActive || lineRenderer == null) return;
        
        // Update animation progress (shoot-out)
        if (animationProgress < 1f)
        {
            animationProgress += Time.deltaTime * shootAnimationSpeed;
            animationProgress = Mathf.Clamp01(animationProgress);
        }
        
        // Get rope start/end positions
        // ALWAYS use player center position (passed parameter) for consistency
        Vector3 startPos = playerPosition;
        Vector3 endPos = anchor;
        
        // === PERFECT ROPE RENDERING - NO BULLSHIT ===
        // Simple rule: Tension >= 0.1 = STRAIGHT LINE, Tension < 0.1 = slight sag
        // This eliminates the "two ropes" visual bug
        
        float energyNormalized = Mathf.Clamp01(swingEnergy / maxEnergyThreshold);
        
        // ULTRA-AGGRESSIVE tension threshold: rope is straight unless COMPLETELY slack
        float effectiveSag = tensionScalar < 0.05f ? sagAmount * 0.3f : 0f;
        
        // Update rope - straight line or minimal curve
        if (effectiveSag > 0.001f && enableCurve && lineRenderer.positionCount > 2)
        {
            // Minimal curve (barely noticeable sag)
            UpdateCurvedRope_Fixed(startPos, endPos, effectiveSag, energyNormalized);
        }
        else
        {
            // STRAIGHT LINE (99% of the time this is what we want!)
            for (int i = 0; i < lineRenderer.positionCount; i++)
            {
                float t = i / (float)(lineRenderer.positionCount - 1);
                t *= animationProgress; // Apply shoot-out animation
                lineRenderer.SetPosition(i, Vector3.Lerp(startPos, endPos, t));
            }
        }
        
        // Update visual effects based on tension
        UpdateRopeEffects(energyNormalized, tensionScalar);
        
        // Update particle positions if enabled
        if (ropeTrailParticles != null)
        {
            for (int i = 0; i < ropeTrailParticles.Length; i++)
            {
                if (ropeTrailParticles[i] != null)
                {
                    float t = (i + 1f) / (ropeTrailParticles.Length + 1f);
                    ropeTrailParticles[i].transform.position = Vector3.Lerp(startPos, endPos, t);
                }
            }
        }
    }
    
    void UpdateCurvedRope_Fixed(Vector3 start, Vector3 end, float sagAmount, float energyNormalized)
    {
        float ropeLength = Vector3.Distance(start, end);
        
        // Generate minimal curve (barely visible sag)
        for (int i = 0; i < lineRenderer.positionCount; i++)
        {
            float t = i / (float)(lineRenderer.positionCount - 1);
            t *= animationProgress; // Apply shoot-out animation
            
            // Linear interpolation with tiny sag in middle
            Vector3 linearPoint = Vector3.Lerp(start, end, t);
            
            // Minimal downward sag (parabola peaks at middle)
            float catenaryFactor = 4f * t * (1f - t);
            float sagDistance = ropeLength * sagAmount * catenaryFactor;
            
            lineRenderer.SetPosition(i, linearPoint + Vector3.down * sagDistance);
        }
    }
    
    void UpdateRopeEffects(float energyNormalized, float tensionScalar)
    {
        // GRAPPLE MODE DETECTION (tension = 1.0 means ultra-taut grappling)
        bool isGrappleMode = tensionScalar >= 0.95f;
        
        // Width scales with both energy AND tension
        if (energyScalesWidth)
        {
            if (isGrappleMode)
            {
                // GRAPPLE MODE: Thicker rope (high tension, reeling in)
                float width = Mathf.Lerp(maxWidth * 0.7f, maxWidth, energyNormalized);
                lineRenderer.startWidth = width;
                lineRenderer.endWidth = width;
            }
            else
            {
                // SWING MODE: Width based on energy and tension
                float widthFactor = Mathf.Max(energyNormalized, tensionScalar * 0.5f);
                float width = Mathf.Lerp(baseWidth, maxWidth, widthFactor);
                lineRenderer.startWidth = width;
                lineRenderer.endWidth = width;
            }
        }
        
        // Color based on mode and tension state
        if (energyColorGradient != null)
        {
            if (isGrappleMode)
            {
                // GRAPPLE MODE: Warm colors (yellow -> orange -> red)
                // Indicates active retraction, high force
                float grappleT = Mathf.Clamp01(energyNormalized * 1.5f); // Shift toward warm end
                Color baseColor = energyColorGradient.Evaluate(grappleT);
                Color warmColor = new Color(
                    Mathf.Max(baseColor.r, 1f),
                    Mathf.Max(baseColor.g * 0.5f, 0.3f),
                    baseColor.b * 0.3f
                );
                lineRenderer.startColor = warmColor;
                lineRenderer.endColor = warmColor;
            }
            else
            {
                // SWING MODE: Cool -> warm gradient (cyan -> purple -> magenta)
                // Use tension for color when rope is taut, energy when slack
                float colorT = tensionScalar > 0.5f ? tensionScalar : energyNormalized;
                Color ropeColor = energyColorGradient.Evaluate(colorT);
                lineRenderer.startColor = ropeColor;
                lineRenderer.endColor = ropeColor;
            }
        }
    }
    
    Vector3 GetRopeStartPosition()
    {
        if (useHandEmitPoint && handEmitPoint != null)
        {
            return handEmitPoint.position;
        }
        return transform.position;
    }
    
    System.Collections.IEnumerator RetractAnimation()
    {
        float retractProgress = 0f;
        Vector3 startPos = GetRopeStartPosition();
        
        while (retractProgress < 1f && lineRenderer != null)
        {
            retractProgress += Time.deltaTime * retractAnimationSpeed;
            
            // Animate rope retracting back to player
            if (lineRenderer.positionCount == 2)
            {
                lineRenderer.SetPosition(1, Vector3.Lerp(currentAnchor, startPos, retractProgress));
            }
            else
            {
                // Curve retraction
                for (int i = 0; i < lineRenderer.positionCount; i++)
                {
                    float t = i / (float)(lineRenderer.positionCount - 1);
                    t *= (1f - retractProgress); // Shrink from end
                    Vector3 point = Vector3.Lerp(startPos, currentAnchor, t);
                    lineRenderer.SetPosition(i, point);
                }
            }
            
            yield return null;
        }
        
        // Cleanup
        CleanupVisuals();
    }
    
    void CleanupVisuals()
    {
        if (ropeLineInstance != null)
        {
            Destroy(ropeLineInstance);
            ropeLineInstance = null;
            lineRenderer = null;
        }
        
        if (anchorParticlesInstance != null)
        {
            Destroy(anchorParticlesInstance);
            anchorParticlesInstance = null;
        }
        
        if (ropeTrailParticles != null)
        {
            foreach (GameObject particle in ropeTrailParticles)
            {
                if (particle != null) Destroy(particle);
            }
            ropeTrailParticles = null;
        }
    }
    
    void OnDisable()
    {
        CleanupVisuals();
    }
    
    void OnDestroy()
    {
        CleanupVisuals();
    }
}
