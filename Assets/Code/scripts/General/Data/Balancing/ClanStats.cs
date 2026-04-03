using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "ClanStats", menuName = "Clan Stats")]
public class ClanStats : ScriptableObject
{
    [Header("Clan Identity")]
    public string ClanTheme;
    [TextArea(3, 6)] public string Characteristics;

    public ScriptableStats[] all_stats_scripts;
    public float AvgPushingPower; 
    public float AvgDPS;         
    public float AvgDefense;

    [HideInInspector] public float TotalPower, TotalCost;
    [HideInInspector] public float TotalPushingPower, TotalDPS, TotalDefense;
    [HideInInspector] public float AvgMove, AvgKB_Dmg, AvgAtk_Dmg, AvgEndlag, AvgHP, AvgKB_HP, AvgRange;
    [HideInInspector] public float[] UnitValueDiscrepancies;

    public void UpdateAverages(
    float wAtk, float wEnd, float wMove, float wKB_Dmg, float wHP, float wKB_HP, float wRange, float wAOE,
    float avgHP, float avgKB_HP, float avgMove, float avgKB_Dmg,
    float avgAtk, float avgEndlag, float avgRange,
    float baseVelocity, float baseAngle,
    float universalSimDist, float universalMaxStat) // <-- Match the new signature
    {
        float tPower = 0;
        float tPush = 0;
        float tDPS = 0;
        float tDef = 0;
        int count = 0;

        foreach (var s in all_stats_scripts)
        {
            if (s == null) continue;

            // Pass the new universal parameters here
            s.RefreshBalancing(
                wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, wAOE,
                avgHP, avgKB_HP, avgMove, avgKB_Dmg,
                avgAtk, avgEndlag, avgRange,
                baseVelocity, baseAngle, 
                universalSimDist, universalMaxStat);

            tPower += s._CalculatedPower;
            
            // Fix: Use the new 'Calculated_Totals' struct paths
            tPush += s.Calculated_Totals.PushingPower; 
            tDPS  += s.Calculated_Totals.DPS;
            tDef  += s.Calculated_Totals.Defense_TTK;
            count++;
        }

        if (count == 0) return;

        TotalPower = tPower / count;
        AvgPushingPower = tPush / count;
        AvgDPS = tDPS / count;
        AvgDefense = tDef / count;
    }
}