using UnityEngine;

/// <summary>
/// Quick fix script to set proper VFX scale for large worlds
/// Attach to any GameObject and click the button in Inspector
/// </summary>
public class FixWallJumpVFXScale : MonoBehaviour
{
    [Header("Click the button below to fix VFX scale")]
    [Tooltip("Set this to match your world scale (e.g., 20-30 for worlds scaled 2000+)")]
    public float newEffectScale = 30f;
    
    [Tooltip("New surface offset to prevent z-fighting")]
    public float newSurfaceOffset = 2f;
    
    [ContextMenu("FIX VFX SCALE NOW")]
    public void FixVFXScale()
    {
        WallJumpImpactVFX vfx = FindObjectOfType<WallJumpImpactVFX>();
        
        if (vfx == null)
        {
            Debug.LogError("❌ No WallJumpImpactVFX found in scene! Make sure the VFX Manager GameObject exists.");
            return;
        }
        
        vfx.SetEffectScale(newEffectScale);
        vfx.effectScale = newEffectScale;
        vfx.surfaceOffset = newSurfaceOffset;
        
        Debug.Log($"✅ VFX FIXED! Effect Scale set to {newEffectScale}, Surface Offset set to {newSurfaceOffset}");
        Debug.Log($"🎯 Wall jump and watch for ripples!");
    }
    
    [ContextMenu("CHECK CURRENT VFX SETTINGS")]
    public void CheckSettings()
    {
        WallJumpImpactVFX vfx = FindObjectOfType<WallJumpImpactVFX>();
        
        if (vfx == null)
        {
            Debug.LogError("❌ No WallJumpImpactVFX found in scene!");
            return;
        }
        
        Debug.Log($"📊 Current VFX Settings:");
        Debug.Log($"  Effect Scale: {vfx.effectScale}");
        Debug.Log($"  Surface Offset: {vfx.surfaceOffset}");
        Debug.Log($"  Base Duration: {vfx.baseDuration}");
        Debug.Log($"  Min Speed Threshold: {vfx.minSpeedThreshold}");
        Debug.Log($"  Max Intensity Speed: {vfx.maxIntensitySpeed}");
    }
}
