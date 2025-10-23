using UnityEngine;
using System.IO;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// MAXIMUM CONTROL TOOL #2: Real-Time Animation State Logger
/// Logs ALL animation states, transitions, layer weights, and parameters to a file in REAL-TIME
/// This gives Cascade LIVE VISIBILITY into animation system behavior!
/// </summary>
public class RuntimeAnimationLogger : MonoBehaviour
{
    [Header("🎬 Animation Logging Configuration")]
    [Tooltip("Enable/disable logging at runtime")]
    public bool enableLogging = true;
    
    [Tooltip("Log every N frames (1 = every frame, 10 = every 10 frames)")]
    public int logFrequency = 5;
    
    [Tooltip("Log only when states change")]
    public bool logOnlyOnChange = true;
    
    [Tooltip("Include parameter values")]
    public bool logParameters = true;
    
    [Tooltip("Include layer weights")]
    public bool logLayerWeights = true;
    
    [Tooltip("Include transition info")]
    public bool logTransitions = true;

    [Header("Target Animators")]
    [Tooltip("Auto-find all animators on Player")]
    public bool autoFindPlayerAnimators = true;
    
    [Tooltip("Manually assign specific animators to track")]
    public Animator[] targetAnimators;

    private StreamWriter logWriter;
    private string logFilePath;
    private int frameCounter = 0;
    private Dictionary<Animator, AnimatorStateCache> stateCache = new Dictionary<Animator, AnimatorStateCache>();

    private class AnimatorStateCache
    {
        public Dictionary<int, int> lastStateHashes = new Dictionary<int, int>();
        public Dictionary<string, object> lastParameterValues = new Dictionary<string, object>();
        public Dictionary<int, float> lastLayerWeights = new Dictionary<int, float>();
    }

    void Awake()
    {
        if (autoFindPlayerAnimators)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                targetAnimators = player.GetComponentsInChildren<Animator>();
                Debug.Log($"[RuntimeAnimationLogger] Auto-found {targetAnimators.Length} animators on Player");
            }
        }

        InitializeLogFile();
    }

    void InitializeLogFile()
    {
        string directory = Path.Combine(Application.dataPath, "..", "CASCADE_DEBUG_EXPORTS");
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        logFilePath = Path.Combine(directory, $"Animation_Runtime_Log_{timestamp}.txt");
        
        logWriter = new StreamWriter(logFilePath, false);
        logWriter.AutoFlush = true;

        WriteLog("═══════════════════════════════════════════════════════════════");
        WriteLog("🎬 RUNTIME ANIMATION LOGGER - MAXIMUM CONTROL MODE");
        WriteLog($"Start Time: {System.DateTime.Now}");
        WriteLog($"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        WriteLog($"Tracking {targetAnimators?.Length ?? 0} Animators");
        WriteLog("═══════════════════════════════════════════════════════════════\n");

        Debug.Log($"✅ Animation logging started: {logFilePath}");
    }

    void Update()
    {
        if (!enableLogging || targetAnimators == null || targetAnimators.Length == 0)
            return;

        frameCounter++;
        if (frameCounter % logFrequency != 0)
            return;

        foreach (Animator animator in targetAnimators)
        {
            if (animator == null) continue;
            LogAnimatorState(animator);
        }
    }

    void LogAnimatorState(Animator animator)
    {
        if (!stateCache.ContainsKey(animator))
        {
            stateCache[animator] = new AnimatorStateCache();
        }

        AnimatorStateCache cache = stateCache[animator];
        bool hasChanges = false;
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"[Frame {Time.frameCount}] [{Time.time:F2}s] Animator: {animator.gameObject.name}");

        // Log layer states
        for (int i = 0; i < animator.layerCount; i++)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(i);
            int currentHash = stateInfo.shortNameHash;

            // Check if state changed
            if (!cache.lastStateHashes.ContainsKey(i) || cache.lastStateHashes[i] != currentHash)
            {
                hasChanges = true;
                cache.lastStateHashes[i] = currentHash;
            }

            sb.AppendLine($"  Layer {i} ({animator.GetLayerName(i)}):");
            sb.AppendLine($"    State Hash: {currentHash}");
            sb.AppendLine($"    Normalized Time: {stateInfo.normalizedTime:F3}");
            sb.AppendLine($"    Length: {stateInfo.length:F3}s");
            sb.AppendLine($"    Speed: {stateInfo.speed:F2}x");
            sb.AppendLine($"    Loop: {stateInfo.loop}");

            // Log layer weight
            if (logLayerWeights)
            {
                float weight = animator.GetLayerWeight(i);
                if (!cache.lastLayerWeights.ContainsKey(i) || Mathf.Abs(cache.lastLayerWeights[i] - weight) > 0.01f)
                {
                    hasChanges = true;
                    cache.lastLayerWeights[i] = weight;
                }
                sb.AppendLine($"    Weight: {weight:F3}");
            }

            // Log transitions
            if (logTransitions && animator.IsInTransition(i))
            {
                AnimatorTransitionInfo transInfo = animator.GetAnimatorTransitionInfo(i);
                sb.AppendLine($"    🔄 IN TRANSITION:");
                sb.AppendLine($"      Progress: {transInfo.normalizedTime:F3}");
                sb.AppendLine($"      Duration: {transInfo.duration:F3}s");
                hasChanges = true;
            }
        }

        // Log parameters
        if (logParameters)
        {
            sb.AppendLine("  Parameters:");
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                object currentValue = null;
                string valueStr = "";

                switch (param.type)
                {
                    case AnimatorControllerParameterType.Float:
                        currentValue = animator.GetFloat(param.name);
                        valueStr = $"{currentValue:F3}";
                        break;
                    case AnimatorControllerParameterType.Int:
                        currentValue = animator.GetInteger(param.name);
                        valueStr = currentValue.ToString();
                        break;
                    case AnimatorControllerParameterType.Bool:
                        currentValue = animator.GetBool(param.name);
                        valueStr = currentValue.ToString();
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        valueStr = "<Trigger>";
                        break;
                }

                // Check if parameter changed
                if (currentValue != null)
                {
                    if (!cache.lastParameterValues.ContainsKey(param.name) || 
                        !cache.lastParameterValues[param.name].Equals(currentValue))
                    {
                        hasChanges = true;
                        cache.lastParameterValues[param.name] = currentValue;
                        sb.AppendLine($"    {param.name} ({param.type}): {valueStr} ⚡CHANGED");
                    }
                    else
                    {
                        sb.AppendLine($"    {param.name} ({param.type}): {valueStr}");
                    }
                }
            }
        }

        // Only write if changes detected (or if logOnlyOnChange is false)
        if (!logOnlyOnChange || hasChanges)
        {
            WriteLog(sb.ToString());
        }
    }

    void WriteLog(string message)
    {
        if (logWriter != null)
        {
            logWriter.WriteLine(message);
        }
    }

    void OnDestroy()
    {
        if (logWriter != null)
        {
            WriteLog("\n═══════════════════════════════════════════════════════════════");
            WriteLog($"🏁 Logging Ended: {System.DateTime.Now}");
            WriteLog($"Total Frames Logged: {frameCounter}");
            WriteLog("═══════════════════════════════════════════════════════════════");
            
            logWriter.Close();
            Debug.Log($"✅ Animation log saved: {logFilePath}");
        }
    }

    void OnApplicationQuit()
    {
        OnDestroy();
    }
}
