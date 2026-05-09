using UnityEngine;
using UnityEngine.Events;

public class CalenderManager : MonoBehaviour
{
    public static CalenderManager Instance { get; private set; }
    public DayCycle dayCycle;
    public CalenderEvent[] events;

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
    [SerializeField] UnityEvent[] dayEvent;
}
