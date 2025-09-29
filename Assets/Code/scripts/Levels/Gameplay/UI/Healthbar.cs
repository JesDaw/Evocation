using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.InputSystem;

public class Healthbar : MonoBehaviour
{
    [SerializeField]
    private Image _healthBarFillImage;

    [SerializeField]
    private Image _healthBarTrailingFillImage;

    [SerializeField]
    private float _trailDelay = 0.4f;

    [SerializeField]
    private float _maxHealth = 100f;
    
    private float _currentHealth;

    private void Awake()
    {
        _currentHealth = _maxHealth;

        _healthBarFillImage.fillAmount = 1f;
        _healthBarTrailingFillImage.fillAmount = 1f;
    }

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            DrainHealthBar();
        }
    }

    private void DrainHealthBar()
    {
        _currentHealth -= 10f;
        float ratio = _currentHealth / _maxHealth;

        Sequence sequence = DOTween.Sequence();
        sequence.Append(_healthBarFillImage.DOFillAmount(ratio, 0.25f))
                .SetEase(Ease.InOutSine);
        sequence.AppendInterval(_trailDelay);
        sequence.Append(_healthBarTrailingFillImage.DOFillAmount(ratio, 0.3f))
                .SetEase(Ease.InOutSine);

        sequence.Play();
    }
}