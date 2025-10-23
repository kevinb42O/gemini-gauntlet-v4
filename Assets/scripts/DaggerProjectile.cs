// --- DaggerProjectile.cs (MODIFIED to use Base Color instead of Emission) ---
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using GeminiGauntlet.Audio;

public class DaggerProjectile : MonoBehaviour, IPooledObject
{
    [Header("Core Settings")]
    public float lifetime = 3f;
    public LayerMask hitDetectionLayers;

    [Header("Visuals - Child Meshes")]
    [Tooltip("Assign the parent GameObject containing all switchable dagger visuals as children. Each child should have one mesh/renderer.")]
    public GameObject visualsContainer;
    private MeshRenderer[] _availableRenderers;
    private GameObject[] _visualGameObjects;
    private MeshRenderer _activeRenderer;

    [Header("Sounds")]
    public AudioClip[] deflectionSounds;
    [Range(0f, 1f)] public float deflectionVolume = 0.7f;
    public AudioClip destroySound;
    [Range(0f, 1f)] public float destroyVolume = 0.6f;

    [Header("Homing Mode (Overrides normal flight if active)")]
    public bool isHoming = false;
    public Transform homingTarget;
    public float homingTurnSpeed = 10f;
    public float homingInitialStraightFlightDuration = 0.2f;
    private float _homingStraightFlightTimer = 0f;
    private bool _homingEngaged = false;

    private MaterialPropertyBlock _propBlock;
    // MODIFIED: Changed from _EmissionColor to _Color. This is the standard property for base color in many shaders.
    // If you use a custom shader (like URP Lit), this might need to be "_BaseColor".
    private static readonly int _baseColorID = Shader.PropertyToID("_Color");

    private float _damage;
    private float _propulsionForce; // Used as speed for both normal and homing
    private string _poolTag;
    private Rigidbody _rb;
    private Collider _collider;
    private bool _isLaunched = false;
    private float _despawnTimer;
    private Vector3 _inheritedVelocity = Vector3.zero;

    private static float _lastDeflectionSoundTime = -1f;
    private static float _deflectionSoundCooldown = 0.1f;
    private static int _maxConcurrentDeflections = 3;
    private static int _currentConcurrentDeflections = 0;

    private bool _hasRunFirstFrameOrIsEditor = false;
    private Coroutine _initializationCoroutineInternal;


    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        _propBlock = new MaterialPropertyBlock();

        if (_rb == null) Debug.LogError($"DaggerProjectile ({name}): Rigidbody is missing!", this);
        else _rb.isKinematic = false;

        if (_collider == null) Debug.LogError($"DaggerProjectile ({name}): Collider is missing!", this);
        if (hitDetectionLayers == 0 && !isHoming) Debug.LogWarning($"DaggerProjectile ({name}): 'Hit Detection Layers' is not set for non-homing dagger. This might be intended if only homing.", this);

        if (visualsContainer == null)
        {
            Debug.LogError($"DaggerProjectile ({name}): 'Visuals Container' GameObject is not assigned! Dagger will be invisible.", this);
            enabled = false; return;
        }

        int childCount = visualsContainer.transform.childCount;
        
        // Handle the case where the Visuals Container itself has the mesh renderer
        // and doesn't have child objects
        if (childCount == 0)
        {
            // Look for renderer on the container itself
            MeshRenderer containerRenderer = visualsContainer.GetComponent<MeshRenderer>();
            if (containerRenderer != null)
            {
                // Use the container itself as the visual
                _availableRenderers = new MeshRenderer[1] { containerRenderer };
                _visualGameObjects = new GameObject[1] { visualsContainer };
                return; // Successfully initialized
            }
            else
            {
                Debug.LogError($"DaggerProjectile ({name}): 'Visuals Container' has no children and no renderer! No dagger visuals available.", this);
                enabled = false; return;
            }
        }

        _availableRenderers = new MeshRenderer[childCount];
        _visualGameObjects = new GameObject[childCount];
        for (int i = 0; i < childCount; i++)
        {
            Transform childVisual = visualsContainer.transform.GetChild(i);
            _visualGameObjects[i] = childVisual.gameObject;
            _availableRenderers[i] = childVisual.GetComponent<MeshRenderer>();
            if (_availableRenderers[i] == null)
            {
                Debug.LogError($"DaggerProjectile ({name}): Child visual '{childVisual.name}' at index {i} is missing a MeshRenderer component.", this);
            }
            if (_visualGameObjects[i] != null) _visualGameObjects[i].SetActive(false);
        }
        if (!Application.isPlaying) _hasRunFirstFrameOrIsEditor = true;
    }

    IEnumerator SetFirstFrameFlagCoroutine()
    {
        yield return new WaitForEndOfFrame();
        _hasRunFirstFrameOrIsEditor = true;
        _initializationCoroutineInternal = null;
    }

    // MODIFIED: Signature changed to accept 'baseColor' instead of emissive properties
    public void InitializeForUse(Vector3 worldPosition, Quaternion worldRotation, Vector3 launchDirection,
                                 float force, string tag, float damage, Vector3 inheritedVel,
                                 Color baseColor, int visualIndex)
    {
        InitializeInternal(worldPosition, worldRotation, launchDirection, force, tag, damage, inheritedVel, baseColor, visualIndex, null, 0f, 0f, false);
    }

    // MODIFIED: Signature changed to accept 'baseColor'
    public void InitializeForHoming(Vector3 worldPosition, Quaternion worldRotation, Vector3 launchDirection,
                                    float force, string tag, float damage, Vector3 inheritedVel,
                                    Color baseColor, int visualIndex,
                                    Transform target, float turnSpeed, float straightFlightDuration)
    {
        InitializeInternal(worldPosition, worldRotation, launchDirection, force, tag, damage, inheritedVel, baseColor, visualIndex, target, turnSpeed, straightFlightDuration, true);
    }

    // MODIFIED: Signature changed to accept 'baseColor'
    private void InitializeInternal(Vector3 worldPosition, Quaternion worldRotation, Vector3 initialLaunchDirection,
                                 float force, string tag, float damage, Vector3 inheritedVel,
                                 Color baseColor, int visualIndex,
                                 Transform targetForHoming, float turnSpeedForHoming, float straightFlightDurationForHoming,
                                 bool isHomingInitialization)
    {
        transform.SetPositionAndRotation(worldPosition, worldRotation);

        _propulsionForce = force;
        _poolTag = tag;
        _damage = damage;
        _despawnTimer = lifetime;
        _isLaunched = false;
        _inheritedVelocity = inheritedVel;
        this.isHoming = isHomingInitialization;

        if (isHomingInitialization && targetForHoming != null)
        {
            this.homingTarget = targetForHoming;
            this.homingTurnSpeed = turnSpeedForHoming;
            this.homingInitialStraightFlightDuration = straightFlightDurationForHoming;
            this._homingStraightFlightTimer = 0f;
            this._homingEngaged = (this.homingInitialStraightFlightDuration <= 0f);
        }
        else if (isHomingInitialization && targetForHoming == null)
        {
            Debug.LogError($"DaggerProjectile ({name}) InitializeInternal: Homing initialization called BUT targetForHoming is NULL. This dagger will likely not home correctly and might self-destruct or fly straight.", this);
            this.isHoming = true;
            this.homingTarget = null;
            this._homingEngaged = false;
        }
        else
        {
            this.isHoming = false;
            this.homingTarget = null;
            this._homingEngaged = false;
        }

        gameObject.SetActive(true);
        if (_collider) _collider.enabled = true;

        if (_rb)
        {
            _rb.isKinematic = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
        else { Debug.LogError($"DaggerProjectile ({name}) InitializeInternal: Rigidbody is NULL!", this); }

        _activeRenderer = null;
        if (visualsContainer != null && _visualGameObjects != null)
        {
            for (int i = 0; i < _visualGameObjects.Length; i++)
            {
                if (_visualGameObjects[i] != null) _visualGameObjects[i].SetActive(false);
            }
            int actualVisualIndex = 0;
            if (visualIndex >= 0 && _availableRenderers != null && _availableRenderers.Length > 0)
            {
                actualVisualIndex = visualIndex % _availableRenderers.Length;
            }

            if (actualVisualIndex < _visualGameObjects.Length && _visualGameObjects[actualVisualIndex] != null)
            {
                _visualGameObjects[actualVisualIndex].SetActive(true);
                _activeRenderer = _availableRenderers[actualVisualIndex];
            }
            else if (_visualGameObjects.Length > 0 && _visualGameObjects[0] != null)
            {
                _visualGameObjects[0].SetActive(true);
                _activeRenderer = _availableRenderers[0];
            }
        }

        if (_activeRenderer != null)
        {
            // MODIFIED: Call the new color application methods
            if (_hasRunFirstFrameOrIsEditor) ApplyBaseColorToRenderer(_activeRenderer, baseColor);
            else if (Application.isPlaying && _initializationCoroutineInternal == null && !_hasRunFirstFrameOrIsEditor)
                _initializationCoroutineInternal = StartCoroutine(ApplyBaseColorAfterDelay(_activeRenderer, baseColor));
        }
    }

    // MODIFIED: Renamed from ApplyEmissionAfterDelay and signature changed
    IEnumerator ApplyBaseColorAfterDelay(MeshRenderer targetRenderer, Color color)
    {
        yield return new WaitUntil(() => _hasRunFirstFrameOrIsEditor);
        ApplyBaseColorToRenderer(targetRenderer, color);
    }

    // MODIFIED: Renamed from ApplyEmissionToRenderer and logic updated
    private void ApplyBaseColorToRenderer(MeshRenderer targetRenderer, Color color)
    {
        if (targetRenderer == null) return;
        targetRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(_baseColorID, color);
        targetRenderer.SetPropertyBlock(_propBlock);
    }

    // MODIFIED: Renamed from ResetEmissionOnRenderer and logic updated
    private void ResetBaseColorOnRenderer(MeshRenderer targetRenderer)
    {
        if (targetRenderer == null) return;
        targetRenderer.GetPropertyBlock(_propBlock);
        // Resetting to white is standard for base color, so it can be tinted correctly again later.
        _propBlock.SetColor(_baseColorID, Color.white);
        targetRenderer.SetPropertyBlock(_propBlock);
    }

    // ... OnEnable, FixedUpdate, Update, OnCollisionEnter are largely the same ...
    // ... They don't deal with the color directly, only the initialization does. ...

    // ... Methods below are updated to use the new color logic where needed ...

    private void ReturnToPool(bool playGenericDestroySound)
    {
        if (!gameObject.activeSelf) return;

        if (playGenericDestroySound && destroySound != null)
        {
            PlaySoundAtPoint(destroySound, transform.position, destroyVolume);
        }

        _isLaunched = false;
        isHoming = false;
        homingTarget = null;
        _homingEngaged = false;
        _homingStraightFlightTimer = 0f;

        if (_activeRenderer != null)
        {
            // MODIFIED: Call new reset method
            ResetBaseColorOnRenderer(_activeRenderer);
            if (_activeRenderer.gameObject.activeSelf) _activeRenderer.gameObject.SetActive(false);
            _activeRenderer = null;
        }

        if (!string.IsNullOrEmpty(_poolTag) && ObjectPooler.Instance != null)
        {
            ObjectPooler.Instance.ReturnToPool(_poolTag, gameObject);
        }
        else
        {
            float delay = 0.01f;
            if (playGenericDestroySound && destroySound != null) delay = Mathf.Max(delay, destroySound.length + 0.1f);
            if (gameObject.activeInHierarchy) gameObject.SetActive(false);
            Destroy(gameObject, delay);
        }
    }

    public void OnObjectReturnedToPool()
    {
        if (_initializationCoroutineInternal != null)
        {
            StopCoroutine(_initializationCoroutineInternal);
            _initializationCoroutineInternal = null;
        }

        _hasRunFirstFrameOrIsEditor = !Application.isPlaying;
        _isLaunched = false;

        isHoming = false;
        homingTarget = null;
        _homingStraightFlightTimer = 0f;
        _homingEngaged = false;

        if (_rb)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
        }
        if (_collider) _collider.enabled = false;

        if (_activeRenderer != null)
        {
            // MODIFIED: Call new reset method
            ResetBaseColorOnRenderer(_activeRenderer);
            if (_activeRenderer.gameObject.activeSelf) _activeRenderer.gameObject.SetActive(false);
            _activeRenderer = null;
        }
        else if (visualsContainer != null && _visualGameObjects != null)
        {
            for (int i = 0; i < _visualGameObjects.Length; ++i)
            {
                if (_visualGameObjects[i] != null && _visualGameObjects[i].activeSelf)
                    _visualGameObjects[i].gameObject.SetActive(false);
            }
        }
    }

    // --- The rest of DaggerProjectile.cs (unchanged methods) ---
    // FixedUpdate, OnCollisionEnter, Update, PlayDeflectionSound, PlaySoundAtPoint, SetHitboxProperties etc...
    // These methods don't need changes as they don't handle color.
    // I am including the full script for completeness with the unchanged parts.
    void OnEnable()
    {
        _despawnTimer = lifetime;
        _isLaunched = false;
        isHoming = false;
        homingTarget = null;
        _homingStraightFlightTimer = 0f;
        _homingEngaged = false;
        if (_rb)
        {
            _rb.isKinematic = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
        else
        {
            Debug.LogError($"DaggerProjectile ({name}) OnEnable: Rigidbody is NULL!");
        }
        if (_collider) _collider.enabled = true;


        if (Application.isPlaying && !_hasRunFirstFrameOrIsEditor && _initializationCoroutineInternal == null)
        {
            _initializationCoroutineInternal = StartCoroutine(SetFirstFrameFlagCoroutine());
        }

        if (visualsContainer != null && _visualGameObjects != null)
        {
            for (int i = 0; i < _visualGameObjects.Length; ++i)
            {
                if (_visualGameObjects[i] != null)
                    _visualGameObjects[i].gameObject.SetActive(false);
            }
        }
        _activeRenderer = null;
    }

    void FixedUpdate()
    {
        if (_rb == null)
        {
            return;
        }
        if (_rb.isKinematic && gameObject.activeInHierarchy)
        {
            _rb.isKinematic = false;
        }


        if (!_isLaunched)
        {
            _rb.linearVelocity = _inheritedVelocity + (transform.forward * _propulsionForce);
            _isLaunched = true;
            if (isHoming && homingTarget == null)
            {
                Debug.LogWarning($"DaggerProjectile ({name}) LAUNCHED IN HOMING MODE BUT homingTarget IS NULL. Will fly straight or behave unpredictably. Returning to pool early to prevent issues.", this);
                ReturnToPool(false);
                return;
            }
            return;
        }

        if (isHoming)
        {
            if (!_homingEngaged)
            {
                _homingStraightFlightTimer += Time.fixedDeltaTime;
                if (_homingStraightFlightTimer >= homingInitialStraightFlightDuration)
                {
                    _homingEngaged = true;
                    if (homingTarget == null || !homingTarget.gameObject.activeInHierarchy)
                    {
                        Debug.LogWarning($"DaggerProjectile ({name}) Homing engaged but target {(homingTarget == null ? "is NULL" : "is INACTIVE ('" + homingTarget.name + "')")}. Reverting to straight flight and will expire.", this);
                        isHoming = false;
                        _homingEngaged = false;
                    }
                }
            }

            if (_homingEngaged && homingTarget != null && homingTarget.gameObject.activeInHierarchy)
            {
                Vector3 currentPos = _rb.position;
                Vector3 targetPos = homingTarget.position;
                Vector3 directionToTarget = (targetPos - currentPos).normalized;

                if (directionToTarget != Vector3.zero)
                {
                    Quaternion currentRotation = _rb.rotation;
                    Quaternion targetLookRotation = Quaternion.LookRotation(directionToTarget);
                    Quaternion newRotation = Quaternion.Slerp(currentRotation, targetLookRotation, homingTurnSpeed * Time.fixedDeltaTime);
                    _rb.MoveRotation(newRotation);
                }
                _rb.linearVelocity = transform.forward * _propulsionForce;
            }
            else if (_homingEngaged && (homingTarget == null || !homingTarget.gameObject.activeInHierarchy))
            {
                isHoming = false;
                _homingEngaged = false;
            }
        }
    }

    void Update()
    {
        if (!_isLaunched) return;
        _despawnTimer -= Time.deltaTime;
        if (_despawnTimer <= 0f)
        {
            ReturnToPool(true);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!_isLaunched || !gameObject.activeSelf)
        {
            ReturnToPool(false);
            return;
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            if (isHoming)
            {
                ReturnToPool(false);
                return;
            }
        }

        if (!isHoming && ((1 << collision.gameObject.layer) & hitDetectionLayers) == 0)
        {
            return;
        }

        ContactPoint contact = collision.GetContact(0);
        bool specificSoundPlayed = false;
        bool damageAttemptedOrDealt = false;

        SkullEnemy enemyScript;
        if (collision.gameObject.TryGetComponent<SkullEnemy>(out enemyScript))
        {
            damageAttemptedOrDealt = true;
            if (!enemyScript.IsDead())
            {
                enemyScript.TakeDamage(_damage, contact.point);
                specificSoundPlayed = true;
            }
            else
            {
                PlayDeflectionSound(contact.point);
                specificSoundPlayed = true;
            }
        }
        else if (collision.gameObject.TryGetComponent<BossGem>(out BossGem bossGemScript))
        {
            damageAttemptedOrDealt = true;
            if (!bossGemScript.IsDetached())
            {
                bossGemScript.TakeDamage(_damage);
                specificSoundPlayed = true;
            }
            else
            {
                PlayDeflectionSound(contact.point);
                specificSoundPlayed = true;
            }
        }
        else if (collision.gameObject.TryGetComponent<Gem>(out Gem gemScript) && !(gemScript is BossGem))
        {
            damageAttemptedOrDealt = true;
            if (!gemScript.IsDetached())
            {
                gemScript.TakeDamage(_damage);
                specificSoundPlayed = true;
            }
            else
            {
                PlayDeflectionSound(contact.point);
                specificSoundPlayed = true;
            }
        }
        else
        {
            PlayDeflectionSound(contact.point);
            specificSoundPlayed = true;
        }

        bool playGenericDestroy = !specificSoundPlayed && destroySound != null && !damageAttemptedOrDealt;
        ReturnToPool(playGenericDestroy);
    }

    private void PlayDeflectionSound(Vector3 position)
    {
        if (deflectionSounds == null || deflectionSounds.Length == 0) return;
        if (Time.time >= _lastDeflectionSoundTime + _deflectionSoundCooldown && _currentConcurrentDeflections < _maxConcurrentDeflections)
        {
            _lastDeflectionSoundTime = Time.time;
            AudioClip clip = deflectionSounds[UnityEngine.Random.Range(0, deflectionSounds.Length)];
            // Use new AAA sound system with proper spatial audio and priority limits
            float pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            SoundSystemCore.Instance?.PlaySound3D(clip, position, SoundCategory.SFX, deflectionVolume, pitch);
            _currentConcurrentDeflections = Mathf.Clamp(_currentConcurrentDeflections + 1, 0, _maxConcurrentDeflections);
        }
    }

    public void SetHitboxProperties(float scaleMultiplier) { /* Placeholder */ }

    private void PlaySoundAtPoint(AudioClip clip, Vector3 position, float volume, float pitchVar = 0.05f, Action onComplete = null)
    {
        if (clip == null || SoundSystemCore.Instance == null) 
        { 
            onComplete?.Invoke(); 
            return; 
        }
        
        // Use new AAA sound system - proper 3D spatial audio for combat sounds
        float pitch = UnityEngine.Random.Range(1f - pitchVar, 1f + pitchVar);
        SoundSystemCore.Instance.PlaySound3D(clip, position, SoundCategory.SFX, volume, pitch);
        
        // Call completion callback immediately since new system handles cleanup
        onComplete?.Invoke();
    }

    private IEnumerator DestroySoundObjectAfterDelay(GameObject soundObject, float delay, Action onComplete)
    {
        float waited = 0f;
        while (waited < delay)
        {
            if (Time.timeScale > 0.001f) yield return new WaitForSeconds(delay - waited);
            else yield return null;
            waited = delay;
        }
        if (soundObject != null) Destroy(soundObject);
        onComplete?.Invoke();
    }

    public void OnObjectSpawnedFromPool(bool isActiveAndReady)
    {
        if (isActiveAndReady)
        {
            _isLaunched = false;
            _despawnTimer = lifetime;

            isHoming = false;
            homingTarget = null;
            _homingStraightFlightTimer = 0f;
            _homingEngaged = false;

            if (_rb)
            {
                _rb.isKinematic = false;
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
            else { Debug.LogError($"DaggerProjectile ({name}) OnObjectSpawnedFromPool: Rigidbody is NULL!"); }

            if (_collider) _collider.enabled = true;
            else { Debug.LogWarning($"DaggerProjectile ({name}) OnObjectSpawnedFromPool: Collider is NULL!"); }

            _hasRunFirstFrameOrIsEditor = !Application.isPlaying;
            if (Application.isPlaying && _initializationCoroutineInternal == null && !_hasRunFirstFrameOrIsEditor)
            {
                _initializationCoroutineInternal = StartCoroutine(SetFirstFrameFlagCoroutine());
            }
        }
    }
}