using UnityEngine;

/// <summary>
/// Plays the level intro cutscene. Automatically transitions to next state when cutscene ends.
/// Configure the cutscene name in the inspector to match your Timeline name.
/// </summary>
[CreateAssetMenu(fileName = "State_Intro", menuName = "Level States/Intro State")]
public class IntroState : LevelState
{
    [Header("Intro Configuration")]
    [SerializeField] private string introCutsceneName = "Intro";
    
    protected override void OnEnterState()
    {
        // Play intro cutscene
        if (TimelineManager.Instance != null && TimelineManager.Instance.HasTimeline(introCutsceneName))
        {
            TimelineManager.Instance.PlayCutscene(introCutsceneName);
        }
        else
        {
            Debug.LogWarning($"[IntroState] Timeline '{introCutsceneName}' not found, skipping to next state");
            context.TransitionToNextState();
        }
    }
}