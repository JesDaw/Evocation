using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CpuHealthBar : MonoBehaviour
{
    [SerializeField] Stats _Stats;
    // [SerializeField] Slider _Slider;
    [SerializeField] GameObject HealthBarObject;
    [SerializeField] internal Image _HealthFillImage;
    [SerializeField] internal Image _HealthTrailImage;
    [SerializeField] private float _tweenDuration = 0.25f;
    [SerializeField] private float _trailDelay = 0.4f;
    void Start()
    {
        SetHealth();
        HealthBarObject.SetActive(false);
    }
    public void SetHealth()
    {
        _HealthFillImage.fillAmount = 1f;
        _HealthTrailImage.fillAmount = 1f;
    }

    public void UpdateHealth()
    {
        HealthChangeAnimation();
        if (_Stats._CurrentHealth == _Stats._MaxHealth) HealthBarObject.SetActive(false);
        else HealthBarObject.SetActive(true);
    }

    void HealthChangeAnimation()
    {
        float ratio = _Stats._MaxHealth > 0 ? (float)_Stats._CurrentHealth / _Stats._MaxHealth : 0f;
            DOTween.To(() => _HealthFillImage.fillAmount,            // getter — reads current fill
                       value => _HealthFillImage.fillAmount = value, // setter — updates the fill
                       ratio, _tweenDuration)                        // target fill value, how long it takes to animate
                   .SetEase(Ease.InOutSine);                         

            // animate trailing bar after delay
            DOTween.Sequence()
                   .AppendInterval(_trailDelay)
                   .Append(DOTween.To(() => _HealthTrailImage.fillAmount,
                                      v => _HealthTrailImage.fillAmount = v,
                                      ratio, _tweenDuration)
                                 .SetEase(Ease.InOutSine));
    }
}
