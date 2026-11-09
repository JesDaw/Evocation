using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using UnityEngine.Events;

public class SpellCaster : MonoBehaviour
{
    public static SpellCaster Instance { get; private set; }

    public enum State { Idle, Aiming, Casting }
    public State CurrentState { get; private set; } = State.Idle;
    public bool IsBusy => CurrentState != State.Idle;

    [SerializeField] Transform detectionRadiusObject;
    [SerializeField] Image aimVisual;
    [SerializeField] RectTransform screenReticleUI;
    [SerializeField] float lookSensitivity = 8f;
    [SerializeField] PlayerAnimationDefinition playerAnimationDefinition;
    [SerializeField] RecticalSnap recticalSnap;
    bool returnToFreeCam;

    Vector2 _screenReticlePos;
    Action<InputAction.CallbackContext> _castHandler;
    [Header("Events")]
    [SerializeField] UnityEvent OnSpellCastEnter;
    [SerializeField] UnityEvent OnSpellCastExit;
    [Header("Debug")]
    [SerializeField] bool DebugLogs = false;

    public Stats CasterStats => ActivePlayer.Instance.CurrentPlayer != null
        ? ActivePlayer.Instance.CurrentPlayer.GetComponent<Stats>()
        : null;

    #region startup
    void Awake() => Instance = this;

    void Start()
    {
        detectionRadiusObject.gameObject.SetActive(false);
        SubscribeToInputs();
    }

    void SubscribeToInputs()
    {
        GlobalInputManager.Instance.InputActions.MagicController.AimSpell.performed += OnAimPressed;
        GlobalInputManager.Instance.InputActions.MagicController.CastSpell.performed += CommitCast;
    }

    void OnDestroy() => UnsubscribeFromInputs();
    void UnsubscribeFromInputs()
    {
        if (GlobalInputManager.Instance == null) return;
        GlobalInputManager.Instance.InputActions.MagicController.AimSpell.performed -= OnAimPressed;
        GlobalInputManager.Instance.InputActions.MagicController.CastSpell.performed -= CommitCast;
    }
    #endregion
    #region aiming
    // The button being held to aim (Right Mouse). Polled in Update rather than using the action's
    // "canceled" callback, because switching input modes re-enables the action mid-hold and the
    // release would never be reported.
    ButtonControl _heldAimButton;

    // Aim button (hold Right Mouse):
    //   Aimed spell  -> aim while held, release to cancel, Left Mouse to cast.
    //   Self-cast    -> cast immediately (e.g. heal).
    void OnAimPressed(InputAction.CallbackContext context)
    {
        if (CurrentState != State.Idle) return;

        var spell = SpellSwapper.Instance.CurrentSpell;
        if (spell == null) return;

        if (spell.castMode == SpellCastMode.Aimed)
        {
            if (ManaSystem.Instance == null || ManaSystem.Instance.PlayerMana < spell.Cost)
            {
                if (DebugLogs) Debug.Log($"Not enough mana to aim {spell.SpellName}");
                return;
            }
            _heldAimButton = context.control as ButtonControl;
            EnterAimCamera();
            OnSpellCastEnter.Invoke();
        }
        else if (ActivePlayer.Instance.CurrentPlayer != null)
        {
            StartCoroutine(RunCastSequence(spell, ActivePlayer.Instance.CurrentPlayer.transform.position));
        }
    }

    void CancelAim()
    {
        _heldAimButton = null;
        ExitAimCamera();
        OnSpellCastExit.Invoke();
    }

    void EnterAimCamera()
    {
        CurrentState = State.Aiming;
        PlaySpellAnimation(true);

        _screenReticlePos = new Vector2(Screen.width, Screen.height) * 0.5f;
        detectionRadiusObject.position = ScreenPointToWorldOnGamePlane(_screenReticlePos);
        detectionRadiusObject.gameObject.SetActive(true);
        detectionRadiusObject.localScale = new Vector3(SpellSwapper.Instance.CurrentSpell.Radius, SpellSwapper.Instance.CurrentSpell.Radius, 0f);

        if (aimVisual != null) aimVisual.enabled = true;
        if (CameraControlSwitcher.Instance != null) 
        {
            if (CameraControlSwitcher.Instance.FreeCamIsActive) returnToFreeCam = true;
            else returnToFreeCam = false;
            CameraControlSwitcher.Instance.SwitchToCameraControl(true);
        }
        GlobalInputManager.Instance.SetMode(InputMode.SpellAim);
        
        

        if (DebugLogs) Debug.Log($"Aiming {SpellSwapper.Instance.CurrentSpell.SpellName}");
    }

    void PlaySpellAnimation(bool Play = false)
    {
        if (ActivePlayer.Instance.CurrentPlayer == null) return;
        if(!Play)
        {
            ActivePlayer.Instance.GetCurrentPlayerController().EndCurrentAnimation();
            return;
        }
        else
        {
            ActivePlayer.Instance.GetCurrentPlayerController().PlayAnimationState(playerAnimationDefinition);
        }
    }
    
    void Update()
    {
        if (CurrentState != State.Aiming) return;

        if (_heldAimButton != null && !_heldAimButton.isPressed)
        {
            CancelAim();
            return;
        }
        SpellAimMovement();
    }

    void SpellAimMovement()
    {
        Vector2 delta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        _screenReticlePos += delta * lookSensitivity;
        _screenReticlePos.x = Mathf.Clamp(_screenReticlePos.x, 0, Screen.width);
        _screenReticlePos.y = Mathf.Clamp(_screenReticlePos.y, 0, Screen.height);

        if (screenReticleUI != null) screenReticleUI.position = _screenReticlePos;

        detectionRadiusObject.position = recticalSnap.SnapToGround(ScreenPointToWorldOnGamePlane(_screenReticlePos));
    }

    Vector3 ScreenPointToWorldOnGamePlane(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        Plane gamePlane = new Plane(Vector3.forward, Vector3.zero);
        if (gamePlane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);

        return detectionRadiusObject.position;
    }
    void ExitAimCamera()
    {  
        if (CurrentState == State.Idle) return;
        CurrentState = State.Idle;
        detectionRadiusObject.gameObject.SetActive(false);
        if (aimVisual != null) aimVisual.enabled = false;
        PlaySpellAnimation(false);

        if (!returnToFreeCam) CameraControlSwitcher.Instance.SwitchToPlayerControl();
        else CameraControlSwitcher.Instance.SwitchToCameraControl(true);
        
    }
    #endregion
    #region Casting
    // Cast button (Left Mouse). Also the Attack button, so it only does something while aiming.
    void CommitCast(InputAction.CallbackContext context)
    {
        if (CurrentState != State.Aiming) return;
        var spell = SpellSwapper.Instance.CurrentSpell;
        if (spell == null || spell.castMode != SpellCastMode.Aimed) return;

        if (ManaSystem.Instance == null || ManaSystem.Instance.PlayerMana < spell.Cost)
        {
            if (DebugLogs) Debug.Log($"Not enough mana for {spell.SpellName}");
            CancelAim();
            return;
        }

        Vector3 target = detectionRadiusObject.position;
        _heldAimButton = null;
        detectionRadiusObject.gameObject.SetActive(false);
        if (aimVisual != null) aimVisual.enabled = false;
        OnSpellCastExit.Invoke();

        // Stay in free cam while the spell plays so the player sees it land, then hand control back.
        GlobalInputManager.Instance.SetMode(InputMode.FreeCam);
        StartCoroutine(CastAimedThenRestoreControl(spell, target));
    }

    IEnumerator CastAimedThenRestoreControl(SpellDefinition spell, Vector3 target)
    {
        yield return StartCoroutine(RunCastSequence(spell, target));
        CurrentState = State.Idle;
        PlaySpellAnimation(false);

        // Return to the hero if that's where the player aimed from, unless they've switched cameras themselves.
        if (!returnToFreeCam && CameraControlSwitcher.Instance != null && CameraControlSwitcher.Instance.FreeCamIsActive)
            CameraControlSwitcher.Instance.SwitchToPlayerControl();
    }

    IEnumerator RunCastSequence(SpellDefinition spell, Vector3 castPosition)
    {
        if (ManaSystem.Instance == null)
        {
            Debug.LogError("SpellCaster: ManaSystem.Instance is null.");
            yield break;
        }

        if (!ManaSystem.Instance.SpendMana(spell.Cost)) yield break;

        CurrentState = State.Casting;
        PlaySpellAnimation(false);
        yield return StartCoroutine(spell.RunCastSequence(this, castPosition));
        CurrentState = State.Idle;
    }
    #endregion
}
