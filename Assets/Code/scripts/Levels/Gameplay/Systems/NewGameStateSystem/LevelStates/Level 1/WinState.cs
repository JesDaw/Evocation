using UnityEngine;
using System;

[Serializable]
public class WinState : LevelState
{
    [Header("Win Configuration")]
    [SerializeField] private string winCutsceneName = "Victory";
    [SerializeField] private bool hasWinCutscene = false;
    [SerializeField] private string victoryUIName = "Victory";
    
    protected override void OnEnterState()
    {
        if (hasWinCutscene && TimelineManager.Instance != null && TimelineManager.Instance.HasTimeline(winCutsceneName))
            TimelineManager.Instance.PlayCutscene(winCutsceneName);
        else
            ShowVictoryScreen();
    }
    
    public void ShowVictoryScreen()
    {
        Time.timeScale = 0;
        GlobalInputManager.Instance.SetPauseMenuMode();
        context.SceneManager.Activate(victoryUIName);
    }
}