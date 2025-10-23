// Attach this to your _Managers GameObject
using UnityEngine;

public class DontDestroyManagers : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }
}