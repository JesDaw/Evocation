using UnityEngine;
using System;

[Serializable]
public class LossState : LevelState
{
    [Header("Loss Configuration")]
    [SerializeField] private string lossCutsceneName = "Defeat";
    [SerializeField] private bool hasLossCutscene = false;
    [SerializeField] private string defeatUIName = "Defeat";
    
    protected override void OnEnterState()
    {
        if (hasLossCutscene && TimelineManager.Instance != null && TimelineManager.Instance.HasTimeline(lossCutsceneName))
            TimelineManager.Instance.PlayCutscene(lossCutsceneName);
        else
            ShowDefeatScreen();
    }
    
    public void ShowDefeatScreen()
    {
        Time.timeScale = 0;
        GlobalInputManager.Instance.SetPauseMenuMode();
        context.SceneManager.Activate(defeatUIName);
    }
}