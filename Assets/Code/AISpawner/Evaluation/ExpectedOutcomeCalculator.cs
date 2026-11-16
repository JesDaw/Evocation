using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Estimates battle outcomes using a Lanchester + Range + Utility combat-strength model.
/// Positive outcomes favor Player/Allies; negative outcomes favor AI/Enemy.
/// </summary>
public static class ExpectedOutcomeCalculator
{
    const float MaxOpeningLeadSeconds = 3f;
    const float MinimumClosingSpeed = 0.25f;
    const float MinimumFightDuration = 1f;
    const float MinimumDps = 0.01f;

    public struct CombatProfile
    {
        public float dps;
        public float hp;
        public float range;
        public float moveSpeed;
        public int maxTargets;
        public float utilityDps;
        public float threatWeight;

        public bool IsAOE => maxTargets > 1;

        public CombatProfile(
            float dps,
            float hp,
            float range,
            float moveSpeed,
            int maxTargets,
            float utilityDps = 0f,
            float threatWeight = 1f)
        {
            this.dps = Mathf.Max(0f, dps);
            this.hp = Mathf.Max(0f, hp);
            this.range = Mathf.Max(0f, range);
            this.moveSpeed = Mathf.Max(0f, moveSpeed);
            this.maxTargets = maxTargets < 0
                ? 0
                : (maxTargets > PowerMath.UnlimitedTargetsThreshold ? int.MaxValue : maxTargets);
            this.utilityDps = Mathf.Max(0f, utilityDps);
            this.threatWeight = Mathf.Clamp(threatWeight, 0.05f, 4f);
        }
    }

    public static float CalculateExpectedOutcome(
        List<Stats> aiTeam,
        List<Stats> playerTeam,
        ScriptableStats extraCharacter = null,
        int extraCharacterCopies = 1)
    {
        List<CombatProfile> aiProfiles = BuildLiveProfiles(aiTeam);

        if (extraCharacter != null && extraCharacterCopies > 0)
        {
            CombatProfile candidateProfile = BuildCandidateProfile(extraCharacter);
            int copies = Mathf.Max(0, extraCharacterCopies);
            for (int i = 0; i < copies; i++)
                aiProfiles.Add(candidateProfile);
        }

        List<CombatProfile> playerProfiles = BuildLiveProfiles(playerTeam);

        return EvaluateProfiles(aiProfiles, playerProfiles);
    }

    /// <summary>
    /// Evaluates the entire battlefield at once while weighting threats by how close they are
    /// to the AI base (EnemyBase). Closer player units have higher urgency/weight.
    /// </summary>
    public static float CalculateDistanceWeightedOutcome(
        List<Stats> allAiUnits,
        List<Stats> allPlayerUnits,
        Vector3 aiBasePos,
        float laneLength,
        ScriptableStats extraCharacter = null,
        int extraCharacterCopies = 1)
    {
        float safeLaneLength = Mathf.Max(1f, laneLength);
        List<CombatProfile> aiProfiles = new List<CombatProfile>();

        if (allAiUnits != null)
        {
            foreach (Stats unit in allAiUnits)
            {
                if (unit == null || unit._IsDead || unit.scriptableStats == null || unit._CurrentHealth <= 0f)
                    continue;

                float distToAiBase = Mathf.Abs(unit.transform.position.x - aiBasePos.x);
                float proximityRatio = 1f - Mathf.Clamp01(distToAiBase / safeLaneLength);
                float weight = Mathf.Lerp(0.35f, 1.5f, proximityRatio);

                aiProfiles.Add(BuildLiveProfile(unit, weight));
            }
        }

        if (extraCharacter != null && extraCharacterCopies > 0)
        {
            CombatProfile candidateProfile = BuildCandidateProfile(extraCharacter, threatWeight: 1.25f);
            for (int i = 0; i < extraCharacterCopies; i++)
                aiProfiles.Add(candidateProfile);
        }

        List<CombatProfile> playerProfiles = new List<CombatProfile>();
        if (allPlayerUnits != null)
        {
            foreach (Stats unit in allPlayerUnits)
            {
                if (unit == null || unit._IsDead || unit.scriptableStats == null || unit._CurrentHealth <= 0f)
                    continue;

                // Player units close to the AI base get up to 2.5x weight; units far away at Player base get 0.25x weight.
                float distToAiBase = Mathf.Abs(unit.transform.position.x - aiBasePos.x);
                float proximityRatio = 1f - Mathf.Clamp01(distToAiBase / safeLaneLength);
                float urgencyWeight = Mathf.Lerp(0.25f, 2.5f, proximityRatio * proximityRatio);

                playerProfiles.Add(BuildLiveProfile(unit, urgencyWeight));
            }
        }

        return EvaluateProfiles(aiProfiles, playerProfiles);
    }

    /// <summary>
    /// Pure-math entry point for matchup tests.
    /// Sign convention: Positive = Player favored, Negative = AI favored.
    /// Uses log-ratio scaling so adding a positive-strength AI unit is guaranteed to move
    /// the score strictly in the AI's favor (never suffers from the old unit-count multiplier inversion).
    /// </summary>
    public static float EvaluateProfiles(
        List<CombatProfile> aiTeam,
        List<CombatProfile> playerTeam)
    {
        if (aiTeam == null) aiTeam = new List<CombatProfile>();
        if (playerTeam == null) playerTeam = new List<CombatProfile>();

        float aiHp = TotalHp(aiTeam);
        float playerHp = TotalHp(playerTeam);

        if (aiHp <= 0f && playerHp <= 0f)
            return 0f;

        float aiStrength = CalculateArmyStrength(aiTeam, playerTeam);
        float playerStrength = CalculateArmyStrength(playerTeam, aiTeam);

        if (aiHp <= 0f)
            return Mathf.Max(1f, playerTeam.Count);

        if (playerHp <= 0f)
            return -Mathf.Max(1f, aiTeam.Count);

        if (aiStrength <= 0f && playerStrength <= 0f)
            return 0f;

        // Smooth, strictly monotonic combat advantage score:
        // Positive when playerStrength > aiStrength, Negative when aiStrength > playerStrength.
        // Adding any AI unit increases aiStrength (and decreases player per-unit focus),
        // which strictly decreases the returned value (more favorable for AI).
        float baselineFloor = 1f;
        float logRatio = Mathf.Log((playerStrength + baselineFloor) / (aiStrength + baselineFloor));
        float scaleFactor = Mathf.Sqrt(aiTeam.Count + playerTeam.Count);

        return logRatio * scaleFactor;
    }

    public static CombatProfile BuildCandidateProfile(
        ScriptableStats candidate,
        float threatWeight = 1f)
    {
        if (candidate == null)
            return new CombatProfile();

        PowerMath.UnitProfile profile = PowerMath.GetProfile(candidate);
        float utility = PowerMath.GetUtilityPower(candidate);

        return new CombatProfile(
            PowerMath.GetDPS(profile),
            profile.HP,
            profile.Range,
            profile.Move,
            profile.MaxTargets,
            utility,
            threatWeight);
    }

    static CombatProfile BuildLiveProfile(Stats unit, float threatWeight = 1f)
    {
        PowerMath.UnitProfile profile = PowerMath.GetProfile(unit);
        float currentHp = Mathf.Max(0f, unit._CurrentHealth);
        float utility = PowerMath.GetUtilityPower(unit);

        return new CombatProfile(
            PowerMath.GetDPS(profile),
            currentHp,
            profile.Range,
            profile.Move,
            profile.MaxTargets,
            utility,
            threatWeight);
    }

    static List<CombatProfile> BuildLiveProfiles(List<Stats> team)
    {
        List<CombatProfile> profiles = new List<CombatProfile>();
        if (team == null) return profiles;

        foreach (Stats unit in team)
        {
            if (unit == null || unit._IsDead || unit.scriptableStats == null)
                continue;

            float currentHp = Mathf.Max(0f, unit._CurrentHealth);
            if (currentHp <= 0f)
                continue;

            profiles.Add(BuildLiveProfile(unit, 1f));
        }

        return profiles;
    }

    static float CalculateArmyStrength(
        List<CombatProfile> attackers,
        List<CombatProfile> defenders)
    {
        if (attackers.Count == 0)
            return 0f;

        float totalAttackerHp = TotalHp(attackers);
        if (totalAttackerHp <= 0f)
            return 0f;

        int defenderCount = Mathf.Max(1, defenders.Count);
        float defenderHp = Mathf.Max(1f, TotalHp(defenders));
        float defenderDps = CalculateBaseDps(defenders, attackers.Count);

        float expectedFightDuration = Mathf.Clamp(
            defenderHp / Mathf.Max(defenderDps, MinimumDps),
            MinimumFightDuration,
            30f);

        float effectiveAttackerDps = 0f;
        int allyCountForSupport = Mathf.Max(0, attackers.Count - 1);

        foreach (CombatProfile attacker in attackers)
        {
            float targetCount = EffectiveTargetCount(attacker, defenderCount);

            float totalOpeningLead = 0f;
            if (defenders.Count > 0)
            {
                foreach (CombatProfile defender in defenders)
                {
                    totalOpeningLead += CalculateOpeningLead(
                        attacker,
                        defender.range,
                        defender.moveSpeed);
                }
                totalOpeningLead /= defenders.Count;
            }

            float rangeMultiplier = 1f + (totalOpeningLead / expectedFightDuration);
            float offensiveContribution = attacker.dps * targetCount * rangeMultiplier;

            // Friendly utility/buffs scale with how many teammates exist to receive the buff/heal.
            float supportMultiplier = allyCountForSupport > 0
                ? Mathf.Min(allyCountForSupport, 4) * 0.5f
                : 0.1f;
            float supportContribution = attacker.utilityDps * supportMultiplier;

            effectiveAttackerDps += (offensiveContribution + supportContribution) * attacker.threatWeight;
        }

        // Lanchester-style combat power with a small defensive durability baseline so 0-DPS walls still contribute EHP.
        return (effectiveAttackerDps * totalAttackerHp) + (totalAttackerHp * 0.1f);
    }

    static float CalculateBaseDps(
        List<CombatProfile> team,
        int opposingUnitCount)
    {
        float total = 0f;
        int safeOpposing = Mathf.Max(1, opposingUnitCount);

        foreach (CombatProfile unit in team)
        {
            total += unit.dps * EffectiveTargetCount(unit, safeOpposing) * unit.threatWeight;
        }

        return total;
    }

    static float EffectiveTargetCount(
        CombatProfile attacker,
        int opposingUnitCount)
    {
        if (opposingUnitCount <= 0 || attacker.maxTargets <= 0)
            return 0f;

        if (!attacker.IsAOE)
            return 1f;

        return Mathf.Min(opposingUnitCount, attacker.maxTargets);
    }

    static float CalculateOpeningLead(
        CombatProfile attacker,
        float defenderRange,
        float defenderSpeed)
    {
        float rangeAdvantage = attacker.range - defenderRange;
        if (rangeAdvantage <= 0f)
            return 0f;

        float closingSpeed = Mathf.Max(
            MinimumClosingSpeed,
            attacker.moveSpeed + defenderSpeed);

        return Mathf.Clamp(
            rangeAdvantage / closingSpeed,
            0f,
            MaxOpeningLeadSeconds);
    }

    static float TotalHp(List<CombatProfile> team)
    {
        float total = 0f;
        foreach (CombatProfile unit in team)
            total += Mathf.Max(0f, unit.hp) * unit.threatWeight;

        return total;
    }
}