using System.Collections.Generic;
using UnityEngine;

public static class CombatLogic
{
    public const int UnlimitedTargetsThreshold = 100;

    public static void ExecuteActionOnSelf(Stats self, CombatAction action)
    {
        ApplyCombatActionToTargets(self, action, new List<Stats> { self });
    }

    #region non self actions
    public static bool CalculateHitbox(Stats attacker, CombatAction action)
    {
        Vector2 center = GetDetectionCenter(attacker, action);
        float radius = attacker._HorizontalRange * action.rangePercent;

        return ExicuteCombatAction(attacker, action, center, radius);
    }

    static Vector2 GetDetectionCenter(Stats attacker, CombatAction action)
    {
        bool facingLeft = attacker.transform.right.x < 0;
        float effectiveRange = attacker._HorizontalRange * action.rangePercent;
        return action.extendsForward
            ? CalculateAttackCenter(attacker.transform.position, facingLeft, new Vector2(effectiveRange, 0f))
            : (Vector2)attacker.transform.position;
    }

    public static Vector2 CalculateAttackCenter(Vector2 pos, bool left, Vector2 range) =>
        pos + new Vector2(left ? -range.x / 2f : range.x / 2f, 0f);

    public static bool ExicuteCombatAction(Stats attacker, CombatAction action, Vector2 position, float radius, List<string> targetTagsOverride = null)
    {
        if (attacker == null || action == null) return false;
        if (action.maxTargets <= 0) return false;

        List<string> tags = targetTagsOverride ?? GetTargetTags(attacker, action);
        List<Stats> candidates = AttackDetection.FindTargetsInCircle(position, radius, tags, attacker, allowSelf: action.includeSelf);
        List<Stats> targets = FilterValidTargets(candidates, position);
        if (targets.Count == 0) return false;

        if (action.UseProjectile)
        {
            // Projectiles target the single closest valid unit.
            SpawnProjectile(attacker, action, targets[0]);
        }
        else
        {
            // maxTargets > 100 counts as unlimited targets; 1..100 caps target count.
            if (action.maxTargets <= UnlimitedTargetsThreshold && targets.Count > action.maxTargets)
                targets = targets.GetRange(0, action.maxTargets);

            ApplyCombatActionToTargets(attacker, action, targets);
        }

        if (action.zoneSpawnPosition == ZoneSpawnPosition.Self && action.zoneData != null)
        {
            Transform sticky = action.zoneSticky ? attacker.transform : null;
            AreaEffectLogic.SpawnZone(action.zoneData, position, attacker, sticky, action.excludeCasterFromZone, action.zoneSticky, tags);
        }
        return true;
    }

    static List<Stats> FilterValidTargets(List<Stats> candidates, Vector2 origin)
    {
        candidates.RemoveAll(t => t == null || t._IsDead);
        candidates.Sort((a, b) =>
            Vector2.Distance(origin, a.transform.position)
                .CompareTo(Vector2.Distance(origin, b.transform.position)));
        return candidates;
    }

    public static List<string> GetTargetTags(Stats attacker, CombatAction action, bool? ZoneTargetsEnemies = null)
    {
        List<string> tags = new List<string>();
        if (ZoneTargetsEnemies == null)
        {
            if (action.targetFriendly)
            {
                if (attacker._Enemy)
                {
                    tags.Add("Enemy");
                }
                else
                {
                    tags.Add("Allies");
                    tags.Add("Player");
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
        }
        else
        {
            if (ZoneTargetsEnemies == true)
            {
                tags.Add("Enemy");
            }
            else
            {
                tags.Add("Allies");
                tags.Add("Player");
            }
        }

        return tags;
    }
    #endregion

    #region action effects
    static void ApplyCombatActionToTargets(Stats attacker, CombatAction action, List<Stats> targets)
    {
        var (healthChange, knockbackChange) = GetEffectMagnitude(attacker, action);

        foreach (Stats target in targets)
        {
            ApplyCombatActionEffects(attacker, action, target, healthChange, knockbackChange);
        }
    }

    static void SpawnProjectile(Stats attacker, CombatAction action, Stats target)
    {
        var ps = action.projectileSettings;

        if (ps == null || ps.prefab == null)
        {
            Debug.LogWarning($"{attacker.gameObject.name}: Action {action.actionName} has Projectile delivery but no ProjectileSettings or prefab set.");
            return;
        }
        var (healthChange, knockbackChange) = GetEffectMagnitude(attacker, action);

        GameObject projGO = Object.Instantiate(ps.prefab, attacker.transform.position, Quaternion.identity);

        if (projGO.TryGetComponent(out Projectile p))
        {
            p.InitializeProjectile(
                target.transform, 
                ps.speed, 
                ps.maxTravelTime,
                ps.maxHeight, 
                ps.trajectoryCurve, 
                ps.axisCorrectionCurve, 
                ps.speedCurve, 
                hitStats =>
                {
                    if (hitStats == null) return;
                    ApplyCombatActionEffects(attacker, action, hitStats, healthChange, knockbackChange);
                }
            );
        }
        else
        {
            Debug.LogWarning($"{attacker.gameObject.name}: Projectile prefab is missing a Projectile component.");
        }
    }

    static void ApplyCombatActionEffects(Stats attacker, CombatAction action, Stats target, float healthChange, float knockbackChange)
    {
        if (healthChange != 0f)
        {
            target.AlterHealth(healthChange, attacker.gameObject.transform.position);
        }

        if (knockbackChange != 0f)
        {
            target.AlterKnockback(knockbackChange, attacker._Enemy);
        }

        ApplyStatusEffects(attacker, action, target);
        SpawnZone(attacker, action, target);
    }

    static (float health, float knockback) GetEffectMagnitude(Stats attacker, CombatAction action)
    {
        if (!action.ScaleEffectsWithUsersStats) return (action.healthChangePercent, action.knockbackPercent);

        return (attacker._AttackDamage * action.healthChangePercent, attacker._KnockBackDamage * action.knockbackPercent);
    }

    static void ApplyStatusEffects(Stats attacker, CombatAction action, Stats target)
    {
        if (action.effectsOnHit != null && action.effectsOnHit.Count != 0)
        {
            foreach (var effect in action.effectsOnHit)
            {
                if (effect == null) continue;
                target.statusEffectManager.AddEffect(effect, effect.duration);
            }
        }
    }

    static void SpawnZone(Stats attacker, CombatAction action, Stats target)
    {
        if (action.zoneData == null || action.zoneSpawnPosition != ZoneSpawnPosition.Touch) return;

        Transform sticky = action.zoneSticky ? target.transform : null;
        List<string> tags = GetTargetTags(attacker, action, action.ZoneTargetsEnemies);

        AreaEffectLogic.SpawnZone(
            action.zoneData,
            target.transform.position,
            attacker,
            sticky,
            action.excludeCasterFromZone,
            action.zoneSticky,
            tags
        );
    }
    #endregion
}