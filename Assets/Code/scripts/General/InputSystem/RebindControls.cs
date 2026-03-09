using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class RebindControls : MonoBehaviour
{
    public InputActionAsset InputActions;

    private InputActionRebindingExtensions.RebindingOperation m_rebindingOperation;

    private InputAction m_jumpAction;
    private Button m_rebindButton;
    private Label m_rebindLabel;

    private void Awake()
    {
        m_walkLeftAction = InputActions.FindAction("Left");
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        m_rebindButton = root.Q<m_rebindButton>("RebindButton");
        m_rebindLabel = root.Q<m_rebindLabel>("RebindLabel");
    }

    private void OnEnable()
    {
        m_rebindbutton.Focus();
        m_rebindButton.clicked += Rebind;
    }

    private void OnDisable()
    {
        m_rebindButton.clicked -= Rebind;
    }

    void Rebind()
    {
        InputActions.FindActionMap("Player").Disable();
        m_rebindLabel.text = "Choose a new button";
        m_rebindButton.SetEnabled(false);
        m_rebindingOperation = m_jumpAction.PerformInteractiveRebinding().OnComplete(operation => RebindCompleted());
        m_rebindingOperation.Start();
    }

    void RebindCompleted()
    {
        m_rebindingOperation.Dispose();

        string newBinding = m_jumpAction.bindings[0].effectivePath;
        m_rebindLabel.text = $"Rebind completed: {newBinding}";

        InputActions.FindActionMap("Player").Enable();

        var rebinds = InputActions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("rebinds", rebinds);
    }
}
