using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
public class PlayerDangerDetector : MonoBehaviour
{
    bool canAutoSwitch = false;
    List<GameObject> enemies = new List<GameObject>();

    void Start()
    {
        GlobalInputManager.Instance.InputActions.ControlManager.PingedPlayer.performed += OnDangerAutoSwitchPressed;
    }
    void OnDisable()
    {
        GlobalInputManager.Instance.InputActions.ControlManager.PingedPlayer.performed -= OnDangerAutoSwitchPressed;
    }

    
    public void SetTargeted(bool isTargeted)
    {
        
        if (isTargeted && (ActivePlayer.Instance.CurrentPlayer != gameObject || CameraControlSwitcher.Instance.FreeCamIsActive))
        {
            PlayerDangerNotification.Instance.Activate();
            canAutoSwitch = true;
        }
        else
        {
            PlayerDangerNotification.Instance.Deactivate();
            canAutoSwitch = false;
        }
    }

    public void OnDangerAutoSwitchPressed(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (canAutoSwitch) PlayerSwitch.Instance.SwitchToPlayer(gameObject);
        
    }
    void Update()
    {
        if (canAutoSwitch == true && ActivePlayer.Instance.CurrentPlayer == gameObject && !CameraControlSwitcher.Instance.FreeCamIsActive)
        {
            PlayerDangerNotification.Instance.Deactivate();
            canAutoSwitch = false;
        }
    }
}
