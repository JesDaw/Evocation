using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExpectedOutcomeCalculator
{
    struct TeamState
    {
        public float normalCount;
        public float aoeCount;
        public float avgNormalDps;
        public float avgAoeDps;
        public float avgNormalHp;
        public float avgAoeHp;

        public readonly float TotalCount => normalCount + aoeCount;
    }

    /// <summary>
    /// Returns positive float for Player win, negative float for AI win, and 0 for a tie.
    /// </summary>
    public static float CalculateExpectedOutcome(List<Stats> aiTeam, List<Stats> playerTeam, ScriptableStats extraCharacter = null)
    {
        TeamState aiState = ExtractTeamData(aiTeam, extraCharacter);
        TeamState playerState = ExtractTeamData(playerTeam);

        SimulateBattle(ref aiState, ref playerState);

        return ComputeOutcomeScore(aiState, playerState);
    }
    static TeamState ExtractTeamData(List<Stats> team, ScriptableStats extraCharacter = null)
    {
        TeamState state = new TeamState();
        if (team == null || team.Count == 0) return state;

        float normalDpsSum = 0f;
        float normalHpSum = 0f;
        float aoeDpsSum = 0f;
        float aoeHpSum = 0f;

        foreach (var unit in team)
        {
            if (unit == null || unit._IsDead) continue;

            var profile = PowerMath.GetProfile(unit);
            float unitDps = PowerMath.GetDPS(profile);
            float unitHp = Mathf.Max(1f, profile.HP);

            if (unit._IsAOE)
            {
                state.aoeCount++;
                aoeDpsSum += unitDps;
                aoeHpSum += unitHp;
            }
            else
            {
                state.normalCount++;
                normalDpsSum += unitDps;
                normalHpSum += unitHp;
            }
        }

        if (extraCharacter != null)
        {
            var profile = PowerMath.GetProfile(extraCharacter);
            float unitDps = PowerMath.GetDPS(profile);
            float unitHp = Mathf.Max(1f, profile.HP);

            if (PowerMath.IsAOE(extraCharacter.combatActions)) 
            {
                state.aoeCount++;
                aoeDpsSum += unitDps;
                aoeHpSum += unitHp;
            }
            else
            {
                state.normalCount++;
                normalDpsSum += unitDps;
                normalHpSum += unitHp;
            }
        }

        if (state.normalCount > 0)
        {
            state.avgNormalDps = normalDpsSum / state.normalCount;
            state.avgNormalHp = normalHpSum / state.normalCount;
        }

        if (state.aoeCount > 0)
        {
            state.avgAoeDps = aoeDpsSum / state.aoeCount;
            state.avgAoeHp = aoeHpSum / state.aoeCount;
        }

        return state;
    }

    static void SimulateBattle(ref TeamState ai, ref TeamState player, float timeStep = 0.01f, int maxTicks = 100000)
    {
        int tick = 0;
        while (ai.TotalCount > 0f && player.TotalCount > 0f && tick < maxTicks)
        {
            float aiDamage = CalculateTeamDamage(ai, player.TotalCount) * timeStep;
            float playerDamage = CalculateTeamDamage(player, ai.TotalCount) * timeStep;

            ApplyDamage(ref player, aiDamage);
            ApplyDamage(ref ai, playerDamage);

            tick++;
        }
    }

    static float CalculateTeamDamage(TeamState attacker, float targetCount)
    {
        float singleTargetDps = attacker.normalCount * attacker.avgNormalDps;
        // AOE units hit the opponent's entire remaining headcount — intentional,
        // now that maxTargets is being set to unlimited across the board.
        float aoeDps = attacker.aoeCount * attacker.avgAoeDps * targetCount;
        return singleTargetDps + aoeDps;
    }

    static void ApplyDamage(ref TeamState defender, float incomingDamage)
    {
        float totalUnits = defender.TotalCount;
        if (totalUnits <= 0f) return;

        float normalRatio = defender.normalCount / totalUnits;
        float aoeRatio = defender.aoeCount / totalUnits;

        float normalDamage = incomingDamage * normalRatio;
        float aoeDamage = incomingDamage * aoeRatio;

        if (defender.avgNormalHp > 0f)
            defender.normalCount -= normalDamage / defender.avgNormalHp;
        if (defender.avgAoeHp > 0f)
            defender.aoeCount -= aoeDamage / defender.avgAoeHp;

        if (defender.normalCount < 0f) defender.normalCount = 0f;
        if (defender.aoeCount < 0f) defender.aoeCount = 0f;
    }

    static float ComputeOutcomeScore(TeamState ai, TeamState player)
    {
        float aiRemaining = ai.TotalCount;
        float playerRemaining = player.TotalCount;

        if (playerRemaining > 0f && aiRemaining <= 0f) return playerRemaining;
        if (aiRemaining > 0f && playerRemaining <= 0f) return -aiRemaining;
        return 0f;
    }
}