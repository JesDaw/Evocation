using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// manages the starting and stopping of individual UI screens
/// </summary>
public class SceneActivity : MonoBehaviour
{
    [Header("Keyboard Navigation")]
    [Tooltip("The button keyboard navigation should start on when this screen becomes active. " +
             "Leave empty if this screen has no keyboard-navigable buttons.")]
    [SerializeField] Selectable defaultSelectedButton;

    [Header("Transition")]
    [Tooltip("Optional. If assigned, this screen fades this CanvasGroup in/out on enter/exit. " +
             "Leave empty for an instant, non-animated transition.")]
    [SerializeField] CanvasGroup transitionCanvasGroup;
    [SerializeField] float transitionDuration = 0.3f;
    [SerializeField] AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public UnityEvent OnActivityStart;
    public UnityEvent OnActivityStop;

    UIButtons defaultUiButton;
    Coroutine transitionRoutine;

    void Start()
    {
        CacheDefaultUiButton();
    }

    void CacheDefaultUiButton()
    {
        if (defaultSelectedButton == null)
        {
            return;
        }

        defaultUiButton = defaultSelectedButton.GetComponent<UIButtons>();
        if (defaultUiButton == null)
        {
            Debug.LogWarning($"[SceneActivity] '{defaultSelectedButton.name}' on '{gameObject.name}' " +
                              "has no UIButtons component, so it can't be registered as this screen's " +
                              "keyboard-nav default.", this);
        }
    }

    void RegisterDefaultButtonWithNavManager()
    {
        if (defaultUiButton == null) return;

        if (UINavigationManager.Instance == null)
        {
            Debug.LogWarning("[SceneActivity] UINavigationManager.Instance is null; " +
                              "can't register this screen's default button.", this);
            return;
        }
        UINavigationManager.Instance.RegisterScreenDefault(defaultUiButton);
    }

    public void StartActivity()
    {
        SetActivityActive(true);
        RegisterDefaultButtonWithNavManager();
        OnActivityStart.Invoke();
    }

    public void StopActivity()
    {
        
        UINavigationManager.Instance?.ClearHighlightedButtonIfBelongsTo(transform);
        SetActivityActive(false);
        OnActivityStop.Invoke();
    }

    void SetActivityActive(bool active)
    {
        gameObject.SetActive(active);
    }

    public void PlayEnterTransition(Action onComplete = null) => PlayTransition(0f, 1f, onComplete);

    public void PlayExitTransition(Action onComplete = null) => PlayTransition(1f, 0f, onComplete);

    void PlayTransition(float from, float to, Action onComplete)
    {
        if (transitionCanvasGroup == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionRoutine(from, to, onComplete));
    }

    IEnumerator TransitionRoutine(float from, float to, Action onComplete)
    {
        transitionCanvasGroup.alpha = from;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = transitionCurve.Evaluate(Mathf.Clamp01(elapsed / transitionDuration));
            transitionCanvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        transitionCanvasGroup.alpha = to;
        onComplete?.Invoke();
    }
}