using UnityEngine;
using UnityEngine.InputSystem;
using System;

[Serializable]
public class ScoutingState : LevelState
{
    [Header("Scouting Configuration")]
    [SerializeField] private string confirmationUIName = "ConfirmationUI";
    
    bool confirmationUIActive = false;
    //bool isInitialized = false;
    
    public override void Initialize(LevelStateManager manager)
    {
        base.Initialize(manager);
        //isInitialized = true;
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
        if (context.CurrentState != this) return;
        
        if (!confirmationUIActive)
        {
            context.SceneManager.Activate(confirmationUIName);
            GlobalInputManager.Instance.SetCharacterSelectingMode();
            confirmationUIActive = true;
        }
        else
        {
            context.TransitionToNextState();
        }
    }
    
    private void OnReturnPressed(InputAction.CallbackContext ctx)
    {
        if (context.CurrentState != this || !confirmationUIActive) return;
        
        context.SceneManager.Activate(sceneActivityName);
        GlobalInputManager.Instance.SetScoutingMode();
        confirmationUIActive = false;
    }
}