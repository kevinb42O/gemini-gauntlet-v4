// --- Fireball.cs ---
using UnityEngine;
using GeminiGauntlet.Audio;

/// <summary>
/// Manages the behavior of a single fireball. It handles collision, lifetime, and effects.
/// This component should be on a prefab with a Rigidbody and a Collider.
/// </summary>
public class Fireball : MonoBehaviour
{
    [Header("Behavior")]
    [Tooltip("Lifetime of the fireball in seconds before it is automatically destroyed.")]
    public float lifetime = 10f;
    [Tooltip("Damage dealt to player on hit.")]
    public float damage = 500f;
    [Tooltip("Effect to spawn on impact with the player.")]
    public GameObject impactEffectPrefab;

    [Header("Sounds")]
    [Tooltip("Sound to play on first impact with the ground.")]
    public AudioClip groundHitSound;
    [Range(0f, 1f)] public float groundHitVolume = 0.5f;
    [Tooltip("Sound to play on impact with the player.")]
    public AudioClip playerHitSound;
    [Range(0f, 1f)] public float playerHitVolume = 1.0f;

    private bool _hasHitGround = false;

    void Start()
    {
        // This is the primary cleanup mechanism to ensure performance.
        // The fireball will destroy itself after 'lifetime' seconds if it hasn't hit the player.
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if the fireball hit the player by checking the "Player" tag.
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("[Fireball] Hit player! Applying directional damage.");

            // Use IDamageable interface for proper directional hit feedback
            IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Vector3 hitPoint = collision.GetContact(0).point;
                // Direction FROM this projectile TO player (attack direction)
                Vector3 hitDirection = (collision.transform.position - transform.position).normalized;
                damageable.TakeDamage(damage, hitPoint, hitDirection);
            }

            // Play effects and destroy the fireball
            PlayEffectAndSound(playerHitSound, playerHitVolume, impactEffectPrefab, collision.GetContact(0).point);
            DestroyFireball();
            return; // Stop further processing
        }

        // If it hits anything else for the first time, play a ground impact sound.
        // We check a layer to avoid playing this sound on walls or other objects.
        // Please ensure your platform/ground has the correct layer.
        // Example uses "Default" layer, change if you have a specific "Ground" layer.
        if (!_hasHitGround && collision.gameObject.layer == LayerMask.NameToLayer("Default"))
        {
            _hasHitGround = true;
            PlayEffectAndSound(groundHitSound, groundHitVolume, null, collision.GetContact(0).point);
        }
    }

    private void PlayEffectAndSound(AudioClip clip, float volume, GameObject effect, Vector3 position)
    {
        if (clip != null) GameSounds.PlayProjectileHit(position, volume);
        if (effect != null) Instantiate(effect, position, Quaternion.identity);
    }

    private void DestroyFireball()
    {
        // To prevent it from colliding with anything else after impact, disable components immediately
        if (GetComponent<Collider>() != null) GetComponent<Collider>().enabled = false;
        if (GetComponent<Renderer>() != null) GetComponent<Renderer>().enabled = false;
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
        }

        // Destroy the object after a delay to let sounds finish playing
        Destroy(gameObject, 2f);
    }
}