using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AIDecisionTrace
{
    // ------------------------------------------------------------------------------------
    // Plain data-transfer classes. They hold VALUES copied at decision time (never live
    // references to mutable AI state). The only Unity reference is CandidateSnapshot.unitAsset,
    // which points at a persistent ScriptableObject asset and exists purely so the Editor
    // window can ping it.
    // ------------------------------------------------------------------------------------

    public enum AIUpdateStatus
    {
        Ok,
        SkippedMissingDependency,
        SkippedInvalidConfig,
        SkippedMissingActivityCurve
    }

    public enum AIExecuteStatus
    {
        RanPasses,
        SkippedMissingDependency,
        SkippedInvalidConfig,
        NoCharactersInPhase
    }

    public enum AIPassOutcome
    {
        Spawned,
        SavingOverBudget,
        WaitingOnCooldown,
        NoValidCandidates,
        SpawnFailed
    }

    public enum AIActivityCurveKind
    {
        None,
        Advantage,
        Disadvantage
    }

    public enum AITickEnd
    {
        NotApplicable,
        PassEndedTick,      // a pass saved / waited / failed / found no candidates and returned
        BudgetExhausted,    // virtual budget reached 0 after a spawn
        MaxPassesReached    // loop ran to maxSpawnsPerTick
    }

    /// <summary>One candidate unit as evaluated inside one spawn pass.</summary>
    public sealed class AICandidateSnapshot
    {
        public string unitName;
        public UnityEngine.Object unitAsset;   // ping-only; may be null

        public float spawnCost;
        public float spawnCooldown;
        public float cooldownRemaining;

        public int actualMoney;                // Money.AIInstance.CurrentMoney when evaluated
        public float virtualBudget;            // moneyBudget when evaluated
        public bool affordableActual;          // actualMoney >= spawnCost
        public bool affordableVirtual;         // !CharacterUtility.overBudget (the AI's own test)

        public int swarmCopies;                // evaluatedCopies
        public float committedCost;            // spawnCost * swarmCopies

        public float moneyUtility;
        public float cooldownUtility;
        public float expenditureUtility;

        public float baselineOutcome;          // currentOutcome (tracker scale, +: player favored)
        public float outcomeWithCandidate;     // outcome after adding the candidate copies
        public float outcomeRaw;               // baseline - withCandidate (+: improves AI position)
        public float outcomeUtility;           // normalized, clamped to [-1, 1]

        public float totalUtility;
        public bool isSelected;
        public int rank;                       // 1 = highest total utility (ties keep array order)

        // ---- Stage 1.5 --------------------------------------------------------------------
        /// <summary>First-seen stats of this unit (shared object; null if the asset reference was missing).</summary>
        public AIUnitSnapshot unit;

        // Everything below is DIAGNOSTIC ONLY: derived from the fields above, never fed back to the AI.

        /// <summary>The AI's score is the plain average of four utilities.</summary>
        public const int UtilityTermCount = 4;

        public bool UsedMultipleCopies { get { return swarmCopies > 1; } }

        /// <summary>outcomeRaw / spawnCost. NOTE: outcomeRaw covers swarmCopies virtual copies, so for swarms prefer the swarm/per-copy variants. NaN when cost is 0 or less.</summary>
        public float ImprovementPerUnitCost { get { return spawnCost > 0f ? outcomeRaw / spawnCost : float.NaN; } }

        /// <summary>outcomeRaw / (spawnCost x copies): improvement per currency actually hypothesised. NaN when cost is 0 or less.</summary>
        public float ImprovementPerSwarmCost { get { return committedCost > 0f ? outcomeRaw / committedCost : float.NaN; } }

        /// <summary>outcomeRaw / copies: average improvement per virtual copy (the gain is not linear in copies).</summary>
        public float ImprovementPerCopy { get { return swarmCopies > 0 ? outcomeRaw / swarmCopies : float.NaN; } }

        public float ContribMoney { get { return moneyUtility / UtilityTermCount; } }
        public float ContribCooldown { get { return cooldownUtility / UtilityTermCount; } }
        public float ContribRate { get { return expenditureUtility / UtilityTermCount; } }
        public float ContribOutcome { get { return outcomeUtility / UtilityTermCount; } }

        /// <summary>Fraction of the total score contributed by a term (NaN when total is 0).</summary>
        public float ShareOfTotal(float contribution) { return totalUtility != 0f ? contribution / totalUtility : float.NaN; }

        public bool AffordableSwarmActual { get { return actualMoney >= committedCost; } }
        public bool AffordableSwarmVirtual { get { return committedCost <= virtualBudget; } }

        /// <summary>Outcome utility sits at the top of its [-1, 1] range.</summary>
        public bool OutcomeSaturated { get { return outcomeUtility >= 0.999f; } }
    }

    /// <summary>One iteration of the spawn-pass loop inside ExicuteBestAction.</summary>
    public sealed class AIPassSnapshot
    {
        public int passIndex;                  // 0-based

        public int actualMoneyBefore;
        public float virtualBudgetBefore;      // after the per-pass clamp

        public float baselineOutcome;
        public float maxAbsOutcomeDelta;
        public float outcomeDenominator;

        public readonly List<AICandidateSnapshot> candidates = new List<AICandidateSnapshot>(8);
        public readonly List<string> skippedCandidates = new List<string>();
        public int nullCharacterEntries;
        public int selectedIndex = -1;

        public AIPassOutcome outcome;
        public float cooldownRemainingAtDecision;
        public bool walletUpgradeAttempted;
        public bool walletUpgraded;
        public string spawnedObjectName;
        public bool spawnZoneNotified;

        public float virtualBudgetAfter;
        public int actualMoneyAfter;

        public AICandidateSnapshot Selected
        {
            get { return selectedIndex >= 0 && selectedIndex < candidates.Count ? candidates[selectedIndex] : null; }
        }

        public string Label
        {
            get
            {
                AICandidateSnapshot s = Selected;
                string n = s != null ? s.unitName : "?";
                switch (outcome)
                {
                    case AIPassOutcome.Spawned: return "Spawned " + n;
                    case AIPassOutcome.SavingOverBudget: return "Saving for " + n;
                    case AIPassOutcome.WaitingOnCooldown: return "Waiting on cooldown: " + n;
                    case AIPassOutcome.NoValidCandidates: return "No valid candidates";
                    case AIPassOutcome.SpawnFailed: return "Spawn FAILED: " + n;
                    default: return outcome.ToString();
                }
            }
        }

        // ---- Stage 1.5 diagnostic-only helpers ---------------------------------------------

        /// <summary>
        /// Which term set the outcome denominator: "MaxDelta" (largest |raw delta| in the pass),
        /// "ScaleFloor" (outcomeUtilityScale was larger) or "Epsilon" (both tiny). Derived from stored values.
        /// </summary>
        public string DenominatorSource(float outcomeUtilityScale)
        {
            if (maxAbsOutcomeDelta >= outcomeUtilityScale && maxAbsOutcomeDelta >= 0.001f) return "MaxDelta";
            if (outcomeUtilityScale >= 0.001f) return "ScaleFloor";
            return "Epsilon";
        }

        /// <summary>True when this candidate's |raw delta| equals the pass maximum, i.e. it defines the normalization.</summary>
        public bool IsPassMaxDelta(AICandidateSnapshot c)
        {
            return c != null && Mathf.Abs(c.outcomeRaw) >= maxAbsOutcomeDelta - 1e-6f;
        }

        public AICandidateSnapshot RunnerUp
        {
            get
            {
                for (int i = 0; i < candidates.Count; i++)
                    if (candidates[i].rank == 2) return candidates[i];
                return null;
            }
        }

        /// <summary>Candidate with the highest improvement per swarm cost (ignores NaN). Diagnostic only.</summary>
        public AICandidateSnapshot MostEfficientCandidate
        {
            get
            {
                AICandidateSnapshot best = null;
                float bestValue = float.NegativeInfinity;
                for (int i = 0; i < candidates.Count; i++)
                {
                    float v = candidates[i].ImprovementPerSwarmCost;
                    if (float.IsNaN(v)) continue;
                    if (v > bestValue) { bestValue = v; best = candidates[i]; }
                }
                return best;
            }
        }

        /// <summary>Stable rank by total utility (descending), ties broken by original order.</summary>
        public void AssignRanks()
        {
            int n = candidates.Count;
            List<int> order = new List<int>(n);
            for (int i = 0; i < n; i++) order.Add(i);
            order.Sort((a, b) =>
            {
                int c = candidates[b].totalUtility.CompareTo(candidates[a].totalUtility);
                return c != 0 ? c : a.CompareTo(b);
            });
            for (int r = 0; r < n; r++) candidates[order[r]].rank = r + 1;
        }
    }

    /// <summary>Everything the AI did (and saw) during one RunAILoop iteration.</summary>
    public sealed class AITickSnapshot
    {
        // identity
        public int tickNumber;
        public float gameTime;                 // Time.time at tick start
        public int frame;

        // AI state / settings in effect
        public bool isRunning;
        public int phaseIndex;
        public string phaseName;
        public int phaseCount;
        public int phaseCharacterCount;
        public float tickInterval;
        public float outcomeUtilityScale;
        public int maxSpawnsPerTick;
        public float swarmEvaluationWindow;
        public int maxSwarmCopies;
        public bool aiSpawnZoneAssigned;

        // stage 1: UpdateThreatAndBudget
        public AIUpdateStatus updateStatus;
        public string updateProblem;
        public int actualMoneyAtUpdate;
        public float moneyGainPerSec;
        public float incomeThisTick;
        public float virtualBudgetBeforeUpdate;
        public float budgetAdded;
        public float virtualBudgetAfterUpdate;

        public bool threatEvaluated;
        public float rawThreat;                // value the AI used (tracker already multiplies by 100)
        public float maxThreatLevel;
        public float unclampedThreatRatio;     // |rawThreat| / maxThreatLevel (derived, for saturation check)
        public float normalizedThreat;         // 0..1 input actually fed to activity curve
        // Step 4: sampled at decision time, without changing old fields.
        public float legacyNormalizedThreat;   // legacy clamped input for side-by-side comparison
        public float relativeMilitaryAdvantage; // signed [-1,1], no urgency
        public float relativeThreatForActivity; // signed [-1,1], with optional urgency
        public bool relativeActivityModeEnabled;
        public float militaryAdvantage;        // Step 3 unweighted x100 zone score
        public float defensiveUrgencyPressure; // Step 3 x100 urgency premium
        public AIActivityCurveKind curveKind;
        public float curveValue;               // raw curve.Evaluate(normalizedThreat)
        public float activityMultiplier;       // hard-coded x3 in AILevelHarness
        public float activityBudgetPercent;    // budgetPercentThisTick (curve * multiplier, floored at 0)

        // stage 2: ExicuteBestAction
        public AIExecuteStatus executeStatus;
        public string executeProblem;
        public readonly List<AIPassSnapshot> passes = new List<AIPassSnapshot>(3);
        public AITickEnd tickEnd;
        public float virtualBudgetAtEnd;
        public int actualMoneyAtEnd;
        // Plan-mode overlay: populated by the recorder at tick completion. These
        // fields do not affect AI decisions, and survive when passes.Count == 0.
        public int planNumber;
        public string planStage, planWaitCategory, planWaitReason, planNextUnit;
        public int planStep, planLength;

        // set by the recorder when the tick is stored
        public AIPassOutcome stallOutcome;
        public string stallUnitName;
        public int stallStreakTicks;           // consecutive ticks saving/waiting on the same unit with no spawn

        public int SpawnCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < passes.Count; i++)
                    if (passes[i].outcome == AIPassOutcome.Spawned) n++;
                return n;
            }
        }

        public AIPassSnapshot LastPass
        {
            get { return passes.Count > 0 ? passes[passes.Count - 1] : null; }
        }

        string _summary;
        public string Summary
        {
            get
            {
                if (_summary == null) _summary = BuildSummary();
                return _summary;
            }
        }

        string BuildSummary()
        {
            StringBuilder sb = new StringBuilder(64);
            if (updateStatus != AIUpdateStatus.Ok) sb.Append("[update skipped] ");

            if (executeStatus != AIExecuteStatus.RanPasses)
            {
                sb.Append("[execute skipped: ").Append(executeStatus).Append(']');
                return sb.ToString();
            }

            if (passes.Count == 0)
            {
                if (planNumber > 0)
                    sb.Append("Plan #").Append(planNumber).Append(" ")
                      .Append(planStage).Append(" [").Append(planWaitCategory).Append("]");
                else sb.Append("(no passes)");
            }
            for (int i = 0; i < passes.Count; i++)
            {
                if (i > 0) sb.Append("  >  ");
                sb.Append(passes[i].Label);
            }
            return sb.ToString();
        }
    }
}