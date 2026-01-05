using UnityEngine;

/// <summary>
/// Plays a countdown cutscene before combat begins (e.g. "3, 2, 1, GO!").
/// Automatically transitions to next state when countdown completes.
/// </summary>
[CreateAssetMenu(fileName = "State_Countdown", menuName = "Level States/Countdown State")]
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
            Debug.LogWarning($"[CountdownState] Timeline '{countdownCutsceneName}' not found, moving to gameplay immediately");
            // No cutscene, move immediately to gameplay
            context.TransitionToNextState();
        }
    }
}