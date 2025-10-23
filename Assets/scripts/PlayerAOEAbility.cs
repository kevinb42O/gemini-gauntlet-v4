// --- PlayerAOEAbility.cs (FULL & FINAL - Corrected for API Changes) ---
using UnityEngine;
using GeminiGauntlet.Audio;
using System.Collections;
using System.Collections.Generic;
using CompanionAI;

public class PlayerAOEAbility : MonoBehaviour
{
    public static PlayerAOEAbility Instance { get; private set; }

    public enum AOEStatus { Unavailable, Ready, Active, Cooldown }
    public AOEStatus CurrentAOEStatus { get; private set; } = AOEStatus.Unavailable;
    
    // Event for PowerupDisplay integration
    public static event System.Action<PowerUpType, bool, float> OnPowerUpStatusChangedForHUD;
    
    // CRITICAL: Event for efficient charge synchronization with PowerupInventoryManager
    public event System.Action<int> OnChargesChanged;
    
    /// <summary>
    /// Get the current number of AOE charges available
    /// </summary>
    public int GetCurrentCharges() { return _aoeChargesFromPowerUp; }

    [Header("AOE Properties")]
    public GameObject visualEffectPrefab;
    public float radius = 35f;
    public float damagePerSecond = 30f;
    public float aoeActiveDuration = 5f;
    public float damageTickInterval = 0.5f;
    public float cooldownDuration = 30f;
    public LayerMask damageableLayers;
    public float visualSpawnOffsetY = 0.05f;
    public LayerMask groundPlacementLayerMask;

    [Header("Precise Ground Shot Properties")]
    public float groundRaycastStartHeightOffset = 100f;
    public float groundRaycastMaxDistanceForGroundShot = 150f;

    [Header("Sound")]
    public AudioClip activationSound;
    [Range(0f, 1f)] public float activationSoundVolume = 1.0f;
    private AudioSource _audioSource;

    private float _timeCooldownEndTime = 0f;
    private Transform _playerTransform;
    private Coroutine _aoeActiveCoroutine;
    private int _aoeChargesFromPowerUp = 0;
    private GameObject _currentAOEVisualEffect;
    
    [Header("Debug")]
    [Tooltip("Enable verbose logging to help diagnose issues")]
    public bool verboseDebugging = true;

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null) _playerTransform = playerGO.transform;
        else
        {
            _playerTransform = transform;
            Debug.LogWarning("PlayerAOEAbility: Player GameObject with tag 'Player' not found. Using this object's transform.", this);
        }

        if (groundPlacementLayerMask == 0) Debug.LogError("PlayerAOEAbility: 'Ground Placement Layer Mask' is not assigned!", this);
        if (damageableLayers == 0) Debug.LogError("PlayerAOEAbility: 'Damageable Layers' (for AOE field) is not assigned!", this);

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0.5f;
    }

    void OnEnable()
    {
        // CRITICAL FIX: PlayerAOEAbility should NOT subscribe to input events!
        // Input is handled by PowerupInventoryManager, which calls InitiateAOE() when AOE powerup is activated
        // Subscribing here causes DUPLICATE INPUT HANDLING and breaks the powerup system
        
        if (verboseDebugging)
        {
            Debug.Log("[PlayerAOEAbility] OnEnable - NOT subscribing to input (handled by PowerupInventoryManager)", this);
        }
    }
    
    // OBSOLETE: PlayerAOEAbility no longer subscribes to input events
    // Input is now handled exclusively by PowerupInventoryManager to prevent duplicate input handling
    // Keeping these methods commented for reference but they should NOT be used
    
    /*
    /// <summary>
    /// OBSOLETE: No longer used - causes duplicate input handling
    /// </summary>
    private System.Collections.IEnumerator WaitForPlayerInputHandler()
    {
        float timeout = 10f;
        float elapsed = 0f;
        
        while (PlayerInputHandler.Instance == null && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        if (PlayerInputHandler.Instance != null)
        {
            SubscribeToInputEvents();
            if (verboseDebugging)
            {
                Debug.Log($"[PlayerAOEAbility] Successfully subscribed to events after {elapsed:F1}s delay", this);
            }
        }
        else
        {
            Debug.LogError($"[PlayerAOEAbility] Failed to find PlayerInputHandler.Instance after {timeout}s timeout!", this);
        }
    }
    
    /// <summary>
    /// OBSOLETE: No longer used - causes duplicate input handling
    /// </summary>
    private void SubscribeToInputEvents()
    {
        PlayerInputHandler.Instance.OnMiddleMouseTapAction += TryActivateAOEByInput;
        if (verboseDebugging)
        {
            Debug.Log("[PlayerAOEAbility] Subscribed to OnMiddleMouseTapAction event", this);
        }
    }
    */

    void OnDisable()
    {
        // CRITICAL FIX: No longer subscribing to input events, so nothing to unsubscribe
        // PowerupInventoryManager handles all input and calls InitiateAOE() directly
    }

    void Start()
    {
        ResetForNewGame();
        if (verboseDebugging)
        {
            Debug.Log("[PlayerAOEAbility] Started and reset for new game - Input handled by PowerupInventoryManager", this);
        }
    }

    public void ResetForNewGame()
    {
        if (_aoeActiveCoroutine != null) { StopCoroutine(_aoeActiveCoroutine); _aoeActiveCoroutine = null; }
        
        // Clean up any existing visual effect
        if (_currentAOEVisualEffect != null)
        {
            Destroy(_currentAOEVisualEffect);
            _currentAOEVisualEffect = null;
        }
        
        CurrentAOEStatus = AOEStatus.Unavailable;
        _aoeChargesFromPowerUp = 0;
        _timeCooldownEndTime = Time.time;
        
        // Fire event for PowerupDisplay - AOE no longer available
        OnPowerUpStatusChangedForHUD?.Invoke(PowerUpType.AOEAttack, false, 0);
        
        UpdateUIManager();
    }

    void Update()
    {
        if (CurrentAOEStatus == AOEStatus.Cooldown && Time.time >= _timeCooldownEndTime)
        {
            CurrentAOEStatus = AOEStatus.Unavailable;
            UpdateUIManager();
        }
        else if (CurrentAOEStatus == AOEStatus.Cooldown)
        {
            UpdateUIManager();
        }
    }

    // OBSOLETE: This method caused duplicate input handling
    // PowerupInventoryManager now handles all middle mouse input and calls InitiateAOE() directly
    // Keeping commented for reference
    
    /*
    private void TryActivateAOEByInput()
    {
        Debug.Log($"[PlayerAOEAbility] Middle mouse input detected. Current status: {CurrentAOEStatus}, Charges: {_aoeChargesFromPowerUp}", this);
        
        if (CurrentAOEStatus == AOEStatus.Ready)
        {
            Debug.Log("[PlayerAOEAbility] Status is Ready, attempting to use AOE", this);
            
            if (PlayerOverheatManager.Instance != null && PlayerOverheatManager.Instance.CanAffordAOE())
            {
                Debug.Log("[PlayerAOEAbility] Heat capacity sufficient, initiating AOE", this);
                InitiateAOE();
            }
            else
            {
                DynamicPlayerFeedManager.Instance?.ShowCustomMessage("AOE: Not enough heat capacity!", 
                    DynamicPlayerFeedManager.Instance != null ? DynamicPlayerFeedManager.Instance.overheatWarningColor : Color.red, 
                    DynamicPlayerFeedManager.Instance != null ? DynamicPlayerFeedManager.Instance.feedIcons.warningIcon : null, 
                    false, 1.5f);
                    
                Debug.Log("[PlayerAOEAbility] Cannot afford AOE heat - insufficient capacity in OverheatManager", this);
                
                if (PlayerOverheatManager.Instance == null)
                {
                    Debug.LogError("[PlayerAOEAbility] PlayerOverheatManager.Instance is null", this);
                }
            }
        }
        else
        {
            Debug.Log($"[PlayerAOEAbility] Cannot activate - Current Status: {CurrentAOEStatus}", this);
        }
    }
    */

    public bool TryActivateAOEByExternalTrigger()
    {
        if (CurrentAOEStatus == AOEStatus.Ready)
        {
            if (PlayerOverheatManager.Instance != null && PlayerOverheatManager.Instance.CanAffordAOE())
            {
                InitiateAOE();
                return true;
            }
            else
            {
                // --- CORRECTED ---
                DynamicPlayerFeedManager.Instance?.ShowCustomMessage("AOE: Not enough heat capacity!", DynamicPlayerFeedManager.Instance.overheatWarningColor, DynamicPlayerFeedManager.Instance.feedIcons.warningIcon, false, 1.5f);
                Debug.Log("PlayerAOEAbility: Cannot afford AOE heat (external trigger).", this);
            }
        }
        else Debug.Log($"PlayerAOEAbility: AOE not ready (external trigger). Status: {CurrentAOEStatus}", this);
        return false;
    }


    public void GrantAOEChargeByPowerUp(int charges = 1)
    {
        if (charges <= 0)
        {
            Debug.LogWarning("[PlayerAOEAbility] Attempted to grant 0 or negative charges", this);
            return;
        }

        _aoeChargesFromPowerUp += charges;
        CurrentAOEStatus = AOEStatus.Ready;
        _timeCooldownEndTime = Time.time;

        Debug.Log($"[PlayerAOEAbility] AOE Power-up granted {charges} charge(s). Total: {_aoeChargesFromPowerUp}. Status: {CurrentAOEStatus}", this);
        
        // Fire event for PowerupDisplay
        OnPowerUpStatusChangedForHUD?.Invoke(PowerUpType.AOEAttack, true, _aoeChargesFromPowerUp);
        
        // CRITICAL: Fire charge change event for PowerupInventoryManager sync
        OnChargesChanged?.Invoke(_aoeChargesFromPowerUp);
        
        UpdateUIManager();
        // The power-up itself will now call ShowPowerUpCollected.
        // This keeps the responsibility in one place.
        
        // Verify that everything is updated properly
        if (verboseDebugging)
        {
            Debug.Log($"[PlayerAOEAbility] After update - Charges: {_aoeChargesFromPowerUp}, Status: {CurrentAOEStatus}, UIManager active: {UIManager.Instance != null}", this);
        }
    }

    public void InitiateAOE()
    {
        if (_playerTransform == null) { Debug.LogError("PlayerAOEAbility: PlayerTransform is null, cannot initiate AOE.", this); return; }
        if (PlayerOverheatManager.Instance == null || !PlayerOverheatManager.Instance.CanAffordAOE()) { Debug.Log("PlayerAOEAbility: OverheatManager missing or cannot afford AOE heat.", this); return; }
        if (damageableLayers == 0 || groundPlacementLayerMask == 0) { Debug.LogError("PlayerAOEAbility: Critical LayerMasks not set for AOE.", this); return; }

        if (_aoeChargesFromPowerUp <= 0)
        {
            Debug.LogWarning("PlayerAOEAbility: InitiateAOE called but no charges available.");
            return;
        }

        _aoeChargesFromPowerUp--;
        
        // CRITICAL: Fire charge change event immediately after decrement
        OnChargesChanged?.Invoke(_aoeChargesFromPowerUp);
        Debug.Log($"[PlayerAOEAbility] AOE charge decremented. New total: {_aoeChargesFromPowerUp}", this);

        PlayerOverheatManager.Instance.ApplyHeatForAOE();
        Vector3 effectSpawnPosition = GetAOESpawnPosition();

        // Destroy any existing visual effect before creating a new one
        if (_currentAOEVisualEffect != null)
        {
            Destroy(_currentAOEVisualEffect);
            _currentAOEVisualEffect = null;
        }

        if (visualEffectPrefab != null) 
        {
            _currentAOEVisualEffect = Instantiate(visualEffectPrefab, effectSpawnPosition, Quaternion.identity);
        }
        if (activationSound != null) GameSounds.PlayAOEActivation(effectSpawnPosition, activationSoundVolume);

        if (_aoeActiveCoroutine != null) StopCoroutine(_aoeActiveCoroutine);
        _aoeActiveCoroutine = StartCoroutine(HandleAOEDamageOverTime_Coroutine(effectSpawnPosition));

        CurrentAOEStatus = AOEStatus.Active;
        
        // Fire event for PowerupDisplay - show countdown during active phase
        OnPowerUpStatusChangedForHUD?.Invoke(PowerUpType.AOEAttack, true, aoeActiveDuration);
        
        UpdateUIManager();
        // --- CORRECTED ---
        DynamicPlayerFeedManager.Instance?.ShowCustomMessage("AOE Activated!", DynamicPlayerFeedManager.Instance.powerUpColor, DynamicPlayerFeedManager.Instance.feedIcons.GetIconForPowerUp(PowerUpType.AOEAttack), true, aoeActiveDuration);
    }

    private Vector3 GetAOESpawnPosition()
    {
        if (_playerTransform == null) return Vector3.zero + Vector3.up * visualSpawnOffsetY;

        // Calculate a more accurate feet position (significantly lower than before)
        Vector3 playerFeetPosition = new Vector3(
            _playerTransform.position.x,
            _playerTransform.position.y - 1.8f, // Much lower offset to get actual feet position
            _playerTransform.position.z
        );
        
        // First, try to find the actual ground beneath the player
        RaycastHit groundHit;
        Vector3 raycastStartPoint = _playerTransform.position + Vector3.up * 0.1f; // Start just above player
        float shortRaycastDistance = 3.0f; // Short distance check for immediate ground
        
        // Debug output for position calculations
        Debug.Log($"[PlayerAOEAbility] Player position: {_playerTransform.position}, calculated feet: {playerFeetPosition}");
        
        Vector3 spawnPosition;
        
        // First try a very short raycast directly down to find immediate ground
        if (Physics.Raycast(raycastStartPoint, Vector3.down, out groundHit, shortRaycastDistance, groundPlacementLayerMask))
        {
            // We're on a platform/ground - use the hit point for precise positioning
            spawnPosition = groundHit.point;
            Debug.Log($"[PlayerAOEAbility] Ground detected directly below player at {spawnPosition}, distance: {groundHit.distance}");
        }
        else
        {
            // Try a longer raycast from higher up (for flying mode or high positions)
            Vector3 highRaycastPoint = new Vector3(
                _playerTransform.position.x,
                _playerTransform.position.y + groundRaycastStartHeightOffset,
                _playerTransform.position.z
            );
            
            // For distant ground
            if (Physics.Raycast(highRaycastPoint, Vector3.down, out groundHit, groundRaycastMaxDistanceForGroundShot, groundPlacementLayerMask))
            {
                // Found distant ground
                spawnPosition = groundHit.point;
                Debug.Log($"[PlayerAOEAbility] Distant ground found at {spawnPosition}");
            }
            else
            {
                // No ground at all - use the calculated feet position
                spawnPosition = playerFeetPosition;
                Debug.Log($"[PlayerAOEAbility] No ground detected - using calculated feet position: {spawnPosition}");
            }
        }
        
        // Add tiny offset to prevent z-fighting and keep effect visible
        spawnPosition += Vector3.up * visualSpawnOffsetY;
        Debug.Log($"[PlayerAOEAbility] Final AOE position: {spawnPosition}");
        return spawnPosition;
    }

    private IEnumerator HandleAOEDamageOverTime_Coroutine(Vector3 aoeCenter)
    {
        float elapsedTime = 0f;
        float damagePerTick = damagePerSecond * damageTickInterval;
        HashSet<GameObject> damagedThisTick = new HashSet<GameObject>();

        while (elapsedTime < aoeActiveDuration)
        {
            damagedThisTick.Clear();
            Collider[] hits = Physics.OverlapSphere(aoeCenter, radius, damageableLayers);

            foreach (Collider hitCollider in hits)
            {
                GameObject hitRootObject = hitCollider.attachedRigidbody ? hitCollider.attachedRigidbody.gameObject : hitCollider.gameObject;
                if (damagedThisTick.Contains(hitRootObject)) continue;
                Vector3 actualDamageHitPointForEnemy = hitCollider.bounds.ClosestPoint(aoeCenter);

                Vector3 hitDirection = (hitCollider.transform.position - aoeCenter).normalized;
                if (hitDirection == Vector3.zero) hitDirection = transform.forward; // Fallback direction

                IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    bool canDamage = false;
                    if (hitCollider.TryGetComponent<SkullEnemy>(out SkullEnemy enemyScript) && !enemyScript.IsDead()) canDamage = true;
                    else if (hitCollider.TryGetComponent<Gem>(out Gem gemScript) && !gemScript.IsDetached()) canDamage = true;
                    else if (hitCollider.TryGetComponent<BossGem>(out BossGem bossGemScript) && !bossGemScript.IsDetached()) canDamage = true;
                    // CRITICAL FIX: Allow AOE to damage ENEMY companions only
                    else if (hitCollider.TryGetComponent<EnemyCompanionBehavior>(out EnemyCompanionBehavior enemyCompanion) && enemyCompanion.isEnemy) canDamage = true;

                    if (canDamage)
                    {
                        damageable.TakeDamage(damagePerTick, actualDamageHitPointForEnemy, hitDirection);
                        damagedThisTick.Add(hitRootObject);
                    }
                }
            }
            
            elapsedTime += damageTickInterval;
            
            // Update PowerupDisplay with countdown every tick
            float timeRemaining = aoeActiveDuration - elapsedTime;
            OnPowerUpStatusChangedForHUD?.Invoke(PowerUpType.AOEAttack, true, timeRemaining);
            
            yield return new WaitForSeconds(damageTickInterval);
        }
        
        // Destroy the visual effect when the AOE duration expires
        if (_currentAOEVisualEffect != null)
        {
            Destroy(_currentAOEVisualEffect);
            _currentAOEVisualEffect = null;
            
            if (verboseDebugging)
            {
                Debug.Log("[PlayerAOEAbility] AOE visual effect destroyed after duration expired", this);
            }
        }
        
        _aoeActiveCoroutine = null;
        SetAOEOnCooldownOrReady();
    }

    private void SetAOEOnCooldownOrReady()
    {
        if (_aoeChargesFromPowerUp > 0)
        {
            CurrentAOEStatus = AOEStatus.Ready;
            // Fire event for PowerupDisplay - show charges available
            OnPowerUpStatusChangedForHUD?.Invoke(PowerUpType.AOEAttack, true, _aoeChargesFromPowerUp);
        }
        else
        {
            CurrentAOEStatus = AOEStatus.Cooldown;
            _timeCooldownEndTime = Time.time + cooldownDuration;
            if (UIManager.Instance != null) UIManager.Instance.StartAOETimeCooldownVisuals(cooldownDuration);
            if (_aoeChargesFromPowerUp <= 0)
            {
                CurrentAOEStatus = AOEStatus.Unavailable;
                OnPowerUpStatusChangedForHUD?.Invoke(PowerUpType.AOEAttack, false, 0);
                // Charge change event already fired above when decrementing
            }
            UpdateUIManager();
        }
    }
    
    private void UpdateUIManager()
    {
        if (UIManager.Instance == null) 
        {
            Debug.LogWarning("[PlayerAOEAbility] UIManager.Instance is null during UpdateUIManager call", this);
            return;
        }
        
        if (verboseDebugging)
        {
            Debug.Log($"[PlayerAOEAbility] Updating UI for status: {CurrentAOEStatus}, charges: {_aoeChargesFromPowerUp}", this);
        }
        
        switch (CurrentAOEStatus)
        {
            case AOEStatus.Unavailable:
                UIManager.Instance.DisplayAOEUnavailable();
                if (verboseDebugging) Debug.Log("[PlayerAOEAbility] UI updated to Unavailable", this);
                break;
            case AOEStatus.Active:
                UIManager.Instance.SetAOEToActiveStateDisplay();
                if (verboseDebugging) Debug.Log("[PlayerAOEAbility] UI updated to Active", this);
                break;
            case AOEStatus.Cooldown:
                float remainingTime = Mathf.Max(0, _timeCooldownEndTime - Time.time);
                UIManager.Instance.DisplayAOETimeCooldown(remainingTime, cooldownDuration);
                if (verboseDebugging) Debug.Log($"[PlayerAOEAbility] UI updated to Cooldown: {remainingTime}/{cooldownDuration}s remaining", this);
                break;
            case AOEStatus.Ready:
                UIManager.Instance.DisplayAOEReady(_aoeChargesFromPowerUp);
                if (verboseDebugging) Debug.Log($"[PlayerAOEAbility] UI updated to Ready with {_aoeChargesFromPowerUp} charges", this);
                break;
        }
    }

    void OnDestroy()
    {
        if (_aoeActiveCoroutine != null) { StopCoroutine(_aoeActiveCoroutine); _aoeActiveCoroutine = null; }
        
        // Clean up visual effect on destroy
        if (_currentAOEVisualEffect != null)
        {
            Destroy(_currentAOEVisualEffect);
            _currentAOEVisualEffect = null;
        }
        
        // CRITICAL FIX: No longer subscribing to input events, so nothing to unsubscribe
    }
}