
#if UNITY_EDITOR
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// New Editor-only per-plan CSV exporter.
/// Does not alter the existing decision CSV schema.
/// </summary>
public static class AITacticalPlanCSVExporter
{
    static readonly CultureInfo Invariant =
        CultureInfo.InvariantCulture;

    [MenuItem("Window/AI/Export Tactical Plan History")]
    public static void ExportSelected()
    {
        AITacticalPlanExecutor plan =
            Object.FindFirstObjectByType<AITacticalPlanExecutor>();

        if (plan == null)
        {
            EditorUtility.DisplayDialog(
                "AI Tactical Plan",
                "No tactical plan executor found in the current scene.",
                "OK");
            return;
        }

        Export(plan);
    }

    public static void Export(AITacticalPlanExecutor plan)
    {
        if (plan == null || plan.PlanRecords.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "AI Tactical Plan",
                "No plan records to export. Run the AI first.",
                "OK");
            return;
        }

        string folder = Path.GetFullPath(
            Path.Combine(
                Application.dataPath,
                "..",
                "AIDecisionExports"));

        Directory.CreateDirectory(folder);

        string file = Path.Combine(
            folder,
            System.DateTime.Now.ToString(
                "yyyyMMdd_HHmmss",
                Invariant) + "_tactical_plans.csv");

        StringBuilder csv = new StringBuilder();

        csv.AppendLine(
            "plan,phase,firstUnit,secondUnit,incomeAware,targetGapSec,startedAt,firstSpawnAt,secondSpawnAt,actualGapSec,forecastHorizonSec,currentActivityAtClear,assumedActivityAtClear,actualAtClear,virtualAtClear,requiredActualAtClear,requiredVirtualAtClear,predictedActualAtFollowup,predictedVirtualAtFollowup,actualAfterFirst,virtualAfterFirst,actualAfterSecond,virtualAfterSecond,result,explanation,orderedUnits,plannedCount,spawnedCount,spawnTimes");

        foreach (var r in plan.PlanRecords)
        {
            string[] cells =
            {
                r.number.ToString(Invariant),
                r.phase.ToString(Invariant),
                Q(r.firstUnit),
                Q(r.secondUnit),
                r.incomeAware ? "1" : "0",
                F(r.targetGap),
                F(r.startTime),
                F(r.firstTime),
                F(r.secondTime),
                F(r.deploymentGap),
                F(r.forecastHorizon),
                F(r.currentActivityAtClear),
                F(r.assumedActivityAtClear),
                F(r.actualAtClear),
                F(r.virtualAtClear),
                F(r.requiredActualAtClear),
                F(r.requiredVirtualAtClear),
                F(r.predictedActualAfterFirst),
                F(r.predictedVirtualAfterFirst),
                F(r.actualAfterFirst),
                F(r.virtualAfterFirst),
                F(r.actualAtFollowup),
                F(r.virtualAtFollowup),
                Q(r.result),
                Q(r.explanation),
                Q(r.orderedUnits),
                r.plannedCount.ToString(Invariant),
                r.spawnedCount.ToString(Invariant),
                Q(r.spawnTimes)
            };

            csv.AppendLine(string.Join(",", cells));
        }

        File.WriteAllText(
            file,
            csv.ToString(),
            new UTF8Encoding(false));

        Debug.Log(
            "[AI Tactical Plan] Exported " +
            plan.PlanRecords.Count +
            " records to " + file);

        EditorUtility.RevealInFinder(file);
    }

    static string F(float x)
    {
        return float.IsNaN(x) ||
               float.IsInfinity(x) ||
               x < 0f
            ? ""
            : x.ToString("R", Invariant);
    }

    static string Q(string text)
    {
        return "\"" +
            (text ?? "").Replace("\"", "\"\"") +
            "\"";
    }
}
#endif
  