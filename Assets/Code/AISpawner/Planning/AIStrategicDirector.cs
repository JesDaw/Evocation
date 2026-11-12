
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Automatic composition selection (legacy pairs, opt-in larger ordered plans).
///
/// Personality comes from AIClan, not this scene component.
/// Wave concentration continuously blends pressure-oriented
/// and concentrated-push composition preferences.
///
/// The scoring terms are provisional heuristics, not validated
/// combat-effectiveness estimates.
///
/// AILevelHarness and AITacticalPlanExecutor still own purchasing.
/// </summary>
[DisallowMultipleComponent]
public sealed class AIStrategicDirector : MonoBehaviour
{
    public enum OperationalGoal
    {
        BuildBreakthrough,
        MaintainPressure,
        StabilizeFront
    }

    [Header("Automatic Composition (Opt-In)")]
    [SerializeField]
    bool enableAutomaticComposition = false;

    [Tooltip(
        "Positive relative military advantage means the player is favored. " +
        "When this threshold is reached, prefer a defensive operation. " +
        "Requires zone-separated UnitTracker outcomes.")]
    [SerializeField, Range(0f, 1f)]
    float defensiveReactionThreshold = 0.35f;

    [Tooltip(
        "0 disables the combined-cost filter. " +
        "Each individual unit must still fit the wallet capacity.")]
    [SerializeField, Min(0f)]
    float maxCombinedPushCost = 0f;

    [Header("Variable-sized plans (opt-in)")]
    [Tooltip("OFF preserves the existing two-unit selector exactly. ON lets AIClan.waveConcentration choose 2 through Max Units Per Plan.")]
    [SerializeField] bool enableVariableSizedPlans = false;

    [Tooltip("Maximum ordered purchases in one plan. Larger plans are initially executed sequentially rather than forecast as one coordinated launch.")]
    [SerializeField, Range(2, 6)] int maxUnitsPerPlan = 4;

    [Header("Economic Plan Valuation (Opt-In)")]
    [Tooltip("OFF preserves the existing M2 selection scores. ON ranks plans by tactical merit relative to estimated resource commitment and saving delay. Does not change purchase execution.")]
    [SerializeField] bool enableEconomicPlanValuation = false;

    [Tooltip("How strongly a plan's total spending competes with its tactical merit. At 0 there is no additional cost-of-investment term. Economic Patience reduces this pressure, but never eliminates it.")]
    [SerializeField, Range(0f, 2f)] float investmentWeight = 0.65f;

    [Tooltip("How strongly estimated seconds until the entire plan is funded affect its selection. Economic Patience reduces this penalty. Does not prohibit saving for expensive units.")]
    [SerializeField, Range(0f, 2f)] float savingDelayWeight = 0.75f;

    [Tooltip("Time scale used to normalize the saving-delay penalty. Larger values make long waits less costly to the ranking.")]
    [SerializeField, Min(1f)] float referenceSavingSeconds = 20f;

    [Tooltip("Inspectable explanation of the latest selected plan's economic inputs. Read-only at runtime; no ScriptableStats are changed.")]
    [SerializeField, TextArea(2, 5)] string lastEconomicExplanation = "Economics not yet evaluated.";

    public string LastEconomicExplanation => lastEconomicExplanation;

    [Header("Initiative: Doctrine vs Battlefield Counterplay (Opt-In)")]
    [Tooltip("OFF retains the existing M3 tactical ranking. ON blends doctrine strength with ExpectedOutcomeCalculator's predicted improvement against the player's current units. AIClan.initiative 0 = reactive counterplay, 1 = proactive doctrine. Purchases and plan timing are unchanged.")]
    [SerializeField] bool enableInitiativeCounterplay = false;

    [Tooltip("Latest initiative diagnostic. Counterplay is evaluated using the existing, unweighted ExpectedOutcomeCalculator. Read-only at runtime.")]
    [SerializeField, TextArea(2, 6)] string lastInitiativeExplanation =
        "Initiative counterplay has not been evaluated.";

    public string LastInitiativeExplanation => lastInitiativeExplanation;

    [Header("Plan Ranking Diagnostics (Read-Only At Runtime)")]
    [Tooltip("Show the top three eligible foundation pairs under doctrine merit, counterplay improvement, and the actual final economic-adjusted score. Does not influence selection.")]
    [SerializeField] bool enableRankingDiagnostics = true;

    [Tooltip("Latest plan-selection ranking snapshot. The counterplay column is unavailable if no player units were found. Read-only during Play mode.")]
    [SerializeField, TextArea(8, 18)] string lastRankingReport =
        "No plan ranking captured yet.";

    [Tooltip("Also log the multi-line ranking report once whenever the director selects a new plan. Requires Log Choices.")]
    [SerializeField] bool logRankingReport = false;

    public string LastRankingReport => lastRankingReport;
    // Structured, copied at the last pair-selection decision. The executor captures it
    // exactly once when the selected plan is accepted. Never used for ranking.
    public readonly List<AIDecisionTrace.AIPlanCandidateSnapshot> LastPairSnapshots =
        new List<AIDecisionTrace.AIPlanCandidateSnapshot>();
    public bool LastCounterplayActive { get; private set; }
    public int LastCounterplayZoneCount { get; private set; }
    public bool EconomicValuationEnabled => enableEconomicPlanValuation;
    public bool InitiativeCounterplayEnabled => enableInitiativeCounterplay;

    [SerializeField]
    bool logChoices = true;

    public bool AutoCompositionEnabled =>
        isActiveAndEnabled && enableAutomaticComposition;

    public OperationalGoal LastGoal { get; private set; }

    public float LastWaveConcentration { get; private set; }

    public string LastReason { get; private set; } =
        "No automatic plan selected.";

    public ScriptableStats LastFirst { get; private set; }

    public ScriptableStats LastSecond { get; private set; }

    public int LastEvaluatedPairs { get; private set; }

    public bool HasEligibleRoster(ScriptableStats[] roster)
    {
        if (!AutoCompositionEnabled || roster == null)
            return false;

        for (int i = 0; i < roster.Length; i++)
        {
            if (IsEligible(roster[i]))
                return true;
        }

        return false;
    }

    // Compatibility for existing callers that request an ordered pair.
    // New execution uses TryCreatePlan instead.
    public bool TryChoosePlan(
        AILevelHarness harness,
        ScriptableStats[] roster,
        out ScriptableStats first,
        out ScriptableStats second)
    {
        return TryChoosePlan(harness, roster, false, 0f, out first, out second);
    }

    public bool TryCreatePlan(
        AILevelHarness harness,
        ScriptableStats[] roster,
        bool incomeAwareLaunch,
        float deploymentGapSeconds,
        out AITacticalPlan plan)
    {
        plan = null;
        // Multi-unit plans use the sequential executor. Do not filter the
        // foundation pair by a two-unit coordinated cooldown window.
        int requestedCount = enableVariableSizedPlans && harness != null && harness.aiClan != null
            ? Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(2f, Mathf.Max(2, maxUnitsPerPlan),
                Mathf.Clamp01(harness.aiClan.waveConcentration))),
                2, Mathf.Max(2, maxUnitsPerPlan))
            : 2;
        ScriptableStats first, second;
        if (!TryChoosePlan(harness, roster,
            incomeAwareLaunch && requestedCount == 2,
            deploymentGapSeconds, out first, out second))
            return false;

        var chosen = new List<ScriptableStats> { first, second };
        // M2's extension heuristic is still provisional. When economic valuation
        // is enabled, evaluate its incremental cost against expected saving time.
        float initialPlanCost = first._spawnCost + second._spawnCost;
        if (enableVariableSizedPlans)
        {
            int count = requestedCount;

            // The tested pair selection remains the foundation of every plan.
            // Additional purchases favor affordable pressure at low concentration
            // and ranged/damaging support at high concentration. This is provisional,
            // not a validated army-outcome model.
            float runningCost = first._spawnCost + second._spawnCost;
            var extensionSnapshots = enableInitiativeCounterplay
                ? CaptureCounterplayBattlefield() : null;
            for (int position = 2; position < count; position++)
            {
                ScriptableStats best = null;
                float bestScore = float.NegativeInfinity;
                var extensionScores = new Dictionary<ScriptableStats, float>();
                float maxExtensionGain = 0f;
                float prefixGain = extensionSnapshots != null && extensionSnapshots.Count > 0
                    ? CounterplayGain(extensionSnapshots, chosen) : 0f;
                if (extensionSnapshots != null && extensionSnapshots.Count > 0)
                {
                    foreach (ScriptableStats candidate in roster)
                    {
                        if (!IsEligible(candidate)) continue;
                        if (maxCombinedPushCost > 0f &&
                            runningCost + candidate._spawnCost > maxCombinedPushCost) continue;
                        var projected = new List<ScriptableStats>(chosen) { candidate };
                        float marginalGain = Mathf.Max(0f,
                            CounterplayGain(extensionSnapshots, projected) - prefixGain);
                        extensionScores[candidate] = marginalGain;
                        maxExtensionGain = Mathf.Max(maxExtensionGain, marginalGain);
                    }
                }
                foreach (ScriptableStats candidate in roster)
                {
                    if (!IsEligible(candidate)) continue;
                    if (maxCombinedPushCost > 0f &&
                        runningCost + candidate._spawnCost > maxCombinedPushCost)
                        continue;

                    // Avoid plan padding solely with a repeated expensive unit.
                    float repetition = 0f;
                    foreach (ScriptableStats existing in chosen)
                        if (existing == candidate) repetition += 1f;

                    float relativeCost = candidate._spawnCost /
                        Mathf.Max(1f, MaxCost(chosen));
                    float score = Mathf.Lerp(
                        candidate._MoveSpeed * 0.1f - relativeCost * 0.4f,
                        candidate._HorizontalRange * 0.12f +
                        candidate._AttackDamage * 0.012f - relativeCost * 0.15f,
                        LastWaveConcentration) - repetition * 0.12f;

                    if (enableInitiativeCounterplay && maxExtensionGain > 0.00001f &&
                        extensionScores.TryGetValue(candidate, out float marginal))
                    {
                        // Extension heuristic is not naturally normalized. Use
                        // a bounded 0..1 transform before combining with the
                        // normalized counter benefit; keep repetition separate.
                        float doctrineNormalized = Mathf.Clamp01(score + repetition * 0.12f);
                        float counterNormalized = marginal / maxExtensionGain;
                        score = Mathf.Lerp(counterNormalized, doctrineNormalized,
                            Mathf.Clamp01(harness.aiClan.initiative)) - repetition * 0.12f;
                    }

                    if (enableEconomicPlanValuation)
                    {
                        float candidateCost = runningCost + candidate._spawnCost;
                        float previousPenalty = GetEconomicDenominator(harness, runningCost,
                            initialPlanCost, harness.aiClan.economicPatience);
                        float nextPenalty = GetEconomicDenominator(harness, candidateCost,
                            initialPlanCost, harness.aiClan.economicPatience);
                        // Penalize extra commitment instead of replacing the M2 heuristic.
                        // The comparison is incremental, so existing prefix cost cancels.
                        score -= Mathf.Max(0f, nextPenalty - previousPenalty);
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = candidate;
                    }
                }
                if (best == null) break;
                chosen.Add(best);
                runningCost += best._spawnCost;
            }
        }

        plan = new AITacticalPlan(chosen, LastGoal.ToString());
        if (chosen.Count > 2)
        {
            LastReason += " Extended to " + chosen.Count +
                " ordered purchases (sequential execution after the pair).";
            if (logChoices) Debug.Log("[AI Strategic Director] " + LastReason, this);
        }
        return true;
    }

    public bool TryChoosePlan(
        AILevelHarness harness,
        ScriptableStats[] roster,
        bool incomeAwareLaunch,
        float deploymentGapSeconds,
        out ScriptableStats first,
        out ScriptableStats second)
    {
        first = null;
        second = null;

        LastEvaluatedPairs = 0;
        LastFirst = null;
        LastSecond = null;
        lastInitiativeExplanation = "No counterplay evaluation for this selection.";
        lastRankingReport = "No eligible pairs evaluated for this selection.";
        LastPairSnapshots.Clear();
        LastCounterplayActive = false;
        LastCounterplayZoneCount = 0;

        if (!AutoCompositionEnabled ||
            harness == null ||
            harness.aiClan == null ||
            roster == null)
        {
            LastReason =
                "Automatic composition disabled or required context missing.";

            return false;
        }

        AIClan clan = harness.aiClan;

        // Personality is owned by the AIClan ScriptableObject.
        float waveConcentration =
            Mathf.Clamp01(clan.waveConcentration);

        LastWaveConcentration = waveConcentration;

        var units = new List<ScriptableStats>();

        foreach (ScriptableStats unit in roster)
        {
            if (IsEligible(unit) && !units.Contains(unit))
                units.Add(unit);
        }

        if (units.Count == 0)
        {
            LastReason =
                "No valid units in the current sequential level phase.";

            return false;
        }

        // Existing defensive reaction signal.
        // This remains separate from the AI's spending preferences.
        bool hasThreatSignal =
            UnitTracker.Instance != null &&
            UnitTracker.Instance.ZoneSeparatedOutcomesEnabled;

        float relativePlayerAdvantage = 0f;

        if (hasThreatSignal)
        {
            relativePlayerAdvantage =
                UnitTracker.Instance
                    .CalculateRelativeMilitaryAdvantage();
        }

        bool defensiveOperation =
            hasThreatSignal &&
            relativePlayerAdvantage >= defensiveReactionThreshold;

        // This label is diagnostic only for normal operations.
        // Normal tactical scores blend continuously below.
        LastGoal = defensiveOperation
            ? OperationalGoal.StabilizeFront
            : waveConcentration >= 0.5f
                ? OperationalGoal.BuildBreakthrough
                : OperationalGoal.MaintainPressure;

        float maxHealth = 1f;
        float maxDamage = 1f;
        float maxSpeed = 1f;
        float maxRange = 1f;

        foreach (ScriptableStats unit in units)
        {
            maxHealth =
                Mathf.Max(maxHealth, unit._MaxHealth);

            maxDamage =
                Mathf.Max(maxDamage, unit._AttackDamage);

            maxSpeed =
                Mathf.Max(maxSpeed, unit._MoveSpeed);

            maxRange =
                Mathf.Max(maxRange, unit._HorizontalRange);
        }

        float bestScore = float.NegativeInfinity;
        float cheapestTie = float.PositiveInfinity;
        float selectedMerit = 0f;
        float runnerUpScore = float.NegativeInfinity;
        float selectedInvestment = 0f;
        float selectedWait = 0f;

        var counterplaySnapshots = enableInitiativeCounterplay
            ? CaptureCounterplayBattlefield() : null;
        bool hasPlayerBattlefield = counterplaySnapshots != null && counterplaySnapshots.Count > 0;
        float initiative = Mathf.Clamp01(clan.initiative);
        var evaluations = new List<PairEvaluation>();
        float maxCounterGain = 0f;

        foreach (ScriptableStats a in units)
        {
            foreach (ScriptableStats b in units)
            {
                float totalCost =
                    a._spawnCost + b._spawnCost;

                if (maxCombinedPushCost > 0f &&
                    totalCost > maxCombinedPushCost)
                {
                    continue;
                }

                // The same character twice may require a cooldown
                // longer than the income-aware deployment window.
                if (a == b &&
                    incomeAwareLaunch &&
                    a._spawnCooldown > deploymentGapSeconds)
                {
                    continue;
                }

                LastEvaluatedPairs++;

                float costDenominator =
                    maxCombinedPushCost > 0f
                        ? maxCombinedPushCost
                        : 2f * MaxCost(units);

                float normalizedCost =
                    totalCost / Mathf.Max(1f, costDenominator);

                float normalizedSpeed =
                    (a._MoveSpeed + b._MoveSpeed) /
                    (2f * maxSpeed);

                float normalizedDamage =
                    (a._AttackDamage + b._AttackDamage) /
                    (2f * maxDamage);

                // Low wave concentration:
                // prefer cheaper, faster pressure.
                float pressureScore =
                    0.45f * normalizedSpeed +
                    0.35f * (1f - normalizedCost) +
                    0.20f * normalizedDamage;

                // High wave concentration:
                // prefer a durable first unit and a
                // strong, long-ranged follow-up.
                float breakthroughScore =
                    0.40f * a._MaxHealth / maxHealth +
                    0.35f * b._HorizontalRange / maxRange +
                    0.15f * b._AttackDamage / maxDamage +
                    0.10f * (1f - normalizedCost);

                // Defensive operation:
                // preserve the previous defensive heuristic.
                float defenseScore =
                    0.50f * a._MaxHealth / maxHealth +
                    0.25f * b._AttackDamage / maxDamage +
                    0.25f * (1f - normalizedCost);

                // No discrete doctrine switch.
                // Every value between 0 and 1 is meaningful.
                float normalScore = Mathf.Lerp(
                    pressureScore,
                    breakthroughScore,
                    waveConcentration);

                float merit =
                    defensiveOperation
                        ? defenseScore
                        : normalScore;
                float gain = hasPlayerBattlefield
                    ? CounterplayGain(counterplaySnapshots, new ScriptableStats[] { a, b }) : 0f;
                maxCounterGain = Mathf.Max(maxCounterGain, Mathf.Max(0f, gain));
                evaluations.Add(new PairEvaluation
                {
                    first = a, second = b, cost = totalCost,
                    merit = merit, gain = gain
                });
            }
        }

        bool activeCounterplay = enableInitiativeCounterplay &&
            hasPlayerBattlefield && maxCounterGain > 0.00001f;
        float selectedGain = 0f;
        float selectedCounterScore = 0f;
        float selectedBlendedMerit = 0f;
        foreach (PairEvaluation entry in evaluations)
        {
            // Normalize across the same eligible pairs, so the counter score
            // remains on approximately the same 0..1 scale as doctrine merit.
            float counterScore = activeCounterplay
                ? Mathf.Clamp01(Mathf.Max(0f, entry.gain) / maxCounterGain) : entry.merit;
            float blendedMerit = activeCounterplay
                ? Mathf.Lerp(counterScore, entry.merit, initiative) : entry.merit;
            float score = blendedMerit;
            if (enableEconomicPlanValuation)
                score = blendedMerit / GetEconomicDenominator(harness, entry.cost,
                    2f * MaxCost(units), clan.economicPatience);
            entry.counterScore = counterScore;
            entry.blendedMerit = blendedMerit;
            entry.finalScore = score;
            float totalCost = entry.cost;
            ScriptableStats a = entry.first;
            ScriptableStats b = entry.second;

            // Stable, deterministic tie-breaking.
            if (score > bestScore + 0.00001f ||
                    (Mathf.Abs(score - bestScore) <= 0.00001f &&
                     totalCost < cheapestTie))
                {
                    runnerUpScore = bestScore;
                    bestScore = score;
                    cheapestTie = totalCost;
                    selectedMerit = entry.merit;
                    selectedBlendedMerit = blendedMerit;
                    selectedGain = entry.gain;
                    selectedCounterScore = counterScore;
                    selectedInvestment = totalCost;
                    selectedWait = EstimateSavingSeconds(harness, totalCost);

                    first = a;
                    second = b;
                }
                else if (score > runnerUpScore)
                    runnerUpScore = score;
        }

        // Persist every eligible pair, not only the top three shown in the Inspector.
        // This does not reorder or mutate the authoritative evaluation list.
        LastCounterplayActive = activeCounterplay;
        LastCounterplayZoneCount = counterplaySnapshots != null ? counterplaySnapshots.Count : 0;
        foreach (PairEvaluation entry in evaluations)
            LastPairSnapshots.Add(new AIDecisionTrace.AIPlanCandidateSnapshot
            {
                first = entry.first.name, second = entry.second.name,
                cost = entry.cost, doctrine = entry.merit,
                counterGain = entry.gain, counterNormalized = entry.counterScore,
                blended = entry.blendedMerit, finalScore = entry.finalScore,
                selected = entry.first == first && entry.second == second
            });
        AssignDiagnosticRanks(LastPairSnapshots);

        lastInitiativeExplanation = !enableInitiativeCounterplay
            ? "OFF: M3 doctrine ranking unchanged."
            : !activeCounterplay
                ? "No eligible live player units / meaningful counterplay gain; doctrine ranking used."
                : string.Format(
                    "initiative {0:0.00} (0 counter, 1 doctrine); selected doctrine {1:0.000}; " +
                    "counter {2:0.000} (gain {3:0.000}, best gain {4:0.000}); " +
                    "blended {5:0.000}; final economic score {6:0.000}; zones evaluated {7}. " +
                    "Unweighted calculator estimate; no status-effect simulation or path forecasting.",
                    initiative, selectedMerit, selectedCounterScore, selectedGain,
                    maxCounterGain, selectedBlendedMerit, bestScore, counterplaySnapshots.Count);

        if (enableRankingDiagnostics)
            lastRankingReport = BuildRankingReport(evaluations, first, second,
                initiative, enableInitiativeCounterplay, hasPlayerBattlefield,
                activeCounterplay, counterplaySnapshots != null ? counterplaySnapshots.Count : 0);
        else
            lastRankingReport = "Ranking diagnostics disabled.";

        if (first == null || second == null)
        {
            LastReason =
                "No two-purchase composition passed the cost " +
                "and cooldown restrictions.";

            return false;
        }

        LastFirst = first;
        LastSecond = second;
        lastEconomicExplanation = enableEconomicPlanValuation
            ? string.Format(
                "merit {0:0.000}, final score {1:0.000}, second-best score {2:0.000}; " +
                "pair cost {3:0.##}; estimated saving {4:0.0}s; patience {5:0.00}; " +
                "real money {6:0.##}; virtual budget {7:0.##}; effective income {8:0.##}/s. " +
                "Time is a ranking estimate, not an execution deadline.",
                selectedMerit, bestScore, runnerUpScore, selectedInvestment,
                selectedWait, Mathf.Clamp01(clan.economicPatience),
                Money.AIInstance != null ? Money.AIInstance.CurrentMoney : 0f,
                harness.VirtualBudget, GetEffectiveFundingRate(harness))
            : "Economic valuation OFF: original M2 plan scoring is unchanged.";

        LastReason = string.Format(
            "Wave concentration {0:0.00}; " +
            "operational goal {1}; " +
            "selected {2} -> {3}; " +
            "evaluated {4} ordered pairs; " +
            "heuristic score {5:0.000}; " +
            "relative player advantage {6:0.00}{7}. {8}",
            waveConcentration,
            LastGoal,
            first.name,
            second.name,
            LastEvaluatedPairs,
            bestScore,
            relativePlayerAdvantage,
            hasThreatSignal
                ? ""
                : " (threat signal unavailable)",
            lastEconomicExplanation + " " + lastInitiativeExplanation);

        if (logChoices)
        {
            Debug.Log(
                "[AI Strategic Director] " + LastReason,
                this);
        }
        if (logChoices && enableRankingDiagnostics && logRankingReport)
            Debug.Log("[AI Strategic Director Rankings]\n" + lastRankingReport, this);


        return true;
    }

    // Capture the battlefield only at plan selection. Nothing here updates a plan
    // already being saved for or changes the calculator's production coefficients.
    sealed class CounterplaySnapshot
    {
        public readonly List<ExpectedOutcomeCalculator.CombatProfile> ai =
            new List<ExpectedOutcomeCalculator.CombatProfile>();
        public readonly List<ExpectedOutcomeCalculator.CombatProfile> player =
            new List<ExpectedOutcomeCalculator.CombatProfile>();
        public float baseline;
    }

    List<CounterplaySnapshot> CaptureCounterplayBattlefield()
    {
        var snapshots = new List<CounterplaySnapshot>();
        UnitTracker tracker = UnitTracker.Instance;
        if (tracker == null || tracker.zones == null) return snapshots;

        var seen = new HashSet<Stats>();
        var seenZones = new HashSet<MapZone>();
        var global = new CounterplaySnapshot();

        foreach (MapZone zone in tracker.zones)
        {
            if (zone == null || !seenZones.Add(zone)) continue;
            CounterplaySnapshot current = tracker.ZoneSeparatedOutcomesEnabled
                ? new CounterplaySnapshot() : global;
            AddCounterplayUnits(zone.EnemyUnits, current.ai, seen);
            AddCounterplayUnits(zone.PlayerUnits, current.player, seen);
            AddCounterplayUnits(zone.AllyUnits, current.player, seen);
            if (tracker.ZoneSeparatedOutcomesEnabled && current.player.Count > 0)
                snapshots.Add(current);
        }

        if (!tracker.ZoneSeparatedOutcomesEnabled && global.player.Count > 0)
            snapshots.Add(global);

        foreach (CounterplaySnapshot snapshot in snapshots)
            snapshot.baseline = ExpectedOutcomeCalculator.EvaluateProfiles(
                snapshot.ai, snapshot.player);
        return snapshots;
    }

    static void AddCounterplayUnits(List<GameObject> objects,
        List<ExpectedOutcomeCalculator.CombatProfile> destination, HashSet<Stats> seen)
    {
        if (objects == null) return;
        foreach (GameObject obj in objects)
        {
            if (obj == null) continue;
            Stats unit = obj.GetComponent<Stats>();
            if (unit == null || unit._IsDead || unit._CurrentHealth <= 0f ||
                unit.scriptableStats == null || !seen.Add(unit)) continue;
            // The calculator's live-profile builder is intentionally reused via
            // CalculateExpectedOutcome elsewhere, but BuildCandidateProfile is
            // not live-health aware. For battlefield snapshots we preserve live HP.
            PowerMath.UnitProfile profile = PowerMath.GetProfile(unit);
            destination.Add(new ExpectedOutcomeCalculator.CombatProfile(
                PowerMath.GetDPS(profile), unit._CurrentHealth, profile.Range,
                profile.Move, profile.MaxTargets, PowerMath.GetUtilityPower(unit)));
        }
    }

    // Positive benefit means the selected reinforcements improve the AI's
    // unweighted expected outcome. For multiple zones, average one plausible
    // placement at a time; do not clone reinforcements to every lane.
    float CounterplayGain(List<CounterplaySnapshot> snapshots,
        IList<ScriptableStats> planned)
    {
        if (snapshots == null || snapshots.Count == 0 || planned == null) return 0f;
        float total = 0f;
        foreach (CounterplaySnapshot snapshot in snapshots)
        {
            var projected = new List<ExpectedOutcomeCalculator.CombatProfile>(snapshot.ai);
            foreach (ScriptableStats unit in planned)
                if (unit != null)
                    projected.Add(ExpectedOutcomeCalculator.BuildCandidateProfile(unit));
            total += snapshot.baseline - ExpectedOutcomeCalculator.EvaluateProfiles(
                projected, snapshot.player);
        }
        return total / snapshots.Count;
    }

    static void AssignDiagnosticRanks(List<AIDecisionTrace.AIPlanCandidateSnapshot> candidates)
    {
        for (int dimension = 0; dimension < 3; dimension++)
        {
            int d = dimension;
            var sorted = new List<AIDecisionTrace.AIPlanCandidateSnapshot>(candidates);
            sorted.Sort((a, b) =>
            {
                float va = d == 0 ? a.doctrine : d == 1 ? a.counterNormalized : a.finalScore;
                float vb = d == 0 ? b.doctrine : d == 1 ? b.counterNormalized : b.finalScore;
                int cmp = vb.CompareTo(va);
                if (cmp != 0) return cmp;
                cmp = a.cost.CompareTo(b.cost);
                if (cmp != 0) return cmp;
                cmp = string.CompareOrdinal(a.first, b.first);
                return cmp != 0 ? cmp : string.CompareOrdinal(a.second, b.second);
            });
            for (int i = 0; i < sorted.Count; i++)
            {
                if (d == 0) sorted[i].doctrineRank = i + 1;
                else if (d == 1) sorted[i].counterRank = i + 1;
                else sorted[i].finalRank = i + 1;
            }
        }
    }

    sealed class PairEvaluation
    {
        public ScriptableStats first;
        public ScriptableStats second;
        public float cost;
        public float merit;
        public float gain;
        // Assigned during the existing decision loop. Diagnostics never affect ranking.
        public float counterScore;
        public float blendedMerit;
        public float finalScore;
    }

    string BuildRankingReport(List<PairEvaluation> evaluations,
        ScriptableStats selectedFirst, ScriptableStats selectedSecond,
        float initiative, bool counterplayEnabled, bool hasPlayerBattlefield,
        bool activeCounterplay, int zoneCount)
    {
        if (evaluations == null || evaluations.Count == 0)
            return "No eligible ordered pairs (check roster, wallet capacity and cooldown restrictions).";

        var sb = new System.Text.StringBuilder(1400);
        sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
            "Pairs {0}; initiative {1:0.00}; economics {2}; player snapshots {3}.\n",
            evaluations.Count, initiative, enableEconomicPlanValuation ? "ON" : "OFF", zoneCount);
        if (!counterplayEnabled)
            sb.AppendLine("Counterplay OFF: doctrine used for the final tactical score.");
        else if (!hasPlayerBattlefield)
            sb.AppendLine("No tracked living player/allied units: counterplay unavailable; doctrine fallback.");
        else if (!activeCounterplay)
            sb.AppendLine("Tracked opposing units but no positive counterplay gain: doctrine fallback.");
        else
            sb.AppendLine("Counterplay available: score is positive improvement normalized within eligible pairs.");

        AppendTopPairs(sb, "DOCTRINE (raw merit)", evaluations,
            e => e.merit, selectedFirst, selectedSecond);
        if (activeCounterplay)
            AppendTopPairs(sb, "COUNTERPLAY (normalized gain)", evaluations,
                e => e.counterScore, selectedFirst, selectedSecond);
        else
            sb.AppendLine("COUNTERPLAY: not ranked (no usable opponent comparison).");
        AppendTopPairs(sb, "FINAL (after initiative + economics)", evaluations,
            e => e.finalScore, selectedFirst, selectedSecond);
        sb.AppendLine("Rankings show foundation pairs only; later plan extensions are not included.");
        return sb.ToString();
    }

    static void AppendTopPairs(System.Text.StringBuilder sb, string title,
        List<PairEvaluation> evaluations, System.Func<PairEvaluation, float> scoreOf,
        ScriptableStats selectedFirst, ScriptableStats selectedSecond)
    {
        // Sort a copy only. The authoritative evaluated order and selection stay untouched.
        var sorted = new List<PairEvaluation>(evaluations);
        sorted.Sort((a, b) =>
        {
            int byScore = scoreOf(b).CompareTo(scoreOf(a));
            if (byScore != 0) return byScore;
            int byCost = a.cost.CompareTo(b.cost);
            if (byCost != 0) return byCost;
            int byFirst = string.CompareOrdinal(a.first.name, b.first.name);
            return byFirst != 0 ? byFirst : string.CompareOrdinal(a.second.name, b.second.name);
        });
        sb.AppendLine(title);
        int show = Mathf.Min(3, sorted.Count);
        for (int i = 0; i < show; i++)
        {
            PairEvaluation e = sorted[i];
            bool selected = e.first == selectedFirst && e.second == selectedSecond;
            sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
                "  {0}. {1} -> {2}{3} | rankScore {4:0.000} | cost {5:0.#} | " +
                "doctrine {6:0.000} | counter {7:0.000} | gain {8:0.000} | final {9:0.000}\n",
                i + 1, e.first.name, e.second.name, selected ? " [SELECTED]" : "",
                scoreOf(e), e.cost, e.merit, e.counterScore, e.gain, e.finalScore);
        }
    }

    // A rough funding opportunity-cost estimate, NOT a simulation of the
    // execution scheduler. The virtual budget is capped by actual money, so
    // the slower of real and activity-limited income constrains purchases.
    float GetEffectiveFundingRate(AILevelHarness harness)
    {
        Money money = Money.AIInstance;
        if (money == null || harness == null || !money.MoneyIsActive) return 0f;
        float actualRate = Mathf.Max(0f, money.MoneyGainPerSec);
        float activityRate = actualRate * Mathf.Max(0f, harness.CurrentActivityBudgetPercent);
        return Mathf.Min(actualRate, activityRate);
    }

    float EstimateSavingSeconds(AILevelHarness harness, float totalCost)
    {
        if (harness == null) return 0f;
        Money money = Money.AIInstance;
        if (money == null) return 0f;
        // Sequential purchases can fund totals above wallet capacity: we
        // deliberately do not reject these plans or clamp total cost.
        float fundingAtStart = Mathf.Min(
            Mathf.Max(0f, harness.VirtualBudget),
            Mathf.Max(0f, money.CurrentMoney));
        float shortfall = Mathf.Max(0f, totalCost - fundingAtStart);
        float rate = GetEffectiveFundingRate(harness);
        // Avoid division by zero: dormant income is a strong *ranking* cost,
        // not grounds to cancel or veto a selected unit.
        return rate > 0.001f ? shortfall / rate : (shortfall > 0f ? 600f : 0f);
    }

    float GetEconomicDenominator(AILevelHarness harness, float totalCost,
        float referenceCost, float patience)
    {
        float p = Mathf.Clamp01(patience);
        // Patient clans still care about opportunity cost, but discount it.
        float sensitivity = Mathf.Lerp(1.5f, 0.35f, p);
        float normalizedInvestment = Mathf.Max(0f, totalCost) /
            Mathf.Max(1f, referenceCost);
        float normalizedWait = Mathf.Min(10f,
            EstimateSavingSeconds(harness, totalCost) / Mathf.Max(1f, referenceSavingSeconds));
        return 1f + sensitivity * (
            Mathf.Max(0f, investmentWeight) * normalizedInvestment +
            Mathf.Max(0f, savingDelayWeight) * normalizedWait);
    }

    static bool IsEligible(ScriptableStats unit)
    {
        if (unit == null ||
            float.IsNaN(unit._spawnCost) ||
            float.IsInfinity(unit._spawnCost) ||
            unit._spawnCost < 0f ||
            float.IsNaN(unit._spawnCooldown) ||
            float.IsInfinity(unit._spawnCooldown) ||
            unit._spawnCooldown < 0f)
        {
            return false;
        }

        Money money = Money.AIInstance;

        return money == null ||
               unit._spawnCost <= money.CurrentMoneyCapacity;
    }

    static float MaxCost(List<ScriptableStats> units)
    {
        float maximum = 1f;

        foreach (ScriptableStats unit in units)
        {
            maximum = Mathf.Max(
                maximum,
                unit._spawnCost);
        }

        return maximum;
    }
}
