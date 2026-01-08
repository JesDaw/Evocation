using System.Collections.Generic;
using UnityEngine;

public abstract class AttackType : ScriptableObject
{
    [Header("Attack Settings")]
    public float _StopDistance; 
    public int _AttackDamage;
    public float _AttackEndlag;

    [Header("Status Effects")]
    public List<StatusEffect> _EffectsToApply = new List<StatusEffect>();

    [Header("Attack Shape")]
    public bool useBoxDetection = true; 
    public Vector2 boxSize = new Vector2(2f, 2f); 
    public float circleRadius = 1.5f; 

    public abstract void Attack(CpuStateManager _context);
    public abstract void Attack(PlayerStateMachine _context);

    protected void DealDamage(Stats attacker, Stats target)
    {
        if (target == null || attacker == null) return;

        DamageSource damageSource = new DamageSource(GetDamageType());
        damageSource.IsEnemy = attacker._Enemy;

        target.damageHandler.TakeDamage(attacker._AttackDamage, damageSource);

        foreach (StatusEffect effect in _EffectsToApply)
        {
            target.statusEffectManager.AddEffect(effect);
        }
    }

    public void DealDamage(CpuStateManager _context) => DealDamage(_context._Stats, _context._AttackingStats);
    public void DealDamage(PlayerStateMachine _context) => DealDamage(_context.PlayerStats, _context._AttackingStats);

    protected virtual DamageSource.DamageType GetDamageType() => DamageSource.DamageType.Melee;

    // The Fixed Center Calculation for Slopes
    protected Vector2 CalculateAttackCenter(Vector2 position, bool facingLeft, Vector2 currentRange)
    {
        float offsetX = (currentRange.x / 2f);
        offsetX = facingLeft ? -offsetX : offsetX;
        // Y is 0 offset so the box extends UP and DOWN from the pivot
        return position + new Vector2(offsetX, 0f); 
    }
}