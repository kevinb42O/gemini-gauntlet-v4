using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    [Header("Checkpoint Generation")]
    [SerializeField] private bool autoGenerateCheckpoints = false;
    [SerializeField] private int numberOfCheckpoints = 8;
    [SerializeField] private float pathRadius = 50f;
    [SerializeField] private float pathHeight = 10f;
    [SerializeField] private Vector3 pathCenter = Vector3.zero;
    
    [Header("Manual Checkpoints")]
    [SerializeField] private Transform[] manualCheckpoints;
    
    [Header("Checkpoint Visualization")]
    [SerializeField] private GameObject checkpointPrefab;
    [SerializeField] private bool showCheckpointGizmos = true;
    [SerializeField] private float gizmoSize = 2f;
    
    private Transform[] generatedCheckpoints;
    
    void Start()
    {
        if (autoGenerateCheckpoints)
        {
            GenerateCircularCheckpoints();
        }
    }
    
    /// <summary>
    /// Generate checkpoints in a circular pattern
    /// </summary>
    public void GenerateCircularCheckpoints()
    {
        // Clear existing generated checkpoints
        ClearGeneratedCheckpoints();
        
        generatedCheckpoints = new Transform[numberOfCheckpoints];
        
        for (int i = 0; i < numberOfCheckpoints; i++)
        {
            // Calculate position on circle
            float angle = (float)i / numberOfCheckpoints * 2f * Mathf.PI;
            Vector3 position = pathCenter + new Vector3(
                Mathf.Cos(angle) * pathRadius,
                pathHeight + Mathf.Sin(angle * 2f) * 5f, // Add some vertical variation
                Mathf.Sin(angle) * pathRadius
            );
            
            // Create checkpoint GameObject
            GameObject checkpoint = new GameObject($"Checkpoint_{i}");
            checkpoint.transform.position = position;
            checkpoint.transform.parent = transform;
            
            // Add visual marker if prefab is provided
            if (checkpointPrefab != null)
            {
                GameObject marker = Instantiate(checkpointPrefab, position, Quaternion.identity, checkpoint.transform);
            }
            
            generatedCheckpoints[i] = checkpoint.transform;
        }
        
        Debug.Log($"[CheckpointManager] Generated {numberOfCheckpoints} circular checkpoints");
    }
    
    /// <summary>
    /// Generate checkpoints in a figure-8 pattern
    /// </summary>
    public void GenerateFigureEightCheckpoints()
    {
        ClearGeneratedCheckpoints();
        generatedCheckpoints = new Transform[numberOfCheckpoints];
        
        for (int i = 0; i < numberOfCheckpoints; i++)
        {
            float t = (float)i / numberOfCheckpoints * 2f * Mathf.PI;
            
            // Figure-8 parametric equations
            Vector3 position = pathCenter + new Vector3(
                Mathf.Sin(t) * pathRadius,
                pathHeight + Mathf.Sin(t * 2f) * 10f,
                Mathf.Sin(t) * Mathf.Cos(t) * pathRadius
            );
            
            GameObject checkpoint = new GameObject($"Checkpoint_Fig8_{i}");
            checkpoint.transform.position = position;
            checkpoint.transform.parent = transform;
            
            if (checkpointPrefab != null)
            {
                Instantiate(checkpointPrefab, position, Quaternion.identity, checkpoint.transform);
            }
            
            generatedCheckpoints[i] = checkpoint.transform;
        }
        
        Debug.Log($"[CheckpointManager] Generated {numberOfCheckpoints} figure-8 checkpoints");
    }
    
    /// <summary>
    /// Generate checkpoints in a spiral pattern
    /// </summary>
    public void GenerateSpiralCheckpoints()
    {
        ClearGeneratedCheckpoints();
        generatedCheckpoints = new Transform[numberOfCheckpoints];
        
        for (int i = 0; i < numberOfCheckpoints; i++)
        {
            float t = (float)i / numberOfCheckpoints;
            float angle = t * 4f * Mathf.PI; // Multiple spirals
            float radius = pathRadius * (1f - t * 0.5f); // Decreasing radius
            
            Vector3 position = pathCenter + new Vector3(
                Mathf.Cos(angle) * radius,
                pathHeight + t * 20f, // Rising spiral
                Mathf.Sin(angle) * radius
            );
            
            GameObject checkpoint = new GameObject($"Checkpoint_Spiral_{i}");
            checkpoint.transform.position = position;
            checkpoint.transform.parent = transform;
            
            if (checkpointPrefab != null)
            {
                Instantiate(checkpointPrefab, position, Quaternion.identity, checkpoint.transform);
            }
            
            generatedCheckpoints[i] = checkpoint.transform;
        }
        
        Debug.Log($"[CheckpointManager] Generated {numberOfCheckpoints} spiral checkpoints");
    }
    
    /// <summary>
    /// Get all available checkpoints (manual or generated)
    /// </summary>
    public Transform[] GetCheckpoints()
    {
        if (autoGenerateCheckpoints && generatedCheckpoints != null)
        {
            return generatedCheckpoints;
        }
        else if (manualCheckpoints != null && manualCheckpoints.Length > 0)
        {
            return manualCheckpoints;
        }
        
        return new Transform[0];
    }
    
    /// <summary>
    /// Clear all generated checkpoints
    /// </summary>
    private void ClearGeneratedCheckpoints()
    {
        if (generatedCheckpoints != null)
        {
            for (int i = 0; i < generatedCheckpoints.Length; i++)
            {
                if (generatedCheckpoints[i] != null)
                {
                    DestroyImmediate(generatedCheckpoints[i].gameObject);
                }
            }
        }
        generatedCheckpoints = null;
    }
    
    /// <summary>
    /// Validate checkpoint array for common issues
    /// </summary>
    public bool ValidateCheckpoints()
    {
        Transform[] checkpoints = GetCheckpoints();
        
        if (checkpoints.Length < 2)
        {
            Debug.LogError("[CheckpointManager] Need at least 2 checkpoints for train operation!");
            return false;
        }
        
        // Check for null references
        for (int i = 0; i < checkpoints.Length; i++)
        {
            if (checkpoints[i] == null)
            {
                Debug.LogError($"[CheckpointManager] Checkpoint {i} is null!");
                return false;
            }
        }
        
        // Check for duplicate positions
        for (int i = 0; i < checkpoints.Length; i++)
        {
            for (int j = i + 1; j < checkpoints.Length; j++)
            {
                if (Vector3.Distance(checkpoints[i].position, checkpoints[j].position) < 0.1f)
                {
                    Debug.LogWarning($"[CheckpointManager] Checkpoints {i} and {j} are very close together!");
                }
            }
        }
        
        Debug.Log($"[CheckpointManager] Checkpoint validation passed - {checkpoints.Length} valid checkpoints");
        return true;
    }
    
    /// <summary>
    /// Auto-assign checkpoints to a SpaceTrainController
    /// </summary>
    public void AssignToTrain(SpaceTrainController train)
    {
        if (train == null)
        {
            Debug.LogError("[CheckpointManager] Cannot assign to null train!");
            return;
        }
        
        Transform[] checkpoints = GetCheckpoints();
        if (ValidateCheckpoints())
        {
            // Use reflection to set the private checkpoints field
            var field = typeof(SpaceTrainController).GetField("checkpoints", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(train, checkpoints);
                Debug.Log($"[CheckpointManager] Assigned {checkpoints.Length} checkpoints to train {train.name}");
            }
            else
            {
                Debug.LogError("[CheckpointManager] Could not find checkpoints field in SpaceTrainController!");
            }
        }
    }
    
    // Editor helper methods
    #if UNITY_EDITOR
    [ContextMenu("Generate Circular Path")]
    public void EditorGenerateCircular()
    {
        GenerateCircularCheckpoints();
    }
    
    [ContextMenu("Generate Figure-8 Path")]
    public void EditorGenerateFigureEight()
    {
        GenerateFigureEightCheckpoints();
    }
    
    [ContextMenu("Generate Spiral Path")]
    public void EditorGenerateSpiral()
    {
        GenerateSpiralCheckpoints();
    }
    
    [ContextMenu("Clear Generated Checkpoints")]
    public void EditorClearCheckpoints()
    {
        ClearGeneratedCheckpoints();
    }
    
    [ContextMenu("Validate Checkpoints")]
    public void EditorValidateCheckpoints()
    {
        ValidateCheckpoints();
    }
    #endif
    
    // Debug visualization
    void OnDrawGizmos()
    {
        if (!showCheckpointGizmos) return;
        
        Transform[] checkpoints = GetCheckpoints();
        
        // Draw checkpoints
        for (int i = 0; i < checkpoints.Length; i++)
        {
            if (checkpoints[i] == null) continue;
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(checkpoints[i].position, gizmoSize);
            
            // Draw connection to next checkpoint
            int nextIndex = (i + 1) % checkpoints.Length;
            if (checkpoints[nextIndex] != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(checkpoints[i].position, checkpoints[nextIndex].position);
            }
            
            // Draw checkpoint number
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(checkpoints[i].position + Vector3.up * gizmoSize * 2f, i.ToString());
            #endif
        }
        
        // Draw path center
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(pathCenter, 1f);
    }
}
