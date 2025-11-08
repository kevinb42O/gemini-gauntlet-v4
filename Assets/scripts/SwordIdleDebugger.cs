using UnityEngine;

/// <summary>
/// Debug tool to diagnose L_sword_idle vs R_sword_idle animation issues.
/// Attach to player and run to see detailed animator state information.
/// </summary>
public class SwordIdleDebugger : MonoBehaviour
{
    [Header("Hand Controllers")]
    public IndividualLayeredHandController leftHandController;
    public IndividualLayeredHandController rightHandController;
    
    [Header("Debug Settings")]
    public bool enableRealTimeLogging = true;
    public KeyCode debugKey = KeyCode.F9;
    
    private void Update()
    {
        if (Input.GetKeyDown(debugKey))
        {
            DiagnoseAnimatorState();
        }
    }
    
    [ContextMenu("Diagnose Animator State")]
    public void DiagnoseAnimatorState()
    {
        Debug.Log("========== SWORD IDLE DIAGNOSTIC ==========");
        
        // Check left hand
        if (leftHandController != null && leftHandController.handAnimator != null)
        {
            DiagnoseHand(leftHandController, "LEFT HAND");
        }
        else
        {
            Debug.LogError("❌ LEFT HAND: Controller or Animator missing!");
        }
        
        // Check right hand
        if (rightHandController != null && rightHandController.handAnimator != null)
        {
            DiagnoseHand(rightHandController, "RIGHT HAND");
        }
        else
        {
            Debug.LogError("❌ RIGHT HAND: Controller or Animator missing!");
        }
        
        Debug.Log("===========================================");
    }
    
    private void DiagnoseHand(IndividualLayeredHandController controller, string handName)
    {
        Animator animator = controller.handAnimator;
        
        Debug.Log($"\n📋 {handName} DIAGNOSTICS:");
        Debug.Log($"   • GameObject: {controller.gameObject.name}");
        Debug.Log($"   • Animator Controller: {(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "NULL")}");
        
        // Check if isSwordMode parameter exists
        bool hasParameter = false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == "isSwordMode")
            {
                hasParameter = true;
                Debug.Log($"   ✅ Parameter 'isSwordMode' EXISTS - Current value: {animator.GetBool("isSwordMode")}");
                break;
            }
        }
        
        if (!hasParameter)
        {
            Debug.LogError($"   ❌ Parameter 'isSwordMode' MISSING! This is the problem!");
        }
        
        // Check current state on base layer
        AnimatorStateInfo baseState = animator.GetCurrentAnimatorStateInfo(0);
        Debug.Log($"   • Base Layer (0) Current State:");
        Debug.Log($"      - Full Path Hash: {baseState.fullPathHash}");
        Debug.Log($"      - Short Name Hash: {baseState.shortNameHash}");
        Debug.Log($"      - Normalized Time: {baseState.normalizedTime:F2}");
        Debug.Log($"      - Length: {baseState.length:F2}s");
        
        // Try to detect which animation is playing
        string currentStateName = "Unknown";
        foreach (AnimatorClipInfo clipInfo in animator.GetCurrentAnimatorClipInfo(0))
        {
            currentStateName = clipInfo.clip.name;
            Debug.Log($"      - Playing Clip: {currentStateName}");
        }
        
        // Check all parameters
        Debug.Log($"   • All Parameters:");
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            string value = "";
            switch (param.type)
            {
                case AnimatorControllerParameterType.Bool:
                    value = animator.GetBool(param.name).ToString();
                    break;
                case AnimatorControllerParameterType.Float:
                    value = animator.GetFloat(param.name).ToString("F2");
                    break;
                case AnimatorControllerParameterType.Int:
                    value = animator.GetInteger(param.name).ToString();
                    break;
                case AnimatorControllerParameterType.Trigger:
                    value = "(Trigger)";
                    break;
            }
            Debug.Log($"      - {param.name} ({param.type}): {value}");
        }
        
        // Check sword mode state
        Debug.Log($"   • IsSwordModeActive: {controller.IsSwordModeActive}");
    }
    
    [ContextMenu("Force Enable Left Sword Mode")]
    public void ForceEnableLeftSwordMode()
    {
        if (leftHandController != null)
        {
            leftHandController.EnableSwordMode();
            Debug.Log("✅ Forced LEFT sword mode ON");
            DiagnoseHand(leftHandController, "LEFT HAND (AFTER ENABLE)");
        }
    }
    
    [ContextMenu("Force Enable Right Sword Mode")]
    public void ForceEnableRightSwordMode()
    {
        if (rightHandController != null)
        {
            rightHandController.EnableSwordMode();
            Debug.Log("✅ Forced RIGHT sword mode ON");
            DiagnoseHand(rightHandController, "RIGHT HAND (AFTER ENABLE)");
        }
    }
    
    [ContextMenu("Force Disable Both Sword Modes")]
    public void ForceDisableBothSwordModes()
    {
        if (leftHandController != null)
        {
            leftHandController.DisableSwordMode();
            Debug.Log("✅ Forced LEFT sword mode OFF");
        }
        
        if (rightHandController != null)
        {
            rightHandController.DisableSwordMode();
            Debug.Log("✅ Forced RIGHT sword mode OFF");
        }
    }
}
