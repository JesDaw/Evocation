
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared calculations for design-time and runtime combat profiles.
/// </summary>
public static class PowerMath
{
    public static CombatAction GetPrimaryOffensiveAction(
        List<CombatAction> actions)
    {
        if (actions == null || actions.Count == 0)
            return null;

        CombatAction fallback = null;

        foreach (CombatAction action in actions)
        {
            if (action == null)
                continue;

            if (fallback == null)
                fallback = action;

            if (action.definesStoppingRange)
                return action;
        }

        if (fallback == null)
            return null;

        foreach (CombatAction action in actions)
        {
            if (action == null)
                continue;

            if (!action.targetFriendly &&
                action.healthChangePercent < 0f)
                return action;
        }

        return fallback;
    }

    public static bool IsAOE(List<CombatAction> actions)
    {
        CombatAction primary = GetPrimaryOffensiveAction(actions);

        return primary != null && primary.maxTargets != 1;
    }

    public static bool IsUtilityAction(CombatAction action)
    {
        return action != null &&
               (action.targetFriendly ||
                action.healthChangePercent > 0f);
    }

    [System.Serializable]
    public struct UnitProfile
    {
        public float Atk;
        public float Move;
        public float KBD;
        public float HP;
        public float KBH;
        public float Range;
        public float Cooldown;

        // int.MaxValue represents unlimited targets.
        public int MaxTargets;
    }

    public static UnitProfile GetProfileFromRaw(
        int attackDamage,
        float moveSpeed,
        float knockBackDamage,
        float maxHealth,
        float knockBackMaxHealth,
        float horizontalRange,
        float actionCooldown,
        List<CombatAction> combatActions)
    {
        CombatAction action =
            GetPrimaryOffensiveAction(combatActions);

        if (action == null)
        {
            return new UnitProfile
            {
                Atk = Mathf.Max(0f, attackDamage),
                Move = Mathf.Max(0f, moveSpeed),
                KBD = Mathf.Max(0f, knockBackDamage),
                HP = Mathf.Max(0f, maxHealth),
                KBH = Mathf.Max(0f, knockBackMaxHealth),
                Range = Mathf.Max(0f, horizontalRange),
                Cooldown = Mathf.Max(0.01f, actionCooldown),
                MaxTargets = 1
            };
        }

        return new UnitProfile
        {
            Atk = Mathf.Max(
                0f,
                attackDamage * Mathf.Abs(action.healthChangePercent)),

            Move = Mathf.Max(0f, moveSpeed),

            KBD = Mathf.Max(
                0f,
                knockBackDamage * action.knockbackPercent),

            HP = Mathf.Max(0f, maxHealth),

            KBH = Mathf.Max(0f, knockBackMaxHealth),

            Range = Mathf.Max(
                0f,
                horizontalRange * action.rangePercent),

            // Matches the existing runtime cooldown formula.
            Cooldown = Mathf.Max(
                0.01f,
                actionCooldown * action.castCooldown),

            MaxTargets = action.maxTargets <= 0
                ? int.MaxValue
                : action.maxTargets
        };
    }

    public static UnitProfile GetProfile(ScriptableStats stats)
    {
        if (stats == null)
            return new UnitProfile();

        return GetProfileFromRaw(
            stats._AttackDamage,
            stats._MoveSpeed,
            stats._KnockBackDamage,
            stats._MaxHealth,
            stats._KnockBackMaxHealth,
            stats._HorizontalRange,
            stats._ActionCooldown,
            stats.combatActions);
    }

    public static UnitProfile GetProfile(Stats stats)
    {
        if (stats == null)
            return new UnitProfile();

        return GetProfileFromRaw(
            stats._AttackDamage,
            stats._MoveSpeed,
            stats._KnockBackDamage,
            stats._MaxHealth,
            stats._KnockBackMaxHealth,
            stats._HorizontalRange,
            stats._ActionCooldown,
            stats._CombatActions);
    }

    public static float GetDPS(UnitProfile profile)
    {
        return Mathf.Max(0f, profile.Atk) /
               Mathf.Max(0.01f, profile.Cooldown);
    }

    public static float GetUtilityPower(
        int attackDamage,
        float actionCooldown,
        List<CombatAction> combatActions,
        float healWeight = 1f,
        float statusEffectWeight = 5f)
    {
        if (combatActions == null)
            return 0f;

        float utility = 0f;

        foreach (CombatAction action in combatActions)
        {
            if (!IsUtilityAction(action))
                continue;

            if (action.healthChangePercent > 0f)
            {
                float healAmount =
                    attackDamage * action.healthChangePercent;

                float cooldown = Mathf.Max(
                    0.01f,
                    actionCooldown * action.castCooldown);

                utility +=
                    (healAmount / cooldown) * healWeight;
            }

            utility +=
                (action.effectsOnHit?.Count ?? 0) *
                statusEffectWeight;
        }

        return utility;
    }

    public static float GetUtilityPower(ScriptableStats stats)
    {
        if (stats == null)
            return 0f;

        return GetUtilityPower(
            stats._AttackDamage,
            stats._ActionCooldown,
            stats.combatActions);
    }

    public static float GetUtilityPower(Stats stats)
    {
        if (stats == null)
            return 0f;

        return GetUtilityPower(
            stats._AttackDamage,
            stats._ActionCooldown,
            stats._CombatActions);
    }
}
