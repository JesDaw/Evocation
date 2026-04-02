using UnityEngine;

[ExecuteInEditMode]
public class MasterBalancingScript : MonoBehaviour
{
    [Header("1. Clan Roster")]
    public ClanStats[] all_clan_stats;

    [Header("2. Global Power Curve")]
    public AnimationCurve GlobalPowerCurve = new AnimationCurve();

    private BalancingGrapher grapher;

    void Update()
    {
        if (all_clan_stats == null) return;

        // Ensure Grapher exists and is at the TOP
        if (grapher == null) 
        {
            grapher = GetComponent<BalancingGrapher>();
            if (grapher == null) grapher = gameObject.AddComponent<BalancingGrapher>();
            
            #if UNITY_EDITOR
            // This moves the Grapher to the top of the inspector list
            for (int i = 0; i < 10; i++) UnityEditorInternal.ComponentUtility.MoveComponentUp(grapher);
            #endif
        }

        grapher.UpdateGraphs();

        GlobalPowerCurve = new AnimationCurve();
        for (int i = 0; i < all_clan_stats.Length; i++)
        {
            if (all_clan_stats[i] == null) continue;

            all_clan_stats[i].UpdateAverages(
                grapher.Weight_AttackDamage, grapher.Weight_AttackEndlag, 
                grapher.Weight_MoveSpeed, grapher.Weight_KnockBackDamage,
                grapher.Weight_MaxHealth, grapher.Weight_KnockBackHealth, 
                grapher.Weight_HorizontalRange, grapher.Weight_AOE_Efficiency);
            
            GlobalPowerCurve.AddKey(i, all_clan_stats[i].TotalPower);
        }

        SyncDisplayComponents();
    }

    void SyncDisplayComponents()
    {
        var existingDisplays = GetComponents<OverallStatsDisplay>();
        
        // Match display count to clan count
        if (existingDisplays.Length > all_clan_stats.Length)
        {
            for (int i = existingDisplays.Length - 1; i >= all_clan_stats.Length; i--)
                DestroyImmediate(existingDisplays[i]);
        }

        for (int i = 0; i < all_clan_stats.Length; i++)
        {
            if (all_clan_stats[i] == null) continue;
            OverallStatsDisplay d;
            if (i < existingDisplays.Length) d = existingDisplays[i];
            else d = gameObject.AddComponent<OverallStatsDisplay>();
            
            d.Clan = all_clan_stats[i];
            d.SyncWithClan();
        }
    }
}