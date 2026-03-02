using UnityEngine;
using System;

[Serializable]
public class WinState : LevelState
{
    [Header("Win Configuration")]
    [SerializeField] string winCutsceneName = "Victory";
    [SerializeField] bool hasWinCutscene = false;
    [SerializeField] string victoryUIName = "Victory";
    
    protected override void OnEnterState()
    {
        if (hasWinCutscene && TimelineManager.Instance != null && TimelineManager.Instance.HasTimeline(winCutsceneName))
            TimelineManager.Instance.PlayCutscene(winCutsceneName);
        else
            ShowVictoryScreen();

        SaveSystem.SaveGame(0); // Level 1 = index 0
    }
    
    public void ShowVictoryScreen()
    {
        Time.timeScale = 0;
        GlobalInputManager.Instance.SetPauseMenuMode();
        context.SceneManager.Activate(victoryUIName);
    }
}