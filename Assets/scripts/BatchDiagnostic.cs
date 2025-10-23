using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Diagnostic tool to find what's causing high batch counts
/// Add to any GameObject and check Console for report
/// </summary>
public class BatchDiagnostic : MonoBehaviour
{
    [ContextMenu("Run Batch Diagnostic")]
    void RunDiagnostic()
    {
        Debug.Log("=== BATCH DIAGNOSTIC REPORT ===");
        
        // Find all renderers
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();
        Debug.Log($"Total Renderers in Scene: {allRenderers.Length}");
        
        // Group by material
        Dictionary<Material, int> materialCounts = new Dictionary<Material, int>();
        Dictionary<string, int> shaderCounts = new Dictionary<string, int>();
        int nullMaterials = 0;
        int shadowCasters = 0;
        int nonStaticCount = 0;
        
        foreach (Renderer r in allRenderers)
        {
            // Check static
            if (!r.gameObject.isStatic && r.GetComponent<ParticleSystem>() == null)
            {
                nonStaticCount++;
            }
            
            // Check shadows
            if (r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off)
            {
                shadowCasters++;
            }
            
            // Check materials
            Material[] mats = r.sharedMaterials;
            foreach (Material mat in mats)
            {
                if (mat == null)
                {
                    nullMaterials++;
                    continue;
                }
                
                if (!materialCounts.ContainsKey(mat))
                {
                    materialCounts[mat] = 0;
                }
                materialCounts[mat]++;
                
                string shaderName = mat.shader.name;
                if (!shaderCounts.ContainsKey(shaderName))
                {
                    shaderCounts[shaderName] = 0;
                }
                shaderCounts[shaderName]++;
            }
        }
        
        Debug.Log($"Unique Materials: {materialCounts.Count}");
        Debug.Log($"Null Materials: {nullMaterials}");
        Debug.Log($"Non-Static Renderers: {nonStaticCount}");
        Debug.Log($"Shadow Casters: {shadowCasters}");
        
        // Top 10 most used materials
        Debug.Log("\n=== TOP 10 MATERIALS (by usage) ===");
        var topMaterials = materialCounts.OrderByDescending(x => x.Value).Take(10);
        foreach (var kvp in topMaterials)
        {
            Debug.Log($"{kvp.Key.name}: {kvp.Value} instances");
        }
        
        // Shaders
        Debug.Log("\n=== SHADERS IN USE ===");
        foreach (var kvp in shaderCounts.OrderByDescending(x => x.Value))
        {
            Debug.Log($"{kvp.Key}: {kvp.Value} materials");
        }
        
        // UI Canvas check
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Debug.Log($"\n=== UI CANVASES: {canvases.Length} ===");
        foreach (Canvas canvas in canvases)
        {
            UnityEngine.UI.Graphic[] graphics = canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>();
            Debug.Log($"Canvas '{canvas.name}': {graphics.Length} UI elements, RenderMode: {canvas.renderMode}");
        }
        
        // Particle systems
        ParticleSystem[] particles = FindObjectsOfType<ParticleSystem>();
        Debug.Log($"\n=== PARTICLE SYSTEMS: {particles.Length} ===");
        int activeParticles = 0;
        foreach (ParticleSystem ps in particles)
        {
            if (ps.isPlaying)
            {
                activeParticles++;
            }
        }
        Debug.Log($"Active Particle Systems: {activeParticles}");
        
        Debug.Log("\n=== RECOMMENDATIONS ===");
        if (materialCounts.Count > 50)
        {
            Debug.LogWarning("⚠️ Too many unique materials! Combine materials using texture atlases.");
        }
        if (nonStaticCount > 100)
        {
            Debug.LogWarning($"⚠️ {nonStaticCount} non-static renderers! Mark static objects as Static.");
        }
        if (shadowCasters > 100)
        {
            Debug.LogWarning($"⚠️ {shadowCasters} shadow casters! Disable shadows on small objects.");
        }
        if (canvases.Length > 5)
        {
            Debug.LogWarning($"⚠️ {canvases.Length} canvases! Combine UI into fewer canvases.");
        }
        
        Debug.Log("\n=== END REPORT ===");
    }
    
    void Start()
    {
        // Auto-run on start
        Invoke("RunDiagnostic", 1f);
    }
}
