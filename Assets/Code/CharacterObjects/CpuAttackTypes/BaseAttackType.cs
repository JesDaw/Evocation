using System.Collections.Generic;
using UnityEngine;

public abstract class AttackType : ScriptableObject
{
    public float _StopDistance;
    public int _AttackDamage;
    public float _AttackEndlag;
    public List<StatusEffect> _EffectsToApply;
    //override init
    public virtual void Start(){}
    public abstract void Attack(CpuStateManager _context);
    public void DealDamage(CpuStateManager _context)
    {
        if(_context._AttackingStats == null || _context._Stats == null) return;

        DamageSource _damageSource = new DamageSource(DamageSource.DamageType.StatusEffect);
        _damageSource.IsEnemy = _context._Stats._Enemy;

        _context._AttackingStats.TakeDamage(_AttackDamage, _damageSource);

        if (_EffectsToApply.Count == 0) return;
        for (int I = 0; I < _EffectsToApply.Count; I++)
        {
            _context._AttackingStats.AddStatusEffect(_EffectsToApply[I]);
        }
    }
    public void DealDamage(Stats _context)
    {
        if(_context == null) return;

        DamageSource _damageSource = new DamageSource(DamageSource.DamageType.StatusEffect);

        _context.TakeDamage(_AttackDamage, _damageSource);

        if (_EffectsToApply.Count == 0) return;
        for (int I = 0; I < _EffectsToApply.Count; I++)
        {
            _context.AddStatusEffect(_EffectsToApply[I]);
        }
    }
}
