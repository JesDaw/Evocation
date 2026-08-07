using UnityEngine;

public class OverallStatsDisplay : MonoBehaviour
{
    public ClanStats Clan;

    // Stored locally — edit freely here without affecting the ScriptableObject.
    [Header("Clan Identity (edit here)")]
    public string Theme;
    [TextArea(2, 4)] public string Characteristics;

    public ScriptableStats[] UnitsInClan;

    // Value discrepancy = how powerful a unit is relative to its spawn cost.
    // Positive = underpriced (strong for cost), negative = overpriced (weak for cost).
    [Header("Unit Value Discrepancies (Cheapest → Most Expensive)")]
    public float[] UnitValueDiscrepancies;

    [Header("Clan Totals")]
    public float TotalLevel;
    public float SumAttack;
    public float SumDefense;
    public float SumSpaceControl;
    public float AvgAttackFrequency;

    [Header("Clan Stat Averages")]
    public float MoveSpeed;
    public float KnockbackDmg;
    public float AttackDmg;
    public float AttackEndlag;
    public float MaxHP;
    public float KnockbackHP;
    public float HorizontalRange;

    public void SyncWithClan()
    {
        if (Clan == null) return;

        // Theme and Characteristics are NOT synced from Clan —
        // edit them directly on this component.
        UnitsInClan = new ScriptableStats[Clan.all_stats_scripts.Length];
        for (int i = 0; i < Clan.all_stats_scripts.Length; i++)
        {
            UnitsInClan[i] = Clan.all_stats_scripts[i]?.scriptableStats;
        }

        if (Clan.UnitValueDiscrepancies != null)
        {
            UnitValueDiscrepancies = new float[Clan.UnitValueDiscrepancies.Length];
            for (int i = 0; i < Clan.UnitValueDiscrepancies.Length; i++)
            {
                UnitValueDiscrepancies[i] = Clan.UnitValueDiscrepancies[i];
            }
        }

        TotalLevel           = Clan.TotalLevel;
        SumAttack            = Clan.SumAttack;
        SumDefense           = Clan.SumDefense;
        SumSpaceControl      = Clan.SumSpaceControl;
        AvgAttackFrequency  = Clan.AvgAttackFrequency;

        MoveSpeed       = Clan.AvgMove;
        KnockbackDmg    = Clan.AvgKB_Dmg;
        AttackDmg       = Clan.AvgAtk_Dmg;
        AttackEndlag    = Clan.AvgEndlag;
        MaxHP           = Clan.AvgHP;
        KnockbackHP     = Clan.AvgKB_HP;
        HorizontalRange = Clan.AvgRange;
    }
}