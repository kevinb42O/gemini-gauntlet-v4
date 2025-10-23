using UnityEngine;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

/// <summary>
/// 🧠 INTELLIGENT DEBUG LOGGER - AI OPTIMIZATION ENGINE
/// Creates separate, focused debug files for each tracked script
/// Provides contextual awareness and smart value tracking
/// ULTIMATE POWER for AI-driven game optimization!
/// </summary>
public class IntelligentDebugLogger : MonoBehaviour
{
    [Header("🧠 INTELLIGENT LOGGING SYSTEM")]
    [Tooltip("Debug profile defining what to track")]
    public SmartDebugProfile debugProfile;
    
    [Tooltip("Enable intelligent logging")]
    public bool enableLogging = true;
    
    [Tooltip("Auto-find target scripts in scene")]
    public bool autoFindTargets = true;
    
    [Header("🎯 TARGET DISCOVERY")]
    [Tooltip("Search in Player GameObject")]
    public bool searchInPlayer = true;
    
    [Tooltip("Search in entire scene")]
    public bool searchInScene = false;
    
    [Tooltip("Manually assigned GameObjects to search")]
    public List<GameObject> manualTargets = new List<GameObject>();
    
    [Header("⚡ PERFORMANCE")]
    [Tooltip("Maximum tracked instances per script type")]
    public int maxInstancesPerScript = 10;
    
    [Tooltip("Enable performance profiling")]
    public bool enableProfiling = true;

    // Internal tracking
    private Dictionary<string, List<Component>> trackedComponents = new Dictionary<string, List<Component>>();
    private Dictionary<string, StreamWriter> logWriters = new Dictionary<string, StreamWriter>();
    private Dictionary<string, Dictionary<Component, FieldValueCache>> valueCaches = new Dictionary<string, Dictionary<Component, FieldValueCache>>();
    private Dictionary<string, PerformanceMetrics> performanceMetrics = new Dictionary<string, PerformanceMetrics>();
    private float nextSampleTime = 0f;
    private string exportDirectory;

    private class FieldValueCache
    {
        public Dictionary<string, object> lastValues = new Dictionary<string, object>();
        public Dictionary<string, ValueStatistics> statistics = new Dictionary<string, ValueStatistics>();
    }

    private class ValueStatistics
    {
        public float min = float.MaxValue;
        public float max = float.MinValue;
        public float sum = 0f;
        public int sampleCount = 0;
        public float average => sampleCount > 0 ? sum / sampleCount : 0f;
    }

    private class PerformanceMetrics
    {
        public float totalExecutionTime = 0f;
        public int sampleCount = 0;
        public float averageExecutionTime => sampleCount > 0 ? totalExecutionTime / sampleCount : 0f;
        public float peakExecutionTime = 0f;
    }

    void Awake()
    {
        if (debugProfile == null)
        {
            Debug.LogError("[IntelligentDebugLogger] No SmartDebugProfile assigned!");
            enableLogging = false;
            return;
        }

        if (!debugProfile.IsValid())
        {
            enableLogging = false;
            return;
        }

        InitializeExportDirectory();
        
        if (autoFindTargets)
        {
            DiscoverTargetComponents();
        }
        
        InitializeLogFiles();
    }

    void InitializeExportDirectory()
    {
        exportDirectory = Path.Combine(Application.dataPath, "..", "CASCADE_DEBUG_EXPORTS", "IntelligentLogs");
        if (!Directory.Exists(exportDirectory))
        {
            Directory.CreateDirectory(exportDirectory);
        }
    }

    void DiscoverTargetComponents()
    {
        List<string> scriptNames = debugProfile.GetTrackedScriptNames();
        
        Debug.Log($"[IntelligentDebugLogger] 🔍 Discovering components for {scriptNames.Count} script types...");

        List<GameObject> searchTargets = new List<GameObject>();
        
        // Add player
        if (searchInPlayer)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) searchTargets.Add(player);
        }
        
        // Add scene objects
        if (searchInScene)
        {
            searchTargets.AddRange(FindObjectsOfType<GameObject>());
        }
        
        // Add manual targets
        searchTargets.AddRange(manualTargets);

        // Find components
        foreach (string scriptName in scriptNames)
        {
            List<Component> foundComponents = new List<Component>();
            
            foreach (GameObject target in searchTargets)
            {
                if (target == null) continue;
                
                Component[] components = target.GetComponentsInChildren<Component>(true);
                foreach (Component comp in components)
                {
                    if (comp == null) continue;
                    
                    if (comp.GetType().Name == scriptName)
                    {
                        foundComponents.Add(comp);
                        
                        if (foundComponents.Count >= maxInstancesPerScript)
                            break;
                    }
                }
                
                if (foundComponents.Count >= maxInstancesPerScript)
                    break;
            }
            
            if (foundComponents.Count > 0)
            {
                trackedComponents[scriptName] = foundComponents;
                valueCaches[scriptName] = new Dictionary<Component, FieldValueCache>();
                performanceMetrics[scriptName] = new PerformanceMetrics();
                
                Debug.Log($"[IntelligentDebugLogger] ✅ Found {foundComponents.Count} instances of {scriptName}");
            }
            else
            {
                Debug.LogWarning($"[IntelligentDebugLogger] ⚠️ No instances found for {scriptName}");
            }
        }
    }

    void InitializeLogFiles()
    {
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        
        foreach (var kvp in trackedComponents)
        {
            string scriptName = kvp.Key;
            string filename = debugProfile.includeTimestamp 
                ? $"{scriptName}_{timestamp}.txt" 
                : $"{scriptName}.txt";
            
            string filepath = Path.Combine(exportDirectory, filename);
            StreamWriter writer = new StreamWriter(filepath, false);
            writer.AutoFlush = true;
            logWriters[scriptName] = writer;
            
            // Write header
            WriteHeader(writer, scriptName, kvp.Value.Count);
        }
        
        Debug.Log($"[IntelligentDebugLogger] 📝 Initialized {logWriters.Count} log files in: {exportDirectory}");
    }

    void WriteHeader(StreamWriter writer, string scriptName, int instanceCount)
    {
        writer.WriteLine("═══════════════════════════════════════════════════════════════");
        writer.WriteLine($"🧠 INTELLIGENT DEBUG LOG - {scriptName}");
        writer.WriteLine($"Start Time: {System.DateTime.Now}");
        writer.WriteLine($"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        writer.WriteLine($"Tracked Instances: {instanceCount}");
        writer.WriteLine($"Sampling Interval: {debugProfile.samplingInterval}s");
        writer.WriteLine($"Log Only On Change: {debugProfile.logOnlyOnChange}");
        writer.WriteLine("═══════════════════════════════════════════════════════════════\n");
    }

    void Update()
    {
        if (!enableLogging || debugProfile == null) return;
        
        if (Time.time >= nextSampleTime)
        {
            SampleAllComponents();
            nextSampleTime = Time.time + debugProfile.samplingInterval;
        }
    }

    void SampleAllComponents()
    {
        foreach (var kvp in trackedComponents)
        {
            string scriptName = kvp.Key;
            List<Component> components = kvp.Value;
            
            float startTime = enableProfiling ? Time.realtimeSinceStartup : 0f;
            
            foreach (Component comp in components)
            {
                if (comp == null) continue;
                SampleComponent(scriptName, comp);
            }
            
            if (enableProfiling)
            {
                float executionTime = (Time.realtimeSinceStartup - startTime) * 1000f; // ms
                UpdatePerformanceMetrics(scriptName, executionTime);
            }
        }
    }

    void SampleComponent(string scriptName, Component comp)
    {
        if (!valueCaches[scriptName].ContainsKey(comp))
        {
            valueCaches[scriptName][comp] = new FieldValueCache();
        }
        
        FieldValueCache cache = valueCaches[scriptName][comp];
        StringBuilder sb = new StringBuilder();
        bool hasChanges = false;
        
        System.Type type = comp.GetType();
        
        // Sample fields
        BindingFlags flags = BindingFlags.Instance;
        if (debugProfile.trackPublicFields) flags |= BindingFlags.Public;
        if (debugProfile.trackSerializedFields) flags |= BindingFlags.NonPublic;
        
        FieldInfo[] fields = type.GetFields(flags);
        
        foreach (FieldInfo field in fields)
        {
            // Check if should track this field
            if (!debugProfile.ShouldTrackField(field.Name)) continue;
            
            // Skip if private and not serialized
            if (!field.IsPublic && !System.Attribute.IsDefined(field, typeof(SerializeField)))
                continue;
            
            try
            {
                object currentValue = field.GetValue(comp);
                string fieldKey = field.Name;
                
                // Check for changes
                bool valueChanged = false;
                if (!cache.lastValues.ContainsKey(fieldKey))
                {
                    valueChanged = true;
                }
                else if (!AreValuesEqual(cache.lastValues[fieldKey], currentValue))
                {
                    valueChanged = true;
                }
                
                if (valueChanged || !debugProfile.logOnlyOnChange)
                {
                    hasChanges = true;
                    cache.lastValues[fieldKey] = CloneValue(currentValue);
                    
                    string valueStr = FormatValue(currentValue);
                    sb.AppendLine($"  {field.Name} ({field.FieldType.Name}): {valueStr}");
                    
                    // Track statistics for numeric values
                    if (debugProfile.trackStatistics && IsNumericType(field.FieldType))
                    {
                        UpdateStatistics(cache, fieldKey, currentValue);
                    }
                }
            }
            catch (System.Exception e)
            {
                sb.AppendLine($"  {field.Name}: <Error: {e.Message}>");
            }
        }
        
        // Sample properties
        if (debugProfile.trackProperties)
        {
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in properties)
            {
                if (!prop.CanRead) continue;
                if (prop.GetIndexParameters().Length > 0) continue;
                if (!debugProfile.ShouldTrackField(prop.Name)) continue;
                
                try
                {
                    object currentValue = prop.GetValue(comp);
                    string propKey = $"prop_{prop.Name}";
                    
                    bool valueChanged = false;
                    if (!cache.lastValues.ContainsKey(propKey))
                    {
                        valueChanged = true;
                    }
                    else if (!AreValuesEqual(cache.lastValues[propKey], currentValue))
                    {
                        valueChanged = true;
                    }
                    
                    if (valueChanged || !debugProfile.logOnlyOnChange)
                    {
                        hasChanges = true;
                        cache.lastValues[propKey] = CloneValue(currentValue);
                        
                        string valueStr = FormatValue(currentValue);
                        sb.AppendLine($"  {prop.Name} [Property] ({prop.PropertyType.Name}): {valueStr}");
                    }
                }
                catch { }
            }
        }
        
        // Write to log if changes detected
        if (hasChanges)
        {
            StreamWriter writer = logWriters[scriptName];
            writer.WriteLine($"[{Time.time:F2}s] [Frame {Time.frameCount}] {comp.gameObject.name}:");
            writer.Write(sb.ToString());
            
            // Add statistics summary
            if (debugProfile.trackStatistics && cache.statistics.Count > 0)
            {
                writer.WriteLine("  📊 Statistics:");
                foreach (var statKvp in cache.statistics)
                {
                    var stat = statKvp.Value;
                    writer.WriteLine($"    {statKvp.Key}: Min={stat.min:F3}, Max={stat.max:F3}, Avg={stat.average:F3}");
                }
            }
            
            writer.WriteLine();
        }
    }

    void UpdateStatistics(FieldValueCache cache, string fieldKey, object value)
    {
        if (!cache.statistics.ContainsKey(fieldKey))
        {
            cache.statistics[fieldKey] = new ValueStatistics();
        }
        
        ValueStatistics stats = cache.statistics[fieldKey];
        float numValue = ConvertToFloat(value);
        
        stats.min = Mathf.Min(stats.min, numValue);
        stats.max = Mathf.Max(stats.max, numValue);
        stats.sum += numValue;
        stats.sampleCount++;
    }

    void UpdatePerformanceMetrics(string scriptName, float executionTime)
    {
        PerformanceMetrics metrics = performanceMetrics[scriptName];
        metrics.totalExecutionTime += executionTime;
        metrics.sampleCount++;
        metrics.peakExecutionTime = Mathf.Max(metrics.peakExecutionTime, executionTime);
    }

    bool AreValuesEqual(object a, object b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        
        if (a is Vector3 v3a && b is Vector3 v3b)
            return Vector3.Distance(v3a, v3b) < 0.001f;
        
        if (a is Vector2 v2a && b is Vector2 v2b)
            return Vector2.Distance(v2a, v2b) < 0.001f;
        
        if (a is Quaternion qa && b is Quaternion qb)
            return Quaternion.Angle(qa, qb) < 0.1f;
        
        if (a is float fa && b is float fb)
            return Mathf.Abs(fa - fb) < 0.001f;
        
        return a.Equals(b);
    }

    object CloneValue(object value)
    {
        if (value == null) return null;
        if (value is Vector3 v3) return new Vector3(v3.x, v3.y, v3.z);
        if (value is Vector2 v2) return new Vector2(v2.x, v2.y);
        if (value is Quaternion q) return new Quaternion(q.x, q.y, q.z, q.w);
        return value;
    }

    bool IsNumericType(System.Type type)
    {
        return type == typeof(int) || type == typeof(float) || type == typeof(double) || 
               type == typeof(long) || type == typeof(short) || type == typeof(byte);
    }

    float ConvertToFloat(object value)
    {
        if (value is int i) return i;
        if (value is float f) return f;
        if (value is double d) return (float)d;
        if (value is long l) return l;
        if (value is short s) return s;
        if (value is byte b) return b;
        return 0f;
    }

    string FormatValue(object value)
    {
        if (value == null) return "null";
        
        if (value is UnityEngine.Object unityObj)
        {
            if (unityObj == null) return "null";
            return $"{unityObj.GetType().Name}: \"{unityObj.name}\"";
        }
        
        if (value is Vector3 v3) return $"({v3.x:F3}, {v3.y:F3}, {v3.z:F3})";
        if (value is Vector2 v2) return $"({v2.x:F3}, {v2.y:F3})";
        if (value is Quaternion q) return $"Euler({q.eulerAngles.x:F1}, {q.eulerAngles.y:F1}, {q.eulerAngles.z:F1})";
        if (value is Color c) return $"RGBA({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})";
        if (value is float f) return f.ToString("F3");
        if (value is double d) return d.ToString("F3");
        
        if (value is System.Collections.IList list)
        {
            return $"[{list.Count} items]";
        }
        
        if (value is System.Enum)
        {
            return $"{value.GetType().Name}.{value}";
        }
        
        return value.ToString();
    }

    void OnDestroy()
    {
        CloseAllLogFiles();
    }

    void OnApplicationQuit()
    {
        CloseAllLogFiles();
    }

    void CloseAllLogFiles()
    {
        foreach (var kvp in logWriters)
        {
            StreamWriter writer = kvp.Value;
            string scriptName = kvp.Key;
            
            writer.WriteLine("\n═══════════════════════════════════════════════════════════════");
            writer.WriteLine($"🏁 Logging Ended: {System.DateTime.Now}");
            writer.WriteLine($"Total Samples: {(performanceMetrics.ContainsKey(scriptName) ? performanceMetrics[scriptName].sampleCount : 0)}");
            
            if (enableProfiling && performanceMetrics.ContainsKey(scriptName))
            {
                var metrics = performanceMetrics[scriptName];
                writer.WriteLine($"📊 Performance Metrics:");
                writer.WriteLine($"  Average Execution Time: {metrics.averageExecutionTime:F3}ms");
                writer.WriteLine($"  Peak Execution Time: {metrics.peakExecutionTime:F3}ms");
            }
            
            writer.WriteLine("═══════════════════════════════════════════════════════════════");
            writer.Close();
        }
        
        logWriters.Clear();
        Debug.Log($"[IntelligentDebugLogger] ✅ All log files closed and saved to: {exportDirectory}");
    }

    [ContextMenu("Force Sample Now")]
    public void ForceSample()
    {
        SampleAllComponents();
    }

    [ContextMenu("Open Export Folder")]
    public void OpenExportFolder()
    {
        if (Directory.Exists(exportDirectory))
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.RevealInFinder(exportDirectory);
#else
            System.Diagnostics.Process.Start(exportDirectory);
#endif
        }
    }
}
