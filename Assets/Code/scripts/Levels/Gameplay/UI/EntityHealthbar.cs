using UnityEngine;
using DG.Tweening;

public class EntityHealthbar : HealthBarBase
{
    [SerializeField] Stats _Stats;

    protected override void Start()
    {
        base.Start();
        
        if (_Stats == null)
        {
            Debug.LogError($"{gameObject.name} missing Stats reference!");
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
        if (_HealthFillImage != null)
            DOTween.Kill(_HealthFillImage);
        if (_HealthTrailImage != null)
            DOTween.Kill(_HealthTrailImage);
    }
}