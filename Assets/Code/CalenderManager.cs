using UnityEngine;

public class CalenderManager : MonoBehaviour
{
    public static CalenderManager Instance { get; private set; }
    private DayCycle dayCycle;
    private CalenderEvent[] events;
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
        return dayCycle != null;
    }
}

[System.Serializable]
public class CalenderEvent
{
    public int day;
    public int month;
    UnityEvent dayEvent;

    public void Invoke()
    {
        dayEvent.Invoke();
    }
}
