using UnityEngine;

/// <summary>
/// Speed particle controller with smooth transitions.
/// Particles emit from momentum direction (trails behind player).
/// </summary>
public class SpeedParticleController : MonoBehaviour
{
    [Header("=== SPEED SETTINGS ===")]
    [SerializeField] private float speedThreshold = 1800f;
    [SerializeField] private float maxSpeedForParticles = 5000f;
    
    [Header("=== AIR BOOST ===")]
    [Tooltip("Boost particle intensity when airborne (makes aerial momentum more visible)")]
    [SerializeField] private bool boostInAir = true;
    [SerializeField] private float airIntensityMultiplier = 1.3f;
    
    [Header("=== REFERENCES ===")]
    [SerializeField] private ParticleSystem speedParticles;
    [SerializeField] private AAAMovementController movementController;
    
    [Header("=== VISUAL SETTINGS ===")]
    [SerializeField] private float transitionSmoothTime = 0.15f;
    [SerializeField] private float rotationSmoothTime = 0.2f;
    [SerializeField] private bool debugMode = false;
    
    // Runtime state
    private float currentEmissionRate = 0f;
    private float emissionVelocity = 0f;
    private ParticleSystem.EmissionModule emissionModule;
    private float baseEmissionRate;
    private Quaternion targetRotation;
    private Quaternion rotationVelocity;
    
    private void Start()
    {
        // Use GameManager caching for performance
        if (movementController == null)
        {
            if (GameManager.Instance != null)
                movementController = GameManager.Instance.GetAAAMovementController();
            else
                movementController = FindObjectOfType<AAAMovementController>();
        }
            
        if (speedParticles == null)
            speedParticles = GetComponent<ParticleSystem>();
            
        if (speedParticles != null)
        {
            emissionModule = speedParticles.emission;
            baseEmissionRate = emissionModule.rateOverTime.constant;
            
            // Start playing but with zero emission
            if (!speedParticles.isPlaying)
                speedParticles.Play();
                
            emissionModule.rateOverTime = 0f;
            currentEmissionRate = 0f;
        }
        
        // Initialize rotation
        targetRotation = transform.rotation;
    }
    
    private void Update()
    {
        if (movementController == null || speedParticles == null) return;
        
        float currentSpeed = movementController.CurrentSpeed;
        
        // Get FULL velocity (including vertical component for up/down visualization!)
        Vector3 velocity = movementController.Velocity;
        
        // Point particles AWAY from movement direction with SMOOTH rotation
        // Use full 3D velocity so particles reflect vertical movement too!
        if (velocity.magnitude > 1f)
        {
            // Calculate target rotation (opposite of velocity direction)
            targetRotation = Quaternion.LookRotation(-velocity.normalized);
            
            // Smoothly interpolate to target rotation (fixes jerky left/right movement)
            transform.rotation = SmoothDampQuaternion(
                transform.rotation,
                targetRotation,
                ref rotationVelocity,
                rotationSmoothTime
            );
        }
        
        // Calculate target emission rate
        float targetEmissionRate = 0f;
        
        if (currentSpeed > speedThreshold)
        {
            // Normalize speed between threshold and max
            float speedFactor = Mathf.InverseLerp(speedThreshold, maxSpeedForParticles, currentSpeed);
            speedFactor = Mathf.Clamp01(speedFactor);
            
            // Scale emission (minimum 30% at threshold, 100% at max speed)
            targetEmissionRate = baseEmissionRate * Mathf.Lerp(0.3f, 1f, speedFactor);
            
            // Boost intensity when airborne (aerial momentum visualization!)
            if (boostInAir && !movementController.IsGrounded)
            {
                targetEmissionRate *= airIntensityMultiplier;
            }
        }
        
        // Smooth transition
        currentEmissionRate = Mathf.SmoothDamp(
            currentEmissionRate,
            targetEmissionRate,
            ref emissionVelocity,
            transitionSmoothTime
        );
        
        // Apply emission rate
        emissionModule.rateOverTime = currentEmissionRate;
        
        // Scale particle speed with player speed
        var main = speedParticles.main;
        float speedScale = Mathf.InverseLerp(speedThreshold, maxSpeedForParticles, currentSpeed);
        main.startSpeedMultiplier = 1f + (speedScale * 1.5f);
        
        if (debugMode && Time.frameCount % 60 == 0)
        {
            Debug.Log($"Speed: {currentSpeed:F0} | Emission: {currentEmissionRate:F1}");
        }
    }
    
    /// <summary>
    /// Smooth quaternion interpolation similar to SmoothDamp for floats.
    /// Provides butter-smooth rotation without jerky snapping.
    /// </summary>
    private Quaternion SmoothDampQuaternion(Quaternion current, Quaternion target, ref Quaternion velocity, float smoothTime)
    {
        if (smoothTime <= 0f) return target;
        
        // Use Slerp with adaptive speed based on angle difference
        float angle = Quaternion.Angle(current, target);
        float t = Mathf.Clamp01(Time.deltaTime / smoothTime);
        
        // Faster interpolation for small angles, smoother for large angles
        t = Mathf.SmoothStep(0f, 1f, t);
        
        return Quaternion.Slerp(current, target, t);
    }
    
    [ContextMenu("Auto Setup Speed Particles")]
    private void AutoSetupSpeedParticles()
    {
        // Get or create particle system
        if (speedParticles == null)
        {
            speedParticles = GetComponent<ParticleSystem>();
            if (speedParticles == null)
            {
                speedParticles = gameObject.AddComponent<ParticleSystem>();
            }
        }
        
        // MAIN MODULE (Scaled for 320-height player!)
        var main = speedParticles.main;
        main.duration = 1.5f;
        main.loop = true;
        main.startLifetime = 1f;
        main.startSpeed = 4500f; // Fast particles!
        main.startSize = 50f; // Scaled up for 320-height player (was 8)
        main.startColor = new Color(0.3f, 0.8f, 1f, 0.8f); // Cyan
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local; // CRITICAL!
        main.maxParticles = 5000;
        main.playOnAwake = false;
        
        // EMISSION
        var emission = speedParticles.emission;
        emission.rateOverTime = 30f; // More particles for larger scale (was 10)
        
        // SHAPE - Cone pointing forward (Scaled for 50-radius player)
        var shape = speedParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f; // Tight cone
        shape.radius = 30f; // Scaled for 50-radius player (was 0.1)
        shape.radiusThickness = 1f;
        shape.arc = 360f;
        shape.arcMode = ParticleSystemShapeMultiModeValue.Random;
        
        // VELOCITY OVER LIFETIME - Makes particles stream backwards!
        var velocityOverLifetime = speedParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.x = 0f;
        velocityOverLifetime.y = 0f;
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-150f); // Stream backwards in local Z
        
        // SIZE OVER LIFETIME - Fade out
        var sizeOverLifetime = speedParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(0.7f, 0.5f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // COLOR OVER LIFETIME - Fade alpha
        var colorOverLifetime = speedParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.white, 0f), 
                new GradientColorKey(Color.white, 1f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f), 
                new GradientAlphaKey(0.5f, 0.5f),
                new GradientAlphaKey(0f, 1f) 
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);
        
        // TRAILS - The important part!
        var trails = speedParticles.trails;
        trails.enabled = true;
        trails.mode = ParticleSystemTrailMode.Ribbon;
        trails.ratio = 1f; // All particles have trails
        trails.lifetime = 0.4f;
        trails.minVertexDistance = 0.2f;
        trails.worldSpace = false; // Follow particles in local space
        trails.dieWithParticles = true;
        trails.sizeAffectsWidth = true;
        trails.sizeAffectsLifetime = false;
        trails.inheritParticleColor = true;
        
        // Trail width curve (taper)
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0f, 1f);
        widthCurve.AddKey(1f, 0.2f);
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, widthCurve);
        
        // RENDERER
        var renderer = speedParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.minParticleSize = 0f;
        renderer.maxParticleSize = 1000f;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.trailMaterial = renderer.trailMaterial; // Keep existing or use default
        
        Debug.Log("✅ Speed particle system configured! Trails enabled, particles stream backwards in local space.");
    }
}
