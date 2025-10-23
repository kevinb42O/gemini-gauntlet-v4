using UnityEngine;

[CreateAssetMenu(fileName = "TankCompanion", menuName = "Companion/Tank Companion")]
public class TankCompanionData : CompanionData
{
    private void OnEnable()
    {
        companionName = "Tank Companion";
        description = "A heavily armored companion designed to take massive damage.";

        EnsureProgressionInitialized();

        if (!TryLoadSavedStats())
        {
            StatPointsManager.OnInitialized += HandleStatPointsManagerInitialized;
        }
    }

    private void OnDisable()
    {
        StatPointsManager.OnInitialized -= HandleStatPointsManagerInitialized;
    }

    private void HandleStatPointsManagerInitialized()
    {
        StatPointsManager.OnInitialized -= HandleStatPointsManagerInitialized;
        TryLoadSavedStats();
    }

    private bool TryLoadSavedStats()
    {
        // Debug.Log($"LoadSavedStats called for: {companionName}");

        const int MAX_STAT_VALUE = 20;  // Stat cap to keep values within inspector range

        // Try to load saved stats, but handle the case where StatPointsManager might not be initialized yet
        if (StatPointsManager.Instance != null)
        {
            // Debug.Log($"StatPointsManager found, loading saved stats...");

            int savedAttack = Mathf.Clamp(StatPointsManager.Instance.GetStatValue(companionName, "attackpower"), 0, MAX_STAT_VALUE);
            int savedDefense = Mathf.Clamp(StatPointsManager.Instance.GetStatValue(companionName, "defense"), 0, MAX_STAT_VALUE);
            int savedSpeed = Mathf.Clamp(StatPointsManager.Instance.GetStatValue(companionName, "speed"), 0, MAX_STAT_VALUE);
            int savedAccuracy = Mathf.Clamp(StatPointsManager.Instance.GetStatValue(companionName, "accuracy"), 0, MAX_STAT_VALUE);
            int savedIntelligence = Mathf.Clamp(StatPointsManager.Instance.GetStatValue(companionName, "intelligence"), 0, MAX_STAT_VALUE);
            int savedLoyalty = Mathf.Clamp(StatPointsManager.Instance.GetStatValue(companionName, "loyalty"), 0, MAX_STAT_VALUE);
            int savedCourage = Mathf.Clamp(StatPointsManager.Instance.GetStatValue(companionName, "courage"), 0, MAX_STAT_VALUE);

            // Use saved values directly
            attackPower = savedAttack;
            defense = savedDefense;
            speed = savedSpeed;
            accuracy = savedAccuracy;
            intelligence = savedIntelligence;
            loyalty = savedLoyalty;
            courage = savedCourage;

            // Debug.Log($"Loaded stats - Attack: {attackPower}, Defense: {defense}");

            // Tank companion has longest cooldown (5-6 hours)
            cooldownHours = Random.Range(5, 7);

            // Debug.Log($"Final stats for {companionName}: Attack={attackPower}, Defense={defense}, Speed={speed}, Accuracy={accuracy}, Intelligence={intelligence}, Loyalty={loyalty}, Courage={courage}");

            return true;
        }
        
        // Debug.LogWarning("StatPointsManager.Instance is null! Using zero values only. Will wait for initialization...");

        // Fallback to zero values if StatPointsManager doesn't exist yet
        attackPower = 0;
        defense = 0;
        speed = 0;
        accuracy = 0;
        intelligence = 0;
        loyalty = 0;
        courage = 0;

        // Tank companion has longest cooldown (5-6 hours)
        cooldownHours = Random.Range(5, 7);

        // Debug.Log($"Final stats for {companionName}: Attack={attackPower}, Defense={defense}, Speed={speed}, Accuracy={accuracy}, Intelligence={intelligence}, Loyalty={loyalty}, Courage={courage}");

        return false;
    }
}
