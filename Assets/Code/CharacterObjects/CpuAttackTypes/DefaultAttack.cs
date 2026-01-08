using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MeleeAttack", menuName = "AttackType/Melee Attack")]
public class DefaultAttackType : AttackType
{
    protected override DamageSource.DamageType GetDamageType()
    {
        return DamageSource.DamageType.Melee;
    }

    public override void Attack(CpuStateManager _context)
    {
        Vector2 attackPosition = _context.transform.position;
        List<Stats> targets;

        if (useBoxDetection)
        {
            Vector2 attackCenter = CalculateAttackCenter(attackPosition, _context._Stats._Enemy);
            AttackDetection.DrawDebugBox(attackCenter, boxSize, Color.red, 1f);
            
            targets = AttackDetection.FindTargetsInBox(
                attackCenter,
                boxSize,
                _context._Stats.targetTags, // Use targetTags instead of _CpuPriority
                _context._Stats
            );
        }
        else
        {
            AttackDetection.DrawDebugCircle(attackPosition, circleRadius, Color.red);
            
            targets = AttackDetection.FindTargetsInCircle(
                attackPosition,
                circleRadius,
                _context._Stats.targetTags, // Use targetTags instead of _CpuPriority
                _context._Stats
            );
        }

        // Attack first target found (already sorted by priority)
        if (targets.Count > 0)
        {
            _context._AttackingStats = targets[0];
            DealDamage(_context);
        }
        else
        {
            Debug.Log("Melee attack found no targets");
        }
    }

    public override void Attack(PlayerStateMachine _context)
    {
        Vector2 attackPosition = _context.transform.position;
        List<Stats> targets;

        if (useBoxDetection)
        {
            Vector2 attackCenter = CalculateAttackCenter(attackPosition, !_context.isFacingRight);
            AttackDetection.DrawDebugBox(attackCenter, boxSize, Color.blue, 1f);
            
            targets = AttackDetection.FindTargetsInBox(
                attackCenter,
                boxSize,
                _context.PlayerStats.targetTags, // Use targetTags instead of _CpuPriority
                _context.PlayerStats
            );
        }
        else
        {
            AttackDetection.DrawDebugCircle(attackPosition, circleRadius, Color.blue);
            
            targets = AttackDetection.FindTargetsInCircle(
                attackPosition,
                circleRadius,
                _context.PlayerStats.targetTags, // Use targetTags instead of _CpuPriority
                _context.PlayerStats
            );
        }

        // Attack first target found (already sorted by priority)
        if (targets.Count > 0)
        {
            _context._AttackingStats = targets[0];
            DealDamage(_context);
        }
        else
        {
            Debug.Log("Melee attack found no targets");
        }
    }

    /// <summary>
    /// Calculate where the attack box should be centered based on facing direction
    /// </summary>
    private Vector2 CalculateAttackCenter(Vector2 position, bool facingLeft)
    {
        float offsetX = (boxSize.x / 2f) + _StopDistance;
        offsetX = facingLeft ? -offsetX : offsetX;
        
        return position + new Vector2(offsetX, boxSize.y / 2f);
    }
}