// --- PlatformHeatController.cs (NEW SCRIPT) ---
using UnityEngine;
using System.Collections;

public class PlatformHeatController : MonoBehaviour
{
    [Header("Heating Mechanics")]
    [Tooltip("Time in seconds for the platform to go from 0 to max heat once the player lands.")]
    public float timeToMaxHeat = 10.0f;
    [Tooltip("At what percentage of max heat (0-1) should the wildfire effect start spawning?")]
    public float heatThresholdForWildfire = 0.6f;
    [Tooltip("At what percentage of max heat (0-1) should the platform start dealing damage?")]
    public float heatThresholdForDamage = 0.85f;
    [Tooltip("Damage per second once the damage threshold is reached.")]
    public float damagePerSecond = 10f;

    [Header("Visuals - Material")]
    [Tooltip("The Renderer of the platform whose material will be tinted.")]
    public Renderer platformRenderer;
    [Tooltip("Name of the shader property for emissive color/intensity or tint. e.g., '_EmissionColor', '_TintColor'")]
    public string heatShaderColorProperty = "_EmissionColor"; // Or "_BaseColorTint", etc.
    public Color normalColor = Color.black; // Or initial emissive color
    public Color maxHeatColor = Color.red * 2f; // Bright red emissive

    [Header("Visuals - Wildfire Effect")]
    [Tooltip("The 'Wildfire' particle effect prefab.")]
    public GameObject wildfireEffectPrefab;
    [Tooltip("How far the wildfire effect should spread from the center (radius).")]
    public float maxWildfireSpreadRadius = 2.0f; // Adjust based on platform size
    [Tooltip("How quickly the wildfire particle effect spreads once activated.")]
    public float wildfireSpreadSpeed = 0.5f; // Units per second for the radius

    private bool _playerIsOnPlatform = false;
    private float _currentHeat = 0f; // Normalized 0 to 1
    private Material _platformMaterialInstance;
    private ParticleSystem _activeWildfireEffect;
    private float _currentWildfireRadius = 0f;
    private bool _wildfireActive = false;
    private Transform _playerTransform; // To apply damage

    void Awake()
    {
        if (platformRenderer != null)
        {
            _platformMaterialInstance = platformRenderer.material; // Creates an instance
            if (!_platformMaterialInstance.HasProperty(heatShaderColorProperty))
            {
                Debug.LogWarning($"PlatformHeatController on {name}: Material does not have property '{heatShaderColorProperty}'. Heat tinting might not work.", this);
            }
        }
        else
        {
            Debug.LogError($"PlatformHeatController on {name}: Platform Renderer not assigned!", this);
            enabled = false;
        }

        if (wildfireEffectPrefab == null)
        {
            Debug.LogWarning($"PlatformHeatController on {name}: Wildfire Effect Prefab not assigned.", this);
        }
    }

    void Start()
    {
        // Initialize material color
        if (_platformMaterialInstance != null && _platformMaterialInstance.HasProperty(heatShaderColorProperty))
        {
            _platformMaterialInstance.SetColor(heatShaderColorProperty, normalColor);
        }
    }

    void Update()
    {
        if (_playerIsOnPlatform)
        {
            // Increase heat
            if (_currentHeat < 1f)
            {
                _currentHeat += (1f / timeToMaxHeat) * Time.deltaTime;
                _currentHeat = Mathf.Clamp01(_currentHeat);

                // Update material tint/emission
                if (_platformMaterialInstance != null && _platformMaterialInstance.HasProperty(heatShaderColorProperty))
                {
                    _platformMaterialInstance.SetColor(heatShaderColorProperty, Color.Lerp(normalColor, maxHeatColor, _currentHeat));
                }

                // Check for wildfire activation
                if (!_wildfireActive && _currentHeat >= heatThresholdForWildfire && wildfireEffectPrefab != null)
                {
                    SpawnWildfireEffect();
                }
            }

            // Spread wildfire if active
            if (_wildfireActive && _activeWildfireEffect != null && _currentWildfireRadius < maxWildfireSpreadRadius)
            {
                _currentWildfireRadius += wildfireSpreadSpeed * Time.deltaTime;
                _currentWildfireRadius = Mathf.Min(_currentWildfireRadius, maxWildfireSpreadRadius);

                // You might need to control the particle system's shape module (e.g., radius of a sphere emitter)
                // or its emission rate based on _currentWildfireRadius.
                // This is highly dependent on how your "wildfire" prefab is set up.
                // For a simple approach, if it's a single particle system emitting outwards:
                var shape = _activeWildfireEffect.shape;
                if (shape.enabled && (shape.shapeType == ParticleSystemShapeType.Sphere || shape.shapeType == ParticleSystemShapeType.Circle))
                {
                    shape.radius = _currentWildfireRadius;
                }
                // Or, if your wildfire prefab handles its own spreading animation over time,
                // you might just ensure it's playing.
            }

            // Apply damage if threshold reached
            if (_currentHeat >= heatThresholdForDamage && _playerTransform != null)
            {
                PlayerHealth health = _playerTransform.GetComponent<PlayerHealth>();
                if (health != null && !health.isDead)
                {
                    // For direct damage, PlayerHealth would need a TakeDamage method.
                    // For simplicity here, let's assume PlayerHealth has a simple damage intake.
                    // This is a placeholder for actual damage application.
                    // health.TakeContinuousDamage(damagePerSecond * Time.deltaTime);
                    Debug.Log($"Platform {name} is hot! Dealing damage. Player might need a TakeDamage method.");
                }
            }
        }
        else // Player is not on platform
        {
            // Cool down (optional, or reset instantly)
            if (_currentHeat > 0f)
            {
                _currentHeat -= (1f / (timeToMaxHeat * 0.5f)) * Time.deltaTime; // Cools faster
                _currentHeat = Mathf.Clamp01(_currentHeat);

                if (_platformMaterialInstance != null && _platformMaterialInstance.HasProperty(heatShaderColorProperty))
                {
                    _platformMaterialInstance.SetColor(heatShaderColorProperty, Color.Lerp(normalColor, maxHeatColor, _currentHeat));
                }

                if (_currentHeat < heatThresholdForWildfire && _wildfireActive)
                {
                    StopWildfireEffect();
                }
            }
        }
    }

    void SpawnWildfireEffect()
    {
        if (wildfireEffectPrefab == null) return;

        // Spawn at the center of the platform (top surface)
        Vector3 spawnPos = transform.position;
        Collider platformCollider = GetComponent<Collider>();
        if (platformCollider != null)
        {
            spawnPos = platformCollider.bounds.center; // Get center of the collider
            spawnPos.y = platformCollider.bounds.max.y + 0.05f; // Slightly above the top surface
        }
        else
        {
            // Fallback if no collider, use transform position + some offset if needed
            spawnPos.y += transform.localScale.y / 2f + 0.05f;
        }


        GameObject effectInstance = Instantiate(wildfireEffectPrefab, spawnPos, Quaternion.LookRotation(Vector3.up), transform); // Parent to platform
        _activeWildfireEffect = effectInstance.GetComponent<ParticleSystem>();
        if (_activeWildfireEffect)
        {
            var main = _activeWildfireEffect.main;
            main.loop = true; // Ensure it loops if it's meant to be a continuous spread

            // Initialize shape radius to small or 0
            var shape = _activeWildfireEffect.shape;
            if (shape.enabled && (shape.shapeType == ParticleSystemShapeType.Sphere || shape.shapeType == ParticleSystemShapeType.Circle))
            {
                shape.radius = 0.01f;
            }
            _activeWildfireEffect.Play();
        }
        _wildfireActive = true;
        _currentWildfireRadius = 0f;
    }

    void StopWildfireEffect()
    {
        if (_activeWildfireEffect != null)
        {
            _activeWildfireEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // Stop and clear
            Destroy(_activeWildfireEffect.gameObject, _activeWildfireEffect.main.duration + 1f); // Destroy after particles fade
        }
        _wildfireActive = false;
        _activeWildfireEffect = null;
    }


    // Called by PlatformTrigger (or whatever detects player landing/leaving)
    public void OnPlayerEnterPlatform(Transform player)
    {
        _playerIsOnPlatform = true;
        _playerTransform = player;
        // Optional: Instantly apply some heat if re-landing on a previously heated platform.
        // For now, heat-up starts from current heat level.
    }

    public void OnPlayerExitPlatform()
    {
        _playerIsOnPlatform = false;
        _playerTransform = null;
        // If you want instant cooldown/reset when player leaves:
        // _currentHeat = 0f;
        // if (_platformMaterialInstance != null && _platformMaterialInstance.HasProperty(heatShaderColorProperty))
        // {
        //     _platformMaterialInstance.SetColor(heatShaderColorProperty, normalColor);
        // }
        // StopWildfireEffect();
    }

    void OnDestroy()
    {
        if (_platformMaterialInstance != null)
        {
            Destroy(_platformMaterialInstance); // Clean up the instanced material
        }
        if (_activeWildfireEffect != null)
        {
            Destroy(_activeWildfireEffect.gameObject);
        }
    }
}