using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Handles per-button visual animation and audio for hover, select, click states.
///
/// INSPECTOR NOTE: Set the Unity Button component's Transition to "None" on every
/// GameObject that uses this script. The built-in Color Tint transition fights with
/// the colour animations here and causes flickering / incorrect colours.
/// </summary>
public class UIButtons : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler
{
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Hover Variables")]
    [SerializeField] private Color hoverColor = Color.white;
    [SerializeField] private Color hoverTextColor = Color.white;
    [SerializeField] private Vector3 hoverScale = Vector3.one * 1.05f;
    
    [Header("Click Variables")]
    [SerializeField] private Color clickColor = Color.white;
    [SerializeField] private Color clickTextColor = Color.white;
    [SerializeField] private Vector3 clickScale = Vector3.one * 0.9f;
    [SerializeField] private float clickDuration = 0.1f;
    [SerializeField] private Color postClickColor = Color.white;
    [SerializeField] private Color postClickTextColor = Color.white;
    [SerializeField] private Vector3 postClickScale = Vector3.one * 1.05f;
    
    [Header("Button Components")]
    [SerializeField] private Image targetImage;
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Color originalColor = Color.white;
    [SerializeField] private Color originalTextColor = Color.white;
    [SerializeField] private Vector3 originalScale = Vector3.one;
    [Header("Events")]
    public UltEvents.UltEvent ButtonHighlighted;
    public UltEvents.UltEvent ButtonClicked;
    private Coroutine currentAnimation;
    private bool isHovered = false;
    // ── Unity ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (targetImage != null)
            originalColor = targetImage.color;
        
        if (targetText != null)
            originalTextColor = targetText.color;
        
        if (targetTransform != null)
            originalScale = targetTransform.localScale;
    }

    private void OnEnable()
    {
        ResetToOriginalState();
    }

    private void OnDisable()
    {
        isHovered = false;
    }

    // ── State helpers ─────────────────────────────────────────────────────────

    public void ResetToOriginalState()
    {
        isHovered = false;
        AnimateToState(originalColor, originalTextColor, originalScale, animationDuration);
    }

    private void OnCanvasGroupChanged()
    {
        if (!gameObject.activeInHierarchy)
            isHovered = false;
    }

    // ── Pointer events (mouse) ────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Tell the navigation manager which button the mouse is over so keyboard
        // mode can resume from here if the player picks up the controller/keyboard.
        UINavigationManager.Instance?.RegisterMouseHover(this);
        if (isHovered) return;
        isHovered = true;


        AnimateToState(hoverColor, hoverTextColor, hoverScale, animationDuration);
        FModAudioManager.instance.PlaySoundByName("showCharacterInfo");

        ButtonHighlighted?.Invoke();
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isHovered) return;
        isHovered = false;
        AnimateToState(originalColor, originalTextColor, originalScale, animationDuration);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ClickSound();
        StartCoroutine(ClickAnimation());
        
    }

    // ── Select events (keyboard / gamepad) ────────────────────────────────────

    public void OnSelect(BaseEventData eventData)
    {
        // Tell the manager this is the last keyboard-reached button so it can
        // resume here if the player briefly switches to mouse without hovering anything.
        UINavigationManager.Instance?.RegisterKeyboardSelect(this);
        FModAudioManager.instance.PlaySoundByName("showCharacterInfo");
        ButtonHighlighted?.Invoke();

        AnimateToState(hoverColor, hoverTextColor, hoverScale, animationDuration);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        // Only animate back to original when the mouse isn't still over this button.
        if (!isHovered)
            AnimateToState(originalColor, originalTextColor, originalScale, animationDuration);
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    private IEnumerator ClickAnimation()
    {
        AnimateToState(clickColor, clickTextColor, clickScale, clickDuration);
        yield return new WaitForSeconds(clickDuration);
        AnimateToState(postClickColor, postClickTextColor, postClickScale, animationDuration);
        ButtonClicked?.Invoke();
    }
    
    private void AnimateToState(Color targetColor, Color targetTextColor, Vector3 targetScale, float duration)
    {
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        currentAnimation = StartCoroutine(AnimateToStateCoroutine(targetColor, targetTextColor, targetScale, duration));
    }

    private IEnumerator AnimateToStateCoroutine(Color targetColor, Color targetTextColor, Vector3 targetScale, float duration)
    {
        Color startColor     = targetImage     != null ? targetImage.color  : Color.white;
        Color startTextColor = targetText      != null ? targetText.color   : Color.white;
        Vector3 startScale   = targetTransform != null ? targetTransform.localScale : Vector3.one;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;;
            float t = animationCurve.Evaluate(Mathf.Clamp01(elapsed / duration));

            if (targetImage != null)
                targetImage.color = Color.Lerp(startColor, targetColor, t);

            if (targetText != null)
                targetText.color = Color.Lerp(startTextColor, targetTextColor, t);

            if (targetTransform != null)
                targetTransform.localScale = Vector3.Lerp(startScale, targetScale, t);

            yield return null;
        }

        // Snap to final values
        if (targetImage != null)     targetImage.color          = targetColor;
        if (targetText != null)      targetText.color           = targetTextColor;
        if (targetTransform != null) targetTransform.localScale = targetScale;

    }

    // ── Audio / misc public methods ───────────────────────────────────────────
    public void HighlightSound()
    {
        FModAudioManager.instance.PlaySoundByName("showCharacterInfo");
    }

    public void ClickSound()
    {
        FModAudioManager.instance.PlaySoundByName("addCharacterToParty");
    }

    public void BackSound()
    {
        FModAudioManager.instance.PlaySoundByName("removeCharacterFromParty");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}