using UnityEngine;
using System;

[Serializable]
public class IntroState : LevelState
{
    [Header("Intro Configuration")]
    [SerializeField] private string introCutsceneName = "Intro";
    
    protected override void OnEnterState()
    {
        if (TimelineManager.Instance != null && TimelineManager.Instance.HasTimeline(introCutsceneName))
            TimelineManager.Instance.PlayCutscene(introCutsceneName);
        else
        {
            Debug.LogWarning("Just an FYI if the intro state doesnt play a cutscene the other scripts dont have enough time to set up and the controls wont work");
           context.TransitionToNextState(); 
        }
            
    }
}