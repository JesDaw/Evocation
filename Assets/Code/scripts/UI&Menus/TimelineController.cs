using System.Diagnostics;
using UnityEngine;
using UnityEngine.Playables;

public class TimelineController : MonoBehaviour
{
    PlayableDirector timeline;
    bool gameIsPaused;
    float _jumpPoint;
    double heldTime;
    bool isHolding = false; 
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

    public void PauseTimeline()
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

    public void PauseGame()
    {
        if (timeline.playableGraph.IsValid())
        {
            if (isHolding)
            {
                return;
            }
            else
            {
                timeline.playableGraph.GetRootPlayable(0).SetSpeed(0);
                heldTime = timeline.time;
                gameIsPaused = true;
            }
        }
    }

    public void UnpauseGame()
    {
        if (timeline.playableGraph.IsValid())
        {
            if (isHolding)
            {
                return;
            }
            else
            {
                timeline.playableGraph.GetRootPlayable(0).SetSpeed(1);
                gameIsPaused = false;
                timeline.Resume();
            }
        }
    }

    public void JumpTimelineToPoint()
    {
        timeline.time = _jumpPoint;
    }

    void LateUpdate()
    {
        if (gameIsPaused)
        {
            timeline.time = heldTime;
            return;
        }
        
        if (isHolding && timeline.playableGraph.IsValid())
        {
            timeline.time = heldTime;
        }
    }
}