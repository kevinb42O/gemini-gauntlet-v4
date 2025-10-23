using UnityEngine;

public class CompanionAudioTester : MonoBehaviour
{
    public CompanionAILegacy companionAI;

    void Update()
    {
        // Press 'T' key to test companion audio
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (companionAI != null)
            {
                Debug.Log("[CompanionAudioTester] 🎵 TESTING COMPANION AUDIO...");
                companionAI.TestAudioSystem();
            }
            else
            {
                Debug.LogError("[CompanionAudioTester] ❌ No CompanionAI assigned!");
            }
        }
    }
}
