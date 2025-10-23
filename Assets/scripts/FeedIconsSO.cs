// --- FeedIconsSO.cs (ULTRA COMPLETE & FINAL) ---
using UnityEngine;

[CreateAssetMenu(fileName = "FeedIcons", menuName = "Game/UI/Feed Icons")]
public class FeedIconsSO : ScriptableObject
{
    [Header("General Icons")]
    public Sprite killSkull;
    public Sprite killTower;
    public Sprite killBoss;
    public Sprite gemCollected;

    [Header("Status Icons")]
    public Sprite levelUp;
    public Sprite levelDown;
    public Sprite handUnlocked; // For secondary hand
    public Sprite overheated;   // Generic overheat icon
    public Sprite recovered;    // Generic recovery icon

    [Header("PowerUp Icons")]
    public Sprite powerUpGeneric;
    public Sprite powerUpMaxHand;
    public Sprite powerUpHomingDaggers;
    public Sprite powerUpAOEAttack;
    public Sprite powerUpDoubleGems;
    public Sprite powerUpSlowTime;
    public Sprite powerUpGodMode;

    [Header("Misc System Icons")]
    public Sprite warningIcon; // General warning
    public Sprite infoIcon;    // General info


    public Sprite GetIconForPowerUp(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.MaxHandUpgrade: return powerUpMaxHand;
            case PowerUpType.HomingDaggers: return powerUpHomingDaggers;
            case PowerUpType.AOEAttack: return powerUpAOEAttack;
            case PowerUpType.DoubleGems: return powerUpDoubleGems;
            case PowerUpType.SlowTime: return powerUpSlowTime;
            case PowerUpType.GodMode: return powerUpGodMode;
            default: return powerUpGeneric;
        }
    }

    public enum KillFeedType { Skull, Tower, Boss }
    public Sprite GetIconForKillFeed(KillFeedType type)
    {
        switch (type)
        {
            case KillFeedType.Skull: return killSkull;
            case KillFeedType.Tower: return killTower;
            case KillFeedType.Boss: return killBoss;
            default: return killSkull; // Default to skull
        }
    }
}