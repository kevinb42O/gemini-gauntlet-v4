using UnityEngine;

[CreateAssetMenu(fileName = "PowerupIconManager", menuName = "Game/Powerup Icon Manager")]
public class PowerupIconManager : ScriptableObject
{
    [Header("Powerup Icons")]
    [SerializeField] private Sprite maxHandUpgradeIcon;
    [SerializeField] private Sprite homingDaggersIcon;
    [SerializeField] private Sprite aoeAttackIcon;
    [SerializeField] private Sprite doubleGemsIcon;
    [SerializeField] private Sprite slowTimeIcon;
    [SerializeField] private Sprite godModeIcon;
    [SerializeField] private Sprite instantCooldownIcon;
    [SerializeField] private Sprite doubleDamageIcon;

    public static PowerupIconManager Instance { get; private set; }

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public Sprite GetIconForPowerupType(PowerUpType powerupType)
    {
        switch (powerupType)
        {
            case PowerUpType.MaxHandUpgrade:
                return maxHandUpgradeIcon;
            case PowerUpType.HomingDaggers:
                return homingDaggersIcon;
            case PowerUpType.AOEAttack:
                return aoeAttackIcon;
            case PowerUpType.DoubleGems:
                return doubleGemsIcon;
            case PowerUpType.SlowTime:
                return slowTimeIcon;
            case PowerUpType.GodMode:
                return godModeIcon;
            case PowerUpType.InstantCooldown:
                return instantCooldownIcon;
            case PowerUpType.DoubleDamage:
                return doubleDamageIcon;
            default:
                return null;
        }
    }

    // Alternative static method if Instance is not available
    public static Sprite GetIcon(PowerUpType powerupType, PowerupIconManager fallbackManager = null)
    {
        if (Instance != null)
        {
            return Instance.GetIconForPowerupType(powerupType);
        }
        else if (fallbackManager != null)
        {
            return fallbackManager.GetIconForPowerupType(powerupType);
        }
        
        return null;
    }
}
