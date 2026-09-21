using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SceneActivity : MonoBehaviour
{
    [SerializeField] public bool disableDefaultBehavior;

    [Header("Keyboard Navigation")]
    [Tooltip("The button keyboard navigation should start on when this screen becomes active. " +
             "Leave empty if this screen has no keyboard-navigable buttons.")]
    [SerializeField] private Selectable defaultSelectedButton;

    public UnityEvent OnActivityStart;
    public UnityEvent OnActivityStop;

    public void StartActivity()
    {
        if (!disableDefaultBehavior)
        {
            gameObject.SetActive(true);
        }

        if (defaultSelectedButton != null)
        {
            var uiButton = defaultSelectedButton.GetComponent<UIButtons>();
            if (uiButton != null && UINavigationManager.Instance != null)
                UINavigationManager.Instance.RegisterScreenDefault(uiButton);
        }
        if (defaultSelectedButton != null) UINavigationManager.Instance.lastHighlightedButton = defaultSelectedButton.gameObject.GetComponent<UIButtons>();
        OnActivityStart.Invoke();
    }

    public void StopActivity()
    {
        if (!disableDefaultBehavior)
        {
            gameObject.SetActive(false);
        }
        OnActivityStop.Invoke();
    }
}