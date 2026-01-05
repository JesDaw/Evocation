using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Free camera exploration phase. Player can look around the battlefield before engaging.
/// Press the engage button once to show confirmation UI, press again to start combat.
/// Press return to cancel and go back to scouting.
/// </summary>
[CreateAssetMenu(fileName = "State_Scouting", menuName = "Level States/Scouting State")]
public class ScoutingState : LevelState
{
    [Header("Scouting Configuration")]
    [SerializeField] private string confirmationUIName = "ConfirmationUI";
    
    private bool confirmationUIActive = false;
    private bool isInitialized = false;
    
    public override void Initialize(LevelStateManager manager)
    {
        base.Initialize(manager);
        
        if (!isInitialized)
        {
            // Subscribe to engagement button
            if (GlobalInputManager.Instance != null)
            {
                var uiActions = GlobalInputManager.Instance.InputActions.UI;
                uiActions.StartEngaugment.performed += OnEngagePressed;
                uiActions.Return.performed += OnReturnPressed;
            }
            isInitialized = true;
        }
    }
    
    protected override void OnEnterState()
    {
        confirmationUIActive = false;
        ToggleInputListeners(true);
    }
    
    protected override void OnExitState()
    {
        ToggleInputListeners(false);
        confirmationUIActive = false;
    }
    
    private void ToggleInputListeners(bool enroll)
    {
        if (GlobalInputManager.Instance == null) return;

        var uiActions = GlobalInputManager.Instance.InputActions.UI;

        if (enroll)
        {
            uiActions.StartEngaugment.performed += OnEngagePressed;
            uiActions.Return.performed += OnReturnPressed;
        }
        else
        {
            uiActions.StartEngaugment.performed -= OnEngagePressed;
            uiActions.Return.performed -= OnReturnPressed;
        }
    }

    private void OnEngagePressed(InputAction.CallbackContext ctx)
    {
        // Only respond if we're in this state
        if (context.CurrentState != this) return;
        if (!ctx.performed) return;
        
        if (!confirmationUIActive)
        {
            // Show confirmation UI
            context.SceneManager.Activate(confirmationUIName);
            GlobalInputManager.Instance.SetCharacterSelectingMode();
            confirmationUIActive = true;
        }
        else
        {
            // Confirm and move to next state
            context.TransitionToNextState();
        }
    }
    
    private void OnReturnPressed(InputAction.CallbackContext ctx)
    {
        // Only respond if we're in this state
        if (context.CurrentState != this) return;
        if (!ctx.performed || !confirmationUIActive) return;
        
        // Return to scouting
        context.SceneManager.Activate(uiCanvasName);
        GlobalInputManager.Instance.SetScoutingMode();
        confirmationUIActive = false;
    }
}