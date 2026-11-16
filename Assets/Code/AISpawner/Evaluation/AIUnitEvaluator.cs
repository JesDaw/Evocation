
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AIDecisionTrace;

/// <summary>
/// Evaluates available AI units without purchasing or spawning them.
/// Preserves AILevelHarness's existing four-term utility calculation.
/// </summary>
public sealed class AIUnitEvaluator
{
    readonly CharacterCooldownHandler cooldownHandler;
    readonly MapZone aiSpawnZone;
    readonly float outcomeUtilityScale;
    readonly float swarmEvaluationWindow;
    readonly int maxSwarmCopies;
    readonly bool showDebugLogs;

    public AIUnitEvaluator(
        CharacterCooldownHandler cooldownHandler,
        MapZone aiSpawnZone,
        float outcomeUtilityScale,
        float swarmEvaluationWindow,
        int maxSwarmCopies,
        bool showDebugLogs)
    {
        this.cooldownHandler = cooldownHandler;
        this.aiSpawnZone = aiSpawnZone;
        this.outcomeUtilityScale = outcomeUtilityScale;
        this.swarmEvaluationWindow = swarmEvaluationWindow;
        this.maxSwarmCopies = maxSwarmCopies;
        this.showDebugLogs = showDebugLogs;
    }

    public CharacterUtility SelectBestCandidate(
        ScriptableStats[] characters,
        AIPassSnapshot pass,
        float moneyBudget)
    {
        List<CharacterUtility> characterUtilities =
            new List<CharacterUtility>();

        float currentOutcome =
            UnitTracker.Instance.CalculateOverallExpectedOutcome(
                AIWeighted: true);

        if (pass != null)
            pass.baselineOutcome = currentOutcome;

        float maxAbsOutcomeDelta = 0f;

        // Pass 1: Raw outcome deltas and non-outcome utilities.
        foreach (ScriptableStats character in characters)
        {
            if (character == null)
            {
                if (pass != null)
                    pass.nullCharacterEntries++;

                continue;
            }

            float cost = character._spawnCost;
            float cooldown = character._spawnCooldown;

            if (float.IsNaN(cost) || float.IsInfinity(cost) ||
                float.IsNaN(cooldown) || float.IsInfinity(cooldown) ||
                cost < 0f || cooldown < 0f)
            {
                if (showDebugLogs)
                    Debug.LogWarning(
                        $"[AI] Skipping invalid candidate: {character.name}");

                if (pass != null)
                    pass.skippedCandidates.Add(
                        $"{character.name} (cost={cost}, cooldown={cooldown})");

                continue;
            }

            var cu = new CharacterUtility
            {
                character = character
            };

            cu.moneyUtility = cost <= 0f
                ? 1f
                : Mathf.Clamp01(
                    (float)Money.AIInstance.CurrentMoney / cost);

            cu.spawnCooldownUtility = cooldown <= 0f
                ? 1f
                : Mathf.Clamp01(
                    1f - cooldownHandler.CooldownRemaining(character)
                    / cooldown);

            int swarmCopies =
                CalculateCandidateSwarmCopies(
                    cost, cooldown, moneyBudget);

            cu.evaluatedCopies = swarmCopies;

            float committedCost = cost * swarmCopies;

            cu.expendatureRateUtility = moneyBudget > 0f
                ? Mathf.Clamp01(committedCost / moneyBudget)
                : 0f;

            cu.overBudget = cost > moneyBudget;

            float outcomeWithCandidate =
                UnitTracker.Instance.CalculateOverallExpectedOutcome(
                    character,
                    relaventZone: aiSpawnZone,
                    AIWeighted: true,
                    extraCharacterCopies: swarmCopies);

            cu.expectedOutcomeRaw =
                currentOutcome - outcomeWithCandidate;

            float absDelta = Mathf.Abs(cu.expectedOutcomeRaw);

            if (absDelta > maxAbsOutcomeDelta)
                maxAbsOutcomeDelta = absDelta;

            // Preserve snapshot ordering for the existing inspector.
            if (pass != null)
            {
                pass.candidates.Add(new AICandidateSnapshot
                {
                    unitName = character.name,
                    unitAsset = character,
                    unit = AIDecisionRecorder.GetOrCreateUnit(character),
                    spawnCost = cost,
                    spawnCooldown = cooldown,
                    cooldownRemaining =
                        cooldownHandler.CooldownRemaining(character),
                    actualMoney = Money.AIInstance.CurrentMoney,
                    virtualBudget = moneyBudget,
                    affordableActual =
                        Money.AIInstance.CurrentMoney >= cost,
                    affordableVirtual = !cu.overBudget,
                    swarmCopies = swarmCopies,
                    committedCost = committedCost,
                    moneyUtility = cu.moneyUtility,
                    cooldownUtility = cu.spawnCooldownUtility,
                    expenditureUtility = cu.expendatureRateUtility,
                    baselineOutcome = currentOutcome,
                    outcomeWithCandidate = outcomeWithCandidate,
                    outcomeRaw = cu.expectedOutcomeRaw
                });
            }

            characterUtilities.Add(cu);
        }

        if (characterUtilities.Count == 0)
            return null;

        // Pass 2: Normalize relative to the largest outcome delta.
        float outcomeDenominator = Mathf.Max(
            maxAbsOutcomeDelta,
            outcomeUtilityScale,
            0.001f);

        foreach (CharacterUtility cu in characterUtilities)
        {
            cu.expectedOutcomeUtility = Mathf.Clamp(
                cu.expectedOutcomeRaw / outcomeDenominator,
                -1f,
                1f);

            cu.totalUtility = (
                cu.moneyUtility +
                cu.spawnCooldownUtility +
                cu.expendatureRateUtility +
                cu.expectedOutcomeUtility
            ) / 4f;

            if (showDebugLogs)
                cu.Print(moneyBudget);
        }

        // LINQ OrderByDescending is stable: equal scores preserve
        // original roster order, as in the existing harness.
        CharacterUtility best = characterUtilities
            .OrderByDescending(cu => cu.totalUtility)
            .First();

        if (pass != null)
        {
            pass.maxAbsOutcomeDelta = maxAbsOutcomeDelta;
            pass.outcomeDenominator = outcomeDenominator;

            for (int i = 0; i < characterUtilities.Count; i++)
            {
                CharacterUtility util = characterUtilities[i];
                AICandidateSnapshot snapshot = pass.candidates[i];

                snapshot.outcomeUtility = util.expectedOutcomeUtility;
                snapshot.totalUtility = util.totalUtility;
            }

            int bestIndex = characterUtilities.IndexOf(best);
            pass.selectedIndex = bestIndex;

            if (bestIndex >= 0)
                pass.candidates[bestIndex].isSelected = true;

            pass.AssignRanks();
        }

        return best;
    }

    int CalculateCandidateSwarmCopies(
        float cost,
        float cooldown,
        float moneyBudget)
    {
        if (cost <= 0f || moneyBudget < cost)
            return 1;

        int affordableCopies =
            Mathf.FloorToInt(moneyBudget / cost);

        int cooldownCopies = cooldown <= 0.05f
            ? maxSwarmCopies
            : 1 + Mathf.FloorToInt(
                Mathf.Max(0f, swarmEvaluationWindow) / cooldown);

        return Mathf.Clamp(
            Mathf.Min(affordableCopies, cooldownCopies),
            1,
            Mathf.Max(1, maxSwarmCopies));
    }
}
