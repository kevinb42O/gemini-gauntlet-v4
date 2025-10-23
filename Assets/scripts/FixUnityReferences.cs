using UnityEngine;
using UnityEditor;
using System.IO;

#if UNITY_EDITOR
public class FixUnityReferences : EditorWindow
{
    [MenuItem("Fix Scripts/Reconnect Missing Scripts")]
    public static void ShowWindow()
    {
        GetWindow<FixUnityReferences>("Fix Missing Scripts");
    }

    void OnGUI()
    {
        GUILayout.Label("Fix Missing Script References", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Force Reimport All Scripts"))
        {
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset("Assets/scripts", ImportAssetOptions.ImportRecursive);
            Debug.Log("Reimported all scripts");
        }
        
        if (GUILayout.Button("Fix Missing References"))
        {
            FixMissingScripts();
        }
        
        if (GUILayout.Button("Delete Library & Force Refresh"))
        {
            if (EditorUtility.DisplayDialog("Warning", 
                "This will delete the Library folder and force Unity to reimport everything. This may take several minutes. Continue?", 
                "Yes", "Cancel"))
            {
                string libraryPath = Path.Combine(Application.dataPath, "../Library");
                if (Directory.Exists(libraryPath))
                {
                    Directory.Delete(libraryPath, true);
                    AssetDatabase.Refresh();
                    Debug.Log("Library deleted - Unity will reimport everything");
                }
            }
        }
    }
    
    void FixMissingScripts()
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        int fixedCount = 0;
        
        foreach (GameObject go in allObjects)
        {
            Component[] components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    Debug.LogWarning($"Missing script on {go.name}", go);
                    fixedCount++;
                }
            }
        }
        
        AssetDatabase.Refresh();
        Debug.Log($"Attempted to fix {fixedCount} missing script references");
    }
}
#endif
