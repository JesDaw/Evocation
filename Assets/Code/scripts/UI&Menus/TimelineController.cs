using UnityEngine;
using UnityEngine.Playables;



public class TimelineController : MonoBehaviour
{
    PlayableDirector timeline;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timeline = GetComponent<PlayableDirector>();
    }

    public void PlayTimeline()
    {
        timeline.Resume();
    }

    public void PauseAnimation()
    {
        timeline.Pause();
    }

    public void ResetTimeline()
    {
        timeline.Stop();
    }
}
