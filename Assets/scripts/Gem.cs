// --- Gem.cs (Modified for Variable Attraction Speed) ---
using UnityEngine;
using System.Collections;

public class Gem : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public float maxHealth = 100f;
    private float _currentHealth;
    public float CurrentHealth
    {
        get { return _currentHealth; }
        private set
        {
            _currentHealth = Mathf.Clamp(value, 0f, maxHealth);
        }
    }

    [Header("Effects & Sounds")]
    public GameObject detachEffectPrefab;
    protected GemSoundManager gemSoundManager;

    [Header("Behavior After Detachment")]
    public float gentleReleaseForce = 1f;
    public float detachedLifetime = 30f;

    [Header("Attraction Behavior")]
    [Tooltip("Distance from which this gem can be attracted to the player.")]
    public float attractionRange = 1000f;
    [Tooltip("Base speed at which the gem moves towards the player.")]
    public float baseAttractionSpeed = 150f;
    public float collectionDistanceThreshold = 1.5f;
    private float _currentAttractionSpeed;

    [Header("Hit Visual Effects")]
    [Tooltip("Duration of glow effect when hit by raycast.")]
    public float hitGlowDuration = 0.2f;
    [Tooltip("Glow intensity multiplier when hit.")]
    public float hitGlowIntensity = 2.0f;
    private Material _originalMaterial;
    private Color _originalEmissionColor;
    private bool _isGlowing = false;

    protected Rigidbody _rb;
    private TowerController _towerController;
    private SnakeController _snakeController;
    protected bool _isDetached = false;
    private bool _isCollected = false;
    private bool _isBeingAttracted = false;
    private Transform _attractionTargetPlayer;
    private bool _willBeCollectedByPrimaryHand;

    private Collider _gemCollider;
    private Renderer _gemRenderer;
    private Coroutine _delayedDestroyCoroutine;
    
    // Store the original scale of the gem prefab to maintain consistent sizing
    private Vector3 _originalPrefabScale = Vector3.one;


    protected virtual void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            _rb = gameObject.AddComponent<Rigidbody>();
        }
        _rb.isKinematic = true;
        _rb.useGravity = false;
        CurrentHealth = maxHealth;

        // CRITICAL FIX: Ensure gem always has a collider
        _gemCollider = GetComponent<Collider>();
        if (_gemCollider == null)
        {
            // Add a default BoxCollider if no collider exists
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true; // Start as trigger for attraction
            _gemCollider = boxCollider;
        }
        _gemRenderer = GetComponent<MeshRenderer>() ?? (Renderer)GetComponent<SpriteRenderer>();

        // Initialize hit glow materials
        InitializeHitGlowMaterials();

        // Get or add GemSoundManager component
        gemSoundManager = GetComponent<GemSoundManager>();
        if (gemSoundManager == null)
        {
            gemSoundManager = gameObject.AddComponent<GemSoundManager>();
        }
        
        // Initialize GemSoundManager for mixer-based audio
        if (gemSoundManager == null)
        {
            gemSoundManager = GetComponent<GemSoundManager>();
            if (gemSoundManager == null)
                gemSoundManager = gameObject.AddComponent<GemSoundManager>();
        }
        
        // Store the original prefab scale to preserve gem sizing consistency
        _originalPrefabScale = transform.localScale;
        
        _currentAttractionSpeed = baseAttractionSpeed;
    }

    protected virtual void Start()
    {
        // Only find TowerController if this is not a BossGem
        if (!(this is BossGem)) // This check ensures BossGem's Start (if it overrides) isn't skipped
        {
            _towerController = GetComponentInParent<TowerController>();
            // No error needed if null, as gems might exist without towers (e.g. dropped by player)
        }
    }

    public void SetSnakeController(SnakeController controller)
    {
        _snakeController = controller;
    }
    
    /// <summary>
    /// Finds the platform parent by traversing up the hierarchy
    /// </summary>
    private Transform FindPlatformParent()
    {
        Transform current = transform.parent;
        
        // Traverse up the hierarchy to find platform
        while (current != null)
        {
            // Check if this is a platform (typically has a PlatformTrigger or similar component)
            if (current.GetComponent<PlatformTrigger>() != null || 
                current.name.ToLower().Contains("platform") ||
                current.GetComponent<TowerSpawner>() != null)
            {
                return current;
            }
            current = current.parent;
        }
        
        // If no platform found, try to get it from the tower controller
        if (_towerController != null && _towerController.transform.parent != null)
        {
            return _towerController.transform.parent;
        }
        
        return null; // No platform found
    }

    // --- MODIFICATION START: Moved attraction logic to FixedUpdate for reliable physics ---
    void FixedUpdate()
    {
        if (_isBeingAttracted && !_isCollected)
        {
            // Critical fix: Make sure attraction continues even if target is temporarily null
            // This prevents freezing when player reference is lost
            if (_attractionTargetPlayer == null)
            {
                // Try to re-acquire player reference if lost
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    _attractionTargetPlayer = playerObj.transform;
                }
            }
            
            // Continue with attraction if we have a target
            if (_attractionTargetPlayer != null)
            {
                HandleAttraction();
            }
        }
    }
    // --- MODIFICATION END ---
    
    // Left empty as logic was moved to FixedUpdate
    void Update() {}

    /// <summary>
    /// Initialize materials for hit glow effect
    /// </summary>
    private void InitializeHitGlowMaterials()
    {
        if (_gemRenderer != null)
        {
            // Create a copy of the material to avoid modifying the original asset
            _originalMaterial = _gemRenderer.material;
            _gemRenderer.material = new Material(_originalMaterial);
            
            // Store original emission color
            if (_gemRenderer.material.HasProperty("_EmissionColor"))
            {
                _originalEmissionColor = _gemRenderer.material.GetColor("_EmissionColor");
            }
            else
            {
                _originalEmissionColor = Color.black;
            }
        }
    }

    /// <summary>
    /// Coroutine that creates a brief glow effect when gem is hit
    /// </summary>
    private IEnumerator HitGlowEffect()
    {
        if (_gemRenderer == null || _gemRenderer.material == null)
            yield break;

        _isGlowing = true;
        
        // Enable emission if the material supports it
        if (_gemRenderer.material.HasProperty("_EmissionColor"))
        {
            // Calculate glow color (make it brighter version of original or use white)
            Color glowColor = _originalEmissionColor;
            if (glowColor == Color.black)
            {
                // If no original emission, use a bright white/yellow glow
                glowColor = Color.white * hitGlowIntensity;
            }
            else
            {
                glowColor *= hitGlowIntensity;
            }
            
            // Apply glow effect
            _gemRenderer.material.SetColor("_EmissionColor", glowColor);
            _gemRenderer.material.EnableKeyword("_EMISSION");
            
            // Wait for glow duration
            yield return new WaitForSeconds(hitGlowDuration);
            
            // Restore original emission
            _gemRenderer.material.SetColor("_EmissionColor", _originalEmissionColor);
            if (_originalEmissionColor == Color.black)
            {
                _gemRenderer.material.DisableKeyword("_EMISSION");
            }
        }
        else
        {
            // Fallback: briefly change the main color if emission isn't supported
            Color originalColor = _gemRenderer.material.color;
            Color glowColor = Color.Lerp(originalColor, Color.white, 0.7f);
            
            _gemRenderer.material.color = glowColor;
            yield return new WaitForSeconds(hitGlowDuration);
            _gemRenderer.material.color = originalColor;
        }
        
        _isGlowing = false;
    }

    // --- MODIFICATION START: Using Rigidbody.MovePosition for stable kinematic movement ---
    private void HandleAttraction()
    {
        if (_attractionTargetPlayer == null)
        {
            return;
        }

        // CRITICAL FIX: Always ensure we're using a fresh player position reference
        Vector3 targetPosition = _attractionTargetPlayer.position;
        
        // Minimize debug logs to avoid overwhelming the console, just output distance occasionally
        if (Time.frameCount % 30 == 0)
        {
            float distance = Vector3.Distance(transform.position, targetPosition);
        }
        
        // CRITICAL FIX: Reset any potential physics state that might be causing freezing
        if (_rb == null)
        {
            _rb = GetComponent<Rigidbody>();
            if (_rb == null)
            {
                _rb = gameObject.AddComponent<Rigidbody>();
            }
        }
        
        // CRITICAL FIX: Reset rigidbody state for kinematic movement
        // NOTE: Don't set velocity on kinematic bodies - Unity doesn't allow it
        _rb.isKinematic = true;
        _rb.useGravity = false;
        _rb.interpolation = RigidbodyInterpolation.Interpolate; // Smoother movement
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Better collision
        
        // Only reset velocities if NOT kinematic (for when gem becomes dynamic later)
        if (!_rb.isKinematic)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
        
        // CRITICAL FIX: Ensure attraction speed is not zero
        if (_currentAttractionSpeed < 5f)
        {
            _currentAttractionSpeed = 15f; // Force a reasonable speed
        }
        
        // CRITICAL FIX: Use proper kinematic Rigidbody movement for attraction
        Vector3 oldPosition = transform.position;
        Vector3 direction = (targetPosition - transform.position).normalized;
        float moveDistance = _currentAttractionSpeed * Time.fixedDeltaTime;
        Vector3 newPosition = transform.position + direction * moveDistance;
        
        // Debug movement calculation every few frames
        if (Time.frameCount % 60 == 0)
        {
        }
        
        // CRITICAL FIX: Use direct transform movement instead of MovePosition to avoid CharacterController conflicts
        // CharacterController players don't interact well with kinematic Rigidbody MovePosition
        transform.position = newPosition;
        
        // Update Rigidbody position to match (prevents desync)
        if (_rb != null)
        {
            _rb.position = newPosition;
        }
        
        // Debug the actual movement result
        if (Time.frameCount % 60 == 0)
        {
            float actualDistance = Vector3.Distance(transform.position, targetPosition);
        }

        // Check distance to target to finalize collection
        float currentDistance = Vector3.Distance(transform.position, targetPosition);
        
        if (currentDistance < collectionDistanceThreshold)
        {
            FinalizeCollection(_willBeCollectedByPrimaryHand);
        }
    }

    // --- MODIFICATION END ---

    // Legacy simple damage method kept for backward compatibility
    public virtual void TakeDamage(float amount)
    {
        TakeDamage(amount, transform.position, Vector3.zero);
    }

    // --- MODIFICATION START: Gem is now invulnerable after being detached ---
    // New damage method from IDamageable
    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        
        // A gem that is detached, being collected, or already flying to the player should not be damageable.
        if (IsDetached() || IsCollected() || IsBeingAttracted())
        {
            return;
        }

        CurrentHealth -= amount;
        
        // Trigger hit glow effect
        if (!_isGlowing && _gemRenderer != null)
        {
            StartCoroutine(HitGlowEffect());
        }
        
        if (gemSoundManager != null)
        {
            // Play gem hit sound when shot
            gemSoundManager.PlayGemHit();
        }

        // If health drops to zero, detach the gem.
        // The check for IsDetached() is handled by the guard clause at the start of the method.
        if (CurrentHealth <= 0)
        {
            DetachFromSource();
        }
    }
    // --- MODIFICATION END ---

    protected virtual void DetachFromSource()
    {
        if (_isDetached) 
        {
            return; // Already detached
        }
        
        _isDetached = true;

        // Store tower reference before reparenting
        TowerController towerToNotify = _towerController;
        SnakeController snakeToNotify = _snakeController;
        
        if (gemSoundManager != null)
        {
            gemSoundManager.PlayGemSpawn(); // Use spawn sound for detach
        }
        if (detachEffectPrefab != null) Instantiate(detachEffectPrefab, transform.position, Quaternion.identity);

        // Preserve the original gem scale when detaching (keep original size)
        transform.localScale = _originalPrefabScale;
        
        // CRITICAL FIX: Parent to platform IMMEDIATELY to prevent flying off
        if (_towerController != null)
        {
            // Find the platform the tower is on and parent gem to it
            Transform platformParent = _towerController.transform.parent;
            if (platformParent != null)
            {
                transform.SetParent(platformParent, true);
            }
            else
            {
            }
        }
        else
        {
        }
        
        // --- XP SYSTEM: XP is granted only when gem is collected, not when destroyed ---
        // Removed XP grant from destruction - gems should only give XP when collected
        
        // CRITICAL FIX: Notify controllers AFTER reparenting so tower can count gems correctly
        if (towerToNotify != null) 
        { 
            towerToNotify.OnGemDestroyed(this); 
        }
        if (snakeToNotify != null) 
        { 
            snakeToNotify.OnGemDestroyed(); 
        }

        if (_rb != null)
        {
            _rb.isKinematic = false; 
            _rb.useGravity = true;
            _rb.linearVelocity = Vector3.zero; 
            _rb.angularVelocity = Vector3.zero; // Reset physics state
            Vector3 randomForceDir = (Random.insideUnitSphere.normalized * 0.7f + Vector3.up * 0.3f).normalized;
            _rb.AddForce(randomForceDir * gentleReleaseForce, ForceMode.Impulse);
            _rb.AddTorque(Random.insideUnitSphere * gentleReleaseForce * 0.5f, ForceMode.Impulse);
        }
        else
        {
        }
        
        if (_gemCollider != null) 
        { 
            _gemCollider.isTrigger = false; // Make it a solid object if it wasn't
        }
        else
        {
        }

        if (gemSoundManager != null)
        {
            gemSoundManager.StartGemHumming();
        }

        if (detachedLifetime > 0)
        {
            if (_delayedDestroyCoroutine != null) StopCoroutine(_delayedDestroyCoroutine);
            _delayedDestroyCoroutine = StartCoroutine(DestroyAfterTimer(detachedLifetime));
        }
        else
        {
        }
    }

    private IEnumerator DestroyAfterTimer(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (!IsCollected() && !IsBeingAttracted()) // Only destroy if not picked up
        {
            if (gemSoundManager != null)
            {
                gemSoundManager.StopGemHumming();
            }
            
            // Destroy the gem
            Destroy(gameObject);
        }
        else
        {
        }
    }

    public void StartAttractionToPlayer(Transform playerTransform, bool forPrimaryHand)
    {
        Debug.Log($"[Gem] StartAttractionToPlayer called - Player: {playerTransform?.name}, PrimaryHand: {forPrimaryHand}");
        
        if (playerTransform == null) { 
            Debug.LogError("[Gem] StartAttractionToPlayer: playerTransform is null!");
            return; 
        }
        // Only attract if not already collected/being attracted
        if (IsCollected() || _isBeingAttracted) { 
            Debug.Log($"[Gem] StartAttractionToPlayer: Already collected ({IsCollected()}) or being attracted ({_isBeingAttracted})");
            return; 
        }

        // CRITICAL FIX: Always force detach gems during collection attempts to fix collection issues
        // This ensures gems that might be in a broken state will still be collectable
        _isDetached = true; // Force detached state
        if (_towerController != null) 
        {
            _towerController.OnGemDestroyed(this);
        }
        
        // Run normal detachment logic to ensure proper setup
        if (!_isDetached)
        {
            DetachFromSource(); // This will handle tower notification and detachment logic
        }

        _isBeingAttracted = true;
        Debug.Log($"[Gem] Attraction started! _isBeingAttracted = {_isBeingAttracted}");
        
        // Prefer the main camera (player head) as attraction target so gems fly toward view instead of feet
        Transform preferredTarget = Camera.main != null ? Camera.main.transform : playerTransform;
        _attractionTargetPlayer = preferredTarget;
        _willBeCollectedByPrimaryHand = forPrimaryHand;
        Debug.Log($"[Gem] Attraction target set to: {_attractionTargetPlayer?.name}, PrimaryHand: {forPrimaryHand}");

        if (PlayerProgression.Instance != null)
        {
            _currentAttractionSpeed = baseAttractionSpeed * PlayerProgression.Instance.GetCurrentGemAttractionSpeedMultiplier();
        }
        else
        {
            _currentAttractionSpeed = baseAttractionSpeed;
        }

        if (_rb != null)
        {
            if (!_rb.isKinematic) { _rb.linearVelocity = Vector3.zero; _rb.angularVelocity = Vector3.zero; }
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }
        if (_gemCollider != null) { _gemCollider.isTrigger = true; } // Make it passable while attracting

        if (gemSoundManager != null && _isDetached)
        {
            gemSoundManager.StartGemHumming();
        }

        if (_delayedDestroyCoroutine != null) { StopCoroutine(_delayedDestroyCoroutine); _delayedDestroyCoroutine = null; }
    }

    private void FinalizeCollection(bool collectedByPrimaryHand)
    {
        if (_isCollected) return;
        _isCollected = true;
        _isBeingAttracted = false;

        if (gemSoundManager != null)
        {
            gemSoundManager.StopGemHumming();
        }

        if (PlayerProgression.Instance != null)
        {
            // Register gem with progression system
            PlayerProgression.Instance.RegisterGemCollection(collectedByPrimaryHand);
            
            // --- XP SYSTEM: Grant XP for gem collection (UPGRADED!) ---
            GeminiGauntlet.Progression.XPHooks.OnGemCollected();
            
            // Add gem directly to InventoryManager gem slot (both exist in game scene)
            InventoryManager inventoryManager = FindFirstObjectByType<InventoryManager>();
            if (inventoryManager != null)
            {
                // Load the standard gem item data
                GemItemData gemItemData = Resources.Load<GemItemData>("Items/GemItemData");
                if (gemItemData != null)
                {
                    // Add gem directly to inventory gem slot using TryAddItem (routes to gem slot automatically)
                    bool success = inventoryManager.TryAddItem(gemItemData, 1);
                }
                else
                {
                }
            }
            else
            {
            }
            
            // Display gem collection message with hand information
            if (FlavorTextManager.Instance != null)
            {
                string handName = collectedByPrimaryHand ? "LEFT" : "RIGHT";
                string message = FlavorTextManager.Instance.RecordGemCollection(handName, collectedByPrimaryHand);
                
                // Show the message instantly (highest priority)
                if (CognitiveFeedManager.Instance != null && !string.IsNullOrEmpty(message))
                {
                    // Use instant display for immediate feedback with short display time
                    CognitiveFeedManager.Instance.ShowInstantMessage(message, 1.5f);
                }
                else
                {
                    // No cognitive feed manager available
                }
            }
            else
            {
                // Gem value not positive
            }
        }
        else
        {
            // No gem value configured
        }

        if (PlayerOverheatManager.Instance != null)
        {
            PlayerOverheatManager.Instance.ApplyHeatReductionFromGemCollection(collectedByPrimaryHand);
        }
        else
        {
            // No overheat manager available
        }

        if (gemSoundManager != null) gemSoundManager.PlayGemCollection();

        if (_gemRenderer != null) _gemRenderer.enabled = false;
        if (_gemCollider != null) _gemCollider.enabled = false;
        if (_rb != null)
        {
            // Stop velocity before making kinematic to avoid warnings
            if (!_rb.isKinematic)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
            _rb.isKinematic = true;
        }

        if (_delayedDestroyCoroutine != null) { StopCoroutine(_delayedDestroyCoroutine); _delayedDestroyCoroutine = null; }

        gameObject.SetActive(false); // Deactivate instead of immediate destroy for pooling potential or delayed destroy
        Destroy(gameObject, 2f); // Destroy after a delay to ensure sounds play out
    }

    public bool IsCollected() { return _isCollected; }
    public bool IsDetached() { return _isDetached; }
    public bool IsBeingAttracted() { return _isBeingAttracted; }
    
    /// <summary>
    /// Sets the original scale for the gem - useful for ensuring consistent sizing when spawned from different sources
    /// </summary>
    /// <param name="scale">The scale to set as the gem's original size</param>
    public void SetOriginalScale(Vector3 scale)
    {
        _originalPrefabScale = scale;
        transform.localScale = scale;
    }
    
    /// <summary>
    /// Gets the original prefab scale of this gem
    /// </summary>
    /// <returns>The original scale vector</returns>
    public Vector3 GetOriginalScale()
    {
        return _originalPrefabScale;
    }

    protected void PlayOneShotSoundAtPoint(AudioClip clip, Vector3 position, float volume, float spatialBlend = 0.85f, float minPitch = 0.90f, float maxPitch = 1.10f)
    {
        if (clip == null) return;
        
        // MODERN APPROACH: Use GemSoundManager if available (centralized sound system)
        if (gemSoundManager != null)
        {
            // Let the centralized sound manager handle it (it will play the correct sound based on gem state)
            gemSoundManager.PlayGemCollection();
            return;
        }
        
        // LEGACY FALLBACK: Create temporary AudioSource only if no sound manager exists
        GameObject soundGameObject = new GameObject($"TempAudio_Gem_{clip.name}");
        soundGameObject.transform.position = position;
        AudioSource audioSource = soundGameObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.spatialBlend = spatialBlend; // 3D sound
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.loop = false;
        // Configure rolloff for 3D sound
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 50f;
        audioSource.Play();
        Destroy(soundGameObject, (clip.length / Mathf.Max(0.01f, Mathf.Abs(audioSource.pitch))) + 0.2f); // Safety margin
    }

    // Called when the GameObject is disabled (e.g. by pooling or SetActive(false))
    protected virtual void OnDisable()
    {
        if (_delayedDestroyCoroutine != null) { StopCoroutine(_delayedDestroyCoroutine); _delayedDestroyCoroutine = null; }
        if (gemSoundManager != null)
        {
            gemSoundManager.StopGemHumming();
        }
    }
    // Called when the GameObject is being destroyed
    protected virtual void OnDestroy()
    {
        // Should be handled by OnDisable, but as a fallback
        if (gemSoundManager != null)
        {
            gemSoundManager.StopGemHumming();
        }
    }
}