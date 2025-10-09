using UnityEngine;
using DG.Tweening;

public class CpuHealthBar : HealthBarBase
{
    [SerializeField] private Stats _Stats;

    protected override void Start()
    {
        base.Start();
    }

    public override void UpdateHealth()
    {
        float ratio = _Stats._MaxHealth > 0 ? (float)_Stats._CurrentHealth / _Stats._MaxHealth : 0f;
        AnimateHealthChange(ratio);

        if (HealthBarObject)
            HealthBarObject.SetActive(_Stats._CurrentHealth != _Stats._MaxHealth);
    }
}
