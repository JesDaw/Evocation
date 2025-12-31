using UnityEngine;
using UnityEngine.InputSystem;

public class GlobalInputManager : MonoBehaviour
{
    // Only manage shared/global inputs now
    public InputSystem_Actions FreecamInputs;
    public InputSystem_Actions ControlSwitchingInputs;
    public InputSystem_Actions PauseMenuInputs;
    public InputSystem_Actions CharacterSelectInputs;

    //getters and setters
    public static GlobalInputManager Instance { get; private set; }
    public InputSystem_Actions InputActions { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // Only create shared input actions
        InputActions = new InputSystem_Actions();
    }
    
    void Start() => EnableAllControls();
    
    void OnDisable() => DisableAllControls();

    // ========================= Enable / Disable Controls =========================
    // Now only manages shared/global action maps
    
    public void EnableAllControls()
    { 
        InputActions.Enable();
        EnableCameraControls();
        EnableControlSwapping();
    }
    public void DisableAllControls()
    { 
        InputActions.Disable();
        DisableCameraControls();
        DisableControlSwapping();

    }

    public void EnableCameraControls()
    {
        if (FreecamInputs != null)
            FreecamInputs.Camera.Enable();
        else
            Debug.LogWarning("FreecamInputs is null");
    }
    
    public void DisableCameraControls()
    {
        if (FreecamInputs != null)
            FreecamInputs.Camera.Disable();
        else
            Debug.LogWarning("FreecamInputs is null");
    }

    public void EnableControlSwapping()
    {
        if (ControlSwitchingInputs != null)
            ControlSwitchingInputs.ControlManager.Enable();
        else
            Debug.LogWarning("ControlSwitchingInputs is null");
    }
    
    public void DisableControlSwapping()
    {
        if (ControlSwitchingInputs != null)
            ControlSwitchingInputs.ControlManager.Disable();
        else
            Debug.LogWarning("ControlSwitchingInputs is null");
    }
    
    public void EnableCharacterSpawnControls() => InputActions.SpawnerController.Enable();
    public void DisableCharacterSpawnControls() => InputActions.SpawnerController.Disable();
}