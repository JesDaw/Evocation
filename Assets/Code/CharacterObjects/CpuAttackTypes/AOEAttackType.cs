using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AOEAttack", menuName = "AttackType/AOE Attack")]
public class AOEAttackType : AttackType
{
    [Header("AOE-Specific Settings")]
    public int maxTargets = 10; // Maximum number of targets to hit

    protected override DamageSource.DamageType GetDamageType()
    {
        return DamageSource.DamageType.AOE;
    }

    public override void Attack(CpuStateManager _context)
    {
        Vector2 attackCenter = CalculateAttackCenter(_context.transform.position, _context._Stats._Enemy);
        
        // Visual debugging
        AttackDetection.DrawDebugBox(attackCenter, boxSize, Color.red, 1f);

        List<Stats> targets = AttackDetection.FindTargetsInBox(
            attackCenter,
            boxSize,
            _context._Stats.targetTags, 
            _context._Stats
        );

        int hitCount = 0;
        foreach (Stats target in targets)
        {
            if (hitCount >= maxTargets) break;

            _context._AttackingStats = target;
            DealDamage(_context);
            hitCount++;
        }

        if (hitCount == 0)
        {
            Debug.Log($"AOE attack found no targets at {attackCenter}");
        }
    }

    public override void Attack(PlayerStateMachine _context)
    {
        Vector2 attackCenter = CalculateAttackCenter(_context.transform.position, !_context.isFacingRight);
        
        AttackDetection.DrawDebugBox(attackCenter, boxSize, Color.blue, 1f);

        List<Stats> targets = AttackDetection.FindTargetsInBox(
            attackCenter,
            boxSize,
            _context.PlayerStats.targetTags, 
            _context.PlayerStats
        );

        int hitCount = 0;
        foreach (Stats target in targets)
        {
            if (hitCount >= maxTargets) break;

            _context._AttackingStats = target;
            DealDamage(_context);
            hitCount++;
        }

        if (hitCount == 0)
        {
            Debug.Log($"AOE attack found no targets at {attackCenter}");
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