#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using AIDecisionTrace;

public static class AIPlanDiagnosticsExporter
{
    static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    [MenuItem("Window/AI/Export Unified Plan Diagnostics")]
    public static void ExportStandalone()
    {
        if (AIDecisionRecorder.Plans.Count == 0)
        {
            EditorUtility.DisplayDialog("AI Plan Export", "No recorded plans. Open the Decision Inspector and enable Record during Play mode.", "OK");
            return;
        }
        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", Invariant);
        Export(stamp);
    }

    public static void Export(string stamp)
    {
        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "AIDecisionExports"));
        Directory.CreateDirectory(dir);
        var plans = AIDecisionRecorder.Plans;
        StringBuilder summary = new StringBuilder();
        StringBuilder pairs = new StringBuilder();
        StringBuilder eventsCsv = new StringBuilder();
        StringBuilder ticks = new StringBuilder();
        summary.AppendLine("plan,phase,startedAt,endedAt,startFrame,clan,source,objective,orderedUnits,plannedCount,completedCount,result,explanation,initialMoney,initialBudget,initiative,economicPatience,waveConcentration,counterplayStatus,directorReason,economicExplanation,initiativeExplanation,rankingReport,candidatesCaptured,eventsCaptured,planTicksCaptured,lastSpawnAt");
        pairs.AppendLine("plan,first,second,selected,cost,doctrine,doctrineRank,counterGain,counterNormalized,counterRank,blended,finalScore,finalRank,initiative,economicPatience,objective,source");
        eventsCsv.AppendLine("plan,time,frame,kind,stage,message,nextUnit,completed,planned,actualMoney,virtualBudget");
        ticks.AppendLine("tick,time,frame,plan,phase,stage,waitCategory,reason,nextUnit,nextCost,completed,planned,actualMoney,virtualBudget,actualShortfall,virtualShortfall,cooldownRemainingObserved,incomeRate,activityBudgetPercent,incomeAwareConfigured,forecastRequiredActual,forecastRequiredVirtual,forecastProjectedActual,forecastProjectedVirtual,forecastFeasible");
        foreach (AIPlanSnapshot plan in plans)
        {
            Row(summary, plan.number, plan.phase, plan.startedAt, plan.endedAt, plan.startFrame,
                plan.clan, plan.selectionSource, plan.objective, plan.orderedUnits, plan.plannedCount,
                plan.completed, plan.result, plan.explanation, plan.initialMoney, plan.initialBudget,
                plan.initiative, plan.economicPatience, plan.waveConcentration, plan.counterplayStatus,
                plan.directorReason, plan.economicExplanation, plan.initiativeExplanation,
                plan.rankingReport, plan.candidates.Count, plan.events.Count, plan.ticks.Count, plan.lastSpawnAt);
            foreach (AIPlanCandidateSnapshot c in plan.candidates)
                Row(pairs, plan.number, c.first, c.second, c.selected, c.cost,
                    c.doctrine, c.doctrineRank, c.counterGain, c.counterNormalized,
                    c.counterRank, c.blended, c.finalScore, c.finalRank,
                    plan.initiative, plan.economicPatience, plan.objective, plan.selectionSource);
            foreach (AIPlanEventSnapshot e in plan.events)
                Row(eventsCsv, plan.number, e.time, e.frame, e.kind, e.stage,
                    e.message, e.target, e.completed, e.planned, e.actualMoney, e.virtualBudget);
            foreach (AIPlanTickSnapshot t in plan.ticks)
                Row(ticks, t.tick, t.time, t.frame, t.planNumber, t.phase, t.stage,
                    t.waitCategory, t.reason, t.nextUnit, t.nextCost, t.completed, t.planned,
                    t.money, t.virtualBudget, t.remainingActual, t.remainingVirtual,
                    t.cooldownLeft, t.incomeRate, t.activity, t.incomeAware,
                    t.forecastRequiredActual, t.forecastRequiredVirtual,
                    t.forecastProjectedActual, t.forecastProjectedVirtual, t.feasible);
        }
        Write(dir, stamp + "_plans.csv", summary);
        Write(dir, stamp + "_plan_candidates.csv", pairs);
        Write(dir, stamp + "_plan_events.csv", eventsCsv);
        Write(dir, stamp + "_plan_ticks.csv", ticks);
        Debug.Log("[AI Inspector] Unified plan export: " + plans.Count + " plans, " +
            (plans.Count > 0 ? "including ranked pair scores, execution events and wait timelines" : "empty") + ". " + dir);
    }

    static void Write(string dir, string name, StringBuilder data)
    {
        File.WriteAllText(Path.Combine(dir, name), data.ToString(), new UTF8Encoding(false));
    }

    static void Row(StringBuilder sb, params object[] cells)
    {
        for (int i = 0; i < cells.Length; i++)
        {
            if (i > 0) sb.Append(',');
            object v = cells[i];
            if (v is bool) sb.Append((bool)v ? "1" : "0");
            else if (v is float)
            {
                float f = (float)v;
                if (!float.IsNaN(f) && !float.IsInfinity(f)) sb.Append(f.ToString("R", Invariant));
            }
            else if (v is int) sb.Append(((int)v).ToString(Invariant));
            else if (v != null)
                sb.Append('"').Append(v.ToString().Replace("\"", "\"\"")).Append('"');
        }
        sb.Append('\n');
    }
}
#endif
