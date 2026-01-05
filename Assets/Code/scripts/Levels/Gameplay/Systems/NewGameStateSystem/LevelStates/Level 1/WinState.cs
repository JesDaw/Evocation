using UnityEngine;

/// <summary>
/// Level victory state. Can optionally play a win cutscene before showing victory screen.
/// If no cutscene is configured, immediately shows the victory UI.
/// </summary>
[CreateAssetMenu(fileName = "State_Win", menuName = "Level States/Win State")]
public class WinState : LevelState
{
    [Header("Win Configuration")]
    [SerializeField] private string winCutsceneName = "Victory";
    [SerializeField] private bool hasWinCutscene = false;
    [SerializeField] private string victoryUIName = "Victory";
    
    protected override void OnEnterState()
    {
        Debug.Log("[WinState] Level won!");
        
        if (hasWinCutscene && TimelineManager.Instance != null && TimelineManager.Instance.HasTimeline(winCutsceneName))
        {
            // Disable auto-transition for win cutscene since we handle it manually
            TimelineManager.Instance.PlayCutscene(winCutsceneName);
            
            // Show victory screen after cutscene ends
            // (This will be called by Timeline's stopped event via UltEvent)
        }
        else
        {
            // No cutscene, show victory screen immediately
            ShowVictoryScreen();
        }
    }
    
    /// <summary>
    /// Call this from Timeline Signal or UltEvent to show victory screen after cutscene
    /// </summary>
    public void ShowVictoryScreen()
    {
        Time.timeScale = 0;
        GlobalInputManager.Instance.SetPauseMenuMode();
        context.SceneManager.Activate(victoryUIName);
        
        Debug.Log("[WinState] Victory screen displayed");
    }
}