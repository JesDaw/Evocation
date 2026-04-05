using UnityEngine;

[CreateAssetMenu(fileName = "ClanStats", menuName = "Clan Stats")]
public class ClanStats : ScriptableObject
{
    public string ClanTheme;
    [TextArea(3, 6)] public string Characteristics;

    public ScriptableStats[] all_stats_scripts;

    [Header("Clan Totals & Averages")]
    public float TotalPower;
    public float TotalPushingPower;
    public float TotalDPS;
    public float TotalDefense;

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

        float tPower = 0, tPush = 0, tDef = 0;
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

            tPower += calculatedPower;
            UnitValueDiscrepancies[i] = s._ValueDiscrepancy;

            tMove += s._MoveSpeed;
            tKBD  += s._KnockBackDamage;
            tAD   += s._AttackDamage;
            tEnd  += s._AttackEndlag;
            tHP   += s._MaxHealth;
            tKBH  += s._KnockBackMaxHealth;
            tRng  += s._HorizontalRange;

            tDef  += s._MaxHealth;
            tPush += s._KnockBackMaxHealth;
        }

        int count = all_stats_scripts.Length;
        TotalPower = tPower / count;
        TotalDefense = tDef / count;
        TotalPushingPower = tPush / count;

        AvgMove = tMove / count;
        AvgKB_Dmg = tKBD / count;
        AvgAtk_Dmg = tAD / count;
        AvgEndlag = tEnd / count;
        AvgHP = tHP / count;
        AvgKB_HP = tKBH / count;
        AvgRange = tRng / count;
    }
}