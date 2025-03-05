using UnityEngine;
using UnityEngine.InputSystem;

public class ControlSwitcher : MonoBehaviour
{
    // 🎥 Camera References
    [SerializeField] private Camera playerCam;
    [SerializeField] private Camera freeCam;

    // 🎮 Script References
    [SerializeField] private CameraController cameraMovement;
    [SerializeField] private PlayersControlerScriptsManager playerMovementController;

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
    /// Switches control back to the player.
    /// </summary>
    private void SwitchToPlayerControl()
    {
        isControllingPlayer = true;

        if (playerCam != null) playerCam.enabled = true;
        if (freeCam != null) freeCam.enabled = false;
        if (playerMovementController != null) playerMovementController.EnagbleControls();
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

        if (freeCam != null) freeCam.enabled = true;
        if (playerCam != null) playerCam.enabled = false;
        if (cameraMovement != null) cameraMovement.enabled = true;
        if (playerMovementController != null) playerMovementController.DisableControls();

        inputActions.Player.Disable();
        inputActions.Camera.Enable();

        Debug.Log("Switched to camera control.");
    }
}
