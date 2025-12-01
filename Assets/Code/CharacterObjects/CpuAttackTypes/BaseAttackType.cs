using System.Collections.Generic;
using UnityEngine;

public abstract class AttackType : ScriptableObject
{
    public float _StopDistance; // How close to get before attacking
    public float _AttackRange;  // How far the attack reaches (for AOE, projectiles, etc.)
    public int _AttackDamage;
    public float _AttackEndlag; 
    public List<StatusEffect> _EffectsToApply;
    
    public virtual void Start(){}
    
    public abstract void Attack(CpuStateManager _context);
    public abstract void Attack(PlayerStateMachine _context);
    protected void DealDamage(Stats attacker, Stats target)
    {
        if (target == null || attacker == null) return;

        DamageSource _damageSource = new DamageSource(DamageSource.DamageType.StatusEffect);
        _damageSource.IsEnemy = attacker._Enemy;

        target.TakeDamage(_AttackDamage, _damageSource);

        if (_EffectsToApply.Count == 0) return;
        for (int I = 0; I < _EffectsToApply.Count; I++)
        {
            target.AddStatusEffect(_EffectsToApply[I]);
        }
    }
    
    // CPU version - uses CPU's _AttackingStats
    public void DealDamage(CpuStateManager _context)
    {
        DealDamage(_context._Stats, _context._AttackingStats);
    }
    
    // Player version - uses Player's _AttackingStats
    public void DealDamage(PlayerStateMachine _context)
    {
        DealDamage(_context.PlayerStats, _context._AttackingStats);
    }
}