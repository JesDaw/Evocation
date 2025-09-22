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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
            timeline.Play();
        if (Input.GetKeyDown(KeyCode.X))
            timeline.Pause();
    }

    //public void PlayTimeline()
    //{
    //    timeline.Play();
    //}
}
