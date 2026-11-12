// Editor-only (lives in an "Editor" folder, so it is never compiled into a build).
// Reads snapshots from AIDecisionRecorder; never talks to the AI's decision methods.
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AIDecisionTrace;

public class AIDecisionInspectorWindow : EditorWindow
{
    // ---------------------------------------------------------------- candidate table columns
    const int ColRank = 0;
    const int ColSel = 1;
    const int ColUnit = 2;
    const int ColCost = 3;
    const int ColActual = 4;
    const int ColBudget = 5;
    const int ColCooldown = 6;
    const int ColCdLeft = 7;
    const int ColCopies = 8;
    const int ColMoneyU = 9;
    const int ColCooldownU = 10;
    const int ColRateU = 11;
    const int ColRawDelta = 12;
    const int ColOutcomeU = 13;
    const int ColTotal = 14;

    static readonly string[] ColHeaders =
    {
        "Rank", "Sel", "Unit", "Cost", "Money OK", "Budget OK", "Cooldown", "CD left",
        "Copies", "Money U", "Cooldown U", "Rate U", "Raw delta", "Outcome U", "TOTAL"
    };

    static readonly float[] ColWidths =
    {
        38, 32, 140, 46, 62, 62, 62, 56, 48, 62, 70, 52, 66, 70, 58
    };

    static readonly string[] ColTips =
    {
        "Rank by total utility (1 = best). Ties keep Characters-array order.",
        "Selected = the candidate the AI chose this pass (always the highest total utility).",
        "Unit asset name.",
        "Spawn cost (_spawnCost).",
        "Actual money (Money.AIInstance.CurrentMoney) >= cost.",
        "Cost <= virtual budget. The AI's own 'overBudget' flag is the inverse of this.",
        "Spawn cooldown in seconds (_spawnCooldown).",
        "Cooldown remaining when evaluated.",
        "Virtual swarm copies evaluated for the outcome calculation (evaluatedCopies).",
        "Money utility = clamp01(actual money / cost); 1 when cost <= 0.",
        "Cooldown utility = clamp01(1 - cooldown remaining / cooldown); 1 when cooldown <= 0.",
        "Expenditure-rate utility = clamp01(cost x copies / virtual budget); 0 when budget <= 0.",
        "RAW. Baseline outcome - outcome with candidate copies (tracker scale, x100). Positive = improves the AI's position. Outcome sign convention: positive = player favored.",
        "NORMALIZED. clamp(raw delta / outcome denominator, -1, 1).",
        "(Money U + Cooldown U + Rate U + Outcome U) / 4. The AI picks the highest."
    };

    static float TableWidth
    {
        get
        {
            float w = 0f;
            for (int i = 0; i < ColWidths.Length; i++) w += ColWidths[i];
            return w;
        }
    }

    // ---------------------------------------------------------------- layout constants
    const string PrefCapacity = "AIDecisionInspector.Capacity";
    const float HistoryWidth = 270f;
    const float HistoryRowHeight = 20f;
    const float TableRowHeight = 20f;

    // ---------------------------------------------------------------- persisted window state
    [SerializeField] bool following = true;
    [SerializeField] bool recording = true;
    [SerializeField] bool showHistory = true;
    [SerializeField] bool showPlanHistory = false;
    [SerializeField] bool showOperationHistory = false;
    [SerializeField] int selectedOperationId = -1;
    [SerializeField] int selectedPlanNumber = -1;
    [SerializeField] bool planCandidatesFoldout = true;
    [SerializeField] bool planEventsFoldout = true;
    [SerializeField] bool planTickFoldout = true;
    [SerializeField] bool onlySpawnTicks = false;
    [SerializeField] bool overviewFoldout = true;
    [SerializeField] int sortColumn = ColTotal;
    [SerializeField] bool sortDescending = true;
    [SerializeField] int capacity = AIDecisionRecorder.DefaultCapacity;
    [SerializeField] string inspectedUnit = "";   // candidate shown in the details panel ("" = the selected one)
    [SerializeField] bool detailsFoldout = true;
    [SerializeField] bool profileFoldout = true;

    // ---------------------------------------------------------------- transient state
    Vector2 historyScroll;
    Vector2 detailScroll;
    AITickSnapshot pinned;
    AITickSnapshot displayed;          // cached on Layout so Layout/Repaint see identical data
    AIPlanSnapshot displayedPlan;
    AIOperationalObservation displayedOperation;
    AITickSnapshot diagnosticsFor;
    readonly List<AIDiagnostic> diagnostics = new List<AIDiagnostic>();
    readonly List<AITickSnapshot> historyView = new List<AITickSnapshot>();
    readonly Dictionary<int, bool> passFoldouts = new Dictionary<int, bool>();

    AILevelHarness liveHarness;
    bool liveMoneyAvailable;
    int liveMoney;

    int lastVersion = -1;
    int lastOperationalVersion = -1;
    double lastRepaintTime;

    GUIStyle cellStyle, cellBold, cellYes, cellNo, rowStyle, foldoutBold;

    // ================================================================ lifecycle

    [MenuItem("Window/AI/Decision Inspector")]
    public static void Open()
    {
        AIDecisionInspectorWindow w = GetWindow<AIDecisionInspectorWindow>("AI Decision Inspector");
        w.minSize = new Vector2(760f, 420f);
        w.Show();
    }

    void OnEnable()
    {
        capacity = EditorPrefs.GetInt(PrefCapacity, AIDecisionRecorder.DefaultCapacity);
        AIDecisionRecorder.Capacity = capacity;
        if (recording) AIDecisionRecorder.Enabled = true;
        EditorApplication.update += OnEditorUpdate;
    }

    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        AIDecisionRecorder.Enabled = false; // closed window => AI captures nothing
    }

    void OnEditorUpdate()
    {
        // Re-assert after domain reloads / play-mode transitions.
        if (recording && !AIDecisionRecorder.Enabled) AIDecisionRecorder.Enabled = true;

        bool dirty = AIDecisionRecorder.Version != lastVersion || AIOperationalObserver.Version != lastOperationalVersion;
        lastOperationalVersion = AIOperationalObserver.Version;
        if (dirty) lastVersion = AIDecisionRecorder.Version;

        double now = EditorApplication.timeSinceStartup;
        if (dirty || (Application.isPlaying && now - lastRepaintTime > 0.25))
        {
            lastRepaintTime = now;
            Repaint();
        }
    }

    // ================================================================ OnGUI

    void OnGUI()
    {
        EnsureStyles();
        if (Event.current.type == EventType.Layout) RefreshLayoutCache();

        DrawToolbar();

        EditorGUILayout.BeginHorizontal();
        if (showHistory) DrawHistory();
        DrawDetails();
        EditorGUILayout.EndHorizontal();
    }

    void EnsureStyles()
    {
        if (cellStyle != null) return;

        cellStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleLeft,
            clipping = TextClipping.Clip,
            padding = new RectOffset(4, 2, 0, 0)
        };
        cellBold = new GUIStyle(cellStyle) { fontStyle = FontStyle.Bold };
        cellYes = new GUIStyle(cellStyle);
        cellYes.normal.textColor = new Color(0.25f, 0.75f, 0.30f);
        cellNo = new GUIStyle(cellStyle);
        cellNo.normal.textColor = new Color(0.90f, 0.35f, 0.35f);
        rowStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleLeft,
            clipping = TextClipping.Clip
        };
        foldoutBold = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
    }

    void RefreshLayoutCache()
    {
        displayed = following ? AIDecisionRecorder.Latest : pinned;
        displayedOperation = null;
        if (showOperationHistory)
        {
            var observations = AIOperationalObserver.History;
            if (following && observations.Count > 0) displayedOperation = observations[observations.Count - 1];
            else
                for (int i = observations.Count - 1; i >= 0; i--)
                    if (observations[i].id == selectedOperationId) { displayedOperation = observations[i]; break; }
        }
        displayedPlan = null;
        if (showPlanHistory)
        {
            var plans = AIDecisionRecorder.Plans;
            if (following && plans.Count > 0) displayedPlan = plans[plans.Count - 1];
            else displayedPlan = AIDecisionRecorder.GetPlan(selectedPlanNumber);
        }

        liveHarness = AILevelHarness.Instance;
        Money ai = Money.AIInstance;
        liveMoneyAvailable = ai != null;
        liveMoney = ai != null ? ai.CurrentMoney : 0;

        historyView.Clear();
        for (int i = 0; i < AIDecisionRecorder.Count; i++)
        {
            AITickSnapshot t = AIDecisionRecorder.GetFromNewest(i);
            if (t == null) continue;
            if (onlySpawnTicks && t.SpawnCount == 0) continue;
            historyView.Add(t);
        }

        if (diagnosticsFor != displayed)
        {
            diagnostics.Clear();
            if (displayed != null)
            {
                AIDecisionDiagnostics.Analyze(displayed, diagnostics);
                List<AIDiagnostic> sorted = diagnostics.OrderByDescending(d => (int)d.severity).ToList();
                diagnostics.Clear();
                diagnostics.AddRange(sorted);
            }
            diagnosticsFor = displayed;
        }
    }

    // ================================================================ toolbar

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        bool rec = GUILayout.Toggle(recording, "Record", EditorStyles.toolbarButton, GUILayout.Width(60));
        if (rec != recording)
        {
            recording = rec;
            AIDecisionRecorder.Enabled = rec;
        }

        if (following)
        {
            if (GUILayout.Button("Pause", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                pinned = displayed;
                following = false;
            }
        }
        else
        {
            if (GUILayout.Button("Resume Live", EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                following = true;
            }
        }

        if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            AIDecisionRecorder.Clear();
            AIOperationalObserver.ClearHistory();
            pinned = null;
            diagnosticsFor = null;
        }

        if (GUILayout.Button("Export CSV", EditorStyles.toolbarButton, GUILayout.Width(75)))
        {
            AIDecisionExporter.Export();
        }

        GUILayout.Space(8f);
        GUILayout.Label("History size", EditorStyles.miniLabel, GUILayout.Width(66f));
        int newCap = EditorGUILayout.DelayedIntField(capacity, EditorStyles.toolbarTextField, GUILayout.Width(50f));
        if (newCap != capacity)
        {
            capacity = Mathf.Clamp(newCap, AIDecisionRecorder.MinCapacity, AIDecisionRecorder.MaxCapacity);
            AIDecisionRecorder.Capacity = capacity;
            EditorPrefs.SetInt(PrefCapacity, capacity);
        }

        bool newPlanView = GUILayout.Toggle(showPlanHistory, "Plans", EditorStyles.toolbarButton, GUILayout.Width(52));
        if (newPlanView != showPlanHistory)
        {
            showPlanHistory = newPlanView;
            if (showPlanHistory) showOperationHistory = false;
            following = true;
        }
        bool newOperationsView = GUILayout.Toggle(showOperationHistory, "Operations", EditorStyles.toolbarButton, GUILayout.Width(84));
        if (newOperationsView != showOperationHistory)
        {
            showOperationHistory = newOperationsView;
            if (showOperationHistory) showPlanHistory = false;
            following = true;
        }
        if (!showPlanHistory && !showOperationHistory)
            onlySpawnTicks = GUILayout.Toggle(onlySpawnTicks, "Spawn ticks only", EditorStyles.toolbarButton, GUILayout.Width(110f));
        showHistory = GUILayout.Toggle(showHistory, "History list", EditorStyles.toolbarButton, GUILayout.Width(80f));

        GUILayout.FlexibleSpace();
        GUILayout.Label(
            (showOperationHistory ? AIOperationalObserver.History.Count + " operation observations" : showPlanHistory ? AIDecisionRecorder.Plans.Count + " plans" : AIDecisionRecorder.Count + "/" + AIDecisionRecorder.Capacity + " ticks") +
            "   |   " + (following ? "LIVE" : "PAUSED (display frozen, AI unaffected)"),
            EditorStyles.miniLabel);

        EditorGUILayout.EndHorizontal();
    }

    // ================================================================ history list

    void DrawHistory()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(HistoryWidth));
        GUILayout.Label("History (newest first)", EditorStyles.boldLabel);

        historyScroll = EditorGUILayout.BeginScrollView(historyScroll, GUILayout.Width(HistoryWidth));
        if (showOperationHistory)
        {
            var observations = AIOperationalObserver.History;
            for (int i = observations.Count - 1; i >= 0; i--)
            {
                AIOperationalObservation o = observations[i];
                if (GUILayout.Button(new GUIContent("#" + o.id + "  " + o.time.ToString("F1") + "s  " + o.chosen, o.chosen), EditorStyles.miniButton))
                {
                    selectedOperationId = o.id;
                    following = false;
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            return;
        }
        if (showPlanHistory)
        {
            var plans = AIDecisionRecorder.Plans;
            for (int i = plans.Count - 1; i >= 0; i--)
            {
                AIPlanSnapshot plan = plans[i];
                string label = "#" + plan.number + "  " + plan.startedAt.ToString("F1") + "s  " +
                    plan.completed + "/" + plan.plannedCount + "  " + plan.result;
                if (GUILayout.Button(new GUIContent(label, plan.orderedUnits),
                    displayedPlan == plan ? EditorStyles.toolbarButton : EditorStyles.miniButton))
                {
                    selectedPlanNumber = plan.number;
                    following = false;
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            return;
        }

        int n = historyView.Count;
        Rect area = GUILayoutUtility.GetRect(HistoryWidth - 24f, Mathf.Max(1, n) * HistoryRowHeight, GUILayout.ExpandWidth(true));

        if (Event.current.type != EventType.Layout)
        {
            int first = Mathf.Clamp(Mathf.FloorToInt(historyScroll.y / HistoryRowHeight), 0, n);
            int visible = Mathf.CeilToInt(position.height / HistoryRowHeight) + 2;
            int last = Mathf.Min(n, first + visible);

            for (int i = first; i < last; i++)
            {
                Rect r = new Rect(area.x, area.y + i * HistoryRowHeight, area.width, HistoryRowHeight);
                DrawHistoryRow(r, historyView[i]);
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void DrawHistoryRow(Rect r, AITickSnapshot t)
    {
        if (ReferenceEquals(t, displayed))
            EditorGUI.DrawRect(r, new Color(0.24f, 0.48f, 0.90f, 0.30f));

        EditorGUI.DrawRect(new Rect(r.x, r.y + 1f, 4f, r.height - 2f), TickColor(t));

        string text = "#" + t.tickNumber + "  " + t.gameTime.ToString("F1") + "s  " + t.Summary;
        GUI.Label(new Rect(r.x + 8f, r.y, r.width - 8f, r.height), new GUIContent(text, t.Summary), rowStyle);

        if (GUI.Button(r, GUIContent.none, GUIStyle.none))
        {
            pinned = t;
            following = false; // selecting an older decision freezes the display
        }
    }

    static Color TickColor(AITickSnapshot t)
    {
        if (t.updateStatus != AIUpdateStatus.Ok || t.executeStatus != AIExecuteStatus.RanPasses)
            return OutcomeColor(AIPassOutcome.SpawnFailed);
        if (t.SpawnCount > 0) return OutcomeColor(AIPassOutcome.Spawned);
        AIPassSnapshot last = t.LastPass;
        return last != null ? OutcomeColor(last.outcome) : OutcomeColor(AIPassOutcome.NoValidCandidates);
    }

    // ================================================================ details column

    void DrawDetails()
    {
        EditorGUILayout.BeginVertical();
        detailScroll = EditorGUILayout.BeginScrollView(detailScroll);

        float oldLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 270f;

        if (showOperationHistory && displayedOperation != null)
            DrawOperationDetail(displayedOperation);
        else if (showPlanHistory && displayedPlan != null)
            DrawPlanDetail(displayedPlan);
        else if (displayed == null)
        {
            DrawEmptyState();
        }
        else
        {
            DrawTickHeader(displayed);
            DrawDiagnostics();
            DrawOverview(displayed);
            DrawDecision(displayed);
        }

        EditorGUIUtility.labelWidth = oldLabelWidth;
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void DrawEmptyState()
    {
        string msg;
        if (!Application.isPlaying)
            msg = "Enter Play mode to record AI decisions. History from the previous Play session stays visible until the next one starts.";
        else if (!recording)
            msg = "Recording is switched off. Enable 'Record' in the toolbar to capture AI ticks.";
        else
            msg = "Waiting for the first AI tick... Make sure AILevelHarness.StartAI() has been called (see live state below).";

        EditorGUILayout.HelpBox(msg, MessageType.Info);
        GUILayout.Space(4f);
        DrawLiveState();
    }

    void DrawTickHeader(AITickSnapshot t)
    {
        string mode = following ? "LIVE" : "PAUSED / PINNED";
        GUILayout.Label("Tick #" + t.tickNumber + "   (t = " + t.gameTime.ToString("F2") + "s, frame " + t.frame + ")   [" + mode + "]", EditorStyles.boldLabel);
        GUILayout.Space(2f);
    }

    void DrawDiagnostics()
    {
        if (diagnostics.Count == 0) return;

        GUILayout.Label("Diagnostics", EditorStyles.boldLabel);
        for (int i = 0; i < diagnostics.Count; i++)
        {
            AIDiagnostic d = diagnostics[i];
            MessageType type = d.severity == AIDiagnosticSeverity.Error ? MessageType.Error
                : d.severity == AIDiagnosticSeverity.Warning ? MessageType.Warning
                : MessageType.Info;
            EditorGUILayout.HelpBox(d.message, type);
        }
        GUILayout.Space(4f);
    }

    // ================================================================ overview

    void DrawLiveState()
    {
        Sub("Live state (current values, not part of the recorded tick)");
        if (liveHarness == null)
        {
            Line("AILevelHarness", "none (not in Play mode, or no harness in the scene)");
        }
        else
        {
            Line("AI running", liveHarness.IsRunning ? "RUNNING" : "STOPPED");
            Line("Current phase index", liveHarness.CurrentPhaseIndex.ToString());
            Line("Virtual budget", liveHarness.VirtualBudget.ToString("F2"));
            Line("Tick interval (setting)", liveHarness.tickInterval.ToString("F2") + "s");
        }
        Line("Actual AI money", liveMoneyAvailable ? liveMoney.ToString() : "n/a (Money.AIInstance missing)");
    }

    void DrawOverview(AITickSnapshot t)
    {
        overviewFoldout = EditorGUILayout.Foldout(overviewFoldout, "Overview", true, foldoutBold);
        if (!overviewFoldout) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        DrawLiveState();

        Sub("AI state at decision time");
        Line("Phase", t.phaseName + "   (index " + t.phaseIndex + " of " + Mathf.Max(0, t.phaseCount - 1) + ")");
        Line("Candidates in phase", t.phaseCharacterCount.ToString());
        Line("Tick interval", t.tickInterval.ToString("F2") + "s");
        Line("Decision time (Time.time)", t.gameTime.ToString("F2") + "s   (frame " + t.frame + ")");
        if (Application.isPlaying)
            Line("Time since this decision (live)", (Time.time - t.gameTime).ToString("F2") + "s");

        Sub("Money   (raw)");
        bool moneyKnown = t.updateStatus == AIUpdateStatus.Ok || t.updateStatus == AIUpdateStatus.SkippedMissingActivityCurve;
        Line("Actual AI money (at update)", moneyKnown ? t.actualMoneyAtUpdate.ToString() : "n/a (update skipped)");
        Line("Money gain per second", moneyKnown ? t.moneyGainPerSec.ToString("F2") : "n/a");
        Line("Income this tick  (gain/s x tick interval)", t.updateStatus == AIUpdateStatus.Ok ? t.incomeThisTick.ToString("F2") : "n/a");
        Line("Budget allocated this tick  (income x activity)", t.updateStatus == AIUpdateStatus.Ok ? t.budgetAdded.ToString("F2") : "0 (update skipped)");
        Line("Virtual budget: before -> after update",
            t.updateStatus == AIUpdateStatus.Ok
                ? t.virtualBudgetBeforeUpdate.ToString("F2") + "  ->  " + t.virtualBudgetAfterUpdate.ToString("F2")
                : "n/a");
        Line("Virtual budget / actual money at end of tick", t.virtualBudgetAtEnd.ToString("F2") + "  /  " + t.actualMoneyAtEnd);

        Sub("Threat   (RAW vs NORMALIZED)");
        if (!t.threatEvaluated)
        {
            Line("Threat", "n/a (update skipped before threat was evaluated)");
        }
        else
        {
            Line("[RAW] Global threat level", t.rawThreat.ToString("F2") + "   (+ = player favored, - = AI favored; tracker already applies x100)");
            Line("[RAW] maxThreatLevel (setting)", t.maxThreatLevel.ToString("F2"));
            Line("[RAW] |threat| / maxThreatLevel  (unclamped)", t.unclampedThreatRatio.ToString("F3"));
            Line("[NORMALIZED] Threat ratio fed to curve (0..1)", t.normalizedThreat.ToString("F3"));
            Line("Relative activity mode", t.relativeActivityModeEnabled ? "ENABLED" : "legacy");
            Line("Legacy curve input (0..1)", t.legacyNormalizedThreat.ToString("F3"));
            Line("Relative military advantage (-1..1)", t.relativeMilitaryAdvantage.ToString("F3"));
            Line("Relative activity threat (-1..1)", t.relativeThreatForActivity.ToString("F3"));
            Line("Unweighted military score (x100)", t.militaryAdvantage.ToString("F2"));
            Line("Defensive urgency premium (x100)", t.defensiveUrgencyPressure.ToString("F2"));
        }

        Sub("Activity");
        if (t.updateStatus == AIUpdateStatus.Ok)
        {
            Line("Activity curve selected",
                t.curveKind == AIActivityCurveKind.Disadvantage
                    ? "DISADVANTAGE  (threat >= 0: player favored)"
                    : "ADVANTAGE  (threat < 0: AI favored)");
            Line("Curve evaluation result", t.curveValue.ToString("F3"));
            Line("[RAW] Activity multiplier (hard-coded in harness)", "x" + t.activityMultiplier.ToString("0.##"));
            Line("Activity = curve x multiplier", t.activityBudgetPercent.ToString("F3") + "   (fraction of income added to the virtual budget)");
        }
        else
        {
            Line("Activity", "n/a (" + t.updateStatus + ")");
        }

        if (t.planNumber > 0)
        {
            Sub("Tactical plan at this tick (historical)");
            Line("Plan / stage", "#" + t.planNumber + " / " + t.planStage);
            Line("Purchase step", t.planStep + " / " + t.planLength);
            Line("Waiting / next unit", t.planWaitCategory + " / " + t.planNextUnit);
            Line("Executor reason", t.planWaitReason);
        }

        Sub("Settings in effect");
        Line("maxSpawnsPerTick", t.maxSpawnsPerTick.ToString());
        Line("outcomeUtilityScale (min. outcome denominator)", t.outcomeUtilityScale.ToString("F2"));
        Line("swarmEvaluationWindow / maxSwarmCopies", t.swarmEvaluationWindow.ToString("F1") + "s  /  " + t.maxSwarmCopies);
        Line("aiSpawnZone assigned", t.aiSpawnZoneAssigned ? "yes" : "NO");

        EditorGUILayout.EndVertical();
        GUILayout.Space(6f);
    }

    // ================================================================ decision + passes

    void DrawDecision(AITickSnapshot t)
    {
        GUILayout.Label("Decision", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(AIDecisionDiagnostics.ExplainTick(t), MessageType.None);

        for (int i = 0; i < t.passes.Count; i++)
            DrawPass(t, t.passes[i]);
    }

    void DrawPass(AITickSnapshot t, AIPassSnapshot p)
    {
        bool open;
        if (!passFoldouts.TryGetValue(p.passIndex, out open)) open = true;

        Rect header = GUILayoutUtility.GetRect(1f, 22f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(header, WithAlpha(OutcomeColor(p.outcome), 0.25f));
        open = EditorGUI.Foldout(header, open, "Pass " + (p.passIndex + 1) + ":  " + p.Label, true, foldoutBold);
        passFoldouts[p.passIndex] = open;

        if (!open) return;

        EditorGUILayout.HelpBox(AIDecisionDiagnostics.ExplainPass(t, p), MessageType.None);

        Line("Actual money / virtual budget (start of pass)", p.actualMoneyBefore + "  /  " + p.virtualBudgetBefore.ToString("F2"));
        if (p.candidates.Count > 0)
        {
            Line("[RAW] Baseline expected outcome (AI-weighted, whole map)", p.baselineOutcome.ToString("F2") + "   (+ = player favored)");
            Line("[RAW] Largest |outcome delta| among candidates", p.maxAbsOutcomeDelta.ToString("F2"));
            Line("[NORMALIZED] Outcome denominator",
                p.outcomeDenominator.ToString("F3") + "  =  max(largest delta, outcomeUtilityScale " + t.outcomeUtilityScale.ToString("0.##") + ", 0.001)");
        }

        GUILayout.Space(2f);
        GUILayout.Label("Click a row to inspect that candidate below. Click a column header to sort.", EditorStyles.miniLabel);
        DrawCandidateTable(p);

        AICandidateSnapshot s = p.Selected;
        if (s != null)
        {
            string formula =
                "Selected '" + s.unitName + "':  total = (money " + s.moneyUtility.ToString("F3") + " + cooldown " + s.cooldownUtility.ToString("F3") +
                " + rate " + s.expenditureUtility.ToString("F3") + " + outcome " + s.outcomeUtility.ToString("F3") + ") / 4 = " + s.totalUtility.ToString("F3") + "\n" +
                "outcome utility = clamp(raw delta " + s.outcomeRaw.ToString("F3") + " / denominator " + p.outcomeDenominator.ToString("F3") + ", -1, 1) = " +
                s.outcomeUtility.ToString("F3") + "     (baseline " + s.baselineOutcome.ToString("F2") + " - with " + s.swarmCopies + "x candidate " +
                s.outcomeWithCandidate.ToString("F2") + ")";
            EditorGUILayout.HelpBox(formula, MessageType.None);
        }

        DrawCandidateDetails(t, p);

        if (p.skippedCandidates.Count > 0)
            EditorGUILayout.HelpBox("Skipped (invalid cost/cooldown): " + string.Join(", ", p.skippedCandidates.ToArray()), MessageType.Warning);

        Line("Virtual budget / actual money after pass", p.virtualBudgetAfter.ToString("F2") + "  /  " + p.actualMoneyAfter);
        GUILayout.Space(8f);
    }

    // ================================================================ candidate table

    void DrawCandidateTable(AIPassSnapshot p)
    {
        if (p.candidates.Count == 0) return;

        EditorGUILayout.BeginHorizontal();
        for (int col = 0; col < ColWidths.Length; col++)
        {
            string text = ColHeaders[col] + (col == sortColumn ? (sortDescending ? " v" : " ^") : "");
            if (GUILayout.Button(new GUIContent(text, ColTips[col]), EditorStyles.toolbarButton, GUILayout.Width(ColWidths[col])))
            {
                if (sortColumn == col)
                {
                    sortDescending = !sortDescending;
                }
                else
                {
                    sortColumn = col;
                    sortDescending = !(col == ColUnit || col == ColRank);
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        List<AICandidateSnapshot> rows = SortCandidates(p.candidates);
        float totalWidth = TableWidth;
        for (int i = 0; i < rows.Count; i++)
        {
            Rect r = GUILayoutUtility.GetRect(totalWidth, TableRowHeight, GUILayout.ExpandWidth(false));
            DrawCandidateRow(r, rows[i], i % 2 == 1, p);
        }
    }

    List<AICandidateSnapshot> SortCandidates(List<AICandidateSnapshot> src)
    {
        if (sortColumn == ColUnit)
        {
            return (sortDescending ? src.OrderByDescending(c => c.unitName) : src.OrderBy(c => c.unitName)).ToList();
        }

        int col = sortColumn;
        return (sortDescending ? src.OrderByDescending(c => NumericKey(c, col)) : src.OrderBy(c => NumericKey(c, col))).ToList();
    }

    static float NumericKey(AICandidateSnapshot c, int col)
    {
        switch (col)
        {
            case ColRank: return c.rank;
            case ColSel: return c.isSelected ? 1f : 0f;
            case ColCost: return c.spawnCost;
            case ColActual: return c.affordableActual ? 1f : 0f;
            case ColBudget: return c.affordableVirtual ? 1f : 0f;
            case ColCooldown: return c.spawnCooldown;
            case ColCdLeft: return c.cooldownRemaining;
            case ColCopies: return c.swarmCopies;
            case ColMoneyU: return c.moneyUtility;
            case ColCooldownU: return c.cooldownUtility;
            case ColRateU: return c.expenditureUtility;
            case ColRawDelta: return c.outcomeRaw;
            case ColOutcomeU: return c.outcomeUtility;
            case ColTotal: return c.totalUtility;
        }
        return 0f;
    }

    void DrawCandidateRow(Rect r, AICandidateSnapshot c, bool alternate, AIPassSnapshot pass)
    {
        if (c.isSelected)
            EditorGUI.DrawRect(r, WithAlpha(OutcomeColor(pass.outcome), 0.35f));
        else if (alternate)
            EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 0.08f));

        if (ReferenceEquals(c, FindInspected(pass)))
        {
            Color oc = new Color(0.30f, 0.60f, 1.00f, 0.95f);
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1f), oc);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1f, r.width, 1f), oc);
            EditorGUI.DrawRect(new Rect(r.x, r.y, 1f, r.height), oc);
            EditorGUI.DrawRect(new Rect(r.xMax - 1f, r.y, 1f, r.height), oc);
        }

        // Drawn before the cells so it never sits on top of anything else.
        if (GUI.Button(r, GUIContent.none, GUIStyle.none))
            inspectedUnit = c.unitName;

        float x = r.x;
        for (int col = 0; col < ColWidths.Length; col++)
        {
            DrawCell(new Rect(x, r.y, ColWidths[col], r.height), c, col);
            x += ColWidths[col];
        }
    }

    AICandidateSnapshot FindInspected(AIPassSnapshot p)
    {
        if (!string.IsNullOrEmpty(inspectedUnit))
        {
            for (int i = 0; i < p.candidates.Count; i++)
                if (p.candidates[i].unitName == inspectedUnit) return p.candidates[i];
        }
        if (p.Selected != null) return p.Selected;
        return p.candidates.Count > 0 ? p.candidates[0] : null;
    }

    void DrawCell(Rect cell, AICandidateSnapshot c, int col)
    {
        GUIStyle style = c.isSelected ? cellBold : cellStyle;
        string text = "";

        switch (col)
        {
            case ColRank: text = c.rank.ToString(); break;
            case ColSel: text = c.isSelected ? "YES" : ""; break;
            case ColUnit:
                GUI.Label(cell, new GUIContent(c.unitName, c.unitName), style);
                return;
            case ColCost: text = c.spawnCost.ToString("0.##"); break;
            case ColActual: text = c.affordableActual ? "Yes" : "No"; style = c.affordableActual ? cellYes : cellNo; break;
            case ColBudget: text = c.affordableVirtual ? "Yes" : "No"; style = c.affordableVirtual ? cellYes : cellNo; break;
            case ColCooldown: text = c.spawnCooldown.ToString("0.##") + "s"; break;
            case ColCdLeft: text = c.cooldownRemaining.ToString("F2") + "s"; break;
            case ColCopies: text = c.swarmCopies + "x"; break;
            case ColMoneyU: text = c.moneyUtility.ToString("F3"); break;
            case ColCooldownU: text = c.cooldownUtility.ToString("F3"); break;
            case ColRateU: text = c.expenditureUtility.ToString("F3"); break;
            case ColRawDelta: text = c.outcomeRaw.ToString("F2"); break;
            case ColOutcomeU: text = c.outcomeUtility.ToString("F3"); break;
            case ColTotal: text = c.totalUtility.ToString("F3"); style = cellBold; break;
        }

        GUI.Label(cell, text, style);
    }

    // ================================================================ candidate details (Stage 1.5)

    void DrawCandidateDetails(AITickSnapshot t, AIPassSnapshot p)
    {
        AICandidateSnapshot c = FindInspected(p);
        if (c == null) return;

        string title = "Candidate details: " + c.unitName + (c.isSelected ? "   [SELECTED]" : "   [not selected]");
        detailsFoldout = EditorGUILayout.Foldout(detailsFoldout, title, true, foldoutBold);
        if (!detailsFoldout) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // ---- utility breakdown -------------------------------------------------------
        Sub("Utility breakdown   (the AI's score is the plain average of four utilities)");
        DrawTermRow("Money utility", c.moneyUtility, c.ContribMoney, c.ShareOfTotal(c.ContribMoney));
        DrawTermRow("Cooldown utility", c.cooldownUtility, c.ContribCooldown, c.ShareOfTotal(c.ContribCooldown));
        DrawTermRow("Expenditure-rate utility", c.expenditureUtility, c.ContribRate, c.ShareOfTotal(c.ContribRate));
        DrawTermRow("Outcome utility", c.outcomeUtility, c.ContribOutcome, c.ShareOfTotal(c.ContribOutcome));
        Line("TOTAL utility", c.totalUtility.ToString("F3") + "   (rank " + c.rank + " of " + p.candidates.Count + ")");

        // ---- raw expected outcome ----------------------------------------------------
        Sub("Raw expected outcome   (tracker scale; + = player favored, - = AI favored)");
        Line("Baseline (current battlefield)", c.baselineOutcome.ToString("F2"));
        Line("With " + c.swarmCopies + " virtual cop" + (c.swarmCopies == 1 ? "y" : "ies") + " of this unit", c.outcomeWithCandidate.ToString("F2"));
        Line("Raw predicted improvement  (baseline - with)", c.outcomeRaw.ToString("F2") + "   (+ = improves the AI's position)");

        // ---- normalized utility ------------------------------------------------------
        Sub("Normalized utility");
        Line("Outcome denominator", p.outcomeDenominator.ToString("F3") + "   (set by " + p.DenominatorSource(t.outcomeUtilityScale) + ")");
        Line("Outcome utility  (raw / denominator)", c.outcomeUtility.ToString("F3"));
        Line("Has the largest |raw| in this pass", YesNo(p.IsPassMaxDelta(c)));
        Line("Outcome utility saturated at 1.0", YesNo(c.OutcomeSaturated));

        // ---- marginal gain -----------------------------------------------------------
        Sub("Marginal gain");
        Line("Virtual copies evaluated",
            c.swarmCopies + (c.UsedMultipleCopies
                ? "   (the raw improvement covers ALL copies; only one unit is spawned per pass)"
                : "   (single unit)"));
        Line("Raw improvement, all copies", c.outcomeRaw.ToString("F2"));
        Line("Raw improvement per copy", Fx(c.ImprovementPerCopy, "F2"));

        // ---- economic efficiency -----------------------------------------------------
        Sub("Economic efficiency   (diagnostic only - the AI does not use it)");
        Line("Spawn cost / hypothetical swarm cost", c.spawnCost.ToString("0.##") + "  /  " + c.committedCost.ToString("0.##"));
        Line("Improvement per unit cost  (raw / cost)", Fx(c.ImprovementPerUnitCost, "F2"));
        Line("Improvement per swarm cost  (raw / (cost x copies))", Fx(c.ImprovementPerSwarmCost, "F2"));
        AICandidateSnapshot eff = p.MostEfficientCandidate;
        Line("Efficiency rank in this pass",
            EfficiencyRank(p, c) + " of " + p.candidates.Count + (eff != null ? "   (most efficient: " + eff.unitName + ")" : ""));

        // ---- affordability -----------------------------------------------------------
        Sub("Real affordability   (actual money)");
        Line("Actual money", c.actualMoney.ToString());
        Line("Can pay for 1 unit", YesNo(c.affordableActual));
        Line("Can pay for the hypothetical swarm", YesNo(c.AffordableSwarmActual));

        Sub("Virtual-budget affordability   (what the AI is currently allowed to spend)");
        Line("Virtual budget", c.virtualBudget.ToString("F2"));
        Line("1 unit fits  (the AI's own over-budget test)", YesNo(c.affordableVirtual));
        Line("Hypothetical swarm fits", YesNo(c.AffordableSwarmVirtual));

        // ---- margin ------------------------------------------------------------------
        AICandidateSnapshot sel = p.Selected;
        if (sel != null && !ReferenceEquals(sel, c))
        {
            Sub("Gap to the selected candidate (" + sel.unitName + ")");
            DrawMargin(c, sel, "Selected minus this one");
        }
        else if (c.isSelected && p.RunnerUp != null)
        {
            Sub("Why it beat the runner-up (" + p.RunnerUp.unitName + ")");
            DrawMargin(c, p.RunnerUp, "This one minus runner-up");
        }

        // ---- combat profile ----------------------------------------------------------
        GUILayout.Space(4f);
        profileFoldout = EditorGUILayout.Foldout(profileFoldout, "Combat profile   (first-seen snapshot)", true, foldoutBold);
        if (profileFoldout) DrawUnitProfile(c);

        EditorGUILayout.EndVertical();
        GUILayout.Space(2f);
    }

    // "a" - "b" per term, expressed as score contribution (utility / 4)
    void DrawMargin(AICandidateSnapshot a, AICandidateSnapshot b, string caption)
    {
        Line("Money term", Signed(a.ContribMoney - b.ContribMoney));
        Line("Cooldown term", Signed(a.ContribCooldown - b.ContribCooldown));
        Line("Expenditure-rate term", Signed(a.ContribRate - b.ContribRate));
        Line("Outcome term", Signed(a.ContribOutcome - b.ContribOutcome));
        Line("Total score difference   (" + caption + ")", Signed(a.totalUtility - b.totalUtility));
    }

    void DrawTermRow(string label, float utility, float contribution, float share)
    {
        Rect r = GUILayoutUtility.GetRect(1f, 18f, GUILayout.ExpandWidth(true));
        float x = r.x;

        GUI.Label(new Rect(x, r.y, 180f, r.height), label, cellStyle);
        x += 180f;
        GUI.Label(new Rect(x, r.y, 60f, r.height), utility.ToString("F3"), cellBold);
        x += 62f;

        Rect bar = new Rect(x, r.y + 4f, 180f, r.height - 8f);
        EditorGUI.DrawRect(bar, new Color(0.5f, 0.5f, 0.5f, 0.20f));
        float fill = Mathf.Clamp01(Mathf.Abs(utility));
        EditorGUI.DrawRect(new Rect(bar.x, bar.y, bar.width * fill, bar.height),
            utility >= 0f ? new Color(0.30f, 0.70f, 0.40f, 0.9f) : new Color(0.85f, 0.30f, 0.30f, 0.9f));
        x += 190f;

        string pct = float.IsNaN(share) ? "n/a" : (share * 100f).ToString("F0") + "%";
        GUI.Label(new Rect(x, r.y, 300f, r.height), "adds " + contribution.ToString("F3") + " to the score  (" + pct + " of total)", cellStyle);
    }

    void DrawUnitProfile(AICandidateSnapshot c)
    {
        AIUnitSnapshot u = c.unit;
        if (u == null)
        {
            EditorGUILayout.HelpBox("No stats snapshot for this candidate (the unit asset reference was missing).", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        Line("First observed", "tick #" + u.firstSeenTick + ", t = " + u.firstSeenTime.ToString("F1") + "s   (later asset edits are not reflected here)");
        Line("Theme / RPS type / ODS", Txt(u.theme) + "  /  " + Txt(u.rpsType) + "  /  " + Txt(u.ods));
        Line("Spawn cost / cooldown", u.spawnCost.ToString("0.##") + "  /  " + u.spawnCooldown.ToString("0.##") + "s");
        Line("Calculated power / value discrepancy", u.calculatedPower.ToString("F2") + "  /  " + u.valueDiscrepancy.ToString("F2") + "   (stored in the asset by the balance tools; may be stale)");

        GUILayout.Space(2f);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("", GUILayout.Width(EditorGUIUtility.labelWidth));
        GUILayout.Label("AI model (PowerMath)", EditorStyles.boldLabel, GUILayout.Width(150f));
        GUILayout.Label("Balance sim", EditorStyles.boldLabel, GUILayout.Width(110f));
        EditorGUILayout.EndHorizontal();
        CmpRow("Attack cycle (s)", u.aiCycleSec, u.balsimCycleSec);
        CmpRow("DPS", u.aiDps, u.balsimDps);
        CmpRow("Stopping range", u.aiRange, u.balsimStoppingRange);

        GUILayout.Space(2f);
        Line("Max health / knockback durability", u.maxHealth.ToString("F1") + "  /  " + u.knockbackMaxHealth.ToString("F1"));
        Line("Attack damage (raw / AI per hit)", u.attackDamage.ToString("F1") + "  /  " + u.aiAtk.ToString("F1"));
        Line("Knockback damage (raw / AI)", u.knockbackDamage.ToString("F1") + "  /  " + u.aiKnockbackDamage.ToString("F1"));
        Line("Move speed", u.moveSpeed.ToString("F2"));
        Line("Horizontal range (raw)", u.horizontalRange.ToString("F2"));
        Line("Max attack targets (AI model)", u.aiMaxTargets == int.MaxValue ? "unlimited" : u.aiMaxTargets.ToString());
        Line("AOE (AI model)", YesNo(u.aiIsAoe));
        Line("Support / utility power", u.utilityPower.ToString("F2"));

        if (u.hasPrimaryAction)
        {
            Line("Primary action",
                u.primaryKind + ";  health change " + u.primaryHealthChangePercent.ToString("0.##") + (u.primaryScalesWithUserStats ? " (x attack stat)" : " (flat)") +
                ";  knockback " + u.primaryKnockbackPercent.ToString("0.##") + ";  cast cooldown x" + u.primaryCastCooldown.ToString("0.##"));
            Line("Primary action targeting",
                "maxTargets raw/normalized " + u.primaryMaxTargetsRaw + "/" + (u.primaryMaxTargetsNormalized == int.MaxValue ? "unlimited" : u.primaryMaxTargetsNormalized.ToString()) +
                ";  projectile " + YesNo(u.primaryUseProjectile) + ";  zone data " + YesNo(u.primaryHasZoneData) +
                ";  targets friendly " + YesNo(u.primaryTargetFriendly) + ";  effects on hit " + u.primaryEffectsOnHitCount);
        }
        else
        {
            Line("Primary action", "none found (PowerMath falls back to raw stats)");
        }

        if (u.hasStoppingAction)
        {
            Line("Stopping-range action",
                (u.stoppingSameAsPrimary ? "same as primary" : "different action") + ";  defines stopping range " + YesNo(u.stoppingDefinesStoppingRange) +
                ";  range x" + u.stoppingRangePercent.ToString("0.##") + (u.stoppingExtendsForward ? " (extends forward, x1.5)" : ""));
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Ping unit asset", GUILayout.Width(110f)) && c.unitAsset != null)
            EditorGUIUtility.PingObject(c.unitAsset);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    static void CmpRow(string label, float aiValue, float simValue)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(EditorGUIUtility.labelWidth));
        GUILayout.Label(Fx(aiValue, "F2"), GUILayout.Width(150f));
        GUILayout.Label(Fx(simValue, "F2"), GUILayout.Width(110f));
        EditorGUILayout.EndHorizontal();
    }

    static int EfficiencyRank(AIPassSnapshot p, AICandidateSnapshot c)
    {
        float v = c.ImprovementPerSwarmCost;
        if (float.IsNaN(v)) return p.candidates.Count;
        int rank = 1;
        for (int i = 0; i < p.candidates.Count; i++)
        {
            float o = p.candidates[i].ImprovementPerSwarmCost;
            if (!float.IsNaN(o) && o > v) rank++;
        }
        return rank;
    }

    static string Fx(float v, string format)
    {
        return (float.IsNaN(v) || float.IsInfinity(v)) ? "n/a" : v.ToString(format);
    }

    static string Signed(float v)
    {
        return (v >= 0f ? "+" : "") + v.ToString("F3");
    }

    static string YesNo(bool v) { return v ? "Yes" : "No"; }

    static string Txt(string s) { return string.IsNullOrEmpty(s) ? "-" : s; }

    // ================================================================ unified plan diagnostics

    void DrawOperationDetail(AIOperationalObservation o)
    {
        GUILayout.Label("Operational shadow evaluation #" + o.id + "  at " + o.time.ToString("F2") + "s", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("OBSERVATION ONLY. The selected shadow proposal did not control purchases, upgrades, plan commitment, or lane routing. These utilities are provisional, not calibrated predictions of winning or capturing a location.", MessageType.Info);
        Line("Current tactical plan", "#" + o.planNumber);
        Line("Phase / eligible roster", o.phase + " / " + o.eligibleCount);
        Line("Money / virtual budget / income", o.money.ToString("F1") + " / " + o.virtualBudget.ToString("F1") + " / " + o.incomeRate.ToString("F2") + "/s");
        Line("Player money / income", o.playerMoney.ToString("F1") + " / " + o.playerIncome.ToString("F2") + "/s");
        Line("Initiative / positional / patience", o.initiative.ToString("F2") + " / " + o.positionalValuation.ToString("F2") + " / " + o.economicPatience.ToString("F2"));
        Line("Highest provisional utility", o.chosen);
        GUILayout.Space(5f);
        GUILayout.Label("Every proposal (ranked, historical values)", EditorStyles.boldLabel);
        foreach (AIOperationCandidate p in o.proposals)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField((p.selected ? "[TOP SHADOW] " : "") + p.objective + " | " + p.target + " | " + p.unit, EditorStyles.boldLabel);
            Line("Maneuver", p.maneuver);
            Line("Utility / military / denial / economic", p.score.ToString("F3") + " / " + p.militaryBenefit.ToString("F3") + " / " + p.objectiveBenefit.ToString("F3") + " / " + p.economicBenefit.ToString("F3"));
            Line("Baseline -> hypothetical / gain (x100)", p.baseline.ToString("F2") + " -> " + p.hypothetical.ToString("F2") + " / " + p.gain.ToString("F2"));
            Line("Cost / estimated funding delay", p.cost.ToString("F1") + " / " + p.estimatedWait.ToString("F1") + "s");
            Line("Units (AI / player) / static incentive", p.enemyCount + " / " + p.playerCount + " / " + p.incentive.ToString("F2"));
            EditorGUILayout.LabelField(p.caveat, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }
    }

    void DrawPlanDetail(AIPlanSnapshot p)
    {
        GUILayout.Label("Plan #" + p.number + " — " + p.result, EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        Line("Created at / phase", p.startedAt.ToString("F2") + "s / " + p.phase);
        Line("Objective / source", p.objective + " / " + p.selectionSource);
        Line("Ordered units", p.orderedUnits);
        Line("Purchased", p.completed + " / " + p.plannedCount);
        Line("Initiative / patience / wave", p.initiative.ToString("F2") + " / " +
            p.economicPatience.ToString("F2") + " / " + p.waveConcentration.ToString("F2"));
        Line("Counterplay", p.counterplayStatus);
        Line("Money / budget at selection", p.initialMoney.ToString("F1") + " / " + p.initialBudget.ToString("F1"));
        if (!string.IsNullOrEmpty(p.explanation))
            EditorGUILayout.HelpBox(p.explanation, MessageType.None);
        EditorGUILayout.EndVertical();

        Sub("Strategic Director — captured at selection");
        EditorGUILayout.HelpBox(string.IsNullOrEmpty(p.directorReason)
            ? "Manual plan: no strategic director ranking." : p.directorReason, MessageType.None);
        if (!string.IsNullOrEmpty(p.initiativeExplanation))
            EditorGUILayout.HelpBox(p.initiativeExplanation, MessageType.None);
        planCandidatesFoldout = EditorGUILayout.Foldout(planCandidatesFoldout,
            "All evaluated foundation pairs (" + p.candidates.Count + ")", true, foldoutBold);
        if (planCandidatesFoldout)
        {
            var ranked = new List<AIPlanCandidateSnapshot>(p.candidates);
            ranked.Sort((a, b) => a.finalRank.CompareTo(b.finalRank));
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Final rank | pair | cost | doctrine | counter | raw gain | final", EditorStyles.miniBoldLabel);
            foreach (AIPlanCandidateSnapshot c in ranked)
            {
                string line = c.finalRank + ". " + c.first + " -> " + c.second +
                    (c.selected ? " [SELECTED]" : "") +
                    "  cost " + c.cost.ToString("F0") +
                    "  doctrine " + c.doctrine.ToString("F3") +
                    "  counter " + c.counterNormalized.ToString("F3") +
                    "  gain " + c.counterGain.ToString("F2") +
                    "  final " + c.finalScore.ToString("F3");
                GUILayout.Label(line, EditorStyles.wordWrappedMiniLabel);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.HelpBox("Doctrine and counter ranks for every candidate, including those outside the top three, are retained in _plan_candidates.csv.", MessageType.Info);
        }

        planEventsFoldout = EditorGUILayout.Foldout(planEventsFoldout,
            "Execution events (" + p.events.Count + ")", true, foldoutBold);
        if (planEventsFoldout)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            foreach (AIPlanEventSnapshot e in p.events)
                GUILayout.Label(e.time.ToString("F2") + "s [" + e.kind + "] " +
                    e.stage + " " + e.completed + "/" + e.planned + " — " + e.message,
                    EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }

        planTickFoldout = EditorGUILayout.Foldout(planTickFoldout,
            "Funding and wait timeline (" + p.ticks.Count + " samples)", true, foldoutBold);
        if (planTickFoldout)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (p.ticks.Count > 0)
            {
                AIPlanTickSnapshot latest = p.ticks[p.ticks.Count - 1];
                Line("Last stage / wait cause", latest.stage + " / " + latest.waitCategory);
                Line("Next purchase / cost", latest.nextUnit + " / " + latest.nextCost.ToString("F1"));
                Line("Actual / virtual", latest.money.ToString("F1") + " / " + latest.virtualBudget.ToString("F1"));
                Line("Shortfall actual / virtual", latest.remainingActual.ToString("F1") + " / " + latest.remainingVirtual.ToString("F1"));
                Line("Latest reason", latest.reason);
            }
            GUILayout.Label("Per-tick records, forecasts and wait categories are exported in _plan_ticks.csv.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }
    }

    // ================================================================ small helpers

    static void Sub(string text)
    {
        GUILayout.Space(4f);
        GUILayout.Label(text, EditorStyles.boldLabel);
    }

    static void Line(string label, string value)
    {
        EditorGUILayout.LabelField(new GUIContent(label), new GUIContent(value));
    }

    static Color OutcomeColor(AIPassOutcome o)
    {
        switch (o)
        {
            case AIPassOutcome.Spawned: return new Color(0.30f, 0.75f, 0.35f);
            case AIPassOutcome.SavingOverBudget: return new Color(0.95f, 0.60f, 0.20f);
            case AIPassOutcome.WaitingOnCooldown: return new Color(0.90f, 0.80f, 0.25f);
            case AIPassOutcome.SpawnFailed: return new Color(0.85f, 0.25f, 0.25f);
            default: return new Color(0.55f, 0.55f, 0.55f);
        }
    }

    static Color WithAlpha(Color c, float a)
    {
        c.a = a;
        return c;
    }
}