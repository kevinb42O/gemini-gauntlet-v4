using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GeminiGauntlet.Audio;

[RequireComponent(typeof(Collider))]
public class PlatformTrigger : MonoBehaviour
{
    [Header("Activation Effects")]
    public Light activationLight;
    [ColorUsage(true, true)] public Color lightSafeColor = Color.green;
    [ColorUsage(true, true)] public Color lightActiveColor = Color.red * 2f;
    [ColorUsage(true, true)] public Color lightPulseColor = Color.white * 3f;
    public int lightPulseCount = 2;
    public float lightPulseSpeed = 0.15f;
    public float lightSettleSpeed = 2.5f;
    public AudioClip platformActivateSound;
    [Range(0f, 1f)] public float platformActivateSoundVolume = 0.7f;

    [Header("Platform Identification")]
    [Tooltip("An index to group platforms, e.g., by the ring they belong to.")]
    public int ringIndex = 0;
    
    [Header("Tower Management")]
    [Tooltip("The TowerSpawner associated with this platform. Will auto-find if not assigned.")]
    public TowerSpawner associatedTowerSpawner;
    
    private Coroutine _lightAnimationCoroutine;
    private AudioSource _localAudioSource;
    private bool _hasActivationSoundPlayedThisSession = false;
    private static AudioSource _globallyActiveActivationSoundSource;
    private static AudioClip _globallyActiveActivationClip;
    private bool _isPlayerCurrentlyOnPlatform = false;

    void Awake()
    {
        // Set up collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider>();
        }
        if (!col.isTrigger)
        {
            col.isTrigger = true;
        }

        // Set up audio source (optional)
        _localAudioSource = GetComponent<AudioSource>();
        if (_localAudioSource == null && platformActivateSound != null)
        {
            _localAudioSource = gameObject.AddComponent<AudioSource>();
            _localAudioSource.playOnAwake = false;
            _localAudioSource.loop = false;
            _localAudioSource.spatialBlend = 0f;
        }

        // Set up light
        if (activationLight == null)
        {
            activationLight = GetComponentInChildren<Light>();
        }
        
        // Find associated TowerSpawner if not assigned
        if (associatedTowerSpawner == null)
        {
            associatedTowerSpawner = GetComponentInChildren<TowerSpawner>();
            if (associatedTowerSpawner == null)
            {
                associatedTowerSpawner = GetComponentInParent<TowerSpawner>();
            }
        }
    }

    void Start()
    {
        if (activationLight != null)
        {
            activationLight.color = lightSafeColor;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"PlatformTrigger ({gameObject.name}): OnTriggerEnter with {other.gameObject.name}, Tag: {other.tag}");
        
        if (other.CompareTag("Player") && !_isPlayerCurrentlyOnPlatform)
        {
            Debug.Log($"PlatformTrigger ({gameObject.name}): PLAYER ENTERED PLATFORM!");
            _isPlayerCurrentlyOnPlatform = true;
            TriggerLightAnimation(true);
            
            // DEBUG: Check TowerSpawner connection
            Debug.Log($"PlatformTrigger ({gameObject.name}): associatedTowerSpawner = {(associatedTowerSpawner != null ? associatedTowerSpawner.name : "NULL")}");
            
            // Notify the associated TowerSpawner that player entered this platform
            if (associatedTowerSpawner != null)
            {
                Debug.Log($"PlatformTrigger ({gameObject.name}): Calling TowerSpawner.OnPlayerEnteredPlatform()");
                associatedTowerSpawner.OnPlayerEnteredPlatform();
                Debug.Log($"PlatformTrigger ({gameObject.name}): TowerSpawner notified - towers should spawn now!");
            }
            else
            {
                Debug.LogError($"PlatformTrigger ({gameObject.name}): CRITICAL - No associated TowerSpawner found! You need to:");
                Debug.LogError($"  1. Create a TowerSpawner component on this platform or a child object");
                Debug.LogError($"  2. Assign this PlatformTrigger to the TowerSpawner's 'Platform Trigger' field in inspector");
                Debug.LogError($"  3. Or manually assign the TowerSpawner to this PlatformTrigger's 'Associated Tower Spawner' field");
            }
            
            // Notify existing towers on this platform
            NotifyTowersOnPlatform(true);

            // Mode rule: Only switch to AAA when entering IF currently in Celestial Flight (not already walking)
            var integrator = other.GetComponentInParent<AAAMovementIntegrator>();
            if (integrator != null)
            {
                if (!integrator.useAAAMovement)
                {
                    // Find the CelestialPlatform associated with this trigger/platform
                    var platform = GetComponentInParent<CelestialPlatform>();
                    if (platform == null)
                        platform = GetComponent<CelestialPlatform>();

                    if (platform != null)
                    {
                        Debug.Log($"PlatformTrigger ({gameObject.name}): Switching to AAA mode via AAAMovementIntegrator.SwitchToAAAMode() (was in Flight)");
                        integrator.SwitchToAAAMode(platform);
                    }
                    else
                    {
                        Debug.LogWarning($"PlatformTrigger ({gameObject.name}): No CelestialPlatform found on this object or its parents. Skipping AAA switch.");
                    }
                }
                else
                {
                    // Already in AAA walking mode; entering another platform should NOT change mode
                    Debug.Log($"PlatformTrigger ({gameObject.name}): Entered while already in AAA. No mode transition.");
                }
            }
            else
            {
                Debug.LogWarning($"PlatformTrigger ({gameObject.name}): Player has no AAAMovementIntegrator component. Cannot enforce AAA switch on enter.");
            }
            
            // Play activation sound
            if (!_hasActivationSoundPlayedThisSession && platformActivateSound != null && _localAudioSource != null)
            {
                if (_globallyActiveActivationSoundSource != null && _globallyActiveActivationSoundSource.isPlaying)
                    _globallyActiveActivationSoundSource.Stop();
                    
                GameSounds.PlayPlatformActivate(transform.position, platformActivateSoundVolume);
                _globallyActiveActivationSoundSource = _localAudioSource;
                _globallyActiveActivationClip = platformActivateSound;
                _hasActivationSoundPlayedThisSession = true;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && _isPlayerCurrentlyOnPlatform)
        {
            Debug.Log($"PlatformTrigger ({gameObject.name}): PLAYER LEFT PLATFORM!");
            _isPlayerCurrentlyOnPlatform = false;
            TriggerLightAnimation(false);
            
            // Notify the associated TowerSpawner that player left this platform
            if (associatedTowerSpawner != null)
            {
                associatedTowerSpawner.OnPlayerLeftPlatform();
                Debug.Log($"PlatformTrigger ({gameObject.name}): Notified TowerSpawner - no more towers will spawn");
            }
            
            // Notify existing towers on this platform
            NotifyTowersOnPlatform(false);

            // Mode rule: Do NOT auto-enable flight when leaving. Stay in walking mode if exiting via jump.
            Debug.Log($"PlatformTrigger ({gameObject.name}): Player exited. Mode unchanged (no auto-switch to flight).");
            
            // Stop platform activation sound if playing
            if (_localAudioSource != null && _globallyActiveActivationSoundSource == _localAudioSource && 
                _localAudioSource.isPlaying && _localAudioSource.clip == _globallyActiveActivationClip)
            {
                _localAudioSource.Stop();
                _globallyActiveActivationSoundSource = null;
                _globallyActiveActivationClip = null;
            }
        }
    }

    void OnDisable()
    {
        if (_localAudioSource != null && _globallyActiveActivationSoundSource == _localAudioSource && 
            _localAudioSource.isPlaying && _localAudioSource.clip == _globallyActiveActivationClip)
        {
            _localAudioSource.Stop();
            _globallyActiveActivationSoundSource = null;
            _globallyActiveActivationClip = null;
        }
        
        if (activationLight != null && _lightAnimationCoroutine != null)
        {
            StopCoroutine(_lightAnimationCoroutine);
            _lightAnimationCoroutine = null;
            activationLight.color = lightSafeColor;
        }
    }

    void TriggerLightAnimation(bool playerEntering)
    {
        if (activationLight == null) return;
        
        if (_lightAnimationCoroutine != null)
            StopCoroutine(_lightAnimationCoroutine);
            
        _lightAnimationCoroutine = StartCoroutine(LightAnimationRoutine(playerEntering));
    }

    IEnumerator LightAnimationRoutine(bool playerEntering)
    {
        if (activationLight == null) yield break;
        
        float singlePulseDuration = Mathf.Max(0.02f, lightPulseSpeed);
        
        if (playerEntering)
        {
            // Pulse animation when player enters
            for (int i = 0; i < lightPulseCount; i++)
            {
                // Fade up to pulse color
                float timer = 0f;
                Color pulseStartColor = activationLight.color;
                while (timer < singlePulseDuration / 2f)
                {
                    activationLight.color = Color.Lerp(pulseStartColor, lightPulseColor, timer / (singlePulseDuration / 2f));
                    timer += Time.deltaTime;
                    yield return new WaitForEndOfFrame();
                }
                
                activationLight.color = lightPulseColor;
                
                // Fade down to target color
                timer = 0f;
                Color pulseDownTarget = (i == lightPulseCount - 1) ? lightActiveColor : 
                    Color.Lerp(lightActiveColor, lightPulseColor, 0.3f);
                pulseStartColor = activationLight.color;
                
                while (timer < singlePulseDuration / 2f)
                {
                    activationLight.color = Color.Lerp(pulseStartColor, pulseDownTarget, timer / (singlePulseDuration / 2f));
                    timer += Time.deltaTime;
                    yield return new WaitForEndOfFrame();
                }
                
                activationLight.color = pulseDownTarget;
            }
            
            // Final settle to active color
            float finalSettleTimer = 0f;
            float maxSettleDuration = 0.25f;
            Color finalSettleStartColor = activationLight.color;
            
            while (finalSettleTimer < maxSettleDuration && activationLight.color != lightActiveColor)
            {
                activationLight.color = Color.Lerp(finalSettleStartColor, lightActiveColor, 
                    finalSettleTimer / maxSettleDuration);
                finalSettleTimer += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
            
            activationLight.color = lightActiveColor;
        }
        else
        {
            // Fade out animation when player exits
            float timer = 0f;
            float exitFadeDuration = lightSettleSpeed > 0.01f ? 1f / lightSettleSpeed : 0.5f;
            Color currentExitColor = activationLight.color;
            
            while (timer < exitFadeDuration)
            {
                activationLight.color = Color.Lerp(currentExitColor, lightSafeColor, timer / exitFadeDuration);
                timer += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
            
            activationLight.color = lightSafeColor;
        }
        
        _lightAnimationCoroutine = null;
    }

    public bool IsPlayerOnThisPlatform() 
    { 
        return _isPlayerCurrentlyOnPlatform; 
    }
    
    /// <summary>
    /// Notifies towers that are associated with this platform about player presence changes
    /// </summary>
    private void NotifyTowersOnPlatform(bool playerEntered)
    {
        // Find towers that belong to this platform's TowerSpawner
        if (associatedTowerSpawner == null) return;
        
        // Get towers from the associated TowerSpawner's active towers list
        TowerController[] allTowers = FindObjectsByType<TowerController>(FindObjectsSortMode.None);
        int towersNotified = 0;
        
        foreach (TowerController tower in allTowers)
        {
            if (tower != null && !tower.IsDead)
            {
                // Check if tower is within reasonable distance of this platform
                float distance = Vector3.Distance(transform.position, tower.transform.position);
                if (distance < 25f) // Reasonable range for platform association
                {
                    if (playerEntered)
                    {
                        tower.SetAssociatedPlatformTrigger(this);
                        // Note: TowerController now handles player detection internally
                        Debug.Log($"PlatformTrigger ({gameObject.name}): ENABLED skull attacks for tower '{tower.name}' - distance: {distance:F1}m");
                    }
                    else
                    {
                        // Note: TowerController now handles player detection internally
                        Debug.Log($"PlatformTrigger ({gameObject.name}): DISABLED skull attacks for tower '{tower.name}' - distance: {distance:F1}m");
                    }
                    towersNotified++;
                }
            }
        }
        
        Debug.Log($"PlatformTrigger ({gameObject.name}): Notified {towersNotified} existing towers about player {(playerEntered ? "entering" : "leaving")}");
    }
}