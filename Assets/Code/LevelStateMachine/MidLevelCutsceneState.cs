using UnityEngine;
using UnityEngine.InputSystem;
using System;

[Serializable]
public class MidLevelCutsceneState : LevelState
{
    [Header("Intro Configuration")]
    [SerializeField] private string introCutsceneName = "MidLevelCutscene";
    
    protected override void OnEnterState()
    {
        if (TimelineManager.Instance != null && TimelineManager.Instance.HasTimeline(introCutsceneName))
            TimelineManager.Instance.PlayCutscene(introCutsceneName);
        else
            context.TransitionToNextState();
    }
}