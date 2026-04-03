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

    // --------- Magic Controls ---------
    public void EnableMagicControls()
    {
        _inputActions.MagicController.Enable();
        if(DebugLogs) Debug.Log("Magic controls enabled");
    }
    
    public void DisableMagicControls()
    {
        _inputActions.MagicController.Disable();
        if(DebugLogs) Debug.Log("Magic controls disabled");
    }

    // --------- Camera Controls ---------
    public void EnableCameraControls()
    {
        _inputActions.Camera.Enable();
        if(DebugLogs) Debug.Log($"Camera controls enabled from: {System.Environment.StackTrace}");
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
        if(DebugLogs) Debug.Log($"UI controls enabled");
    }
    
    public void DisableUIControls()
    {
        _inputActions.UI.Disable();
    }

    // ========================= Game State Presets =========================
    
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
        
    }

    public void SetScoutingMode()
    {
        if(DebugLogs) Debug.Log("=========Input Mode: scouting=========");
        DisableAllControls();
        EnableCameraControls();
        EnableUIControls();
        
    }
    
    public void SetCutsceneMode()
    {
        if(DebugLogs) Debug.Log("=========Input Mode: Cutscene=========");
        DisableAllControls();
        EnableUIControls();
        
    }
    
    public void SetDialogueMode()
    {
        if(DebugLogs) Debug.Log("=========Input Mode: Dialogue=========");
        DisableAllControls();
        EnableUIControls();
        
    }

    public void SetPauseMenuMode()
    {
        if(DebugLogs) Debug.Log("=========Input Mode: Pause Menu=========");
        DisableAllControls();
        EnableUIControls();
        EnableCursor();
        
    }
    
    public void SetCharacterSelectingMode()
    {
        if(DebugLogs) Debug.Log("=========Input Mode: Spawning=========");
        DisableAllControls();
        EnableUIControls();  
        
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
