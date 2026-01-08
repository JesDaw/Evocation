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
    [SerializeField] public float maxTimeRemaining = 1f; // PUBLIC for AI system
    [SerializeField] public FloatVariable remainingTimeSeconds; // PUBLIC for AI system
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] UnityEvent _TimeHitZero;
    [SerializeField] UnityEvent _TimeStarted;

    bool _timer_is_active = false;

    internal bool TimeIsActive
    {
        get { return _timer_is_active; }
        set { _timer_is_active = value;}
    }

    void Awake()
    {
        remainingTimeSeconds._Value = maxTimeRemaining;
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
        if (remainingTimeSeconds._Value > 0)
        {
            remainingTimeSeconds._Value -= Time.deltaTime;
        }
        else
        {
            remainingTimeSeconds._Value = 0;
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

    public void ResetTimer() { remainingTimeSeconds.Reset(); }

    // conversion from seconds to minutes and seconds and displays it in UI
    void DesplayTime()
    {
        int minutes = Mathf.FloorToInt(remainingTimeSeconds._Value / 60);
        int seconds = Mathf.FloorToInt(remainingTimeSeconds._Value % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}