using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.InputSystem;

public abstract class HealthBarBase : MonoBehaviour
{
    [Header("Shared References")]
    [SerializeField] protected GameObject HealthBarObject;
    [SerializeField] protected Image _HealthFillImage;
    [SerializeField] protected Image _HealthTrailImage;

    [Header("Animation Settings")]
    [SerializeField] protected float _tweenDuration = 0.25f;
    [SerializeField] protected float _trailDelay = 0.4f;
    protected DG.Tweening.Sequence _healthBarSequence;
    protected DG.Tweening.Sequence _filledSequence;

    protected virtual void Start()
    {
        _healthBarSequence = DOTween.Sequence();
        _filledSequence = DOTween.Sequence();
        // UILogic.PauseEvent.AddListener(HandlePauseState);
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
        _filledSequence = DOTween.Sequence()
            .Append(DOTween.To(() => _HealthFillImage != null ? _HealthFillImage.fillAmount : 0f,
                   value => { if (_HealthFillImage != null) _HealthFillImage.fillAmount = value; },
                   ratio, _tweenDuration)
               .SetEase(Ease.InOutSine)).SetUpdate(UpdateType.Normal, true);
        _filledSequence.timeScale = 1;

        _healthBarSequence = DOTween.Sequence()
               .AppendInterval(_trailDelay)
               .Append(DOTween.To(() => _HealthTrailImage != null ? _HealthTrailImage.fillAmount : 0f,
                                  v => { if (_HealthTrailImage != null) _HealthTrailImage.fillAmount = v; },
                                  ratio, _tweenDuration)
                             .SetEase(Ease.InOutSine)).SetUpdate(UpdateType.Normal, true);
        _healthBarSequence.timeScale = 1;
    }

    private void HandlePauseState()
    {
        if (UILogic.pauseState.HasFlag(UILogic.PauseState.MenuPaused))
        {
            _filledSequence.timeScale = 0;
            _healthBarSequence.timeScale = 0;
        }
        else
        {
            _filledSequence.timeScale = 1;
            _healthBarSequence.timeScale = 1;
        }
    }

    private void OnDestroy()
    {
        // UILogic.PauseEvent.RemoveListener(HandlePauseState);
    }

    public abstract void UpdateHealth(); // implemented differently per subclass
}
