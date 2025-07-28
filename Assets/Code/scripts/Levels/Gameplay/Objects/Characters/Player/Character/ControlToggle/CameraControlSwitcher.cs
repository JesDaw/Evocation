using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CameraControlSwitcher : MonoBehaviour
{
    // Camera References
    [SerializeField] private CinemachineCamera freeCam;

    // Script References
    [SerializeField] private FreeCamController cameraMovement;
    [SerializeField] private PlayerSwitch playerSwitcher;
    [SerializeField] private IntVeriable player_lives;

    //for walking sound effect
    private AudioManager audio_manager;

    private InputSystem_Actions inputActions;
    //InputAction inputActions;
    public bool FreeCamIsActive = false; // Starts by controlling the player

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Enable();

        inputActions.ControlManager.ToggleCameraControl.performed += SwitchControl;
    }

    private void Start()
    {
        audio_manager = FindAnyObjectByType<AudioManager>();
        inputActions.Player.Enable();
    }

    public void SwitchControl(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        audio_manager.Play("Switching Cameras"); //plays pop noise when cam switched
        ToggleControl();

        //auto switches to free cam when player lives hit 0 (also cant use controls anymroe)
        if (player_lives != null && player_lives._Value <= 0 && !FreeCamIsActive)
        {
            SwitchToCameraControl();
        }
    }

    /// <summary>
    /// Toggles between player and camera control.
    /// </summary>
    private void ToggleControl()
    {

        if (!FreeCamIsActive)
        {
            SwitchToCameraControl();
        }
        else
        {
            SwitchToPlayerControl();
        }
    }

    /// <summary>
    /// Switches control back to the current player.
    /// </summary>
    private void SwitchToPlayerControl()
    {
        FreeCamIsActive = false;

        if (playerSwitcher != null)
        {
            CinemachineCamera currentPlayerCam = playerSwitcher.GetCurrentPlayerCamera();
            PlayersControlerScriptsManager currentPlayer = playerSwitcher.GetCurrentPlayerController();

            if (currentPlayerCam != null) currentPlayerCam.Priority = 2;
            if (freeCam != null) freeCam.Priority = 1;
            if (currentPlayer != null) currentPlayer.EnableControls();
        }

        if (cameraMovement != null) cameraMovement.enabled = false;

        inputActions.Camera.Disable();
        inputActions.Player.Enable();



        Debug.Log("Switched to player control.");
    }

    /// <summary>
    /// Switches control to free camera mode.
    /// </summary>
    public void SwitchToCameraControl()
    {
        FreeCamIsActive = true;

        if (playerSwitcher != null)
        {
            CinemachineCamera currentPlayerCam = playerSwitcher.GetCurrentPlayerCamera();
            PlayersControlerScriptsManager currentPlayer = playerSwitcher.GetCurrentPlayerController();
        
            //set freecam to active and make it start in the same position as the player cam
            if (freeCam != null) 
            {
                freeCam.Priority = 2;
                freeCam.transform.position = playerSwitcher.GetCurrentPlayerCamera().transform.position;
                freeCam.gameObject.GetComponent<CinemachineCamera>().Lens.OrthographicSize = playerSwitcher.GetCurrentPlayerCamera().GetComponent<CinemachineCamera>().Lens.OrthographicSize;
            }

            //dissable player
            if (currentPlayerCam != null) currentPlayerCam.Priority = 0;
            if (currentPlayer != null) 
            {
                currentPlayer.DisableControls();
            }
        }

        //enable camera controls
        if (cameraMovement != null) cameraMovement.enabled = true;
        inputActions.Player.Disable();
        inputActions.Camera.Enable();

        Debug.Log("Switched to camera control.");
    }

    //auto switch to free cam w/o movement/controls when there are no more player lives
    public void dead_free_cam()
    {
        FreeCamIsActive = true;

        if (playerSwitcher != null)
        {
            CinemachineCamera currentPlayerCam = playerSwitcher.GetCurrentPlayerCamera();
            PlayersControlerScriptsManager currentPlayer = playerSwitcher.GetCurrentPlayerController();
        
            if (freeCam != null) {
                freeCam.Priority = 2;
                freeCam.transform.position = playerSwitcher.GetCurrentPlayerCamera().transform.position;
                freeCam.gameObject.GetComponent<CinemachineCamera>().Lens.OrthographicSize = playerSwitcher.GetCurrentPlayerCamera().GetComponent<CinemachineCamera>().Lens.OrthographicSize;
            }

            if (currentPlayerCam != null) currentPlayerCam.Priority = 0;
            if (currentPlayer != null) 
            {
                currentPlayer.DisableControls();
            }
        }


        if (cameraMovement != null) cameraMovement.enabled = true;

        inputActions.Player.Disable();
        //inputActions.Camera.Enable();

        //stop the walking & climbing sound effect when switching to free cam
        if (audio_manager != null)
        {
            audio_manager.gameObject.SetActive(false); //deactivate the audio manager game obj
        }

        Debug.Log("All players dead - Switched to camera control.");
    }
}
