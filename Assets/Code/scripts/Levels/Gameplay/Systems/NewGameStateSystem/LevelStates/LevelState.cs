using UnityEngine;
using UltEvents;

/// <summary>
/// Base class for all level states. Each state represents a phase in the level.
/// Derive from this to create custom states with specific behavior.
/// All states are ScriptableObjects that can be configured in the inspector.
/// </summary>
public abstract class LevelState : ScriptableObject
{
    [Header("State Configuration")]
    [SerializeField] protected string stateName = "Unnamed State";
    [Header("SceneActivity stuff")]
    [SerializeField] protected string uiCanvasName = "";
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
    
    [Header("Events")]
    [SerializeField] protected UltEvent onStateEnter;
    [SerializeField] protected UltEvent onStateExit;

    [Header("Debugging")]
    [SerializeField] bool DebugLogs = false;
    
    protected LevelStateManager context;
    
    public enum InputMode
    {
        Disabled,
        Cutscene,
        Scouting,
        Gameplay,
        CharacterSelecting,
        PauseMenu,
        FreeCam
    }
    
    public string StateName => stateName;
    
    /// <summary>
    /// Initialize the state with a reference to the state manager
    /// </summary>
    public virtual void Initialize(LevelStateManager manager)
    {
        context = manager;
    }
    
    /// <summary>
    /// Called when entering this state
    /// </summary>
    public virtual void EnterState()
    {
        if (DebugLogs) Debug.Log($"[LevelState] Entering: {stateName}");
        
        // Apply time scale
        Time.timeScale = timeScale;
        
        // Update UI
        if (!string.IsNullOrEmpty(uiCanvasName) && context.SceneManager != null)
        {
            context.SceneManager.Activate(uiCanvasName, makeAnchor: makeUIAnchor);
        }
        
        // Configure input
        ConfigureInput();
        
        // Configure camera control
        ConfigureCameraControl();
        
        // Configure game mechanics
        ConfigureGameMechanics();
        
        // Configure audio
        ConfigureAudio();
        
        // Invoke custom events
        onStateEnter?.Invoke();
        
        // Allow derived classes to add custom behavior
        OnEnterState();
    }
    
    /// <summary>
    /// Called every frame while in this state
    /// </summary>
    public virtual void UpdateState()
    {
        OnUpdateState();
    }
    
    /// <summary>
    /// Called when exiting this state
    /// </summary>
    public virtual void ExitState()
    {
        if (DebugLogs) Debug.Log($"[LevelState] Exiting: {stateName}");
        
        onStateExit?.Invoke();
        OnExitState();
    }
    
    /// <summary>
    /// Override this for custom enter behavior
    /// </summary>
    protected virtual void OnEnterState() { }
    
    /// <summary>
    /// Override this for custom update behavior
    /// </summary>
    protected virtual void OnUpdateState() { }
    
    /// <summary>
    /// Override this for custom exit behavior
    /// </summary>
    protected virtual void OnExitState() { }
    
    protected virtual void ConfigureInput()
    {
        if (GlobalInputManager.Instance == null)
        {
            if (DebugLogs) Debug.LogWarning("GlobalInputManager not found!");
            return;
        }
        
        switch (inputMode)
        {
            case InputMode.Disabled:
                GlobalInputManager.Instance.DisableAllControls();
                break;
            case InputMode.Cutscene:
                GlobalInputManager.Instance.SetCutsceneMode();
                break;
            case InputMode.Scouting:
                GlobalInputManager.Instance.SetScoutingMode();
                break;
            case InputMode.Gameplay:
                GlobalInputManager.Instance.SetPlayerCharacterMode();
                break;
            case InputMode.CharacterSelecting:
                GlobalInputManager.Instance.SetCharacterSelectingMode();
                break;
            case InputMode.PauseMenu:
                GlobalInputManager.Instance.SetPauseMenuMode();
                break;
            case InputMode.FreeCam:
                GlobalInputManager.Instance.SetFreeCamMode();
                break;
        }
    }
    
    protected virtual void ConfigureCameraControl()
    {
        if (!alterPlayerControls) return;
        var controlSwitcher = CameraControlSwitcher.Instance;
        if (controlSwitcher == null) return;
        
        if (switchToPlayerControl)
        {
            controlSwitcher.SwitchToPlayerControl();
        }
        else if (switchToCameraControl)
        {
            controlSwitcher.SwitchToCameraControl();
        }
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
        
        if (playMusic && !string.IsNullOrEmpty(musicStateName))
        {
            audio.SetMusicState(musicStateName);
        }
    }
}