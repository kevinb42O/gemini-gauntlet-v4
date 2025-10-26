using UnityEngine;

/// <summary>
/// AAA HEAD COLLISION SYSTEM - CONFIGURATION
/// 
/// Handles upward collision damage (hitting head on ceilings/overhangs during jumps/grappling)
/// Physics-based damage with realistic bounce-back and camera effects
/// 
/// Design Philosophy:
/// - Velocity-based damage (faster = more damage)
/// - Realistic physics response (bounce back down)
/// - Integrated with existing trauma/audio systems
/// - No magic numbers - all values configurable
/// 
/// Author: Professional AAA Implementation
/// Date: 2025-10-25
/// Scale: Tuned for 320-unit player characters
/// </summary>
[CreateAssetMenu(fileName = "HeadCollisionConfig", menuName = "Game/Head Collision Configuration", order = 3)]
public class HeadCollisionConfig : ScriptableObject
{
    [Header("=== 🎯 COLLISION DETECTION ===")]
    [Tooltip("Enable head collision damage system")]
    public bool enableHeadCollisionDamage = true;
    
    [Tooltip("Minimum upward velocity to register head collision (units/s)")]
    public float minVelocityThreshold = 300f; // Light jump upward speed
    
    [Tooltip("Maximum angle from straight up to count as head collision (degrees)")]
    [Range(0f, 45f)]
    public float headCollisionAngleThreshold = 30f; // Within 30° of vertical
    
    [Tooltip("Cooldown between head collision damage events (prevents spam)")]
    public float collisionCooldown = 0.3f;
    
    
    [Header("=== 💥 DAMAGE CALCULATION (VELOCITY-BASED) ===")]
    [Tooltip("Velocity threshold for light head collision damage (units/s)")]
    public float lightCollisionVelocity = 500f; // Weak jump
    
    [Tooltip("Velocity threshold for moderate head collision damage (units/s)")]
    public float moderateCollisionVelocity = 1200f; // Full jump
    
    [Tooltip("Velocity threshold for severe head collision damage (units/s)")]
    public float severeCollisionVelocity = 2500f; // Grapple/aerial trick launch
    
    [Tooltip("Damage at light collision velocity")]
    public float lightCollisionDamage = 150f; // Minor bump
    
    [Tooltip("Damage at moderate collision velocity")]
    public float moderateCollisionDamage = 500f; // Solid hit
    
    [Tooltip("Damage at severe collision velocity")]
    public float severeCollisionDamage = 1200f; // Brutal impact
    
    [Tooltip("Damage scaling curve (velocity to damage multiplier)")]
    public AnimationCurve damageScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    
    [Header("=== 🔄 BOUNCE-BACK PHYSICS ===")]
    [Tooltip("Bounce-back coefficient (0 = no bounce, 1 = perfect elastic)")]
    [Range(0f, 1f)]
    public float bounceCoefficient = 0.5f; // Realistic 50% energy loss
    
    [Tooltip("Minimum bounce-back velocity (prevents tiny bounces)")]
    public float minBounceVelocity = 200f;
    
    [Tooltip("Maximum bounce-back velocity (caps extreme cases)")]
    public float maxBounceVelocity = 2000f;
    
    [Tooltip("Horizontal velocity dampening on collision (prevents skating on ceiling)")]
    [Range(0f, 1f)]
    public float horizontalDampening = 0.7f; // Keep 70% of horizontal speed
    
    [Tooltip("Add small outward push from surface (prevents sticking)")]
    public float surfacePushForce = 50f;
    
    
    [Header("=== 🪝 ROPE/GRAPPLING COLLISION PHYSICS ===")]
    [Tooltip("Enable special collision handling when attached to rope")]
    public bool enableRopeCollisionHandling = true;
    
    [Tooltip("Rope bounce coefficient (REDUCED for predictable control)")]
    [Range(0f, 1f)]
    public float ropeBounceCoefficient = 0.3f; // Much softer bounce while roped
    
    [Tooltip("Rope bounce direction bias - redirects toward anchor (predictable)")]
    [Range(0f, 1f)]
    public float ropeAnchorBias = 0.6f; // 60% toward anchor, 40% pure reflection
    
    [Tooltip("Extra velocity dampening while roped (punishment for collision)")]
    [Range(0f, 0.5f)]
    public float ropeCollisionPenalty = 0.3f; // Lose 30% total speed on collision
    
    [Tooltip("Maximum bounce velocity while roped (more controlled)")]
    public float ropeMaxBounceVelocity = 1200f; // Lower cap for predictability
    
    [Tooltip("Horizontal dampening multiplier while roped (extra drag)")]
    [Range(0f, 1f)]
    public float ropeHorizontalDampeningMultiplier = 0.5f; // Lose more horizontal speed
    
    
    [Header("=== 🎥 CAMERA EFFECTS ===")]
    [Tooltip("Enable camera trauma on head collision")]
    public bool enableCameraTrauma = true;
    
    [Tooltip("Camera trauma intensity at light collision")]
    [Range(0f, 1f)]
    public float lightTraumaIntensity = 0.2f;
    
    [Tooltip("Camera trauma intensity at moderate collision")]
    [Range(0f, 1f)]
    public float moderateTraumaIntensity = 0.4f;
    
    [Tooltip("Camera trauma intensity at severe collision")]
    [Range(0f, 1f)]
    public float severeTraumaIntensity = 0.7f;
    
    
    [Header("=== 🔊 AUDIO ===")]
    [Tooltip("Enable audio feedback on head collision")]
    public bool enableAudio = true;
    
    [Tooltip("Volume at light collision")]
    [Range(0f, 1f)]
    public float lightCollisionVolume = 0.4f;
    
    [Tooltip("Volume at moderate collision")]
    [Range(0f, 1f)]
    public float moderateCollisionVolume = 0.7f;
    
    [Tooltip("Volume at severe collision")]
    [Range(0f, 1f)]
    public float severeCollisionVolume = 1.0f;
    
    
    [Header("=== 🎨 VISUAL FEEDBACK ===")]
    [Tooltip("Enable blood splat effect on severe head collisions")]
    public bool enableBloodSplat = true;
    
    [Tooltip("Blood splat intensity threshold (severity normalized 0-1)")]
    [Range(0f, 1f)]
    public float bloodSplatThreshold = 0.5f; // Show blood above moderate impacts
    
    
    [Header("=== 🐛 DEBUG ===")]
    [Tooltip("Show debug logs for head collisions")]
    public bool showDebugLogs = false;
    
    [Tooltip("Draw debug rays in Scene view")]
    public bool showDebugRays = false;
    
    
    /// <summary>
    /// Calculate damage amount based on collision velocity
    /// </summary>
    public float CalculateDamage(float velocity)
    {
        // Determine damage tier
        if (velocity < lightCollisionVelocity)
        {
            // Below threshold - no damage
            return 0f;
        }
        else if (velocity < moderateCollisionVelocity)
        {
            // Light to moderate range
            float t = Mathf.InverseLerp(lightCollisionVelocity, moderateCollisionVelocity, velocity);
            return Mathf.Lerp(lightCollisionDamage, moderateCollisionDamage, damageScaleCurve.Evaluate(t));
        }
        else if (velocity < severeCollisionVelocity)
        {
            // Moderate to severe range
            float t = Mathf.InverseLerp(moderateCollisionVelocity, severeCollisionVelocity, velocity);
            return Mathf.Lerp(moderateCollisionDamage, severeCollisionDamage, damageScaleCurve.Evaluate(t));
        }
        else
        {
            // Severe+ (cap at max damage)
            return severeCollisionDamage;
        }
    }
    
    /// <summary>
    /// Calculate camera trauma intensity based on collision velocity
    /// </summary>
    public float CalculateTrauma(float velocity)
    {
        if (velocity < lightCollisionVelocity)
            return 0f;
        else if (velocity < moderateCollisionVelocity)
        {
            float t = Mathf.InverseLerp(lightCollisionVelocity, moderateCollisionVelocity, velocity);
            return Mathf.Lerp(lightTraumaIntensity, moderateTraumaIntensity, t);
        }
        else if (velocity < severeCollisionVelocity)
        {
            float t = Mathf.InverseLerp(moderateCollisionVelocity, severeCollisionVelocity, velocity);
            return Mathf.Lerp(moderateTraumaIntensity, severeTraumaIntensity, t);
        }
        else
        {
            return severeTraumaIntensity;
        }
    }
    
    /// <summary>
    /// Calculate audio volume based on collision velocity
    /// </summary>
    public float CalculateAudioVolume(float velocity)
    {
        if (velocity < lightCollisionVelocity)
            return 0f;
        else if (velocity < moderateCollisionVelocity)
        {
            float t = Mathf.InverseLerp(lightCollisionVelocity, moderateCollisionVelocity, velocity);
            return Mathf.Lerp(lightCollisionVolume, moderateCollisionVolume, t);
        }
        else if (velocity < severeCollisionVelocity)
        {
            float t = Mathf.InverseLerp(moderateCollisionVelocity, severeCollisionVelocity, velocity);
            return Mathf.Lerp(moderateCollisionVolume, severeCollisionVolume, t);
        }
        else
        {
            return severeCollisionVolume;
        }
    }
    
    /// <summary>
    /// Get severity classification for debug/UI purposes
    /// </summary>
    public string GetSeverityName(float velocity)
    {
        if (velocity < lightCollisionVelocity)
            return "None";
        else if (velocity < moderateCollisionVelocity)
            return "Light";
        else if (velocity < severeCollisionVelocity)
            return "Moderate";
        else
            return "Severe";
    }
    
    /// <summary>
    /// Calculate normalized severity (0-1)
    /// </summary>
    public float GetSeverityNormalized(float velocity)
    {
        if (velocity < lightCollisionVelocity)
            return 0f;
        else if (velocity < severeCollisionVelocity)
            return Mathf.InverseLerp(lightCollisionVelocity, severeCollisionVelocity, velocity);
        else
            return 1f;
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        // Performance fix: Skip validation during play mode
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        
        // Ensure logical velocity progression
        lightCollisionVelocity = Mathf.Max(0f, lightCollisionVelocity);
        moderateCollisionVelocity = Mathf.Max(lightCollisionVelocity + 100f, moderateCollisionVelocity);
        severeCollisionVelocity = Mathf.Max(moderateCollisionVelocity + 100f, severeCollisionVelocity);
        
        // Ensure logical damage progression
        lightCollisionDamage = Mathf.Max(0f, lightCollisionDamage);
        moderateCollisionDamage = Mathf.Max(lightCollisionDamage, moderateCollisionDamage);
        severeCollisionDamage = Mathf.Max(moderateCollisionDamage, severeCollisionDamage);
        
        // Ensure bounce physics are valid
        minBounceVelocity = Mathf.Max(0f, minBounceVelocity);
        maxBounceVelocity = Mathf.Max(minBounceVelocity, maxBounceVelocity);
        surfacePushForce = Mathf.Max(0f, surfacePushForce);
        
        // Ensure cooldown is reasonable
        collisionCooldown = Mathf.Max(0.1f, collisionCooldown);
    }
#endif
    
    /// <summary>
    /// Create default configuration with optimal values for 320-unit character
    /// </summary>
    public static HeadCollisionConfig CreateDefault()
    {
        var config = CreateInstance<HeadCollisionConfig>();
        // Uses default field initializers
        return config;
    }
}
