using UnityEngine;
using UnityEngine.InputSystem;

public class GlobalInputManager : MonoBehaviour
{
    public static GlobalInputManager Instance { get; private set; }
    private InputSystem_Actions _inputActions;
    public InputSystem_Actions InputActions => _inputActions;
    [SerializeField] bool DebugLogs = false;

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

    // ========================= Control Groups =========================
    

    public void EnableAllControls()
    {
        _inputActions.Enable();
    }
    

    public void DisableAllControls()
    {
        _inputActions.Disable();
    }

    public void EnableCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void DisableCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // --------- Player Controls ---------
    public void EnablePlayerControls()
    {
        _inputActions.Player.Enable();
        if(DebugLogs) Debug.Log("Player controls enabled");
    }
    
    public void DisablePlayerControls()
    {
        _inputActions.Player.Disable();
        if(DebugLogs) Debug.Log("Player controls disabled");
    }

    // --------- Camera Controls ---------
    public void EnableCameraControls()
    {
        _inputActions.Camera.Enable();
        if(DebugLogs) Debug.Log("Camera controls enabled");
    }
    
    public void DisableCameraControls()
    {
        _inputActions.Camera.Disable();
        if(DebugLogs) Debug.Log("Camera controls disabled");
    }

    // --------- Control Manager (switching between player/camera) ---------
    public void EnableControlSwapping()
    {
        _inputActions.ControlManager.Enable();
    }
    
    public void DisableControlSwapping()
    {
        _inputActions.ControlManager.Disable();
    }

    // --------- Player Switching (NextPlayer/PreviousPlayer) ---------
    public void EnablePlayerSwitching()
    {

        _inputActions.ControlManager.Enable();
    }
    
    public void DisablePlayerSwitching()
    {
        _inputActions.ControlManager.Disable();
    }

    // --------- Spawner Controls ---------
    public void EnableSpawnerControls()
    {
        _inputActions.SpawnerController.Enable();
    }
    
    public void DisableSpawnerControls()
    {
        _inputActions.SpawnerController.Disable();
    }
    
    public void EnableCharacterSpawnControls() => EnableSpawnerControls();
    public void DisableCharacterSpawnControls() => DisableSpawnerControls();

    // --------- UI Controls ---------
    public void EnableUIControls()
    {
        _inputActions.UI.Enable();
    }
    
    public void DisableUIControls()
    {
        _inputActions.UI.Disable();
    }

    // ========================= Game State Presets =========================
    
    public void SetPlayerCharacterMode()
    {
        DisableAllControls();
        EnablePlayerControls();
        EnableControlSwapping();
        EnablePlayerSwitching();
        EnableSpawnerControls();  
        EnableUIControls(); 
        if(DebugLogs) Debug.Log("Input Mode: Gameplay");
    }
    
    public void SetFreeCamMode()
    {
        DisableAllControls();
        EnableCameraControls();
        EnableControlSwapping();
        EnablePlayerSwitching();
        EnableSpawnerControls();
        EnableUIControls(); 
        if(DebugLogs) Debug.Log("Input Mode: FreeCam");
    }

    public void SetScoutingMode()
    {
        DisableAllControls();
        EnableCameraControls();
        EnableUIControls(); 
        if(DebugLogs) Debug.Log("Input Mode: scouting");
    }
    
    public void SetCutsceneMode()
    {
        DisableAllControls();
        EnableUIControls();
        if(DebugLogs) Debug.Log("Input Mode: Cutscene");
    }
    
    public void SetDialogueMode()
    {
        DisableAllControls();
        EnableUIControls();
        if(DebugLogs) Debug.Log("Input Mode: Dialogue");
    }

    public void SetPauseMenuMode()
    {
        DisableAllControls();
        EnableUIControls();
        EnableCursor();
        if(DebugLogs) Debug.Log("Input Mode: Pause Menu");
    }
    
    public void SetCharacterSelectingMode()
    {
        DisableAllControls();
        EnableUIControls();  
        if(DebugLogs) Debug.Log("Input Mode: Spawning");
    }

    // ========================= Utilities =========================
    
    /// <summary>
    /// Get the current state of all action maps (for debugging)
    /// </summary>
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