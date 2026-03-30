using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class TimelineController : MonoBehaviour
{
    PlayableDirector timeline;
    
    [SerializeField] bool _skipable = false;
    public UnityEvent SkipedTimeline;
    bool gameIsPaused; // if the game is pause stop the timeline
    double heldTime;
    bool isHolding = false; // the timeline can be stopped independantly of if the game is paused though
    [SerializeField] bool ShowDebugLogs = false;

    
    void Start()
    {
        timeline = GetComponent<PlayableDirector>();
        if (GlobalInputManager.Instance != null)
        {
            SubscribeToInputs();
        }
    }

    void OnEnable()
    {
        
    }

    void OnDisable()
    {
        // Unsubscribe when disabled
        UnsubscribeFromInputs();
    }

    void SubscribeToInputs()
    {
        if (GlobalInputManager.Instance == null) return;

        var uiActions = GlobalInputManager.Instance.InputActions.UI;
        uiActions.SkipCutscene.performed += SkipTimeline;
    }

    void UnsubscribeFromInputs()
    {
        if (GlobalInputManager.Instance == null) return;

        var uiActions = GlobalInputManager.Instance.InputActions.UI;
        uiActions.SkipCutscene.performed -= SkipTimeline;
    }

    public void PlayTimeline()
    {
        if (TimelineManager.Instance != null)
        {
            if (TimelineManager.Instance.showDebugLogs == true || ShowDebugLogs) Debug.Log($"[timelineControler] Playing timeline");
        }
        isHolding = false;
        if (timeline.playableGraph.IsValid())
        {
            timeline.playableGraph.GetRootPlayable(0).SetSpeed(1);
        }
        timeline.Resume();
    }

    public void PauseTimeline()
    {
        if (TimelineManager.Instance != null)
        {
            if (TimelineManager.Instance.showDebugLogs == true || ShowDebugLogs) Debug.Log($"[timelineControler] Pausing timeline");
        }
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
                PauseTimeline();
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
                PlayTimeline();
                gameIsPaused = false;
            }
        }
    }

    public void SkipTimeline(InputAction.CallbackContext context)
    {
        if (!context.performed || !_skipable || timeline == null || timeline.state != PlayState.Playing) return;
        SkipTimeline();
    }

    public void SkipTimeline()
    {
        ResetTimeline();
        SkipedTimeline?.Invoke();
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