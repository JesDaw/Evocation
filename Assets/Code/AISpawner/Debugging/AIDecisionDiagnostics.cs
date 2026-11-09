using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AIDecisionTrace
{
    public enum AIDiagnosticSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    public struct AIDiagnostic
    {
        public AIDiagnosticSeverity severity;
        public string message;

        public AIDiagnostic(AIDiagnosticSeverity severity, string message)
        {
            this.severity = severity;
            this.message = message;
        }
    }

    /// <summary>
    /// Pure functions over snapshots. They describe what was observed; they never decide anything.
    /// Intentional behaviours (saving, waiting) are reported as Info, not as problems.
    /// </summary>
    public static class AIDecisionDiagnostics
    {
        public const int StallNoticeTicks = 10;

        static void Add(List<AIDiagnostic> o, AIDiagnosticSeverity s, string m)
        {
            o.Add(new AIDiagnostic(s, m));
        }

        public static void Analyze(AITickSnapshot t, List<AIDiagnostic> o)
        {
            if (t == null || o == null) return;

            // ---------- stage 1: threat / budget ----------
            switch (t.updateStatus)
            {
                case AIUpdateStatus.SkippedMissingDependency:
                    Add(o, AIDiagnosticSeverity.Error,
                        "AI has a missing dependency - threat/budget update was skipped (no budget added this tick): " + t.updateProblem);
                    break;
                case AIUpdateStatus.SkippedInvalidConfig:
                    Add(o, AIDiagnosticSeverity.Error,
                        "AI configuration is invalid - threat/budget update was skipped (no budget added this tick): " + t.updateProblem);
                    break;
                case AIUpdateStatus.SkippedMissingActivityCurve:
                    Add(o, AIDiagnosticSeverity.Error,
                        "Phase '" + t.phaseName + "' is missing its Advantage or Disadvantage activity curve - the budget was NOT increased this tick.");
                    break;
            }

            if (t.threatEvaluated)
            {
                if (float.IsNaN(t.rawThreat) || float.IsInfinity(t.rawThreat))
                {
                    Add(o, AIDiagnosticSeverity.Error, "Raw threat is NaN/Infinity. Check unit stats feeding ExpectedOutcomeCalculator.");
                }

                if (t.maxThreatLevel <= 0f)
                {
                    Add(o, AIDiagnosticSeverity.Warning,
                        "maxThreatLevel is <= 0, so the normalized threat is forced to 0 and the activity curve is always read at x=0.");
                }
                else if (t.unclampedThreatRatio >= 1f)
                {
                    Add(o, AIDiagnosticSeverity.Warning,
                        "Normalized threat is saturated at 1.0 (|raw threat| " + Mathf.Abs(t.rawThreat).ToString("F1") +
                        " >= maxThreatLevel " + t.maxThreatLevel.ToString("F1") +
                        "). The activity curve is read at its far-right edge, so any further change in threat is invisible to it.");
                }
            }

            if (t.updateStatus == AIUpdateStatus.Ok && t.activityBudgetPercent <= 0f)
            {
                Add(o, AIDiagnosticSeverity.Info,
                    "Activity evaluated to 0 - no virtual budget was allocated this tick (curve value " + t.curveValue.ToString("F3") + ").");
            }

            // ---------- stage 2: decision ----------
            switch (t.executeStatus)
            {
                case AIExecuteStatus.SkippedMissingDependency:
                    Add(o, AIDiagnosticSeverity.Error, "AI has a missing dependency - no candidates were evaluated: " + t.executeProblem);
                    break;
                case AIExecuteStatus.SkippedInvalidConfig:
                    Add(o, AIDiagnosticSeverity.Error, "AI configuration is invalid - no candidates were evaluated: " + t.executeProblem);
                    break;
                case AIExecuteStatus.NoCharactersInPhase:
                    Add(o, AIDiagnosticSeverity.Warning, "No candidates exist: phase '" + t.phaseName + "' has an empty or unassigned Characters array.");
                    break;
            }

            for (int i = 0; i < t.passes.Count; i++)
            {
                AnalyzePass(t, t.passes[i], o);
            }

            if (t.stallStreakTicks >= StallNoticeTicks)
            {
                float seconds = t.stallStreakTicks * t.tickInterval;
                string verb = t.stallOutcome == AIPassOutcome.SavingOverBudget ? "saving for" : "waiting on the cooldown of";
                Add(o, AIDiagnosticSeverity.Info,
                    "The AI has been " + verb + " '" + t.stallUnitName + "' for " + t.stallStreakTicks +
                    " consecutive ticks (~" + seconds.ToString("F0") + "s) without spawning. This is expected for expensive units, " +
                    "but worth a look if the unit is not actually reachable.");
            }

            if (t.passes.Count > 1 && !t.aiSpawnZoneAssigned)
            {
                Add(o, AIDiagnosticSeverity.Info,
                    "aiSpawnZone is not assigned, so units spawned earlier in this tick are NOT registered before later passes are evaluated.");
            }
        }

        static void AnalyzePass(AITickSnapshot t, AIPassSnapshot p, List<AIDiagnostic> o)
        {
            string tag = "Pass " + (p.passIndex + 1) + ": ";

            if (p.nullCharacterEntries > 0)
            {
                Add(o, AIDiagnosticSeverity.Warning,
                    tag + p.nullCharacterEntries + " null entr" + (p.nullCharacterEntries == 1 ? "y" : "ies") + " in the phase's Characters array were ignored.");
            }
            if (p.skippedCandidates.Count > 0)
            {
                Add(o, AIDiagnosticSeverity.Warning,
                    tag + p.skippedCandidates.Count + " candidate(s) skipped for an invalid cost/cooldown (NaN, Infinity or negative): " +
                    string.Join(", ", p.skippedCandidates.ToArray()));
            }

            AICandidateSnapshot sel = p.Selected;

            switch (p.outcome)
            {
                case AIPassOutcome.NoValidCandidates:
                    Add(o, AIDiagnosticSeverity.Warning, tag + "No valid candidates were available to evaluate.");
                    break;

                case AIPassOutcome.SpawnFailed:
                    Add(o, AIDiagnosticSeverity.Warning,
                        tag + "SpawnObjects.EnemyInstance.Spawn returned null for '" + (sel != null ? sel.unitName : "?") +
                        "'. No cooldown was started and no budget was spent.");
                    break;

                case AIPassOutcome.SavingOverBudget:
                    if (sel != null)
                        Add(o, AIDiagnosticSeverity.Info,
                            tag + "Selected candidate '" + sel.unitName + "' is over the virtual budget (cost " + sel.spawnCost.ToString("0.##") +
                            " > " + sel.virtualBudget.ToString("F2") + "). Intentional saving - lower-ranked units are not purchased meanwhile.");
                    break;

                case AIPassOutcome.WaitingOnCooldown:
                    if (sel != null)
                        Add(o, AIDiagnosticSeverity.Info,
                            tag + "Selected candidate '" + sel.unitName + "' is on cooldown (" + p.cooldownRemainingAtDecision.ToString("F2") +
                            "s left). Intentional wait - alternatives are not spawned meanwhile.");
                    break;
            }

            if (p.walletUpgraded)
            {
                Add(o, AIDiagnosticSeverity.Info, tag + "Saving triggered Money.UpgradeMaxMoney() and the AI wallet tier was upgraded.");
            }

            if (p.candidates.Count == 0) return;

            // Observations about the scoring inputs
            if (p.maxAbsOutcomeDelta < t.outcomeUtilityScale)
            {
                Add(o, AIDiagnosticSeverity.Info,
                    tag + "Largest raw outcome delta (" + p.maxAbsOutcomeDelta.ToString("F2") + ") is below outcomeUtilityScale (" +
                    t.outcomeUtilityScale.ToString("F1") + "), so outcome utilities are divided by that floor rather than by the best candidate.");
            }

            bool anyImprovement = false;
            for (int i = 0; i < p.candidates.Count; i++)
                if (p.candidates[i].outcomeRaw > 0f) { anyImprovement = true; break; }
            if (!anyImprovement)
            {
                Add(o, AIDiagnosticSeverity.Info, tag + "No candidate improves the expected outcome (every raw delta is <= 0).");
            }

            if (sel != null)
            {
                int ties = 0;
                for (int i = 0; i < p.candidates.Count; i++)
                {
                    AICandidateSnapshot c = p.candidates[i];
                    if (c != sel && Mathf.Abs(c.totalUtility - sel.totalUtility) < 1e-6f) ties++;
                }
                if (ties > 0)
                {
                    Add(o, AIDiagnosticSeverity.Info,
                        tag + ties + " other candidate(s) tie with '" + sel.unitName + "' for the highest utility; the Characters array order decided.");
                }

                // Sanity check of the instrumentation itself: the AI averages four terms.
                float expected = (sel.moneyUtility + sel.cooldownUtility + sel.expenditureUtility + sel.outcomeUtility) / 4f;
                if (Mathf.Abs(expected - sel.totalUtility) > 1e-3f)
                {
                    Add(o, AIDiagnosticSeverity.Error,
                        tag + "Instrumentation sanity check failed: recorded components average to " + expected.ToString("F4") +
                        " but the recorded total is " + sel.totalUtility.ToString("F4") + ".");
                }
            }
        }

        // ------------------------------------------------------------------------------------
        // Plain-English explanations
        // ------------------------------------------------------------------------------------

        public static string ExplainTick(AITickSnapshot t)
        {
            if (t == null) return "";

            switch (t.executeStatus)
            {
                case AIExecuteStatus.SkippedMissingDependency:
                    return "The decision stage did not run: " + t.executeProblem;
                case AIExecuteStatus.SkippedInvalidConfig:
                    return "The decision stage did not run: " + t.executeProblem;
                case AIExecuteStatus.NoCharactersInPhase:
                    return "Phase '" + t.phaseName + "' has no characters assigned, so there was nothing to evaluate.";
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(t.passes.Count).Append(t.passes.Count == 1 ? " spawn pass was executed" : " spawn passes were executed");
            sb.Append(" (").Append(t.SpawnCount).Append(" spawned). ");

            switch (t.tickEnd)
            {
                case AITickEnd.PassEndedTick:
                    sb.Append("The tick ended because the last pass did not spawn (see its explanation below).");
                    break;
                case AITickEnd.BudgetExhausted:
                    sb.Append("The tick ended because the virtual budget reached 0 after the last spawn.");
                    break;
                case AITickEnd.MaxPassesReached:
                    sb.Append("The tick ended after reaching maxSpawnsPerTick (").Append(t.maxSpawnsPerTick).Append(").");
                    break;
            }
            return sb.ToString();
        }

        public static string ExplainPass(AITickSnapshot t, AIPassSnapshot p)
        {
            if (p == null) return "";
            AICandidateSnapshot s = p.Selected;

            if (s == null && p.outcome != AIPassOutcome.NoValidCandidates)
                return "Pass outcome: " + p.outcome + " (no selected candidate was recorded).";

            switch (p.outcome)
            {
                case AIPassOutcome.Spawned:
                {
                    string msg = "SPAWNED '" + s.unitName + "'. It had the highest total utility (" + s.totalUtility.ToString("F3") +
                        "), fit inside the virtual budget (cost " + s.spawnCost.ToString("0.##") + " <= " + s.virtualBudget.ToString("F2") +
                        ") and was off cooldown. Virtual budget " + p.virtualBudgetBefore.ToString("F2") + " -> " + p.virtualBudgetAfter.ToString("F2") +
                        "; its " + s.spawnCooldown.ToString("0.##") + "s spawn cooldown started.";
                    if (!string.IsNullOrEmpty(p.spawnedObjectName)) msg += " Spawned object: " + p.spawnedObjectName + ".";
                    msg += p.spawnZoneNotified
                        ? " The new unit was sent to aiSpawnZone so later passes can see it."
                        : " aiSpawnZone is unassigned, so later passes cannot see the new unit.";
                    return msg;
                }

                case AIPassOutcome.SavingOverBudget:
                {
                    string msg = "SAVING for '" + s.unitName + "' (intentional). It has the highest total utility (" + s.totalUtility.ToString("F3") +
                        ") but costs " + s.spawnCost.ToString("0.##") + " while the virtual budget is only " + p.virtualBudgetBefore.ToString("F2") +
                        " (actual money " + s.actualMoney + "). The AI does not spend on lower-ranked units while it saves.";
                    if (s.affordableActual)
                        msg += " Actual money already covers the cost; the activity-driven virtual budget simply has not accumulated enough yet.";
                    else
                        msg += " Actual money is also below the cost.";
                    if (p.walletUpgradeAttempted)
                        msg += p.walletUpgraded
                            ? " This pass called Money.UpgradeMaxMoney() and the wallet WAS upgraded."
                            : " This pass called Money.UpgradeMaxMoney() but no upgrade happened (not enough money or already at the top tier).";
                    return msg;
                }

                case AIPassOutcome.WaitingOnCooldown:
                    return "WAITING on cooldown for '" + s.unitName + "' (intentional). It has the highest total utility (" + s.totalUtility.ToString("F3") +
                        ") and is affordable (cost " + s.spawnCost.ToString("0.##") + " <= " + s.virtualBudget.ToString("F2") +
                        "), but " + p.cooldownRemainingAtDecision.ToString("F2") + "s of its " + s.spawnCooldown.ToString("0.##") +
                        "s spawn cooldown remain. The AI waits rather than spawning alternatives.";

                case AIPassOutcome.NoValidCandidates:
                    return "NO VALID CANDIDATES. Every entry in the phase's Characters array was null or had an invalid cost/cooldown (" +
                        p.nullCharacterEntries + " null, " + p.skippedCandidates.Count + " invalid).";

                case AIPassOutcome.SpawnFailed:
                    return "SPAWN FAILED. SpawnObjects.EnemyInstance.Spawn('" + s.unitName + "') returned null. The AI stopped for this tick; " +
                        "no cooldown was started and no virtual budget was spent.";
            }
            return p.outcome.ToString();
        }
    }
}
