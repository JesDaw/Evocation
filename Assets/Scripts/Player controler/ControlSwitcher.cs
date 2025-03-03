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
        //if (playerInput.currentActionMap.name == "Player") //when controlling player
        //{
        //    SwitchToCameraControl();
        //}
        //else
        //{
        //    SwitchToPlayerControl();
        //}

    }

    public void SwitchToPlayerControl()
    {
        //StartCoroutine(SwitchActionMap("Player"));
        //playerInput.SwitchCurrentActionMap("Player");
        //playerMovementScript.enabled = true;
        //cameraControllerScript.enabled = false;


        player_cam.gameObject.SetActive(true);
        player_cam.enabled = true;

        free_cam.enabled = false;
        free_cam.gameObject.SetActive(false);

        if (player_movement != null)
            player_movement.enabled = true;

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
        //StartCoroutine(SwitchActionMap("Camera"));
        //playerInput.SwitchCurrentActionMap("Camera");
        //playerMovementScript.enabled = false;
        //cameraControllerScript.enabled = true;


        player_cam.enabled = false;
        player_cam.gameObject.SetActive(false);

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
    //private IEnumerator SwitchActionMap(string actionMapName)
    //{
    //    yield return null; // Wait one frame to ensure input system updates

    //    playerInput.SwitchCurrentActionMap(actionMapName);
    //    Debug.Log("Switched to: " + actionMapName);
    //}

}