using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RangeAttack", menuName = "AttackType/Range Attack")]
public class RangeAttackType : AttackType
{
    public GameObject projObject;
    public AnimationCurve projectileCurve;
    public float speed = 10f;
    public float offset = 2f;

    protected override DamageSource.DamageType GetDamageType() => DamageSource.DamageType.Ranged;

    public override void Attack(CpuStateManager _context)
    {
        Vector2 range = _context._Stats._AttackRange;
        Vector2 attackCenter = CalculateAttackCenter(_context.transform.position, _context._Stats._Enemy, range);
        
        List<Stats> targets = AttackDetection.FindTargetsInBox(attackCenter, range, _context._Stats.targetTags, _context._Stats);
        Stats target = AttackDetection.FindClosestTarget(_context.transform.position, targets);
        
        if (target != null)
        {
            _context._AttackingStats = target;
            SpawnProjectile(_context.transform.position, target.transform, () => DealDamage(_context));
        }
    }

    public override void Attack(PlayerStateMachine _context)
    {
        Vector2 range = _context.PlayerStats._AttackRange;
        Vector2 attackCenter = CalculateAttackCenter(_context.transform.position, !_context.isFacingRight, range);
        
        List<Stats> targets = AttackDetection.FindTargetsInBox(attackCenter, range, _context.PlayerStats.targetTags, _context.PlayerStats);
        Stats target = AttackDetection.FindClosestTarget(_context.transform.position, targets);
        
        if (target != null)
        {
            _context._AttackingStats = target;
            SpawnProjectile(_context.transform.position, target.transform, () => DealDamage(_context));
        }
    }

    private void SpawnProjectile(Vector3 startPos, Transform target, System.Action onHit)
    {
        if (projObject == null) return;
        GameObject createdProj = Instantiate(projObject);
        if (createdProj.TryGetComponent(out Projectile p)) p.Launch(startPos, target, projectileCurve, speed, offset, onHit);
    }
}