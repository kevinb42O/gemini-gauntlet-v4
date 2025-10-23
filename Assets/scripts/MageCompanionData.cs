using UnityEngine;

[CreateAssetMenu(fileName = "MageCompanion", menuName = "Companion/Mage Companion")]
public class MageCompanionData : CompanionData
{
    void OnEnable()
    {
        companionName = "Mystic Mage";
        description = "A wise magical companion with high intelligence and special abilities.";
        attackPower = 12;
        defense = 10;
        speed = 11;
        accuracy = 16;
        intelligence = 20;
        loyalty = 14;
        courage = 8;
    }
}
