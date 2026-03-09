using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Convert;

[CreateAssetMenu(fileName = "ClanStats", menuName = "Clan Stats")]
public class ClanStats : ScriptableObject
{
    
    public ScriptableStats[] all_stats_scripts;

    public Dictionary<string, float> averages = new Dictionary<string, float>
        {
          {"Avg. MaxHP", GetAverageOf("_MaxHealth")},
          {"Avg. MoveSpeed", GetAverageOf("_MoveSpeed")},
          {"Avg. Cost", GetAverageOf("_spawnCost")},
          {"Avg. Damage", GetAverageOf("_AttackDamage")},
          {"Avg. Endlag", GetAverageOf("_AttackEndlag")},
          {"Avg. Knockback MaxHP", GetAverageOf("_KnockBackMaxHealth")},
          {"Avg. Knockback Damage", GetAverageOf("_KnockBackDamage")},  
        };

    // No clue if the "ref" is needed because this is my first time using c#, I'll leave it there just in case... I'm used to it - chris

    public void UpdateAverages(ref Dictionary<string, float> average_stats)
    {
        
    }
    public float GetAverageOf(string property_name)
    {
        int number_of_obj = 0;
        float cur_total = 0f;

        foreach (ScriptableStats enemy_stats in all_stats_scripts) {

            number_of_obj += 1;
            object value = GetValueByName(enemy_stats, property_name); 
            
            if (value is not float) {
                cur_total += Convert.ToSingle(value);
                continue;
            }
            
            cur_total += value;
        }

        return number_of_obj/cur_total;
    }

}