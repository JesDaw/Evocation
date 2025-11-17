using UnityEngine;
using UnityEngine.Playables;



public class TimelineController : MonoBehaviour
{
    PlayableDirector timeline;
    float rewindTime;

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

    public void RewindTimeline()
    {
        timeline.time = rewindTime;
    }
}
