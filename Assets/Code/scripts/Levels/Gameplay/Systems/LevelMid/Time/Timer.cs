using UnityEngine;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Countdown timer functionality
/// Takes a number of seconds and a UI object from unity interface
/// Converts seconds to minutes and seconds and counts down to 0 from there
/// Displays time in provided UI object
/// </summary>
public class Timer : MonoBehaviour
{
    [SerializeField] public float maxTimeRemaining = 1f;
    [HideInInspector] public float RemainingTimeSeconds;
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] UnityEvent _TimeHitZero;
    [SerializeField] UnityEvent _TimeStarted;

    bool _timer_is_active = false;

    internal bool TimeIsActive
    {
        get { return _timer_is_active; }
        set { _timer_is_active = value;}
    }
    public static Timer Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        RemainingTimeSeconds = maxTimeRemaining;
        if (timerText == null)
        {
            GameObject timerTextObj = GameObject.Find("TimerText");
            if (timerTextObj == null) Debug.LogError("TimerManager could not find the TimerText game object");
            timerText = timerTextObj.GetComponent<TextMeshProUGUI>();
        }
    }

    void Update()
    {
        if (_timer_is_active)
        {
            Countdown();
        }
        DesplayTime();
    }

    //counts down that stops at 0
    void Countdown()
    {
        if (RemainingTimeSeconds > 0)
        {
            RemainingTimeSeconds -= Time.deltaTime;
        }
        else
        {
            RemainingTimeSeconds = 0;
            _TimeHitZero?.Invoke();
            DeactivateTimer();   
        }
    }

    public void DeactivateTimer(){ _timer_is_active = false; }
    public void ActivateTimer()
    { 
        _timer_is_active = true;
        _TimeStarted?.Invoke();
    }

    // conversion from seconds to minutes and seconds and displays it in UI
    void DesplayTime()
    {
        int minutes = Mathf.FloorToInt(RemainingTimeSeconds / 60);
        int seconds = Mathf.FloorToInt(RemainingTimeSeconds % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void ResetTimer()
    {
        RemainingTimeSeconds = maxTimeRemaining;
    }
}