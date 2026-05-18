using System.Collections.Generic;
using UnityEngine;

public static class CombatLogic
{
    public static void ExecuteAction(Stats attacker, CombatAction action, Stats primaryTarget)
    {
        float healthChange = attacker._AttackDamage * action.healthChangePercent;
        float knockbackChange = attacker._KnockBackDamage * action.knockbackPercent;

        if (action.maxTargets > 1)
        {
            ExecuteAOE(attacker, action, primaryTarget, healthChange, knockbackChange);
        }
        else
        {
            ExecuteSingle(attacker, action, primaryTarget, healthChange, knockbackChange);
        }

        if (action.zoneSpawnPosition == ZoneSpawnPosition.Self && action.zoneData != null)
        {
            Transform sticky = action.zoneSticky ? attacker.transform : null;
            AreaEffectLogic.SpawnZone(
                action.zoneData,
                attacker.transform.position,
                attacker,
                sticky,
                action.excludeCasterFromZone,
                action.zoneSticky);
        }

        if (action.zoneSpawnPosition == ZoneSpawnPosition.Projectile)
        {
            SpawnProjectile(attacker, action, primaryTarget, healthChange, knockbackChange);
        }
    }

    static void ExecuteSingle(Stats attacker, CombatAction action, Stats target, float healthChange, float knockbackChange)
    {
        if (healthChange != 0f)
            target.AlterHealth(healthChange, new DamageSource(DamageSource.DamageType.Melee) { IsEnemy = attacker._Enemy });

        if (knockbackChange != 0f)
            target.AlterKnockback(knockbackChange, attacker._Enemy);

        ApplyEffectsToTarget(action, target);
    }

    static void ExecuteAOE(Stats attacker, CombatAction action, Stats primaryTarget, float healthChange, float knockbackChange)
    {
        Vector2 center = GetDetectionCenter(attacker, action);
        float effectiveRange = attacker._HorizontalRange * action.rangePercent;

        List<string> targetTags = GetTargetTags(attacker, action);
        List<Stats> targets = AttackDetection.FindTargetsInCircle(
            center, effectiveRange, targetTags, attacker);

        targets.Sort((a, b) =>
            Vector2.Distance(attacker.transform.position, a.transform.position)
                .CompareTo(Vector2.Distance(attacker.transform.position, b.transform.position)));

        int count = 0;
        foreach (Stats t in targets)
        {
            if (count >= action.maxTargets) break;
            count++;

            if (healthChange != 0f)
                t.AlterHealth(healthChange, new DamageSource(DamageSource.DamageType.AOE) { IsEnemy = attacker._Enemy });

            if (knockbackChange != 0f)
                t.AlterKnockback(knockbackChange, attacker._Enemy);

            ApplyEffectsToTarget(action, t);
        }
    }

    static void ApplyEffectsToTarget(CombatAction action, Stats target)
    {
        foreach (var effect in action.effectsOnHit)
            target.statusEffectManager.ApplyEffect(effect, effect.duration);

        if (action.zoneData != null && action.zoneSpawnPosition == ZoneSpawnPosition.Touch)
        {
            Transform sticky = action.zoneSticky ? target.transform : null;
            AreaEffectLogic.SpawnZone(
                action.zoneData,
                target.transform.position,
                target.statusEffectManager.stats,
                sticky,
                action.excludeCasterFromZone,
                action.zoneSticky);
        }
    }

    static void SpawnProjectile(Stats attacker, CombatAction action, Stats target, float healthChange, float knockbackChange)
    {
        var ps = action.projectileSettings;
        if (ps == null || ps.prefab == null)
        {
            Debug.LogWarning($"{attacker.gameObject.name}: Action '{action.actionName}' has Projectile delivery but no ProjectileSettings or prefab set.");
            return;
        }

        GameObject projGO = Object.Instantiate(ps.prefab, attacker.transform.position, Quaternion.identity);

        if (projGO.TryGetComponent(out Projectile p))
        {
            p.InitializeProjectile(
                target.transform,
                ps.speed,
                ps.maxHeight,
                ps.trajectoryCurve,
                ps.axisCorrectionCurve,
                ps.speedCurve,
                hit =>
                {
                    if (hit is Stats hitStats)
                    {
                        if (healthChange != 0f)
                            hitStats.AlterHealth(healthChange, new DamageSource(DamageSource.DamageType.Ranged) { IsEnemy = attacker._Enemy });

                        if (knockbackChange != 0f)
                            hitStats.AlterKnockback(knockbackChange, attacker._Enemy);

                        ApplyEffectsToTarget(action, hitStats);
                    }
                });
        }
        else
        {
            Debug.LogWarning($"{attacker.gameObject.name}: Projectile prefab is missing a Projectile component.");
        }
    }

    static Vector2 GetDetectionCenter(Stats attacker, CombatAction action)
    {
        bool facingLeft = attacker.transform.right.x < 0;
        float effectiveRange = attacker._HorizontalRange * action.rangePercent;
        return action.extendsForward
            ? AttackLogic.CalculateAttackCenter(
                attacker.transform.position,
                facingLeft,
                new Vector2(effectiveRange, 0f))
            : (Vector2)attacker.transform.position;
    }

    public static List<string> GetTargetTags(Stats attacker, CombatAction action)
    {
        List<string> tags = new List<string>();
        if (action.targetFriendly)
        {
            if (attacker._Enemy)
            {
                tags.Add("Allies");
                tags.Add("Player");
            }
            else
            {
                tags.Add("Allies");
            }
        }
        else
        {
            if (attacker._Enemy)
            {
                tags.Add("Player");
                tags.Add("Allies");
            }
            else
            {
                tags.Add("Enemy");
            }
        }
        return tags;
    }
}