//using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class ControlSwitcher : MonoBehaviour
{
    //events
    public UnityEvent player_control;
    public UnityEvent camera_control;

    //cameras
    public Camera player_cam;
    public Camera free_cam;

    //script references
    public CameraController camera_movement;
    public PlayerMovement player_movement;

    private InputSystem_Actions input_actions;

    private bool on_player = true; //starts w/ controlling the player


    private void Awake()
    {
        input_actions = new InputSystem_Actions();
        input_actions.Enable();
    }

    private void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)//change the control w/ tab key
        {
            ToggleControl();
        }
    }

    private void ToggleControl()
    {
        if (on_player)
        {
            SwitchToCameraControl();
        }
        else
        {
            SwitchToPlayerControl();
        }
    }

    public void SwitchToPlayerControl()
    {
        on_player = true;
        player_control.Invoke(); //triggerr the unity event

        player_cam.gameObject.SetActive(true);
        player_cam.enabled = true;

        free_cam.enabled = false;
        free_cam.gameObject.SetActive(false);

        if (player_movement != null)
            player_movement.enabled = true;

        //disable the camera controls
        //disable the camera controller script
        if (camera_movement != null)
            camera_movement.enabled = false;

        //switching the input system action map
        input_actions.Camera.Disable();
        input_actions.Player.Enable();

        Debug.Log("switched to player");
    }

    public void SwitchToCameraControl()
    {
        on_player = false;
        camera_control.Invoke();

        free_cam.gameObject.SetActive(true);
        free_cam.enabled = true;

        player_cam.enabled = false;
        player_cam.gameObject.SetActive(false);

        camera_movement.enabled = true;

         if (camera_movement != null)
            camera_movement.enabled = true;

        //disable the player movmenet script when switched to camera controls
        if (player_movement != null)
            player_movement.enabled = false;

        //switching the input system action map
        input_actions.Player.Disable();
        input_actions.Camera.Enable();

        Debug.Log("switched to camera");
    }
}
