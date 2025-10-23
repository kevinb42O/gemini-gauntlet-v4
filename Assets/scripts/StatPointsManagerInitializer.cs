using UnityEngine;

public class StatPointsManagerInitializer : MonoBehaviour
{
    void Awake()
    {
        // Create StatPointsManager if it doesn't exist
        if (StatPointsManager.Instance == null)
        {
            GameObject managerObject = new GameObject("StatPointsManager");
            managerObject.AddComponent<StatPointsManager>();
            DontDestroyOnLoad(managerObject);
        }
    }
}
