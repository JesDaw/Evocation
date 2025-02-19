using UnityEngine;
using TMPro;

// Countdown timer functioality 
// takes a number of seconds and a UI object from unity interface
// convrts seconds to minuts and seconds and counts down to 0 from there 
// desplays time in provided UI object
public class Timer : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] float remainingTimeSeconds;
    void Update()
    {
        Countdown();
        DesplayTime();
    }

    //counts down that stops at 0
    void Countdown()
    {
        if (remainingTimeSeconds > 0)
        {
            remainingTimeSeconds -= Time.deltaTime; 
        }
        else 
        {
            remainingTimeSeconds = 0;
        }
    }

    // conversion from seconds to minuts and seconds and desplays it in UI
    void DesplayTime()
    {
        int minutes = Mathf.FloorToInt(remainingTimeSeconds / 60);
        int seconds = Mathf.FloorToInt(remainingTimeSeconds % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
