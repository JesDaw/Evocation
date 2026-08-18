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

    Vector2 _screenReticlePos;
    Action<InputAction.CallbackContext> _castHandler;

    public Stats CasterStats => ActivePlayer.Instance.CurrentPlayer != null
        ? ActivePlayer.Instance.CurrentPlayer.GetComponent<Stats>()
        : null;

    void Awake() => Instance = this;

    void Start()
    {
        detectionRadiusObject.gameObject.SetActive(false);
        SubscribeToInputs();
    }

    void SubscribeToInputs()
    {
        var input = GlobalInputManager.Instance.InputActions.MagicController;
        _castHandler = _ => TryInvoke();
        input.CastSpell.performed += _castHandler;
    }

    void UnsubscribeFromInputs()
    {
        if (GlobalInputManager.Instance == null) return;
        GlobalInputManager.Instance.InputActions.MagicController.CastSpell.performed -= _castHandler;
    }

    void Update()
    {
        if (CurrentState != State.Aiming) return;

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

    void TryInvoke()
    {
        var spell = SpellSwapper.Instance != null ? SpellSwapper.Instance.CurrentSpell : null;
        if (spell == null) return;

        if (CurrentState == State.Idle) AimSpell(spell);
        else if (CurrentState == State.Aiming) CommitCast(spell);
    }

    void AimSpell(SpellDefinition spell)
    {
        if (spell.castMode == SpellCastMode.Aimed)
        {
            CurrentState = State.Aiming;

            _screenReticlePos = new Vector2(Screen.width, Screen.height) * 0.5f;
            detectionRadiusObject.position = ScreenPointToWorldOnGamePlane(_screenReticlePos);
            detectionRadiusObject.gameObject.SetActive(true);
            detectionRadiusObject.localScale = new Vector3(spell.Radius, spell.Radius, 0f);

            if (aimVisual != null) aimVisual.enabled = true; 

            EnterAimCamera();
            GlobalInputManager.Instance.SetMode(InputMode.SpellAim);

            if (DebugLogs) Debug.Log($"Aiming {spell.SpellName}");
        }
        else
        {
            Vector3 castPos = ActivePlayer.Instance.CurrentPlayer.transform.position;
            StartCoroutine(RunCastSequence(spell, castPos));
        }
    }

    void EnterAimCamera()
    {
        var playerCam = ActivePlayer.Instance.GetCurrentPlayerCamera();
        if (playerCam != null && CameraControlSwitcher.Instance != null)
            CameraControlSwitcher.Instance.SwitchToFreeCamAtPosition(playerCam.transform.position, playerCam.Lens.FieldOfView);
    }

    void ExitAimCamera()
    {
        if (CameraControlSwitcher.Instance != null)
            CameraControlSwitcher.Instance.SwitchToPlayerControl();
    }

    void CommitCast(SpellDefinition spell) 
    {
        StartCoroutine(RunCastSequence(spell, detectionRadiusObject.position));
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
            if (aimVisual != null)
                aimVisual.enabled = false;

            GlobalInputManager.Instance.SetMode(InputMode.FreeCam);
        }

        yield return StartCoroutine(spell.RunCastSequence(this, castPosition));

        if (wasAimed)
            detectionRadiusObject.gameObject.SetActive(false);

        CurrentState = State.Idle;
    }

    void OnDestroy() => UnsubscribeFromInputs();
}