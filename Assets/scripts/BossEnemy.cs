// --- BossEnemy.cs (CORRECTED - GameStats kill call REMOVED from minion spawn) ---
using UnityEngine;
using GeminiGauntlet.Audio;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BossEnemy : MonoBehaviour
{
    public enum BossState { Dormant, Awakening, Active, Dying }
    public BossState currentState = BossState.Dormant;

    [Header("Core Stats & Setup")]
    public Transform[] gemSlots;
    public GameObject bossGemPrefab; // Ensure this prefab has BossGem.cs

    [Header("Awakening")]
    public BossPlatformTrigger awakeningPlatformTrigger;
    public float awakeningDuration = 3.0f;
    public AudioClip awakenSound;
    [Range(0f, 1f)] public float awakenSoundVolume = 0.9f;

    [Header("Movement (Basic)")]
    public float moveSpeed = 1.5f;
    public float rotationSpeed = 60f; // Degrees per second
    public float idealDistanceFromPlayer = 10f;
    public float stoppingDistance = 8f; // Stop if closer than this to ideal distance

    [Header("Skull Minion Spawning")]
    public GameObject bossMinionSkullPrefab; // SkullEnemy prefab
    public Transform[] minionSpawnPoints;
    public float minionSpawnInterval = 10.0f;
    public int minionsPerSpawn = 2;
    public float minionSpawnForce = 5f;
    private float _nextMinionSpawnTime;

    [Header("Death Sequence")]
    public GameObject deathEffectPrefab;
    public AudioClip deathSound;
    [Range(0f, 1f)] public float deathSoundVolume = 1.0f;
    public float delayBeforeDestroy = 3.0f;

    private List<BossGem> _attachedGems = new List<BossGem>();
    private Transform _playerTransform;
    private Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb != null) _rb.isKinematic = true; // Start kinematic

        if (gemSlots == null || gemSlots.Length == 0) { Debug.LogError($"Boss ({name}): No Gem Slots assigned!", this); enabled = false; return; }
        if (bossGemPrefab == null) { Debug.LogError($"Boss ({name}): BossGem Prefab not assigned!", this); enabled = false; return; }
        if (bossGemPrefab.GetComponent<BossGem>() == null) { Debug.LogError($"Boss ({name}): BossGem Prefab is MISSING BossGem.cs script!", this); enabled = false; return; }
        if (awakeningPlatformTrigger == null) { Debug.LogWarning($"Boss ({name}): AwakeningPlatformTrigger (BossPlatformTrigger type) not assigned. Boss might not awaken via platform.", this); }
        if (bossMinionSkullPrefab == null) { Debug.LogWarning($"Boss ({name}): BossMinionSkullPrefab not assigned.", this); }
        else if (bossMinionSkullPrefab.GetComponent<SkullEnemy>() == null) { Debug.LogError($"Boss ({name}): BossMinionSkullPrefab MISSING SkullEnemy.cs script!", this); }
        if (minionSpawnPoints == null || minionSpawnPoints.Length == 0) { Debug.LogWarning($"Boss ({name}): MinionSpawnPoints not assigned.", this); }

        SetVisualsActive(false); // Start invisible and non-interactive
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _playerTransform = playerObj.transform;
        else Debug.LogWarning($"Boss ({name}): Player not found. Some behaviors might be affected.", this);

        InitializeGems(); // Populate gem slots, but keep them inactive
    }

    void InitializeGems()
    {
        _attachedGems.Clear();
        for (int i = 0; i < gemSlots.Length; i++)
        {
            if (gemSlots[i] == null)
            {
                Debug.LogWarning($"Boss ({name}): Gem Slot at index {i} is null. Skipping.", this);
                continue;
            }
            GameObject gemGO = Instantiate(bossGemPrefab, gemSlots[i].position, gemSlots[i].rotation, gemSlots[i]);
            BossGem gemScript = gemGO.GetComponent<BossGem>();
            if (gemScript != null)
            {
                _attachedGems.Add(gemScript);
            }
            else
            {
                Destroy(gemGO);
                Debug.LogError($"Boss ({name}): Failed to get BossGem script from instantiated gem in slot {i}.", this);
            }
            gemGO.SetActive(false); // Keep individual gems inactive until boss awakens
        }
        // Debug.Log($"Boss ({name}): Initialized {_attachedGems.Count} gems (dormant).");
    }

    void Update()
    {
        if (currentState == BossState.Dormant && awakeningPlatformTrigger != null && _playerTransform != null)
        {
            if (awakeningPlatformTrigger.IsPlayerOnThisPlatform())
            {
                StartCoroutine(AwakenSequence());
            }
        }

        if (currentState != BossState.Active) return;

        HandleMovement();
        HandleMinionSpawning();
    }

    IEnumerator AwakenSequence()
    {
        if (currentState != BossState.Dormant) yield break;
        currentState = BossState.Awakening;
        Debug.Log($"Boss ({name}): Awakening sequence started!");

        SetVisualsActive(true); // Make boss body visible
        foreach (BossGem gem in _attachedGems) // Activate individual gems
        {
            if (gem != null) gem.gameObject.SetActive(true);
        }

        if (awakenSound != null) GameSounds.PlayBossAwaken(transform.position, awakenSoundVolume);

        // Example awakening animation: Scale up
        float timer = 0f;
        Vector3 originalScale = transform.localScale;
        transform.localScale = Vector3.zero; // Start invisible/tiny

        while (timer < awakeningDuration)
        {
            transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, timer / awakeningDuration);
            timer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        transform.localScale = originalScale;

        Debug.Log($"Boss ({name}): Awakened! Now Active.");
        currentState = BossState.Active;
        if (_rb != null) _rb.isKinematic = false; // Allow physics-based movement
        _nextMinionSpawnTime = Time.time + minionSpawnInterval; // Schedule first minion spawn
    }

    void SetVisualsActive(bool isActive)
    {
        // Activate/Deactivate main boss renderers (excluding gems, which are handled separately)
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true); // true to include inactive
        foreach (Renderer r in renderers)
        {
            // Ensure we don't accidentally toggle renderers of child BossGem objects
            if (r.GetComponentInParent<BossGem>() == null && r.GetComponent<BossGem>() == null)
            {
                r.enabled = isActive;
            }
        }
        // Activate/Deactivate main boss collider
        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null) mainCollider.enabled = isActive;
    }


    void HandleMovement()
    {
        if (_playerTransform == null || _rb == null || _rb.isKinematic) return;

        Vector3 directionToPlayer = (_playerTransform.position - transform.position);
        directionToPlayer.y = 0; // Keep movement on the XZ plane for simplicity
        float distanceToPlayer = directionToPlayer.magnitude;

        // Rotation
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer.normalized);
            _rb.MoveRotation(Quaternion.RotateTowards(_rb.rotation, targetRotation, rotationSpeed * Time.deltaTime));
        }

        // Movement
        if (distanceToPlayer > idealDistanceFromPlayer + stoppingDistance || distanceToPlayer < idealDistanceFromPlayer - stoppingDistance)
        {
            // Move towards/away from ideal distance
            Vector3 moveDirection = (distanceToPlayer > idealDistanceFromPlayer) ? transform.forward : -transform.forward;
            if (distanceToPlayer < stoppingDistance * 0.5f) moveDirection = -transform.forward; // Always move away if too close

            Vector3 moveVelocity = moveDirection * moveSpeed;
            _rb.linearVelocity = new Vector3(moveVelocity.x, _rb.linearVelocity.y, moveVelocity.z); // Keep existing Y velocity for jumps/falls if any
        }
        else
        {
            _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0); // Stop XZ movement
        }
    }

    void HandleMinionSpawning()
    {
        if (bossMinionSkullPrefab == null || minionSpawnPoints.Length == 0 || _playerTransform == null) return;

        if (Time.time >= _nextMinionSpawnTime)
        {
            // Debug.Log($"Boss ({name}): Spawning minions.");
            for (int i = 0; i < minionsPerSpawn; i++)
            {
                Transform spawnPoint = minionSpawnPoints[Random.Range(0, minionSpawnPoints.Length)];
                if (spawnPoint == null) continue;

                // PLATFORM FIX: Parent minion to boss's parent (if boss is on platform, minion inherits platform movement)
                GameObject minionGO = Instantiate(bossMinionSkullPrefab, spawnPoint.position, spawnPoint.rotation, transform.parent);
                
                SkullEnemy minionSkull = minionGO.GetComponent<SkullEnemy>();
                if (minionSkull != null)
                {
                    // Minions spawned by boss are not tied to a specific TowerController for their AI aggro by default,
                    // and directly target the player. isBossMinion flag is set to true.
                    minionSkull.InitializeSkull(null, null, true); // Updated API - removed player parameter
                }
                Rigidbody minionRb = minionGO.GetComponent<Rigidbody>();
                if (minionRb != null && minionSpawnForce > 0)
                {
                    minionRb.AddForce(spawnPoint.forward * minionSpawnForce, ForceMode.Impulse);
                }
                // GameStats.AddBossMinionKillToCurrentRun(); // REMOVED: This should be when minion is KILLED, not spawned.
            }
            _nextMinionSpawnTime = Time.time + minionSpawnInterval;
        }
    }

    public void OnGemDetached(BossGem detachedGem) // Called by BossGem
    {
        if (currentState == BossState.Dying) return;

        if (_attachedGems.Contains(detachedGem))
        {
            _attachedGems.Remove(detachedGem);
            // Debug.Log($"Boss ({name}): Gem detached. Remaining: {_attachedGems.Count}");
        }

        if (_attachedGems.Count == 0 && gemSlots.Length > 0) // Ensure it expected gems in the first place
        {
            Die();
        }
    }

    void Die()
    {
        if (currentState == BossState.Dying) return;
        currentState = BossState.Dying;
        Debug.Log($"Boss ({name}): All gems destroyed! Boss is dying.");

        GameStats.AddBossKillToCurrentRun(); // CORRECTED: Use specific boss kill stat method

        if (_rb != null) _rb.isKinematic = true; // Stop physics
        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null) mainCollider.enabled = false; // Prevent further collisions

        // Stop further actions
        StopAllCoroutines(); // Stop awakening, movement, spawning

        SetVisualsActive(false); // Hide main boss body
        foreach (BossGem gem in _attachedGems) // Ensure any remaining (should be none) gem visuals are also off
        {
            if (gem != null) gem.gameObject.SetActive(false);
        }
        // Detached gems might still be flying around; BossGem handles its own lifecycle.

        if (deathEffectPrefab != null) Instantiate(deathEffectPrefab, transform.position, transform.rotation);
        if (deathSound != null) GameSounds.PlayBossDeath(transform.position, deathSoundVolume);

        Destroy(gameObject, delayBeforeDestroy);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Use currentState to check if the boss itself is in a state where it can act
        if (currentState != BossState.Active && currentState != BossState.Awakening) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null && !playerHealth.isDead) // Check player's dead status
            {
                Debug.Log($"Boss ({name}): Touched player! Player dies.");
                playerHealth.Die();
            }
        }
    }
}