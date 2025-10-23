// --- ChasingFireball.cs ---
using UnityEngine;
using GeminiGauntlet.Audio;

/// <summary>
/// A fireball that actively chases a target using Rigidbody forces. It is immortal
/// DeathPlane can destroy it.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ChasingFireball : MonoBehaviour
{
    [Header("Behavior")]
    [Tooltip("Chase force applied towards player.")]
    public float chaseForce = 150f;
    [Tooltip("Maximum velocity the fireball can reach.")]
    public float maxVelocity = 15f;
    [Tooltip("Lifetime of the fireball in seconds before it is automatically destroyed.")]
    public float lifetime = 25f;
    [Tooltip("Damage dealt to player on hit.")]
    public float damage = 750f;
    [Tooltip("Custom gravity force to apply. Set to -750 to match game gravity.")]
    public float gravityForce = -750f;
    [Tooltip("Height offset to target above/below player position for better chasing.")]
    public float heightOffset = 0f;
    
    [Header("Effects & Sound")]
    [Tooltip("Prefab to instantiate when the fireball hits the player.")]
    public GameObject impactEffectPrefab;
    [Tooltip("Sound to play on player impact.")]
    public AudioClip playerHitSound;
    [Range(0f, 1f)] public float playerHitVolume = 1.0f;

    private Rigidbody _rb;
    private Transform _playerTransform;
    private bool _isActive = true;

    /// <summary>
    /// This MUST be called by the spawner (e.g., HybridTowerController) to tell the fireball who to chase.
    /// </summary>
    public void Initialize(Transform target)
    {
        _playerTransform = target;
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        
        // Disable Unity's default gravity since we're applying custom gravity
        _rb.useGravity = false;
        
        // This is a safety net. The fireball will destroy itself after 'lifetime' seconds
        // in case it gets stuck somewhere and never falls off the map.
        Destroy(gameObject, lifetime);
    }

    // FixedUpdate is used for all Rigidbody and physics-based calculations.
    void FixedUpdate()
    {
        // Always apply custom gravity
        _rb.AddForce(Vector3.up * gravityForce * Time.fixedDeltaTime, ForceMode.Acceleration);

        // Stop chasing if there is no target (e.g., player is dead) or if the fireball has already been "killed".
        if (_playerTransform == null || !_isActive)
        {
            // Still apply gravity but don't chase - let it fall naturally
            return;
        }

        // 1. Calculate the direction to the player with height offset
        Vector3 targetPosition = _playerTransform.position + Vector3.up * heightOffset;
        Vector3 directionToPlayer = targetPosition - transform.position;

        // 2. REMOVED the Y = 0 limitation - now chases in full 3D space
        // This allows the fireball to follow the player's height and fall with gravity
        directionToPlayer.Normalize();

        // 3. Apply a continuous force to the Rigidbody, causing it to move towards the player in 3D
        _rb.AddForce(directionToPlayer * chaseForce);

        // 4. Clamp the velocity to prevent the fireball from exceeding its maximum speed
        // Only clamp horizontal velocity to allow natural falling
        Vector3 horizontalVelocity = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
        if (horizontalVelocity.sqrMagnitude > maxVelocity * maxVelocity)
        {
            horizontalVelocity = horizontalVelocity.normalized * maxVelocity;
            _rb.linearVelocity = new Vector3(horizontalVelocity.x, _rb.linearVelocity.y, horizontalVelocity.z);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!_isActive) return;

        // If we collide with the Player, apply directional damage
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("[ChasingFireBall] Hit player! Applying directional damage.");
            
            // Use IDamageable interface for proper directional hit feedback
            IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Vector3 hitPoint = collision.GetContact(0).point;
                // Direction FROM this projectile TO player (attack direction)
                Vector3 hitDirection = (collision.transform.position - transform.position).normalized;
                damageable.TakeDamage(damage, hitPoint, hitDirection);
            }

            // Play effects and destroy this fireball.
            if (playerHitSound != null) GameSounds.PlayProjectileHit(transform.position, playerHitVolume);
            if (impactEffectPrefab != null) Instantiate(impactEffectPrefab, collision.GetContact(0).point, Quaternion.identity);

            DestroyFireball();
        }
    }

    /// <summary>
    /// This is a public method designed to be called by an external script, like DeathPlane.
    /// It handles the destruction of the fireball when it falls off the map.
    /// </summary>
    public void FellOffPlatform()
    {
        // You could add a special "sizzle" or "poof" effect here.
        Debug.Log($"{name} fell off the platform and was destroyed by a DeathPlane.");
        DestroyFireball();
    }

    private void DestroyFireball()
    {
        _isActive = false; // Stop all behaviors in FixedUpdate.
        if (GetComponent<Collider>() != null) GetComponent<Collider>().enabled = false;
        if (GetComponent<Renderer>() != null) GetComponent<Renderer>().enabled = false;

        // Stop all movement immediately.
        _rb.isKinematic = true;
        _rb.linearVelocity = Vector3.zero;

        // Destroy the GameObject after a delay to ensure any final sounds can play.
        Destroy(gameObject, 2f);
    }
}