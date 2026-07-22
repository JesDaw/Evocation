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
    public void ResetToOriginalState()
    {
        isHovered = false;
        AnimateToState(originalColor, originalTextColor, originalScale, animationDuration);
    }

    private void OnDisable()
    {
        isHovered = false;
    }

    private void OnCanvasGroupChanged()
    {
        if (!gameObject.activeInHierarchy)
            isHovered = false;
    }

    // OnSelect and OnPointerEnter are like the same function they should just be merged into one OnbuttonHighlight function
    public void OnSelect(BaseEventData eventData)
    {
        UINavigationManager.Instance?.RegisterKeyboardSelect(this);
        ActivateHighlightEffects();
        ButtonHighlighted?.Invoke();
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        UINavigationManager.Instance?.RegisterMouseHover(this);
        if (isHovered) return;
        isHovered = true;        
        
        ButtonHighlighted?.Invoke();
    }

    void ActivateHighlightEffects()
    {
        AnimateToState(hoverColor, hoverTextColor, hoverScale, animationDuration);
        FModAudioManager.instance.PlaySoundByName("showCharacterInfo");
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isHovered) return;
        isHovered = false;
    }
     public void OnDeselect(BaseEventData eventData)
    {
        if (!isHovered) DeactivateHighlightEffects();
    }

    void DeactivateHighlightEffects()
    {
        AnimateToState(originalColor, originalTextColor, originalScale, animationDuration);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        StartCoroutine(ClickAnimation());
    }

    private IEnumerator ClickAnimation()
    {
        AnimateToState(clickColor, clickTextColor, clickScale, clickDuration); 
        yield return new WaitForSeconds(clickDuration);
        AnimateToState(postClickColor, postClickTextColor, postClickScale, animationDuration); // usually what happens is the sceneactifity will switch scenes before this animation is done so it can be weird, or works with a scnee transition animation I think
        ButtonClicked?.Invoke();
    }
    
    private void AnimateToState(Color targetColor, Color targetTextColor, Vector3 targetScale, float duration)
    {
        if (currentAnimation != null) StopCoroutine(currentAnimation);
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

    public void HighlightSound() //these should be public variables set in the inspector I dont think they should be super hard coded like this
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

    public void QuitGame() //I want to make a general game logic script with this logic in it quitting the game is not what this script is for its purly for button visuals
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}