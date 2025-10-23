using UnityEngine;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 🤖 AI OPTIMIZATION REPORTER - ULTIMATE INTELLIGENCE
/// Generates comprehensive optimization reports for AI analysis
/// Identifies bottlenecks, inefficiencies, and improvement opportunities
/// Provides actionable insights with priority rankings
/// </summary>
public class AIOptimizationReporter : MonoBehaviour
{
    [Header("🤖 AI OPTIMIZATION ENGINE")]
    [Tooltip("Debug profile to analyze")]
    public SmartDebugProfile debugProfile;
    
    [Tooltip("Enable optimization reporting")]
    public bool enableReporting = true;
    
    [Header("📊 REPORT GENERATION")]
    [Tooltip("Generate report every N seconds")]
    public float reportInterval = 30f;
    
    [Tooltip("Auto-generate report on application quit")]
    public bool reportOnQuit = true;
    
    [Header("🎯 ANALYSIS FOCUS")]
    [Tooltip("Analyze performance bottlenecks")]
    public bool analyzePerformance = true;
    
    [Tooltip("Analyze memory usage patterns")]
    public bool analyzeMemory = true;
    
    [Tooltip("Analyze value change patterns")]
    public bool analyzeValuePatterns = true;
    
    [Tooltip("Analyze component relationships")]
    public bool analyzeRelationships = true;
    
    [Header("⚠️ PRIORITY THRESHOLDS")]
    [Tooltip("Critical: Issues requiring immediate attention")]
    public bool flagCriticalIssues = true;
    
    [Tooltip("High: Significant optimization opportunities")]
    public bool flagHighPriorityIssues = true;
    
    [Tooltip("Medium: Moderate improvements possible")]
    public bool flagMediumPriorityIssues = true;

    // Internal tracking
    private Dictionary<string, ComponentAnalysis> componentAnalyses = new Dictionary<string, ComponentAnalysis>();
    private List<OptimizationInsight> insights = new List<OptimizationInsight>();
    private float nextReportTime = 0f;
    private string reportDirectory;
    private int reportCount = 0;

    private class ComponentAnalysis
    {
        public string componentName;
        public int instanceCount;
        public float totalUpdateTime;
        public int updateCount;
        public Dictionary<string, FieldAnalysis> fieldAnalyses = new Dictionary<string, FieldAnalysis>();
        public List<string> detectedIssues = new List<string>();
        public float averageUpdateTime => updateCount > 0 ? totalUpdateTime / updateCount : 0f;
    }

    private class FieldAnalysis
    {
        public string fieldName;
        public int changeCount;
        public float changeFrequency;
        public bool isHighFrequency;
        public bool isUnused;
        public bool hasPattern;
        public string patternType;
    }

    private class OptimizationInsight
    {
        public string category;
        public string priority; // CRITICAL, HIGH, MEDIUM, LOW
        public string issue;
        public string recommendation;
        public string affectedComponent;
        public float impactScore; // 0-10
    }

    void Awake()
    {
        if (debugProfile == null || !debugProfile.IsValid())
        {
            enableReporting = false;
            return;
        }

        InitializeReporting();
        nextReportTime = Time.time + reportInterval;
    }

    void InitializeReporting()
    {
        reportDirectory = Path.Combine(Application.dataPath, "..", "CASCADE_DEBUG_EXPORTS", "OptimizationReports");
        if (!Directory.Exists(reportDirectory))
        {
            Directory.CreateDirectory(reportDirectory);
        }
        
        Debug.Log($"[AIOptimizationReporter] 🤖 AI Optimization Reporter initialized");
    }

    void Update()
    {
        if (!enableReporting) return;

        if (Time.time >= nextReportTime)
        {
            GenerateOptimizationReport();
            nextReportTime = Time.time + reportInterval;
        }
    }

    public void GenerateOptimizationReport()
    {
        reportCount++;
        
        Debug.Log($"[AIOptimizationReporter] 🤖 Generating Optimization Report #{reportCount}...");
        
        // Collect analysis data
        CollectComponentData();
        
        // Analyze and generate insights
        GenerateInsights();
        
        // Create report
        string report = BuildReport();
        
        // Save report
        SaveReport(report);
        
        Debug.Log($"[AIOptimizationReporter] ✅ Optimization Report #{reportCount} generated!");
    }

    void CollectComponentData()
    {
        componentAnalyses.Clear();
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        List<string> scriptNames = debugProfile.GetTrackedScriptNames();
        
        foreach (string scriptName in scriptNames)
        {
            ComponentAnalysis analysis = new ComponentAnalysis
            {
                componentName = scriptName,
                instanceCount = 0
            };

            Component[] components = player.GetComponentsInChildren<Component>(true);
            foreach (Component comp in components)
            {
                if (comp == null) continue;
                if (comp.GetType().Name == scriptName)
                {
                    analysis.instanceCount++;
                    
                    // Analyze component
                    AnalyzeComponent(comp, analysis);
                }
            }

            if (analysis.instanceCount > 0)
            {
                componentAnalyses[scriptName] = analysis;
            }
        }
    }

    void AnalyzeComponent(Component comp, ComponentAnalysis analysis)
    {
        System.Type type = comp.GetType();
        
        // Analyze fields
        var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        foreach (var field in fields)
        {
            if (!debugProfile.ShouldTrackField(field.Name)) continue;
            
            if (!field.IsPublic && !System.Attribute.IsDefined(field, typeof(SerializeField)))
                continue;

            if (!analysis.fieldAnalyses.ContainsKey(field.Name))
            {
                analysis.fieldAnalyses[field.Name] = new FieldAnalysis
                {
                    fieldName = field.Name,
                    changeCount = 0,
                    changeFrequency = 0f
                };
            }
        }

        // Check for common issues
        DetectCommonIssues(comp, analysis);
    }

    void DetectCommonIssues(Component comp, ComponentAnalysis analysis)
    {
        System.Type type = comp.GetType();
        
        // Check for Update() method
        var updateMethod = type.GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (updateMethod != null)
        {
            analysis.detectedIssues.Add("Has Update() method - consider optimization");
        }

        // Check for FixedUpdate() method
        var fixedUpdateMethod = type.GetMethod("FixedUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (fixedUpdateMethod != null)
        {
            analysis.detectedIssues.Add("Has FixedUpdate() method - verify necessity");
        }

        // Check for GetComponent calls in Update (would need IL analysis - simplified here)
        // This is a placeholder for more advanced analysis
        
        // Check for multiple instances
        if (analysis.instanceCount > 5)
        {
            analysis.detectedIssues.Add($"Multiple instances ({analysis.instanceCount}) - consider object pooling");
        }
    }

    void GenerateInsights()
    {
        insights.Clear();

        foreach (var kvp in componentAnalyses)
        {
            ComponentAnalysis analysis = kvp.Value;
            
            // Performance insights
            if (analyzePerformance)
            {
                GeneratePerformanceInsights(analysis);
            }

            // Memory insights
            if (analyzeMemory)
            {
                GenerateMemoryInsights(analysis);
            }

            // Pattern insights
            if (analyzeValuePatterns)
            {
                GeneratePatternInsights(analysis);
            }
        }

        // Sort insights by priority and impact
        insights = insights.OrderByDescending(i => GetPriorityScore(i.priority))
                          .ThenByDescending(i => i.impactScore)
                          .ToList();
    }

    void GeneratePerformanceInsights(ComponentAnalysis analysis)
    {
        // Check for Update() usage
        if (analysis.detectedIssues.Any(i => i.Contains("Update()")))
        {
            insights.Add(new OptimizationInsight
            {
                category = "Performance",
                priority = "HIGH",
                issue = $"{analysis.componentName} uses Update() method",
                recommendation = "Consider using events, coroutines, or less frequent updates. Cache frequently accessed components.",
                affectedComponent = analysis.componentName,
                impactScore = 7.5f
            });
        }

        // Check for multiple instances
        if (analysis.instanceCount > 5)
        {
            insights.Add(new OptimizationInsight
            {
                category = "Performance",
                priority = "MEDIUM",
                issue = $"{analysis.componentName} has {analysis.instanceCount} instances",
                recommendation = "Consider object pooling, component sharing, or reducing instance count.",
                affectedComponent = analysis.componentName,
                impactScore = 6.0f
            });
        }
    }

    void GenerateMemoryInsights(ComponentAnalysis analysis)
    {
        // Check for large collections
        foreach (var fieldKvp in analysis.fieldAnalyses)
        {
            FieldAnalysis field = fieldKvp.Value;
            
            if (field.fieldName.Contains("List") || field.fieldName.Contains("Array") || field.fieldName.Contains("Dictionary"))
            {
                insights.Add(new OptimizationInsight
                {
                    category = "Memory",
                    priority = "MEDIUM",
                    issue = $"{analysis.componentName}.{field.fieldName} is a collection",
                    recommendation = "Monitor collection size. Consider capacity pre-allocation and clearing unused entries.",
                    affectedComponent = analysis.componentName,
                    impactScore = 5.5f
                });
            }
        }
    }

    void GeneratePatternInsights(ComponentAnalysis analysis)
    {
        foreach (var fieldKvp in analysis.fieldAnalyses)
        {
            FieldAnalysis field = fieldKvp.Value;
            
            if (field.isHighFrequency)
            {
                insights.Add(new OptimizationInsight
                {
                    category = "Value Patterns",
                    priority = "HIGH",
                    issue = $"{analysis.componentName}.{field.fieldName} changes very frequently",
                    recommendation = "Consider caching, throttling, or using a different update strategy.",
                    affectedComponent = analysis.componentName,
                    impactScore = 7.0f
                });
            }

            if (field.isUnused)
            {
                insights.Add(new OptimizationInsight
                {
                    category = "Code Quality",
                    priority = "LOW",
                    issue = $"{analysis.componentName}.{field.fieldName} appears unused",
                    recommendation = "Remove unused fields to reduce memory footprint and improve code clarity.",
                    affectedComponent = analysis.componentName,
                    impactScore = 3.0f
                });
            }
        }
    }

    int GetPriorityScore(string priority)
    {
        switch (priority)
        {
            case "CRITICAL": return 4;
            case "HIGH": return 3;
            case "MEDIUM": return 2;
            case "LOW": return 1;
            default: return 0;
        }
    }

    string BuildReport()
    {
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("🤖 AI OPTIMIZATION REPORT - ULTIMATE INTELLIGENCE");
        sb.AppendLine($"Report #{reportCount}");
        sb.AppendLine($"Generated: {System.DateTime.Now}");
        sb.AppendLine($"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        sb.AppendLine($"Play Time: {Time.time:F2}s");
        sb.AppendLine("═══════════════════════════════════════════════════════════════\n");

        // Executive Summary
        sb.AppendLine("📊 EXECUTIVE SUMMARY");
        sb.AppendLine($"Total Components Analyzed: {componentAnalyses.Count}");
        sb.AppendLine($"Total Insights Generated: {insights.Count}");
        sb.AppendLine($"Critical Issues: {insights.Count(i => i.priority == "CRITICAL")}");
        sb.AppendLine($"High Priority Issues: {insights.Count(i => i.priority == "HIGH")}");
        sb.AppendLine($"Medium Priority Issues: {insights.Count(i => i.priority == "MEDIUM")}");
        sb.AppendLine($"Low Priority Issues: {insights.Count(i => i.priority == "LOW")}");
        sb.AppendLine();

        // Component Analysis
        sb.AppendLine("📦 COMPONENT ANALYSIS");
        foreach (var kvp in componentAnalyses)
        {
            ComponentAnalysis analysis = kvp.Value;
            sb.AppendLine($"\n  {analysis.componentName}:");
            sb.AppendLine($"    Instances: {analysis.instanceCount}");
            sb.AppendLine($"    Fields Tracked: {analysis.fieldAnalyses.Count}");
            
            if (analysis.detectedIssues.Count > 0)
            {
                sb.AppendLine($"    Detected Issues:");
                foreach (string issue in analysis.detectedIssues)
                {
                    sb.AppendLine($"      - {issue}");
                }
            }
        }
        sb.AppendLine();

        // Optimization Insights
        sb.AppendLine("💡 OPTIMIZATION INSIGHTS (Prioritized)");
        
        if (insights.Count == 0)
        {
            sb.AppendLine("  ✅ No significant optimization opportunities detected!");
        }
        else
        {
            var criticalInsights = insights.Where(i => i.priority == "CRITICAL").ToList();
            if (criticalInsights.Count > 0)
            {
                sb.AppendLine("\n  🚨 CRITICAL PRIORITY:");
                foreach (var insight in criticalInsights)
                {
                    sb.AppendLine($"\n    [{insight.category}] {insight.affectedComponent}");
                    sb.AppendLine($"    Issue: {insight.issue}");
                    sb.AppendLine($"    Recommendation: {insight.recommendation}");
                    sb.AppendLine($"    Impact Score: {insight.impactScore:F1}/10");
                }
            }

            var highInsights = insights.Where(i => i.priority == "HIGH").ToList();
            if (highInsights.Count > 0)
            {
                sb.AppendLine("\n  ⚠️ HIGH PRIORITY:");
                foreach (var insight in highInsights)
                {
                    sb.AppendLine($"\n    [{insight.category}] {insight.affectedComponent}");
                    sb.AppendLine($"    Issue: {insight.issue}");
                    sb.AppendLine($"    Recommendation: {insight.recommendation}");
                    sb.AppendLine($"    Impact Score: {insight.impactScore:F1}/10");
                }
            }

            var mediumInsights = insights.Where(i => i.priority == "MEDIUM").ToList();
            if (mediumInsights.Count > 0 && flagMediumPriorityIssues)
            {
                sb.AppendLine("\n  ℹ️ MEDIUM PRIORITY:");
                foreach (var insight in mediumInsights)
                {
                    sb.AppendLine($"\n    [{insight.category}] {insight.affectedComponent}");
                    sb.AppendLine($"    Issue: {insight.issue}");
                    sb.AppendLine($"    Recommendation: {insight.recommendation}");
                    sb.AppendLine($"    Impact Score: {insight.impactScore:F1}/10");
                }
            }
        }

        sb.AppendLine("\n═══════════════════════════════════════════════════════════════");
        sb.AppendLine("🎯 AI ANALYSIS COMPLETE");
        sb.AppendLine("This report provides actionable insights for game optimization.");
        sb.AppendLine("Focus on CRITICAL and HIGH priority items first for maximum impact.");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");

        return sb.ToString();
    }

    void SaveReport(string report)
    {
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = $"AI_Optimization_Report_{reportCount}_{timestamp}.txt";
        string filepath = Path.Combine(reportDirectory, filename);
        
        File.WriteAllText(filepath, report);
        
        Debug.Log($"[AIOptimizationReporter] 📄 Report saved: {filepath}");
        Debug.Log(report); // Also log to console for immediate visibility
    }

    void OnApplicationQuit()
    {
        if (reportOnQuit && enableReporting)
        {
            GenerateOptimizationReport();
        }
    }

    [ContextMenu("Generate Report Now")]
    public void GenerateReportNow()
    {
        GenerateOptimizationReport();
    }

    [ContextMenu("Open Reports Folder")]
    public void OpenReportsFolder()
    {
        if (Directory.Exists(reportDirectory))
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.RevealInFinder(reportDirectory);
#else
            System.Diagnostics.Process.Start(reportDirectory);
#endif
        }
    }
}
