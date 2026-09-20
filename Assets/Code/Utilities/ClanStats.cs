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

    public void UpdateAverages(BalancingGrapher g, PowerMath.UnitProfile anchor, float powerOffset)
    {
        if (all_stats_scripts == null || all_stats_scripts.Length == 0) return;

        float tAttack = 0, tDefense = 0, tSpaceControl = 0, tAttackFreq = 0, tLevel = 0;
        float tMove = 0, tKBD = 0, tAtk = 0, tCooldown = 0, tHP = 0, tKBH = 0, tRng = 0;

        UnitValueDiscrepancies = new float[all_stats_scripts.Length];

        for (int i = 0; i < all_stats_scripts.Length; i++)
        {
            var cd = all_stats_scripts[i];
            if (cd == null || cd.scriptableStats == null) continue;
            var s = cd.scriptableStats;

            var result = CharacterStatBalancer.CalculatePower(s, anchor,
                g.Weight_AttackDamage, g.Weight_AttackEndlag, g.Weight_MoveSpeed, g.Weight_KnockBackDamage,
                g.Weight_MaxHealth, g.Weight_KnockBackHealth, g.Weight_HorizontalRange, g.Weight_AOE,
                g.Base_Velocity, g.SimulationDistance, powerOffset);

            s._CalculatedPower = result.Power;
            s._ValueDiscrepancy = result.Power - s._spawnCost;
            UnitValueDiscrepancies[i] = s._ValueDiscrepancy;

            // Legacy LevelBalancerMath totals — s.Attack/s.Defense/etc are populated
            // in a separate pass by MasterBalancingScript.PopulateLevelFields.
            tAttack += s.Attack;
            tDefense += s.Defense;
            tSpaceControl += s.SpaceControl;
            tAttackFreq += s.AttackFrequency;
            tLevel += s.Level_Total;

            var profile = PowerMath.GetProfile(s);
            tMove += profile.Move;
            tKBD  += profile.KBD;
            tAtk  += profile.Atk;
            tCooldown += profile.Cooldown;
            tHP   += profile.HP;
            tKBH  += profile.KBH;
            tRng  += profile.Range;
        }

        int count = all_stats_scripts.Length;
        TotalLevel = tLevel;
        SumAttack = tAttack;
        SumDefense = tDefense;
        SumSpaceControl = tSpaceControl;
        AvgAttackFrequency = tAttackFreq / count;

        AvgMove = tMove / count;
        AvgKB_Dmg = tKBD / count;
        AvgAtk_Dmg = tAtk / count;
        AvgCooldown = tCooldown / count;
        AvgHP = tHP / count;
        AvgKB_HP = tKBH / count;
        AvgRange = tRng / count;
    }
}