using UnityEngine;
using FMODUnity;

public class FModEvents : MonoBehaviour
{
    [field: Header("Click SFX")]
    [field: SerializeField] public EventReference menuClick { get; private set; }
    [field: Header("Ambiance")]
    [field: SerializeField] public EventReference ambiance { get; private set; }
    [field: Header("Music")]
    [field: SerializeField] public EventReference music { get; private set; }
    
    public static FModEvents instance { get; private set; }

    void Awake()
    {
        if (instance != null && instance != this) 
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }
}