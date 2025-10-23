// --- PlayerRunStats.cs (Corrected for Milestone Tracking) ---
using UnityEngine;
using System;

public class PlayerRunStats : MonoBehaviour
{
    public static PlayerRunStats Instance { get; private set; }

    [Header("Base Stats (Defaults)")]
    public float baseMoveSpeed = 12f;
    public float baseNormalJumpHeight = 1.5f;

    [Header("Current Run Bonuses (Upgraded In-Run)")]
    [SerializeField]
    private float _currentMoveSpeedBonus = 0f;
    public float CurrentMoveSpeedBonus => _currentMoveSpeedBonus;

    [SerializeField]
    private float _currentJumpHeightBonus = 0f;
    public float CurrentJumpHeightBonus => _currentJumpHeightBonus;

    // --- Milestone Tracking Fields ---
    [Header("Milestone Tracking")]
    public int skullsKilledThisRun = 0;
    public int towersDestroyedThisRun = 0;
    public int gemsCollectedThisRun = 0;

    private int nextSkullMilestone = 25;
    private int nextTowerMilestone = 5;
    private int nextGemMilestone = 25;

    public float GetEffectiveMoveSpeed() => baseMoveSpeed + _currentMoveSpeedBonus;
    public float GetEffectiveNormalJumpHeight() => baseNormalJumpHeight + _currentJumpHeightBonus;

    public static event Action OnPlayerRunStatsChanged;
    // --- Milestone Events ---
    public static event Action<int> OnSkullKillMilestoneReached;
    public static event Action<int> OnTowerDestroyedMilestoneReached;
    public static event Action<int> OnGemCollectMilestoneReached;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        ResetStats();
    }

    public void ResetStats()
    {
        _currentMoveSpeedBonus = 0f;
        _currentJumpHeightBonus = 0f;

        skullsKilledThisRun = 0;
        towersDestroyedThisRun = 0;
        gemsCollectedThisRun = 0;
        nextSkullMilestone = 25;
        nextTowerMilestone = 5;
        nextGemMilestone = 25;

        OnPlayerRunStatsChanged?.Invoke();
    }

    public void ApplyMoveSpeedBonus(float bonusAmount)
    {
        _currentMoveSpeedBonus += bonusAmount;
        OnPlayerRunStatsChanged?.Invoke();
    }

    public void ApplyJumpHeightBonus(float bonusAmount)
    {
        _currentJumpHeightBonus += bonusAmount;
        OnPlayerRunStatsChanged?.Invoke();
    }

    public void RegisterSkullKill()
    {
        skullsKilledThisRun++;
        if (skullsKilledThisRun >= nextSkullMilestone)
        {
            OnSkullKillMilestoneReached?.Invoke(skullsKilledThisRun);
            if (nextSkullMilestone == 25) nextSkullMilestone = 50;
            else if (nextSkullMilestone == 50) nextSkullMilestone = 100;
            else nextSkullMilestone += 100;
        }
    }

    public void RegisterTowerKill()
    {
        towersDestroyedThisRun++;
        if (towersDestroyedThisRun >= nextTowerMilestone)
        {
            OnTowerDestroyedMilestoneReached?.Invoke(towersDestroyedThisRun);
            if (nextTowerMilestone == 5) nextTowerMilestone = 10;
            else nextTowerMilestone += 10;
        }
    }

    public void RegisterGemCollect()
    {
        gemsCollectedThisRun++;
        if (gemsCollectedThisRun >= nextGemMilestone)
        {
            OnGemCollectMilestoneReached?.Invoke(gemsCollectedThisRun);
            if (nextGemMilestone == 25) nextGemMilestone = 50;
            else nextGemMilestone += 50;
        }
    }
}