using UnityEngine;
using System.IO;
using System.Text;

/// <summary>
/// MAXIMUM CONTROL TOOL #4: Complete Scene Hierarchy Exporter
/// Exports the entire scene hierarchy with GameObject relationships
/// Perfect for understanding scene structure and finding objects!
/// </summary>
public class SceneHierarchyExporter : MonoBehaviour
{
    [Header("🌍 Scene Hierarchy Exporter")]
    [Tooltip("Export on Start")]
    public bool exportOnStart = false;
    
    [Tooltip("Press this key to export at runtime")]
    public KeyCode exportKey = KeyCode.F10;
    
    [Tooltip("Include inactive GameObjects")]
    public bool includeInactive = true;
    
    [Tooltip("Include component counts")]
    public bool includeComponentCounts = true;
    
    [Tooltip("Include tags and layers")]
    public bool includeTagsAndLayers = true;

    void Start()
    {
        if (exportOnStart)
        {
            ExportSceneHierarchy();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(exportKey))
        {
            ExportSceneHierarchy();
        }
    }

    [ContextMenu("Export Scene Hierarchy")]
    public void ExportSceneHierarchy()
    {
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("🌍 COMPLETE SCENE HIERARCHY - MAXIMUM CONTROL MODE");
        sb.AppendLine($"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        sb.AppendLine($"Time: {System.DateTime.Now}");
        sb.AppendLine("═══════════════════════════════════════════════════════════════\n");

        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        sb.AppendLine($"Root GameObjects: {rootObjects.Length}\n");

        foreach (GameObject root in rootObjects)
        {
            ExportGameObjectRecursive(root, sb, 0);
        }

        SaveToFile(sb.ToString());
    }

    void ExportGameObjectRecursive(GameObject obj, StringBuilder sb, int depth)
    {
        if (!includeInactive && !obj.activeSelf)
            return;

        string indent = new string('│', depth);
        string branch = depth > 0 ? "├─ " : "";
        
        // GameObject info
        string activeIcon = obj.activeSelf ? "✓" : "✗";
        string name = $"{activeIcon} {obj.name}";
        
        if (includeTagsAndLayers)
        {
            name += $" [Tag: {obj.tag}, Layer: {LayerMask.LayerToName(obj.layer)}]";
        }
        
        if (includeComponentCounts)
        {
            Component[] components = obj.GetComponents<Component>();
            name += $" ({components.Length} components)";
        }

        sb.AppendLine($"{indent}{branch}{name}");

        // Export children
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            ExportGameObjectRecursive(obj.transform.GetChild(i).gameObject, sb, depth + 1);
        }
    }

    void SaveToFile(string content)
    {
        string directory = Path.Combine(Application.dataPath, "..", "CASCADE_DEBUG_EXPORTS");
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filepath = Path.Combine(directory, $"SceneHierarchy_{sceneName}_{timestamp}.txt");
        
        File.WriteAllText(filepath, content);
        
        Debug.Log($"✅ Scene Hierarchy Exported!\nSaved to: {filepath}");
    }
}
