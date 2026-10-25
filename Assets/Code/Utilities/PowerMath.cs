using System.Collections.Generic;
using UnityEngine;

public static class PowerMath
{
    public const int UnlimitedTargetsThreshold = 100;

    /// <summary>
    /// Selects the primary damage-dealing offensive action.
    /// Prefers an offensive action that also defines stopping range, then the highest-priority
    /// enemy-damaging action, then falls back to the first non-null action.
    /// </summary>
    public static CombatAction GetPrimaryOffensiveAction(List<CombatAction> actions)
    {
        if (actions == null || actions.Count == 0) return null;

        CombatAction bestOffensiveStopping = null;
        CombatAction bestOffensive = null;
        CombatAction firstValid = null;

        foreach (CombatAction a in actions)
        {
            if (a == null) continue;
            if (firstValid == null) firstValid = a;

            bool isOffensive = !a.targetFriendly && a.healthChangePercent < 0f;
            if (!isOffensive) continue;

            if (a.definesStoppingRange)
            {
                if (bestOffensiveStopping == null || a.priority > bestOffensiveStopping.priority)
                    bestOffensiveStopping = a;
            }

            if (bestOffensive == null || a.priority > bestOffensive.priority)
                bestOffensive = a;
        }

        return bestOffensiveStopping ?? bestOffensive ?? firstValid;
    }

    /// <summary>
    /// Matches CpuMoveState.HasStoppingRangeTarget: uses definesStoppingRange if any action sets it,
    /// otherwise uses the highest-priority action.
    /// </summary>
    public static CombatAction GetStoppingRangeAction(List<CombatAction> actions)
    {
        if (actions == null || actions.Count == 0) return null;

        CombatAction bestStopping = null;
        CombatAction bestPriority = null;

        foreach (CombatAction a in actions)
        {
            if (a == null) continue;
            if (a.definesStoppingRange)
            {
                if (bestStopping == null || a.priority > bestStopping.priority)
                    bestStopping = a;
            }
            if (bestPriority == null || a.priority > bestPriority.priority)
                bestPriority = a;
        }

        return bestStopping ?? bestPriority;
    }

    public static int NormalizeMaxTargets(CombatAction action)
    {
        if (action == null) return 1;
        if (action.maxTargets <= 0) return 0;
        if (action.UseProjectile && action.zoneData == null) return 1;
        return action.maxTargets > UnlimitedTargetsThreshold ? int.MaxValue : action.maxTargets;
    }

    public static bool IsAOE(List<CombatAction> actions)
    {
        var primary = GetPrimaryOffensiveAction(actions);
        return NormalizeMaxTargets(primary) > 1;
    }

    public static bool IsUtilityAction(CombatAction a)
    {
        if (a == null) return false;
        return a.targetFriendly || a.healthChangePercent > 0f || (a.effectsOnHit != null && a.effectsOnHit.Count > 0);
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
        public int MaxTargets;
    }

    public static UnitProfile GetProfileFromRaw(
        int attackDamage, float moveSpeed, float knockBackDamage,
        float maxHealth, float knockBackMaxHealth, float horizontalRange,
        float actionCooldown, List<CombatAction> combatActions,
        float animationStartupMs = 0f, float animationRecoveryMs = 0f, float extraEndlagSec = 0f)
    {
        var offensiveAction = GetPrimaryOffensiveAction(combatActions);
        var stoppingAction = GetStoppingRangeAction(combatActions) ?? offensiveAction;

        float safeMove = Mathf.Max(0f, moveSpeed);
        float safeHP = Mathf.Max(0f, maxHealth);
        float safeKBH = Mathf.Max(0f, knockBackMaxHealth);
        float safeBaseRange = Mathf.Max(0f, horizontalRange);
        float safeBaseCooldown = Mathf.Max(0.01f, actionCooldown);
        float animOverheadSec = Mathf.Max(0f, (animationStartupMs + animationRecoveryMs) / 1000f) + Mathf.Max(0f, extraEndlagSec);

        if (offensiveAction == null)
        {
            return new UnitProfile
            {
                Atk = Mathf.Max(0f, attackDamage),
                Move = safeMove,
                KBD = Mathf.Max(0f, knockBackDamage),
                HP = safeHP,
                KBH = safeKBH,
                Range = safeBaseRange,
                Cooldown = Mathf.Max(safeBaseCooldown + animOverheadSec, 0.05f),
                MaxTargets = 1
            };
        }

        // If the action does not damage enemies, offensive Atk is 0.
        bool dealsEnemyDamage = !offensiveAction.targetFriendly && offensiveAction.healthChangePercent < 0f;
        float rawAtk = dealsEnemyDamage
            ? (offensiveAction.ScaleEffectsWithUsersStats
                ? attackDamage * Mathf.Abs(offensiveAction.healthChangePercent)
                : Mathf.Abs(offensiveAction.healthChangePercent))
            : 0f;

        float rawKBD = !offensiveAction.targetFriendly && offensiveAction.knockbackPercent > 0f
            ? (offensiveAction.ScaleEffectsWithUsersStats
                ? knockBackDamage * offensiveAction.knockbackPercent
                : offensiveAction.knockbackPercent)
            : 0f;

        float rangeMult = Mathf.Max(0f, stoppingAction.rangePercent);
        float effectiveRange = safeBaseRange * rangeMult;
        if (stoppingAction.extendsForward)
            effectiveRange *= 1.5f;

        float castCooldown = Mathf.Max(0.01f, offensiveAction.castCooldown);
        float totalCycleCooldown = Mathf.Max((safeBaseCooldown * castCooldown) + animOverheadSec, 0.05f);

        return new UnitProfile
        {
            Atk = Mathf.Max(0f, rawAtk),
            Move = safeMove,
            KBD = Mathf.Max(0f, rawKBD),
            HP = safeHP,
            KBH = safeKBH,
            Range = effectiveRange,
            Cooldown = totalCycleCooldown,
            MaxTargets = dealsEnemyDamage ? NormalizeMaxTargets(offensiveAction) : 0
        };
    }

    public static UnitProfile GetProfile(ScriptableStats s)
    {
        if (s == null) return default;
        return GetProfileFromRaw(
            s._AttackDamage, s._MoveSpeed, s._KnockBackDamage,
            s._MaxHealth, s._KnockBackMaxHealth, s._HorizontalRange,
            s._ActionCooldown, s.combatActions,
            s._AnimationStartupTime, s._AnimationRecoveryTime, s._ExtraEndlag);
    }

    public static UnitProfile GetProfile(Stats s)
    {
        if (s == null) return default;
        return GetProfileFromRaw(
            s._AttackDamage, s._MoveSpeed, s._KnockBackDamage,
            s._MaxHealth, s._KnockBackMaxHealth, s._HorizontalRange,
            s._ActionCooldown, s._CombatActions,
            s._AnimationStartupTime, s._AnimationRecoveryTime, s._ExtraEndlag);
    }

    public static float GetDPS(UnitProfile profile) =>
        profile.MaxTargets <= 0 ? 0f : profile.Atk / Mathf.Max(profile.Cooldown, 0.05f);

    public static float GetUtilityPower(
        int attackDamage, float actionCooldown, List<CombatAction> combatActions,
        float healWeight = 1f, float statusEffectWeight = 5f)
    {
        if (combatActions == null) return 0f;

        float utility = 0f;
        float safeBaseCooldown = Mathf.Max(0.01f, actionCooldown);

        foreach (var a in combatActions)
        {
            if (!IsUtilityAction(a)) continue;

            int targetCap = NormalizeMaxTargets(a);
            float aoeSupportFactor = targetCap <= 1 ? 1f : Mathf.Min(targetCap, 4);

            if (a.healthChangePercent > 0f)
            {
                float healAmount = a.ScaleEffectsWithUsersStats
                    ? Mathf.Max(0, attackDamage) * a.healthChangePercent
                    : a.healthChangePercent;
                float cooldown = Mathf.Max(safeBaseCooldown * Mathf.Max(0.01f, a.castCooldown), 0.05f);
                utility += (healAmount / cooldown) * healWeight * aoeSupportFactor;
            }

            int validEffects = 0;
            if (a.effectsOnHit != null)
            {
                foreach (var eff in a.effectsOnHit)
                    if (eff != null) validEffects++;
            }

            utility += validEffects * statusEffectWeight * aoeSupportFactor;
        }
        return utility;
    }

    public static float GetUtilityPower(ScriptableStats s) =>
        s == null ? 0f : GetUtilityPower(s._AttackDamage, s._ActionCooldown, s.combatActions);

    public static float GetUtilityPower(Stats s) =>
        s == null ? 0f : GetUtilityPower(s._AttackDamage, s._ActionCooldown, s._CombatActions);
}