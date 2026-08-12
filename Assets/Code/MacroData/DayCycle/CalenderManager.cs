using UnityEngine;


public class CalenderManager : MonoBehaviour
{
    public static CalenderManager Instance { get; private set; }
    public DayCycle dayCycle;
    

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void IncrementActionCounter()
    {
        dayCycle.IncrementActionCounter();
    }

 


}


