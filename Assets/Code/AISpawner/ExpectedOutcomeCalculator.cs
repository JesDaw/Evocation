
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Estimates battle outcomes using a lightweight combat-strength model.
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

        public bool IsAOE => maxTargets != 1;

        public CombatProfile(
            float dps,
            float hp,
            float range,
            float moveSpeed,
            int maxTargets)
        {
            this.dps = Mathf.Max(0f, dps);
            this.hp = Mathf.Max(0f, hp);
            this.range = Mathf.Max(0f, range);
            this.moveSpeed = Mathf.Max(0f, moveSpeed);
            this.maxTargets = maxTargets <= 0
                ? int.MaxValue
                : maxTargets;
        }
    }

    public static float CalculateExpectedOutcome(
        List<Stats> aiTeam,
        List<Stats> playerTeam,
        ScriptableStats extraCharacter = null,
        int extraCharacterCopies = 1)
    {
        List<CombatProfile> aiProfiles = BuildLiveProfiles(aiTeam);

        if (extraCharacter != null)
        {
            CombatProfile candidateProfile =
                BuildCandidateProfile(extraCharacter);

            for (int i = 0; i < Mathf.Max(0, extraCharacterCopies); i++)
                aiProfiles.Add(candidateProfile);
        }

        List<CombatProfile> playerProfiles =
            BuildLiveProfiles(playerTeam);

        return EvaluateProfiles(aiProfiles, playerProfiles);
    }

    /// <summary>
    /// Pure-math entry point for matchup tests.
    /// </summary>
    public static float EvaluateProfiles(
        List<CombatProfile> aiTeam,
        List<CombatProfile> playerTeam)
    {
        if (aiTeam == null)
            aiTeam = new List<CombatProfile>();

        if (playerTeam == null)
            playerTeam = new List<CombatProfile>();

        float aiHp = TotalHp(aiTeam);
        float playerHp = TotalHp(playerTeam);

        if (aiHp <= 0f && playerHp <= 0f)
            return 0f;

        if (aiHp <= 0f)
            return playerTeam.Count;

        if (playerHp <= 0f)
            return -aiTeam.Count;

        float aiStrength =
            CalculateArmyStrength(aiTeam, playerTeam);

        float playerStrength =
            CalculateArmyStrength(playerTeam, aiTeam);

        float totalStrength = aiStrength + playerStrength;

        if (totalStrength <= 0f)
            return 0f;

        float relativeAdvantage =
            (playerStrength - aiStrength) / totalStrength;

        return relativeAdvantage *
               (aiTeam.Count + playerTeam.Count);
    }

    public static CombatProfile BuildCandidateProfile(
        ScriptableStats candidate)
    {
        if (candidate == null)
            return new CombatProfile();

        PowerMath.UnitProfile profile =
            PowerMath.GetProfile(candidate);

        return new CombatProfile(
            PowerMath.GetDPS(profile),
            profile.HP,
            profile.Range,
            profile.Move,
            profile.MaxTargets);
    }

    static List<CombatProfile> BuildLiveProfiles(List<Stats> team)
    {
        List<CombatProfile> profiles =
            new List<CombatProfile>();

        if (team == null)
            return profiles;

        foreach (Stats unit in team)
        {
            if (unit == null ||
                unit._IsDead ||
                unit.scriptableStats == null)
                continue;

            PowerMath.UnitProfile profile =
                PowerMath.GetProfile(unit);

            float currentHp = Mathf.Max(0f, unit._CurrentHealth);

            if (currentHp <= 0f)
                continue;

            profiles.Add(new CombatProfile(
                PowerMath.GetDPS(profile),
                currentHp,
                profile.Range,
                profile.Move,
                profile.MaxTargets));
        }

        return profiles;
    }

    static float CalculateArmyStrength(
        List<CombatProfile> attackers,
        List<CombatProfile> defenders)
    {
        if (attackers.Count == 0 || defenders.Count == 0)
            return 0f;

        float defenderHp = TotalHp(defenders);

        float defenderDps =
            CalculateBaseDps(defenders, attackers.Count);

        float expectedFightDuration =
            defenderHp / Mathf.Max(defenderDps, MinimumDps);

        expectedFightDuration =
            Mathf.Max(MinimumFightDuration, expectedFightDuration);

        float attackerDps = 0f;

        foreach (CombatProfile attacker in attackers)
        {
            float targetCount =
                EffectiveTargetCount(attacker, defenders.Count);

            // Evaluate range advantage against individual defenders
            // rather than relying on one average defender profile.
            float totalOpeningLead = 0f;

            foreach (CombatProfile defender in defenders)
            {
                totalOpeningLead += CalculateOpeningLead(
                    attacker,
                    defender.range,
                    defender.moveSpeed);
            }

            float averageOpeningLead =
                totalOpeningLead / defenders.Count;

            float rangeMultiplier =
                1f + averageOpeningLead / expectedFightDuration;

            attackerDps +=
                attacker.dps * targetCount * rangeMultiplier;
        }

        return attackerDps * TotalHp(attackers);
    }

    static float CalculateBaseDps(
        List<CombatProfile> team,
        int opposingUnitCount)
    {
        float total = 0f;

        foreach (CombatProfile unit in team)
        {
            total += unit.dps *
                     EffectiveTargetCount(unit, opposingUnitCount);
        }

        return total;
    }

    static float EffectiveTargetCount(
        CombatProfile attacker,
        int opposingUnitCount)
    {
        if (opposingUnitCount <= 0)
            return 0f;

        if (!attacker.IsAOE)
            return 1f;

        return Mathf.Min(
            opposingUnitCount,
            attacker.maxTargets);
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
            total += Mathf.Max(0f, unit.hp);

        return total;
    }
}
