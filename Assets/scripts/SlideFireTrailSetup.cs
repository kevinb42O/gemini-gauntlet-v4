using UnityEngine;

/// <summary>
/// 🔥 FIRE TRAIL SETUP: Configures a particle system to leave ground fire trails while sliding
/// Attach to the same GameObject as your slide particle system (FireWall)
/// Click [Context Menu > Setup Fire Trail Particles] to auto-configure
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class SlideFireTrailSetup : MonoBehaviour
{
    [Header("=== 🔥 FIRE TRAIL CONFIGURATION ===")]
    [Tooltip("The particle system to configure as a fire trail (auto-detected if null)")]
    [SerializeField] private ParticleSystem fireTrailParticles;
    
    [Header("=== 🎨 VISUAL SETTINGS ===")]
    [Tooltip("Fire color gradient (default: orange/yellow/red)")]
    [SerializeField] private bool useCustomGradient = false;
    [SerializeField] private Gradient customFireGradient;
    
    [Header("=== 🎯 TRAIL SETTINGS ===")]
    [Tooltip("How long the fire trail stays on the ground (seconds)")]
    [SerializeField] private float trailDuration = 2.0f; // Longer for visible trail
    [Tooltip("Base emission rate (particles/second) - controlled by slide speed")]
    [SerializeField] private float baseEmissionRate = 30f; // Lower since trails do the work
    [Tooltip("Base particle size (scaled for 320-unit character)")]
    [SerializeField] private float baseParticleSize = 40f;
    
    private void Awake()
    {
        if (fireTrailParticles == null)
            fireTrailParticles = GetComponent<ParticleSystem>();
    }
    
    [ContextMenu("Setup Fire Trail Particles")]
    public void SetupFireTrailParticles()
    {
        if (fireTrailParticles == null)
        {
            fireTrailParticles = GetComponent<ParticleSystem>();
            if (fireTrailParticles == null)
            {
                Debug.LogError("❌ No ParticleSystem found! Add a ParticleSystem component first.");
                return;
            }
        }
        
        Debug.Log("🔥 Setting up FIRE TRAIL particles for ground sliding...");
        
        // ═══════════════════════════════════════════════════════════════════
        // MAIN MODULE - Core fire particle settings
        // ═══════════════════════════════════════════════════════════════════
        var main = fireTrailParticles.main;
        main.duration = 5f; // Long duration for continuous sliding
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, trailDuration); // Random variation
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 5f); // REDUCED: Minimal drift so particles stay near ground
        main.startSize = new ParticleSystem.MinMaxCurve(baseParticleSize * 0.7f, baseParticleSize * 1.3f); // Size variation
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad); // Random rotation
        main.startColor = GetFireGradient(); // Orange/yellow fire colors
        main.gravityModifier = 0f; // DISABLED: No gravity - particles stay exactly where emitted!
        main.simulationSpace = ParticleSystemSimulationSpace.World; // CRITICAL: Leave trail on ground!
        main.maxParticles = 3000;
        main.playOnAwake = false;
        
        // ═══════════════════════════════════════════════════════════════════
        // EMISSION - Controlled by CleanAAACrouch based on slide speed
        // ═══════════════════════════════════════════════════════════════════
        var emission = fireTrailParticles.emission;
        emission.enabled = true;
        emission.rateOverTime = baseEmissionRate; // Base rate (updated dynamically)
        
        // CRITICAL: Ensure particle system starts inactive (CleanAAACrouch will activate it)
        if (fireTrailParticles.isPlaying)
            fireTrailParticles.Stop();
        fireTrailParticles.gameObject.SetActive(false); // Start disabled - CleanAAACrouch enables when sliding
        
        // ═══════════════════════════════════════════════════════════════════
        // SHAPE - Emit from bottom of player (ground contact point)
        // ═══════════════════════════════════════════════════════════════════
        var shape = fireTrailParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 10f; // TINY radius = precise ground contact (scaled for 320-unit character)
        shape.radiusThickness = 1f; // Emit from surface only
        shape.position = new Vector3(0f, -160f, 0f); // EXACTLY at ground level (320/2 = 160)
        shape.alignToDirection = false; // CRITICAL: Don't align particles to movement direction!
        shape.randomDirectionAmount = 0f; // No random direction - spawn in place
        
        // ═══════════════════════════════════════════════════════════════════
        // VELOCITY OVER LIFETIME - DISABLED for ground trail (no drift!)
        // ═══════════════════════════════════════════════════════════════════
        var velocityOverLifetime = fireTrailParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = false; // DISABLED: Particles stay exactly where emitted!
        
        // ═══════════════════════════════════════════════════════════════════
        // SIZE OVER LIFETIME - Fire grows then fades
        // ═══════════════════════════════════════════════════════════════════
        var sizeOverLifetime = fireTrailParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);   // Start small
        sizeCurve.AddKey(0.2f, 1f);   // Quickly grow
        sizeCurve.AddKey(0.8f, 0.8f); // Stay visible
        sizeCurve.AddKey(1f, 0f);     // Fade out
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // ═══════════════════════════════════════════════════════════════════
        // COLOR OVER LIFETIME - Fire color transition (bright orange → red → black)
        // ═══════════════════════════════════════════════════════════════════
        var colorOverLifetime = fireTrailParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        
        Gradient fireGradient = new Gradient();
        fireGradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(new Color(1f, 0.9f, 0.4f), 0f),  // Bright yellow (hot)
                new GradientColorKey(new Color(1f, 0.5f, 0.1f), 0.3f), // Orange
                new GradientColorKey(new Color(0.8f, 0.2f, 0.1f), 0.7f), // Red
                new GradientColorKey(new Color(0.1f, 0.05f, 0.05f), 1f)  // Dark ember
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0.8f, 0f),   // Start visible
                new GradientAlphaKey(0.9f, 0.2f), // Peak brightness
                new GradientAlphaKey(0.6f, 0.6f), // Fade
                new GradientAlphaKey(0f, 1f)      // Fully transparent
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fireGradient);
        
        // ═══════════════════════════════════════════════════════════════════
        // ROTATION OVER LIFETIME - Fire flickers
        // ═══════════════════════════════════════════════════════════════════
        var rotationOverLifetime = fireTrailParticles.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-180f * Mathf.Deg2Rad, 180f * Mathf.Deg2Rad);
        
        // ═══════════════════════════════════════════════════════════════════
        // NOISE - DISABLED for ground trail (prevents drift away from spawn point)
        // ═══════════════════════════════════════════════════════════════════
        var noise = fireTrailParticles.noise;
        noise.enabled = false; // DISABLED: No turbulence = particles stay on ground!
        
        // ═══════════════════════════════════════════════════════════════════
        // RENDERER - Billboard mode for visibility from all angles
        // ═══════════════════════════════════════════════════════════════════
        var renderer = fireTrailParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.minParticleSize = 0f;
        renderer.maxParticleSize = 2000f;
        renderer.alignment = ParticleSystemRenderSpace.World; // CRITICAL: Particles face camera in WORLD space, not local!
        renderer.allowRoll = false; // Don't rotate with emitter!
        
        // ═══════════════════════════════════════════════════════════════════
        // TRAILS MODULE - CRITICAL for drawing continuous path behind player!
        // ═══════════════════════════════════════════════════════════════════
        var trails = fireTrailParticles.trails;
        trails.enabled = true; // ENABLED: This draws the actual trail path!
        trails.mode = ParticleSystemTrailMode.PerParticle; // Each particle leaves a ribbon trail
        trails.ratio = 1f; // All particles have trails
        trails.lifetime = trailDuration * 1.5f; // Trail stays longer than particle
        trails.minVertexDistance = 5f; // How often to add trail vertices (lower = smoother but more expensive)
        trails.worldSpace = true; // CRITICAL: Trail stays in world space (on ground)!
        trails.dieWithParticles = false; // Trail persists after particle dies
        trails.ribbonCount = 1; // Single ribbon per particle
        trails.splitSubEmitterRibbons = false;
        trails.attachRibbonsToTransform = false; // CRITICAL: Don't attach to player transform!
        trails.textureMode = ParticleSystemTrailTextureMode.Stretch;
        trails.sizeAffectsWidth = true;
        trails.sizeAffectsLifetime = false;
        trails.inheritParticleColor = true;
        trails.colorOverLifetime = new ParticleSystem.MinMaxGradient(Color.white); // Use particle color
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f); // Constant width
        trails.colorOverTrail = new ParticleSystem.MinMaxGradient(Color.white); // Fade handled by particle color
        
        // TRAIL MATERIAL: Assign default particle material if none exists (reuse renderer variable from above)
        if (renderer.trailMaterial == null)
        {
            renderer.trailMaterial = renderer.sharedMaterial; // Use same material as particles
        }
        
        Debug.Log("✅ Fire trail particles configured! Settings:");
        Debug.Log($"   • Simulation Space: {main.simulationSpace} (WORLD = leaves trail on ground)");
        Debug.Log($"   • Emission Shape: Sphere at ground level (Y: -150)");
        Debug.Log($"   • Particle Lifetime: {trailDuration}s (how long fire stays)");
        Debug.Log($"   • Base Emission Rate: {baseEmissionRate} particles/sec");
        Debug.Log($"   • Gravity: {main.gravityModifier} (negative = rises like fire)");
        Debug.Log("\n🎮 HOW TO USE:");
        Debug.Log("   1. Assign this particle system to CleanAAACrouch.slideParticles");
        Debug.Log("   2. Enable 'Slide Particles Enabled' in CleanAAACrouch");
        Debug.Log("   3. Slide to see fire trail left behind on the ground!");
    }
    
    private Color GetFireGradient()
    {
        if (useCustomGradient && customFireGradient != null)
        {
            return customFireGradient.Evaluate(Random.value);
        }
        
        // Default fire color (bright orange)
        return new Color(1f, 0.7f, 0.3f, 0.9f);
    }
    
    [ContextMenu("Test Fire Trail (5 seconds)")]
    public void TestFireTrail()
    {
        if (fireTrailParticles == null)
        {
            Debug.LogError("❌ No particle system assigned!");
            return;
        }
        
        Debug.Log("🔥 Testing fire trail for 5 seconds...");
        fireTrailParticles.gameObject.SetActive(true);
        fireTrailParticles.Play();
        
        Invoke(nameof(StopTest), 5f);
    }
    
    private void StopTest()
    {
        if (fireTrailParticles != null)
        {
            fireTrailParticles.Stop();
            Debug.Log("✅ Fire trail test complete!");
        }
    }
}
