using UnityEngine;
using UnityEngine.Playables;



public class TimelineController : MonoBehaviour
{
    PlayableDirector timeline;
    private bool isPaused = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timeline = GetComponent<PlayableDirector>();
    }

    public void PlayTimeline()
    {
        timeline.Resume();
        isPaused = false;
    }

    public void PauseAnimation()
    {
        timeline.Pause();
        isPaused = true;
    }

    public void ResetTimeline()
    {
        timeline.Stop();
    }
}
