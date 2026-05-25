using System.Collections.Generic;
using UnityEngine;

public static class CombatLogic
{
    public static bool ExecuteAction(Stats attacker, CombatAction action, Stats primaryTarget, bool recheckTargets = false)
    {
        Stats targetToHit = primaryTarget;

        if (recheckTargets)
        {
            List<Stats> inRange = GetTargetsInRange(attacker, action);

            if (inRange.Count == 0)
                return false;

            bool originalInRange = inRange.Exists(t => t == primaryTarget);
            targetToHit = originalInRange ? primaryTarget : inRange[0];

            if (!originalInRange)
                Debug.Log($"[Combat] {attacker.gameObject.name}: Primary target '{primaryTarget.gameObject.name}' left range, retargeting to '{targetToHit.gameObject.name}'");

            if (action.maxTargets > 1)
            {
                ExecuteAOEFromList(attacker, action, inRange);
                if (action.zoneSpawnPosition == ZoneSpawnPosition.Self && action.zoneData != null)
                {
                    Transform sticky = action.zoneSticky ? attacker.transform : null;
                    List<string> tags = GetTargetTags(attacker, action);
                    AreaEffectLogic.SpawnZone(action.zoneData, attacker.transform.position, attacker, sticky, action.excludeCasterFromZone, action.zoneSticky, tags);
                }
                return true;
            }
        }

        float healthChange  = attacker._AttackDamage * action.healthChangePercent;
        float knockbackChange = attacker._KnockBackDamage * action.knockbackPercent;

        if (action.zoneSpawnPosition != ZoneSpawnPosition.Projectile)
            ExecuteSingle(attacker, action, targetToHit, healthChange, knockbackChange);

        if (action.zoneSpawnPosition == ZoneSpawnPosition.Self && action.zoneData != null)
        {
            Transform sticky = action.zoneSticky ? attacker.transform : null;
            List<string> tags = GetTargetTags(attacker, action);
            AreaEffectLogic.SpawnZone(
                action.zoneData,
                attacker.transform.position,
                attacker,
                sticky,
                action.excludeCasterFromZone,
                action.zoneSticky,
                tags);
        }

        if (action.zoneSpawnPosition == ZoneSpawnPosition.Projectile)
            SpawnProjectile(attacker, action, targetToHit, healthChange, knockbackChange);

        return true;
    }

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    static List<Stats> GetTargetsInRange(Stats attacker, CombatAction action)
    {
        Vector2 center = GetDetectionCenter(attacker, action);
        float effectiveRange = attacker._HorizontalRange * action.rangePercent;
        List<string> targetTags = GetTargetTags(attacker, action);

        List<Stats> targets = AttackDetection.FindTargetsInCircle(center, effectiveRange, targetTags, attacker);
        targets.RemoveAll(t => t == null || t._IsDead);
        targets.Sort((a, b) =>
            Vector2.Distance(attacker.transform.position, a.transform.position)
                .CompareTo(Vector2.Distance(attacker.transform.position, b.transform.position)));

        return targets;
    }

    static void ExecuteSingle(Stats attacker, CombatAction action, Stats target, float healthChange, float knockbackChange)
    {
        if (healthChange != 0f)
        {
            target.AlterHealth(healthChange,
                new DamageSource(DamageSource.DamageType.Melee, attacker.transform.position)
                    { IsEnemy = attacker._Enemy });
        }

        if (knockbackChange != 0f)
            target.AlterKnockback(knockbackChange, attacker._Enemy);

        ApplyEffectsToTarget(attacker, action, target);
    }

    static void ExecuteAOEFromList(Stats attacker, CombatAction action, List<Stats> targets)
    {
        float healthChange    = attacker._AttackDamage  * action.healthChangePercent;
        float knockbackChange = attacker._KnockBackDamage * action.knockbackPercent;

        int count = 0;
        foreach (Stats t in targets)
        {
            if (action.maxTargets >= 0 && count >= action.maxTargets) break;
            count++;

            if (healthChange != 0f)
            {
                t.AlterHealth(healthChange,
                    new DamageSource(DamageSource.DamageType.AOE, attacker.transform.position)
                        { IsEnemy = attacker._Enemy });
            }

            if (knockbackChange != 0f)
                t.AlterKnockback(knockbackChange, attacker._Enemy);

            ApplyEffectsToTarget(attacker, action, t);
        }
    }

    // Kept for internal AOE re-detection path
    static void ExecuteAOE(Stats attacker, CombatAction action, Stats primaryTarget, float healthChange, float knockbackChange)
    {
        List<Stats> targets = GetTargetsInRange(attacker, action);

        int count = 0;
        foreach (Stats t in targets)
        {
            if (action.maxTargets >= 0 && count >= action.maxTargets) break;
            count++;

            if (healthChange != 0f)
            {
                t.AlterHealth(healthChange,
                    new DamageSource(DamageSource.DamageType.AOE, attacker.transform.position)
                        { IsEnemy = attacker._Enemy });
            }

            if (knockbackChange != 0f)
                t.AlterKnockback(knockbackChange, attacker._Enemy);

            ApplyEffectsToTarget(attacker, action, t);
        }
    }

    static void ApplyEffectsToTarget(Stats attacker, CombatAction action, Stats target)
    {
        foreach (var effect in action.effectsOnHit)
            target.statusEffectManager.ApplyEffect(effect, effect.duration);

        if (action.zoneData != null && action.zoneSpawnPosition == ZoneSpawnPosition.Touch)
        {
            Transform sticky = action.zoneSticky ? target.transform : null;
            List<string> tags = GetTargetTags(attacker, action);
            Debug.Log($"[Combat] {attacker.gameObject.name} spawning Touch zone '{action.zoneData.name}' at {target.gameObject.name}, tags=[{string.Join(",", tags)}]");
            AreaEffectLogic.SpawnZone(
                action.zoneData,
                target.transform.position,
                attacker,
                sticky,
                action.excludeCasterFromZone,
                action.zoneSticky,
                tags);
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

        // Capture attacker position for the async callback — the attacker may have moved by hit time
        Vector3 attackerPos = attacker.transform.position;
        bool isEnemy        = attacker._Enemy;

        GameObject projGO = UnityEngine.Object.Instantiate(ps.prefab, attacker.transform.position, Quaternion.identity);

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
                        {
                            hitStats.AlterHealth(healthChange,
                                new DamageSource(DamageSource.DamageType.Ranged, attackerPos)
                                    { IsEnemy = isEnemy });
                        }

                        if (knockbackChange != 0f)
                            hitStats.AlterKnockback(knockbackChange, isEnemy);

                        ApplyEffectsToTarget(attacker, action, hitStats);
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
            tags.Add("Allies");
            tags.Add("Player");
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