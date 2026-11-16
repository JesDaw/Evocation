
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AITacticalPlanExecutor))]
public sealed class AITacticalPlanExecutorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        AITacticalPlanExecutor plan =
            (AITacticalPlanExecutor)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            "Execution state (read-only)",
            EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Enable the plan and income-aware launch to test conservative funding. The CSV exporter is available under Window/AI/Export Tactical Plan History while playing.",
                MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Stage", plan.Stage.ToString());
        EditorGUILayout.LabelField("Plan #", plan.PlanNumber.ToString());
        EditorGUILayout.LabelField(
            "Origin phase",
            plan.OriginPhaseIndex.ToString());
        EditorGUILayout.LabelField(
            "Successful purchases",
            plan.SuccessfulSteps + " / " + plan.PlannedUnitCount);
        EditorGUILayout.LabelField(
            "Next unit",
            plan.CurrentTarget != null
                ? plan.CurrentTarget.name
                : "(none)");

        if (plan.IncomeAwareLaunchEnabled)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Income-aware forecast",
                EditorStyles.boldLabel);

            EditorGUILayout.LabelField(
                "Conservative activity factor",
                plan.ForecastActivityMultiplier.ToString("F3"));

            EditorGUILayout.LabelField(
                "Forecast horizon",
                plan.ForecastHorizonSeconds.ToString("F2") + " s");

            EditorGUILayout.LabelField(
                "Required actual / virtual",
                plan.RequiredActualLaunchMoney.ToString("F2") +
                " / " +
                plan.RequiredVirtualLaunchBudget.ToString("F2"));

            EditorGUILayout.LabelField(
                "Projected actual / virtual",
                plan.ProjectedActualAtFollowup.ToString("F2") +
                " / " +
                plan.ProjectedVirtualAtFollowup.ToString("F2"));

            EditorGUILayout.LabelField(
                "Forecast passed",
                plan.ProjectedLaunchFeasible ? "Yes" : "No");

            EditorGUILayout.LabelField(
                "Money / budget at launch",
                plan.ActualMoneyAtLaunch.ToString("F2") +
                " / " +
                plan.VirtualBudgetAtLaunch.ToString("F2"));

            EditorGUILayout.LabelField(
                "After first purchase",
                plan.ActualMoneyAfterFirst.ToString("F2") +
                " / " +
                plan.VirtualBudgetAfterFirst.ToString("F2"));

            EditorGUILayout.LabelField(
                "After second purchase",
                plan.ActualMoneyAtFollowup.ToString("F2") +
                " / " +
                plan.VirtualBudgetAtFollowup.ToString("F2"));

            EditorGUILayout.LabelField(
                "Completed within window",
                plan.CompletedWithinWindowCount.ToString());

            EditorGUILayout.LabelField(
                "Completed beyond window",
                plan.CompletedOutsideWindowCount.ToString());
        }

        EditorGUILayout.LabelField(
            "First spawn at",
            plan.FirstSpawnAt >= 0f
                ? plan.FirstSpawnAt.ToString("F2")
                : "(none)");

        EditorGUILayout.LabelField(
            "Last spawn gap",
            plan.LastDeploymentGapSeconds >= 0f
                ? plan.LastDeploymentGapSeconds.ToString("F2") + " s"
                : "(none)");

        EditorGUILayout.HelpBox(
            plan.Explanation,
            MessageType.None);

        if (GUILayout.Button("Export Tactical Plan History (CSV)"))
            AITacticalPlanCSVExporter.Export(plan);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            "Recent events",
            EditorStyles.boldLabel);

        foreach (string evt in plan.RecentEvents)
        {
            EditorGUILayout.LabelField(
                evt,
                EditorStyles.wordWrappedMiniLabel);
        }

        if (Event.current.type == EventType.Repaint)
            Repaint();
    }
}
#endif
  