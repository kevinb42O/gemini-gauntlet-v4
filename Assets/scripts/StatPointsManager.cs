using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class StatPointsManager : MonoBehaviour
{
    private const int POINTS_PER_COMPANION = 70; // Each companion gets their own 70 points
    private const int MAX_POINTS_PER_STAT = 20;
    private const string SAVE_FILE_NAME = "CompanionStatPoints.json";

    // Only these 4 companions are used
    private static readonly string[] VALID_COMPANIONS = { "Medic Companion", "Aggressive Companion", "Tank Companion", "Loyal Companion" };

    [System.Serializable]
    private class CompanionStatData
    {
        public string companionName;
        public int attackPower;
        public int defense;
        public int speed;
        public int accuracy;
        public int intelligence;
        public int loyalty;
        public int courage;
        public int totalSpentPoints;
    }

    private Dictionary<string, CompanionStatData> companionStats = new Dictionary<string, CompanionStatData>();
    private int totalSpentPoints = 0;

    public static StatPointsManager Instance { get; private set; }
    public static event System.Action OnInitialized;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadStatPoints();
            OnInitialized?.Invoke();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool IsValidCompanion(string companionName)
    {
        return System.Array.Exists(VALID_COMPANIONS, name => name == companionName);
    }

    public int GetAvailablePoints(string companionName)
    {
        if (!IsValidCompanion(companionName))
            return 0;

        if (!companionStats.ContainsKey(companionName))
            return POINTS_PER_COMPANION; // New companion gets full 70 points

        CompanionStatData data = companionStats[companionName];
        int availablePoints = POINTS_PER_COMPANION - data.totalSpentPoints;

        // Ensure we never return negative values
        return Mathf.Max(0, availablePoints);
    }

    public int GetTotalSpentPoints(string companionName)
    {
        if (!IsValidCompanion(companionName))
            return 0;

        if (!companionStats.ContainsKey(companionName))
            return 0;

        return companionStats[companionName].totalSpentPoints;
    }

    public int GetTotalAvailablePoints(string companionName)
    {
        return POINTS_PER_COMPANION; // Always 70 for all companions
    }

    public bool CanSpendPoints(string companionName, int points)
    {
        if (!IsValidCompanion(companionName))
            return false;

        int available = GetAvailablePoints(companionName);
        return (available - points) >= 0;
    }

    public int GetBaseTotalForCompanion(string companionName)
    {
        // Legacy support: all companions now start from zero base stats
        return 0;
    }

    public bool CanIncreaseStat(string companionName, string statName)
    {
        Debug.Log($"CanIncreaseStat called: companion={companionName}, stat={statName}");

        if (!IsValidCompanion(companionName) || !companionStats.ContainsKey(companionName))
        {
            Debug.Log($"New companion or no saved data: valid={IsValidCompanion(companionName)}, availablePoints={GetAvailablePoints(companionName)}");
            return GetAvailablePoints(companionName) > 0;
        }

        CompanionStatData data = companionStats[companionName];

        switch (statName.ToLower())
        {
            case "attackpower":
                bool canAttack = data.attackPower < MAX_POINTS_PER_STAT && GetAvailablePoints(companionName) > 0;
                Debug.Log($"Attack check: current={data.attackPower}, max={MAX_POINTS_PER_STAT}, available={GetAvailablePoints(companionName)}, canIncrease={canAttack}");
                return canAttack;
            case "defense":
                bool canDefense = data.defense < MAX_POINTS_PER_STAT && GetAvailablePoints(companionName) > 0;
                Debug.Log($"Defense check: current={data.defense}, max={MAX_POINTS_PER_STAT}, available={GetAvailablePoints(companionName)}, canIncrease={canDefense}");
                return canDefense;
            case "speed":
                bool canSpeed = data.speed < MAX_POINTS_PER_STAT && GetAvailablePoints(companionName) > 0;
                Debug.Log($"Speed check: current={data.speed}, max={MAX_POINTS_PER_STAT}, available={GetAvailablePoints(companionName)}, canIncrease={canSpeed}");
                return canSpeed;
            case "accuracy":
                bool canAccuracy = data.accuracy < MAX_POINTS_PER_STAT && GetAvailablePoints(companionName) > 0;
                Debug.Log($"Accuracy check: current={data.accuracy}, max={MAX_POINTS_PER_STAT}, available={GetAvailablePoints(companionName)}, canIncrease={canAccuracy}");
                return canAccuracy;
            case "intelligence":
                bool canIntelligence = data.intelligence < MAX_POINTS_PER_STAT && GetAvailablePoints(companionName) > 0;
                Debug.Log($"Intelligence check: current={data.intelligence}, max={MAX_POINTS_PER_STAT}, available={GetAvailablePoints(companionName)}, canIncrease={canIntelligence}");
                return canIntelligence;
            case "loyalty":
                bool canLoyalty = data.loyalty < MAX_POINTS_PER_STAT && GetAvailablePoints(companionName) > 0;
                Debug.Log($"Loyalty check: current={data.loyalty}, max={MAX_POINTS_PER_STAT}, available={GetAvailablePoints(companionName)}, canIncrease={canLoyalty}");
                return canLoyalty;
            case "courage":
                bool canCourage = data.courage < MAX_POINTS_PER_STAT && GetAvailablePoints(companionName) > 0;
                Debug.Log($"Courage check: current={data.courage}, max={MAX_POINTS_PER_STAT}, available={GetAvailablePoints(companionName)}, canIncrease={canCourage}");
                return canCourage;
            default:
                Debug.LogError($"Unknown stat name in CanIncreaseStat: {statName}");
                return false;
        }
    }

    public bool SpendPoint(string companionName, string statName)
    {
        Debug.Log($"SpendPoint called: companion={companionName}, stat={statName}");

        if (!IsValidCompanion(companionName) || !CanIncreaseStat(companionName, statName))
        {
            Debug.Log($"Cannot spend point: validCompanion={IsValidCompanion(companionName)}, canIncrease={CanIncreaseStat(companionName, statName)}");
            return false;
        }

        if (!companionStats.ContainsKey(companionName))
        {
            companionStats[companionName] = new CompanionStatData
            {
                companionName = companionName,
                attackPower = 0,
                defense = 0,
                speed = 0,
                accuracy = 0,
                intelligence = 0,
                loyalty = 0,
                courage = 0,
                totalSpentPoints = 0
            };
        }

        CompanionStatData data = companionStats[companionName];

        switch (statName.ToLower())
        {
            case "attackpower":
                data.attackPower++;
                break;
            case "defense":
                data.defense++;
                break;
            case "speed":
                data.speed++;
                break;
            case "accuracy":
                data.accuracy++;
                break;
            case "intelligence":
                data.intelligence++;
                break;
            case "loyalty":
                data.loyalty++;
                break;
            case "courage":
                data.courage++;
                break;
            default:
                Debug.LogError($"Unknown stat name: {statName}");
                return false;
        }

        data.totalSpentPoints++;
        SaveStatPoints();
        Debug.Log($"Point spent successfully! New total: {data.totalSpentPoints}/{POINTS_PER_COMPANION}");
        return true;
    }

    public bool RefundPoint(string companionName, string statName)
    {
        if (!IsValidCompanion(companionName) || !companionStats.ContainsKey(companionName))
            return false;

        CompanionStatData data = companionStats[companionName];

        switch (statName.ToLower())
        {
            case "attackpower":
                if (data.attackPower > 0)
                {
                    data.attackPower--;
                    data.totalSpentPoints--;
                    SaveStatPoints();
                    return true;
                }
                break;
            case "defense":
                if (data.defense > 0)
                {
                    data.defense--;
                    data.totalSpentPoints--;
                    SaveStatPoints();
                    return true;
                }
                break;
            case "speed":
                if (data.speed > 0)
                {
                    data.speed--;
                    data.totalSpentPoints--;
                    SaveStatPoints();
                    return true;
                }
                break;
            case "accuracy":
                if (data.accuracy > 0)
                {
                    data.accuracy--;
                    data.totalSpentPoints--;
                    SaveStatPoints();
                    return true;
                }
                break;
            case "intelligence":
                if (data.intelligence > 0)
                {
                    data.intelligence--;
                    data.totalSpentPoints--;
                    SaveStatPoints();
                    return true;
                }
                break;
            case "loyalty":
                if (data.loyalty > 0)
                {
                    data.loyalty--;
                    data.totalSpentPoints--;
                    SaveStatPoints();
                    return true;
                }
                break;
            case "courage":
                if (data.courage > 0)
                {
                    data.courage--;
                    data.totalSpentPoints--;
                    SaveStatPoints();
                    return true;
                }
                break;
        }

        return false;
    }

    public void ResetCompanionStats(string companionName)
    {
        if (!IsValidCompanion(companionName) || !companionStats.ContainsKey(companionName))
            return;

        companionStats.Remove(companionName);
        SaveStatPoints();
    }

    public int GetStatValue(string companionName, string statName)
    {
        if (!IsValidCompanion(companionName) || !companionStats.ContainsKey(companionName))
            return 0;

        CompanionStatData data = companionStats[companionName];

        switch (statName.ToLower())
        {
            case "attackpower":
                return data.attackPower;
            case "defense":
                return data.defense;
            case "speed":
                return data.speed;
            case "accuracy":
                return data.accuracy;
            case "intelligence":
                return data.intelligence;
            case "loyalty":
                return data.loyalty;
            case "courage":
                return data.courage;
            default:
                return 0;
        }
    }

    public void ResetAllCompanionStats()
    {
        Debug.Log("ResetAllCompanionStats called - clearing all saved companion data");

        // Clear all companion data
        companionStats.Clear();
        totalSpentPoints = 0;

        // Save empty data to file
        SaveStatPoints();

        Debug.Log("All companion stats reset to zero. Fresh start!");
    }

    private void SaveStatPoints()
    {
        List<CompanionStatData> dataList = new List<CompanionStatData>(companionStats.Values);
        string json = JsonUtility.ToJson(new SerializableData { companions = dataList, totalSpent = totalSpentPoints });
        string path = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

        try
        {
            File.WriteAllText(path, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save stat points: {e.Message}");
        }
    }

    private void LoadStatPoints()
    {
        string path = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                SerializableData data = JsonUtility.FromJson<SerializableData>(json);

                companionStats.Clear();
                foreach (var companionData in data.companions)
                {
                    if (IsValidCompanion(companionData.companionName))
                    {
                        companionStats[companionData.companionName] = companionData;
                    }
                }
                totalSpentPoints = data.totalSpent;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load stat points: {e.Message}");
                totalSpentPoints = 0;
                companionStats.Clear();
            }
        }
    }

    [System.Serializable]
    private class SerializableData
    {
        public List<CompanionStatData> companions;
        public int totalSpent;
    }
}
