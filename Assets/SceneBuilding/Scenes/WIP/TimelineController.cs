using UnityEngine;
using UnityEngine.Playables;



public class TimelineController : MonoBehaviour
{
    private bool isPaused = false;
    PlayableDirector timeline;
    private bool isPaused = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timeline = GetComponent<PlayableDirector>();
    }

    void Update()
    {
        if (isPaused == true && Input.GetKeyDown(KeyCode.E))
        {
            timeline.Resume();
            isPaused = false;
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            timeline.Stop();
        }
    }

    public void PauseAnimation()
    {
        timeline.Pause();
        isPaused = true;
    }
}
