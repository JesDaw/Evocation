using UnityEngine;
using UnityEngine.InputSystem;

public class ControlSwitcher : MonoBehaviour
{
    // 🎥 Camera References
    [SerializeField] private Camera freeCam;

    // 🎮 Script References
    [SerializeField] private CameraController cameraMovement;
    [SerializeField] private PlayerSwitch playerSwitcher; // Reference to PlayerSwitch script

    private InputSystem_Actions inputActions;
    private bool isControllingPlayer = true; // Starts by controlling the player

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Enable();
    }

    private void Update()
    {
        // Switch control using Tab key
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
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
            Camera currentPlayerCam = playerSwitcher.GetCurrentPlayerCamera();
            PlayersControlerScriptsManager currentPlayer = playerSwitcher.GetCurrentPlayerController();

            if (currentPlayerCam != null) currentPlayerCam.enabled = true;
            if (freeCam != null) freeCam.enabled = false;
            if (currentPlayer != null) currentPlayer.EnagbleControls();
        }

        if (cameraMovement != null) cameraMovement.enabled = false;

        inputActions.Camera.Disable();
        inputActions.Player.Enable();

        Debug.Log("Switched to player control.");
    }

    /// <summary>
    /// Switches control to free camera mode.
    /// </summary>
    private void SwitchToCameraControl()
    {
        isControllingPlayer = false;

        if (playerSwitcher != null)
        {
            Camera currentPlayerCam = playerSwitcher.GetCurrentPlayerCamera();
            PlayersControlerScriptsManager currentPlayer = playerSwitcher.GetCurrentPlayerController();

            if (currentPlayerCam != null) currentPlayerCam.enabled = false;
            if (currentPlayer != null) currentPlayer.DisableControls();
        }

        if (freeCam != null) {
            freeCam.enabled = true;
            freeCam.transform.position = playerSwitcher.GetCurrentPlayerCamera().transform.position;
            freeCam.orthographicSize = playerSwitcher.GetCurrentPlayerCamera().orthographicSize;
        }
        if (cameraMovement != null) cameraMovement.enabled = true;

        inputActions.Player.Disable();
        inputActions.Camera.Enable();

        Debug.Log("Switched to camera control.");
    }
}
