using UnityEngine;

/// <summary>
/// 🎯 UNIFIED IMPACT SYSTEM - IMPACT DATA STRUCTURE
/// 
/// Single source of truth for all landing impact data.
/// Broadcast by FallingDamageSystem, consumed by camera, audio, and effects systems.
/// 
/// Design: Value type (struct) for zero GC pressure, event-based communication
/// Author: Senior Coding Expert
/// Date: 2025-10-16
/// </summary>
public struct ImpactData
{
    // ========== CORE IMPACT METRICS ==========
    
    /// <summary>
    /// Vertical distance fallen (units)
    /// Calculated from highest point during fall to landing position
    /// </summary>
    public float fallDistance;
    
    /// <summary>
    /// Time spent in air (seconds)
    /// Total duration from leaving ground to landing
    /// </summary>
    public float airTime;
    
    /// <summary>
    /// Vertical velocity at moment of impact (units/s)
    /// Absolute value of Y velocity component
    /// </summary>
    public float impactSpeed;
    
    /// <summary>
    /// World position where impact occurred
    /// </summary>
    public Vector3 landingPosition;
    
    /// <summary>
    /// Ground surface normal at impact point
    /// Used for slope detection and effect orientation
    /// </summary>
    public Vector3 landingNormal;
    
    // ========== SEVERITY CLASSIFICATION ==========
    
    /// <summary>
    /// Impact severity tier (None/Light/Moderate/Severe/Lethal)
    /// </summary>
    public ImpactSeverity severity;
    
    /// <summary>
    /// Normalized severity value (0-1)
    /// 0 = no impact, 0.3 = moderate, 0.6 = severe, 1.0 = lethal
    /// Useful for smooth scaling of effects
    /// </summary>
    public float severityNormalized;
    
    // ========== CALCULATED VALUES ==========
    
    /// <summary>
    /// Damage amount to apply (0 if damage disabled)
    /// Scaled by fall distance using damage curve
    /// </summary>
    public float damageAmount;
    
    /// <summary>
    /// Camera trauma intensity (0-1)
    /// Drives screen shake and impact feel
    /// </summary>
    public float traumaIntensity;
    
    /// <summary>
    /// Camera spring compression amount (units)
    /// Drives landing "knee bend" visual
    /// </summary>
    public float compressionAmount;
    
    // ========== CONTEXT FLAGS ==========
    
    /// <summary>
    /// Was player on a slope when landing? (ground angle > 15°)
    /// </summary>
    public bool wasOnSlope;
    
    /// <summary>
    /// Was player sprinting before fall?
    /// </summary>
    public bool wasSprinting;
    
    /// <summary>
    /// Was player performing aerial tricks during fall?
    /// </summary>
    public bool wasInTrick;
    
    /// <summary>
    /// Should this impact trigger epic superhero landing animation?
    /// Considers fall height, air time, and trick context
    /// </summary>
    public bool shouldTriggerSuperheroLanding;
    
    // ========== TIMING ==========
    
    /// <summary>
    /// Time.time when impact occurred
    /// Used for cooldowns and timing synchronization
    /// </summary>
    public float timestamp;
}

/// <summary>
/// Impact severity classification tiers
/// Based on multiples of player height (320 units)
/// </summary>
public enum ImpactSeverity
{
    /// <summary>No impact (fall too small to register)</summary>
    None = 0,
    
    /// <summary>Light impact (1-2x player height, 320-640 units)</summary>
    Light = 1,
    
    /// <summary>Moderate impact (2-3x player height, 640-960 units)</summary>
    Moderate = 2,
    
    /// <summary>Severe impact (3-4x player height, 960-1280 units)</summary>
    Severe = 3,
    
    /// <summary>Lethal impact (4x+ player height, 1280+ units)</summary>
    Lethal = 4
}

/// <summary>
/// 🎯 UNIFIED IMPACT THRESHOLDS
/// Single source of truth for all impact-related thresholds
/// Based on 320-unit player height
/// </summary>
public static class ImpactThresholds
{
    // ========== FALL HEIGHT TIERS (units) ==========
    
    /// <summary>Light impact threshold (1x player height)</summary>
    public const float LIGHT_IMPACT = 320f;
    
    /// <summary>Moderate impact threshold (2x player height)</summary>
    public const float MODERATE_IMPACT = 640f;
    
    /// <summary>Severe impact threshold (3x player height)</summary>
    public const float SEVERE_IMPACT = 960f;
    
    /// <summary>Lethal impact threshold (4x player height)</summary>
    public const float LETHAL_IMPACT = 1280f;
    
    /// <summary>Superhero landing threshold (6.25x player height, epic impacts only)</summary>
    public const float SUPERHERO_IMPACT = 2000f;
    
    // ========== AIR TIME THRESHOLDS (seconds) ==========
    
    /// <summary>Minimum air time to count as a real fall (anti-spam)</summary>
    public const float MIN_AIR_TIME = 1.0f;
    
    /// <summary>Epic air time for superhero landings</summary>
    public const float EPIC_AIR_TIME = 2.0f;
    
    // ========== SPEED THRESHOLDS (units/s) ==========
    
    /// <summary>High-speed movement threshold</summary>
    public const float HIGH_SPEED_THRESHOLD = 960f;
    
    /// <summary>Impact speed threshold for collision damage</summary>
    public const float IMPACT_SPEED_THRESHOLD = 100f;
}
