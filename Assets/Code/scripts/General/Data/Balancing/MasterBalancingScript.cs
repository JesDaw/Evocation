using UnityEngine;

[ExecuteInEditMode]
public class MasterBalancingScript : MonoBehaviour
{
    [Header("1. Clan Roster")]
    public ClanStats[] all_clan_stats;

    [Header("2. Global Power Stats")]
    public float Global_AvgPower;
    public float MinPowerOffset; 

    [Header("3. Global Stat Averages")]
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

        if (grapher == null)
        {
            grapher = GetComponent<BalancingGrapher>();
            if (grapher == null) grapher = gameObject.AddComponent<BalancingGrapher>();
        }

        // Pass 1: Compute Raw Stat Averages
        ComputeGlobalAverages();

        // Pass 2: Find the Lowest Raw Power to determine Offset
        float minPowerFound = 0;
        foreach (var clan in all_clan_stats)
        {
            if (clan?.all_stats_scripts == null) continue;
            foreach (var s in clan.all_stats_scripts)
            {
                if (s == null) continue;
                float raw = s.SimulatePower(
                    s._AttackDamage, s._AttackEndlag, s._MoveSpeed, s._KnockBackDamage, s._MaxHealth, s._KnockBackMaxHealth, s._HorizontalRange,
                    grapher.Weight_AttackDamage, grapher.Weight_AttackEndlag, grapher.Weight_MoveSpeed, grapher.Weight_KnockBackDamage, 
                    grapher.Weight_MaxHealth, grapher.Weight_KnockBackHealth, grapher.Weight_HorizontalRange,
                    Global_AvgHP, Global_AvgKB_MaxHealth, Global_AvgMoveSpeed, Global_AvgKB_Dmg, Global_AvgAtk_Dmg, Global_AvgEndlag, Global_AvgRange,
                    grapher.Base_Velocity, grapher.SimulationDistance
                );
                if (raw < minPowerFound) minPowerFound = raw;
            }
        }
        MinPowerOffset = Mathf.Abs(minPowerFound) + 1f;

        // Pass 3: Final Update with Offset
        float totalPowerSum = 0;
        int activeUnits = 0;

        for (int i = 0; i < all_clan_stats.Length; i++)
        {
            if (all_clan_stats[i] == null) continue;

            all_clan_stats[i].UpdateAverages(
                grapher.Weight_AttackDamage, grapher.Weight_AttackEndlag, grapher.Weight_MoveSpeed, grapher.Weight_KnockBackDamage,
                grapher.Weight_MaxHealth, grapher.Weight_KnockBackHealth, grapher.Weight_HorizontalRange, 1f,
                Global_AvgHP, Global_AvgKB_MaxHealth, Global_AvgMoveSpeed, Global_AvgKB_Dmg, Global_AvgAtk_Dmg, Global_AvgEndlag, Global_AvgRange,
                grapher.Base_Velocity, 45f, grapher.SimulationDistance,
                grapher.Max_MoveSpeed, grapher.Max_Endlag, grapher.Max_Range, grapher.Max_Health, grapher.Max_Damage, grapher.Max_KBDamage, grapher.Max_KBHealth,
                MinPowerOffset 
            );

            totalPowerSum += all_clan_stats[i].TotalPower;
            activeUnits++;
        }

        Global_AvgPower = activeUnits > 0 ? totalPowerSum / activeUnits : 0;
        SyncDisplayComponents();
    }

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
                tHP += s._MaxHealth; tKBH += s._KnockBackMaxHealth; tMove += s._MoveSpeed;
                tKBD += s._KnockBackDamage; tAD += s._AttackDamage; tEnd += s._AttackEndlag;
                tRange += s._HorizontalRange;
                count++;
            }
        }

        if (count == 0) return;
        Global_TotalUnitCount = count;
        Global_AvgHP = tHP / count; Global_AvgKB_MaxHealth = tKBH / count;
        Global_AvgMoveSpeed = tMove / count; Global_AvgKB_Dmg = tKBD / count;
        Global_AvgAtk_Dmg = tAD / count; Global_AvgEndlag = tEnd / count;
        Global_AvgRange = tRange / count;
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
}