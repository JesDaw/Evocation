using UnityEngine;
using System;

[Serializable]
public class CountdownState : LevelState
{
    [Header("Countdown Configuration")]
    [SerializeField] private string countdownCutsceneName = "Countdown";
    
    protected override void OnEnterState()
    {
        if (TimelineManager.Instance != null && TimelineManager.Instance.HasTimeline(countdownCutsceneName))
        {
            TimelineManager.Instance.PlayCutscene(countdownCutsceneName);
        }
        else
        {
            if (DebugLogs) Debug.LogWarning($"[CountdownState] Timeline '{countdownCutsceneName}' not found, moving to gameplay immediately");
            // No cutscene, move immediately to gameplay
            context.TransitionToNextState();
        }
    }
}