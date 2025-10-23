using UnityEngine;
using GeminiGauntlet.UI;

/// <summary>
/// Simple test dummy for testing shooting and sword damage.
/// Attach to any object (cube, pill, etc.) to test damage systems.
/// Shows damage numbers via FloatingTextManager and glows when hit.
/// </summary>
public class TestDummy : MonoBehaviour, IDamageable
{
    [Header("Damage Display")]
    [Tooltip("Shows damage numbers above the dummy")]
    public bool showDamageNumbers = true;

    [Header("Visual Feedback")]
    [Tooltip("Color to glow when hit")]
    public Color glowColor = Color.red;
    
    [Tooltip("How long the glow lasts (seconds)")]
    public float glowDuration = 0.3f;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    // Internal state
    private Renderer objectRenderer;
    private Material originalMaterial;
    private Material glowMaterial;
    private Color originalColor;
    private float glowEndTime = 0f;
    private bool isGlowing = false;

    void Start()
    {
        // Get renderer for glow effect
        objectRenderer = GetComponent<Renderer>();
        
        if (objectRenderer != null)
        {
            // Store original material/color
            originalMaterial = objectRenderer.material;
            originalColor = originalMaterial.color;
            
            // Create glow material (clone to avoid modifying shared material)
            glowMaterial = new Material(originalMaterial);
        }
        else
        {
            Debug.LogWarning("[TestDummy] No Renderer found - glow effect will not work!");
        }
    }

    void Update()
    {
        // Handle glow fade
        if (isGlowing && Time.time >= glowEndTime)
        {
            ResetGlow();
        }
    }

    /// <summary>
    /// IDamageable interface implementation
    /// </summary>
    public void TakeDamage(float damageAmount, Vector3 hitPoint, Vector3 damageDirection)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[TestDummy] '{gameObject.name}' took {damageAmount} damage at {hitPoint}");
        }

        // Show damage number
        if (showDamageNumbers)
        {
            ShowDamageText(damageAmount, hitPoint);
        }

        // Trigger glow effect
        TriggerGlow();
    }

    /// <summary>
    /// Show floating damage text
    /// </summary>
    private void ShowDamageText(float damage, Vector3 position)
    {
        if (FloatingTextManager.Instance != null)
        {
            FloatingTextManager.Instance.ShowFloatingText(
                damage.ToString("F0"), // Show damage as whole number
                position,
                Color.white
            );
        }
        else
        {
            Debug.LogWarning("[TestDummy] FloatingTextManager not found! Cannot show damage numbers.");
        }
    }

    /// <summary>
    /// Make the object glow when hit
    /// </summary>
    private void TriggerGlow()
    {
        if (objectRenderer == null || glowMaterial == null)
            return;

        // Set glow color
        glowMaterial.color = glowColor;
        objectRenderer.material = glowMaterial;

        // Set glow duration
        glowEndTime = Time.time + glowDuration;
        isGlowing = true;
    }

    /// <summary>
    /// Reset glow back to original appearance
    /// </summary>
    private void ResetGlow()
    {
        if (objectRenderer == null || originalMaterial == null)
            return;

        objectRenderer.material.color = originalColor;
        isGlowing = false;
    }

    void OnDestroy()
    {
        // Clean up materials
        if (glowMaterial != null)
        {
            Destroy(glowMaterial);
        }
    }

    /// <summary>
    /// Draw gizmo in editor to show this is a test dummy
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
