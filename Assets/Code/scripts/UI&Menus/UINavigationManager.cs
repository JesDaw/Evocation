using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Manages switching between mouse and keyboard navigation modes.
/// Place this on a persistent GameObject in your scene (e.g. a UIManager or GameManager object).
///
/// Keyboard mode: cursor hidden, EventSystem drives selection.
/// Mouse mode:    cursor visible, EventSystem selection cleared so nav keys don't steal focus.
///
/// Selection priority when entering keyboard mode:
///   1. screenDefaultButton   — set by SceneActivity.StartActivity() via defaultSelectedButton
///   2. lastHighlightedButton — whichever button was most recently highlighted by either
///                              mouse hover OR keyboard navigation, in chronological order
///
/// Screen-change timing
/// ────────────────────
/// ActivateWithFade activates the destination screen inside a fade callback — many frames
/// after the source button's OnClick fires. SceneActivity.StartActivity() calls
/// RegisterScreenDefault() after SetActive(true), so the button is always active by
/// the time we try to select it. No polling or frame-deferral is needed.
/// </summary>
public class UINavigationManager : MonoBehaviour
{
    public static UINavigationManager Instance { get; private set; }

    // ── State ─────────────────────────────────────────────────────────────────

    private bool isKeyboardMode = false;
    private EventSystem eventSystem;

    /// <summary>
    /// The most recently highlighted button, regardless of whether it was reached
    /// by mouse hover or keyboard navigation. Written by both RegisterMouseHover()
    /// and RegisterKeyboardSelect() so the last-touched button always wins.
    /// </summary>
    public UIButtons lastHighlightedButton;

    /// <summary>Stored by RegisterScreenDefault(); consumed by SelectBestKeyboardTarget().</summary>
    private UIButtons screenDefaultButton;

    // Prevents stacking multiple deferred-recovery coroutines in one burst
    private bool recoveryPending = false;

    // Axis edge-detection
    //private bool wasAxisNavigating = false;
    private const float AxisThreshold = 0.5f;

    // ── Unity ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        eventSystem = EventSystem.current;
        if (eventSystem == null)
            Debug.LogWarning("[UINavigationManager] No EventSystem found in scene.", this);
    }

    private void Update()
    {
        if (GlobalInputManager.Instance.MenuNavigation == false) 
        {
            //Debug.Log("here");
            return;
        }
        if (DetectKeyboardNavigation())
        {
            if (isKeyboardMode)
            {
                if (eventSystem.currentSelectedGameObject == null && !recoveryPending)
                    StartCoroutine(DeferredRecover());
            }
            else
            {
                SwitchToKeyboardMode();
            }
        }
        else if (DetectMouseMovement())
        {
            if (isKeyboardMode) SwitchToMouseMode();
        }
    }

    // ── Detection ─────────────────────────────────────────────────────────────

    private bool DetectKeyboardNavigation()
    {
        if (GlobalInputManager.Instance.InputActions.UI.Navigate.WasPerformedThisFrame() || 
        GlobalInputManager.Instance.InputActions.UI.ConfirmDialogue.WasPerformedThisFrame())
        {
            return true;
        }
        return false;

        // 2. Read the current 2D Vector from your Navigate action (Replaces GetAxisRaw)
        // Vector2 navInput = InputSystem_Actions.InputActions.Player.Navigate.ReadValue<Vector2>();
        
        // 3. Check if the keys are currently being held past your threshold
        // bool axisActive = Mathf.Abs(navInput.x) > AxisThreshold || Mathf.Abs(navInput.y) > AxisThreshold;
        
        // 4. Determine if it was just pressed this frame
        // bool justPressed = axisActive && !wasAxisNavigating;
        // wasAxisNavigating = axisActive;
    }

    private bool DetectMouseMovement()
    {
        return Input.GetAxis("Mouse X") != 0f || Input.GetAxis("Mouse Y") != 0f;
    }

    // ── Mode switching ────────────────────────────────────────────────────────

    private void SwitchToKeyboardMode()
    {
        isKeyboardMode = true;
        GlobalInputManager.Instance.DisableCursor();

        if (screenDefaultButton == null)
            SelectBestKeyboardTarget();

        //Debug.Log("[UINavigationManager] Switched to keyboard mode.");
    }

    private void SwitchToMouseMode()
    {
        isKeyboardMode = false;
        GlobalInputManager.Instance.EnableCursor();

        // Manually reset visuals on the outgoing button because SetSelectedGameObject(null)
        // does NOT fire OnDeselect on UIButtons.
        if (eventSystem.currentSelectedGameObject != null)
        {
            var btn = eventSystem.currentSelectedGameObject.GetComponent<UIButtons>();
            if (btn != null && btn != lastHighlightedButton)
                btn.ResetToOriginalState();
        }

        eventSystem.SetSelectedGameObject(null);
        //Debug.Log("[UINavigationManager] Switched to mouse mode.");
    }

    // ── Selection logic ───────────────────────────────────────────────────────

    /// <summary>
    /// Waits one frame after a keypress so this frame's OnClick chain can finish,
    /// then recovers a lost selection. screenDefaultButton (if any) takes priority.
    /// </summary>
    private IEnumerator DeferredRecover()
    {
        recoveryPending = true;
        yield return null;

        recoveryPending = false;

        if (isKeyboardMode && eventSystem.currentSelectedGameObject == null)
            SelectBestKeyboardTarget();
    }

    /// <summary>
    /// Selects the best available button.
    /// Priority: screenDefaultButton → lastHighlightedButton.
    /// </summary>
    private void SelectBestKeyboardTarget()
    {
        UIButtons target = null;

        if (screenDefaultButton != null && screenDefaultButton.gameObject.activeInHierarchy)
        {
            target = screenDefaultButton;
            screenDefaultButton = null; // consume
        }
        else if (lastHighlightedButton != null && lastHighlightedButton.gameObject.activeInHierarchy)
        {
            target = lastHighlightedButton;
        }

        if (target != null)
            eventSystem.SetSelectedGameObject(target.gameObject);
        else
            Debug.LogWarning("[UINavigationManager] No valid button found to select. " +
                                $"Set defaultSelectedButton on the SceneActivity.");
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by UIButtons.OnPointerEnter.
    /// Updates lastHighlightedButton so keyboard mode can resume from here.
    /// </summary>
    public void RegisterMouseHover(UIButtons button)
    {
        lastHighlightedButton = button;
    }

    /// <summary>
    /// Called by UIButtons.OnSelect.
    /// Updates lastHighlightedButton so mouse mode can resume from here.
    /// </summary>
    public void RegisterKeyboardSelect(UIButtons button)
    {
        lastHighlightedButton = button;
    }

    /// <summary>
    /// Called by SceneActivity.StartActivity() via defaultSelectedButton.
    /// Because StartActivity() calls SetActive(true) first, the button is always
    /// active by the time this runs — no polling required.
    ///
    /// • Keyboard mode → select the button immediately.
    /// • Mouse mode    → store it; selected the next time a nav key is pressed.
    /// </summary>
    public void RegisterScreenDefault(UIButtons button)
    {
        if (button == null)
        {
            Debug.LogWarning("[UINavigationManager] RegisterScreenDefault called with null button.");
            return;
        }

        // Clear last-highlighted so it can't win over the new screen's default.
        lastHighlightedButton = null;
        screenDefaultButton = button;

        if (isKeyboardMode)
        {
            eventSystem.SetSelectedGameObject(button.gameObject);
            screenDefaultButton = null; // consumed
        }
        // Otherwise consumed by SelectBestKeyboardTarget on the next nav keypress.
    }
}