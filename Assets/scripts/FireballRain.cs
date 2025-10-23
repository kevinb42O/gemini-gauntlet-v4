// --- FireballRain.cs ---
// Created by Cascade AI on 2025-07-06
// This component spawns fireballs at random intervals and positions,
// giving a dramatic "comet rain" effect. Attach it to an empty GameObject
// and assign a fireball prefab (with a Rigidbody + Fireball script).

using UnityEngine;
using System.Collections;

/// <summary>
/// Manages the fireball rain system. Spawns fireballs at random intervals and positions
/// that fall from the sky like comets at high speed.
/// </summary>
public class FireballRain : MonoBehaviour
{
    [Header("Fireball Settings")]
    [Tooltip("The fireball prefab to spawn (should have the Fireball component).")]
    public GameObject fireballPrefab;

    [Header("Spawn Settings")]
    [Tooltip("Minimum time between fireball spawns (in seconds).")]
    public float minSpawnInterval = 2f;
    [Tooltip("Maximum time between fireball spawns (in seconds).")]
    public float maxSpawnInterval = 8f;
    [Tooltip("Height above the play area where fireballs spawn.")]
    public float spawnHeight = 50f;
    [Tooltip("Radius around the spawn center where fireballs can spawn.")]
    public float spawnRadius = 30f;
    [Tooltip("Center point for spawning fireballs. If not set, uses this object's position.")]
    public Transform spawnCenter;

    [Header("Fireball Physics")]
    [Tooltip("Speed range for falling fireballs (comet-like speed).")]
    public float minFallSpeed = 15f;
    [Tooltip("Maximum speed for falling fireballs.")]
    public float maxFallSpeed = 25f;
    [Tooltip("Random horizontal velocity range to add variation.")]
    public float horizontalVariation = 5f;
    [Tooltip("Downward force multiplier for gravity effect.")]
    public float gravityMultiplier = 2f;

    [Header("Control")]
    [Tooltip("Whether the fireball rain is currently active.")]
    public bool isRaining = true;
    [Tooltip("Auto-start the rain when the game starts.")]
    public bool autoStart = true;

    // Internal state
    private Vector3 _spawnCenterPosition;
    private Coroutine _rainCoroutine;

    void Start()
    {
        // Determine initial spawn center
        _spawnCenterPosition = spawnCenter != null ? spawnCenter.position : transform.position;

        if (autoStart)
        {
            StartRain();
        }
    }

    /// <summary>
    /// Starts the fireball rain system.
    /// </summary>
    public void StartRain()
    {
        if (_rainCoroutine == null && fireballPrefab != null)
        {
            isRaining = true;
            _rainCoroutine = StartCoroutine(RainCoroutine());
            Debug.Log("Fireball rain started!");
        }
    }

    /// <summary>
    /// Stops the fireball rain system.
    /// </summary>
    public void StopRain()
    {
        if (_rainCoroutine != null)
        {
            isRaining = false;
            StopCoroutine(_rainCoroutine);
            _rainCoroutine = null;
            Debug.Log("Fireball rain stopped!");
        }
    }

    /// <summary>
    /// Toggles the fireball rain on/off.
    /// </summary>
    public void ToggleRain()
    {
        if (isRaining)
        {
            StopRain();
        }
        else
        {
            StartRain();
        }
    }

    private IEnumerator RainCoroutine()
    {
        while (isRaining)
        {
            // Wait for a random interval before spawning the next fireball.
            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);

            SpawnFireball();
        }
    }

    private void SpawnFireball()
    {
        Vector3 spawnPosition = GetRandomSpawnPosition();
        GameObject fireballInstance = Instantiate(fireballPrefab, spawnPosition, Quaternion.identity);
        SetupFireballPhysics(fireballInstance);
        Debug.Log($"Fireball spawned at {spawnPosition}");
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        return new Vector3(
            _spawnCenterPosition.x + randomCircle.x,
            _spawnCenterPosition.y + spawnHeight,
            _spawnCenterPosition.z + randomCircle.y);
    }

    private void SetupFireballPhysics(GameObject fireball)
    {
        Rigidbody rb = fireball.GetComponent<Rigidbody>();
        if (rb == null) return;

        float fallSpeed = Random.Range(minFallSpeed, maxFallSpeed);
        Vector3 horizontalVelocity = new Vector3(
            Random.Range(-horizontalVariation, horizontalVariation),
            0f,
            Random.Range(-horizontalVariation, horizontalVariation));

        Vector3 velocity = new Vector3(horizontalVelocity.x, -fallSpeed, horizontalVelocity.z);
        rb.linearVelocity = velocity;

        // Increase gravity scale for more dramatic falling effect (requires custom extension or Unity 2023+)
        // Apply extra downward force to simulate stronger gravity
        rb.AddForce(Physics.gravity * (gravityMultiplier - 1f), ForceMode.Acceleration);

        // Random rotation for visual flair
        rb.angularVelocity = Random.insideUnitSphere * 2f;
    }

    /// <summary>
    /// Updates the spawn center position (e.g., follow a moving player).
    /// </summary>
    public void UpdateSpawnCenter(Vector3 newCenter)
    {
        _spawnCenterPosition = newCenter;
    }

    /// <summary>
    /// Spawns a single fireball immediately (for testing or events).
    /// </summary>
    public void SpawnSingleFireball()
    {
        if (fireballPrefab != null)
        {
            SpawnFireball();
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 center = spawnCenter != null ? spawnCenter.position : transform.position;

        // Draw ground-level spawn area
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, spawnRadius);

        // Draw elevated spawn sphere
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center + Vector3.up * spawnHeight, spawnRadius);

        // Draw connecting line
        Gizmos.color = new Color(1f, 0.5f, 0f); // orange-ish
        Gizmos.DrawLine(center, center + Vector3.up * spawnHeight);
    }
#endif
}
