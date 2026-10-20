using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Estimates the result of a battle using a lightweight Lanchester-inspired model.
/// Sign convention is kept compatible with the existing AI:
/// positive = Player/Allies favored, negative = AI/Enemy favored.
/// </summary>
public static class ExpectedOutcomeCalculator
{
    // A practical cap: range advantage matters, but shouldn't make a unit effectively
    // invulnerable just because its opponent is slow or stationary.
    const float MaxOpeningLeadSeconds = 3f;
    const float MinimumClosingSpeed = 0.25f;
    const float MinimumFightDuration = 1f;
    const float MinimumDps = 0.01f;

    /// <summary>
    /// Shared normalized profile used for both existing units and spawn candidates.
    /// maxTargets == int.MaxValue represents unlimited AOE targeting.
    /// </summary>
    public struct CombatProfile
    {
        public float dps;
        public float hp;
        public float range;
        public float moveSpeed;
        public int maxTargets;

        public bool IsAOE => maxTargets != 1;

        public CombatProfile(float dps, float hp, float range, float moveSpeed, int maxTargets)
        {
            this.dps = Mathf.Max(0f, dps);
            this.hp = Mathf.Max(0f, hp);
            this.range = Mathf.Max(0f, range);
            this.moveSpeed = Mathf.Max(0f, moveSpeed);
            this.maxTargets = maxTargets <= 0 ? int.MaxValue : maxTargets;
        }
    }

    /// <summary>
    /// Returns a signed estimate of the battle outcome. Positive favors the Player team;
    /// negative favors the AI team. The magnitude scales with army size and relative strength.
    /// </summary>
    public static float CalculateExpectedOutcome(List<Stats> aiTeam, List<Stats> playerTeam, ScriptableStats extraCharacter = null, int extraCharacterCopies = 1)
    {
        var aiProfiles = BuildLiveProfiles(aiTeam);
        if (extraCharacter != null)
        {
            CombatProfile candidateProfile = BuildCandidateProfile(extraCharacter);
            for (int i = 0; i < Mathf.Max(0, extraCharacterCopies); i++)
                aiProfiles.Add(candidateProfile);
        }

        var playerProfiles = BuildLiveProfiles(playerTeam);
        return EvaluateProfiles(aiProfiles, playerProfiles);
    }

    /// <summary>
    /// Public pure-math entry point, also useful for automated matchup tests.
    /// </summary>
    public static float EvaluateProfiles(List<CombatProfile> aiTeam, List<CombatProfile> playerTeam)
    {
        if (aiTeam == null) aiTeam = new List<CombatProfile>();
        if (playerTeam == null) playerTeam = new List<CombatProfile>();

        float aiHp = TotalHp(aiTeam);
        float playerHp = TotalHp(playerTeam);
        if (aiHp <= 0f && playerHp <= 0f) return 0f;
        if (aiHp <= 0f) return playerTeam.Count;
        if (playerHp <= 0f) return -aiTeam.Count;

        float aiStrength = CalculateArmyStrength(aiTeam, playerTeam);
        float playerStrength = CalculateArmyStrength(playerTeam, aiTeam);
        float totalStrength = aiStrength + playerStrength;
        if (totalStrength <= 0f) return 0f;

        // Keep the previous API's signed, survivor-like scale without running a long
        // frame-by-frame simulation. This also lets a swarm gain nonlinear value because
        // both its combined damage and combined durability grow with its headcount.
        float relativeAdvantage = (playerStrength - aiStrength) / totalStrength;
        return relativeAdvantage * (aiTeam.Count + playerTeam.Count);
    }

    public static CombatProfile BuildCandidateProfile(ScriptableStats candidate)
    {
        if (candidate == null) return new CombatProfile();

        PowerMath.UnitProfile profile = PowerMath.GetProfile(candidate);
        return new CombatProfile(
            PowerMath.GetDPS(profile),
            profile.HP,
            profile.Range,
            profile.Move,
            profile.MaxTargets);
    }

    static List<CombatProfile> BuildLiveProfiles(List<Stats> team)
    {
        var profiles = new List<CombatProfile>();
        if (team == null) return profiles;

        foreach (Stats unit in team)
        {
            if (unit == null || unit._IsDead || unit.scriptableStats == null) continue;

            PowerMath.UnitProfile profile = PowerMath.GetProfile(unit);
            float currentHp = unit._CurrentHealth > 0f ? unit._CurrentHealth : 0f;
            if (currentHp <= 0f) continue;

            // Use the same PowerMath profile fields as a candidate, but use current HP
            // for a live unit because damaged units should contribute less durability.
            profiles.Add(new CombatProfile(
                PowerMath.GetDPS(profile),
                currentHp,
                profile.Range,
                profile.Move,
                profile.MaxTargets));
        }

        return profiles;
    }

    static float CalculateArmyStrength(List<CombatProfile> attackers, List<CombatProfile> defenders)
    {
        if (attackers.Count == 0 || defenders.Count == 0) return 0f;

        float defenderHp = TotalHp(defenders);
        float defenderDps = CalculateBaseDps(defenders, attackers.Count);
        float expectedFightDuration = defenderHp / Mathf.Max(defenderDps, MinimumDps);
        expectedFightDuration = Mathf.Max(MinimumFightDuration, expectedFightDuration);

        float attackerDps = 0f;
        float averageDefenderRange = AverageRange(defenders);
        float averageDefenderSpeed = AverageSpeed(defenders);

        foreach (CombatProfile attacker in attackers)
        {
            float targetCount = EffectiveTargetCount(attacker, defenders.Count);
            float openingLead = CalculateOpeningLead(attacker, averageDefenderRange, averageDefenderSpeed);
            float rangeMultiplier = 1f + openingLead / expectedFightDuration;
            attackerDps += attacker.dps * targetCount * rangeMultiplier;
        }

        return attackerDps * TotalHp(attackers);
    }

    static float CalculateBaseDps(List<CombatProfile> team, int opposingUnitCount)
    {
        float total = 0f;
        foreach (CombatProfile unit in team)
            total += unit.dps * EffectiveTargetCount(unit, opposingUnitCount);
        return total;
    }

    static float EffectiveTargetCount(CombatProfile attacker, int opposingUnitCount)
    {
        if (!attacker.IsAOE) return 1f;
        if (opposingUnitCount <= 0) return 0f;
        return Mathf.Min(opposingUnitCount, attacker.maxTargets);
    }

    static float CalculateOpeningLead(CombatProfile attacker, float defenderRange, float defenderSpeed)
    {
        float rangeAdvantage = attacker.range - defenderRange;
        if (rangeAdvantage <= 0f) return 0f;

        // Both sides are assumed to close distance. This is deliberately an estimate,
        // not a replacement for the actual movement/combat systems.
        float closingSpeed = Mathf.Max(MinimumClosingSpeed, attacker.moveSpeed + defenderSpeed);
        return Mathf.Clamp(rangeAdvantage / closingSpeed, 0f, MaxOpeningLeadSeconds);
    }

    static float TotalHp(List<CombatProfile> team)
    {
        float total = 0f;
        foreach (CombatProfile unit in team) total += Mathf.Max(0f, unit.hp);
        return total;
    }

    static float AverageRange(List<CombatProfile> team)
    {
        if (team.Count == 0) return 0f;
        float total = 0f;
        foreach (CombatProfile unit in team) total += unit.range;
        return total / team.Count;
    }

    static float AverageSpeed(List<CombatProfile> team)
    {
        if (team.Count == 0) return 0f;
        float total = 0f;
        foreach (CombatProfile unit in team) total += unit.moveSpeed;
        return total / team.Count;
    }
}
