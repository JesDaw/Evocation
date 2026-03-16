using UnityEngine;

public class MasterBalancingScript : MonoBehaviour
{

    public ClanStats[] all_clan_stats;
    private Component[] displays;
    [Header("Summary")]
    public float OverallAvgCosts;
    public float OverallPunchingPower;
    public float OverallDPS;
    public float OverallDefense;


    void Start()
    {
        displays = GetComponentsInParent<OverallStatsDisplay>();

    }
    void Update()
    {
        float oAvgCost = 0f;
        float oPunchingPower = 0f;
        float oDPS = 0f;
        float oDefense = 0f;

        foreach (ClanStats clan in all_clan_stats)
        {
            clan.UpdateAverages();
            oAvgCost += clan.AvgCost;
            oPunchingPower += clan.PunchingPower;
            oDPS += clan.DPS;
            oDefense += clan.Defense;

            foreach (OverallStatsDisplay display in displays)
            {
                if (clan.name == display.ClanToDisplay.name)
                {
                    display.AvgCosts = clan.AvgCost;
                    display.PunchingPower = clan.PunchingPower;
                    display.DPS = clan.DPS;
                    display.Defense = clan.Defense;
                }
            }
        }
        OverallAvgCosts = oAvgCost / all_clan_stats.Length;
        OverallPunchingPower = oPunchingPower / all_clan_stats.Length;
        OverallDPS = oDPS / all_clan_stats.Length;
        OverallDefense = oDefense / all_clan_stats.Length;
    }
}
