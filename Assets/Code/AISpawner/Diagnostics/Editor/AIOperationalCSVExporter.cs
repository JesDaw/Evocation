#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using AIDecisionTrace;

public static class AIOperationalCSVExporter
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    [MenuItem("Window/AI/Export Operational Shadow History")]
    public static void ExportStandalone()
    {
        if (AIOperationalObserver.History.Count == 0)
        {
            EditorUtility.DisplayDialog("Operational Shadow History", "No observations. Attach AIOperationalObserver and enable Record in the Decision Inspector.", "OK");
            return;
        }
        Export(DateTime.Now.ToString("yyyyMMdd_HHmmss", Inv));
    }

    public static void Export(string stamp)
    {
        string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "AIDecisionExports"));
        Directory.CreateDirectory(directory);
        var obs = new StringBuilder(8192);
        var proposals = new StringBuilder(32768);
        obs.AppendLine("observation,time,frame,plan,phase,eligibleCount,aiMoney,aiVirtualBudget,aiIncomePerSec,playerMoney,playerIncomePerSec,initiative,positionalValuation,economicPatience,bestShadowProposal,status");
        proposals.AppendLine("observation,time,frame,plan,rank,shadowWinner,rankable,objective,maneuver,target,zone,unit,enemyCount,playerCount,baselineOutcomeX100,withReinforcementX100,rawGainX100,zoneIncentive,cost,estimatedWaitSeconds,normalizedMilitaryBenefit,objectiveDenialBenefit,economicDiscount,provisionalUtility,caveat");
        foreach (AIOperationalObservation o in AIOperationalObserver.History)
        {
            Row(obs, o.id, o.time, o.frame, o.planNumber, o.phase, o.eligibleCount,
                o.money, o.virtualBudget, o.incomeRate, o.playerMoney, o.playerIncome,
                o.initiative, o.positionalValuation, o.economicPatience, o.chosen, o.status);
            for (int i = 0; i < o.proposals.Count; i++)
            {
                AIOperationCandidate p = o.proposals[i];
                Row(proposals, o.id, o.time, o.frame, o.planNumber, i + 1, p.selected, p.rankable,
                    p.objective, p.maneuver, p.target, p.zoneNumber, p.unit,
                    p.enemyCount, p.playerCount, p.baseline, p.hypothetical, p.gain,
                    p.incentive, p.cost, p.estimatedWait, p.militaryBenefit,
                    p.objectiveBenefit, p.economicBenefit, p.score, p.caveat);
            }
        }
        File.WriteAllText(Path.Combine(directory, stamp + "_operations.csv"), obs.ToString(), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(directory, stamp + "_operation_candidates.csv"), proposals.ToString(), new UTF8Encoding(false));
        Debug.Log("[AI Operations] Exported " + AIOperationalObserver.History.Count + " shadow evaluations to " + directory);
    }

    static void Row(StringBuilder sb, params object[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0) sb.Append(',');
            object v = values[i];
            if (v is bool) sb.Append((bool)v ? "1" : "0");
            else if (v is float)
            {
                float f = (float)v;
                if (!float.IsNaN(f) && !float.IsInfinity(f)) sb.Append(f.ToString("R", Inv));
            }
            else if (v is int) sb.Append(((int)v).ToString(Inv));
            else if (v != null) sb.Append('"').Append(v.ToString().Replace("\"", "\"\"")).Append('"');
        }
        sb.Append('\n');
    }
}
#endif
