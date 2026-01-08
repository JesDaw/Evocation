using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RangeAttack", menuName = "AttackType/Range Attack")]
public class RangeAttackType : AttackType
{
    [Header("Projectile Settings")]
    public GameObject projObject;
    public Sprite attackAppearance;
    public AnimationCurve projectileCurve;
    public float speed = 10f;
    public float offset = 2f;

    protected override DamageSource.DamageType GetDamageType()
    {
        return DamageSource.DamageType.Ranged;
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
                _context._Stats.targetTags,
                _context._Stats
            );
        }
        else
        {
            AttackDetection.DrawDebugCircle(attackPosition, circleRadius, Color.red);
            
            targets = AttackDetection.FindTargetsInCircle(
                attackPosition,
                circleRadius,
                _context._Stats.targetTags,
                _context._Stats
            );
        }

        Stats target = AttackDetection.FindClosestTarget(attackPosition, targets);
        
        if (target != null)
        {
            _context._AttackingStats = target;
            SpawnProjectile(_context.transform.position, target.transform, () => DealDamage(_context));
        }
        else
        {
            Debug.Log("Range attack found no targets");
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
                _context.PlayerStats.targetTags,
                _context.PlayerStats
            );
        }
        else
        {
            AttackDetection.DrawDebugCircle(attackPosition, circleRadius, Color.blue);
            
            targets = AttackDetection.FindTargetsInCircle(
                attackPosition,
                circleRadius,
                _context.PlayerStats.targetTags,
                _context.PlayerStats
            );
        }

        Stats target = AttackDetection.FindClosestTarget(attackPosition, targets);
        
        if (target != null)
        {
            _context._AttackingStats = target;
            SpawnProjectile(_context.transform.position, target.transform, () => DealDamage(_context));
        }
        else
        {
            Debug.Log("Range attack found no targets");
        }
    }

    /// <summary>
    /// Spawn a projectile towards a target
    /// </summary>
    private void SpawnProjectile(Vector3 startPos, Transform target, System.Action onHit)
    {
        if (projObject == null)
        {
            Debug.LogWarning("No projectile object assigned!");
            return;
        }

        GameObject createdProj = Instantiate(projObject);
        Projectile projectile = createdProj.GetComponent<Projectile>();
        
        if (projectile != null)
        {
            projectile.Launch(startPos, target, projectileCurve, speed, offset, onHit);
        }
        else
        {
            Debug.LogWarning("Projectile object missing Projectile component!");
            Destroy(createdProj);
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