using UnityEngine;
using System.IO;

/// <summary>
/// 🧠 CASCADE MAXIMUM CONTROL MANAGER - ULTIMATE AI INTELLIGENCE
/// Single component that manages ALL Cascade debug tools!
/// Now with INTELLIGENT TRACKING for AI-driven optimization!
/// Add this ONE component and get ULTIMATE POWER!
/// </summary>
public class CascadeDebugManager : MonoBehaviour
{
    [Header("🚀 CASCADE MAXIMUM CONTROL MANAGER")]
    [Tooltip("Enable all debug tools")]
    public bool enableAllTools = true;

    [Header("🧠 INTELLIGENT TRACKING (NEW!)")]
    [Tooltip("Smart Debug Profile - drag scripts to track")]
    public SmartDebugProfile intelligentProfile;
    
    [Tooltip("Enable intelligent per-script logging")]
    public bool enableIntelligentLogging = true;
    
    [Tooltip("Enable contextual value tracking")]
    public bool enableContextualTracking = true;
    
    [Tooltip("Enable AI optimization reporting")]
    public bool enableAIReporting = true;

    [Header("📦 Classic Tool Components")]
    [Tooltip("Auto-add RuntimeAnimationLogger")]
    public bool addAnimationLogger = true;
    
    [Tooltip("Auto-add ComponentConfigDumper")]
    public bool addComponentDumper = true;
    
    [Tooltip("Auto-add SceneHierarchyExporter")]
    public bool addSceneExporter = true;
    
    [Tooltip("Auto-add UnityEditorLogReader")]
    public bool addLogReader = true;

    [Header("⚡ Quick Actions")]
    [Tooltip("Export everything on Start")]
    public bool exportOnStart = true;
    
    [Tooltip("Press this to export EVERYTHING instantly")]
    public KeyCode exportAllKey = KeyCode.F12;

    // Classic tools
    private RuntimeAnimationLogger animLogger;
    private ComponentConfigDumper compDumper;
    private SceneHierarchyExporter sceneExporter;
    private UnityEditorLogReader logReader;
    
    // Intelligent tools
    private IntelligentDebugLogger intelligentLogger;
    private ContextualInspectorTracker contextualTracker;
    private AIOptimizationReporter aiReporter;

    void Awake()
    {
        if (!enableAllTools) return;

        Debug.Log("═══════════════════════════════════════════════════════════════");
        Debug.Log("🧠 CASCADE MAXIMUM CONTROL MODE - ULTIMATE AI INTELLIGENCE!");
        Debug.Log("═══════════════════════════════════════════════════════════════");

        // Add components if they don't exist
        if (addAnimationLogger && GetComponent<RuntimeAnimationLogger>() == null)
        {
            animLogger = gameObject.AddComponent<RuntimeAnimationLogger>();
            animLogger.enableLogging = true;
            animLogger.logFrequency = 5;
            animLogger.logOnlyOnChange = true;
            animLogger.autoFindPlayerAnimators = true;
            Debug.Log("✅ RuntimeAnimationLogger added and configured");
        }

        if (addComponentDumper && GetComponent<ComponentConfigDumper>() == null)
        {
            compDumper = gameObject.AddComponent<ComponentConfigDumper>();
            compDumper.dumpOnStart = false; // We'll trigger it manually
            Debug.Log("✅ ComponentConfigDumper added and configured");
        }

        if (addSceneExporter && GetComponent<SceneHierarchyExporter>() == null)
        {
            sceneExporter = gameObject.AddComponent<SceneHierarchyExporter>();
            sceneExporter.exportOnStart = false; // We'll trigger it manually
            Debug.Log("✅ SceneHierarchyExporter added and configured");
        }

        if (addLogReader && GetComponent<UnityEditorLogReader>() == null)
        {
            logReader = gameObject.AddComponent<UnityEditorLogReader>();
            logReader.copyOnStart = true;
            logReader.copyPeriodically = true;
            logReader.copyInterval = 30f;
            Debug.Log("✅ UnityEditorLogReader added and configured");
        }

        // Get existing components if already added
        if (animLogger == null) animLogger = GetComponent<RuntimeAnimationLogger>();
        if (compDumper == null) compDumper = GetComponent<ComponentConfigDumper>();
        if (sceneExporter == null) sceneExporter = GetComponent<SceneHierarchyExporter>();
        if (logReader == null) logReader = GetComponent<UnityEditorLogReader>();

        // Add intelligent tracking components
        if (intelligentProfile != null)
        {
            if (enableIntelligentLogging && GetComponent<IntelligentDebugLogger>() == null)
            {
                intelligentLogger = gameObject.AddComponent<IntelligentDebugLogger>();
                intelligentLogger.debugProfile = intelligentProfile;
                intelligentLogger.enableLogging = true;
                intelligentLogger.autoFindTargets = true;
                intelligentLogger.searchInPlayer = true;
                Debug.Log("✅ IntelligentDebugLogger added - per-script tracking enabled!");
            }
            
            if (enableContextualTracking && GetComponent<ContextualInspectorTracker>() == null)
            {
                contextualTracker = gameObject.AddComponent<ContextualInspectorTracker>();
                contextualTracker.debugProfile = intelligentProfile;
                contextualTracker.enableTracking = true;
                contextualTracker.detectPatterns = true;
                contextualTracker.detectAnomalies = true;
                Debug.Log("✅ ContextualInspectorTracker added - smart value tracking enabled!");
            }
            
            if (enableAIReporting && GetComponent<AIOptimizationReporter>() == null)
            {
                aiReporter = gameObject.AddComponent<AIOptimizationReporter>();
                aiReporter.debugProfile = intelligentProfile;
                aiReporter.enableReporting = true;
                aiReporter.reportInterval = 30f;
                aiReporter.analyzePerformance = true;
                aiReporter.analyzeMemory = true;
                Debug.Log("✅ AIOptimizationReporter added - AI-driven optimization enabled!");
            }
            
            // Get existing intelligent components
            if (intelligentLogger == null) intelligentLogger = GetComponent<IntelligentDebugLogger>();
            if (contextualTracker == null) contextualTracker = GetComponent<ContextualInspectorTracker>();
            if (aiReporter == null) aiReporter = GetComponent<AIOptimizationReporter>();
        }
        else
        {
            Debug.LogWarning("⚠️ No SmartDebugProfile assigned! Intelligent tracking disabled.");
            Debug.LogWarning("   Create one: Right-click > Create > Cascade Debug > Smart Debug Profile");
        }

        Debug.Log("═══════════════════════════════════════════════════════════════");
        Debug.Log($"📂 All exports will be saved to: CASCADE_DEBUG_EXPORTS/");
        Debug.Log($"🧠 Intelligent logs: CASCADE_DEBUG_EXPORTS/IntelligentLogs/");
        Debug.Log($"🎯 Context tracking: CASCADE_DEBUG_EXPORTS/ContextualTracking/");
        Debug.Log($"🤖 AI reports: CASCADE_DEBUG_EXPORTS/OptimizationReports/");
        Debug.Log($"⚡ Press {exportAllKey} to export EVERYTHING instantly!");
        Debug.Log("═══════════════════════════════════════════════════════════════");
    }

    void Start()
    {
        if (exportOnStart && enableAllTools)
        {
            ExportEverything();
        }
    }

    void Update()
    {
        if (enableAllTools && Input.GetKeyDown(exportAllKey))
        {
            ExportEverything();
        }
    }

    [ContextMenu("Export Everything Now")]
    public void ExportEverything()
    {
        Debug.Log("🚀 EXPORTING EVERYTHING - MAXIMUM CONTROL MODE!");

        // Component configuration
        if (compDumper != null)
        {
            compDumper.DumpConfiguration();
            Debug.Log("✅ Component configuration dumped");
        }

        // Scene hierarchy
        if (sceneExporter != null)
        {
            sceneExporter.ExportSceneHierarchy();
            Debug.Log("✅ Scene hierarchy exported");
        }

        // Unity log
        if (logReader != null)
        {
            logReader.CopyEditorLog();
            Debug.Log("✅ Unity Editor log copied");
        }

        // Animation logging is continuous, so just confirm it's running
        if (animLogger != null && animLogger.enableLogging)
        {
            Debug.Log("✅ Animation logging is active (continuous)");
        }
        
        // Intelligent logging is continuous
        if (intelligentLogger != null && intelligentLogger.enableLogging)
        {
            Debug.Log("✅ Intelligent per-script logging is active (continuous)");
        }
        
        // Contextual tracking is continuous
        if (contextualTracker != null && contextualTracker.enableTracking)
        {
            Debug.Log("✅ Contextual value tracking is active (continuous)");
        }
        
        // Generate AI optimization report
        if (aiReporter != null && aiReporter.enableReporting)
        {
            aiReporter.GenerateOptimizationReport();
            Debug.Log("✅ AI Optimization Report generated");
        }

        Debug.Log("═══════════════════════════════════════════════════════════════");
        Debug.Log("🎉 COMPLETE! All data exported to CASCADE_DEBUG_EXPORTS/");
        Debug.Log("🧠 Check IntelligentLogs/ for per-script tracking!");
        Debug.Log("🎯 Check ContextualTracking/ for value pattern analysis!");
        Debug.Log("🤖 Check OptimizationReports/ for AI insights!");
        Debug.Log("═══════════════════════════════════════════════════════════════");

        // Open the folder
        string directory = Path.Combine(Application.dataPath, "..", "CASCADE_DEBUG_EXPORTS");
        if (Directory.Exists(directory))
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.RevealInFinder(directory);
#else
            System.Diagnostics.Process.Start(directory);
#endif
        }
    }

    void OnGUI()
    {
        if (!enableAllTools) return;

        // Draw a small UI in the corner
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 12;
        style.normal.textColor = Color.green;
        style.alignment = TextAnchor.MiddleLeft;

        int height = 180;
        if (intelligentProfile != null) height = 240;
        
        GUILayout.BeginArea(new Rect(10, 10, 350, height));
        GUILayout.Box("🧠 CASCADE ULTIMATE AI CONTROL", style);
        GUILayout.Label($"Press {exportAllKey} to export all data");
        GUILayout.Label($"Exports: CASCADE_DEBUG_EXPORTS/");
        
        if (animLogger != null && animLogger.enableLogging)
        {
            GUILayout.Label("🎬 Animation logging: ACTIVE");
        }
        
        if (intelligentProfile != null)
        {
            GUILayout.Label("───────────────────────────────");
            if (intelligentLogger != null && intelligentLogger.enableLogging)
            {
                GUILayout.Label("🧠 Intelligent tracking: ACTIVE");
            }
            if (contextualTracker != null && contextualTracker.enableTracking)
            {
                GUILayout.Label("🎯 Context analysis: ACTIVE");
            }
            if (aiReporter != null && aiReporter.enableReporting)
            {
                GUILayout.Label("🤖 AI optimization: ACTIVE");
            }
        }
        
        GUILayout.EndArea();
    }

    [ContextMenu("Open Export Folder")]
    public void OpenExportFolder()
    {
        string directory = Path.Combine(Application.dataPath, "..", "CASCADE_DEBUG_EXPORTS");
        if (Directory.Exists(directory))
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.RevealInFinder(directory);
#else
            System.Diagnostics.Process.Start(directory);
#endif
            Debug.Log($"📂 Opened: {directory}");
        }
        else
        {
            Debug.LogWarning("CASCADE_DEBUG_EXPORTS folder doesn't exist yet. Run the game first!");
        }
    }

    [ContextMenu("Disable All Tools")]
    public void DisableAllTools()
    {
        enableAllTools = false;
        
        if (animLogger != null) animLogger.enableLogging = false;
        
        Debug.Log("⏸️ All Cascade debug tools disabled");
    }

    [ContextMenu("Enable All Tools")]
    public void EnableAllTools()
    {
        enableAllTools = true;
        
        if (animLogger != null) animLogger.enableLogging = true;
        
        Debug.Log("▶️ All Cascade debug tools enabled");
    }
}
