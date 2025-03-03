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

    private bool on_player = true; //starts w/ controlling the player

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

        Debug.Log("switched to player");
    }

    public void SwitchToCameraControl()
    {
        on_player = false;
        player_control.Invoke();

        free_cam.gameObject.SetActive(true);
        free_cam.enabled = true;

        player_cam.enabled = false;
        player_cam.gameObject.SetActive(false);

        Debug.Log("switched to camera");
    }
    

}