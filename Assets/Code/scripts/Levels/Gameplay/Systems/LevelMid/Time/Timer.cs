using UnityEngine;
using UnityEngine.Events;
using TMPro;


// Countdown timer functioality 
// takes a number of seconds and a UI object from unity interface
// convrts seconds to minuts and seconds and counts down to 0 from there 
// desplays time in provided UI object
public class Timer : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] UnityEvent _TimeHitZero;
    public FloatVariable remainingTimeSeconds;

    bool _timer_is_active = true;

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
    public void ActivateTimer(){ _timer_is_active = true;}

    public void ResetTimer() { remainingTimeSeconds.Reset(); }

    // conversion from seconds to minuts and seconds and desplays it in UI
    void DesplayTime()
    {
        int minutes = Mathf.FloorToInt(remainingTimeSeconds._Value / 60);
        int seconds = Mathf.FloorToInt(remainingTimeSeconds._Value % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
