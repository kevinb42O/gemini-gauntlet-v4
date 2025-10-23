using UnityEngine;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// 🎯 CONTEXTUAL INSPECTOR TRACKER - REAL-TIME VALUE MONITORING
/// Tracks Inspector values in real-time with contextual awareness
/// Detects patterns, anomalies, and optimization opportunities
/// Provides AI with deep understanding of runtime behavior
/// </summary>
public class ContextualInspectorTracker : MonoBehaviour
{
    [Header("🎯 CONTEXTUAL TRACKING")]
    [Tooltip("Debug profile to use")]
    public SmartDebugProfile debugProfile;
    
    [Tooltip("Enable contextual tracking")]
    public bool enableTracking = true;
    
    [Header("🔍 CONTEXT DETECTION")]
    [Tooltip("Detect value patterns (oscillation, drift, spikes)")]
    public bool detectPatterns = true;
    
    [Tooltip("Detect anomalies (unexpected values)")]
    public bool detectAnomalies = true;
    
    [Tooltip("Track value correlations between fields")]
    public bool trackCorrelations = true;
    
    [Header("⚠️ ALERT THRESHOLDS")]
    [Tooltip("Alert on rapid value changes")]
    public bool alertOnRapidChanges = true;
    
    [Tooltip("Change rate threshold (changes per second)")]
    public float rapidChangeThreshold = 10f;
    
    [Tooltip("Alert on stuck values (no change for N seconds)")]
    public bool alertOnStuckValues = true;
    
    [Tooltip("Stuck value timeout (seconds)")]
    public float stuckValueTimeout = 5f;
    
    [Header("📊 ANALYSIS")]
    [Tooltip("Generate optimization suggestions")]
    public bool generateSuggestions = true;
    
    [Tooltip("Track frame-by-frame changes")]
    public bool trackFrameByFrame = false;

    // Internal tracking
    private Dictionary<string, ComponentContext> componentContexts = new Dictionary<string, ComponentContext>();
    private StreamWriter contextWriter;
    private string contextFilePath;
    private float lastAnalysisTime = 0f;
    private const float ANALYSIS_INTERVAL = 2f;

    private class ComponentContext
    {
        public Component component;
        public Dictionary<string, FieldContext> fieldContexts = new Dictionary<string, FieldContext>();
        public float lastUpdateTime;
    }

    private class FieldContext
    {
        public string fieldName;
        public System.Type fieldType;
        public List<object> valueHistory = new List<object>();
        public List<float> changeTimestamps = new List<float>();
        public float lastChangeTime;
        public int changeCount;
        public object lastValue;
        public bool isPotentiallyStuck;
        public bool isOscillating;
        public bool hasAnomalies;
        public string detectedPattern;
        public List<string> suggestions = new List<string>();
    }

    void Awake()
    {
        if (debugProfile == null || !debugProfile.IsValid())
        {
            enableTracking = false;
            return;
        }

        InitializeContextTracking();
        DiscoverComponents();
    }

    void InitializeContextTracking()
    {
        string directory = Path.Combine(Application.dataPath, "..", "CASCADE_DEBUG_EXPORTS", "ContextualTracking");
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        contextFilePath = Path.Combine(directory, $"ContextualAnalysis_{timestamp}.txt");
        
        contextWriter = new StreamWriter(contextFilePath, false);
        contextWriter.AutoFlush = true;

        contextWriter.WriteLine("═══════════════════════════════════════════════════════════════");
        contextWriter.WriteLine("🎯 CONTEXTUAL INSPECTOR TRACKER - AI OPTIMIZATION ENGINE");
        contextWriter.WriteLine($"Start Time: {System.DateTime.Now}");
        contextWriter.WriteLine($"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        contextWriter.WriteLine("═══════════════════════════════════════════════════════════════\n");
        
        Debug.Log($"[ContextualInspectorTracker] 📝 Context tracking initialized: {contextFilePath}");
    }

    void DiscoverComponents()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        List<string> scriptNames = debugProfile.GetTrackedScriptNames();
        
        foreach (string scriptName in scriptNames)
        {
            Component[] components = player.GetComponentsInChildren<Component>(true);
            foreach (Component comp in components)
            {
                if (comp == null) continue;
                if (comp.GetType().Name == scriptName)
                {
                    string contextKey = $"{scriptName}_{comp.GetInstanceID()}";
                    componentContexts[contextKey] = new ComponentContext
                    {
                        component = comp,
                        lastUpdateTime = Time.time
                    };
                }
            }
        }
        
        Debug.Log($"[ContextualInspectorTracker] 🔍 Tracking {componentContexts.Count} component instances");
    }

    void Update()
    {
        if (!enableTracking) return;

        // Track values
        foreach (var kvp in componentContexts)
        {
            TrackComponentContext(kvp.Value);
        }

        // Periodic analysis
        if (Time.time - lastAnalysisTime >= ANALYSIS_INTERVAL)
        {
            AnalyzeAllContexts();
            lastAnalysisTime = Time.time;
        }
    }

    void TrackComponentContext(ComponentContext context)
    {
        if (context.component == null) return;

        System.Type type = context.component.GetType();
        BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        if (debugProfile.trackSerializedFields) flags |= BindingFlags.NonPublic;

        FieldInfo[] fields = type.GetFields(flags);

        foreach (FieldInfo field in fields)
        {
            if (!debugProfile.ShouldTrackField(field.Name)) continue;
            
            if (!field.IsPublic && !System.Attribute.IsDefined(field, typeof(SerializeField)))
                continue;

            try
            {
                object currentValue = field.GetValue(context.component);
                string fieldKey = field.Name;

                if (!context.fieldContexts.ContainsKey(fieldKey))
                {
                    context.fieldContexts[fieldKey] = new FieldContext
                    {
                        fieldName = field.Name,
                        fieldType = field.FieldType,
                        lastChangeTime = Time.time
                    };
                }

                FieldContext fieldContext = context.fieldContexts[fieldKey];
                
                // Check for value change
                if (fieldContext.lastValue == null || !AreValuesEqual(fieldContext.lastValue, currentValue))
                {
                    // Record change
                    fieldContext.valueHistory.Add(CloneValue(currentValue));
                    fieldContext.changeTimestamps.Add(Time.time);
                    fieldContext.lastChangeTime = Time.time;
                    fieldContext.changeCount++;
                    fieldContext.lastValue = CloneValue(currentValue);

                    // Limit history size
                    if (fieldContext.valueHistory.Count > 100)
                    {
                        fieldContext.valueHistory.RemoveAt(0);
                        fieldContext.changeTimestamps.RemoveAt(0);
                    }

                    // Check for rapid changes
                    if (alertOnRapidChanges)
                    {
                        CheckRapidChanges(fieldContext, context.component.gameObject.name, field.Name);
                    }
                }
                else
                {
                    // Check for stuck values
                    if (alertOnStuckValues)
                    {
                        CheckStuckValue(fieldContext, context.component.gameObject.name, field.Name);
                    }
                }
            }
            catch { }
        }

        context.lastUpdateTime = Time.time;
    }

    void CheckRapidChanges(FieldContext fieldContext, string objectName, string fieldName)
    {
        if (fieldContext.changeTimestamps.Count < 2) return;

        // Calculate change rate over last second
        float currentTime = Time.time;
        int recentChanges = 0;
        
        for (int i = fieldContext.changeTimestamps.Count - 1; i >= 0; i--)
        {
            if (currentTime - fieldContext.changeTimestamps[i] <= 1f)
            {
                recentChanges++;
            }
            else
            {
                break;
            }
        }

        if (recentChanges > rapidChangeThreshold)
        {
            string alert = $"⚠️ RAPID CHANGES DETECTED: {objectName}.{fieldName} changed {recentChanges} times in 1 second!";
            contextWriter.WriteLine($"[{Time.time:F2}s] {alert}");
            Debug.LogWarning($"[ContextualInspectorTracker] {alert}");
            
            if (!fieldContext.suggestions.Contains("Consider caching or throttling this value"))
            {
                fieldContext.suggestions.Add("Consider caching or throttling this value");
            }
        }
    }

    void CheckStuckValue(FieldContext fieldContext, string objectName, string fieldName)
    {
        float timeSinceChange = Time.time - fieldContext.lastChangeTime;
        
        if (timeSinceChange > stuckValueTimeout && !fieldContext.isPotentiallyStuck)
        {
            fieldContext.isPotentiallyStuck = true;
            string alert = $"⚠️ STUCK VALUE DETECTED: {objectName}.{fieldName} hasn't changed in {timeSinceChange:F1}s";
            contextWriter.WriteLine($"[{Time.time:F2}s] {alert}");
            
            if (!fieldContext.suggestions.Contains("Verify this value is being updated correctly"))
            {
                fieldContext.suggestions.Add("Verify this value is being updated correctly");
            }
        }
        else if (timeSinceChange < stuckValueTimeout)
        {
            fieldContext.isPotentiallyStuck = false;
        }
    }

    void AnalyzeAllContexts()
    {
        contextWriter.WriteLine($"\n═══ CONTEXTUAL ANALYSIS [{Time.time:F2}s] ═══");
        
        foreach (var compKvp in componentContexts)
        {
            ComponentContext context = compKvp.Value;
            if (context.component == null) continue;

            contextWriter.WriteLine($"\n📦 {context.component.GetType().Name} on {context.component.gameObject.name}:");

            foreach (var fieldKvp in context.fieldContexts)
            {
                FieldContext fieldContext = fieldKvp.Value;
                
                if (detectPatterns)
                {
                    DetectPatterns(fieldContext);
                }

                if (fieldContext.changeCount > 0 || fieldContext.suggestions.Count > 0)
                {
                    contextWriter.WriteLine($"  🔍 {fieldContext.fieldName}:");
                    contextWriter.WriteLine($"    Changes: {fieldContext.changeCount}");
                    contextWriter.WriteLine($"    Last Change: {Time.time - fieldContext.lastChangeTime:F2}s ago");
                    
                    if (!string.IsNullOrEmpty(fieldContext.detectedPattern))
                    {
                        contextWriter.WriteLine($"    Pattern: {fieldContext.detectedPattern}");
                    }

                    if (fieldContext.suggestions.Count > 0)
                    {
                        contextWriter.WriteLine($"    💡 Suggestions:");
                        foreach (string suggestion in fieldContext.suggestions)
                        {
                            contextWriter.WriteLine($"      - {suggestion}");
                        }
                    }
                }
            }
        }
        
        contextWriter.WriteLine();
    }

    void DetectPatterns(FieldContext fieldContext)
    {
        if (fieldContext.valueHistory.Count < 10) return;
        if (!IsNumericType(fieldContext.fieldType)) return;

        List<float> numericValues = new List<float>();
        foreach (object val in fieldContext.valueHistory)
        {
            numericValues.Add(ConvertToFloat(val));
        }

        // Detect oscillation
        if (DetectOscillation(numericValues))
        {
            fieldContext.isOscillating = true;
            fieldContext.detectedPattern = "Oscillating";
            
            if (!fieldContext.suggestions.Contains("Value is oscillating - consider smoothing or damping"))
            {
                fieldContext.suggestions.Add("Value is oscillating - consider smoothing or damping");
            }
        }

        // Detect constant increase/decrease
        if (DetectMonotonicTrend(numericValues, out bool increasing))
        {
            fieldContext.detectedPattern = increasing ? "Constantly Increasing" : "Constantly Decreasing";
            
            string suggestion = increasing 
                ? "Value constantly increasing - check for memory leaks or unbounded growth"
                : "Value constantly decreasing - verify intended behavior";
            
            if (!fieldContext.suggestions.Contains(suggestion))
            {
                fieldContext.suggestions.Add(suggestion);
            }
        }

        // Detect spikes
        if (DetectSpikes(numericValues))
        {
            fieldContext.detectedPattern = "Spiking";
            
            if (!fieldContext.suggestions.Contains("Value has spikes - investigate sudden changes"))
            {
                fieldContext.suggestions.Add("Value has spikes - investigate sudden changes");
            }
        }
    }

    bool DetectOscillation(List<float> values)
    {
        if (values.Count < 6) return false;

        int directionChanges = 0;
        for (int i = 2; i < values.Count; i++)
        {
            float prev = values[i - 1] - values[i - 2];
            float curr = values[i] - values[i - 1];
            
            if (Mathf.Sign(prev) != Mathf.Sign(curr) && Mathf.Abs(curr) > 0.001f)
            {
                directionChanges++;
            }
        }

        return directionChanges > values.Count / 3;
    }

    bool DetectMonotonicTrend(List<float> values, out bool increasing)
    {
        increasing = true;
        if (values.Count < 5) return false;

        int increasingCount = 0;
        int decreasingCount = 0;

        for (int i = 1; i < values.Count; i++)
        {
            if (values[i] > values[i - 1]) increasingCount++;
            if (values[i] < values[i - 1]) decreasingCount++;
        }

        increasing = increasingCount > decreasingCount;
        return (increasingCount > values.Count * 0.8f) || (decreasingCount > values.Count * 0.8f);
    }

    bool DetectSpikes(List<float> values)
    {
        if (values.Count < 5) return false;

        float average = 0f;
        foreach (float v in values) average += v;
        average /= values.Count;

        float stdDev = 0f;
        foreach (float v in values)
        {
            stdDev += (v - average) * (v - average);
        }
        stdDev = Mathf.Sqrt(stdDev / values.Count);

        int spikeCount = 0;
        foreach (float v in values)
        {
            if (Mathf.Abs(v - average) > stdDev * 2f)
            {
                spikeCount++;
            }
        }

        return spikeCount > 0 && spikeCount < values.Count * 0.3f;
    }

    bool AreValuesEqual(object a, object b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        
        if (a is Vector3 v3a && b is Vector3 v3b)
            return Vector3.Distance(v3a, v3b) < 0.001f;
        
        if (a is float fa && b is float fb)
            return Mathf.Abs(fa - fb) < 0.001f;
        
        return a.Equals(b);
    }

    object CloneValue(object value)
    {
        if (value == null) return null;
        if (value is Vector3 v3) return new Vector3(v3.x, v3.y, v3.z);
        if (value is Vector2 v2) return new Vector2(v2.x, v2.y);
        return value;
    }

    bool IsNumericType(System.Type type)
    {
        return type == typeof(int) || type == typeof(float) || type == typeof(double);
    }

    float ConvertToFloat(object value)
    {
        if (value is int i) return i;
        if (value is float f) return f;
        if (value is double d) return (float)d;
        return 0f;
    }

    void OnDestroy()
    {
        if (contextWriter != null)
        {
            contextWriter.WriteLine("\n═══════════════════════════════════════════════════════════════");
            contextWriter.WriteLine($"🏁 Contextual Tracking Ended: {System.DateTime.Now}");
            contextWriter.WriteLine("═══════════════════════════════════════════════════════════════");
            contextWriter.Close();
        }
    }

    void OnApplicationQuit()
    {
        OnDestroy();
    }
}
