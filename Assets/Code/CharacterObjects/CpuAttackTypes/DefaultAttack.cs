using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MeleeAttack", menuName = "AttackType/Melee Attack")]
public class DefaultAttackType : AttackType
{
    public override void Attack(CpuStateManager _context)
    {
        Vector2 range = _context._Stats._AttackRange;
        Vector2 attackCenter = CalculateAttackCenter(_context.transform.position, _context._Stats._Enemy, range);

        AttackDetection.DrawDebugBox(attackCenter, range, Color.red, 1f);

        List<IDamageable> targets = AttackDetection.FindTargetsInBox(attackCenter, range, _context._Stats.targetTags, _context._Stats);

        if (targets.Count > 0)
        {
            _context._AttackingStats = targets[0] as Stats;
            DealDamage(_context);
        }
    }

    public override void Attack(PlayerStateMachine _context)
    {
        Vector2 range = _context.PlayerStats._AttackRange;
        Vector2 attackCenter = CalculateAttackCenter(_context.transform.position, !_context.isFacingRight, range);

        AttackDetection.DrawDebugBox(attackCenter, range, Color.blue, 1f);

        List<IDamageable> targets = AttackDetection.FindTargetsInBox(attackCenter, range, _context.PlayerStats.targetTags, _context.PlayerStats);

        if (targets.Count > 0)
        {
            _context._AttackingStats = targets[0] as Stats;
            DealDamage(_context);
        }
    }
}