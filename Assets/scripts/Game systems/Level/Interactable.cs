using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    public UnityEvent interactAction;
    List<GameObject> _playersInRange = new List<GameObject>();
    [SerializeField] ActivePlayer activePlayer;
    bool ActivePlayerIsInRange;


    public void ActionPressed(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        if (_playersInRange.Count == 0) return;
        if (CheckActivePlayerIsInRange()) interactAction.Invoke(); 
    }

    void TogglePlayerNotification()
    {
        // gameObject.notification.enabled = !gameObject.notification.enabled;
    }

    bool CheckActivePlayerIsInRange()
    {
        if (_playersInRange.Contains(activePlayer.CurrentPlayer)) return true;
        else return false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            _playersInRange.Add(collision.gameObject);
           
            if(CheckActivePlayerIsInRange())
            {
                TogglePlayerNotification();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            _playersInRange.Remove(collision.gameObject);

            if (CheckActivePlayerIsInRange())
            {
                TogglePlayerNotification();
            }
        }
    }
}
