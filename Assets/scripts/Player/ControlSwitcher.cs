using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class ControlSwitcher : MonoBehaviour
{
    // 🎥 Camera References
    [SerializeField] private CinemachineCamera freeCam;

    // 🎮 Script References
    [SerializeField] private CameraController cameraMovement;
    [SerializeField] private PlayerSwitch playerSwitcher; // Reference to PlayerSwitch script
    [SerializeField] private PlayerMovement playerMovement;

    //for walking sound effect
    private AudioManager audio_manager;

    private InputSystem_Actions inputActions;
    private bool isControllingPlayer = true; // Starts by controlling the player

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Enable();
    }

    private void Start()
    {
        audio_manager = FindAnyObjectByType<AudioManager>();
    }

    private void Update()
    {
        // Switch control using Tab key
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            audio_manager.Play("Switching Cameras"); //plays pop noise when cam switched
            ToggleControl();
        }
    }

    /// <summary>
    /// Toggles between player and camera control.
    /// </summary>
    private void ToggleControl()
    {
        if (isControllingPlayer)
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
        isControllingPlayer = true;

        if (playerSwitcher != null)
        {
            CinemachineCamera currentPlayerCam = playerSwitcher.GetCurrentPlayerCamera();
            PlayersControlerScriptsManager currentPlayer = playerSwitcher.GetCurrentPlayerController();

            if (currentPlayerCam != null) currentPlayerCam.Priority = 2;
            if (freeCam != null) freeCam.Priority = 1;
            if (currentPlayer != null) currentPlayer.EnagbleControls();
        }

        if (cameraMovement != null) cameraMovement.enabled = false;

        inputActions.Camera.Disable();
        inputActions.Player.Enable();

        //walking & climbing sound effects only play when on player control
        if (audio_manager != null)
        {
            audio_manager.gameObject.SetActive(true); //activate the audio manager game obj
        }

        //reset bool for walking & climbing (idk y this part isnt resetting)
        if (playerMovement != null)
        {
            playerMovement.stop_walking();
            playerMovement.stop_climbing();
        }

        Debug.Log("Switched to player control.");
    }

    /// <summary>
    /// Switches control to free camera mode.
    /// </summary>
    public void SwitchToCameraControl()
    {
        isControllingPlayer = false;

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
        inputActions.Camera.Enable();

        //stop the walking & climbing sound effect when switching to free cam
        if (audio_manager != null)
        {
            audio_manager.gameObject.SetActive(false); //deactivate the audio manager game obj
        }
        

        Debug.Log("Switched to camera control.");
    }
}
