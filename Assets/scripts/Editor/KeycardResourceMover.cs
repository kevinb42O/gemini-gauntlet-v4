using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor utility to move keycard assets to Resources folder for persistence
/// This is required because Unity's Resources.Load() only works with assets in Resources folders
/// </summary>
public class KeycardResourceMover : EditorWindow
{
    private const string SOURCE_PATH = "Assets/prefabs_made/KEYCARDS/Keycards";
    private const string DEST_PATH = "Assets/Resources/Keycards";
    
    [MenuItem("Tools/Keycard Setup/Move Keycards to Resources Folder")]
    public static void ShowWindow()
    {
        GetWindow<KeycardResourceMover>("Keycard Resource Mover");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Keycard Persistence Setup", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Keycards must be in a Resources folder to persist across game sessions.\n\n" +
            "This tool will COPY (not move) keycard assets from:\n" +
            $"'{SOURCE_PATH}'\n\n" +
            "To:\n" +
            $"'{DEST_PATH}'\n\n" +
            "Original files will remain intact.",
            MessageType.Info
        );
        
        EditorGUILayout.Space();
        
        // Check if source folder exists
        if (!Directory.Exists(SOURCE_PATH))
        {
            EditorGUILayout.HelpBox($"Source folder not found: {SOURCE_PATH}", MessageType.Error);
            return;
        }
        
        // Check if destination folder exists
        bool destExists = Directory.Exists(DEST_PATH);
        if (destExists)
        {
            EditorGUILayout.HelpBox($"Destination folder already exists: {DEST_PATH}", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox($"Destination folder will be created: {DEST_PATH}", MessageType.Info);
        }
        
        EditorGUILayout.Space();
        
        // Show keycard files that will be copied
        string[] keycardFiles = Directory.GetFiles(SOURCE_PATH, "*.asset");
        EditorGUILayout.LabelField($"Found {keycardFiles.Length} keycard asset(s) to copy:", EditorStyles.boldLabel);
        
        foreach (string file in keycardFiles)
        {
            string fileName = Path.GetFileName(file);
            EditorGUILayout.LabelField($"  • {fileName}");
        }
        
        EditorGUILayout.Space();
        
        // Copy button
        if (GUILayout.Button("Copy Keycards to Resources Folder", GUILayout.Height(40)))
        {
            CopyKeycardsToResources();
        }
        
        EditorGUILayout.Space();
        
        // Manual instructions
        EditorGUILayout.HelpBox(
            "ALTERNATIVE: Manual Setup\n\n" +
            "1. Create folder: Assets/Resources/Keycards/\n" +
            "2. Copy all .asset files from Assets/prefabs_made/KEYCARDS/Keycards/\n" +
            "3. Paste into Assets/Resources/Keycards/\n" +
            "4. Unity will automatically update references",
            MessageType.None
        );
    }
    
    private void CopyKeycardsToResources()
    {
        try
        {
            // Create destination folder if it doesn't exist
            if (!Directory.Exists(DEST_PATH))
            {
                Directory.CreateDirectory(DEST_PATH);
                Debug.Log($"[KeycardResourceMover] Created directory: {DEST_PATH}");
            }
            
            // Get all keycard asset files
            string[] keycardFiles = Directory.GetFiles(SOURCE_PATH, "*.asset");
            
            if (keycardFiles.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Keycards Found",
                    $"No .asset files found in {SOURCE_PATH}",
                    "OK"
                );
                return;
            }
            
            int copiedCount = 0;
            int skippedCount = 0;
            
            foreach (string sourceFile in keycardFiles)
            {
                string fileName = Path.GetFileName(sourceFile);
                string destFile = Path.Combine(DEST_PATH, fileName);
                
                // Check if file already exists
                if (File.Exists(destFile))
                {
                    Debug.LogWarning($"[KeycardResourceMover] File already exists, skipping: {fileName}");
                    skippedCount++;
                    continue;
                }
                
                // Copy the file using Unity's AssetDatabase for proper handling
                string sourceAssetPath = sourceFile.Replace("\\", "/");
                string destAssetPath = destFile.Replace("\\", "/");
                
                bool success = AssetDatabase.CopyAsset(sourceAssetPath, destAssetPath);
                
                if (success)
                {
                    Debug.Log($"[KeycardResourceMover] ✅ Copied: {fileName}");
                    copiedCount++;
                }
                else
                {
                    Debug.LogError($"[KeycardResourceMover] ❌ Failed to copy: {fileName}");
                }
            }
            
            // Refresh AssetDatabase
            AssetDatabase.Refresh();
            
            // Show result dialog
            string message = $"Keycard Setup Complete!\n\n" +
                           $"Copied: {copiedCount} file(s)\n" +
                           $"Skipped: {skippedCount} file(s)\n\n" +
                           $"Keycards will now persist across game sessions!";
            
            EditorUtility.DisplayDialog(
                "Success",
                message,
                "OK"
            );
            
            Debug.Log($"[KeycardResourceMover] ✅ SETUP COMPLETE: {copiedCount} keycards copied to Resources folder");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog(
                "Error",
                $"Failed to copy keycards:\n{e.Message}",
                "OK"
            );
            
            Debug.LogError($"[KeycardResourceMover] Error: {e.Message}\n{e.StackTrace}");
        }
    }
}
