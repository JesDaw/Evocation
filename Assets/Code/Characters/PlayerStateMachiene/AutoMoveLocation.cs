using UnityEngine;

public class AutoMoveLocation : MonoBehaviour
{
     public static AutoMoveLocation Instance { get; private set; }
     [HideInInspector] public Vector3 Location;
     void Awake()
    {
        Instance = this;
        Location = gameObject.transform.position;
    } 
}
