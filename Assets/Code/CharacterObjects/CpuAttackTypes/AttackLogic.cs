using System.Collections.Generic;
using UnityEngine;

public static class AttackLogic
{
    public static void ExecuteAttack(CpuStateManager context) => PerformAttack(context.transform.position, context._Stats, context._Stats._Enemy, context);
    public static void ExecuteAttack(PlayerStateMachine context) => PerformAttack(context.transform.position, context.PlayerStats, !context.isFacingRight, context);
    private static void PerformAttack(Vector3 origin, Stats attackerStats, bool facingLeft, object contextObj)
    {
        Vector2 range = attackerStats._AttackRange;
        Vector2 center = CalculateAttackCenter(origin, facingLeft, range);
        
        AttackDetection.DrawDebugBox(center, range, attackerStats._Enemy ? Color.red : Color.blue, 1f);
        List<IDamageable> targets = AttackDetection.FindTargetsInBox(center, range, attackerStats.targetTags, attackerStats);

        if (targets.Count == 0) return;

        if (attackerStats._IsProjectile)
        {
            IDamageable primaryTarget = AttackDetection.FindClosestTarget(origin, targets);
            if (primaryTarget != null)
            {
                SetAttackingStatRef(contextObj, primaryTarget);
                SpawnProjectile(origin, primaryTarget, attackerStats);
            }
        }
        else
        {
            if (attackerStats._IsAOE)
            {
                int count = 0;
                foreach (var t in targets)
                {
                    if (count >= attackerStats._MaxAOETargets) break;
                    SetAttackingStatRef(contextObj, t);
                    ApplyDamage(attackerStats, t);
                    count++;
                }
            }
            else
            {
                IDamageable primaryTarget = AttackDetection.FindClosestTarget(origin, targets);
                if (primaryTarget != null)
                {
                    SetAttackingStatRef(contextObj, primaryTarget);
                    ApplyDamage(attackerStats, primaryTarget);
                }
            }
        }
    }

    private static void SpawnProjectile(Vector3 start, IDamageable target, Stats attackerStats)
    {
        if (attackerStats._ProjectilePrefab == null) return;
        GameObject projGO = Object.Instantiate(attackerStats._ProjectilePrefab, start, Quaternion.identity);
        if (projGO.TryGetComponent(out Projectile p))
        {
            p.InitializeProjectile(target.transform, attackerStats._ProjectileSpeed, attackerStats._ProjectileMaxHeight,
                attackerStats._TrajectoryCurve, attackerStats._AxisCorrectionCurve, attackerStats._SpeedCurve,
                (hitDamageable) => ApplyDamage(attackerStats, hitDamageable));
        }
    }

    private static void ApplyDamage(Stats attacker, IDamageable target)
    {
        DamageSource.DamageType type = attacker._IsProjectile ? DamageSource.DamageType.Ranged : DamageSource.DamageType.Melee;
        if (attacker._IsAOE) type = DamageSource.DamageType.AOE;

        //Debug.Log($"{attacker.gameObject.name} attacking {target.gameObject.name} with {attacker._AttackDamage} damage");

        target.TakeDamage(attacker._AttackDamage, new DamageSource(type) { IsEnemy = attacker._Enemy });

        if (target is Stats statsTarget && attacker._EffectsToApply != null)
        {
            foreach (var e in attacker._EffectsToApply) statsTarget.statusEffectManager.AddEffect(e);
        }
    }

    public static Vector2 CalculateAttackCenter(Vector2 pos, bool left, Vector2 range) => pos + new Vector2(left ? -range.x / 2f : range.x / 2f, 0f);

    private static void SetAttackingStatRef(object context, IDamageable target)
    {
        Stats statsTarget = target as Stats;
        if (context is CpuStateManager cpu) cpu._AttackingStats = statsTarget;
        else if (context is PlayerStateMachine player) player._AttackingStats = statsTarget;
    }
}
