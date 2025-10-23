using UnityEngine;
using TMPro;

/// <summary>
/// 🎨 TEXTMESHPRO EFFECTS CONTROLLER - EXTRAORDINARY EDITION
/// Applies ALL the cool TMP effects: gradients, outlines, glows, vertex colors, animations!
/// </summary>
public class TMPEffectsController : MonoBehaviour
{
    [Header("=== FONT ASSIGNMENTS ===")]
    [Tooltip("Combat font - Oswald Bold (aggressive, powerful)")]
    public TMP_FontAsset combatFont;
    [Tooltip("Movement font - Roboto Bold (clean, dynamic)")]
    public TMP_FontAsset movementFont;
    [Tooltip("Tricks font - Bangers (flashy, explosive!)")]
    public TMP_FontAsset tricksFont;
    
    [Header("=== COLOR GRADIENTS ===")]
    [Tooltip("Combat gradient - Yellow to Orange")]
    public TMP_ColorGradient combatGradient;
    [Tooltip("Movement gradient - Blue to Purple")]
    public TMP_ColorGradient movementGradient;
    [Tooltip("Tricks gradient - Light to Dark Green")]
    public TMP_ColorGradient tricksGradient;
    
    [Header("=== EFFECT SETTINGS ===")]
    [Range(0f, 1f)] public float outlineThickness = 0.3f;
    [Range(0f, 1f)] public float glowPower = 0.5f;
    [Range(0f, 1f)] public float underlayOffsetX = 0.5f;
    [Range(0f, 1f)] public float underlayOffsetY = -0.5f;
    [Range(0f, 1f)] public float underlayDilate = 0.5f;
    [Range(0f, 1f)] public float underlaySoftness = 0.2f;
    
    void Start()
    {
        // Auto-load fonts and gradients if not assigned
        LoadDefaultAssets();
    }
    
    void LoadDefaultAssets()
    {
        // CRITICAL FIX: Use TMP's guaranteed default font instead of trying to load fonts that may not exist
        // If custom fonts aren't assigned, we'll just use the default font with different styles
        if (combatFont == null && TMP_Settings.defaultFontAsset != null)
            combatFont = TMP_Settings.defaultFontAsset;
        if (movementFont == null && TMP_Settings.defaultFontAsset != null)
            movementFont = TMP_Settings.defaultFontAsset;
        if (tricksFont == null && TMP_Settings.defaultFontAsset != null)
            tricksFont = TMP_Settings.defaultFontAsset;
        
        // Try to load color gradients (optional - won't break if missing)
        if (combatGradient == null)
            combatGradient = Resources.Load<TMP_ColorGradient>("Color Gradient Presets/Yellow to Orange - Vertical");
        if (movementGradient == null)
            movementGradient = Resources.Load<TMP_ColorGradient>("Color Gradient Presets/Blue to Purple - Vertical");
        if (tricksGradient == null)
            tricksGradient = Resources.Load<TMP_ColorGradient>("Color Gradient Presets/Light to Dark Green - Vertical");
    }
    
    /// <summary>
    /// Apply EXTRAORDINARY effects to TextMeshPro component
    /// </summary>
    public void ApplyEffects(TextMeshPro tmp, GeminiGauntlet.UI.FloatingTextManager.TextStyle style)
    {
        if (tmp == null) return;
        
        // Apply font and gradient based on style
        switch (style)
        {
            case GeminiGauntlet.UI.FloatingTextManager.TextStyle.Combat:
                ApplyCombatEffects(tmp);
                break;
            case GeminiGauntlet.UI.FloatingTextManager.TextStyle.Movement:
                ApplyMovementEffects(tmp);
                break;
            case GeminiGauntlet.UI.FloatingTextManager.TextStyle.Tricks:
                ApplyTricksEffects(tmp);
                break;
        }
        
        // CRITICAL: Ensure material renders through walls (apply AFTER all effects!)
        if (tmp.fontSharedMaterial != null)
        {
            tmp.fontSharedMaterial.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
            tmp.fontSharedMaterial.renderQueue = 5000;
        }
    }
    
    /// <summary>
    /// COMBAT: Bold, aggressive, powerful with yellow-orange gradient
    /// </summary>
    void ApplyCombatEffects(TextMeshPro tmp)
    {
        // Font
        if (combatFont != null)
            tmp.font = combatFont;
        
        // Style
        tmp.fontStyle = FontStyles.Bold;
        
        // Color gradient (yellow to orange - fire!)
        if (combatGradient != null)
            tmp.colorGradientPreset = combatGradient;
        else
            tmp.enableVertexGradient = true;
        
        // Thick black outline for impact
        tmp.outlineWidth = 0.35f;
        tmp.outlineColor = Color.black;
        
        // MEMORY LEAK FIX: Check if material exists before modifying
        if (tmp.fontSharedMaterial != null)
        {
            // Underlay for depth (shadow effect)
            tmp.fontSharedMaterial.EnableKeyword("UNDERLAY_ON");
            tmp.fontSharedMaterial.SetFloat("_UnderlayOffsetX", 0.5f);
            tmp.fontSharedMaterial.SetFloat("_UnderlayOffsetY", -0.5f);
            tmp.fontSharedMaterial.SetFloat("_UnderlayDilate", 0.5f);
            tmp.fontSharedMaterial.SetFloat("_UnderlaySoftness", 0.2f);
            tmp.fontSharedMaterial.SetColor("_UnderlayColor", new Color(0, 0, 0, 0.8f));
            
            // Glow for intensity
            tmp.fontSharedMaterial.EnableKeyword("GLOW_ON");
            tmp.fontSharedMaterial.SetFloat("_GlowPower", 0.6f);
            tmp.fontSharedMaterial.SetColor("_GlowColor", new Color(1f, 0.5f, 0f, 1f)); // Orange glow
        }
    }
    
    /// <summary>
    /// MOVEMENT: Dynamic, flowing with blue-purple gradient
    /// </summary>
    void ApplyMovementEffects(TextMeshPro tmp)
    {
        // Font
        if (movementFont != null)
            tmp.font = movementFont;
        
        // Style
        tmp.fontStyle = FontStyles.Bold | FontStyles.Italic;
        
        // Color gradient (blue to purple - energy!)
        if (movementGradient != null)
            tmp.colorGradientPreset = movementGradient;
        else
            tmp.enableVertexGradient = true;
        
        // Medium outline for definition
        tmp.outlineWidth = 0.3f;
        tmp.outlineColor = new Color(0, 0, 0.5f, 1f); // Dark blue outline
        
        // MEMORY LEAK FIX: Check if material exists before modifying
        if (tmp.fontSharedMaterial != null)
        {
            // Subtle underlay
            tmp.fontSharedMaterial.EnableKeyword("UNDERLAY_ON");
            tmp.fontSharedMaterial.SetFloat("_UnderlayOffsetX", 0.3f);
            tmp.fontSharedMaterial.SetFloat("_UnderlayOffsetY", -0.3f);
            tmp.fontSharedMaterial.SetFloat("_UnderlayDilate", 0.3f);
            tmp.fontSharedMaterial.SetFloat("_UnderlaySoftness", 0.3f);
            tmp.fontSharedMaterial.SetColor("_UnderlayColor", new Color(0, 0, 0.5f, 0.6f));
            
            // Cyan glow for speed
            tmp.fontSharedMaterial.EnableKeyword("GLOW_ON");
            tmp.fontSharedMaterial.SetFloat("_GlowPower", 0.5f);
            tmp.fontSharedMaterial.SetColor("_GlowColor", new Color(0f, 1f, 1f, 1f)); // Cyan glow
        }
    }
    
    /// <summary>
    /// TRICKS: Flashy, extraordinary with green gradient and MAXIMUM effects!
    /// </summary>
    void ApplyTricksEffects(TextMeshPro tmp)
    {
        // Font (Bangers - explosive!)
        if (tricksFont != null)
            tmp.font = tricksFont;
        
        // Style
        tmp.fontStyle = FontStyles.Bold;
        
        // Color gradient (light to dark green - nature/energy!)
        if (tricksGradient != null)
            tmp.colorGradientPreset = tricksGradient;
        else
            tmp.enableVertexGradient = true;
        
        // THICK outline for maximum impact
        tmp.outlineWidth = 0.4f;
        tmp.outlineColor = Color.black;
        
        // MEMORY LEAK FIX: Check if material exists before modifying
        if (tmp.fontSharedMaterial != null)
        {
            // Strong underlay for depth
            tmp.fontSharedMaterial.EnableKeyword("UNDERLAY_ON");
            tmp.fontSharedMaterial.SetFloat("_UnderlayOffsetX", 0.6f);
            tmp.fontSharedMaterial.SetFloat("_UnderlayOffsetY", -0.6f);
            tmp.fontSharedMaterial.SetFloat("_UnderlayDilate", 0.6f);
            tmp.fontSharedMaterial.SetFloat("_UnderlaySoftness", 0.15f);
            tmp.fontSharedMaterial.SetColor("_UnderlayColor", new Color(0, 0, 0, 0.9f));
            
            // MAXIMUM glow for flashiness!
            tmp.fontSharedMaterial.EnableKeyword("GLOW_ON");
            tmp.fontSharedMaterial.SetFloat("_GlowPower", 0.8f);
            tmp.fontSharedMaterial.SetColor("_GlowColor", new Color(0f, 1f, 0.5f, 1f)); // Neon green glow
        }
    }
}
