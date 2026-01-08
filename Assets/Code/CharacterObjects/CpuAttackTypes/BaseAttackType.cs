using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for all attack types
/// </summary>
public abstract class AttackType : ScriptableObject
{
    [Header("Attack Settings")]
    public float _StopDistance; // How close to get before attacking
    public int _AttackDamage;
    public float _AttackEndlag;

    [Header("Status Effects")]
    public List<StatusEffect> _EffectsToApply = new List<StatusEffect>();

    [Header("Attack Shape")]
    public bool useBoxDetection = true; // Box vs Circle detection
    public Vector2 boxSize = new Vector2(2f, 2f); 
    public float circleRadius = 1.5f; 

    public virtual void Start() { }

    public abstract void Attack(CpuStateManager _context);
    public abstract void Attack(PlayerStateMachine _context);

    /// <summary>
    /// Deal damage and apply status effects to a target
    /// </summary>
    protected void DealDamage(Stats attacker, Stats target)
    {
        if (target == null || attacker == null) return;

        DamageSource damageSource = new DamageSource(GetDamageType());
        damageSource.IsEnemy = attacker._Enemy;

        target.damageHandler.TakeDamage(_AttackDamage, damageSource);

        foreach (StatusEffect effect in _EffectsToApply)
        {
            target.statusEffectManager.AddEffect(effect);
        }
    }

    /// <summary>
    /// CPU version - uses CPU's _AttackingStats
    /// </summary>
    public void DealDamage(CpuStateManager _context)
    {
        DealDamage(_context._Stats, _context._AttackingStats);
    }

    /// <summary>
    /// Player version - uses Player's _AttackingStats
    /// </summary>
    public void DealDamage(PlayerStateMachine _context)
    {
        DealDamage(_context.PlayerStats, _context._AttackingStats);
    }

    /// <summary>
    /// Override this to specify the damage type for each attack
    /// </summary>
    protected virtual DamageSource.DamageType GetDamageType()
    {
        return DamageSource.DamageType.Melee;
    }
}