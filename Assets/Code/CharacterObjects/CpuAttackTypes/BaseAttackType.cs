using System.Collections.Generic;
using UnityEngine;

public abstract class AttackType : ScriptableObject
{
    public Sprite attackApperance;
    public int _AttackDamage;
    public float _AttackEndlag;
    public List<StatusEffect> _EffectsToApply;
    public abstract void Attack(CpuStateManager _context);
    public void DealDamage(CpuStateManager _context)
    {
        DamageSource _damageSource = new DamageSource(DamageSource.DamageType.StatusEffect);
        _damageSource.IsEnemy = _context._Stats._Enemy;

        _context._AttackingStats.TakeDamage(_AttackDamage, _damageSource);

        if (_EffectsToApply.Count == 0) return;
        for (int I = 0; I < _EffectsToApply.Count; I++)
        {
            _context._AttackingStats.AddStatusEffect(_EffectsToApply[I]);
        }
    }
}
