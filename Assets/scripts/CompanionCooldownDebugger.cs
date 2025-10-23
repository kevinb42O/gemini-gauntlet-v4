using UnityEngine;

public class CompanionCooldownDebugger : MonoBehaviour
{
    [Header("Debug Controls")]
    public bool showDebugInfo = true;
    
    [Header("Test Buttons (Call these methods)")]
    [Space]
    public string instructions = "Use these methods in inspector or code:";

    void Update()
    {
        if (showDebugInfo && Input.GetKeyDown(KeyCode.F1))
        {
            ShowCooldownStatus();
        }
        
        if (Input.GetKeyDown(KeyCode.F2))
        {
            ClearAllCooldowns();
        }
    }

    [ContextMenu("Show Cooldown Status")]
    public void ShowCooldownStatus()
    {
        CompanionSelectionManager csm = FindObjectOfType<CompanionSelectionManager>();
        if (csm != null)
        {
            Debug.Log("=== COMPANION COOLDOWN STATUS ===");
            for (int i = 0; i < csm.companions.Length; i++)
            {
                if (csm.companions[i] != null)
                {
                    string status = csm.companions[i].isOnCooldown 
                        ? $"ON COOLDOWN - {csm.companions[i].currentCooldownTime / 60f:F1} minutes remaining"
                        : "READY";
                    Debug.Log($"{csm.companions[i].companionName}: {status}");
                }
            }
        }
    }

    [ContextMenu("Clear All Cooldowns")]
    public void ClearAllCooldowns()
    {
        CompanionSelectionManager csm = FindObjectOfType<CompanionSelectionManager>();
        if (csm != null)
        {
            for (int i = 0; i < csm.companions.Length; i++)
            {
                if (csm.companions[i] != null)
                {
                    csm.companions[i].currentCooldownTime = 0f;
                    csm.companions[i].isOnCooldown = false;
                }
            }
            Debug.Log("All companion cooldowns cleared!");
        }

        if (PersistentCompanionSelectionManager.Instance != null)
        {
            PersistentCompanionSelectionManager.Instance.ClearCooldownSaveData();
        }
    }

    [ContextMenu("Start Test Cooldowns")]
    public void StartTestCooldowns()
    {
        CompanionSelectionManager csm = FindObjectOfType<CompanionSelectionManager>();
        if (csm != null)
        {
            for (int i = 0; i < csm.companions.Length; i++)
            {
                if (csm.companions[i] != null)
                {
                    // Start 2 minute test cooldowns
                    csm.companions[i].currentCooldownTime = 120f; // 2 minutes
                    csm.companions[i].isOnCooldown = true;
                }
            }
            Debug.Log("Started 2-minute test cooldowns for all companions!");
        }
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("Companion Cooldown Debugger");
        GUILayout.Label("F1 - Show Status | F2 - Clear All");
        
        if (GUILayout.Button("Show Cooldown Status"))
        {
            ShowCooldownStatus();
        }
        
        if (GUILayout.Button("Clear All Cooldowns"))
        {
            ClearAllCooldowns();
        }
        
        if (GUILayout.Button("Start Test Cooldowns (2 min)"))
        {
            StartTestCooldowns();
        }
        
        GUILayout.EndArea();
    }
}