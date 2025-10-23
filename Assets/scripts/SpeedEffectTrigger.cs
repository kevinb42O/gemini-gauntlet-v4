using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Place this on your Player. When speed exceeds a threshold, an effect is shown.
/// Works with both CharacterController (grounded) and Rigidbody (flight) without extra wiring.
///
/// Usage:
/// - Assign either an already-present effect GameObject (effectObject) to toggle OR an effect prefab (effectPrefab) to auto-instantiate under attachPoint.
/// - Set On Speed (turn-on threshold) and Off Speed (turn-off threshold) to add hysteresis and avoid flicker.
/// - Leave Speed Source = Auto to work in both modes. It will prefer Rigidbody.velocity when available, then CharacterController.velocity, otherwise use transform delta.
/// - Optional: Enable "Scale Emission With Speed" to drive a ParticleSystem's emission rate based on current speed.
/// </summary>
[DisallowMultipleComponent]
public class SpeedEffectTrigger : MonoBehaviour
{
    public enum SpeedSource
    {
        Auto,
        Rigidbody,
        CharacterController,
        TransformDelta
    }

    [Header("Speed Source (Auto works for both modes)")]
    [SerializeField] private SpeedSource speedSource = SpeedSource.Auto;
    [SerializeField] private Rigidbody rb; // (Flight)
    [SerializeField] private CharacterController cc; // (Grounded)

    [Header("Thresholds (units/sec)")]
    [Tooltip("Speed at or above this turns the effect ON")]
    public float onSpeed = 165f; // Triggers at ~90% of sprint speed (183.75)

    [Tooltip("Speed at or below this turns the effect OFF (hysteresis)")]
    public float offSpeed = 145f; // Turns off when slowing down below sprint

    [Tooltip("Seconds to smooth measured speed. 0 = no smoothing")]
    [Range(0f, 0.5f)] public float smoothingSeconds = 0.15f; // Slightly smoother transitions

    [Tooltip("Ignore transform-delta spikes larger than this (teleports)")]
    public float maxDeltaPositionForTransformSpeed = 50f;

    [Header("Effect Target")]
    [Tooltip("Existing effect object to toggle active/inactive (e.g., speed lines VFX root)")]
    public GameObject effectObject;

    [Tooltip("Optional effect prefab to auto-instantiate if effectObject is not assigned")]
    public GameObject effectPrefab;

    [Tooltip("Where to attach instantiated prefab (defaults to Camera.main if available, otherwise this.transform)")]
    public Transform attachPoint;

    [Tooltip("Optional particle system whose emission will be driven by speed")]
    public ParticleSystem effectParticles;

    [Header("Auto-Attach")]
    [Tooltip("If true and attachPoint is empty (or left as Player transform), auto-parent effect to Camera.main or first scene camera.")]
    public bool autoAttachToCamera = true;

    [Header("Emission Scaling (optional)")]
    public bool scaleEmissionWithSpeed = true;
    [Tooltip("Emission rate (particles/sec) when speed == On Speed")]
    public float emissionAtOnSpeed = 50f;
    [Tooltip("Emission rate (particles/sec) at Max Speed For Scaling")]
    public float emissionAtMaxSpeed = 200f;
    [Tooltip("Speed value considered 'max' for emission scaling")]
    public float maxSpeedForScaling = 250f; // Updated to match sprint speeds

    [Header("Audio Fade (optional)")]
    [Tooltip("Enable smooth audio volume fade in/out")]
    public bool enableAudioFade = true;
    [Tooltip("AudioSource to fade (auto-finds if not assigned)")]
    public AudioSource audioSource;
    [Tooltip("How fast to fade in audio (seconds)")]
    [Range(0.05f, 2f)] public float audioFadeInTime = 0.2f; // Fast but smooth fade-in
    [Tooltip("How fast to fade out audio (seconds)")]
    [Range(0.05f, 2f)] public float audioFadeOutTime = 0.5f; // Slower, more natural fade-out
    [Tooltip("Maximum volume when at full speed")]
    [Range(0f, 1f)] public float maxAudioVolume = 0.8f;
    [Tooltip("Scale volume with speed (not just on/off)")]
    public bool scaleVolumeWithSpeed = true;
    [Tooltip("Speed at which volume reaches maximum")]
    public float maxVolumeSpeed = 200f; // Full volume at ~sprint speed

    [Header("Debug")]
    public bool debugLog;
    [ReadOnlyInspector] public float currentSpeed; // smoothed
    [ReadOnlyInspector] public bool isEffectActive;
    [ReadOnlyInspector] public float currentAudioVolume;

    private Vector3 _lastPos;
    private bool _hasLastPos;
    private float _rawSpeed;
    private float _targetAudioVolume;
    private float _audioVelocity; // For SmoothDamp

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
        cc = GetComponent<CharacterController>();
        attachPoint = transform;
        if (offSpeed >= onSpeed) offSpeed = Mathf.Max(0f, onSpeed - 1f);
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (cc == null) cc = GetComponent<CharacterController>();

        // Resolve attach point: prefer camera if requested or if default is still Player transform
        if ((attachPoint == null || attachPoint == transform) && autoAttachToCamera)
        {
            Transform camT = null;
            if (Camera.main != null)
            {
                camT = Camera.main.transform;
            }
            else
            {
                var anyCam = FindObjectOfType<Camera>();
                if (anyCam != null) camT = anyCam.transform;
            }
            if (camT != null) attachPoint = camT;
        }
        if (attachPoint == null) attachPoint = transform;

        // Instantiate prefab if needed
        if (effectObject == null && effectPrefab != null)
        {
            effectObject = Instantiate(effectPrefab, attachPoint, false);
        }

        // Find a particle system if not assigned
        if (effectParticles == null && effectObject != null)
        {
            effectParticles = effectObject.GetComponentInChildren<ParticleSystem>(true);
        }

        // Find audio source if not assigned
        if (audioSource == null && effectObject != null)
        {
            audioSource = effectObject.GetComponentInChildren<AudioSource>(true);
        }
        if (audioSource == null)
        {
            audioSource = GetComponentInChildren<AudioSource>(true);
        }

        // Initialize audio volume
        if (audioSource != null)
        {
            currentAudioVolume = 0f;
            audioSource.volume = 0f;
        }

        // Start hidden until we measure speed
        SetEffectActive(false, true);
    }

    private void OnValidate()
    {
        if (offSpeed > onSpeed)
            offSpeed = onSpeed;
        if (maxSpeedForScaling < onSpeed)
            maxSpeedForScaling = onSpeed;
    }

    private void Update()
    {
        float measured = MeasureSpeed();
        _rawSpeed = measured;

        // Exponential smoothing
        if (smoothingSeconds > 0f)
        {
            float a = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, smoothingSeconds));
            currentSpeed = Mathf.Lerp(currentSpeed, measured, a);
        }
        else
        {
            currentSpeed = measured;
        }

        // Hysteresis-based toggle
        if (!isEffectActive && currentSpeed >= onSpeed)
        {
            SetEffectActive(true);
            if (debugLog) Debug.Log($"[SpeedEffectTrigger] ON at {currentSpeed:F1} u/s", this);
        }
        else if (isEffectActive && currentSpeed <= offSpeed)
        {
            SetEffectActive(false);
            if (debugLog) Debug.Log($"[SpeedEffectTrigger] OFF at {currentSpeed:F1} u/s", this);
        }

        // Continuous intensity update
        if (scaleEmissionWithSpeed && effectParticles != null)
        {
            float t = 0f;
            if (maxSpeedForScaling > Mathf.Epsilon)
                t = Mathf.InverseLerp(onSpeed, maxSpeedForScaling, currentSpeed);
            float rate = Mathf.Lerp(emissionAtOnSpeed, emissionAtMaxSpeed, Mathf.Clamp01(t));

            var emission = effectParticles.emission; // struct wrapper
            emission.rateOverTime = rate;
        }

        // Smooth audio volume fade - SPEED RESPONSIVE!
        if (enableAudioFade && audioSource != null)
        {
            // Calculate target volume based on ACTUAL SPEED (not just on/off state)
            if (scaleVolumeWithSpeed && isEffectActive)
            {
                // Volume scales smoothly with speed between onSpeed and maxVolumeSpeed
                float speedRange = maxVolumeSpeed - onSpeed;
                float speedAboveThreshold = Mathf.Max(0f, currentSpeed - onSpeed);
                float volumePercent = Mathf.Clamp01(speedAboveThreshold / speedRange);
                
                // Use a curve for more natural feel (starts gentle, ramps up)
                volumePercent = Mathf.Pow(volumePercent, 0.7f); // Slight curve for smoother perception
                
                _targetAudioVolume = volumePercent * maxAudioVolume;
            }
            else
            {
                // Simple on/off mode (legacy behavior)
                _targetAudioVolume = isEffectActive ? maxAudioVolume : 0f;
            }

            // Choose fade speed based on direction
            float fadeTime = (_targetAudioVolume > currentAudioVolume) ? audioFadeInTime : audioFadeOutTime;

            // Smooth fade using SmoothDamp for natural acceleration/deceleration
            currentAudioVolume = Mathf.SmoothDamp(currentAudioVolume, _targetAudioVolume, ref _audioVelocity, fadeTime);

            // Apply to audio source
            audioSource.volume = currentAudioVolume;

            // Ensure audio is playing when effect is active
            if (isEffectActive && !audioSource.isPlaying && audioSource.clip != null)
            {
                audioSource.Play();
            }
            else if (!isEffectActive && audioSource.isPlaying && currentAudioVolume < 0.01f)
            {
                audioSource.Stop();
            }
        }
    }

    private float MeasureSpeed()
    {
        // Choose best available source
        if (speedSource == SpeedSource.Auto)
        {
            float rbSpeed = (rb != null) ? rb.linearVelocity.magnitude : 0f;
            float ccSpeed = (cc != null && cc.enabled) ? cc.velocity.magnitude : 0f;
            float best = Mathf.Max(rbSpeed, ccSpeed);
            if (best > 0f)
                return best;
            // else fall through to transform delta
        }
        else if (speedSource == SpeedSource.Rigidbody)
        {
            if (rb != null) return rb.linearVelocity.magnitude;
        }
        else if (speedSource == SpeedSource.CharacterController)
        {
            if (cc != null && cc.enabled) return cc.velocity.magnitude;
        }

        // Fallback: world-space transform delta per second
        Vector3 pos = transform.position;
        float speed = 0f;
        if (_hasLastPos)
        {
            Vector3 delta = pos - _lastPos;
            // Ignore teleports that would produce absurd speeds
            if (delta.magnitude <= maxDeltaPositionForTransformSpeed)
                speed = delta.magnitude / Mathf.Max(Time.deltaTime, 1e-6f);
        }
        _lastPos = pos;
        _hasLastPos = true;
        return speed;
    }

    private SpeedSource EffectiveSource()
    {
        if (speedSource != SpeedSource.Auto)
            return speedSource;

        // Prefer Rigidbody when present (flight)
        if (rb != null)
            return SpeedSource.Rigidbody;

        if (cc != null && cc.enabled)
            return SpeedSource.CharacterController;

        return SpeedSource.TransformDelta;
    }

    private void SetEffectActive(bool active, bool force = false)
    {
        if (!force && isEffectActive == active) return;
        isEffectActive = active;

        if (effectObject != null)
        {
            effectObject.SetActive(active);
        }

        if (effectParticles != null)
        {
            if (active)
            {
                if (!effectParticles.isPlaying) effectParticles.Play(true);
            }
            else
            {
                if (effectParticles.isPlaying) effectParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    // Expose for other systems if needed
    public float RawSpeed => _rawSpeed;
}

/// <summary>
/// Simple read-only inspector attribute for debugging values.
/// </summary>
public class ReadOnlyInspectorAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyInspectorAttribute))]
public class ReadOnlyInspectorDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
#endif
