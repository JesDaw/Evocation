using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CameraControlSwitcher : MonoBehaviour
{
    [SerializeField] CinemachineCamera freeCam;
    [SerializeField] FreeCamController cameraMovement;
    [SerializeField] IntVeriable player_lives;
    [SerializeField] AudioManager soundEffectsManager;

    internal InputSystem_Actions inputActions;
    public bool FreeCamIsActive = false;
    internal bool _camModeIsTogglable = true; 

    public static CameraControlSwitcher Instance { get; set; }

    public void DisableSwitching() { GlobalInputManager.Instance.DisableControlSwapping(); }
    public void EnableSwitching() { GlobalInputManager.Instance.EnableControlSwapping(); }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        inputActions = new InputSystem_Actions();
    }

    void OnEnable()
    {
        // Subscribe to toggle camera control
        inputActions.ControlManager.ToggleCameraControl.performed += OnToggleCameraControl;
        inputActions.ControlManager.Enable();
    }

    void OnDisable()
    {
        // Unsubscribe when disabled
        inputActions.ControlManager.ToggleCameraControl.performed -= OnToggleCameraControl;
    }

    void Start()
    {
        if (soundEffectsManager == null)
            soundEffectsManager = FindAnyObjectByType<AudioManager>();

        GlobalInputManager.Instance.ControlSwitchingInputs = inputActions;
    }

    public void OnToggleCameraControl(InputAction.CallbackContext context)
    {
        Debug.Log("Switching camera button pressed"); 
        if (!_camModeIsTogglable) return;
        if (!context.performed) return;

        if (player_lives != null && player_lives._Value <= 0 && !FreeCamIsActive)
        {
            SwitchToCameraControl();
            return;
        }
        
        soundEffectsManager?.Play("Switching Cameras");
        ToggleControl();
    }

    public void ToggleControl()
    {
        if (FreeCamIsActive)
            SwitchToPlayerControl();
        else
            SwitchToCameraControl();
    }

    public void SwitchToPlayerControl()
    {
        FreeCamIsActive = false;
        
        // Disable freecam
        GlobalInputManager.Instance.DisableCameraControls();

        // Get the current active player and enable their inputs
        var currentPlayer = ActivePlayer.Instance.CurrentPlayer?.GetComponent<PlayerStateMachine>();
        if (currentPlayer != null)
        {
            currentPlayer.SetActive(true);
            Debug.Log($"SwitchToPlayerControl: Enabled {currentPlayer.gameObject.name}");
        }
        else
        {
            Debug.LogError("SwitchToPlayerControl: No current player found!");
        }

        // Update camera priorities
        var playerCam = ActivePlayer.Instance.GetCurrentPlayerCamera();
        if (playerCam != null)
        {
            playerCam.Priority = 2;
        }

        if (freeCam != null)
        {
            freeCam.Priority = 1;
        }
    }

    public void SwitchToCameraControl()
    {
        FreeCamIsActive = true;

        // Disable the current player's inputs
        var currentPlayer = ActivePlayer.Instance.CurrentPlayer?.GetComponent<PlayerStateMachine>();
        if (currentPlayer != null)
        {
            currentPlayer.SetActive(false);
        }

        // Enable freecam controls
        GlobalInputManager.Instance.EnableCameraControls();

        // Update camera position and priority
        var playerCam = ActivePlayer.Instance.GetCurrentPlayerCamera();
        if (playerCam != null && freeCam != null)
        {
            freeCam.transform.position = playerCam.transform.position;
            freeCam.Lens.FieldOfView = playerCam.Lens.FieldOfView;
            freeCam.Priority = 2;
            playerCam.Priority = 0;
        }
    }

    public void DeadFreeCam()
    {
        FreeCamIsActive = true;

        var playerCam = ActivePlayer.Instance.GetCurrentPlayerCamera();
        if (freeCam != null && playerCam != null)
        {
            freeCam.Priority = 2;
            freeCam.transform.position = playerCam.transform.position;
            freeCam.Lens.FieldOfView = playerCam.Lens.FieldOfView;
        }

        playerCam.Priority = 0;

        // Disable current player
        var currentPlayer = ActivePlayer.Instance.CurrentPlayer?.GetComponent<PlayerStateMachine>();
        if (currentPlayer != null)
        {
            currentPlayer.SetActive(false);
        }

        if (soundEffectsManager != null)
            soundEffectsManager.gameObject.SetActive(false);

        Debug.Log("All players dead - switched to freecam.");
    }
}