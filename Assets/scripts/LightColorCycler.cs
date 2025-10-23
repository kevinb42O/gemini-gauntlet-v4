using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Light))]
[AddComponentMenu("Lighting/Light Color Cycler")]
public class LightColorCycler : MonoBehaviour
{
    [Tooltip("How many full color cycles per second. 0.1 = one full cycle every 10 seconds.")]
    [Min(0f)]
    [SerializeField] private float cyclesPerSecond = 0.1f;

    private Light _light;
    private float _hue;

    private void Awake()
    {
        _light = GetComponent<Light>();
        // Start at current light color's hue if possible
        Color.RGBToHSV(_light.color, out _hue, out _, out _);
    }

    private void Update()
    {
        if (_light == null) return;

        // Advance hue and wrap 0..1
        _hue += cyclesPerSecond * Time.deltaTime;
        if (_hue > 1f) _hue -= 1f;
        else if (_hue < 0f) _hue += 1f;

        _light.color = Color.HSVToRGB(_hue, 1f, 1f);
    }
}
