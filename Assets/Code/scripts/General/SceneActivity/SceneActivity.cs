using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SceneActivity : MonoBehaviour
{
    [Header("Keyboard Navigation")]
    [Tooltip("The button keyboard navigation should start on when this screen becomes active. " +
             "Leave empty if this screen has no keyboard-navigable buttons.")]
    [SerializeField] Selectable defaultSelectedButton;

    public UnityEvent OnActivityStart;
    public UnityEvent OnActivityStop;

    public void StartActivity()
    {
        SetActivityActive(true);
        // animation logic should be here like fand ins and outs or other stuff
        SetHighlightedButton();
        OnActivityStart.Invoke();
    }



    void SetHighlightedButton()
    {
        if (defaultSelectedButton != null)
        {
            var uiButton = defaultSelectedButton.GetComponent<UIButtons>();
            if (uiButton != null && UINavigationManager.Instance != null) UINavigationManager.Instance.RegisterScreenDefault(uiButton);
            UINavigationManager.Instance.lastHighlightedButton = defaultSelectedButton.gameObject.GetComponent<UIButtons>();
        }
        else
        {
            Debug.Log($"no defaultSelectedButton set on {gameObject.name}");
        }
    }

    public void StopActivity()
    {
        SetActivityActive(false);
        // animation logic
        // clear highlighted button?
        OnActivityStop.Invoke();
    }

    void SetActivityActive(bool active)
    {
        gameObject.SetActive(active);
    }
}