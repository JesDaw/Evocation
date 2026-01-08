using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AOEAttack", menuName = "AttackType/AOE Attack")]
public class AOEAttackType : AttackType
{
    public int maxTargets = 10;

    protected override DamageSource.DamageType GetDamageType() => DamageSource.DamageType.AOE;

    public override void Attack(CpuStateManager _context)
    {
        Vector2 range = _context._Stats._AttackRange;
        Vector2 attackCenter = CalculateAttackCenter(_context.transform.position, _context._Stats._Enemy, range);
        
        List<Stats> targets = AttackDetection.FindTargetsInBox(attackCenter, range, _context._Stats.targetTags, _context._Stats);

        for (int i = 0; i < Mathf.Min(targets.Count, maxTargets); i++)
        {
            _context._AttackingStats = targets[i];
            DealDamage(_context);
        }
    }

    public override void Attack(PlayerStateMachine _context)
    {
        Vector2 range = _context.PlayerStats._AttackRange;
        Vector2 attackCenter = CalculateAttackCenter(_context.transform.position, !_context.isFacingRight, range);
        
        List<Stats> targets = AttackDetection.FindTargetsInBox(attackCenter, range, _context.PlayerStats.targetTags, _context.PlayerStats);

        for (int i = 0; i < Mathf.Min(targets.Count, maxTargets); i++)
        {
            _context._AttackingStats = targets[i];
            DealDamage(_context);
        }
    }
}