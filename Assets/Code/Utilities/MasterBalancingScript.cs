using UnityEngine;

[ExecuteInEditMode]
public class MasterBalancingScript : MonoBehaviour
{
    [Header("1. Clan Roster")]
    public ClanStats[] all_clan_stats;

    [Header("2. Global Power Stats (display only)")]
    public float Global_AvgPower;
    public int Global_TotalUnitCount;

    [Header("2b. Calibration")]
    [Tooltip("Added to every unit's raw simulated power (vs BalancingGrapher.AnchorUnit) so numbers " +
             "stay positive for cost purposes. Right-click this component and choose 'Recalibrate Min " +
             "Power Offset' after changing the anchor or adding a new weakest unit — it no longer " +
             "recomputes every frame, since that was the mechanism causing one unit's stat change to " +
             "silently re-price every other unit.")]
    public float MinPowerOffset;

    [Header("3. Character Balancers")]
    public CharacterStatBalancer[] all_balancers;

    private BalancingGrapher grapher;

    void Update()
    {
        if (all_clan_stats == null) return;

        if (grapher == null)
        {
            grapher = GetComponent<BalancingGrapher>();
            if (grapher == null) grapher = gameObject.AddComponent<BalancingGrapher>();
        }

        if (grapher.AnchorUnit == null) return; // nothing to compare against yet — assign one in the Inspector

        var anchor = PowerMath.GetProfile(grapher.AnchorUnit);

        float totalPowerSum = 0;
        int activeUnits = 0;

        for (int i = 0; i < all_clan_stats.Length; i++)
        {
            if (all_clan_stats[i] == null) continue;

            all_clan_stats[i].UpdateAverages(grapher, anchor, MinPowerOffset);

            totalPowerSum += all_clan_stats[i].TotalLevel;
            activeUnits += all_clan_stats[i].all_stats_scripts?.Length ?? 0;
        }

        if (all_balancers != null)
        {
            foreach (var bal in all_balancers)
            {
                if (bal?.Stats == null) continue;
                totalPowerSum += bal.Stats._CalculatedPower;
                activeUnits++;
            }
        }

        Global_TotalUnitCount = activeUnits;
        Global_AvgPower = activeUnits > 0 ? totalPowerSum / activeUnits : 0;
        SyncDisplayComponents();
        UpdateLevelDisplays();
    }

    // Scans the FULL roster (all_clan_stats + all_balancers) — previously this
    // only scanned all_balancers, so the offset was calibrated off whichever
    // few units happened to have a live CharacterStatBalancer in the scene,
    // then silently applied to every unit in every clan.
    [ContextMenu("Recalibrate Min Power Offset")]
    public void RecalibrateOffset()
    {
        if (grapher == null) grapher = GetComponent<BalancingGrapher>();
        if (grapher == null || grapher.AnchorUnit == null) return;

        var anchor = PowerMath.GetProfile(grapher.AnchorUnit);
        float minPowerFound = 0f;

        void Check(ScriptableStats s)
        {
            var result = CharacterStatBalancer.CalculatePower(s, anchor,
                grapher.Weight_AttackDamage, grapher.Weight_AttackEndlag, grapher.Weight_MoveSpeed, grapher.Weight_KnockBackDamage,
                grapher.Weight_MaxHealth, grapher.Weight_KnockBackHealth, grapher.Weight_HorizontalRange, grapher.Weight_AOE,
                grapher.Base_Velocity, grapher.SimulationDistance, 0f);
            if (result.Power < minPowerFound) minPowerFound = result.Power;
        }

        if (all_clan_stats != null)
            foreach (var clan in all_clan_stats)
            {
                if (clan?.all_stats_scripts == null) continue;
                foreach (var cd in clan.all_stats_scripts)
                    if (cd?.scriptableStats != null) Check(cd.scriptableStats);
            }

        if (all_balancers != null)
            foreach (var bal in all_balancers)
                if (bal?.Stats != null) Check(bal.Stats);

        MinPowerOffset = Mathf.Abs(minPowerFound) + 1f;
        Debug.Log($"[MasterBalancingScript] Recalibrated MinPowerOffset = {MinPowerOffset:F2}");
    }

    void SyncDisplayComponents()
    {
        var existingDisplays = GetComponents<OverallStatsDisplay>();
        for (int i = 0; i < all_clan_stats.Length; i++)
        {
            if (all_clan_stats[i] == null) continue;
            OverallStatsDisplay d = (i < existingDisplays.Length) ? existingDisplays[i] : gameObject.AddComponent<OverallStatsDisplay>();
            d.Clan = all_clan_stats[i];
            d.SyncWithClan();
        }
    }

    void UpdateLevelDisplays()
    {
        if (grapher == null) return;

        if (all_clan_stats != null)
        {
            foreach (var clan in all_clan_stats)
            {
                if (clan?.all_stats_scripts == null) continue;
                foreach (var cd in clan.all_stats_scripts)
                {
                    if (cd == null || cd.scriptableStats == null) continue;
                    PopulateLevelFields(cd.scriptableStats);
                }
            }
        }

        if (all_balancers != null)
        {
            foreach (var bal in all_balancers)
            {
                if (bal?.Stats == null) continue;
                PopulateLevelFields(bal.Stats);
            }
        }
    }

    void PopulateLevelFields(ScriptableStats s)
    {
        var breakdown = LevelBalancerMath.GetLevelBreakdown(s, grapher);
        s.Attack = breakdown.attack;
        s.Defense = breakdown.defense;
        s.SpaceControl = breakdown.spaceControl;
        s.AttackFrequency = breakdown.attackFrequency;
        s.Level_Total = breakdown.total;
        s.Level_Discrepancy = breakdown.total - s._spawnCost;
    }
}