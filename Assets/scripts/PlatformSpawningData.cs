using UnityEngine;
using System.Collections.Generic;

public class PlatformSpawningData : MonoBehaviour
{
    [Header("Spawning Configuration")]
    public List<GameObject> allowedTowerPrefabs = new List<GameObject>();

    [Tooltip("Override the default max towers for this specific platform. Set to 0 to use default.")]
    public int maxTowersOverride = 0;

    [Tooltip("An index to group platforms, e.g., by the ring they belong to.")]
    public int ringIndex = 0;
}
