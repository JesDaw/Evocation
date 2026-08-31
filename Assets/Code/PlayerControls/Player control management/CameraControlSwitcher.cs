using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CameraControlSwitcher : MonoBehaviour
{
    [SerializeField] CinemachineCamera freeCam;
    [SerializeField] bool DebugLogs = false;

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
        
    }

    void OnDisable()
    {
        if (GlobalInputManager.Instance != null)
        {
            var controlManager = GlobalInputManager.Instance.InputActions.ControlManager;
            controlManager.ToggleCameraControl.performed -= OnToggleCameraControl;
        }
    }

    void Start()
    {
        var controlManager = GlobalInputManager.Instance.InputActions.ControlManager;
        controlManager.ToggleCameraControl.performed += OnToggleCameraControl;
    }

    public void OnToggleCameraControl(InputAction.CallbackContext context)
    {
        if (DebugLogs) UnityEngine.Debug.Log($"[CameraControlSwitcher] OnToggleCameraControl called. FreeCamIsActive: {FreeCamIsActive}, _camModeIsTogglable: {_camModeIsTogglable}");
        if (!_camModeIsTogglable) return;
        if (!context.performed) return;

        if (PlayerLivesManager.Instance.LifeCount <= 0)
        {
            SwitchToCameraControl(true);
            return;
        }

        ToggleControl();
    }

    public void ToggleControl()
    {
        if (FreeCamIsActive)
        {
            SwitchToPlayerControl();
        }
        else 
        {
            SwitchToCameraControl(true);
        }
    }

    public void SwitchToPlayerControl()
    {
        FreeCamIsActive = false;
        
        GlobalInputManager.Instance.SetMode(InputMode.PlayerCharacter);

        var currentPlayer = ActivePlayer.Instance.CurrentPlayer?.GetComponent<PlayerStateMachine>();
        if (currentPlayer == null)
        {
            if(DebugLogs) Debug.Log("SwitchToPlayerControl: No current player found!");
            return;         
        }

        currentPlayer.SetActive(true);
        if(DebugLogs) Debug.Log($"SwitchToPlayerControl. Enabled: {currentPlayer.gameObject.name}");

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

    public void SwitchToCameraControl(bool swapControls = false)
    {
        if (DebugLogs) UnityEngine.Debug.Log($"[CameraControlSwitcher] Switching to camera control. FreeCamIsActive was: {FreeCamIsActive}");
        if (swapControls) 
        {
            GlobalInputManager.Instance.SetMode(InputMode.FreeCam);
        }
        if (FreeCamIsActive)
        {
            if (DebugLogs) UnityEngine.Debug.Log("Free Cam Is already Active");
            return;
        }
        FreeCamIsActive = true;

        var currentPlayer = ActivePlayer.Instance.CurrentPlayer?.GetComponent<PlayerStateMachine>();
        if (currentPlayer != null)
        {
            currentPlayer.SetActive(false); 
        }

        
        var playerCam = ActivePlayer.Instance.GetCurrentPlayerCamera();
        if (playerCam != null && freeCam != null)
        {
            freeCam.transform.position = playerCam.transform.position;
            freeCam.Lens.FieldOfView = playerCam.Lens.FieldOfView;
            freeCam.Priority = 2;
            playerCam.Priority = 0;
        }

        
        if (DebugLogs) UnityEngine.Debug.Log($"[CameraControlSwitcher] Switched to camera control. FreeCam now active, player disabled.");

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

        // Disable current player and clear commands
        var currentPlayer = ActivePlayer.Instance.CurrentPlayer?.GetComponent<PlayerStateMachine>();
        if (currentPlayer != null)
        {
            currentPlayer.SetActive(false);
        }
    }
}