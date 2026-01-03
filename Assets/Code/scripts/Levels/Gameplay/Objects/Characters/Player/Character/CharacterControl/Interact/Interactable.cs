using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    public UnityEvent interactAction;
    List<GameObject> _playersInRange = new List<GameObject>();
    [SerializeField] bool FreeCamActsAsActivePlayer;
    [SerializeField] GameObject InteractionNotificationIcon;
    bool _iconIsActive = false;
    bool ActivePlayerIsInRange;

    void ToggleIcon()
    {
        _iconIsActive = !_iconIsActive;
        InteractionNotificationIcon.SetActive(_iconIsActive);
    }

    public void ActionPressed(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        if (_playersInRange.Count == 0) return;
        if (CheckActivePlayerIsInRange()) interactAction.Invoke(); 
    }

    bool CheckActivePlayerIsInRange()
    {
        if (_playersInRange.Contains(ActivePlayer.Instance.CurrentPlayer)) return true;
        else return false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        bool playerEntered = false;
        if (collision.gameObject.CompareTag("Player")) playerEntered = true;
        if (collision.gameObject.CompareTag("FreeCam") && FreeCamActsAsActivePlayer) playerEntered = true;
        
        if (playerEntered)
        {
            _playersInRange.Add(collision.gameObject);
           
            if(CheckActivePlayerIsInRange())
            {
                //Debug.Log("in range");
                ToggleIcon();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
                bool playerEntered = false;
        if (collision.gameObject.CompareTag("Player")) playerEntered = true;
        if (collision.gameObject.CompareTag("FreeCam") && FreeCamActsAsActivePlayer) playerEntered = true;
        if (playerEntered)
        {
            _playersInRange.Remove(collision.gameObject);

            if (!CheckActivePlayerIsInRange())
            {
                //Debug.Log("out of range");
                ToggleIcon();
            }
        }
    }
}
