using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Diagnostics;

public class Interactable : MonoBehaviour
{
    public UnityEvent interactAction;
    List<GameObject> _playersInRange = new List<GameObject>();
    [SerializeField] bool FreeCamActsAsActivePlayer;
    [SerializeField] GameObject InteractionNotificationIcon;
    [SerializeField] float ActivationDuration = 0f;
    bool _iconIsActive = false;
    bool isHolding = false;
    bool ActivePlayerIsInRange;
    float currentHoldTime = 0f;

    void Start()
    {
        // Subscribe to input from GlobalInputManager
        if (GlobalInputManager.Instance != null)
        {
            SubscribeToInputs();
        }
        else
        {
            UnityEngine.Debug.LogWarning("GlobalInputManager not found when Interactable started");
        }
    }

    void OnEnable()
    {

    }

    void OnDisable()
    {
        // Unsubscribe when disabled
        UnsubscribeFromInputs();
    }

    void SubscribeToInputs()
    {
        if (GlobalInputManager.Instance == null) return;

        var playerActions = GlobalInputManager.Instance.InputActions.Player;
        
        // Subscribe to the Interact action
        playerActions.Interact.started += ActionPressed;
        playerActions.Interact.canceled += ActionPressed;
        
        //UnityEngine.Debug.Log("Interactable: Subscribed to Interact action");
    }

    void UnsubscribeFromInputs()
    {
        if (GlobalInputManager.Instance == null) return;

        var playerActions = GlobalInputManager.Instance.InputActions.Player;
        
        playerActions.Interact.started -= ActionPressed;
        playerActions.Interact.canceled -= ActionPressed;
        
        //UnityEngine.Debug.Log("Interactable: Unsubscribed from Interact action");
    }

    void ToggleIcon()
    {
        _iconIsActive = !_iconIsActive;
        InteractionNotificationIcon.SetActive(_iconIsActive);
    }

    public void ActionPressed(InputAction.CallbackContext context)
    {
        // Only respond if the active player is in range
        if (!CheckActivePlayerIsInRange()) return;

        if (context.started)
        {
            isHolding = true;
            currentHoldTime = 0f;
            UnityEngine.Debug.Log("Interact started");
        }
        else if (context.canceled)
        {
            isHolding = false;
            currentHoldTime = 0f;
            UnityEngine.Debug.Log("Interact canceled");
        }
    }

    void Update()
    {
        if (isHolding)
        {
            currentHoldTime += Time.deltaTime;
            //UnityEngine.Debug.Log($"currentHoldTime: {currentHoldTime}");
            
            if (currentHoldTime >= ActivationDuration)
            {
                interactAction?.Invoke();
                UnityEngine.Debug.Log($"Interaction action triggered on {gameObject.name}");
                isHolding = false; 
                currentHoldTime = 0f;
            }
        }
    }

    bool CheckActivePlayerIsInRange()
    {
        if (ActivePlayer.Instance == null || ActivePlayer.Instance.CurrentPlayer == null)
            return false;

        if (_playersInRange.Contains(ActivePlayer.Instance.CurrentPlayer))
            return true;
        
        return false;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        bool playerEntered = false;
        if (collision.gameObject.CompareTag("Player")) playerEntered = true;
        if (collision.gameObject.CompareTag("FreeCam") && FreeCamActsAsActivePlayer) playerEntered = true;
        
        if (playerEntered)
        {
            _playersInRange.Add(collision.gameObject);
           
            if(CheckActivePlayerIsInRange())
            {
                //UnityEngine.Debug.Log($"{collision.gameObject.name} entered interaction range");
                ToggleIcon();
            }
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        bool playerEntered = false;
        if (collision.gameObject.CompareTag("Player")) playerEntered = true;
        if (collision.gameObject.CompareTag("FreeCam") && FreeCamActsAsActivePlayer) playerEntered = true;
        
        if (playerEntered)
        {
            _playersInRange.Remove(collision.gameObject);

            if (!CheckActivePlayerIsInRange())
            {
                //UnityEngine.Debug.Log($"{collision.gameObject.name} left interaction range");
                ToggleIcon();
            }
        }
    }
}