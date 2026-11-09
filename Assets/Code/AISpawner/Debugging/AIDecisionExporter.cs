// Editor-only. Exports the recorder's current history to two CSV files:
//   <stamp>_ticks.csv       one row per AI tick
//   <stamp>_candidates.csv  one row per candidate per spawn pass
// Files go to <project root>/AIDecisionExports/.
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using AIDecisionTrace;

public static class AIDecisionExporter
{
    [MenuItem("Window/AI/Export Decision History")]
    public static void Export()
    {
        int n = AIDecisionRecorder.Count;
        if (n == 0)
        {
            EditorUtility.DisplayDialog("AI Decision Export", "No recorded ticks to export.", "OK");
            return;
        }

        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "AIDecisionExports"));
        Directory.CreateDirectory(dir);
        string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string ticksPath = Path.Combine(dir, stamp + "_ticks.csv");
        string candPath = Path.Combine(dir, stamp + "_candidates.csv");

        StringBuilder ticks = new StringBuilder();
        StringBuilder cands = new StringBuilder();

        ticks.AppendLine("tick,time,frame,phase,phaseIndex,tickInterval,updateStatus,actualMoney,moneyGainPerSec,income,budgetBefore,budgetAdded,budgetAfter," +
            "rawThreat,maxThreat,unclampedRatio,normalizedThreat,curve,curveValue,multiplier,activity,executeStatus,passes,spawns,tickEnd," +
            "budgetAtEnd,moneyAtEnd,stallStreak,summary");

        cands.AppendLine("tick,time,pass,passOutcome,unit,selected,rank,cost,cooldown,cooldownLeft,actualMoney,virtualBudget,affordActual,affordVirtual," +
            "copies,committedCost,moneyU,cooldownU,rateU,baselineOutcome,outcomeWithCandidate,rawDelta,outcomeU,total,maxAbsDelta,denominator");

        for (int i = 0; i < n; i++)
        {
            AITickSnapshot t = AIDecisionRecorder.GetFromOldest(i);
            if (t == null) continue;

            Row(ticks, t.tickNumber, F(t.gameTime), t.frame, S(t.phaseName), t.phaseIndex, F(t.tickInterval), t.updateStatus,
                t.actualMoneyAtUpdate, F(t.moneyGainPerSec), F(t.incomeThisTick), F(t.virtualBudgetBeforeUpdate), F(t.budgetAdded), F(t.virtualBudgetAfterUpdate),
                F(t.rawThreat), F(t.maxThreatLevel), F(t.unclampedThreatRatio), F(t.normalizedThreat), t.curveKind, F(t.curveValue), F(t.activityMultiplier),
                F(t.activityBudgetPercent), t.executeStatus, t.passes.Count, t.SpawnCount, t.tickEnd,
                F(t.virtualBudgetAtEnd), t.actualMoneyAtEnd, t.stallStreakTicks, S(t.Summary));

            foreach (AIPassSnapshot p in t.passes)
            {
                foreach (AICandidateSnapshot c in p.candidates)
                {
                    Row(cands, t.tickNumber, F(t.gameTime), p.passIndex + 1, p.outcome, S(c.unitName), c.isSelected ? 1 : 0, c.rank,
                        F(c.spawnCost), F(c.spawnCooldown), F(c.cooldownRemaining), c.actualMoney, F(c.virtualBudget),
                        c.affordableActual ? 1 : 0, c.affordableVirtual ? 1 : 0, c.swarmCopies, F(c.committedCost),
                        F(c.moneyUtility), F(c.cooldownUtility), F(c.expenditureUtility),
                        F(c.baselineOutcome), F(c.outcomeWithCandidate), F(c.outcomeRaw), F(c.outcomeUtility), F(c.totalUtility),
                        F(p.maxAbsOutcomeDelta), F(p.outcomeDenominator));
                }
            }
        }

        File.WriteAllText(ticksPath, ticks.ToString(), new UTF8Encoding(false));
        File.WriteAllText(candPath, cands.ToString(), new UTF8Encoding(false));

        Debug.Log("[AI Inspector] Exported " + n + " ticks to " + dir);
        EditorUtility.RevealInFinder(ticksPath);
    }

    static string F(float v)
    {
        return v.ToString("R", CultureInfo.InvariantCulture);
    }

    // CSV-escape a text field.
    static string S(string s)
    {
        if (s == null) return "";
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }

    static void Row(StringBuilder sb, params object[] fields)
    {
        for (int i = 0; i < fields.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(fields[i]);
        }
        sb.AppendLine();
    }
}
