using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enhanced Platform Controller for Gemini Gauntlet
/// Adds advanced features to individual platforms including:
/// - Dynamic lighting effects
/// - Particle systems
/// - Interactive elements
/// - Sound effects
/// - Animation systems
/// </summary>
public class EnhancedPlatformController : MonoBehaviour
{
    [Header("Platform Configuration")]
    [SerializeField] private PlatformType platformType = PlatformType.Large;
    [SerializeField] private bool enableEnhancements = true;
    
    [Header("Visual Effects")]
    [SerializeField] private bool enableGlowEffect = true;
    [SerializeField] private bool enableParticleEffects = true;
    [SerializeField] private bool enablePulseAnimation = true;
    [SerializeField] private Color glowColor = Color.cyan;
    [SerializeField] private float glowIntensity = 2f;
    
    [Header("Lighting")]
    [SerializeField] private bool enableDynamicLighting = true;
    [SerializeField] private Light platformLight;
    [SerializeField] private float lightRange = 1000f;
    [SerializeField] private float lightIntensity = 1f;
    [SerializeField] private bool enableLightFlicker = true;
    
    [Header("Particle Systems")]
    [SerializeField] private ParticleSystem ambientParticles;
    [SerializeField] private ParticleSystem activationParticles;
    [SerializeField] private bool enableFloatingParticles = true;
    [SerializeField] private int particleCount = 50;
    
    [Header("Animation")]
    [SerializeField] private bool enableFloatingAnimation = true;
    [SerializeField] private float floatAmplitude = 50f;
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private bool enableRotationAnimation = false;
    [SerializeField] private float rotationSpeed = 10f;
    
    [Header("Interactive Features")]
    [SerializeField] private bool enablePlayerDetection = true;
    [SerializeField] private float detectionRange = 2000f;
    [SerializeField] private bool enableActivationEffects = true;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip activationSound;
    [SerializeField] private AudioClip ambientSound;
    [SerializeField] private bool enableSpatialAudio = true;
    
    [Header("Performance")]
    [SerializeField] private bool enableLOD = true;
    [SerializeField] private float highDetailDistance = 5000f;
    [SerializeField] private float mediumDetailDistance = 10000f;
    
    // Internal components
    private Renderer platformRenderer;
    private Material originalMaterial;
    private Material enhancedMaterial;
    private Transform playerTransform;
    private Coroutine animationCoroutine;
    private Coroutine effectsCoroutine;
    
    // State tracking
    private bool isPlayerNear = false;
    private bool isActivated = false;
    private float originalY;
    private Vector3 originalScale;
    private LODLevel currentLOD = LODLevel.High;
    
    // Performance optimization
    private float lastPlayerDistanceCheck = 0f;
    private float playerDistanceCheckInterval = 0.5f;
    
    private enum LODLevel
    {
        High,
        Medium,
        Low,
        Disabled
    }
    
    void Start()
    {
        InitializePlatform();
        StartEnhancements();
    }
    
    void InitializePlatform()
    {
        // Cache components
        platformRenderer = GetComponent<Renderer>();
        originalY = transform.position.y;
        originalScale = transform.localScale;
        
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerTransform = playerObj.transform;
        
        // Setup materials
        if (platformRenderer != null)
        {
            originalMaterial = platformRenderer.material;
            CreateEnhancedMaterial();
        }
        
        // Setup lighting
        SetupDynamicLighting();
        
        // Setup particle systems
        SetupParticleSystems();
        
        // Setup audio
        SetupAudio();
        
        Debug.Log($"[EnhancedPlatformController] Initialized {platformType} platform: {gameObject.name}");
    }
    
    void CreateEnhancedMaterial()
    {
        if (originalMaterial == null) return;
        
        enhancedMaterial = new Material(originalMaterial);
        
        // Enable emission for glow effect
        if (enableGlowEffect)
        {
            enhancedMaterial.EnableKeyword("_EMISSION");
            enhancedMaterial.SetColor("_EmissionColor", glowColor * glowIntensity);
        }
        
        // Apply enhanced material
        platformRenderer.material = enhancedMaterial;
    }
    
    void SetupDynamicLighting()
    {
        if (!enableDynamicLighting) return;
        
        if (platformLight == null)
        {
            GameObject lightObj = new GameObject("PlatformLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.up * 200f;
            
            platformLight = lightObj.AddComponent<Light>();
        }
        
        platformLight.type = LightType.Point;
        platformLight.range = lightRange;
        platformLight.intensity = lightIntensity;
        platformLight.color = glowColor;
        
        // Adjust light settings based on platform type
        if (platformType == PlatformType.Large)
        {
            platformLight.range *= 2f;
            platformLight.intensity *= 1.5f;
        }
    }
    
    void SetupParticleSystems()
    {
        if (!enableParticleEffects) return;
        
        // Create ambient particles
        if (ambientParticles == null && enableFloatingParticles)
        {
            GameObject particleObj = new GameObject("AmbientParticles");
            particleObj.transform.SetParent(transform);
            particleObj.transform.localPosition = Vector3.up * 100f;
            
            ambientParticles = particleObj.AddComponent<ParticleSystem>();
            ConfigureAmbientParticles();
        }
        
        // Create activation particles
        if (activationParticles == null)
        {
            GameObject activationObj = new GameObject("ActivationParticles");
            activationObj.transform.SetParent(transform);
            activationObj.transform.localPosition = Vector3.up * 150f;
            
            activationParticles = activationObj.AddComponent<ParticleSystem>();
            ConfigureActivationParticles();
        }
    }
    
    void ConfigureAmbientParticles()
    {
        if (ambientParticles == null) return;
        
        var main = ambientParticles.main;
        main.startLifetime = 10f;
        main.startSpeed = 20f;
        main.startSize = platformType == PlatformType.Large ? 30f : 15f;
        main.startColor = glowColor;
        main.maxParticles = particleCount;
        
        var emission = ambientParticles.emission;
        emission.rateOverTime = particleCount * 0.1f;
        
        var shape = ambientParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = platformType == PlatformType.Large ? 
            new Vector3(3000f, 200f, 3000f) : 
            new Vector3(200f, 100f, 200f);
        
        var velocityOverLifetime = ambientParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(10f, 50f);
        
        var colorOverLifetime = ambientParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(glowColor, 0.0f), 
                new GradientColorKey(glowColor, 1.0f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0.0f, 0.0f), 
                new GradientAlphaKey(1.0f, 0.2f), 
                new GradientAlphaKey(1.0f, 0.8f), 
                new GradientAlphaKey(0.0f, 1.0f) 
            }
        );
        colorOverLifetime.color = gradient;
    }
    
    void ConfigureActivationParticles()
    {
        if (activationParticles == null) return;
        
        var main = activationParticles.main;
        main.startLifetime = 2f;
        main.startSpeed = 100f;
        main.startSize = platformType == PlatformType.Large ? 50f : 25f;
        main.startColor = Color.white;
        main.maxParticles = 200;
        
        var emission = activationParticles.emission;
        emission.enabled = false; // We'll trigger this manually
        
        var shape = activationParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = platformType == PlatformType.Large ? 1500f : 100f;
        
        var velocityOverLifetime = activationParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(200f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(100f, 300f);
        
        // Stop playing initially
        activationParticles.Stop();
    }
    
    void SetupAudio()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.spatialBlend = enableSpatialAudio ? 1f : 0f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.maxDistance = detectionRange;
        audioSource.volume = 0.3f;
        
        // Play ambient sound if available
        if (ambientSound != null)
        {
            audioSource.clip = ambientSound;
            audioSource.loop = true;
            audioSource.Play();
        }
    }
    
    void StartEnhancements()
    {
        if (!enableEnhancements) return;
        
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
        
        if (effectsCoroutine != null)
            StopCoroutine(effectsCoroutine);
        
        animationCoroutine = StartCoroutine(AnimationLoop());
        effectsCoroutine = StartCoroutine(EffectsLoop());
    }
    
    IEnumerator AnimationLoop()
    {
        float time = 0f;
        
        while (enableEnhancements)
        {
            time += Time.deltaTime;
            
            // Floating animation
            if (enableFloatingAnimation)
            {
                float floatOffset = Mathf.Sin(time * floatSpeed) * floatAmplitude;
                Vector3 newPosition = transform.position;
                newPosition.y = originalY + floatOffset;
                transform.position = newPosition;
            }
            
            // Rotation animation
            if (enableRotationAnimation)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }
            
            // Pulse animation for glow
            if (enablePulseAnimation && enhancedMaterial != null)
            {
                float pulseIntensity = (Mathf.Sin(time * 2f) + 1f) * 0.5f * glowIntensity;
                enhancedMaterial.SetColor("_EmissionColor", glowColor * pulseIntensity);
            }
            
            // Light flicker
            if (enableLightFlicker && platformLight != null)
            {
                float flicker = 1f + Mathf.Sin(time * 10f) * 0.1f;
                platformLight.intensity = lightIntensity * flicker;
            }
            
            yield return null;
        }
    }
    
    IEnumerator EffectsLoop()
    {
        while (enableEnhancements)
        {
            // Check player distance periodically for performance
            if (Time.time - lastPlayerDistanceCheck > playerDistanceCheckInterval)
            {
                CheckPlayerProximity();
                UpdateLOD();
                lastPlayerDistanceCheck = Time.time;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    void CheckPlayerProximity()
    {
        if (!enablePlayerDetection || playerTransform == null) return;
        
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        bool wasPlayerNear = isPlayerNear;
        isPlayerNear = distance <= detectionRange;
        
        // Player entered detection range
        if (isPlayerNear && !wasPlayerNear)
        {
            OnPlayerEnterRange();
        }
        // Player left detection range
        else if (!isPlayerNear && wasPlayerNear)
        {
            OnPlayerExitRange();
        }
    }
    
    void OnPlayerEnterRange()
    {
        if (!enableActivationEffects) return;
        
        Debug.Log($"[EnhancedPlatformController] Player entered range of {gameObject.name}");
        
        // Trigger activation effects
        if (activationParticles != null)
        {
            activationParticles.Emit(50);
        }
        
        // Play activation sound
        if (activationSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(activationSound);
        }
        
        // Enhance glow
        if (enhancedMaterial != null)
        {
            StartCoroutine(EnhanceGlow());
        }
        
        isActivated = true;
    }
    
    void OnPlayerExitRange()
    {
        Debug.Log($"[EnhancedPlatformController] Player exited range of {gameObject.name}");
        isActivated = false;
    }
    
    IEnumerator EnhanceGlow()
    {
        float duration = 2f;
        float time = 0f;
        float originalGlow = glowIntensity;
        float enhancedGlow = glowIntensity * 3f;
        
        // Glow up
        while (time < duration * 0.5f)
        {
            time += Time.deltaTime;
            float intensity = Mathf.Lerp(originalGlow, enhancedGlow, time / (duration * 0.5f));
            if (enhancedMaterial != null)
                enhancedMaterial.SetColor("_EmissionColor", glowColor * intensity);
            yield return null;
        }
        
        // Glow down
        time = 0f;
        while (time < duration * 0.5f)
        {
            time += Time.deltaTime;
            float intensity = Mathf.Lerp(enhancedGlow, originalGlow, time / (duration * 0.5f));
            if (enhancedMaterial != null)
                enhancedMaterial.SetColor("_EmissionColor", glowColor * intensity);
            yield return null;
        }
    }
    
    void UpdateLOD()
    {
        if (!enableLOD || playerTransform == null) return;
        
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        LODLevel newLOD;
        
        if (distance <= highDetailDistance)
            newLOD = LODLevel.High;
        else if (distance <= mediumDetailDistance)
            newLOD = LODLevel.Medium;
        else
            newLOD = LODLevel.Low;
        
        if (newLOD != currentLOD)
        {
            ApplyLOD(newLOD);
            currentLOD = newLOD;
        }
    }
    
    void ApplyLOD(LODLevel lod)
    {
        switch (lod)
        {
            case LODLevel.High:
                // Full detail
                if (ambientParticles != null) ambientParticles.gameObject.SetActive(true);
                if (platformLight != null) platformLight.enabled = true;
                enablePulseAnimation = true;
                break;
                
            case LODLevel.Medium:
                // Reduced detail
                if (ambientParticles != null) ambientParticles.gameObject.SetActive(true);
                if (platformLight != null) platformLight.enabled = true;
                enablePulseAnimation = false;
                break;
                
            case LODLevel.Low:
                // Minimal detail
                if (ambientParticles != null) ambientParticles.gameObject.SetActive(false);
                if (platformLight != null) platformLight.enabled = false;
                enablePulseAnimation = false;
                break;
        }
    }
    
    // Public API methods
    public void ActivatePlatform()
    {
        OnPlayerEnterRange();
    }
    
    public void SetGlowColor(Color color)
    {
        glowColor = color;
        if (enhancedMaterial != null)
            enhancedMaterial.SetColor("_EmissionColor", glowColor * glowIntensity);
        if (platformLight != null)
            platformLight.color = glowColor;
    }
    
    public void SetEnhancementsEnabled(bool enabled)
    {
        enableEnhancements = enabled;
        if (enabled)
            StartEnhancements();
        else
        {
            if (animationCoroutine != null) StopCoroutine(animationCoroutine);
            if (effectsCoroutine != null) StopCoroutine(effectsCoroutine);
        }
    }
    
    void OnDestroy()
    {
        // Cleanup
        if (enhancedMaterial != null)
            DestroyImmediate(enhancedMaterial);
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw detection range
        if (enablePlayerDetection)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
        
        // Draw LOD ranges
        if (enableLOD)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, highDetailDistance);
            Gizmos.color = Color.orange;
            Gizmos.DrawWireSphere(transform.position, mediumDetailDistance);
        }
    }
}
