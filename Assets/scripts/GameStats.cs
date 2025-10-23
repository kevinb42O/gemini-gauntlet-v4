// --- GameStats.cs (Ensured CurrentGameTotalEnemiesKilled is present) ---
using UnityEngine;

public static class GameStats
{
    // --- Stats from the LAST COMPLETED/EXFILLED run ---
    public static int LastExfilSkullKills { get; private set; } = 0;
    public static int LastExfilTowerKills { get; private set; } = 0;
    public static int LastExfilGemsCollected { get; private set; } = 0;
    public static float LastExfilSurvivalTimeSeconds { get; private set; } = 0f;
    public static int LastExfilBossKills { get; private set; } = 0;
    public static int LastExfilBossMinionKills { get; private set; } = 0;
    public static int LastExfilTotalXP { get; private set; } = 0;

    // --- Stats for the CURRENT ONGOING run ---
    public static int CurrentRunSkullKills { get; private set; } = 0;
    public static int CurrentRunTowerKills { get; private set; } = 0;
    public static int CurrentRunGemsCollected { get; private set; } = 0;
    public static float CurrentRunSurvivalTimeSeconds { get; private set; } = 0f;
    public static int CurrentRunBossKills { get; private set; } = 0;
    public static int CurrentRunBossMinionKills { get; private set; } = 0;

    // --- XP Values per unit ---
    public const int XP_PER_SKULL_KILL = 15;
    public const int XP_PER_TOWER_KILL = 50;
    public const int XP_PER_GEM_COLLECTED = 20;
    public const int XP_PER_SECOND_SURVIVED = 1;
    public const int XP_PER_BOSS_KILL = 100;
    public const int XP_PER_BOSS_MINION_KILL = 30;

    // --- General Kill Counter for the CURRENT RUN (used for Game Over display) ---
    public static int CurrentGameTotalEnemiesKilled { get; private set; } = 0; // THIS IS THE FIELD IN QUESTION

    // --- Methods to update CURRENT RUN stats ---
    public static void AddSkullKillToCurrentRun() { CurrentRunSkullKills++; CurrentGameTotalEnemiesKilled++; }
    public static void AddTowerKillToCurrentRun() { CurrentRunTowerKills++; CurrentGameTotalEnemiesKilled++; }
    public static void AddGemCollectedToCurrentRun() { CurrentRunGemsCollected++; }
    public static void AddBossKillToCurrentRun() { CurrentRunBossKills++; CurrentGameTotalEnemiesKilled++; }
    public static void AddBossMinionKillToCurrentRun() { CurrentRunBossMinionKills++; CurrentGameTotalEnemiesKilled++; }
    public static void UpdateCurrentRunSurvivalTime(float time) { CurrentRunSurvivalTimeSeconds = time; }

    public static void RecordSuccessfulExfilStats(float finalSurvivalTime)
    {
        UpdateCurrentRunSurvivalTime(finalSurvivalTime);
        LastExfilSkullKills = CurrentRunSkullKills;
        LastExfilTowerKills = CurrentRunTowerKills;
        LastExfilGemsCollected = CurrentRunGemsCollected;
        LastExfilSurvivalTimeSeconds = CurrentRunSurvivalTimeSeconds;
        LastExfilBossKills = CurrentRunBossKills;
        LastExfilBossMinionKills = CurrentRunBossMinionKills;

        int skullXp = LastExfilSkullKills * XP_PER_SKULL_KILL;
        int towerXp = LastExfilTowerKills * XP_PER_TOWER_KILL;
        int gemXp = LastExfilGemsCollected * XP_PER_GEM_COLLECTED;
        int timeXp = Mathf.FloorToInt(LastExfilSurvivalTimeSeconds * XP_PER_SECOND_SURVIVED);
        int bossXp = LastExfilBossKills * XP_PER_BOSS_KILL;
        int minionXp = LastExfilBossMinionKills * XP_PER_BOSS_MINION_KILL;
        LastExfilTotalXP = skullXp + towerXp + gemXp + timeXp + bossXp + minionXp;

        // Clear "LastGame" death stats as exfil was successful
        LastGameEnemiesKilled = 0;
        LastGameTotalGemsCollected = 0;
        LastGamePrimaryHandLevelReached = 0;
        LastGameSecondaryHandLevelReached = 0;
        LastGameSurvivalTimeSeconds = 0f;

        Debug.Log($"GameStats: Successful Exfil. Skulls: {LastExfilSkullKills}({skullXp}XP), Towers: {LastExfilTowerKills}({towerXp}XP), Gems: {LastExfilGemsCollected}({gemXp}XP), Time: {LastExfilSurvivalTimeSeconds:F1}s({timeXp}XP), Bosses: {LastExfilBossKills}({bossXp}XP), Minions: {LastExfilBossMinionKills}({minionXp}XP). TOTAL XP: {LastExfilTotalXP}");
    }

    // --- Stats for Game Over (Player Death) ---
    public static int LastGameEnemiesKilled { get; private set; } = 0;
    public static int LastGameTotalGemsCollected { get; private set; } = 0;
    public static int LastGamePrimaryHandLevelReached { get; private set; } = 1;
    public static int LastGameSecondaryHandLevelReached { get; private set; } = 1;
    public static float LastGameSurvivalTimeSeconds { get; private set; } = 0f;

    // MODIFIED Method Signature to accept individual hand levels
    public static void RecordPlayerDeathStats(int totalEnemiesKilledThisRun, int totalGemsFromRun, int primaryHandLevel, int secondaryHandLevel, float survivalTime)
    {
        LastGameEnemiesKilled = totalEnemiesKilledThisRun; // This uses the passed-in value
        LastGameTotalGemsCollected = totalGemsFromRun;
        LastGamePrimaryHandLevelReached = primaryHandLevel;
        LastGameSecondaryHandLevelReached = secondaryHandLevel;
        LastGameSurvivalTimeSeconds = survivalTime;

        // Clear "LastExfil" stats as player died
        LastExfilSkullKills = 0;
        LastExfilTowerKills = 0;
        LastExfilGemsCollected = 0;
        LastExfilSurvivalTimeSeconds = 0f;
        LastExfilBossKills = 0;
        LastExfilBossMinionKills = 0;
        LastExfilTotalXP = 0;

        Debug.Log($"GameStats: Player DEATH. Total Kills: {LastGameEnemiesKilled}, Total Gems: {LastGameTotalGemsCollected}, L.Hand Lvl: {LastGamePrimaryHandLevelReached}, R.Hand Lvl: {LastGameSecondaryHandLevelReached}, Time: {LastGameSurvivalTimeSeconds:F1}s");
    }

    public static void ResetStatsForNewRun()
    {
        CurrentRunSkullKills = 0;
        CurrentRunTowerKills = 0;
        CurrentRunGemsCollected = 0;
        CurrentRunSurvivalTimeSeconds = 0f;
        CurrentRunBossKills = 0;
        CurrentRunBossMinionKills = 0;
        CurrentGameTotalEnemiesKilled = 0; // Reset this critical field
        // LastExfil... and LastGame... stats are NOT reset here. They hold the summary of the *previous* run.
        // Debug.Log("GameStats: All current run/game stats reset for a new game.");
    }
}