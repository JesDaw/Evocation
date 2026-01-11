using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class VisualEffectsManager : MonoBehaviour
{
    public static VisualEffectsManager Instance { get; private set; }

    [Header("Effect Prefabs")]
    [SerializeField] GameObject[] particleEffectPrefabs;
    [SerializeField] Material[] shaderEffects;
    
    Dictionary<string, GameObject> activeParticleEffects = new Dictionary<string, GameObject>();
    List<Coroutine> activeCoroutines = new List<Coroutine>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #region Particle Effects
    
    public GameObject SpawnParticleEffect(GameObject prefab, Vector3 position, UnityEngine.Quaternion rotation, Transform parent = null)
    {
        GameObject effect = Instantiate(prefab, position, rotation, parent);
        return effect;
    }

    public GameObject SpawnParticleEffectOnObject(GameObject prefab, Transform target)
    {
        return SpawnParticleEffect(prefab, target.position, target.rotation, target);
    }

    #endregion

    #region Post Processing Effects
    
    [Header("Post Processing")]
    [SerializeField] private Volume globalVolume;

    public void ApplyPostProcessingEffect(Volume volumeOverride, float blendDuration = 0f)
    {

        if (globalVolume != null && volumeOverride != null)
        {
            DOTween.To(() => globalVolume.weight, x => globalVolume.weight = x, 1f, blendDuration);
        }
    }

    #endregion

    #region Shader Effects
    
    private List<Material> activeShaderInstances = new List<Material>();

    public void CallShockwave(Renderer targetRenderer, float duration = 1.0f)
    {
        // Ensure this matches the "Reference" name in your Shader Graph!
        TweenShaderFloat(targetRenderer, "_RippleDistanceFromCenter", 1.0f, duration, -0.1f)
            .SetEase(Ease.OutQuad); 
    }

    public Tween TweenShaderFloat(Renderer targetRenderer, string propertyName, float endValue, float duration, float startValue = 0f)
    {
        if (targetRenderer == null) return null;
        
        // .material automatically creates a local instance for this object
        Material mat = targetRenderer.material; 
        
        // Only add to the list if we haven't tracked this specific instance yet
        if (!activeShaderInstances.Contains(mat)) 
        {
            activeShaderInstances.Add(mat);
        }

        mat.SetFloat(propertyName, startValue);
        return mat.DOFloat(endValue, propertyName, duration);
    }

    

    public void ApplyShaderEffect(Renderer targetRenderer, Material shaderMaterial)
    {
        if (targetRenderer != null && shaderMaterial != null)
        {
            Material instanceMaterial = new Material(shaderMaterial);
            targetRenderer.material = instanceMaterial;
            activeShaderInstances.Add(instanceMaterial);
        }
    }

    #endregion

    #region Animation Effects (DOTween)

    public Tween FadeAlpha(GameObject target, float targetAlpha, float duration, System.Action onComplete = null)
{
    Tween fadeTween = null;

    if (target.TryGetComponent<CanvasGroup>(out var canvasGroup))
    {
        fadeTween = canvasGroup.DOFade(targetAlpha, duration);
    }
    else if (target.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
    {
        fadeTween = spriteRenderer.DOFade(targetAlpha, duration);
    }
    else if (target.TryGetComponent<Image>(out var image))
    {
        fadeTween = image.DOFade(targetAlpha, duration);
    }
    else
    {
        Debug.LogWarning($"FadeAlpha: No compatible component found on {target.name}");
        return null;
    }

    if (onComplete != null)
    {
        fadeTween.OnComplete(() => onComplete());
    }

    return fadeTween;
}

    public Tween FadeIn(GameObject target, float duration, System.Action onComplete = null)
    {
        return FadeAlpha(target, 1f, duration, onComplete);
    }

    public Tween FadeOut(GameObject target, float duration, System.Action onComplete = null)
    {
        return FadeAlpha(target, 0f, duration, onComplete);
    }

    public Tween FadeFromTo(GameObject target, float fromAlpha, float toAlpha, float duration, System.Action onComplete = null)
    {
        SetAlpha(target, fromAlpha);
        return FadeAlpha(target, toAlpha, duration, onComplete);
    }


    public void SetAlpha(GameObject target, float alpha)
    {
        SpriteRenderer spriteRenderer = target.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = alpha;
            spriteRenderer.color = c;
            return;
        }

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            return;
        }

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            Color c = renderer.material.color;
            c.a = alpha;
            renderer.material.color = c;
            return;
        }

        Debug.LogWarning($"SetAlpha: No compatible component found on {target.name}");
    }

    #endregion

    #region Lighting Effects

    #endregion

    #region Texture Effects

    #endregion

    #region Mesh Effects

    #endregion

    #region Cleanup
    public void ClearAllEffects()
    {
        foreach (var coroutine in activeCoroutines)
        {
            if (coroutine != null) StopCoroutine(coroutine);
        }
        activeCoroutines.Clear();

        foreach (var effect in activeParticleEffects.Values)
        {
            if (effect != null) Destroy(effect);
        }
        activeParticleEffects.Clear();

        foreach (var mat in activeShaderInstances)
        {
            if (mat != null) Destroy(mat);
        }
        activeShaderInstances.Clear();

        DOTween.Kill(this);
    }

    private void OnDestroy()
    {
        ClearAllEffects();
    }

    #endregion
}