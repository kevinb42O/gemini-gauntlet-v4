using UnityEngine;
using UnityEditor;
using GeminiGauntlet.Animation;

[CustomEditor(typeof(ProceduralHandAnimator))]
public class ProceduralHandAnimatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        
        // Get the target component
        ProceduralHandAnimator animator = (ProceduralHandAnimator)target;
        
        // Big friendly setup button
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 14;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.normal.textColor = Color.white;
        
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("🔧 Easy Arm Setup", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Click the button below to automatically find and connect all arm bones", EditorStyles.helpBox);
        
        EditorGUILayout.Space();
        
        // Check current connection status
        bool leftArmConnected = animator.leftShoulderBone && animator.leftElbowBone && animator.leftWristBone;
        bool rightArmConnected = animator.rightShoulderBone && animator.rightElbowBone && animator.rightWristBone;
        
        // Check finger connection status
        int leftFingerCount = CountFingers(animator.leftThumbBones) + CountFingers(animator.leftIndexBones) + 
                             CountFingers(animator.leftMiddleBones) + CountFingers(animator.leftRingBones) + 
                             CountFingers(animator.leftPinkyBones);
        int rightFingerCount = CountFingers(animator.rightThumbBones) + CountFingers(animator.rightIndexBones) + 
                              CountFingers(animator.rightMiddleBones) + CountFingers(animator.rightRingBones) + 
                              CountFingers(animator.rightPinkyBones);
        
        // Status display
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Left Arm:", EditorStyles.label, GUILayout.Width(70));
        EditorGUILayout.LabelField(leftArmConnected ? "✅ Connected" : "❌ Disconnected", 
                                 leftArmConnected ? EditorStyles.label : EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Right Arm:", EditorStyles.label, GUILayout.Width(70));
        EditorGUILayout.LabelField(rightArmConnected ? "✅ Connected" : "❌ Disconnected", 
                                 rightArmConnected ? EditorStyles.label : EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
        
        // Show finger status
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Left Fingers:", EditorStyles.label, GUILayout.Width(70));
        EditorGUILayout.LabelField($"{leftFingerCount}/20 bones", 
                                 leftFingerCount == 20 ? EditorStyles.label : EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Right Fingers:", EditorStyles.label, GUILayout.Width(70));
        EditorGUILayout.LabelField($"{rightFingerCount}/20 bones", 
                                 rightFingerCount == 20 ? EditorStyles.label : EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Auto-connect button
        bool allConnected = leftArmConnected && rightArmConnected && leftFingerCount == 20 && rightFingerCount == 20;
        GUI.backgroundColor = allConnected ? Color.green : Color.yellow;
        if (GUILayout.Button("🚀 Auto-Connect Arms + ALL FINGERS", buttonStyle, GUILayout.Height(40)))
        {
            animator.AutoConnectArms();
            EditorUtility.SetDirty(animator); // Mark the object as dirty so changes are saved
        }
        GUI.backgroundColor = Color.white;
        
        if (allConnected)
        {
            EditorGUILayout.LabelField("🌟 INCREDIBLE! Both arms + all 40 fingers connected!", EditorStyles.centeredGreyMiniLabel);
        }
        else if (leftArmConnected && rightArmConnected)
        {
            EditorGUILayout.LabelField($"🎉 Arms connected! Fingers: {leftFingerCount + rightFingerCount}/40", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            EditorGUILayout.LabelField("⚠️ Click the button above to set up missing connections", EditorStyles.centeredGreyMiniLabel);
        }
        
        EditorGUILayout.EndVertical();
        
        // Advanced options
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("🔧 Advanced Options", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Refresh Bone References"))
        {
            animator.RefreshBoneReferences();
            EditorUtility.SetDirty(animator);
        }
        
        if (GUILayout.Button("Reset All Bone References"))
        {
            animator.leftShoulderBone = null;
            animator.leftElbowBone = null;
            animator.leftWristBone = null;
            animator.rightShoulderBone = null;
            animator.rightElbowBone = null;
            animator.rightWristBone = null;
            
            // Clear all finger arrays
            ClearFingerArray(animator.leftThumbBones);
            ClearFingerArray(animator.leftIndexBones);
            ClearFingerArray(animator.leftMiddleBones);
            ClearFingerArray(animator.leftRingBones);
            ClearFingerArray(animator.leftPinkyBones);
            ClearFingerArray(animator.rightThumbBones);
            ClearFingerArray(animator.rightIndexBones);
            ClearFingerArray(animator.rightMiddleBones);
            ClearFingerArray(animator.rightRingBones);
            ClearFingerArray(animator.rightPinkyBones);
            
            EditorUtility.SetDirty(animator);
            Debug.Log("🔄 All bone references cleared (arms + fingers)");
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private int CountFingers(Transform[] fingerArray)
    {
        if (fingerArray == null) return 0;
        int count = 0;
        foreach (var bone in fingerArray)
        {
            if (bone != null) count++;
        }
        return count;
    }
    
    private void ClearFingerArray(Transform[] fingerArray)
    {
        if (fingerArray != null)
        {
            for (int i = 0; i < fingerArray.Length; i++)
            {
                fingerArray[i] = null;
            }
        }
    }
}