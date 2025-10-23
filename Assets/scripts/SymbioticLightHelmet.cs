using UnityEngine;
using UnityEngine.UI;

// Architect's Refactor v3.0: Now with logically-triggered audio.
[RequireComponent(typeof(AudioSource))] // Ensures an AudioSource is always present.
public class SymbioticLightHelmet : MonoBehaviour
{
    // By grouping references, the Inspector becomes cleaner and more organized.
    [System.Serializable]
    public class UIReferences
    {
        [Tooltip("A UI Image for the low-power/damage vignette effect.")]
        public Image vignetteImage;
        [Tooltip("A UI Image for the auto-darkening visor effect.")]
        public Image visorImage;
    }
    
    // NEW: Grouping audio clips for organization.
    [System.Serializable]
    public class SoundEffects
    {
        [Tooltip("Sound for when the headlamp turns on.")]
        public AudioClip headlampOn;
        [Tooltip("Sound for when the headlamp turns off.")]
        public AudioClip headlampOff;
        [Tooltip("Sound for when the headlamp starts to flicker at low energy.")]
        public AudioClip flickerStart;
        [Tooltip("Sound for when the protective visor activates.")]
        public AudioClip visorActivate;
        [Tooltip("Sound for when the protective visor deactivates.")]
        public AudioClip visorDeactivate;
        [Tooltip("A low, persistent hum when energy is critically low. (Set to Loop in AudioSource)")]
        public AudioClip lowEnergyHum;
    }

    [Header("Core Systems")]
    [Tooltip("The Light component that acts as the headlamp.")]
    public Light headlampLight;
    [Tooltip("The UI elements this helmet controls.")]
    public UIReferences helmetUI;

    [Header("Audio")]
    public SoundEffects sounds;
    private AudioSource audioSource;

    [Header("Energy Core")]
    public float maxEnergy = 100f;
    [SerializeField] private float currentEnergy;
    public float chargeRate = 10f;
    public float darknessDrainRate = 5f;

    [Header("Light Sensor Precision")]
    [Tooltip("An empty GameObject on the player that acts as the 'eye' for sensing light.")]
    public Transform lightSensorSource;
    [Tooltip("The radius around the sensor to check for light sources.")]
    public float lightSensorRadius = 10f;
    [Tooltip("The ambient light intensity below which is considered 'dark'.")]
    public float darknessThreshold = 0.3f;
    [Tooltip("The ambient light intensity above which triggers the protective visor.")]
    public float glareThreshold = 1.5f;

    [Header("Headlamp Dynamics")]
    public float headlampDrainRate = 2f;
    [Range(0, 1)] public float flickerThreshold = 0.25f;
    public float maxHeadlampIntensity = 2.0f;
    public AnimationCurve flickerCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(0.5f, 0.7f), new Keyframe(1, 1));
    
    [Header("Debugging")]
    [Tooltip("Check this to force the vignette to be visible on start for testing its look.")]
    public bool startWithVignetteVisible = false;

    private Color vignetteBaseColor;
    private Color visorBaseColor;
    private float flickerTimer = 0f;

    // --- STATE TRACKING FOR SOUNDS ---
    // These variables remember what happened last frame to detect changes.
    private bool wasHeadlampOnLastFrame = false;
    private bool wasFlickeringLastFrame = false;
    private bool wasVisorActiveLastFrame = false;
    private bool wasLowEnergyHumming = false;

    void Awake()
    {
        // --- VALIDATION & INITIALIZATION ---
        if (headlampLight == null || helmetUI.vignetteImage == null || helmetUI.visorImage == null || lightSensorSource == null)
        {
            // Debug.LogError("PERFECTOMUNDO ERROR: One or more essential components are not assigned in the Inspector!", this);
            enabled = false;
            return;
        }

        // Get the required AudioSource component.
        audioSource = GetComponent<AudioSource>();

        vignetteBaseColor = helmetUI.vignetteImage.color;
        visorBaseColor = helmetUI.visorImage.color;
        
        if (!startWithVignetteVisible)
        {
            helmetUI.vignetteImage.color = Color.clear;
        }
        helmetUI.visorImage.color = Color.clear;

        headlampLight.enabled = false;
        currentEnergy = maxEnergy;
    }

    void Update()
    {
        float effectiveLightLevel = SenseEffectiveLightLevel();
        bool isDark = effectiveLightLevel < darknessThreshold;
        float energyPercent = currentEnergy / maxEnergy;

        HandleEnergy(isDark, energyPercent);
        HandleHeadlamp(isDark, energyPercent);
        HandleVisuals(effectiveLightLevel, energyPercent);

        // Update state trackers at the very end of the frame.
        UpdateStateTrackers();
    }

    private void HandleEnergy(bool isDark, float energyPercent)
    {
        float totalDrain = isDark ? darknessDrainRate : -chargeRate; // Negative drain = charge
        if (headlampLight.enabled) totalDrain += headlampDrainRate;
        
        currentEnergy -= totalDrain * Time.deltaTime;
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);

        // --- SOUND LOGIC FOR LOW ENERGY HUM ---
        bool isLowEnergy = energyPercent < flickerThreshold && currentEnergy > 0;
        if (isLowEnergy && !wasLowEnergyHumming)
        {
            // Start the looping hum
            audioSource.clip = sounds.lowEnergyHum;
            audioSource.loop = true;
            audioSource.Play();
            wasLowEnergyHumming = true;
        }
        else if (!isLowEnergy && wasLowEnergyHumming)
        {
            // Stop the looping hum
            if (audioSource.clip == sounds.lowEnergyHum)
            {
                audioSource.Stop();
                audioSource.loop = false;
            }
            wasLowEnergyHumming = false;
        }
    }

    private void HandleHeadlamp(bool isDark, float energyPercent)
    {
        bool shouldBeOn = isDark && currentEnergy > 0;
        headlampLight.enabled = shouldBeOn;

        // --- SOUND LOGIC FOR HEADLAMP ON/OFF ---
        if (shouldBeOn && !wasHeadlampOnLastFrame)
        {
            PlaySound(sounds.headlampOn); // Play ON sound
        }
        else if (!shouldBeOn && wasHeadlampOnLastFrame)
        {
            PlaySound(sounds.headlampOff); // Play OFF sound
        }
        
        if (!headlampLight.enabled) return;

        headlampLight.intensity = maxHeadlampIntensity * energyPercent;

        bool isFlickering = energyPercent < flickerThreshold;
        if (isFlickering)
        {
            flickerTimer += Time.deltaTime * (5f + (1 - energyPercent) * 10f);
            headlampLight.intensity *= flickerCurve.Evaluate(flickerTimer % 1f);

            // --- SOUND LOGIC FOR FLICKER START ---
            if (!wasFlickeringLastFrame)
            {
                PlaySound(sounds.flickerStart);
            }
        }
    }

    private void HandleVisuals(float lightLevel, float energyPercent)
    {
        // Vignette fades in as energy drops.
        helmetUI.vignetteImage.color = Color.Lerp(Color.clear, vignetteBaseColor, 1 - energyPercent);

        // Visor activates in bright light.
        float glareAmount = Mathf.Clamp01(Mathf.InverseLerp(glareThreshold, glareThreshold + 1, lightLevel));
        helmetUI.visorImage.color = Color.Lerp(Color.clear, visorBaseColor, glareAmount);

        // --- SOUND LOGIC FOR VISOR ACTIVATE/DEACTIVATE ---
        bool isVisorActive = glareAmount > 0.1f; // Use a small threshold for robustness
        if(isVisorActive && !wasVisorActiveLastFrame)
        {
            PlaySound(sounds.visorActivate);
        }
        else if (!isVisorActive && wasVisorActiveLastFrame)
        {
            PlaySound(sounds.visorDeactivate);
        }
    }

    // This is a more precise way to measure light than just global ambient intensity.
    private float SenseEffectiveLightLevel()
    {
        float lightLevel = RenderSettings.ambientIntensity;
        Collider[] nearbyLights = Physics.OverlapSphere(lightSensorSource.position, lightSensorRadius, -1, QueryTriggerInteraction.Ignore);
        foreach (var col in nearbyLights)
        {
            if (col.TryGetComponent<Light>(out Light light) && light.enabled)
            {
                float distance = Vector3.Distance(lightSensorSource.position, light.transform.position);
                if (distance < light.range)
                {
                    lightLevel += light.intensity * (1f - (distance / light.range));
                }
            }
        }
        return lightLevel;
    }

    // NEW: Helper function to play a one-shot sound safely.
    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            // PlayOneShot is ideal for effects; it doesn't interrupt other sounds.
            audioSource.PlayOneShot(clip);
        }
    }

    // NEW: Centralized place to update our state trackers for the next frame.
    private void UpdateStateTrackers()
    {
        wasHeadlampOnLastFrame = headlampLight.enabled;
        wasFlickeringLastFrame = (currentEnergy / maxEnergy) < flickerThreshold && headlampLight.enabled;
        wasVisorActiveLastFrame = helmetUI.visorImage.color.a > 0.1f;
        // wasLowEnergyHumming is handled inside HandleEnergy because it's a looping sound
    }
}