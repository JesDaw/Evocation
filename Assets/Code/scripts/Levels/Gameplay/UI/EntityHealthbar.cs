using UnityEngine;
using DG.Tweening;

public class EntityHealthbar : HealthBarBase
{
    [SerializeField] bool DebugLogs;
    Stats _Stats;

    public void Initialize(Stats statsComponent)
    {
        _Stats = statsComponent;
        UpdateHealth();
    }

    protected override void Start()
    {
        base.Start();   
    }

    public override void UpdateHealth()
    {
        if (_Stats == null || _HealthFillImage == null || _HealthTrailImage == null)
        {
            Debug.LogError($"{gameObject.name} heathbar is missing reference!");
            return;
        }

        float ratio = _Stats._MaxHealth > 0 ? (float)_Stats._CurrentHealth / _Stats._MaxHealth : 0f;
        ratio = Mathf.Clamp01(ratio);
        
        AnimateHealthChange(ratio);

        if (HealthBarObject != null)
        {
            HealthBarObject.SetActive(_Stats._CurrentHealth < _Stats._MaxHealth && _Stats._CurrentHealth > 0);
        }
        if(DebugLogs) Debug.Log($"{gameObject.name} health now at {_Stats._CurrentHealth}/{_Stats._MaxHealth}");
    }

    void OnDestroy()
    {
        if (_HealthFillImage != null)
            DOTween.Kill(_HealthFillImage);
        if (_HealthTrailImage != null)
            DOTween.Kill(_HealthTrailImage);
    }
}