using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class RebindControls : MonoBehaviour
{
    public InputActionAsset InputActions;

    InputActionRebindingExtensions.RebindingOperation m_rebindingOperation;

    InputAction m_attackAction;
    Button m_rebindButton;
    Label m_rebindLabel;

    void Awake()
    {
        m_attackAction = InputActions.FindAction("attack");

        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        m_rebindButton = root.Q<Button>("RebindButton");
        m_rebindLabel = root.Q<Label>("RebindLabel");
    }

    void OnEnable()
    {
        m_rebindButton.Focus();
        m_rebindButton.clicked += Rebind;
    }

    void OnDisable()
    {
        m_rebindButton.clicked -= Rebind;
    }

    void Rebind()
    {
        InputActions.FindActionMap("Player").Disable();
        m_rebindLabel.text = "Choose a new button";
        m_rebindButton.SetEnabled(false);

        m_rebindingOperation = m_attackAction.PerformInteractiveRebinding().OnComplete(operation => RebindCompleted());
        m_rebindingOperation.Start();
    }

    void RebindCompleted()
    {
        m_rebindingOperation.Dispose();

        string newBinding = m_attackAction.bindings[0].effectivePath;
        m_rebindLabel.text = $"Rebind completed: {newBinding}";

        InputActions.FindActionMap("Player").Enable();

        var rebinds = InputActions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("rebinds", rebinds);
    }
}