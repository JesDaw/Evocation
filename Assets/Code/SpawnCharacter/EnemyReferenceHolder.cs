using UnityEngine;

public class EnemyReferenceHolder : MonoBehaviour
{
    public static Transform Instance;

    void Awake()
    {
        Instance = this.transform;
    }
}
