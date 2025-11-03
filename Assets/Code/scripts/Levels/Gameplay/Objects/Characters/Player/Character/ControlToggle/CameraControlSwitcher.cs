using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CameraControlSwitcher : MonoBehaviour
{
    [SerializeField] CinemachineCamera freeCam;
    [SerializeField] FreeCamController cameraMovement;
    [SerializeField] ActivePlayer activePlayer;
    [SerializeField] IntVeriable player_lives;
    [SerializeField] AudioManager audio_manager;

    internal InputSystem_Actions inputActions;
    public bool FreeCamIsActive = false;
    internal bool _camModeIsTogglable = true; 


    public void DisableSwitching() { GlobalInputManager.Instance.DisableControlSwapping(); }
    public void EnableSwitching() { GlobalInputManager.Instance.EnableControlSwapping(); }

    void Onable()
    {
        inputActions = new InputSystem_Actions();
    }

    void Start()
    {
        if (audio_manager == null)
            audio_manager = FindAnyObjectByType<AudioManager>();

        GlobalInputManager.Instance.ControlSwitchingInputs = inputActions;

        GlobalInputManager.Instance.SetActiveCamera(this);
    }

    public void OnToggleCameraControl(InputAction.CallbackContext context)
    {
        if (!_camModeIsTogglable) return;
        if (!context.performed) return;

        if (player_lives != null && player_lives._Value <= 0 && !FreeCamIsActive)
        {
            SwitchToCameraControl();
            return;
        }

        audio_manager?.Play("Switching Cameras");
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
        GlobalInputManager.Instance.EnableCharacterControls();
        GlobalInputManager.Instance.DisableCameraControls();
        //cameraMovement.enabled = false;

        var playerCam = activePlayer.GetCurrentPlayerCamera();
        if (playerCam != null)
        {
            playerCam.Priority = 2;
        }

        if (freeCam != null)
        {
            freeCam.Priority = 1;
        }

        var playerController = activePlayer.CurrentPlayer?.GetComponent<PlayerStateMachine>();
        GlobalInputManager.Instance?.EnableCharacterControls();
    }

    public void SwitchToCameraControl()
    {
        FreeCamIsActive = true;

        GlobalInputManager.Instance.DisableCharacterControls();
        GlobalInputManager.Instance.EnableCameraControls();

        var playerCam = activePlayer.GetCurrentPlayerCamera();

        Debug.Log($"{playerCam == null}");
        freeCam.transform.position = playerCam.transform.position;
        freeCam.Lens.FieldOfView = playerCam.Lens.FieldOfView;

        freeCam.Priority = 2;

        playerCam.Priority = 0;

        //  disable control of the player while freecam is active
        //var playerController = activePlayer.CurrentPlayer?.GetComponent<PlayerStateMachine>();
        GlobalInputManager.Instance?.DisableCharacterControls();
    }

    public void DeadFreeCam()
    {
        FreeCamIsActive = true;

        var playerCam = activePlayer.GetCurrentPlayerCamera();
        if (freeCam != null && playerCam != null)
        {
            freeCam.Priority = 2;
            freeCam.transform.position = playerCam.transform.position;
            freeCam.Lens.FieldOfView = playerCam.Lens.FieldOfView;
        }

        playerCam.Priority = 0;

        //var playerController = activePlayer.CurrentPlayer?.GetComponent<PlayerStateMachine>();
        GlobalInputManager.Instance?.DisableCharacterControls();

        if (audio_manager != null)
            audio_manager.gameObject.SetActive(false);

        Debug.Log("All players dead - switched to freecam.");
    }
}
