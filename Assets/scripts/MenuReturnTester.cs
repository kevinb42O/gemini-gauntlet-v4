using UnityEngine;

public class MenuReturnTester : MonoBehaviour
{
    [Header("Debug Info")]
    public bool showDebugOnStart = true;

    void Start()
    {
        if (showDebugOnStart)
        {
            Invoke("CheckMenuState", 1f);
        }
    }

    [ContextMenu("Check Menu State")]
    public void CheckMenuState()
    {
        Debug.Log("🏠 ===== MENU RETURN DEBUG =====");
        
        // Check PersistentCompanionSelectionManager
        if (PersistentCompanionSelectionManager.Instance != null)
        {
            int equippedCount = PersistentCompanionSelectionManager.Instance.GetEquippedCount();
            Debug.Log($"🏠 Equipped companions in selection panel: {equippedCount}");
            
            var selected = PersistentCompanionSelectionManager.Instance.GetSelectedCompanions();
            for (int i = 0; i < selected.Count; i++)
            {
                Debug.Log($"   Slot {i}: {selected[i].companionName} (Cooldown: {selected[i].isOnCooldown})");
            }
        }
        
        // Check CompanionSelectionManager
        CompanionSelectionManager csm = FindObjectOfType<CompanionSelectionManager>();
        if (csm != null)
        {
            Debug.Log($"🏠 CompanionSelectionManager found with {csm.companions.Length} companions");
            
            for (int i = 0; i < csm.companions.Length; i++)
            {
                if (csm.companions[i] != null)
                {
                    string status = csm.companions[i].isOnCooldown 
                        ? $"ON COOLDOWN ({csm.companions[i].currentCooldownTime / 60f:F1} min remaining)"
                        : "READY";
                    Debug.Log($"   Companion {i}: {csm.companions[i].companionName} - {status}");
                }
            }
        }
        else
        {
            Debug.Log("🏠 CompanionSelectionManager not found (normal if in game scene)");
        }
    }

    [ContextMenu("Force Clear Selection")]
    public void ForceClearSelection()
    {
        if (PersistentCompanionSelectionManager.Instance != null)
        {
            // Use reflection to access private method
            var method = typeof(PersistentCompanionSelectionManager).GetMethod("ClearSelectedCompanionsAfterSpawn", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(PersistentCompanionSelectionManager.Instance, null);
            
            Debug.Log("🏠 Forced clear of selected companions");
        }
    }
}