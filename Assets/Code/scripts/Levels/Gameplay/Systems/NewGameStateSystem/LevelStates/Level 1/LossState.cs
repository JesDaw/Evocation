using UnityEngine;

/// <summary>
/// Level defeat state. Can optionally play a loss cutscene before showing defeat screen.
/// If no cutscene is configured, immediately shows the defeat UI.
/// </summary>
[CreateAssetMenu(fileName = "State_Loss", menuName = "Level States/Loss State")]
public class LossState : LevelState
{
    [Header("Loss Configuration")]
    [SerializeField] private string lossCutsceneName = "Defeat";
    [SerializeField] private bool hasLossCutscene = false;
    [SerializeField] private string defeatUIName = "Defeat";
    
    protected override void OnEnterState()
    {
        Debug.Log("[LossState] Level lost!");
        
        if (hasLossCutscene && TimelineManager.Instance != null && TimelineManager.Instance.HasTimeline(lossCutsceneName))
        {
            // Disable auto-transition for loss cutscene since we handle it manually
            TimelineManager.Instance.PlayCutscene(lossCutsceneName);
            
            // Show defeat screen after cutscene ends
            // (This will be called by Timeline's stopped event via UltEvent)
        }
        else
        {
            // No cutscene, show defeat screen immediately
            ShowDefeatScreen();
        }
    }
    
    /// <summary>
    /// Call this from Timeline Signal or UltEvent to show defeat screen after cutscene
    /// </summary>
    public void ShowDefeatScreen()
    {
        Time.timeScale = 0;
        GlobalInputManager.Instance.SetPauseMenuMode();
        context.SceneManager.Activate(defeatUIName);
        
        Debug.Log("[LossState] Defeat screen displayed");
    }
}