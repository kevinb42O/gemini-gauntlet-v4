// --- BossGem.cs (Corrected for Gem.cs protected _isDetached and Rigidbody.velocity) ---
using UnityEngine;

public class BossGem : Gem // Inherit from Gem
{
    [Header("Boss Gem Specifics")]
    public AudioClip detachSound_BossSpecific;
    [Range(0f, 2f)] public float detachSoundVolume_BossSpecific = 1.0f;
    public GameObject detachEffectPrefab_BossSpecific;

    private BossEnemy _bossParent;

    // Awake from Gem.cs handles Rigidbody, Collider, Renderer setup.
    // We override Start for BossGem specific parent finding.
    protected override void Start() // Override Gem's Start
    {
        // base.Start(); // Call base Start if Gem's Start had logic BossGem also needs. Currently it's minor.
        _bossParent = GetComponentInParent<BossEnemy>();
        if (_bossParent == null)
        {
            Debug.LogError($"BossGem ({name}): Could not find BossEnemy in parent! This gem will not function correctly.", this);
            enabled = false;
            gameObject.SetActive(false);
        }
        // CurrentHealth is set in Gem's Awake.
    }

    // TakeDamage is inherited from Gem.cs.
    // If health <= 0, Gem.TakeDamage() will call DetachFromSource().
    // We override DetachFromSource for BossGem's specific behavior.
    protected override void DetachFromSource()
    {
        // This method is called when health reaches 0 via Gem's TakeDamage().
        // It now directly calls the BossGem's specific logic.
        // The _isDetached flag will be set to true in Gem.DetachFromSource()
        // *before* this override would conceptually run if we called base.DetachFromSource().
        // However, since we are fully overriding, we need to manage the state here.
        // Or, ensure Gem.DetachFromSource() handles the _isDetached = true; line.
        // Gem.DetachFromSource() now sets _isDetached = true, so this override continues from there.
        DetachFromBoss();
    }

    public void DetachFromBoss() // Public for direct calls if needed by BossEnemy
    {
        if (_isDetached) return; // Accesses protected member from Gem
        _isDetached = true;      // Sets protected member

        Debug.Log($"BossGem ({name}) detaching from boss.");

        if (_bossParent != null)
        {
            _bossParent.OnGemDetached(this);
        }
        else
        {
            Debug.LogWarning($"BossGem ({name}): BossParent is null, cannot notify on detachment.", this);
        }

        GameObject effectToUse = detachEffectPrefab_BossSpecific != null ? detachEffectPrefab_BossSpecific : base.detachEffectPrefab;

        // Play detach sound using GemSoundManager/GameSounds system
        if (detachSound_BossSpecific != null)
        {
            // BossGem has specific sound - play it directly (legacy fallback)
            PlayOneShotSoundAtPoint(detachSound_BossSpecific, transform.position, detachSoundVolume_BossSpecific);
        }
        else if (gemSoundManager != null)
        {
            // Use GemSoundManager for mixer-based audio
            gemSoundManager.PlayGemDetach();
        }
        if (effectToUse != null) Instantiate(effectToUse, transform.position, Quaternion.identity);

        Renderer currentRenderer = GetComponent<MeshRenderer>() ?? (Renderer)GetComponent<SpriteRenderer>();
        Collider currentCollider = GetComponent<Collider>();

        if (currentRenderer != null) currentRenderer.enabled = false;
        if (currentCollider != null) currentCollider.enabled = false;

        // Rigidbody _rb is inherited and accessible as protected
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.linearVelocity = Vector3.zero; // CORRECTED: Rigidbody.velocity to linearVelocity
            _rb.angularVelocity = Vector3.zero;
        }

        gameObject.SetActive(false);
        Destroy(gameObject, 2f);
    }
    // IsDetached() is inherited from Gem.cs
    // PlayOneShotSoundAtPoint is inherited from Gem.cs
}