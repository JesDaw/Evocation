using UnityEngine;

public class OverallStatsDisplay : MonoBehaviour
{
    public ClanStats Clan;

    [Header("Clan Identity")]
    public string Theme;
    [TextArea(2, 4)] public string Characteristics;
    public ScriptableStats[] UnitsInClan;

    [Header("Unit Power Values (Cheapest → Most Expensive)")]
    public int[] UnitPowerValues;

    [Header("Clan Totals")]
    public float TotalPower;
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

        Theme           = Clan.ClanTheme;
        Characteristics = Clan.Characteristics;
        UnitsInClan     = Clan.all_stats_scripts;

        if (Clan.UnitPowerValues != null)
        {
            UnitPowerValues = new int[Clan.UnitPowerValues.Length];
            for (int i = 0; i < Clan.UnitPowerValues.Length; i++)
                UnitPowerValues[i] = Mathf.RoundToInt(Clan.UnitPowerValues[i]);
        }

        TotalPower        = Clan.TotalPower;
        TotalPushingPower = Clan.TotalPushingPower;
        TotalDPS          = Clan.TotalDPS;
        TotalDefense      = Clan.TotalDefense;

        MoveSpeed      = Clan.AvgMove;
        KnockbackDmg   = Clan.AvgKB_Dmg;
        AttackDmg      = Clan.AvgAtk_Dmg;
        AttackEndlag   = Clan.AvgEndlag;
        MaxHP          = Clan.AvgHP;
        KnockbackHP    = Clan.AvgKB_HP;
        HorizontalRange = Clan.AvgRange;
    }
}