using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TeamManager : MonoBehaviour
{
    public List<Stats> UnitStats;
    public void UpdateCurrentTeam(GameObject _Units)
    {
        CleanUp();
        Stats unitStat = _Units?.GetComponent<Stats>();
        if (unitStat == null) Debug.LogWarning("Unit does not have stats?");

        UnitStats.Add(unitStat);
    }

    void CleanUp()
    {
        UnitStats.RemoveAll(u => u == null);
    }
}
