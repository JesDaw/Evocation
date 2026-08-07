using UnityEngine;
using UltEvents;
using System;

[Serializable]
public abstract class LevelState
{
    [Header("State Configuration")]
    public string stateName = "Unnamed State";
    
    [Header("SceneActivity stuff")]
    [SerializeField] protected string sceneActivityName = "";
    [SerializeField] protected bool makeUIAnchor = false;
    
    [Header("Input Mode")]
    [SerializeField] protected InputMode inputMode = InputMode.Disabled;
    
    [Header("Game Mechanics")]
    [SerializeField] protected bool enableMoney = false;
    [SerializeField] protected bool enableTimer = false;
    [SerializeField] protected bool enablePlayerSpawning = false;
    [SerializeField] protected bool enableEnemySpawning = false;
    [SerializeField] protected float timeScale = 1f;
    
    [Header("Camera Control")]
    [SerializeField] protected bool alterPlayerControls = false;
    [SerializeField] protected bool switchToPlayerControl = false;
    [SerializeField] protected bool switchToCameraControl = false;
    
    [Header("Audio")]
    [SerializeField] protected bool playMusic = false;
    [SerializeField] protected string musicStateName = "";
    [SerializeField] protected bool playAmbiance = false;
    [SerializeField] protected string ambianceStateName = "";
    
    [Header("Events")]
    public UltEvent onStateEnter;
    public UltEvent onStateExit;

    [Header("Debugging")]
    [SerializeField] protected bool DebugLogs = false;
    
    protected LevelStateManager context;
    
    public enum InputMode
    {
        Disabled, Cutscene, Scouting, Gameplay, CharacterSelecting, PauseMenu, FreeCam, Levelover
    }
    
    public string StateName => stateName;
    
    public virtual void Initialize(LevelStateManager manager)
    {
        context = manager;
    }
    
    public virtual void EnterState()
    {
        if (DebugLogs) Debug.Log($"[LevelState] Entering: {stateName}");
        Time.timeScale = timeScale;
        
        if (!string.IsNullOrEmpty(sceneActivityName) && context.SceneManager != null)
        {
            context.SceneManager.Activate(sceneActivityName, makeAnchor: makeUIAnchor);
            if (DebugLogs) Debug.Log($"[LevelState] activating scene: {sceneActivityName}");
        }
        
        ConfigureInput();
        ConfigureCameraControl();
        ConfigureGameMechanics();
        ConfigureAudio();
        ConfigureAmbiance();
        
        onStateEnter?.Invoke();
        OnEnterState();
    }
    
    public virtual void UpdateState() => OnUpdateState();
    
    public virtual void ExitState()
    {
        if (DebugLogs) Debug.Log($"[LevelState] Exiting: {stateName}");
        onStateExit?.Invoke();
        OnExitState();
    }
    
    protected virtual void OnEnterState() { }
    protected virtual void OnUpdateState() { }
    protected virtual void OnExitState() { }
    
    protected virtual void ConfigureInput()
    {
        if (GlobalInputManager.Instance == null) 
        {
            Debug.LogWarning("Level state cant find GlobalInputManager");
            return;
        }
        switch (inputMode)
        {
            case InputMode.Disabled: GlobalInputManager.Instance.DisableAllControls(); break;
            case InputMode.Cutscene: GlobalInputManager.Instance.SetCutsceneMode(); break;
            case InputMode.Scouting: GlobalInputManager.Instance.SetScoutingMode(); break;
            case InputMode.Gameplay: GlobalInputManager.Instance.SetPlayerCharacterMode(); break;
            case InputMode.CharacterSelecting: GlobalInputManager.Instance.SetCharacterSelectingMode(); break;
            case InputMode.PauseMenu: GlobalInputManager.Instance.SetPauseMenuMode(); break;
            case InputMode.FreeCam: GlobalInputManager.Instance.SetFreeCamMode(); break;
            case InputMode.Levelover: GlobalInputManager.Instance.SetLevelOverScreenMode(); break;
        }
    }

    protected virtual void ConfigureCameraControl()
    {
        if (!alterPlayerControls) return;
        var controlSwitcher = CameraControlSwitcher.Instance;
        if (controlSwitcher == null) return;
        if (switchToPlayerControl) controlSwitcher.SwitchToPlayerControl();
        else if (switchToCameraControl) controlSwitcher.SwitchToCameraControl();
    }

    protected virtual void ConfigureGameMechanics()
    {
        var mechanics = GameMechanicsManager.Instance;
        if (mechanics == null) return;
        mechanics.SetMoneyActive(enableMoney);
        mechanics.SetTimerActive(enableTimer);
        mechanics.SetPlayerSpawningActive(enablePlayerSpawning);
        mechanics.SetEnemySpawningActive(enableEnemySpawning);
    }

    protected virtual void ConfigureAudio()
    {
        var audio = LevelAudioManager.Instance;
        if (audio == null) return;
        if (playMusic && !string.IsNullOrEmpty(musicStateName)) audio.SetMusicState(musicStateName);
    }

    protected virtual void ConfigureAmbiance()
    {
        var audio = LevelAudioManager.Instance;
        if (audio == null) return;
        if (playAmbiance && !string.IsNullOrEmpty(ambianceStateName)) audio.SetAmbianceState(ambianceStateName);
    }


    public virtual void StartBattle(){throw new NotImplementedException();}
    public virtual void Wait(){throw new NotImplementedException();}
}