using UnityEngine;

[CreateAssetMenu(fileName = "ScoutCompanion", menuName = "Companion/Scout Companion")]
public class ScoutCompanionData : CompanionData
{
    void OnEnable()
    {
        companionName = "Swift Scout";
        description = "A fast and agile companion perfect for reconnaissance and quick strikes.";
        attackPower = 14;
        defense = 6;
        speed = 20;
        accuracy = 12;
        intelligence = 16;
        loyalty = 18;
        courage = 15;
    }
}
