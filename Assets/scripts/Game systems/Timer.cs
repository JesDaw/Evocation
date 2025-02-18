using UnityEngine;
using TMPro;

public class gameplaySystems : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] float remainingTimeSeconds;
    void Update()
    {
        if (remainingTimeSeconds > 0)
        {
            remainingTimeSeconds -= Time.deltaTime; 
        }
        else if (remainingTimeSeconds < 0)
        {
            remainingTimeSeconds = 0;
        }

        
       int minutes = Mathf.FloorToInt(remainingTimeSeconds / 60);
       int seconds = Mathf.FloorToInt(remainingTimeSeconds % 60);

        timerText.text = string.Format("{0:00}:{1:00}",minutes, seconds);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
  
}
