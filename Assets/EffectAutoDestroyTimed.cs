// --- EffectAutoDestroyTimed.cs (NEW SCRIPT) ---
using UnityEngine;

public class EffectAutoDestroyTimed : MonoBehaviour
{
    [Tooltip("How long in seconds before this GameObject (and all its children) is destroyed.")]
    public float destroyAfterSeconds = 10.0f;

    void Start()
    {
        // Schedule the GameObject this script is attached to for destruction
        // after 'destroyAfterSeconds'.
        Destroy(gameObject, destroyAfterSeconds);
        // Debug.Log($"Effect '{this.name}' scheduled to self-destruct in {destroyAfterSeconds} seconds.");
    }

    // No Update() needed for this simple timed destruction.
}