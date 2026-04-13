using UnityEngine;

[CreateAssetMenu(fileName = "ClanStats", menuName = "Clan Stats")]
public class ClanStats : ScriptableObject
{
    public string ClanTheme;
    [TextArea(3, 6)] public string Characteristics;

    public ScriptableStats[] all_stats_scripts;

    [Header("Clan Level Totals")]
    public float TotalLevel;
    public float SumAttack;
    public float SumDefense;
    public float SumSpaceControl;
    public float AvgAttackFrequency;

    [Header("Individual Stat Averages")]
    public float AvgMove;
    public float AvgKB_Dmg;
    public float AvgAtk_Dmg;
    public float AvgEndlag;
    public float AvgHP;
    public float AvgKB_HP;
    public float AvgRange;

    [Header("Value Analysis")]
    public float[] UnitValueDiscrepancies;

    public void UpdateAverages(
        float wAtk, float wEnd, float wMove, float wKB_Dmg, float wHP, float wKB_HP, float wRange, float wAOE,
        float avgHP, float avgKB_HP, float avgMove, float avgKB_Dmg,
        float avgAtk, float avgEndlag, float avgRange,
        float baseVelocity, float baseAngle,
        float universalSimDist, 
        float mMove, float mEnd, float mRange, float mHP, float mAtk, float mKBD, float mKBH,
        float powerOffset)
    {
        if (all_stats_scripts == null || all_stats_scripts.Length == 0) return;

        float tAttack = 0, tDefense = 0, tSpaceControl = 0, tAttackFreq = 0, tLevel = 0;
        float tMove = 0, tKBD = 0, tAD = 0, tEnd = 0, tHP = 0, tKBH = 0, tRng = 0;
        
        UnitValueDiscrepancies = new float[all_stats_scripts.Length];

        for (int i = 0; i < all_stats_scripts.Length; i++)
        {
            var s = all_stats_scripts[i];
            if (s == null) continue;

            float calculatedPower = CharacterStatBalancer.CalculatePower(
                s, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange,
                avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange,
                baseVelocity, universalSimDist, powerOffset);
            
            s._CalculatedPower = calculatedPower;
            s._ValueDiscrepancy = calculatedPower - s._spawnCost;

            UnitValueDiscrepancies[i] = s._ValueDiscrepancy;

            tAttack += s.Attack;
            tDefense += s.Defense;
            tSpaceControl += s.SpaceControl;
            tAttackFreq += s.AttackFrequency;
            tLevel += s.Level_Total;

            tMove += s._MoveSpeed;
            tKBD  += s._KnockBackDamage;
            tAD   += s._AttackDamage;
            tEnd  += s._ExtraEndlag;
            tHP   += s._MaxHealth;
            tKBH  += s._KnockBackMaxHealth;
            tRng  += s._HorizontalRange;
        }

        int count = all_stats_scripts.Length;
        TotalLevel = tLevel;
        SumAttack = tAttack;
        SumDefense = tDefense;
        SumSpaceControl = tSpaceControl;
        AvgAttackFrequency = tAttackFreq / count;

        AvgMove = tMove / count;
        AvgKB_Dmg = tKBD / count;
        AvgAtk_Dmg = tAD / count;
        AvgEndlag = tEnd / count;
        AvgHP = tHP / count;
        AvgKB_HP = tKBH / count;
        AvgRange = tRng / count;
    }
}