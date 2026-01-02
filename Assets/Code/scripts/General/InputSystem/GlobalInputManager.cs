using UnityEngine;
using UnityEngine.InputSystem;

public class GlobalInputManager : MonoBehaviour
{
    public static GlobalInputManager Instance { get; private set; }
    
    // Single source of truth for all input actions
    private InputSystem_Actions _inputActions;
    public InputSystem_Actions InputActions => _inputActions;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // Create the single InputSystem_Actions instance
        _inputActions = new InputSystem_Actions();
        
        //Debug.Log("GlobalInputManager initialized");
    }
    
    void Start()
    {
        // Start with everything disabled, then enable what's needed
        DisableAllControls();
    }
    
    void OnDestroy()
    {
        _inputActions?.Disable();
        _inputActions?.Dispose();
    }

    // ========================= Control Groups =========================
    
    /// <summary>
    /// Enable all controls - use sparingly, prefer specific enable methods
    /// </summary>
    public void EnableAllControls()
    {
        _inputActions.Enable();
    }
    
    /// <summary>
    /// Disable all controls - useful for cutscenes, game over, etc.
    /// </summary>
    public void DisableAllControls()
    {
        _inputActions.Disable();
    }

    // --------- Player Controls ---------
    public void EnablePlayerControls()
    {
        _inputActions.Player.Enable();
        //Debug.Log("Player controls enabled");
    }
    
    public void DisablePlayerControls()
    {
        _inputActions.Player.Disable();
        //Debug.Log("Player controls disabled");
    }

    // --------- Camera Controls ---------
    public void EnableCameraControls()
    {
        _inputActions.Camera.Enable();
        //Debug.Log("Camera controls enabled");
    }
    
    public void DisableCameraControls()
    {
        _inputActions.Camera.Disable();
        //Debug.Log("Camera controls disabled");
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
        // Assuming you have a PlayerSwitching action map
        // If NextPlayer/PreviousPlayer are in ControlManager, use that instead
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
    
    // Aliases for backward compatibility
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
    
    /// <summary>
    /// Normal gameplay - player controls active, camera switching available
    /// </summary>
    public void SetGameplayMode()
    {
        DisableAllControls();
        EnablePlayerControls();
        EnableControlSwapping();
        EnablePlayerSwitching();
        EnableSpawnerControls();  // Added spawner controls
        EnableUIControls(); // For pause menu
        //Debug.Log("Input Mode: Gameplay");
    }
    
    /// <summary>
    /// Free camera mode - camera controls active
    /// </summary>
    public void SetFreeCamMode()
    {
        DisableAllControls();
        EnableCameraControls();
        EnableControlSwapping();
        EnablePlayerSwitching();
        EnableUIControls(); // For pause menu
        //Debug.Log("Input Mode: FreeCam");
    }
    
    /// <summary>
    /// Cutscene mode - only pause/skip available
    /// </summary>
    public void SetCutsceneMode()
    {
        DisableAllControls();
        EnableUIControls(); // Only UI controls (pause, skip)
        //Debug.Log("Input Mode: Cutscene");
    }
    
    /// <summary>
    /// Dialogue mode - only dialogue progression and pause
    /// </summary>
    public void SetDialogueMode()
    {
        DisableAllControls();
        EnableUIControls(); // For dialogue interaction and pause
        //Debug.Log("Input Mode: Dialogue");
    }
    
    /// <summary>
    /// Pause menu - only menu navigation
    /// </summary>
    public void SetPauseMenuMode()
    {
        DisableAllControls();
        EnableUIControls();
        //Debug.Log("Input Mode: Pause Menu");
    }
    
    /// <summary>
    /// Character select/spawning mode
    /// </summary>
    public void SetSpawningMode()
    {
        DisableAllControls();
        EnableSpawnerControls();
        EnableUIControls(); // For closing the menu
        //Debug.Log("Input Mode: Spawning");
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