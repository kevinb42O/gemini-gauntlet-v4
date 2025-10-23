using UnityEngine;

/// <summary>
/// Tracks which zones the player has completed
/// Add trigger colliders to goal platforms and reference this script
/// </summary>
public class ArenaProgressTracker : MonoBehaviour
{
    public static ArenaProgressTracker Instance;
    
    private bool[] zonesCompleted = new bool[6];
    private float[] zoneBestTimes = new float[6];
    private float[] zoneStartTimes = new float[6];
    
    private string[] zoneNames = new string[]
    {
        "Zone 1: Basic Wall Jump",
        "Zone 2: Drop Launch",
        "Zone 3: Zigzag Climb",
        "Zone 4: Speed Gauntlet",
        "Zone 5: Spiral Tower",
        "Zone 6: Canyon Flow"
    };
    
    private string[] zoneEmojis = new string[]
    {
        "🟢", "🔵", "🟡", "🟠", "🟣", "🔴"
    };
    
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
            
        for (int i = 0; i < 6; i++)
        {
            zoneBestTimes[i] = float.MaxValue;
            zoneStartTimes[i] = -1;
        }
    }
    
    public void EnterZone(int zoneIndex)
    {
        if (zoneIndex < 0 || zoneIndex >= 6) return;
        
        if (zoneStartTimes[zoneIndex] < 0)
        {
            zoneStartTimes[zoneIndex] = Time.time;
            Debug.Log($"{zoneEmojis[zoneIndex]} Entered {zoneNames[zoneIndex]}");
        }
    }
    
    public void CompleteZone(int zoneIndex)
    {
        if (zoneIndex < 0 || zoneIndex >= 6) return;
        
        if (!zonesCompleted[zoneIndex])
        {
            zonesCompleted[zoneIndex] = true;
            Debug.Log($"✅ {zoneEmojis[zoneIndex]} {zoneNames[zoneIndex]} COMPLETED!");
        }
        
        if (zoneStartTimes[zoneIndex] > 0)
        {
            float time = Time.time - zoneStartTimes[zoneIndex];
            if (time < zoneBestTimes[zoneIndex])
            {
                zoneBestTimes[zoneIndex] = time;
                Debug.Log($"⏱️ New best time: {time:F2}s");
            }
            zoneStartTimes[zoneIndex] = -1; // Reset for next attempt
        }
        
        CheckFullCompletion();
    }
    
    private void CheckFullCompletion()
    {
        bool allComplete = true;
        foreach (bool completed in zonesCompleted)
        {
            if (!completed)
            {
                allComplete = false;
                break;
            }
        }
        
        if (allComplete)
        {
            float totalTime = 0;
            foreach (float time in zoneBestTimes)
            {
                if (time < float.MaxValue)
                    totalTime += time;
            }
            
            Debug.Log("🏆 ═══════════════════════════════════");
            Debug.Log("🏆 ALL ZONES COMPLETED!");
            Debug.Log($"🏆 Total Best Time: {totalTime:F2}s");
            Debug.Log("🏆 YOU ARE SPEED!");
            Debug.Log("🏆 ═══════════════════════════════════");
        }
    }
    
    void OnGUI()
    {
        int x = 10;
        int y = 180;
        
        GUI.Box(new Rect(x, y, 300, 210), "🏆 ARENA PROGRESS");
        
        for (int i = 0; i < 6; i++)
        {
            string status = zonesCompleted[i] ? "✅" : "⬜";
            string timeStr = "";
            
            if (zoneBestTimes[i] < float.MaxValue)
            {
                timeStr = $" - {zoneBestTimes[i]:F2}s";
            }
            
            GUI.Label(new Rect(x + 10, y + 25 + (i * 30), 280, 25), 
                $"{status} {zoneEmojis[i]} {zoneNames[i]}{timeStr}");
        }
        
        // Progress bar
        int completed = 0;
        foreach (bool done in zonesCompleted)
            if (done) completed++;
        
        float progress = completed / 6f;
        GUI.Box(new Rect(x + 10, y + 185, 280, 20), "");
        GUI.Box(new Rect(x + 10, y + 185, 280 * progress, 20), $"{completed}/6");
    }
}

/// <summary>
/// Add this to goal platform trigger colliders
/// </summary>
public class ZoneGoalTrigger : MonoBehaviour
{
    [Header("Zone Configuration")]
    [Tooltip("0=Zone1, 1=Zone2, etc.")]
    public int zoneIndex = 0;
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (ArenaProgressTracker.Instance != null)
            {
                ArenaProgressTracker.Instance.CompleteZone(zoneIndex);
            }
        }
    }
}

/// <summary>
/// Add this to zone start platform trigger colliders
/// </summary>
public class ZoneStartTrigger : MonoBehaviour
{
    [Header("Zone Configuration")]
    [Tooltip("0=Zone1, 1=Zone2, etc.")]
    public int zoneIndex = 0;
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (ArenaProgressTracker.Instance != null)
            {
                ArenaProgressTracker.Instance.EnterZone(zoneIndex);
            }
        }
    }
}
