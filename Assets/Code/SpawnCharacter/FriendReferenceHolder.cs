using UnityEngine;

public class FriendReferenceHolder : MonoBehaviour
{
    public static Transform Instance;

    void Awake()
    {
        Instance = this.transform;
    }
}
