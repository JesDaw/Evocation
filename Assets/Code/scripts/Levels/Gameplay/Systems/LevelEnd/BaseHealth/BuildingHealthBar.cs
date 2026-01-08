using UnityEngine;
using DG.Tweening;

/// <summary>
/// Health bar for buildings
/// Now includes null checking to prevent errors after destruction
/// </summary>
public class BuildingHealthBar : HealthBarBase
{
    [SerializeField] FloatVariable _health;
    float _MaxHealth = 100f;

    protected override void Start()
    {
        base.Start();
        
        if (_health != null)
        {
            _MaxHealth = _health._Value;
        }
    }

    public override void UpdateHealth()
    {
        // Null check to prevent errors after destruction
        if (_health == null || _HealthFillImage == null || _HealthTrailImage == null)
            return;

        float ratio = _MaxHealth > 0 ? _health._Value / _MaxHealth : 0f;
        ratio = Mathf.Clamp01(ratio);
        
        AnimateHealthChange(ratio);

        // Show/hide health bar
        if (HealthBarObject != null)
        {
            HealthBarObject.SetActive(_health._Value < _MaxHealth && _health._Value > 0);
        }
    }

    void OnDestroy()
    {
        // Kill any ongoing tweens when destroyed
        if (_HealthFillImage != null)
            DOTween.Kill(_HealthFillImage);
        if (_HealthTrailImage != null)
            DOTween.Kill(_HealthTrailImage);
    }
}