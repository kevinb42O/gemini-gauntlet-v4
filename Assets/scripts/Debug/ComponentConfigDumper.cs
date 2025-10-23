using UnityEngine;
using System.IO;
using System.Text;
using System.Reflection;

/// <summary>
/// MAXIMUM CONTROL TOOL #3: Specific Component Configuration Dumper
/// Dumps detailed configuration of specific components with all Inspector values
/// Perfect for debugging specific systems like LayeredHandAnimationController!
/// </summary>
public class ComponentConfigDumper : MonoBehaviour
{
    [Header("🔍 Component Dumper Configuration")]
    [Tooltip("Dump configuration on Start")]
    public bool dumpOnStart = true;
    
    [Tooltip("Press this key to dump configuration at runtime")]
    public KeyCode dumpKey = KeyCode.F9;

    [Header("Target Components")]
    [Tooltip("Dump all components on this GameObject")]
    public bool dumpAllComponents = true;
    
    [Tooltip("Specific component types to dump (leave empty for all)")]
    public string[] specificComponentTypes = new string[]
    {
        "LayeredHandAnimationController",
        "IndividualLayeredHandController",
        "PlayerAnimationStateManager",
        "HandAnimationController",
        "ArmorPlateSystem",
        "PlayerShooterOrchestrator",
        "AAAMovementController",
        "CleanAAACrouch",
        "EnergySystem"
    };

    void Start()
    {
        if (dumpOnStart)
        {
            DumpConfiguration();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(dumpKey))
        {
            DumpConfiguration();
        }
    }

    [ContextMenu("Dump Configuration Now")]
    public void DumpConfiguration()
    {
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("🔍 COMPONENT CONFIGURATION DUMP - MAXIMUM CONTROL MODE");
        sb.AppendLine($"GameObject: {gameObject.name}");
        sb.AppendLine($"Time: {System.DateTime.Now}");
        sb.AppendLine($"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        sb.AppendLine("═══════════════════════════════════════════════════════════════\n");

        Component[] allComponents = GetComponents<Component>();
        
        foreach (Component comp in allComponents)
        {
            if (comp == null) continue;
            
            string typeName = comp.GetType().Name;
            
            // Check if we should dump this component
            if (!dumpAllComponents && specificComponentTypes.Length > 0)
            {
                bool shouldDump = false;
                foreach (string targetType in specificComponentTypes)
                {
                    if (typeName.Contains(targetType))
                    {
                        shouldDump = true;
                        break;
                    }
                }
                if (!shouldDump) continue;
            }

            DumpComponent(comp, sb);
        }

        SaveToFile(sb.ToString());
    }

    void DumpComponent(Component comp, StringBuilder sb)
    {
        System.Type type = comp.GetType();
        
        sb.AppendLine($"┌─────────────────────────────────────────────────────────────");
        sb.AppendLine($"│ 📦 {type.Name}");
        sb.AppendLine($"│ Namespace: {type.Namespace}");
        sb.AppendLine($"│ Enabled: {(comp is Behaviour behaviour ? behaviour.enabled.ToString() : "N/A")}");
        sb.AppendLine($"├─────────────────────────────────────────────────────────────");

        // Public fields
        FieldInfo[] publicFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        if (publicFields.Length > 0)
        {
            sb.AppendLine($"│ 🔓 PUBLIC FIELDS ({publicFields.Length}):");
            foreach (FieldInfo field in publicFields)
            {
                DumpField(comp, field, sb, "│   ");
            }
        }

        // Private/protected serialized fields
        FieldInfo[] privateFields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        var serializedFields = System.Array.FindAll(privateFields, f => 
            System.Attribute.IsDefined(f, typeof(SerializeField)));
        
        if (serializedFields.Length > 0)
        {
            sb.AppendLine($"│ 🔒 SERIALIZED PRIVATE FIELDS ({serializedFields.Length}):");
            foreach (FieldInfo field in serializedFields)
            {
                DumpField(comp, field, sb, "│   ");
            }
        }

        // Public properties
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        if (properties.Length > 0)
        {
            sb.AppendLine($"│ 📊 PUBLIC PROPERTIES ({properties.Length}):");
            foreach (PropertyInfo prop in properties)
            {
                if (!prop.CanRead) continue;
                if (prop.GetIndexParameters().Length > 0) continue;

                try
                {
                    object value = prop.GetValue(comp);
                    string valueStr = FormatValue(value);
                    sb.AppendLine($"│   {prop.Name} ({prop.PropertyType.Name}): {valueStr}");
                }
                catch (System.Exception e)
                {
                    sb.AppendLine($"│   {prop.Name}: <Error: {e.Message}>");
                }
            }
        }

        sb.AppendLine($"└─────────────────────────────────────────────────────────────\n");
    }

    void DumpField(Component comp, FieldInfo field, StringBuilder sb, string indent)
    {
        try
        {
            object value = field.GetValue(comp);
            string valueStr = FormatValue(value);
            
            // Add attribute info
            string attributes = "";
            if (System.Attribute.IsDefined(field, typeof(SerializeField)))
                attributes += "[SerializeField] ";
            if (System.Attribute.IsDefined(field, typeof(HeaderAttribute)))
                attributes += "[Header] ";
            if (System.Attribute.IsDefined(field, typeof(TooltipAttribute)))
            {
                var tooltip = (TooltipAttribute)System.Attribute.GetCustomAttribute(field, typeof(TooltipAttribute));
                attributes += $"[Tooltip: \"{tooltip.tooltip}\"] ";
            }
            if (System.Attribute.IsDefined(field, typeof(RangeAttribute)))
            {
                var range = (RangeAttribute)System.Attribute.GetCustomAttribute(field, typeof(RangeAttribute));
                attributes += $"[Range({range.min}, {range.max})] ";
            }

            sb.AppendLine($"{indent}{field.Name} ({field.FieldType.Name}): {valueStr}");
            if (!string.IsNullOrEmpty(attributes))
            {
                sb.AppendLine($"{indent}  {attributes}");
            }
        }
        catch (System.Exception e)
        {
            sb.AppendLine($"{indent}{field.Name}: <Error: {e.Message}>");
        }
    }

    string FormatValue(object value)
    {
        if (value == null) return "null";
        
        // Unity Objects
        if (value is UnityEngine.Object unityObj)
        {
            if (unityObj == null) return "null";
            return $"{unityObj.GetType().Name}: \"{unityObj.name}\"";
        }
        
        // Vectors and Quaternions
        if (value is Vector3 v3) return $"({v3.x:F3}, {v3.y:F3}, {v3.z:F3})";
        if (value is Vector2 v2) return $"({v2.x:F3}, {v2.y:F3})";
        if (value is Quaternion q) return $"Euler({q.eulerAngles.x:F1}, {q.eulerAngles.y:F1}, {q.eulerAngles.z:F1})";
        if (value is Color c) return $"RGBA({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})";
        
        // Collections
        if (value is System.Collections.IList list)
        {
            StringBuilder listSb = new StringBuilder();
            listSb.Append($"[{list.Count} items");
            if (list.Count > 0 && list.Count <= 5)
            {
                listSb.Append(": ");
                for (int i = 0; i < list.Count; i++)
                {
                    if (i > 0) listSb.Append(", ");
                    listSb.Append(FormatValue(list[i]));
                }
            }
            listSb.Append("]");
            return listSb.ToString();
        }
        
        // Enums
        if (value is System.Enum)
        {
            return $"{value.GetType().Name}.{value}";
        }
        
        // Primitives
        if (value is float f) return f.ToString("F3");
        if (value is double d) return d.ToString("F3");
        
        return value.ToString();
    }

    void SaveToFile(string content)
    {
        string directory = Path.Combine(Application.dataPath, "..", "CASCADE_DEBUG_EXPORTS");
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filepath = Path.Combine(directory, $"{gameObject.name}_ComponentDump_{timestamp}.txt");
        
        File.WriteAllText(filepath, content);
        
        Debug.Log($"✅ Component Configuration Dumped!\nSaved to: {filepath}");
        Debug.Log(content); // Also log to console
    }
}
