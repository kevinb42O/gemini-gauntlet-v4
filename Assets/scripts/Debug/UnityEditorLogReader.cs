using UnityEngine;
using System.IO;
using System.Text;

/// <summary>
/// MAXIMUM CONTROL TOOL #5: Unity Editor Log Reader
/// Automatically copies Unity's Editor.log to an easily accessible location
/// Gives Cascade access to ALL Unity console output including errors and warnings!
/// </summary>
public class UnityEditorLogReader : MonoBehaviour
{
    [Header("📋 Unity Log Reader Configuration")]
    [Tooltip("Copy log file on Start")]
    public bool copyOnStart = true;
    
    [Tooltip("Copy log file periodically")]
    public bool copyPeriodically = true;
    
    [Tooltip("Copy interval in seconds")]
    public float copyInterval = 30f;
    
    [Tooltip("Press this key to copy log immediately")]
    public KeyCode copyKey = KeyCode.F11;

    private float lastCopyTime = 0f;
    private string editorLogPath;

    void Awake()
    {
        // Determine Unity Editor log path based on platform
#if UNITY_EDITOR_WIN
        editorLogPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "Unity", "Editor", "Editor.log"
        );
#elif UNITY_EDITOR_OSX
        editorLogPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
            "Library", "Logs", "Unity", "Editor.log"
        );
#elif UNITY_EDITOR_LINUX
        editorLogPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
            ".config", "unity3d", "Editor.log"
        );
#endif

        Debug.Log($"[UnityEditorLogReader] Editor log path: {editorLogPath}");
    }

    void Start()
    {
        if (copyOnStart)
        {
            CopyEditorLog();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(copyKey))
        {
            CopyEditorLog();
        }

        if (copyPeriodically && Time.time - lastCopyTime >= copyInterval)
        {
            CopyEditorLog();
            lastCopyTime = Time.time;
        }
    }

    [ContextMenu("Copy Editor Log Now")]
    public void CopyEditorLog()
    {
        if (string.IsNullOrEmpty(editorLogPath))
        {
            Debug.LogWarning("[UnityEditorLogReader] Editor log path not set (not in editor?)");
            return;
        }

        if (!File.Exists(editorLogPath))
        {
            Debug.LogWarning($"[UnityEditorLogReader] Editor log not found at: {editorLogPath}");
            return;
        }

        try
        {
            string directory = Path.Combine(Application.dataPath, "..", "CASCADE_DEBUG_EXPORTS");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string destPath = Path.Combine(directory, $"Unity_Editor_Log_{timestamp}.txt");

            // Read the log file (Unity keeps it open, so we need to read it carefully)
            using (FileStream fs = new FileStream(editorLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(fs))
            {
                string content = reader.ReadToEnd();
                
                // Add header
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine("📋 UNITY EDITOR LOG - MAXIMUM CONTROL MODE");
                sb.AppendLine($"Copied: {System.DateTime.Now}");
                sb.AppendLine($"Source: {editorLogPath}");
                sb.AppendLine("═══════════════════════════════════════════════════════════════\n");
                sb.Append(content);

                File.WriteAllText(destPath, sb.ToString());
            }

            Debug.Log($"✅ Editor log copied to: {destPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UnityEditorLogReader] Failed to copy log: {e.Message}");
        }
    }

    void OnApplicationQuit()
    {
        // Copy log one final time when quitting
        CopyEditorLog();
    }
}
