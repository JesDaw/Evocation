// Editor-only. Exports the recorder's current history to four CSV files in
// <project root>/AIDecisionExports/ :
//
//   <stamp>_ticks.csv       one row per AI tick                      (existing columns kept, new ones appended)
//   <stamp>_candidates.csv  one row per candidate per spawn pass     (existing columns kept, new ones appended)
//   <stamp>_units.csv       one row per unique unit asset            (NEW)
//   <stamp>_session.csv     key,value rows: provenance and settings  (NEW)
//
// Read-only with respect to the game and to assets: nothing is saved, modified or recalculated with a new model.
// Historical values come from snapshots captured while recording; the only values read at export time are the
// columns prefixed "exportTime_" (used to flag assets edited after they were first observed).
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using AIDecisionTrace;

public static class AIDecisionExporter
{
    public const int SchemaVersion = 4;

    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    [MenuItem("Window/AI/Export Decision History")]
    public static void Export()
    {
        int n = AIDecisionRecorder.Count;
        if (n == 0)
        {
            EditorUtility.DisplayDialog("AI Decision Export", "No recorded ticks to export.", "OK");
            return;
        }

        try
        {
            ExportInternal(n);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[AI Inspector] Export failed: " + e);
            EditorUtility.DisplayDialog("AI Decision Export", "Export failed:\n" + e.Message, "OK");
        }
    }

    static void ExportInternal(int n)
    {
        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "AIDecisionExports"));
        Directory.CreateDirectory(dir);
        string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss", Inv);

        string ticksName = stamp + "_ticks.csv";
        string candName = stamp + "_candidates.csv";
        string unitsName = stamp + "_units.csv";
        string sessionName = stamp + "_session.csv";

        Dictionary<Object, string> guidCache = new Dictionary<Object, string>();

        // ------------------------------------------------------------ ticks
        CsvTable ticks = new CsvTable(
            // --- existing columns (order unchanged) ---
            "tick", "time", "frame", "phase", "phaseIndex", "tickInterval", "updateStatus", "actualMoney", "moneyGainPerSec", "income",
            "budgetBefore", "budgetAdded", "budgetAfter", "rawThreat", "maxThreat", "unclampedRatio", "normalizedThreat", "curve",
            "curveValue", "multiplier", "activity", "executeStatus", "passes", "spawns", "tickEnd", "budgetAtEnd", "moneyAtEnd",
            "stallStreak", "summary",
            // --- appended in schema 2 ---
            "phaseCharacterCount", "outcomeUtilityScale", "maxSpawnsPerTick", "swarmEvaluationWindow", "maxSwarmCopies",
            "aiSpawnZoneAssigned", "stallUnit",
            // --- appended in schema 3 ---
            "legacyNormalizedThreat", "relativeMilitaryAdvantage", "relativeThreatForActivity",
            "relativeActivityModeEnabled", "militaryAdvantage", "defensiveUrgencyPressure",
            // --- appended in schema 4 ---
            "planNumber", "planStage", "planStep", "planLength", "planWaitCategory", "planNextUnit", "planWaitReason");

        // ------------------------------------------------------------ candidates
        CsvTable cands = new CsvTable(
            // --- existing columns (order unchanged) ---
            "tick", "time", "pass", "passOutcome", "unit", "selected", "rank", "cost", "cooldown", "cooldownLeft", "actualMoney",
            "virtualBudget", "affordActual", "affordVirtual", "copies", "committedCost", "moneyU", "cooldownU", "rateU",
            "baselineOutcome", "outcomeWithCandidate", "rawDelta", "outcomeU", "total", "maxAbsDelta", "denominator",
            // --- appended in schema 2 ---
            "unitGuid", "improvementPerUnitCost", "improvementPerSwarmCost", "improvementPerCopy", "usedMultipleCopies",
            "contribMoney", "contribCooldown", "contribRate", "contribOutcome",
            "shareMoney", "shareCooldown", "shareRate", "shareOutcome",
            "outcomeIsPassMax", "outcomeSaturated", "denominatorSource", "totalGapToSelected",
            "affordSwarmActual", "affordSwarmVirtual", "rawDeltaRank", "efficiencyRank");

        // aggregates for the units file
        Dictionary<string, UnitAgg> units = new Dictionary<string, UnitAgg>();

        int firstTick = int.MaxValue, lastTick = int.MinValue;
        float firstTime = float.MaxValue, lastTime = float.MinValue;
        float deltaSum = 0f;
        int deltaCount = 0;
        float prevTime = float.NaN;

        for (int i = 0; i < n; i++)
        {
            AITickSnapshot t = AIDecisionRecorder.GetFromOldest(i);
            if (t == null) continue;

            if (t.tickNumber < firstTick) firstTick = t.tickNumber;
            if (t.tickNumber > lastTick) lastTick = t.tickNumber;
            if (t.gameTime < firstTime) firstTime = t.gameTime;
            if (t.gameTime > lastTime) lastTime = t.gameTime;
            if (!float.IsNaN(prevTime)) { deltaSum += t.gameTime - prevTime; deltaCount++; }
            prevTime = t.gameTime;

            ticks.Add(
                I(t.tickNumber), N(t.gameTime), I(t.frame), T(t.phaseName), I(t.phaseIndex), N(t.tickInterval), T(t.updateStatus.ToString()),
                I(t.actualMoneyAtUpdate), N(t.moneyGainPerSec), N(t.incomeThisTick),
                N(t.virtualBudgetBeforeUpdate), N(t.budgetAdded), N(t.virtualBudgetAfterUpdate),
                N(t.rawThreat), N(t.maxThreatLevel), N(t.unclampedThreatRatio), N(t.normalizedThreat), T(t.curveKind.ToString()),
                N(t.curveValue), N(t.activityMultiplier), N(t.activityBudgetPercent), T(t.executeStatus.ToString()),
                I(t.passes.Count), I(t.SpawnCount), T(t.tickEnd.ToString()), N(t.virtualBudgetAtEnd), I(t.actualMoneyAtEnd),
                I(t.stallStreakTicks), T(t.Summary),
                I(t.phaseCharacterCount), N(t.outcomeUtilityScale), I(t.maxSpawnsPerTick), N(t.swarmEvaluationWindow), I(t.maxSwarmCopies),
                B(t.aiSpawnZoneAssigned), T(t.stallUnitName),
                N(t.legacyNormalizedThreat), N(t.relativeMilitaryAdvantage), N(t.relativeThreatForActivity),
                B(t.relativeActivityModeEnabled), N(t.militaryAdvantage), N(t.defensiveUrgencyPressure),
                I(t.planNumber), T(t.planStage), I(t.planStep), I(t.planLength),
                T(t.planWaitCategory), T(t.planNextUnit), T(t.planWaitReason));

            for (int pi = 0; pi < t.passes.Count; pi++)
            {
                AIPassSnapshot p = t.passes[pi];
                AICandidateSnapshot sel = p.Selected;
                string denomSource = p.DenominatorSource(t.outcomeUtilityScale);

                int[] rawRank = RankBy(p.candidates, c => c.outcomeRaw);
                int[] effRank = RankBy(p.candidates, c => c.ImprovementPerSwarmCost);

                for (int ci = 0; ci < p.candidates.Count; ci++)
                {
                    AICandidateSnapshot c = p.candidates[ci];
                    string guid = GuidOf(c.unitAsset, guidCache);

                    cands.Add(
                        I(t.tickNumber), N(t.gameTime), I(p.passIndex + 1), T(p.outcome.ToString()), T(c.unitName), B(c.isSelected), I(c.rank),
                        N(c.spawnCost), N(c.spawnCooldown), N(c.cooldownRemaining), I(c.actualMoney), N(c.virtualBudget),
                        B(c.affordableActual), B(c.affordableVirtual), I(c.swarmCopies), N(c.committedCost),
                        N(c.moneyUtility), N(c.cooldownUtility), N(c.expenditureUtility),
                        N(c.baselineOutcome), N(c.outcomeWithCandidate), N(c.outcomeRaw), N(c.outcomeUtility), N(c.totalUtility),
                        N(p.maxAbsOutcomeDelta), N(p.outcomeDenominator),
                        T(guid), N(c.ImprovementPerUnitCost), N(c.ImprovementPerSwarmCost), N(c.ImprovementPerCopy), B(c.UsedMultipleCopies),
                        N(c.ContribMoney), N(c.ContribCooldown), N(c.ContribRate), N(c.ContribOutcome),
                        N(c.ShareOfTotal(c.ContribMoney)), N(c.ShareOfTotal(c.ContribCooldown)), N(c.ShareOfTotal(c.ContribRate)), N(c.ShareOfTotal(c.ContribOutcome)),
                        B(p.IsPassMaxDelta(c)), B(c.OutcomeSaturated), T(denomSource),
                        sel != null ? N(c.totalUtility - sel.totalUtility) : "",
                        B(c.AffordableSwarmActual), B(c.AffordableSwarmVirtual), I(rawRank[ci]), I(effRank[ci]));

                    // unit aggregation
                    string key = !string.IsNullOrEmpty(guid) ? guid : "name:" + c.unitName;
                    UnitAgg agg;
                    if (!units.TryGetValue(key, out agg))
                    {
                        agg = new UnitAgg { guid = guid, name = c.unitName, asset = c.unitAsset, snapshot = c.unit };
                        units[key] = agg;
                    }
                    if (agg.snapshot == null) agg.snapshot = c.unit;
                    agg.timesEvaluated++;
                    if (c.isSelected)
                    {
                        agg.timesSelected++;
                        if (p.outcome == AIPassOutcome.Spawned) agg.timesSpawned++;
                    }
                }
            }
        }

        // ------------------------------------------------------------ units
        CsvTable unitsTable = new CsvTable(
            "unitGuid", "unitName", "assetPath", "assetFound", "firstSeenTick", "firstSeenTime",
            "theme", "ods", "rpsType",
            "spawnCost", "spawnCooldown", "calculatedPower", "valueDiscrepancy",
            "maxHealth", "attackDamage", "knockbackDamage", "knockbackDurability", "moveSpeed", "horizontalRange", "verticalRange",
            "extraEndlagSec", "actionCooldown", "animStartupMs", "animRecoveryMs", "combatActionCount",
            "ai_atk", "ai_cycleSec", "ai_dps", "ai_range", "ai_moveSpeed", "ai_health", "ai_kbd", "ai_kbh",
            "ai_maxTargets", "ai_maxTargetsUnlimited", "ai_isAoe", "utilityPower",
            "balsim_cycleSec", "balsim_dps", "balsim_stoppingRange", "balsim_kbdps",
            "primary_present", "primary_kind", "primary_targetFriendly", "primary_healthChangePercent", "primary_scalesWithUserStats",
            "primary_maxTargetsRaw", "primary_maxTargetsNormalized", "primary_useProjectile", "primary_hasZoneData",
            "primary_priority", "primary_castCooldown", "primary_knockbackPercent", "primary_effectsOnHitCount",
            "stopping_present", "stopping_sameAsPrimary", "stopping_definesStoppingRange", "stopping_rangePercent", "stopping_extendsForward",
            "timesEvaluated", "timesSelected", "timesSpawned",
            "exportTime_spawnCost", "exportTime_spawnCooldown", "exportTime_ai_dps", "exportTime_ai_range", "assetChangedSinceFirstSeen");

        List<string> unitKeys = new List<string>(units.Keys);
        unitKeys.Sort(System.StringComparer.Ordinal);

        foreach (string key in unitKeys)
        {
            UnitAgg a = units[key];
            AIUnitSnapshot u = a.snapshot;
            string path = a.asset != null ? AssetDatabase.GetAssetPath(a.asset) : "";

            // Export-time read of the CURRENT asset (clearly labelled; used only to detect edits).
            AIUnitSnapshot now = null;
            ScriptableStats currentAsset = a.asset as ScriptableStats;
            if (currentAsset != null) now = AIUnitSnapshot.Capture(currentAsset, -1, 0f);

            if (u == null)
            {
                // Missing snapshot / asset: keep the row (so joins don't silently drop the unit) but leave stats blank.
                string[] blank = unitsTable.NewRow();
                unitsTable.Set(blank, "unitGuid", T(a.guid));
                unitsTable.Set(blank, "unitName", T(a.name));
                unitsTable.Set(blank, "assetPath", T(path));
                unitsTable.Set(blank, "assetFound", B(a.asset != null));
                unitsTable.Set(blank, "timesEvaluated", I(a.timesEvaluated));
                unitsTable.Set(blank, "timesSelected", I(a.timesSelected));
                unitsTable.Set(blank, "timesSpawned", I(a.timesSpawned));
                unitsTable.Add(blank);
                continue;
            }

            bool hp = u.hasPrimaryAction;
            bool hs = u.hasStoppingAction;

            unitsTable.Add(
                T(a.guid), T(u.unitName), T(path), B(a.asset != null), I(u.firstSeenTick), N(u.firstSeenTime),
                T(u.theme), T(u.ods), T(u.rpsType),
                N(u.spawnCost), N(u.spawnCooldown), N(u.calculatedPower), N(u.valueDiscrepancy),
                N(u.maxHealth), N(u.attackDamage), N(u.knockbackDamage), N(u.knockbackMaxHealth), N(u.moveSpeed), N(u.horizontalRange), N(u.verticalRange),
                N(u.extraEndlagSec), N(u.actionCooldown), N(u.animStartupMs), N(u.animRecoveryMs), I(u.combatActionCount),
                N(u.aiAtk), N(u.aiCycleSec), N(u.aiDps), N(u.aiRange), N(u.aiMoveSpeed), N(u.aiHealth), N(u.aiKnockbackDamage), N(u.aiKnockbackDurability),
                I(u.aiMaxTargets), B(u.aiMaxTargets == int.MaxValue), B(u.aiIsAoe), N(u.utilityPower),
                N(u.balsimCycleSec), N(u.balsimDps), N(u.balsimStoppingRange), N(u.balsimKbdps),
                B(hp), hp ? T(u.primaryKind) : "", hp ? B(u.primaryTargetFriendly) : "", hp ? N(u.primaryHealthChangePercent) : "", hp ? B(u.primaryScalesWithUserStats) : "",
                hp ? I(u.primaryMaxTargetsRaw) : "", hp ? I(u.primaryMaxTargetsNormalized) : "", hp ? B(u.primaryUseProjectile) : "", hp ? B(u.primaryHasZoneData) : "",
                hp ? N(u.primaryPriority) : "", hp ? N(u.primaryCastCooldown) : "", hp ? N(u.primaryKnockbackPercent) : "", hp ? I(u.primaryEffectsOnHitCount) : "",
                B(hs), hs ? B(u.stoppingSameAsPrimary) : "", hs ? B(u.stoppingDefinesStoppingRange) : "", hs ? N(u.stoppingRangePercent) : "", hs ? B(u.stoppingExtendsForward) : "",
                I(a.timesEvaluated), I(a.timesSelected), I(a.timesSpawned),
                now != null ? N(now.spawnCost) : "", now != null ? N(now.spawnCooldown) : "", now != null ? N(now.aiDps) : "", now != null ? N(now.aiRange) : "",
                now != null ? B(u.KeyValuesDifferFrom(now)) : "");
        }

        // ------------------------------------------------------------ session
        CsvTable session = new CsvTable("key", "value");
        AddKv(session, "schemaVersion", SchemaVersion.ToString(Inv));
        AddKv(session, "exportedLocal", System.DateTime.Now.ToString("o", Inv));
        AddKv(session, "exportedUtc", System.DateTime.UtcNow.ToString("o", Inv));
        AddKv(session, "editorUnityVersion", Application.unityVersion);
        AddKv(session, "exportedDuringPlayMode", Application.isPlaying ? "1" : "0");
        AddKv(session, "ticksExported", n.ToString(Inv));
        AddKv(session, "firstTick", firstTick.ToString(Inv));
        AddKv(session, "lastTick", lastTick.ToString(Inv));
        AddKv(session, "firstGameTime", F(firstTime));
        AddKv(session, "lastGameTime", F(lastTime));
        AddKv(session, "meanObservedTickIntervalSec", deltaCount > 0 ? F(deltaSum / deltaCount) : "");
        AddKv(session, "recorderCapacity", AIDecisionRecorder.Capacity.ToString(Inv));
        AddKv(session, "distinctUnits", units.Count.ToString(Inv));

        AISessionMetadata m = AIDecisionRecorder.Session;
        AddKv(session, "sessionMetadataAvailable", m != null ? "1" : "0");
        if (m != null)
        {
            AddKv(session, "scene", m.sceneName);
            AddKv(session, "aiClan", m.aiClanName);
            AddKv(session, "aiSpawnZone", m.aiSpawnZoneName);
            AddKv(session, "sessionStartedUtc", m.startedUtc);
            AddKv(session, "sessionStartGameTime", F(m.startGameTime));
            AddKv(session, "sessionStartFrame", m.startFrame.ToString(Inv));
            AddKv(session, "runtimeUnityVersion", m.unityVersion);
            AddKv(session, "timeScaleAtStart", F(m.timeScale));
            AddKv(session, "tickIntervalSetting", F(m.tickInterval));
            AddKv(session, "maxThreatLevelAtStart", F(m.maxThreatLevel));
            AddKv(session, "outcomeUtilityScaleAtStart", F(m.outcomeUtilityScale));
            AddKv(session, "maxSpawnsPerTickAtStart", m.maxSpawnsPerTick.ToString(Inv));
            AddKv(session, "swarmEvaluationWindowAtStart", F(m.swarmEvaluationWindow));
            AddKv(session, "maxSwarmCopiesAtStart", m.maxSwarmCopies.ToString(Inv));
            for (int i = 0; i < m.phases.Count; i++)
            {
                AISessionPhase ph = m.phases[i];
                AddKv(session, "phase_" + ph.index + "_name", ph.name);
                AddKv(session, "phase_" + ph.index + "_units", string.Join("|", ph.unitNames.ToArray()));
            }
        }
        AddKv(session, "note_settings", "Per-tick settings are in ticks.csv; *AtStart values are the first recorded tick of the session.");
        AddKv(session, "note_units", "units.csv values are first-sight snapshots captured while recording; exportTime_* columns were read from the asset at export time.");
        AddKv(session, "note_models", "ai_* columns use PowerMath (what the AI uses); balsim_* use BalanceSimulationRunner helpers. They are different approximations and are not expected to match.");
        AddKv(session, "note_blank", "Blank cells mean not applicable or not derivable (NaN/Infinity are written as blank).");
        AddKv(session, "ticksFile", ticksName);
        AddKv(session, "candidatesFile", candName);
        AddKv(session, "unitsFile", unitsName);
        AddKv(session, "plansFile", stamp + "_plans.csv");
        AddKv(session, "planCandidatesFile", stamp + "_plan_candidates.csv");
        AddKv(session, "planEventsFile", stamp + "_plan_events.csv");
        AddKv(session, "planTicksFile", stamp + "_plan_ticks.csv");
        AddKv(session, "note_planHistory", "Plan captures are retained independently of tick ring rollover, up to 500 plans. Open inspector and enable Record before starting AI.");
        AddKv(session, "note_waitCategory", "Wait categories are diagnostic inferences. Exact cooldown requires a recorded pass or a future authoritative cooldown API.");

        // ------------------------------------------------------------ write + validate
        WriteAndValidate(dir, ticksName, ticks);
        WriteAndValidate(dir, candName, cands);
        WriteAndValidate(dir, unitsName, unitsTable);
        WriteAndValidate(dir, sessionName, session);
        // Plan history has its own lifecycle and is intentionally not limited to 200 ticks.
        // Appends four new CSVs without changing existing tick/candidate/unit columns.
        AIPlanDiagnosticsExporter.Export(stamp);
        AIOperationalCSVExporter.Export(stamp);

        Debug.Log("[AI Inspector] Exported " + n + " ticks, " + cands.Rows + " candidate rows, " + unitsTable.Rows + " units to " + dir);
        EditorUtility.RevealInFinder(Path.Combine(dir, ticksName));
    }

    // ================================================================ helpers

    sealed class UnitAgg
    {
        public string guid;
        public string name;
        public Object asset;
        public AIUnitSnapshot snapshot;
        public int timesEvaluated;
        public int timesSelected;
        public int timesSpawned;
    }

    static void AddKv(CsvTable t, string key, string value)
    {
        t.Add(T(key), T(value));
    }

    static string GuidOf(Object asset, Dictionary<Object, string> cache)
    {
        if (asset == null) return "";
        string g;
        if (cache.TryGetValue(asset, out g)) return g;
        string path = AssetDatabase.GetAssetPath(asset);
        g = string.IsNullOrEmpty(path) ? "" : AssetDatabase.AssetPathToGUID(path);
        cache[asset] = g;
        return g;
    }

    /// <summary>1-based rank, descending by key; NaN keys last; ties keep original order.</summary>
    static int[] RankBy(List<AICandidateSnapshot> list, System.Func<AICandidateSnapshot, float> key)
    {
        int n = list.Count;
        List<int> order = new List<int>(n);
        for (int i = 0; i < n; i++) order.Add(i);

        order.Sort((a, b) =>
        {
            float ka = key(list[a]), kb = key(list[b]);
            bool na = float.IsNaN(ka), nb = float.IsNaN(kb);
            if (na || nb)
            {
                if (na && nb) return a.CompareTo(b);
                return na ? 1 : -1;
            }
            int c = kb.CompareTo(ka);
            return c != 0 ? c : a.CompareTo(b);
        });

        int[] ranks = new int[n];
        for (int r = 0; r < n; r++) ranks[order[r]] = r + 1;
        return ranks;
    }

    // ---- field formatters: invariant numbers, RFC 4180 quoting, NaN/Infinity -> blank ----
    static string N(float v) { return (float.IsNaN(v) || float.IsInfinity(v)) ? "" : v.ToString("R", Inv); }
    static string F(float v) { return N(v); }
    static string I(int v) { return v.ToString(Inv); }
    static string B(bool v) { return v ? "1" : "0"; }
    static string T(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }

    static void WriteAndValidate(string dir, string fileName, CsvTable table)
    {
        string text = table.ToString();
        string path = Path.Combine(dir, fileName);
        File.WriteAllText(path, text, new UTF8Encoding(false));

        string error = ValidateCsv(text, table.ColumnCount, out int records);
        if (error != null)
            Debug.LogError("[AI Inspector] CSV validation FAILED for " + fileName + ": " + error);
        else
            Debug.Log("[AI Inspector] CSV validation OK for " + fileName + " (" + (records - 1) + " data rows, " + table.ColumnCount + " columns).");
    }

    /// <summary>Re-parses the text as RFC 4180 and checks every record has the same number of fields.</summary>
    static string ValidateCsv(string text, int expectedColumns, out int records)
    {
        records = 0;
        bool inQuotes = false;
        int fields = 1;
        bool recordHasContent = false;

        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') i++;   // escaped quote
                    else inQuotes = false;
                }
                continue;
            }

            if (ch == '"') { inQuotes = true; recordHasContent = true; }
            else if (ch == ',') { fields++; recordHasContent = true; }
            else if (ch == '\n')
            {
                if (recordHasContent || fields > 1)
                {
                    records++;
                    if (fields != expectedColumns)
                        return "record " + records + " has " + fields + " fields, expected " + expectedColumns + ".";
                }
                fields = 1;
                recordHasContent = false;
            }
            else if (ch != '\r') recordHasContent = true;
        }

        if (inQuotes) return "unterminated quoted field.";
        if (recordHasContent) return "missing final newline.";
        return null;
    }

    sealed class CsvTable
    {
        readonly string[] headers;
        readonly StringBuilder sb = new StringBuilder(4096);

        public int Rows { get; private set; }
        public int ColumnCount { get { return headers.Length; } }

        public CsvTable(params string[] headers)
        {
            this.headers = headers;
            sb.Append(string.Join(",", headers)).Append('\n');
        }

        public void Add(params string[] fields)
        {
            if (fields.Length != headers.Length)
                throw new System.InvalidOperationException("CSV row has " + fields.Length + " fields but the header has " + headers.Length + " (first field: '" + (fields.Length > 0 ? fields[0] : "") + "').");

            sb.Append(string.Join(",", fields)).Append('\n');
            Rows++;
        }

        /// <summary>A row of empty fields, to be filled by column name (used when a unit has no snapshot).</summary>
        public string[] NewRow()
        {
            string[] row = new string[headers.Length];
            for (int i = 0; i < row.Length; i++) row[i] = "";
            return row;
        }

        public void Set(string[] row, string column, string value)
        {
            int idx = System.Array.IndexOf(headers, column);
            if (idx < 0) throw new System.ArgumentException("Unknown CSV column: " + column);
            row[idx] = value;
        }

        public override string ToString() { return sb.ToString(); }
    }
}