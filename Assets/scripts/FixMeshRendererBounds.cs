using UnityEngine;

/// <summary>
/// EMERGENCY FIX: Forces mesh renderer bounds to be HUGE so objects never get culled
/// This fixes the "disappears when looking at it" bug caused by incorrect bounds calculation
/// 
/// USAGE:
/// 1. Add this to any object that's disappearing
/// 2. OR add to parent object and check "Fix Children"
/// 3. Click "Fix Bounds Now" in inspector
/// 
/// WHY THIS HAPPENS:
/// - ProBuilder meshes can have wrong bounds
/// - Procedural meshes don't calculate bounds properly
/// - Large-scale worlds (300+ unit characters) mess up Unity's auto-calculation
/// </summary>
public class FixMeshRendererBounds : MonoBehaviour
{
    [Header("=== BOUNDS FIX SETTINGS ===")]
    [Tooltip("Size of the bounding box (make HUGE for large worlds)")]
    [SerializeField] private Vector3 boundsSize = new Vector3(5000f, 5000f, 5000f);
    
    [Tooltip("Center offset of bounds (usually Vector3.zero)")]
    [SerializeField] private Vector3 boundsCenter = Vector3.zero;
    
    [Tooltip("Fix all MeshRenderers in children too")]
    [SerializeField] private bool fixChildren = true;
    
    [Tooltip("Auto-fix on Start()")]
    [SerializeField] private bool autoFixOnStart = true;
    
    [Tooltip("Show debug gizmos in Scene view")]
    [SerializeField] private bool showDebugGizmos = true;
    
    [Header("=== DEBUG INFO ===")]
    [SerializeField] private bool showDebugLogs = true;
    
    private void Start()
    {
        if (autoFixOnStart)
        {
            FixBoundsNow();
        }
    }
    
    [ContextMenu("Fix Bounds Now")]
    public void FixBoundsNow()
    {
        int fixedCount = 0;
        
        // Fix this object
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            FixRenderer(renderer);
            fixedCount++;
        }
        
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            FixMesh(meshFilter.sharedMesh);
        }
        
        // Fix children if enabled
        if (fixChildren)
        {
            MeshRenderer[] childRenderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var childRenderer in childRenderers)
            {
                if (childRenderer != renderer) // Don't double-fix
                {
                    FixRenderer(childRenderer);
                    fixedCount++;
                }
            }
            
            MeshFilter[] childFilters = GetComponentsInChildren<MeshFilter>();
            foreach (var childFilter in childFilters)
            {
                if (childFilter != meshFilter && childFilter.sharedMesh != null)
                {
                    FixMesh(childFilter.sharedMesh);
                }
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[FixMeshRendererBounds] ✅ Fixed {fixedCount} renderers on '{gameObject.name}' with bounds size {boundsSize}");
        }
    }
    
    private void FixRenderer(MeshRenderer renderer)
    {
        if (renderer == null) return;
        
        // Create huge bounds
        Bounds newBounds = new Bounds(boundsCenter, boundsSize);
        
        // Get MeshFilter
        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            // Set mesh bounds (this fixes the root cause)
            meshFilter.sharedMesh.bounds = newBounds;
            
            if (showDebugLogs)
            {
                Debug.Log($"[FixMeshRendererBounds] 🔧 Fixed mesh bounds for '{renderer.name}' → Size: {boundsSize}, Center: {boundsCenter}");
            }
        }
        
        // Force renderer to update
        renderer.enabled = false;
        renderer.enabled = true;
    }
    
    private void FixMesh(Mesh mesh)
    {
        if (mesh == null) return;
        
        Bounds newBounds = new Bounds(boundsCenter, boundsSize);
        mesh.bounds = newBounds;
        
        if (showDebugLogs)
        {
            Debug.Log($"[FixMeshRendererBounds] 🔧 Fixed mesh '{mesh.name}' → Size: {boundsSize}");
        }
    }
    
    [ContextMenu("Show Current Bounds")]
    public void ShowCurrentBounds()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Debug.Log($"=== CURRENT BOUNDS: {gameObject.name} ===");
            Debug.Log($"Renderer Bounds: {renderer.bounds}");
            Debug.Log($"Bounds Size: {renderer.bounds.size}");
            Debug.Log($"Bounds Center: {renderer.bounds.center}");
            
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Debug.Log($"Mesh Bounds: {meshFilter.sharedMesh.bounds}");
                Debug.Log($"Mesh Size: {meshFilter.sharedMesh.bounds.size}");
            }
            Debug.Log("=====================================");
        }
        else
        {
            Debug.LogWarning($"[FixMeshRendererBounds] No MeshRenderer on '{gameObject.name}'");
        }
    }
    
    [ContextMenu("Auto-Calculate Huge Bounds")]
    public void AutoCalculateHugeBounds()
    {
        // Get all renderers in children
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        
        if (renderers.Length == 0)
        {
            Debug.LogWarning("[FixMeshRendererBounds] No renderers found!");
            return;
        }
        
        // Calculate combined bounds
        Bounds combinedBounds = renderers[0].bounds;
        foreach (var r in renderers)
        {
            combinedBounds.Encapsulate(r.bounds);
        }
        
        // Make bounds 3x bigger to be safe
        boundsSize = combinedBounds.size * 3f;
        boundsCenter = combinedBounds.center - transform.position; // Local offset
        
        Debug.Log($"[FixMeshRendererBounds] 📏 Auto-calculated bounds: Size={boundsSize}, Center={boundsCenter}");
        Debug.Log($"[FixMeshRendererBounds] 💡 Now click 'Fix Bounds Now' to apply!");
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        // Draw the bounds box
        Gizmos.color = Color.green;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(boundsCenter, boundsSize);
        
        // Draw current renderer bounds (if exists)
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = Matrix4x4.identity; // World space
            Gizmos.DrawWireCube(renderer.bounds.center, renderer.bounds.size);
        }
    }
}
