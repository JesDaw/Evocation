using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CameraControlSwitcher : MonoBehaviour
{
    [SerializeField] CinemachineCamera freeCam;
    [SerializeField] FreeCamController cameraMovement;
    [SerializeField] IntVeriable player_lives;
    [SerializeField] AudioManager soundEffectsManager;

    public bool FreeCamIsActive = false;
    internal bool _camModeIsTogglable = true; 

    public static CameraControlSwitcher Instance { get; set; }

    public void DisableSwitching() => GlobalInputManager.Instance.DisableControlSwapping();
    public void EnableSwitching() => GlobalInputManager.Instance.EnableControlSwapping();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        // Subscribe to toggle camera control from GlobalInputManager
        var controlManager = GlobalInputManager.Instance.InputActions.ControlManager;
        controlManager.ToggleCameraControl.performed += OnToggleCameraControl;
    }

    void OnDisable()
    {
        // Unsubscribe when disabled
        if (GlobalInputManager.Instance != null)
        {
            var controlManager = GlobalInputManager.Instance.InputActions.ControlManager;
            controlManager.ToggleCameraControl.performed -= OnToggleCameraControl;
        }
    }

    void Start()
    {
        if (soundEffectsManager == null)
            soundEffectsManager = FindAnyObjectByType<AudioManager>();
    }

    public void OnToggleCameraControl(InputAction.CallbackContext context)
    {
        //Debug.Log("Switching camera button pressed"); 
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
        
        // Use GlobalInputManager to switch control modes
        GlobalInputManager.Instance.SetPlayerCharacterMode();

        // Get the current active player and enable their inputs
        var currentPlayer = ActivePlayer.Instance.CurrentPlayer?.GetComponent<PlayerStateMachine>();
        if (currentPlayer != null)
        {
            currentPlayer.SetActive(true);
            //Debug.Log($"SwitchToPlayerControl: Enabled {currentPlayer.gameObject.name}");
        }
        else
        {
            Debug.LogError("SwitchToPlayerControl: No current player found!");
        }

        var playerCam = ActivePlayer.Instance.GetCurrentPlayerCamera();
        if (playerCam != null && playerCam.Priority != 2)
        {
            playerCam.Priority = 2;
        }

        if (freeCam != null && freeCam.Priority != 1)
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

        // Use GlobalInputManager to switch control modes
        GlobalInputManager.Instance.SetFreeCamMode();

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


    public void SwitchToFreeCamAtPosition(Vector3 position, float fieldOfView) 
    {
        FreeCamIsActive = true;

        var currentPlayer = ActivePlayer.Instance.CurrentPlayer?.GetComponent<PlayerStateMachine>();
        if (currentPlayer != null)
        {
            currentPlayer.SetActive(false);
        }

        GlobalInputManager.Instance.SetFreeCamMode();

        if (freeCam != null)
        {
            freeCam.transform.position = position;
            freeCam.Lens.FieldOfView = fieldOfView;
            freeCam.Priority = 2;
        }

        var playerCam = ActivePlayer.Instance.GetCurrentPlayerCamera();
        if (playerCam != null)
        {
            playerCam.Priority = 0;
        }

        //Debug.Log($"Ghost cam activated at position {position}");
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

        // Use GlobalInputManager to set appropriate mode
        GlobalInputManager.Instance.SetFreeCamMode();

        if (soundEffectsManager != null)
            soundEffectsManager.gameObject.SetActive(false);

        Debug.Log("All players dead - switched to freecam.");
    }
}