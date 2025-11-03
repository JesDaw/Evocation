using UnityEngine;
using UnityEngine.InputSystem;

public class GlobalInputManager : MonoBehaviour
{

    [SerializeField] CameraControlSwitcher _cameraControlSwitcher;
    [SerializeField] public InputSystem_Actions ControlSwitchingInputs;
    [SerializeField] public InputSystem_Actions FreecamInputs;
    [SerializeField] public InputSystem_Actions PauseMenuInputs;
    [SerializeField] public InputSystem_Actions CharacterSelectInputs;

    // Cached delegates
    System.Action<InputAction.CallbackContext> _movePerformed;
    System.Action<InputAction.CallbackContext> _moveCanceled;
    System.Action<InputAction.CallbackContext> _attackPerformed;
    System.Action<InputAction.CallbackContext> _toggleCameraControl;

    //getters and setters
    public static GlobalInputManager Instance { get; private set; }
    public InputSystem_Actions InputActions { get; private set; }

    public PlayerStateMachine ActivePlayerStateMachiene { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        //DontDestroyOnLoad(gameObject);

        InputActions = new InputSystem_Actions();
    }
    void OnEnable() => EnableAllControls();
    void Start()
    {

    }
    
    void OnDisable() => DisableAllControls();

    

    

    
    

    // ========================= Input Linking =========================
    public void RegisterPlayerCharacterInputCallbacks()
    {
        if (ActivePlayerStateMachiene == null)
        {
            Debug.LogWarning("No active player to register inputs to!");
            return;
        }

        // Define delegate references once
        _movePerformed = ctx => ActivePlayerStateMachiene.OnMove(ctx);
        _moveCanceled = ctx => ActivePlayerStateMachiene.OnMove(ctx);
        _attackPerformed = ctx => ActivePlayerStateMachiene.OnAttack(ctx);
        _toggleCameraControl = ctx => _cameraControlSwitcher?.OnToggleCameraControl(ctx);

        // Subscribe once
        InputActions.Player.Move.performed += _movePerformed;
        InputActions.Player.Move.canceled += _moveCanceled;
        InputActions.Player.Attack.performed += _attackPerformed;
        InputActions.ControlManager.ToggleCameraControl.performed += _toggleCameraControl;

        Debug.Log($"Registered input callbacks for player: {ActivePlayerStateMachiene.name}");
    }

    public void UnregisterPlayerCharacterInputCallbacks()
    {
        // If they weren’t registered, just skip
        if (_movePerformed != null)
        {
            InputActions.Player.Move.performed -= _movePerformed;
            InputActions.Player.Move.canceled -= _moveCanceled;
            InputActions.Player.Attack.performed -= _attackPerformed;
            InputActions.ControlManager.ToggleCameraControl.performed -= _toggleCameraControl;

            Debug.Log("Unregistered previous input callbacks");
        }

        _movePerformed = null;
        _moveCanceled = null;
        _attackPerformed = null;
        _toggleCameraControl = null;
    }

    // ========================= Active Player Handling =========================
    public void SetActivePlayer(PlayerStateMachine player)
    {
        // Unhook old
        UnregisterPlayerCharacterInputCallbacks();

        // Set new
        ActivePlayerStateMachiene = player;

        // Rehook new
        RegisterPlayerCharacterInputCallbacks();
    }

    public void ClearActivePlayer()
    {
        UnregisterPlayerCharacterInputCallbacks();
        ActivePlayerStateMachiene = null;
    }

    // ========================= Camera =========================
    public void SetActiveCamera(CameraControlSwitcher camera)
    {
        _cameraControlSwitcher = camera;
        Debug.Log($"Active camera set to: {camera?.name ?? "None"}");
    }

    // ========================= Enable / Disable Controls =========================
    public void EnableAllControls() => InputActions.Enable();
    public void DisableAllControls() => InputActions.Disable();

    public void EnableCharacterControls() => InputActions.Player.Enable();
    public void DisableCharacterControls() => InputActions.Player.Disable();

    public void EnableCameraControls() => InputActions.Camera.Enable();
    public void DisableCameraControls() => InputActions.Camera.Disable();

    public void EnableUIControls() => InputActions.UI.Enable();
    public void DisableUIControls() => InputActions.UI.Disable();

    public void EnableControlSwapping() => InputActions.ControlManager.Enable();
    public void DisableControlSwapping() => InputActions.ControlManager.Disable();
    
    public void EnableCharacterSpawnControls() => InputActions.SpawnerController.Enable();
    public void DisableCharacterSpawnControls() => InputActions.SpawnerController.Disable();
}
