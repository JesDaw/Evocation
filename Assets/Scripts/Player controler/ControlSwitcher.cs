//using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class ControlSwitcher : MonoBehaviour
{
    //events
    public UnityEvent player_control;
    public UnityEvent camera_control;

    private bool on_player = true;

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

        //currentMode = "Player";
    }

    public void SwitchToCameraControl()
    {
        //StartCoroutine(SwitchActionMap("Camera"));
        //playerInput.SwitchCurrentActionMap("Camera");
        //playerMovementScript.enabled = false;
        //cameraControllerScript.enabled = true;

        //currentMode = "Camera";
    }

    //private IEnumerator SwitchActionMap(string actionMapName)
    //{
    //    yield return null; // Wait one frame to ensure input system updates

    //    playerInput.SwitchCurrentActionMap(actionMapName);
    //    Debug.Log("Switched to: " + actionMapName);
    //}
}