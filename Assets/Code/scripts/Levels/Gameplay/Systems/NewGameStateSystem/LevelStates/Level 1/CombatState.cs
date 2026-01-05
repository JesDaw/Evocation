using UnityEngine;

/// <summary>
/// Active combat phase. Enables all gameplay systems and switches control to player character.
/// Can be used for multiple combat phases by creating multiple instances with different configurations.
/// </summary>
[CreateAssetMenu(fileName = "State_Combat", menuName = "Level States/Combat State")]
public class CombatState : LevelState
{
    [Header("Combat Configuration")]
    [SerializeField] private int phaseNumber = 1;
    [Tooltip("Optional: Name of the combat phase for logging")]
    [SerializeField] private string phaseName = "Combat Phase";
    
    protected override void OnEnterState()
    {
        Debug.Log($"[CombatState] Starting {phaseName} {phaseNumber}");
        
        // Switch to player control if configured
        if (switchToPlayerControl)
        {
            var controlSwitcher = CameraControlSwitcher.Instance;
            if (controlSwitcher != null)
            {
                controlSwitcher.SwitchToPlayerControl();
            }
        }
    }
    
    protected override void OnExitState()
    {
        Debug.Log($"[CombatState] Ending {phaseName} {phaseNumber}");
    }
}