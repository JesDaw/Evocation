using UnityEngine;
using DG.Tweening;

/// <summary>
/// Health bar for CPU characters
/// </summary>
public class CpuHealthBar : HealthBarBase
{
    [SerializeField] private Stats _Stats;

    protected override void Start()
    {
        base.Start();
        
        if (_Stats == null)
        {
            Debug.LogError("CpuHealthBar missing Stats reference!");
        }
    }

    public override void UpdateHealth()
    {
        if (_Stats == null || _HealthFillImage == null || _HealthTrailImage == null)
            return;

        float ratio = _Stats._MaxHealth > 0 ? (float)_Stats._CurrentHealth / _Stats._MaxHealth : 0f;
        ratio = Mathf.Clamp01(ratio);
        
        AnimateHealthChange(ratio);

        if (HealthBarObject != null)
        {
            HealthBarObject.SetActive(_Stats._CurrentHealth < _Stats._MaxHealth && _Stats._CurrentHealth > 0);
        }
    }

    void OnDestroy()
    {
        DOTween.Kill(_HealthFillImage);
        DOTween.Kill(_HealthTrailImage);
    }
}