using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public abstract class HealthBarBase : MonoBehaviour
{
    [Header("Shared References")]
    [SerializeField] protected GameObject HealthBarObject;
    [SerializeField] protected Image _HealthFillImage;
    [SerializeField] protected Image _HealthTrailImage;

    [Header("Animation Settings")]
    [SerializeField] protected float _tweenDuration = 0.25f;
    [SerializeField] protected float _trailDelay = 0.4f;

    protected virtual void Start()
    {
        SetStartHealth();
        if (HealthBarObject != null)
            HealthBarObject.SetActive(false);
    }

    protected void SetStartHealth()
    {
        if (_HealthFillImage) _HealthFillImage.fillAmount = 1f;
        if (_HealthTrailImage) _HealthTrailImage.fillAmount = 1f;
    }

    protected void AnimateHealthChange(float ratio)
    {
        DOTween.To(() => _HealthFillImage.fillAmount,
                   value => _HealthFillImage.fillAmount = value,
                   ratio, _tweenDuration)
               .SetEase(Ease.InOutSine);

        DOTween.Sequence()
               .AppendInterval(_trailDelay)
               .Append(DOTween.To(() => _HealthTrailImage.fillAmount,
                                  v => _HealthTrailImage.fillAmount = v,
                                  ratio, _tweenDuration)
                             .SetEase(Ease.InOutSine));
    }

    public abstract void UpdateHealth(); // implemented differently per subclass
}
