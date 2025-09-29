using UnityEngine;
using UnityEngine.Playables;

private bool isPaused = false;

public class TimelineController : MonoBehaviour
{
    PlayableDirector timeline;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timeline = GetComponent<PlayableDirector>();
    }

    //void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Z))
    //        timeline.Play();
    //    if (Input.GetKeyDown(KeyCode.X))
    //        timeline.Pause();
    //}

    public void TogglePause()
    {
        timeline.Resume();
        isPaused = !isPaused;
    }

    public void PauseAnimation()
    {
        timeline.Pause();
        isPaused = !isPaused;
    }
    //private bool IsInputAllowed()
    //{
    //    return timeline.state != PlayState.Playing;
    //}
}
