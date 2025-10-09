using UnityEngine;

public class BuildingHealthBar : HealthBarBase
{
    [SerializeField] FloatVariable _health;
    [SerializeField] float _MaxHealth = 100f;

    protected override void Start()
    {
        base.Start();
        _MaxHealth = _health._Value;
    }

    public override void UpdateHealth()
    {
        float ratio = _MaxHealth > 0 ? _health._Value / _MaxHealth : 0f;
        AnimateHealthChange(ratio);

        if (HealthBarObject)
            HealthBarObject.SetActive(_health._Value != _MaxHealth);
    }
}
