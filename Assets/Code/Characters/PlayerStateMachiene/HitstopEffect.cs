using UnityEngine;

public class HitstopEffect : MonoBehaviour
{
    [SerializeField] float hitstopDuration;
    [SerializeField] bool debug;
    bool active = false;
    float pauseTime;

    public void Apply()
    {
        if(debug) Debug.Log("Hitstop Active!");
        Time.timeScale = 0.0f;
        pauseTime = Time.realtimeSinceStartup + hitstopDuration;
        active = true;
    }

    public void Update()
    {
        if(!active) return;
        if(Time.realtimeSinceStartup > pauseTime)
        {
            active = false;
            Time.timeScale = 1.0f;
        }
    }
}
