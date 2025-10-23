using UnityEngine;

[CreateAssetMenu(fileName = "ArcherCompanion", menuName = "Companion/Archer Companion")]
public class ArcherCompanionData : CompanionData
{
    void OnEnable()
    {
        companionName = "Shadow Archer";
        description = "A precise ranged companion with exceptional accuracy and stealth.";
        attackPower = 16;
        defense = 8;
        speed = 14;
        accuracy = 20;
        intelligence = 15;
        loyalty = 12;
        courage = 10;
    }
}
