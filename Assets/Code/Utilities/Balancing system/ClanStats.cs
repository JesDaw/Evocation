using UnityEngine;

[CreateAssetMenu(fileName = "ClanStats", menuName = "Clan Stats")]
public class ClanStats : ScriptableObject
{
    public string ClanTheme;
    [TextArea(3, 6)] public string Characteristics;

    public CharacterData[] all_stats_scripts;

    [Header("Clan Level Totals (LevelBalancerMath system — see chat notes)")]
    public float TotalLevel;
    public float SumAttack;
    public float SumDefense;
    public float SumSpaceControl;
    public float AvgAttackFrequency;

    [Header("Individual Stat Averages (display only — no longer feeds cost calc)")]
    public float AvgMove;
    public float AvgKB_Dmg;
    public float AvgAtk_Dmg;
    public float AvgCooldown;
    public float AvgHP;
    public float AvgKB_HP;
    public float AvgRange;

    [Header("Value Analysis")]
    public float[] UnitValueDiscrepancies;
}