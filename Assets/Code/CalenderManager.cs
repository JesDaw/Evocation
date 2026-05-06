using UnityEngine;
using UnityEngine.Events;

public class CalenderManager : MonoBehaviour
{
    public static CalenderManager Instance { get; private set; }
    DayCycle dayCycle;
    public CalenderEvent[] events;
    [SerializeField] UnityEvent[] dayEvent;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void isEventDay()
    {
        // Still needs work
    }
}

[System.Serializable]
public class CalenderEvent
{
    public int day;
    public int month;
}
