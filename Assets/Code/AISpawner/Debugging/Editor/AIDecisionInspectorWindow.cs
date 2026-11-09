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
        "Unit asset name. Click to ping the asset.",
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
    [SerializeField] bool onlySpawnTicks = false;
    [SerializeField] bool overviewFoldout = true;
    [SerializeField] int sortColumn = ColTotal;
    [SerializeField] bool sortDescending = true;
    [SerializeField] int capacity = AIDecisionRecorder.DefaultCapacity;

    // ---------------------------------------------------------------- transient state
    Vector2 historyScroll;
    Vector2 detailScroll;
    AITickSnapshot pinned;
    AITickSnapshot displayed;          // cached on Layout so Layout/Repaint see identical data
    AITickSnapshot diagnosticsFor;
    readonly List<AIDiagnostic> diagnostics = new List<AIDiagnostic>();
    readonly List<AITickSnapshot> historyView = new List<AITickSnapshot>();
    readonly Dictionary<int, bool> passFoldouts = new Dictionary<int, bool>();

    AILevelHarness liveHarness;
    bool liveMoneyAvailable;
    int liveMoney;

    int lastVersion = -1;
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

        bool dirty = AIDecisionRecorder.Version != lastVersion;
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

        onlySpawnTicks = GUILayout.Toggle(onlySpawnTicks, "Spawn ticks only", EditorStyles.toolbarButton, GUILayout.Width(110f));
        showHistory = GUILayout.Toggle(showHistory, "History list", EditorStyles.toolbarButton, GUILayout.Width(80f));

        GUILayout.FlexibleSpace();
        GUILayout.Label(
            AIDecisionRecorder.Count + "/" + AIDecisionRecorder.Capacity + " ticks   |   " + (following ? "LIVE" : "PAUSED (display frozen, AI unaffected)"),
            EditorStyles.miniLabel);

        EditorGUILayout.EndHorizontal();
    }

    // ================================================================ history list

    void DrawHistory()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(HistoryWidth));
        GUILayout.Label("History (newest first)", EditorStyles.boldLabel);

        historyScroll = EditorGUILayout.BeginScrollView(historyScroll, GUILayout.Width(HistoryWidth));

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

        if (displayed == null)
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

        float x = r.x;
        for (int col = 0; col < ColWidths.Length; col++)
        {
            DrawCell(new Rect(x, r.y, ColWidths[col], r.height), c, col);
            x += ColWidths[col];
        }
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
                if (c.unitAsset != null && GUI.Button(cell, new GUIContent("", "Click to ping the asset"), GUIStyle.none))
                    EditorGUIUtility.PingObject(c.unitAsset);
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