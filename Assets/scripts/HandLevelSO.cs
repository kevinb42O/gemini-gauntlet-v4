// --- HandLevelSO.cs (FINAL - VFX & Area Damage) ---
using UnityEngine;

[CreateAssetMenu(fileName = "HandLevel_LvlX", menuName = "Game/Hand Level Configuration")]
public class HandLevelSO : ScriptableObject
{
    [Header("General")]
    [Tooltip("Layer mask for detecting enemies and other damageable objects.")]
    public LayerMask damageLayerMask;

    [Space(10)]
    [Header("Stream / Beam Attack (Hold)")]
    [Tooltip("Prefab with MagicBeamStatic component for beam effects.")]
    public GameObject streamBeamPrefab;
    
    [Tooltip("Legacy VFX prefab for stream effects (used alongside new beam system).")]
    public GameObject streamVFX;
    
    [Space(5)]
    [Tooltip("Damage dealt per second by the stream attack.")]
    [Min(0f)]
    public float streamDamagePerSecond = 20f;
    
    [Tooltip("How many times per second the stream deals damage (higher = smoother damage).")]
    [Range(1f, 30f)]
    public float streamDamageRateHz = 10f;
    
    [Tooltip("The radius of the stream's damage-dealing spherecast.")]
    [Min(0.1f)]
    public float streamDamageRadius = 0.5f;
    
    [Tooltip("The maximum distance the stream can reach and deal damage.")]
    [Min(1f)]
    public float streamMaxDistance = 100f;
    
    [Space(5)]
    [Tooltip("The looping audio clip played during stream attacks.")]
    public AudioClip fireStreamSound;
    
    [Tooltip("Volume level for the stream attack sound (0 = silent, 1 = full volume).")]
    [Range(0f, 1f)] 
    public float fireStreamVolume = 0.7f;

    [Space(10)]
    [Header("Shotgun Attack (Tap)")]
    [Tooltip("Does this hand level have a shotgun mode? If false, tapping does nothing.")]
    public bool hasShotgunMode = true;
    
    [Tooltip("Simple shotgun burst VFX prefab - cone particle effect only.")]
    public GameObject legacyShotgunVFX;
    
    [Space(5)]
    [Tooltip("Cooldown time before the shotgun can be fired again (in seconds).")]
    [Min(0.1f)]
    public float shotgunCooldown = 0.5f;
    
    [Tooltip("Instant damage dealt by a single shotgun blast to every enemy hit.")]
    [Min(0f)]
    public float shotgunDamage = 50f;
    
    [Tooltip("The radius of the shotgun's damage-dealing spherecast, simulating the blast's spread.")]
    [Min(0.1f)]
    public float shotgunDamageRadius = 5f;
    
    [Tooltip("The maximum distance the shotgun blast can travel and deal damage.")]
    [Min(1f)]
    public float shotgunMaxDistance = 30f;
    
    [Space(5)]
    [Tooltip("The one-shot audio clip played when firing the shotgun blast.")]
    public AudioClip shotgunBlastSound;
    
    [Tooltip("Volume level for the shotgun blast sound (0 = silent, 1 = full volume).")]
    [Range(0f, 1f)] 
    public float shotgunBlastVolume = 0.85f;

    [Space(10)]
    [Header("Progression Stats")]
    [Tooltip("Multiplier for how fast gems are pulled towards the player (1.0 = normal speed).")]
    [Min(0.1f)]
    public float gemAttractionSpeedMultiplier = 1f;
    
#if UNITY_EDITOR
    // Editor-time validation and clamping - ONLY runs in Editor, not during play mode scene updates
    private void OnValidate()
    {
        // PERFORMANCE FIX: Skip validation during play mode to prevent scene update freezes
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        // Clamp numeric ranges defensively (attributes help in Inspector but not at runtime changes)
        streamDamagePerSecond = Mathf.Max(0f, streamDamagePerSecond);
        streamDamageRateHz = Mathf.Clamp(streamDamageRateHz, 1f, 30f);
        streamDamageRadius = Mathf.Max(0.1f, streamDamageRadius);
        streamMaxDistance = Mathf.Max(1f, streamMaxDistance);
        fireStreamVolume = Mathf.Clamp01(fireStreamVolume);

        shotgunCooldown = Mathf.Max(0.1f, shotgunCooldown);
        shotgunDamage = Mathf.Max(0f, shotgunDamage);
        shotgunDamageRadius = Mathf.Max(0.1f, shotgunDamageRadius);
        shotgunMaxDistance = Mathf.Max(1f, shotgunMaxDistance);
        shotgunBlastVolume = Mathf.Clamp01(shotgunBlastVolume);

        gemAttractionSpeedMultiplier = Mathf.Max(0.1f, gemAttractionSpeedMultiplier);

        // Reference warnings to help catch missing assignments in Editor
        // REMOVED: These warnings run on every Inspector change and cause performance issues
        // Use the context menu "Validate Settings" instead to manually check
    }
#endif

#if UNITY_EDITOR
    [ContextMenu("Validate Settings")]
    private void ValidateSettingsContextMenu()
    {
        // Manual validation - checks for missing references
        if (streamBeamPrefab == null)
        {
            Debug.LogWarning($"[HandLevelSO:{name}] streamBeamPrefab is not assigned - stream will not render.");
        }

        if (hasShotgunMode && legacyShotgunVFX == null)
        {
            Debug.LogWarning($"[HandLevelSO:{name}] legacyShotgunVFX is not assigned - shotgun effect will not spawn.");
        }
        
        Debug.Log($"[HandLevelSO:{name}] Validation complete.");
    }
#endif
}