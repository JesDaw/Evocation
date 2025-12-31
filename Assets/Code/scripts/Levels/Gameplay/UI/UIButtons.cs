using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class UIButtons : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
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

    private Coroutine currentAnimation;
    private bool isHovered = false;

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

    public void ResetToOriginalState()
    {
        isHovered = false;
        AnimateToState(originalColor, originalTextColor, originalScale, animationDuration);
    }

    private void OnCanvasGroupChanged()
    {
        if (!gameObject.activeInHierarchy)
        {
            isHovered = false;
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isHovered) return;
        isHovered = true;
        AnimateToState(hoverColor, hoverTextColor, hoverScale, animationDuration);
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isHovered) return;
        isHovered = false;
        AnimateToState(originalColor, originalTextColor, originalScale, animationDuration);
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
       // StartCoroutine(ClickAnimation());
    }

    private IEnumerator ClickAnimation()
    {
        AnimateToState(clickColor, clickTextColor, clickScale, clickDuration);
        yield return new WaitForSeconds(clickDuration);

        AnimateToState(postClickColor, postClickTextColor, postClickScale, animationDuration);
    }
    
    private void AnimateToState(Color targetColor, Color targetTextColor, Vector3 targetScale, float duration)
    {
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);
            
        currentAnimation = StartCoroutine(AnimateToStateCoroutine(targetColor, targetTextColor, targetScale, duration));
    }

    private IEnumerator AnimateToStateCoroutine(Color targetColor, Color targetTextColor, Vector3 targetScale, float duration)
    {
        Color startColor = targetImage != null ? targetImage.color : Color.white;
        Color startTextColor = targetText != null ? targetText.color : Color.white;
        Vector3 startScale = targetTransform.localScale;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = animationCurve.Evaluate(elapsed / duration);

            if (targetImage != null)
                targetImage.color = Color.Lerp(startColor, targetColor, t);

            if (targetText != null)
                targetText.color = Color.Lerp(startTextColor, targetTextColor, t);

            targetTransform.localScale = Vector3.Lerp(startScale, targetScale, t);

            yield return null;
        }

        if (targetImage != null)
            targetImage.color = targetColor;

        if (targetText != null)
            targetText.color = targetTextColor;

        targetTransform.localScale = targetScale;
    }
}
