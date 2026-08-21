using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
    [SerializeField] bool DebugLogs = false;
    bool returnToFreeCam;

    Vector2 _screenReticlePos;
    Action<InputAction.CallbackContext> _castHandler;

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
        GlobalInputManager.Instance.InputActions.MagicController.AimSpell.performed += ToggleAimSpell;
        GlobalInputManager.Instance.InputActions.MagicController.CastSpell.performed += CommitCast;
    }

    void OnDestroy() => UnsubscribeFromInputs();
    void UnsubscribeFromInputs()
    {
        GlobalInputManager.Instance.InputActions.MagicController.AimSpell.performed -= ToggleAimSpell;
        GlobalInputManager.Instance.InputActions.MagicController.CastSpell.performed -= CommitCast;
    }
    #endregion
    #region aiming
    void ToggleAimSpell(InputAction.CallbackContext context)
    {
        if (ActivePlayer.Instance.CurrentPlayer == null)
        {
            ExitAimCamera();
            return;
        }
        if (CurrentState == State.Idle)
        {
            if (SpellSwapper.Instance.CurrentSpell.castMode == SpellCastMode.Aimed) // means we use the aiming logic not that we are already aiming
            {
                EnterAimCamera();
            }
        }
        else
        {
            ExitAimCamera();
        }
    }

    void EnterAimCamera()
    {
        CurrentState = State.Aiming;

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
    
    void Update()
    {
        if (CurrentState != State.Aiming) return;
        SpellAimMovement();
        if (ActivePlayer.Instance.CurrentPlayer == null)
        {
            ExitAimCamera();
        }
    }

    void SpellAimMovement()
    {
        Vector2 delta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        _screenReticlePos += delta * lookSensitivity;
        _screenReticlePos.x = Mathf.Clamp(_screenReticlePos.x, 0, Screen.width);
        _screenReticlePos.y = Mathf.Clamp(_screenReticlePos.y, 0, Screen.height);

        if (screenReticleUI != null) screenReticleUI.position = _screenReticlePos;

        detectionRadiusObject.position = ScreenPointToWorldOnGamePlane(_screenReticlePos);
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
        Debug.Log("exiting.");
        CurrentState = State.Idle;
        detectionRadiusObject.gameObject.SetActive(false);
        if (aimVisual != null) aimVisual.enabled = false;

        if (!returnToFreeCam) CameraControlSwitcher.Instance.SwitchToPlayerControl();
        else CameraControlSwitcher.Instance.SwitchToCameraControl(true);
    }
    #endregion
    #region Casting
    void CommitCast(InputAction.CallbackContext context) 
    {
        if (SpellSwapper.Instance.CurrentSpell == null) return;
        if (CurrentState == State.Aiming && SpellSwapper.Instance.CurrentSpell.castMode == SpellCastMode.Aimed)
        {
            StartCoroutine(RunCastSequence(SpellSwapper.Instance.CurrentSpell, detectionRadiusObject.position));
        }
        StartCoroutine(RunCastSequence(SpellSwapper.Instance.CurrentSpell, ActivePlayer.Instance.CurrentPlayer.transform.position));
    }

    IEnumerator RunCastSequence(SpellDefinition spell, Vector3 castPosition)
    {
        if (ManaSystem.Instance == null)
        {
            Debug.LogError("SpellCaster: ManaSystem.Instance is null.");
            yield break;
        }

        if (!ManaSystem.Instance.SpendMana(spell.Cost))
        {
            Debug.Log($"Not enough mana to cast {spell.SpellName}.");
            yield break;
        }

        CurrentState = State.Casting;
        bool wasAimed = spell.castMode == SpellCastMode.Aimed;

        if (wasAimed)
        {
            if (aimVisual != null) aimVisual.enabled = false;
            GlobalInputManager.Instance.SetMode(InputMode.FreeCam);
        }

        yield return StartCoroutine(spell.RunCastSequence(this, castPosition));

        if (wasAimed) detectionRadiusObject.gameObject.SetActive(false);

        CurrentState = State.Idle;
    }
    #endregion
}