using UnityEngine;
using System.Linq;

[ExecuteInEditMode]
public class MasterBalancingScript : MonoBehaviour
{
    [Header("1. Clan Roster")]
    public ClanStats[] all_clan_stats;

    [Header("2. Simulation Settings")]
    [Tooltip("The number of units of space a unit must claim to complete the simulation. " +
             "Set this to a typical map length so power values are meaningful.")]
    public float SpaceTarget = 100f;

    [Header("3. Global Power Curve")]
    public AnimationCurve GlobalPowerCurve = new AnimationCurve();

    [Header("4. Global Stat Averages (All Units in All Clans)")]
    public int   Global_TotalUnitCount;
    public float Global_AvgHP;
    public float Global_AvgKB_MaxHealth;
    public float Global_AvgMoveSpeed;
    public float Global_AvgKB_Dmg;
    public float Global_AvgAtk_Dmg;
    public float Global_AvgEndlag;
    public float Global_AvgRange;

    private BalancingGrapher grapher;

    void Update()
    {
        if (all_clan_stats == null) return;

        // Ensure Grapher exists and is at the top of the inspector
        if (grapher == null)
        {
            grapher = GetComponent<BalancingGrapher>();
            if (grapher == null) grapher = gameObject.AddComponent<BalancingGrapher>();

#if UNITY_EDITOR
            for (int i = 0; i < 10; i++) UnityEditorInternal.ComponentUtility.MoveComponentUp(grapher);
#endif
        }

        // PASS 1: Read raw stats to compute global averages.
        // Must happen before RefreshBalancing so every unit is compared against
        // the same baseline.
        ComputeGlobalAverages();

        // Update the grapher now that averages are known
        grapher.UpdateGraphs(
            Global_AvgHP, Global_AvgKB_MaxHealth, Global_AvgMoveSpeed, SpaceTarget);

        // PASS 2: Refresh every unit's power using global averages
        GlobalPowerCurve = new AnimationCurve();
        for (int i = 0; i < all_clan_stats.Length; i++)
        {
            if (all_clan_stats[i] == null) continue;

            all_clan_stats[i].UpdateAverages(
                grapher.Weight_AttackDamage, grapher.Weight_AttackEndlag,
                grapher.Weight_MoveSpeed,    grapher.Weight_KnockBackDamage,
                grapher.Weight_MaxHealth,    grapher.Weight_KnockBackHealth,
                grapher.Weight_HorizontalRange, grapher.Weight_AOE_Efficiency,
                Global_AvgHP, Global_AvgKB_MaxHealth, Global_AvgMoveSpeed,
                SpaceTarget);

            GlobalPowerCurve.AddKey(i, all_clan_stats[i].TotalPower);
        }

        SyncDisplayComponents();
    }

    // Iterates every unit in every clan and averages their raw stats.
    // No simulation is run here — this is purely a stat read.
    void ComputeGlobalAverages()
    {
        float tHP = 0, tKBH = 0, tMove = 0, tKBD = 0, tAD = 0, tEnd = 0, tRange = 0;
        int count = 0;

        foreach (var clan in all_clan_stats)
        {
            if (clan?.all_stats_scripts == null) continue;
            foreach (var s in clan.all_stats_scripts)
            {
                if (s == null) continue;
                tHP    += s._MaxHealth;         tKBH  += s._KnockBackMaxHealth;
                tMove  += s._MoveSpeed;         tKBD  += s._KnockBackDamage;
                tAD    += s._AttackDamage;      tEnd  += s._AttackEndlag;
                tRange += s._HorizontalRange;
                count++;
            }
        }

        if (count == 0) return;

        Global_TotalUnitCount = count;
        Global_AvgHP          = tHP   / count;
        Global_AvgKB_MaxHealth = tKBH / count;
        Global_AvgMoveSpeed   = tMove / count;
        Global_AvgKB_Dmg      = tKBD  / count;
        Global_AvgAtk_Dmg     = tAD   / count;
        Global_AvgEndlag      = tEnd  / count;
        Global_AvgRange       = tRange / count;
    }

    void SyncDisplayComponents()
    {
        var existingDisplays = GetComponents<OverallStatsDisplay>();

        if (existingDisplays.Length > all_clan_stats.Length)
        {
            for (int i = existingDisplays.Length - 1; i >= all_clan_stats.Length; i--)
                DestroyImmediate(existingDisplays[i]);
        }

        for (int i = 0; i < all_clan_stats.Length; i++)
        {
            if (all_clan_stats[i] == null) continue;
            OverallStatsDisplay d;
            if (i < existingDisplays.Length) d = existingDisplays[i];
            else d = gameObject.AddComponent<OverallStatsDisplay>();

            d.Clan = all_clan_stats[i];
            d.SyncWithClan();
        }
    }
}