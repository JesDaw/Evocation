using UnityEngine;
using UnityEngine.InputSystem;
using System;

[Serializable]
public class ScoutingState : LevelState
{
    [Header("Scouting Configuration")]
    [SerializeField] private string confirmationUIName = "ConfirmationUI";

    bool confirmationUIActive = false;
    bool characterSelectIsOpen = false;
    //bool isInitialized = false;
    
    public override void Initialize(LevelStateManager manager)
    {
        base.Initialize(manager);
        //isInitialized = true;
    }
    
    protected override void OnEnterState()
    {
        confirmationUIActive = false;
        characterSelectIsOpen = false;
        ToggleInputListeners(true);
    }

    protected override void OnExitState()
    {
        ToggleInputListeners(false);
        confirmationUIActive = false;
        characterSelectIsOpen = false;
    }
    
    private void ToggleInputListeners(bool enroll)
    {
        if (GlobalInputManager.Instance == null) return;
        var uiActions = GlobalInputManager.Instance.InputActions.UI;

        if (enroll)
        {
            uiActions.StartEngaugment.performed += OnEngagePressed;
            uiActions.Return.performed += OnReturnPressed;
            uiActions.ToggleCharacterSelect.performed += OnToggleCharacterSelect;
        }
        else
        {
            uiActions.StartEngaugment.performed -= OnEngagePressed;
            uiActions.Return.performed -= OnReturnPressed;
            uiActions.ToggleCharacterSelect.performed -= OnToggleCharacterSelect;
        }
    }

    void OnEngagePressed(InputAction.CallbackContext ctx)
    {
        if (context.CurrentState != this) return;
        
        if (!confirmationUIActive)
        {
            context.SceneManager.Activate(confirmationUIName, true);
            GlobalInputManager.Instance.SetPauseMenuMode();
            confirmationUIActive = true;
            FModAudioManager.instance.PlaySoundByName("pauseGame");
        }
        else
        {
            StartBattle();
        }
    }

    public void StartBattle()
    {
        FModAudioManager.instance.PlaySoundByName("engageInBattle");
        context.TransitionToNextState();
    }
    
    void OnReturnPressed(InputAction.CallbackContext ctx)
    {
        if (context.CurrentState != this || !confirmationUIActive) return;
        FModAudioManager.instance.PlaySoundByName("backToScouting");
        BackToScouting();
    }

    void OnToggleCharacterSelect(InputAction.CallbackContext ctx)
    {
        if (context.CurrentState != this) return;


        if (!characterSelectIsOpen)
        {
            FModAudioManager.instance.PlaySoundByName("openCharacterSelect");
            context.SceneManager.Activate("LoadoutSelectUI", true);
            characterSelectIsOpen = true;

            GlobalInputManager.Instance.SetCharacterSelectingMode();
            GlobalInputManager.Instance.EnableCursor();
        }
        else
        {
            FModAudioManager.instance.PlaySoundByName("closeCharacterSelect");
            context.SceneManager.Activate(sceneActivityName, true);
            characterSelectIsOpen = false;
            GlobalInputManager.Instance.DisableCursor();
            GlobalInputManager.Instance.SetScoutingMode();
        }
    }

    public void BackToScouting()
    {
        context.SceneManager.Activate(sceneActivityName, true);
        GlobalInputManager.Instance.SetScoutingMode();
        confirmationUIActive = false;
    }
}