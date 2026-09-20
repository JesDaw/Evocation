using System.Collections.Generic;
using UnityEngine;

// Central place for turning base stats + CombatAction data into a combat
// profile. The "raw" method is the ONE implementation of this math — both
// ScriptableStats (design-time) and Stats (live, runtime) go through it via
// thin overloads below, instead of each keeping its own copy of the
// cooldown/DPS formula.
public static class PowerMath
{
    // Works on the raw list so both ScriptableStats.combatActions and
    // Stats._CombatActions can share this. Uses definesStoppingRange (already
    // documented on CombatAction as meant for "your highest-priority
    // offensive action") as the primary signal.
    public static CombatAction GetPrimaryOffensiveAction(List<CombatAction> actions)
    {
        if (actions == null || actions.Count == 0) return null;
        if (actions.Count == 1) return actions[0];

        var flagged = actions.Find(a => a.definesStoppingRange);
        if (flagged != null) return flagged;

        var fallback = actions.Find(a => !a.targetFriendly && a.healthChangePercent < 0f);
        return fallback ?? actions[0];
    }

    // True if the primary offensive action hits anything other than exactly
    // one target — works whether maxTargets is a small cap or the "unlimited"
    // sentinel (<= 0).
    public static bool IsAOE(List<CombatAction> actions)
    {
        var primary = GetPrimaryOffensiveAction(actions);
        return primary != null && primary.maxTargets != 1;
    }

    // Friendly-targeted or healing (positive healthChangePercent) actions are
    // utility, not combat power. Rough heuristic — refine once effectsOnHit's
    // actual StatusEffect data is available.
    public static bool IsUtilityAction(CombatAction a)
    {
        return a.targetFriendly || a.healthChangePercent > 0f;
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
        public int MaxTargets; // int.MaxValue represents "unlimited" (maxTargets <= 0 on the action)
    }

    // THE single implementation of "base stats scaled by primary action" —
    // everything else calls into this one.
    public static UnitProfile GetProfileFromRaw(
        int attackDamage, float moveSpeed, float knockBackDamage,
        float maxHealth, float knockBackMaxHealth, float horizontalRange,
        float actionCooldown, List<CombatAction> combatActions)
    {
        var action = GetPrimaryOffensiveAction(combatActions);

        if (action == null)
        {
            // No CombatAction data — fall back to raw base stats (equivalent
            // to every percent being 100%).
            return new UnitProfile
            {
                Atk = attackDamage,
                Move = moveSpeed,
                KBD = knockBackDamage,
                HP = maxHealth,
                KBH = knockBackMaxHealth,
                Range = horizontalRange,
                Cooldown = Mathf.Max(actionCooldown, 0.01f),
                MaxTargets = 1
            };
        }

        return new UnitProfile
        {
            Atk = attackDamage * Mathf.Abs(action.healthChangePercent),
            Move = moveSpeed,
            KBD = knockBackDamage * action.knockbackPercent,
            HP = maxHealth,
            KBH = knockBackMaxHealth,
            Range = horizontalRange * action.rangePercent,
            // Confirmed real formula (from CpuCombatActionState): _ActionCooldown * action.castCooldown.
            Cooldown = Mathf.Max(actionCooldown * action.castCooldown, 0.01f),
            MaxTargets = action.maxTargets <= 0 ? int.MaxValue : action.maxTargets
        };
    }

    public static UnitProfile GetProfile(ScriptableStats s)
    {
        return GetProfileFromRaw(s._AttackDamage, s._MoveSpeed, s._KnockBackDamage,
            s._MaxHealth, s._KnockBackMaxHealth, s._HorizontalRange, s._ActionCooldown, s.combatActions);
    }

    // Runtime overload — same math, fed from the live spawned unit's mirrored fields.
    public static UnitProfile GetProfile(Stats s)
    {
        return GetProfileFromRaw(s._AttackDamage, s._MoveSpeed, s._KnockBackDamage,
            s._MaxHealth, s._KnockBackMaxHealth, s._HorizontalRange, s._ActionCooldown, s._CombatActions);
    }

    public static float GetDPS(UnitProfile profile) => profile.Atk / profile.Cooldown;

    // ROUGH FIRST PASS — heals valued as heal-per-second, status effects a flat
    // placeholder weight each (StatusEffect's own fields aren't visible yet).
    public static float GetUtilityPower(int attackDamage, float actionCooldown, List<CombatAction> combatActions,
        float healWeight = 1f, float statusEffectWeight = 5f)
    {
        if (combatActions == null) return 0f;

        float utility = 0f;
        foreach (var a in combatActions)
        {
            if (!IsUtilityAction(a)) continue;

            if (a.healthChangePercent > 0f)
            {
                float healAmount = attackDamage * a.healthChangePercent;
                float cooldown = Mathf.Max(actionCooldown * a.castCooldown, 0.01f);
                utility += (healAmount / cooldown) * healWeight;
            }

            utility += (a.effectsOnHit?.Count ?? 0) * statusEffectWeight;
        }
        return utility;
    }

    public static float GetUtilityPower(ScriptableStats s) =>
        GetUtilityPower(s._AttackDamage, s._ActionCooldown, s.combatActions);

    public static float GetUtilityPower(Stats s) =>
        GetUtilityPower(s._AttackDamage, s._ActionCooldown, s._CombatActions);
}
