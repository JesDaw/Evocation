using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "ClanStats", menuName = "Clan Stats")]
public class ClanStats : ScriptableObject
{
    [Header("Clan Identity")]
    public string ClanTheme;
    [TextArea(3, 6)] public string Characteristics;
    
    public ScriptableStats[] all_stats_scripts;

    [HideInInspector] public float TotalPower, TotalCost;
    [HideInInspector] public float TotalPushingPower, TotalDPS, TotalDefense;
    [HideInInspector] public float AvgMove, AvgKB_Dmg, AvgAtk_Dmg, AvgEndlag, AvgHP, AvgKB_HP, AvgRange;
    [HideInInspector] public AnimationCurve InternalUnitPowerScaling;

    public void UpdateAverages(float wAtk, float wEnd, float wMove, float wKB_Dmg, float wHP, float wKB_HP, float wRange, float wAOE)
    {
        if (all_stats_scripts == null || all_stats_scripts.Length == 0) return;

        TotalPower = 0; TotalCost = 0; 
        TotalPushingPower = 0; TotalDPS = 0; TotalDefense = 0;
        float tMove = 0, tKBD = 0, tAD = 0, tEnd = 0, tHP = 0, tKBH = 0, tRange = 0;
        InternalUnitPowerScaling = new AnimationCurve();

        var sorted = all_stats_scripts.OrderBy(s => s._spawnCost).ToArray();

        for (int i = 0; i < sorted.Length; i++)
        {
            var s = sorted[i];
            if (s == null) continue;

            s.RefreshBalancing(wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, wAOE);

            TotalPower += s._CalculatedPower;
            TotalCost += s._spawnCost;
            TotalPushingPower += s._Calc_PushingPower;
            TotalDPS += s._Calc_DPS;
            TotalDefense += s._Calc_Defense;

            tMove += s._MoveSpeed; tKBD += s._KnockBackDamage;
            tAD += s._AttackDamage; tEnd += s._AttackEndlag;
            tHP += s._MaxHealth; tKBH += s._KnockBackMaxHealth;
            tRange += s._HorizontalRange;

            InternalUnitPowerScaling.AddKey(i, s._CalculatedPower);
        }

        float count = sorted.Length;
        AvgMove = tMove / count; AvgKB_Dmg = tKBD / count;
        AvgAtk_Dmg = tAD / count; AvgEndlag = tEnd / count;
        AvgHP = tHP / count; AvgKB_HP = tKBH / count;
        AvgRange = tRange / count;
    }
}