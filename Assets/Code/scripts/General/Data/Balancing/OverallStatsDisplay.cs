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
    public float TotalPower;
    public float AverageValueDiscrepancy;
    public float TotalPushingPower;
    public float TotalDPS;
    public float TotalDefense;

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
        UnitsInClan = Clan.all_stats_scripts;

        if (Clan.UnitValueDiscrepancies != null)
        {
            UnitValueDiscrepancies = new float[Clan.UnitValueDiscrepancies.Length];
            float discrepancySum = 0f;
            for (int i = 0; i < Clan.UnitValueDiscrepancies.Length; i++)
            {
                UnitValueDiscrepancies[i] = Clan.UnitValueDiscrepancies[i];
                discrepancySum += Clan.UnitValueDiscrepancies[i];
            }
            AverageValueDiscrepancy = Clan.UnitValueDiscrepancies.Length > 0
                ? discrepancySum / Clan.UnitValueDiscrepancies.Length
                : 0f;
        }

        TotalPower        = Clan.TotalPower;
        TotalPushingPower = Clan.TotalPushingPower;
        TotalDPS          = Clan.TotalDPS;
        TotalDefense      = Clan.TotalDefense;

        MoveSpeed       = Clan.AvgMove;
        KnockbackDmg    = Clan.AvgKB_Dmg;
        AttackDmg       = Clan.AvgAtk_Dmg;
        AttackEndlag    = Clan.AvgEndlag;
        MaxHP           = Clan.AvgHP;
        KnockbackHP     = Clan.AvgKB_HP;
        HorizontalRange = Clan.AvgRange;
    }
}