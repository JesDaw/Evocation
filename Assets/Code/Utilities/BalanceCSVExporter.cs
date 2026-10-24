using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BalanceCSVExporter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Anchor unit used for baseline comparison in the report.")]
    public ScriptableStats anchorUnit;

    [Tooltip("Drag your ClanStats assets here (or click Auto-Find Clans).")]
    public ClanStats[] all_clan_stats;

    [Header("Export Settings")]
    public string fileName = "GameBalanceReport.csv";

    [ContextMenu("Auto-Find Clans in Project")]
    public void AutoFindClans()
    {
#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:ClanStats");
        List<ClanStats> clans = new List<ClanStats>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ClanStats c = AssetDatabase.LoadAssetAtPath<ClanStats>(path);
            if (c != null && !clans.Contains(c)) clans.Add(c);
        }
        all_clan_stats = clans.ToArray();
        Debug.Log($"[BalanceCSV] Found {all_clan_stats.Length} ClanStats assets in project.");
#endif
    }

    [ContextMenu("Export Full Game Balance to CSV")]
    public void ExportToCSV()
    {
        if (all_clan_stats == null || all_clan_stats.Length == 0)
        {
            AutoFindClans();
        }

        if (anchorUnit == null)
        {
            var runner = FindFirstObjectByType<BalanceSimulationRunner>();
            if (runner != null) anchorUnit = runner.anchorUnit;
        }

        StringBuilder csv = new StringBuilder();

        // ── 1. GLOBAL METADATA ──────────────────────────────────────────
        csv.AppendLine("=== GAME BALANCE REPORT ===");
        csv.AppendLine($"Export Date,{System.DateTime.Now}");
        csv.AppendLine($"Anchor Unit,{(anchorUnit != null ? anchorUnit.name : "None")}");
        csv.AppendLine($"Anchor Cost,{(anchorUnit != null ? anchorUnit._spawnCost.ToString() : "0")}");
        csv.AppendLine($"Anchor HP/Atk/Speed,{(anchorUnit != null ? $"{anchorUnit._MaxHealth}/{anchorUnit._AttackDamage}/{anchorUnit._MoveSpeed}" : "N/A")}");
        csv.AppendLine();

        // ── 2. CLAN-BY-CLAN BREAKDOWN WITH STATS ────────────────────────
        csv.AppendLine("ROSTER & CLAN STATISTICS");
        string header = "Clan,Row Type,Unit Name,Spawn Cost,Calculated Power,Discrepancy (Power - Cost),Max Health,Attack Dmg,Move Speed,Horizontal Range,Attack Freq (hits/s),KB Dmg,KB Max HP,RPS Type,Notes";
        csv.AppendLine(header);

        if (all_clan_stats != null)
        {
            foreach (var clan in all_clan_stats)
            {
                if (clan == null) continue;

                string theme = string.IsNullOrEmpty(clan.ClanTheme) ? clan.name : clan.ClanTheme;
                List<UnitRowData> clanUnits = new List<UnitRowData>();

                if (clan.all_stats_scripts != null)
                {
                    foreach (var cd in clan.all_stats_scripts)
                    {
                        if (cd == null || cd.scriptableStats == null) continue;
                        var s = cd.scriptableStats;

                        float cycleSec = BalanceSimulationRunner.GetTrueAttackCycleSeconds(s);
                        float freq = 1f / Mathf.Max(cycleSec, 0.05f);

                        var row = new UnitRowData
                        {
                            clan = theme,
                            name = s.name,
                            cost = s._spawnCost,
                            calcPower = s._CalculatedPower,
                            discrepancy = s._ValueDiscrepancy,
                            hp = s._MaxHealth,
                            atk = s._AttackDamage,
                            move = s._MoveSpeed,
                            range = s._HorizontalRange,
                            freq = freq,
                            kbd = s._KnockBackDamage,
                            kbh = s._KnockBackMaxHealth,
                            rps = s.RPS_Type,
                            notes = s.OtherNotes?.Replace(",", ";").Replace("\n", " ")
                        };
                        clanUnits.Add(row);

                        // Character row
                        csv.AppendLine($"\"{row.clan}\",Unit,\"{row.name}\",{row.cost},{row.calcPower:F1},{row.discrepancy:F1},{row.hp},{row.atk},{row.move:F2},{row.range:F2},{row.freq:F2},{row.kbd:F1},{row.kbh:F1},\"{row.rps}\",\"{row.notes}\"");
                    }
                }

                if (clanUnits.Count > 0)
                {
                    AppendClanSummaryRows(csv, theme, clanUnits);
                }
                else
                {
                    csv.AppendLine($"\"{theme}\",Empty Clan,,0,0,0,0,0,0,0,0,0,0,,");
                }

                csv.AppendLine(); // Blank line between clans
            }
        }

        string fullPath = Path.Combine(Application.dataPath, fileName);
        File.WriteAllText(fullPath, csv.ToString(), Encoding.UTF8);

        Debug.Log($"<b>[Balance CSV Exporter] Successfully exported balance data to:</b>\n{fullPath}");

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }

    private void AppendClanSummaryRows(StringBuilder csv, string theme, List<UnitRowData> units)
    {
        int n = units.Count;

        // Collect stat arrays
        float[] costs  = new float[n];
        float[] powers = new float[n];
        float[] discs  = new float[n];
        float[] hps    = new float[n];
        float[] atks   = new float[n];
        float[] moves  = new float[n];
        float[] ranges = new float[n];
        float[] freqs  = new float[n];
        float[] kbds   = new float[n];
        float[] kbhs   = new float[n];

        for (int i = 0; i < n; i++)
        {
            costs[i]  = units[i].cost;
            powers[i] = units[i].calcPower;
            discs[i]  = units[i].discrepancy;
            hps[i]    = units[i].hp;
            atks[i]   = units[i].atk;
            moves[i]  = units[i].move;
            ranges[i] = units[i].range;
            freqs[i]  = units[i].freq;
            kbds[i]   = units[i].kbd;
            kbhs[i]   = units[i].kbh;
        }

        // 1. AVERAGE
        csv.AppendLine($"\"{theme}\",AVERAGE,({n} Units),{Avg(costs):F1},{Avg(powers):F1},{Avg(discs):F1},{Avg(hps):F1},{Avg(atks):F1},{Avg(moves):F2},{Avg(ranges):F2},{Avg(freqs):F2},{Avg(kbds):F1},{Avg(kbhs):F1},,");

        // 2. STANDARD DEVIATION
        csv.AppendLine($"\"{theme}\",STD DEV,,{StdDev(costs):F1},{StdDev(powers):F1},{StdDev(discs):F1},{StdDev(hps):F1},{StdDev(atks):F1},{StdDev(moves):F2},{StdDev(ranges):F2},{StdDev(freqs):F2},{StdDev(kbds):F1},{StdDev(kbhs):F1},,");

        // 3. RANGE (MAX - MIN)
        csv.AppendLine($"\"{theme}\",RANGE (Max-Min),,{Range(costs):F1},{Range(powers):F1},{Range(discs):F1},{Range(hps):F1},{Range(atks):F1},{Range(moves):F2},{Range(ranges):F2},{Range(freqs):F2},{Range(kbds):F1},{Range(kbhs):F1},,");
    }

    private struct UnitRowData
    {
        public string clan, name, rps, notes;
        public float cost, calcPower, discrepancy, hp, atk, move, range, freq, kbd, kbh;
    }

    private static float Avg(float[] vals)
    {
        if (vals == null || vals.Length == 0) return 0f;
        float sum = 0f;
        for (int i = 0; i < vals.Length; i++) sum += vals[i];
        return sum / vals.Length;
    }

    private static float StdDev(float[] vals)
    {
        if (vals == null || vals.Length < 2) return 0f;
        float avg = Avg(vals);
        float sumSq = 0f;
        for (int i = 0; i < vals.Length; i++)
        {
            float diff = vals[i] - avg;
            sumSq += diff * diff;
        }
        return Mathf.Sqrt(sumSq / (vals.Length - 1)); // Sample StdDev
    }

    private static float Range(float[] vals)
    {
        if (vals == null || vals.Length == 0) return 0f;
        float min = vals[0];
        float max = vals[0];
        for (int i = 1; i < vals.Length; i++)
        {
            if (vals[i] < min) min = vals[i];
            if (vals[i] > max) max = vals[i];
        }
        return max - min;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(BalanceCSVExporter))]
public class BalanceCSVExporterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BalanceCSVExporter exporter = (BalanceCSVExporter)target;
        GUILayout.Space(10);

        if (GUILayout.Button("Find All ClanStats in Project", GUILayout.Height(24)))
        {
            Undo.RecordObject(exporter, "Find ClanStats");
            exporter.AutoFindClans();
            EditorUtility.SetDirty(exporter);
        }

        GUILayout.Space(4);
        GUI.backgroundColor = new Color(0.2f, 0.7f, 1.0f);
        if (GUILayout.Button("📊 Export Full Game Balance to CSV", GUILayout.Height(35)))
        {
            exporter.ExportToCSV();
        }
    }
}
#endif