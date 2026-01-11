using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RangeAttack", menuName = "AttackType/Range Attack")]
public class RangeAttackType : AttackType
{
    public GameObject projObject;
    
    [Header("Video Trajectory Settings")]
    public float projectileMaxRelativeHeight = 2f;
    public AnimationCurve heightCurve;
    public AnimationCurve axisCorrectionCurve;
    public AnimationCurve speedCurve;

    protected override DamageSource.DamageType GetDamageType() => DamageSource.DamageType.Ranged;

    public override void Attack(CpuStateManager _context)
    {
        ExecuteRangeAttack(_context.transform.position, _context._Stats, _context._Stats._Enemy, (target) => {
             _context._AttackingStats = target;
             DealDamage(_context);
        });
    }

    public override void Attack(PlayerStateMachine _context)
    {
        ExecuteRangeAttack(_context.transform.position, _context.PlayerStats, !_context.isFacingRight, (target) => {
             _context._AttackingStats = target;
             DealDamage(_context);
        });
    }

    void ExecuteRangeAttack(Vector3 origin, Stats attackerStats, bool facingLeft, System.Action<Stats> onHitCallback)
    {
        Vector2 range = attackerStats._AttackRange;
        Vector2 center = CalculateAttackCenter(origin, facingLeft, range);
        
        List<Stats> targets = AttackDetection.FindTargetsInBox(center, range, attackerStats.targetTags, attackerStats);
        Stats target = AttackDetection.FindClosestTarget(origin, targets);
        
        if (target != null)
        {
            float moveSpeed = attackerStats._ProjectileSpeed > 0 ? attackerStats._ProjectileSpeed : 10f;
            SpawnProjectile(origin, target.transform, moveSpeed, onHitCallback);
        }
    }

    void SpawnProjectile(Vector3 startPos, Transform target, float moveSpeed, System.Action<Stats> onHit)
    {
        if (projObject == null) return;
        GameObject go = Instantiate(projObject, startPos, Quaternion.identity);
        if (go.TryGetComponent(out Projectile p))
        {
            p.InitializeProjectile(
                target, 
                moveSpeed, 
                projectileMaxRelativeHeight,
                heightCurve,
                axisCorrectionCurve, 
                speedCurve, 
                onHit
            );
        }
    }
}