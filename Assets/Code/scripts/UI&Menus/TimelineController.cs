using System.Diagnostics;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class TimelineController : MonoBehaviour
{
    PlayableDirector timeline;
    bool gameIsPaused;
    [SerializeField] bool _skipable = false;
    public UnityEvent SkipedTimeline;
    double heldTime;
    bool isHolding = false;

    
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

    public void SkipTimeline(InputAction.CallbackContext context)
    {
        if (!context.performed || !_skipable) return;
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