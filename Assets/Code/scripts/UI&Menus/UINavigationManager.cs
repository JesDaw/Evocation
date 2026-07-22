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
/// </summary>
public class UINavigationManager : MonoBehaviour
{
    public static UINavigationManager Instance;
    [SerializeField] bool StartInMousMode = false;
    [SerializeField] bool DebugLogs;

    private bool isKeyboardMode = false;
    private EventSystem eventSystem;
    public UIButtons lastHighlightedButton;
    private UIButtons screenDefaultButton;
    private bool recoveryPending = false;
    private const float AxisThreshold = 0.5f;

    void LogDebug(string message)
    {
        if (DebugLogs) this.Log("");
    }

    private void Awake()
    {
         if (!SingletonManager.Initialize(this, ref Instance))
        {
            return;
        }
        eventSystem = EventSystem.current;
    }

    void Start()
    {
        ValidationUtilities.CheckMonoBehaviours(this, GlobalInputManager.Instance, eventSystem);
        if (DebugLogs) this.Log($"Awake — starting in {(StartInMousMode ? "mouse" : "keyboard")} mode.");
        if (StartInMousMode)
        {
            SwitchToMouseMode();
        }
        else
        {
            SwitchToKeyboardMode();
        }
    }

    private void Update()
    {
        if (GlobalInputManager.Instance.MenuNavigation == false) 
        {
            return;
        }
        CheckForNavigationModeSwitch();
        
    }

    void CheckForNavigationModeSwitch()
    {
        if (DetectKeyboardNavigation())
        {
            if (isKeyboardMode)
            {
                if (!recoveryPending) StartCoroutine(DeferredRecover());
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


    private bool DetectKeyboardNavigation()
    {
        if (GlobalInputManager.Instance.InputActions.UI.Navigate.WasPerformedThisFrame())
        {
            if (DebugLogs) this.Log("DetectKeyboardNavigation: Navigate action performed this frame.");
            return true;
        }
        if (GlobalInputManager.Instance.InputActions.UI.ConfirmDialogue.WasPerformedThisFrame())
        {
            if (DebugLogs) this.Log("DetectKeyboardNavigation: ConfirmDialogue action performed this frame.");
            return true;
        }
        return false;
    }

    private bool DetectMouseMovement()
    {
        bool moved = Input.GetAxis("Mouse X") != 0f || Input.GetAxis("Mouse Y") != 0f;
        return moved;
    }


    private void SwitchToKeyboardMode()
    {
        if (DebugLogs) this.Log("SwitchToKeyboardMode: entering keyboard mode.");
        isKeyboardMode = true;
        GlobalInputManager.Instance.DisableCursor();
        SelectBestKeyboardTarget();
    }

    private void SwitchToMouseMode()
    {
        if (DebugLogs) this.Log("SwitchToMouseMode: entering mouse mode.");
        isKeyboardMode = false;
        GlobalInputManager.Instance.EnableCursor();
        if (eventSystem.currentSelectedGameObject != null)
        {
            var btn = eventSystem.currentSelectedGameObject.GetComponent<UIButtons>();
            if (btn != null && btn != lastHighlightedButton)
                btn.ResetToOriginalState();
        }

        eventSystem.SetSelectedGameObject(null);
    }
    private IEnumerator DeferredRecover()
    {
        recoveryPending = true;
        if (DebugLogs) this.Log("DeferredRecover: waiting one frame to check for lost selection.");
        yield return null;

        if (isKeyboardMode && eventSystem.currentSelectedGameObject == null) 
        {
            if (DebugLogs) this.Log("DeferredRecover: selection was lost, re-selecting.");
            SelectBestKeyboardTarget();
        }
        recoveryPending = false;
    }

    private void SelectBestKeyboardTarget()
    {
        UIButtons target = null;

        if (screenDefaultButton != null && screenDefaultButton.gameObject.activeInHierarchy)
        {
            target = ConsumeScreenDefault();
        }
        else if (lastHighlightedButton != null && lastHighlightedButton.gameObject.activeInHierarchy)
        {
            target = lastHighlightedButton;
        }

        if (target != null)
        {
            if (DebugLogs) this.Log($"SelectBestKeyboardTarget: selecting '{target.name}'.");
            eventSystem.SetSelectedGameObject(target.gameObject);
        }
        else
            this.LogWarning($"No valid button found to select. Set defaultSelectedButton on the SceneActivity.");
    }

    private UIButtons ConsumeScreenDefault()
    {
        var button = screenDefaultButton;
        if (DebugLogs) this.Log($"ConsumeScreenDefault: consuming '{(button != null ? button.name : "null")}'.");
        screenDefaultButton = null;
        return button;
    }

    public void RegisterHighlighted(UIButtons button)
    {
        if (DebugLogs) this.Log($"RegisterHighlighted: '{(button != null ? button.name : "null")}'.");
        lastHighlightedButton = button;
    }
    public void ClearHighlightedButtonIfBelongsTo(Transform owner)
    {
        if (lastHighlightedButton != null && lastHighlightedButton.transform.IsChildOf(owner))
        {
            if (DebugLogs) this.Log($"ClearHighlightedButtonIfBelongsTo: clearing '{lastHighlightedButton.name}' (owned by '{owner.name}').");
            lastHighlightedButton = null;
        }
    }
    public void RegisterScreenDefault(UIButtons button)
    {
        if (button == null)
        {
            Debug.LogWarning("[UINavigationManager] RegisterScreenDefault called with null button.");
            return;
        }

        if (DebugLogs) this.Log($"RegisterScreenDefault: '{button.name}' (isKeyboardMode: {isKeyboardMode}).");
        lastHighlightedButton = null;
        screenDefaultButton = button;

        if (isKeyboardMode)
        {
            var target = ConsumeScreenDefault();
            eventSystem.SetSelectedGameObject(target.gameObject);
        }
    }
}