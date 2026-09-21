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
    [SerializeField] float ActivationDuration = 0f;
    [Header("UI Progress")]
    [SerializeField] GameObject ProgressBarContainer;
    [SerializeField] UnityEngine.UI.Slider ProgressSlider;
    [Header("Claim effects")]
    [SerializeField] bool rippleEffect = false;
    [SerializeField] string ClaimLocationSound = "claimLocation";
    [Header("Player animation stuff")]
    [SerializeField] bool HoldPlayer;
    bool playerIsHeald = false;
    PlayerStateMachine HeldPlayer;
    [SerializeField] PlayerAnimationDefinition playerAnimationDefinition;
    public UnityEvent endInteractAction;
    bool _iconIsActive = false;
    bool isHolding = false;
    bool ActivePlayerIsInRange;
    float currentHoldTime = 0f;
    bool LocationClaimed = false;
    [SerializeField] bool DebugLog = false;

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

    public void ClaimLocation()
    {
        LocationClaimed = true;
        ToggleIconOff();
        FModAudioManager.instance.PlaySoundByName(ClaimLocationSound);
        HeldPlayer = ActivePlayer.Instance.GetCurrentPlayerController();

        if(rippleEffect) 
        {
            VisualEffectsManager.Instance.SpawnShockwave(transform.position); 
        }

        if (HoldPlayer)
        {
            CameraControlSwitcher.Instance.SwitchToCameraControl(true);
            ActivePlayer.Instance.GetCurrentPlayerController().PlayAnimationState(playerAnimationDefinition);
            playerIsHeald = true;
        }

        if (DebugLog) Debug.Log($"{gameObject.name} claimed");
    }

    public void UnclaimLocation()
    {
        LocationClaimed = false;
        if (DebugLog) Debug.Log($"{gameObject.name} unclaimed");
       if (playerIsHeald) ActivePlayer.Instance.GetCurrentPlayerController().EndCurrentAnimation();
       playerIsHeald = false;
       HeldPlayer = null;
       
    }

    void ToggleIconOn()
    {
        if (LocationClaimed) return;
        _iconIsActive = true;
        InteractionNotificationIcon.SetActive(_iconIsActive);
    }

    void ToggleIconOff()
    {
        _iconIsActive = false;
        InteractionNotificationIcon.SetActive(_iconIsActive);
    }
    void Update()
    {
        CheckActivePlayerIsInRange();
        if (isHolding)
        {
            currentHoldTime += Time.deltaTime;
            
            if (ProgressSlider != null)
            {
                ProgressSlider.value = currentHoldTime / ActivationDuration;
            }
            
            if (currentHoldTime >= ActivationDuration)
            {
                interactAction?.Invoke();
                
                if (DebugLog) Debug.Log($"Interaction action triggered on {gameObject.name}");
                
                StopHolding();
            }
        }
        if (HeldPlayer != null && HeldPlayer._currentState is PlayerKnockedBackState)
        {
            endInteractAction?.Invoke();
        } 
    }

    public void ActionPressed(InputAction.CallbackContext context)
    {
        if (!ActivePlayerIsInRange) return;

        if (context.started)
        {
            if (ActivationDuration == 0f)
            {
                if (playerIsHeald)
                {
                    endInteractAction?.Invoke();
                    if (DebugLog) Debug.Log($"ActivationDuration = 0 so Interaction endInteraction triggered on {gameObject.name}");
                    
                }
                else
                {
                    interactAction?.Invoke();
                    if (DebugLog) Debug.Log($"ActivationDuration = 0 so Interaction action triggered on {gameObject.name}");
                }
                return;
            }
            isHolding = true;
            currentHoldTime = 0f;
            
            if (ProgressBarContainer != null) ProgressBarContainer.SetActive(true);
            if (ProgressSlider != null) ProgressSlider.value = 0;
            
            if (DebugLog) Debug.Log("Interact started");
        }
        else if (context.canceled)
        {
            StopHolding();
            if (DebugLog) Debug.Log("Interact canceled");
        }
    }
    void StopHolding()
    {
        isHolding = false;
        currentHoldTime = 0f;
        if (ProgressBarContainer != null) ProgressBarContainer.SetActive(false);
    }

    bool CheckActivePlayerIsInRange()
    {
        if (ActivePlayer.Instance == null || ActivePlayer.Instance.CurrentPlayer == null)
            return false;

        if (_playersInRange.Contains(ActivePlayer.Instance.CurrentPlayer))
        {
            ToggleIconOn();
            ActivePlayerIsInRange = true;
            if (DebugLog) Debug.Log($"Player In range");
            return true;
        }
        ActivePlayerIsInRange = false;
        ToggleIconOff();
        return false;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if(DebugLog) Debug.Log($"{collision.gameObject.name} entered interaction range");
        if (CheckIfPlayer(collision)) _playersInRange.Add(collision.gameObject);
        
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if(DebugLog) Debug.Log($"{collision.gameObject.name} left interaction range");
        if (CheckIfPlayer(collision)) _playersInRange.Remove(collision.gameObject);

    }

    bool CheckIfPlayer(Collider2D collision)
    {
        bool player = false;
        if (collision.gameObject.CompareTag("Player")) player = true;
        if (collision.gameObject.CompareTag("FreeCam") && FreeCamActsAsActivePlayer) player = true;
        return player;
    }
}