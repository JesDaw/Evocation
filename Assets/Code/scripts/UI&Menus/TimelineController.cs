using UnityEngine;
using UnityEngine.Playables;

public class TimelineController : MonoBehaviour
{
    PlayableDirector timeline;
    float rewindTime;
    private double heldTime;
    private bool isHolding = false; // Track if we're intentionally holding

    void Start()
    {
        timeline = GetComponent<PlayableDirector>();
    }

    public void PlayTimeline()
    {
        isHolding = false;
        if (timeline.playableGraph.IsValid())
        {
            timeline.playableGraph.GetRootPlayable(0).SetSpeed(1);
        }
        timeline.Resume();
    }

    public void PauseAnimation()
    {
        if (timeline.playableGraph.IsValid())
        {
            timeline.playableGraph.GetRootPlayable(0).SetSpeed(0);
            heldTime = timeline.time;
            isHolding = true;
        }
    }

    public void ResetTimeline()
    {
        isHolding = false;
        timeline.Stop();
    }

    public void RewindTimeline()
    {
        timeline.time = rewindTime;
    }

    void LateUpdate()
    {
        // Only try to hold if we're intentionally holding AND the graph is valid
        if (isHolding && timeline.playableGraph.IsValid())
        {
            timeline.time = heldTime;
        }
    }
}