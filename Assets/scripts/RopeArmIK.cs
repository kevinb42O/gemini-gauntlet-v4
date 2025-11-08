// --- RopeArmIK.cs ---
// ZERO-BLOAT PROCEDURAL ARM POINTING FOR DUAL ROPE GRAPPLING
// Rotates arm models to point toward rope anchors WITHOUT needing animations
// Works with RobotArmII_L and RobotArmII_R models (NO IK RIG NEEDED!)

using UnityEngine;

/// <summary>
/// Procedurally rotates entire arm models to point toward rope anchors.
/// Runs in LateUpdate() to override animator's falling animation.
/// Zero animation files needed - pure code solution!
/// Works with mesh renderer arm models - NO IK RIG REQUIRED!
/// </summary>
public class RopeArmIK : MonoBehaviour
{
    [Header("=== 🦾 ARM BONE REFERENCES ===")]
    [Tooltip("Left arm bone (L_Arm inside RobotArmII_L)")]
    [SerializeField] private Transform leftArmBone;
    
    [Tooltip("Right arm bone (R_Arm inside RobotArmII_R)")]
    [SerializeField] private Transform rightArmBone;
    
    [Header("=== 🎯 SYSTEM REFERENCES ===")]
    [Tooltip("Reference to grappling system (auto-found if not set)")]
    [SerializeField] private AdvancedGrapplingSystem grapplingSystem;
    
    [Tooltip("Reference to hand animation controller (auto-found if not set)")]
    [SerializeField] private LayeredHandAnimationController handAnimationController;
    
    [Header("=== ⚙️ IK SETTINGS ===")]
    [Tooltip("Up/down offset for pointing direction (negative = point down more)")]
    [Range(-90f, 90f)]
    [SerializeField] private float pitchOffset = -45f;
    
    [Tooltip("Left/right offset for pointing direction")]
    [Range(-90f, 90f)]
    [SerializeField] private float yawOffset = 0f;
    
    [Header("=== 🔧 DEBUG ===")]
    [Tooltip("Draw debug lines showing arm aim direction")]
    [SerializeField] private bool showDebugLines = true;
    
    // Track if we were just roping
    private bool _wasRopingLastFrame = false;
    
    void Awake()
    {
        // Auto-find grappling system if not assigned
        if (grapplingSystem == null)
        {
            grapplingSystem = GetComponent<AdvancedGrapplingSystem>();
            
            if (grapplingSystem == null)
                Debug.LogError("[RopeArmIK] ❌ AdvancedGrapplingSystem not found! Please assign in inspector.", this);
        }
        
        // Auto-find hand animation controller if not assigned
        if (handAnimationController == null)
        {
            handAnimationController = GetComponent<LayeredHandAnimationController>();
            
            if (handAnimationController == null)
                Debug.LogWarning("[RopeArmIK] ⚠️ LayeredHandAnimationController not found! Animator may conflict with IK.", this);
        }
    }
    
    void OnEnable()
    {
        // Reset state when enabled
        _wasRopingLastFrame = false;
    }
    
    void OnDisable()
    {
        // Reset state when disabled
        _wasRopingLastFrame = false;
    }
    
    void LateUpdate()
    {
        if (grapplingSystem == null) return;
        
        bool anyRopeActive = grapplingSystem.IsLeftRopeActive || grapplingSystem.IsRightRopeActive;
        
        // Track state changes
        if (anyRopeActive && !_wasRopingLastFrame)
        {
            _wasRopingLastFrame = true;
        }
        else if (!anyRopeActive && _wasRopingLastFrame)
        {
            _wasRopingLastFrame = false;
            return; // Let animator take over immediately
        }
        
        // Only override when actively roping
        if (!anyRopeActive) return;
        
        // 🔥 HARD OVERRIDE EVERY FRAME - constantly track the anchor position!
        // No state checks, no blending - just pure real-time tracking
        if (grapplingSystem.IsLeftRopeActive && leftArmBone != null)
        {
            Vector3 leftAnchor = grapplingSystem.LeftRopeAnchor;
            PointArmAtTarget(leftArmBone, leftAnchor, true);
        }
        
        if (grapplingSystem.IsRightRopeActive && rightArmBone != null)
        {
            Vector3 rightAnchor = grapplingSystem.RightRopeAnchor;
            PointArmAtTarget(rightArmBone, rightAnchor, false);
        }
    }
    
    /// <summary>
    /// HARD OVERRIDE - Point arm directly at target (no blending with animator)
    /// Simplified for perfect tracking - just rotate the shoulder toward anchor
    /// </summary>
    private void PointArmAtTarget(Transform armBone, Vector3 targetWorldPos, bool isLeftArm)
    {
        // 🔥 Calculate direction from shoulder to anchor
        Vector3 shoulderPos = armBone.position;
        Vector3 toAnchor = (targetWorldPos - shoulderPos).normalized;
        
        // 🔥 Create world-space rotation pointing at anchor
        // Use world up for stability (not parent.up which can be rotated)
        Quaternion worldRotation = Quaternion.LookRotation(toAnchor, Vector3.up);
        
        // 🔥 Apply natural arm pointing offset (slight pitch down)
        worldRotation *= Quaternion.Euler(pitchOffset, yawOffset, 0);
        
        // 🔥 Convert to local space (relative to parent bone)
        Quaternion localRotation = Quaternion.Inverse(armBone.parent.rotation) * worldRotation;
        
        // 🔥 LEFT ARM: Mirror the rotation for proper left-hand pointing
        if (isLeftArm)
        {
            // Mirror by flipping the Y rotation (horizontal axis)
            Vector3 euler = localRotation.eulerAngles;
            euler.y = -euler.y; // Flip horizontal rotation
            localRotation = Quaternion.Euler(euler);
        }
        
        // 🔥 HARD SET - Override animator completely!
        armBone.localRotation = localRotation;
        
        // Debug visualization
        if (showDebugLines)
        {
            Color debugColor = isLeftArm ? Color.cyan : Color.magenta;
            Debug.DrawLine(shoulderPos, targetWorldPos, debugColor);
            // Debug.DrawRay removed for performance
        }
    }
    
    /// <summary>
    /// Context menu helper to find arm bones automatically
    /// Right-click component > Auto-Find Arm Bones
    /// Looks for L_Arm and R_Arm bones (inside RobotArmII_L/R)
    /// </summary>
    [ContextMenu("Auto-Find Arm Bones")]
    private void AutoFindArmBones()
    {
        // Search for L_Arm and R_Arm bones in hierarchy
        Transform[] allTransforms = GetComponentsInChildren<Transform>(true); // Include inactive
        
        foreach (Transform t in allTransforms)
        {
            string name = t.name;
            
            // Look for L_Arm bone (the actual rotating bone inside RobotArmII_L)
            if (name == "L_Arm")
            {
                leftArmBone = t;
                Debug.Log($"[RopeArmIK] ✅ Found LEFT arm bone: {t.name} (path: {GetPath(t)})", t);
            }
            
            // Look for R_Arm bone (the actual rotating bone inside RobotArmII_R)
            if (name == "R_Arm")
            {
                rightArmBone = t;
                Debug.Log($"[RopeArmIK] ✅ Found RIGHT arm bone: {t.name} (path: {GetPath(t)})", t);
            }
        }
        
        if (leftArmBone == null)
            Debug.LogWarning("[RopeArmIK] ⚠️ Could not find L_Arm bone. Please assign manually.");
        if (rightArmBone == null)
            Debug.LogWarning("[RopeArmIK] ⚠️ Could not find R_Arm bone. Please assign manually.");
    }
    
    /// <summary>
    /// Helper to get full hierarchy path for debugging
    /// </summary>
    private string GetPath(Transform t)
    {
        string path = t.name;
        Transform current = t.parent;
        while (current != null && current != transform)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }
    
    void OnDrawGizmosSelected()
    {
        // Visualize arm bones in scene view
        if (leftArmBone != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(leftArmBone.position, 50f);
            Gizmos.DrawRay(leftArmBone.position, leftArmBone.forward * 300f);
        }
        
        if (rightArmBone != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(rightArmBone.position, 50f);
            Gizmos.DrawRay(rightArmBone.position, rightArmBone.forward * 300f);
        }
    }
}
