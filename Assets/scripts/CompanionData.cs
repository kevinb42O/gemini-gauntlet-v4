using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CompanionData", menuName = "Companion/Companion Data")]
public class CompanionData : ScriptableObject
{
    [Header("Companion Basic Info")]
    public string companionName = "Companion";
    public Sprite companionImage;
    public string description = "A loyal companion";

    [Header("Combat Stats (0-20)")]
    [Range(0, 20)] public int attackPower = 0;
    [Range(0, 20)] public int defense = 0;
    [Range(0, 20)] public int speed = 0;
    [Range(0, 20)] public int accuracy = 0;

    [Header("Special Stats (0-20)")]
    [Range(0, 20)] public int intelligence = 0;
    [Range(0, 20)] public int loyalty = 0;
    [Range(0, 20)] public int courage = 0;

    public const int MaxLevel = 70;

    [Header("Companion Level")]
    [Range(1, MaxLevel)] public int companionLevel = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 100;

    [Header("Level Scaling Settings")]
    public int baseXPRequirement = 100;
    public float xpGrowthMultiplier = 1.35f;

    [Header("Cooldown Settings")]
    [Range(2, 6)] public int cooldownHours = 4; // Random between 2-6 hours
    public float currentCooldownTime = 0f; // Current cooldown in seconds
    public bool isOnCooldown = false;

    [Header("Visual Settings")]
    public Color companionColor = Color.white;
    public RuntimeAnimatorController animatorController;

    private static readonly List<CompanionData> _registeredCompanions = new List<CompanionData>();
    private const string PendingXPKey = "CompanionProgress_PendingXP";
    private bool _progressionInitialized = false;

    public static IReadOnlyList<CompanionData> RegisteredCompanions => _registeredCompanions;

    public static void QueueXPForAll(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int pending = PlayerPrefs.GetInt(PendingXPKey, 0);
        long newTotal = (long)pending + amount;
        int clamped = newTotal > int.MaxValue ? int.MaxValue : (int)newTotal;
        PlayerPrefs.SetInt(PendingXPKey, clamped);
        PlayerPrefs.Save();
    }

    public static int ConsumePendingXP()
    {
        int pending = PlayerPrefs.GetInt(PendingXPKey, 0);
        if (pending > 0)
        {
            PlayerPrefs.DeleteKey(PendingXPKey);
            PlayerPrefs.Save();
        }

        return pending;
    }

    public static void AwardXPToAll(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        foreach (var companion in _registeredCompanions)
        {
            if (companion == null)
            {
                continue;
            }

            companion.EnsureProgressionInitialized();
            companion.AddXP(amount);
        }
    }

    public void EnsureProgressionInitialized()
    {
        if (_progressionInitialized)
        {
            return;
        }

        _progressionInitialized = true;

        if (!_registeredCompanions.Contains(this))
        {
            _registeredCompanions.Add(this);
        }

        LoadProgressionFromStorage();
    }

    public void CleanupProgression()
    {
        if (!_progressionInitialized)
        {
            return;
        }

        SaveProgression();
        _progressionInitialized = false;
    }

    public void AddXP(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        EnsureProgressionInitialized();

        if (xpToNextLevel <= 0)
        {
            xpToNextLevel = CalculateXPRequirementForLevel(Mathf.Max(1, companionLevel));
        }

        int remaining = amount;
        while (remaining > 0)
        {
            int required = Mathf.Max(1, xpToNextLevel - currentXP);

            if (remaining >= required)
            {
                remaining -= required;
                currentXP += required;
                LevelUp();
            }
            else
            {
                currentXP = Mathf.Clamp(currentXP + remaining, 0, xpToNextLevel);
                remaining = 0;
            }
        }

        SaveProgression();
    }

    public float GetLevelProgress01()
    {
        EnsureProgressionInitialized();

        if (companionLevel >= MaxLevel)
        {
            return 1f;
        }

        if (xpToNextLevel <= 0)
        {
            return 0f;
        }

        return Mathf.Clamp01((float)currentXP / xpToNextLevel);
    }

    public static CompanionData GetCompanionByName(string targetName)
    {
        if (string.IsNullOrEmpty(targetName))
        {
            return null;
        }

        foreach (var companion in _registeredCompanions)
        {
            if (companion != null && companion.companionName == targetName)
            {
                return companion;
            }
        }

        return null;
    }

    private void LoadProgressionFromStorage()
    {
        string levelKey = GetLevelKey();
        string xpKey = GetXPKey();

        int savedLevel = PlayerPrefs.GetInt(levelKey, companionLevel);
        int savedXP = PlayerPrefs.GetInt(xpKey, currentXP);

        Debug.Log($"[CompanionData] LoadProgression: {companionName} LevelKey={levelKey} XPKey={xpKey} SavedLevel={savedLevel} SavedXP={savedXP}");

        companionLevel = Mathf.Clamp(savedLevel, 1, MaxLevel);
        xpToNextLevel = companionLevel >= MaxLevel ? 1 : CalculateXPRequirementForLevel(companionLevel);

        currentXP = Mathf.Clamp(savedXP, 0, Mathf.Max(0, xpToNextLevel));

        NormalizeOverflowXP();
    }

    private void LevelUp()
    {
        companionLevel = Mathf.Min(MaxLevel, companionLevel + 1);

        if (companionLevel >= MaxLevel)
        {
            xpToNextLevel = 1;
            currentXP = xpToNextLevel;
        }
        else
        {
            currentXP = 0;
            xpToNextLevel = CalculateXPRequirementForLevel(companionLevel);
        }
    }

    private void NormalizeOverflowXP()
    {
        int overflowXP = Mathf.Max(0, currentXP);
        int workingLevel = Mathf.Max(1, companionLevel);

        int requirement = CalculateXPRequirementForLevel(workingLevel);

        while (requirement > 0 && overflowXP >= requirement && workingLevel < MaxLevel)
        {
            overflowXP -= requirement;
            workingLevel = Mathf.Min(MaxLevel, workingLevel + 1);
            requirement = CalculateXPRequirementForLevel(workingLevel);
        }

        companionLevel = workingLevel;
        if (companionLevel >= MaxLevel)
        {
            xpToNextLevel = 1;
            currentXP = xpToNextLevel;
        }
        else
        {
            currentXP = Mathf.Clamp(overflowXP, 0, requirement);
            xpToNextLevel = Mathf.Max(1, requirement);
        }
    }

    private int CalculateXPRequirementForLevel(int level)
    {
        if (level >= MaxLevel)
        {
            return 1;
        }

        int baseRequirement = Mathf.Max(1, baseXPRequirement);
        float growth = Mathf.Max(1f, xpGrowthMultiplier);
        float exponent = Mathf.Max(0, level - 1);
        int requirement = Mathf.RoundToInt(baseRequirement * Mathf.Pow(growth, exponent));
        return Mathf.Max(1, requirement);
    }

    public void SaveProgression()
    {
        string levelKey = GetLevelKey();
        string xpKey = GetXPKey();
        int clampedLevel = Mathf.Clamp(companionLevel, 1, MaxLevel);
        int clampedXP = Mathf.Max(0, currentXP);

        PlayerPrefs.SetInt(levelKey, clampedLevel);
        PlayerPrefs.SetInt(xpKey, clampedXP);
        Debug.Log($"[CompanionData] SaveProgression: {companionName} LevelKey={levelKey} Level={clampedLevel} XPKey={xpKey} XP={clampedXP}");
        PlayerPrefs.Save();
    }

    private string GetLevelKey()
    {
        return $"CompanionProgress_{companionName}_Level";
    }

    private string GetXPKey()
    {
        return $"CompanionProgress_{companionName}_CurrentXP";
    }

    // Hook into ScriptableObject lifecycle for automatic persistence
    public virtual void OnEnable()
    {
        EnsureProgressionInitialized();
    }

    public virtual void OnDisable()
    {
        CleanupProgression();
    }
}
