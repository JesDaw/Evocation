using UnityEngine;
using UnityEngine.InputSystem;

public class GlobalInputManager : MonoBehaviour
{
    public static GlobalInputManager Instance { get; private set; }
    private InputSystem_Actions _inputActions;
    public InputSystem_Actions InputActions => _inputActions;
    [SerializeField] bool DebugLogs = false;
    public bool MenuNavigation = false;
    #region Start and stop
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        _inputActions = new InputSystem_Actions();
        
        if(DebugLogs) Debug.Log("GlobalInputManager initialized");

        var rebinds = PlayerPrefs.GetString("rebinds");
        if (!string.IsNullOrEmpty(rebinds))
            InputActions.LoadBindingOverridesFromJson(rebinds);
    }
    
    void Start()
    {
        DisableAllControls();
    }
    
    void OnDestroy()
    {
        _inputActions?.Disable();
        _inputActions?.Dispose();
    }
    #endregion
    #region Control Groups
    public void EnableAllControls()
    {
        _inputActions.Enable();
    }
    

    public void DisableAllControls()
    {
        DisablePlayerControls();
        DisableMagicControls();
        DisableCameraControls();
        DisableControlSwapping();
        DisablePlayerSwitching();
        DisableSpawnerControls();
    }

    public void EnableCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    public void EnableMenuNavigation()
    {
        _inputActions.UI.Navigate.Enable();
        _inputActions.UI.ConfirmDialogue.Enable();
        MenuNavigation = true;
    }
    public void DisableMenuNavigation()
    {
        _inputActions.UI.Navigate.Disable();
        _inputActions.UI.ConfirmDialogue.Disable();
        MenuNavigation = false;
        UINavigationManager.Instance.lastHighlightedButton = null;
    }

    public void DisableCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // --------- Player Controls ---------
    public void EnablePlayerControls()
    {
        _inputActions.Player.Move.Enable();
        _inputActions.Player.Attack.Enable();
        _inputActions.Player.Interact.Enable();
        if(DebugLogs) Debug.Log("Player controls enabled");
    }
    
    public void DisablePlayerControls()
    {
        _inputActions.Player.Move.Disable();
        _inputActions.Player.Attack.Disable();
        _inputActions.Player.Interact.Disable();
        if(DebugLogs) Debug.Log("Player controls disabled");
    }

    // --------- Magic Controls ---------
    public void EnableMagicControls()
    {
        _inputActions.MagicController.CastSpell.Enable();
        _inputActions.MagicController.Look.Enable();
        _inputActions.MagicController.SwapSpell1.Enable();
        _inputActions.MagicController.SwapSpell2.Enable();
        if(DebugLogs) Debug.Log("Magic controls enabled");
    }
    
    public void DisableMagicControls()
    {
        _inputActions.MagicController.CastSpell.Disable();
        _inputActions.MagicController.Look.Disable();
        _inputActions.MagicController.SwapSpell1.Disable();
        _inputActions.MagicController.SwapSpell2.Disable();
        if(DebugLogs) Debug.Log("Magic controls disabled");
    }

    // --------- Camera Controls ---------
    public void EnableCameraControls()
    {
        _inputActions.Camera.Move.Enable();
        _inputActions.Camera.Zoom.Enable();
        if(DebugLogs) Debug.Log($"Camera controls enabled from: {System.Environment.StackTrace}");
    }
    
    public void DisableCameraControls()
    {
        _inputActions.Camera.Move.Disable();
        _inputActions.Camera.Zoom.Disable();
        if(DebugLogs) Debug.Log("Camera controls disabled");
    }

    // --------- Control Manager (switching between player/camera) ---------
    public void EnableControlSwapping()
    {
        _inputActions.ControlManager.ToggleCameraControl.Enable();

    }
    
    public void DisableControlSwapping()
    {
        _inputActions.ControlManager.ToggleCameraControl.Disable();
    }

    // --------- Player Switching (NextPlayer/PreviousPlayer) ---------
    public void EnablePlayerSwitching()
    {
        _inputActions.ControlManager.NextPlayer.Enable();
        _inputActions.ControlManager.PreviousPlayer.Enable();
    }
    
    public void DisablePlayerSwitching()
    {
        _inputActions.ControlManager.NextPlayer.Disable();
        _inputActions.ControlManager.PreviousPlayer.Disable();
    }

    // --------- Spawner Controls ---------
    public void EnableSpawnerControls()
    {
        _inputActions.SpawnerController.Spawn1.Enable();
        _inputActions.SpawnerController.Spawn2.Enable();
        _inputActions.SpawnerController.Spawn3.Enable();
        _inputActions.SpawnerController.Spawn4.Enable();
        _inputActions.SpawnerController.Spawn5.Enable();
        _inputActions.SpawnerController.Spawn6.Enable();
        _inputActions.SpawnerController.Spawn7.Enable();
        _inputActions.SpawnerController.Spawn8.Enable();
        _inputActions.SpawnerController.Spawn9.Enable();
        _inputActions.SpawnerController.SpawnPlayer.Enable();
    }
    
    public void DisableSpawnerControls()
    {
        _inputActions.SpawnerController.Spawn1.Disable();
        _inputActions.SpawnerController.Spawn2.Disable();
        _inputActions.SpawnerController.Spawn3.Disable();
        _inputActions.SpawnerController.Spawn4.Disable();
        _inputActions.SpawnerController.Spawn5.Disable();
        _inputActions.SpawnerController.Spawn6.Disable();
        _inputActions.SpawnerController.Spawn7.Disable();
        _inputActions.SpawnerController.Spawn8.Disable();
        _inputActions.SpawnerController.Spawn9.Disable();
        _inputActions.SpawnerController.SpawnPlayer.Disable();
    }

    // --------- UI Controls ---------
    public void EnableUIControls()
    {
        _inputActions.UI.TogglePause.Enable();
        _inputActions.UI.StartEngaugment.Enable();
        _inputActions.UI.ToggleCharacterSelect.Enable();
        _inputActions.UI.SkipCutscene.Enable();
        _inputActions.UI.ConfirmDialogue.Enable();
        _inputActions.UI.Return.Enable();
        if(DebugLogs) Debug.Log($"UI controls enabled");
    }
    
    public void DisableUIControls()
    {
        _inputActions.UI.TogglePause.Disable();
        _inputActions.UI.StartEngaugment.Disable();
        _inputActions.UI.ToggleCharacterSelect.Disable();
        _inputActions.UI.SkipCutscene.Disable();
        _inputActions.UI.ConfirmDialogue.Disable();
        _inputActions.UI.Return.Disable();
    }

    #endregion
    #region Game State Presets
    
    public void SetPlayerCharacterMode()
    {
        if(DebugLogs) Debug.Log("=========Input Mode: Gameplay=========");
        DisableAllControls();
        EnablePlayerControls();
        EnableControlSwapping();
        EnablePlayerSwitching();
        EnableSpawnerControls();  
        EnableMagicControls();
        EnableUIControls();
        DisableMenuNavigation();
        DisableCursor(); 
               
    }
    
    public void SetFreeCamMode()
    {
        if(DebugLogs) Debug.Log("=========Input Mode: FreeCam=========");
        DisableAllControls();
        EnableCameraControls();
        EnableControlSwapping();
        EnablePlayerSwitching();
        EnableSpawnerControls();
        EnableUIControls();
        DisableMenuNavigation();
        DisableCursor();
        
    }

    public void SetScoutingMode()
    {
        if(DebugLogs) Debug.Log("=========Input Mode: scouting=========");
        DisableAllControls();
        EnableCameraControls();
        EnableUIControls();
        DisableMenuNavigation();
        DisableCursor();
        
    }
    
    public void SetCutsceneMode()
    {
        if(DebugLogs) Debug.Log("=========Input Mode: Cutscene=========");
        DisableAllControls();
        _inputActions.UI.SkipCutscene.Enable();
        _inputActions.UI.ConfirmDialogue.Enable();
        _inputActions.UI.TogglePause.Enable();
        DisableMenuNavigation();
        DisableCursor();
        
    }
    
    public void SetDialogueMode()
    {
        if(DebugLogs) Debug.Log("=========Input Mode: Dialogue=========");
        DisableAllControls();
        _inputActions.UI.SkipCutscene.Enable();
        _inputActions.UI.ConfirmDialogue.Enable();
        _inputActions.UI.TogglePause.Enable();
        
        EnableMenuNavigation();        
    }

    public void SetPauseMenuMode()
    {
        if(DebugLogs) Debug.Log("=========Input Mode: Pause Menu=========");
        DisableAllControls();
        _inputActions.UI.TogglePause.Enable();
        EnableMenuNavigation();
    }

    public void SetEngaugeScreenMode()
    {
        if(DebugLogs) Debug.Log("=========Input Mode: Pause Menu=========");
        DisableAllControls();
        _inputActions.UI.TogglePause.Disable();
        _inputActions.UI.ToggleCharacterSelect.Disable();
        EnableMenuNavigation();
    }

    public void SetCharacterSelectingMode()
    {
        if(DebugLogs) Debug.Log("=========Input Mode: Spawning=========");
        DisableAllControls();
        _inputActions.UI.ToggleCharacterSelect.Enable();
        _inputActions.UI.TogglePause.Enable();
        EnableMenuNavigation();
    }

    public void SetLevelOverScreenMode()
    {
        if(DebugLogs) Debug.Log("=========Input Mode: LevelOver=========");
        DisableAllControls();
        EnableMenuNavigation();
    }
    #endregion
    public void LogInputState()
    {
        Debug.Log($"=== Input State ===\n" +
                  $"Player: {_inputActions.Player.enabled}\n" +
                  $"Camera: {_inputActions.Camera.enabled}\n" +
                  $"ControlManager: {_inputActions.ControlManager.enabled}\n" +
                  $"SpawnerController: {_inputActions.SpawnerController.enabled}\n" +
                  $"UI: {_inputActions.UI.enabled}");
    }
}
